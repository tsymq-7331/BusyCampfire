using System.Runtime.CompilerServices;
using BusyCampfire.BusyCampfireCode.Config;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Sync;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Timeline.Epochs;
using BusyCampfire.BusyCampfireCode.Diagnostics;

namespace BusyCampfire.BusyCampfireCode.Patches;

/// <summary>
/// Adds an act-appropriate event after the vanilla Shovel reward without
/// consuming the act's normal event grab bag or unknown-room RNG.
/// </summary>
internal static class BusyCampfireDigPatches
{
    private static EventModel? PendingEvent;
    private static bool IsEnteringPendingEvent;
    private static Task? PendingTransitionTask;
    private static readonly HashSet<ulong> FinishedPlayers = [];
    private static readonly ConditionalWeakTable<Player, ShovelEventState> VisitedShovelEvents = new();

    private static bool Enabled =>
        MainFile.IsInitialized &&
        MainFile.RuntimeMode.GameplayChangesAllowed &&
        BusyCampfireConfig.EnableDigEvents;

    [HarmonyPatch(typeof(DigRestSiteOption), nameof(DigRestSiteOption.OnSelect))]
    private static class DigOnSelectPatch
    {
        private static void Postfix(DigRestSiteOption __instance, ref Task<bool> __result)
        {
            if (Enabled)
                __result = CompleteAndChooseEvent(__instance, __result);
        }
    }

    [HarmonyPatch(typeof(RestSiteSynchronizer), nameof(RestSiteSynchronizer.BeginRestSite))]
    private static class RestSiteBeginPatch
    {
        private static void Prefix()
        {
            PendingEvent = null;
            PendingTransitionTask = null;
            IsEnteringPendingEvent = false;
            FinishedPlayers.Clear();
        }
    }

    [HarmonyPatch(typeof(RestSiteSynchronizer), "HandleRestSiteSkippedMessage")]
    private static class RemotePlayerFinishedPatch
    {
        private static void Postfix(ulong senderId)
        {
            FinishedPlayers.Add(senderId);
            TryStartPendingEvent();
        }
    }

    [HarmonyPatch(typeof(RestSiteSynchronizer), "OnPeerDisconnected")]
    private static class DisconnectedPlayerFinishedPatch
    {
        private static void Postfix(ulong peerId)
        {
            FinishedPlayers.Add(peerId);
            TryStartPendingEvent();
        }
    }

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.EnterMapCoord))]
    private static class NextMapRoomPatch
    {
        private static bool Prefix(ref Task __result)
        {
            if (!Enabled || (PendingEvent == null && !IsEnteringPendingEvent))
                return true;

            // A pending shovel event must finish before any selected map node
            // can be recorded or entered. Players can return from the map and
            // explicitly finish their campfire using the added button.
            __result = AllPlayersExplicitlyFinished()
                ? EnsurePendingEventTransition()
                : Task.CompletedTask;
            return false;
        }
    }

    [HarmonyPatch(typeof(NRestSiteRoom), nameof(NRestSiteRoom._Ready))]
    private static class RestSiteFinishButtonPatch
    {
        private const string ButtonName = "BusyCampfireFinishButton";

        private static void Postfix(NRestSiteRoom __instance)
        {
            if (__instance.HasNode(ButtonName))
                return;

            Button button = new()
            {
                Name = ButtonName,
                Text = "结束火堆",
                AnchorLeft = 1f,
                AnchorTop = 1f,
                AnchorRight = 1f,
                AnchorBottom = 1f,
                OffsetLeft = -610f,
                OffsetTop = -145f,
                OffsetRight = -390f,
                OffsetBottom = -75f,
                FocusMode = Control.FocusModeEnum.All
            };
            button.AddThemeFontSizeOverride("font_size", 28);
            button.Pressed += () => FinishLocalCampfire(__instance, button);
            __instance.AddChild(button);
        }
    }

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.CleanUp))]
    private static class RunCleanupPatch
    {
        private static void Postfix()
        {
            PendingEvent = null;
            IsEnteringPendingEvent = false;
            PendingTransitionTask = null;
        }
    }

    private static async Task<bool> CompleteAndChooseEvent(
        DigRestSiteOption option,
        Task<bool> originalTask)
    {
        bool succeeded = await originalTask;
        if (!succeeded)
            return false;

        Player owner = Traverse.Create(option).Property("Owner").GetValue<Player>();
        if (owner.RunState is not RunState runState)
            return true;

        if (owner.RunState.Players.Count > 1 && !BusyCampfireConfig.EnableDigEventsInMultiplayer)
            return true;

        ShovelEventState shovelState = GetShovelState(owner);
        List<EventModel> candidates = runState.Act.AllEvents
            .Where(eventModel => IsEligible(eventModel, runState))
            .Where(eventModel => !shovelState.VisitedIds.Contains(eventModel.Id.Entry))
            .OrderBy(eventModel => eventModel.Id.Entry, StringComparer.Ordinal)
            .ToList();
        if (candidates.Count == 0 && shovelState.VisitedIds.Count > 0)
        {
            shovelState.VisitedIds.Clear();
            candidates = runState.Act.AllEvents
                .Where(eventModel => IsEligible(eventModel, runState))
                .OrderBy(eventModel => eventModel.Id.Entry, StringComparer.Ordinal)
                .ToList();
        }
        if (candidates.Count == 0)
            return true;

        ulong mixin = ((ulong)(uint)owner.RunState.CurrentActIndex << 32) |
                      (uint)owner.RunState.TotalFloor;
        Rng rng = new(owner, ModelDb.Relic<Shovel>().Id, mixin);
        EventModel selectedEvent = candidates[rng.NextInt(candidates.Count)];

        shovelState.VisitedIds.Add(selectedEvent.Id.Entry);
        CampfireTestLog.Write(owner, "Dig/挖掘", $"Event={selectedEvent.Id.Entry}, Candidates={candidates.Count}");
        PendingEvent ??= selectedEvent;
        return true;
    }

    private static void FinishLocalCampfire(NRestSiteRoom room, Button button)
    {
        RestSiteSynchronizer synchronizer = RunManager.Instance.RestSiteSynchronizer;
        var runState = RunManager.Instance.DebugOnlyGetState();
        Player? localPlayer = runState == null ? null : LocalContext.GetMe(runState);
        if (localPlayer == null || !FinishedPlayers.Add(localPlayer.NetId))
            return;

        synchronizer.BeforeLocalRestSiteExited();
        room.DisableOptions();
        button.Disabled = true;
        button.Text = "等待其他玩家…";

        if (localPlayer.RunState.Players.Count > 1)
        {
            RunLocationTargetedMessageBuffer messageBuffer = Traverse.Create(synchronizer)
                .Field("_messageBuffer")
                .GetValue<RunLocationTargetedMessageBuffer>();
            RunManager.Instance.NetService.SendMessage(new RestSiteSkippedMessage
            {
                Location = messageBuffer.CurrentLocation
            });
        }

        TryStartPendingEvent();
        if (PendingEvent != null || IsEnteringPendingEvent)
            return;

        // Without a shovel event, this is simply an explicit vanilla skip.
        NMapScreen.Instance?.Open();
    }

    private static bool AllPlayersExplicitlyFinished()
    {
        var runState = RunManager.Instance.DebugOnlyGetState();
        return runState != null && runState.Players.All(player => FinishedPlayers.Contains(player.NetId));
    }

    private static void TryStartPendingEvent()
    {
        if (PendingEvent != null && AllPlayersExplicitlyFinished())
            _ = TaskHelper.RunSafely(EnsurePendingEventTransition());
    }

    private static Task EnsurePendingEventTransition()
    {
        if (PendingTransitionTask != null)
            return PendingTransitionTask;

        PendingTransitionTask = WaitForPartyAndEnterPendingEvent();
        return PendingTransitionTask;
    }

    private static async Task WaitForPartyAndEnterPendingEvent()
    {
        await RunManager.Instance.RestSiteSynchronizer.AfterAllRestSitesCompleted();

        EventModel? selectedEvent = PendingEvent;
        if (selectedEvent == null)
            return;

        PendingEvent = null;
        IsEnteringPendingEvent = true;
        try
        {
            await RunManager.Instance.EnterRoomWithoutExitingCurrentRoom(
                new EventRoom(selectedEvent),
                fadeToBlack: true);
        }
        finally
        {
            IsEnteringPendingEvent = false;
            PendingTransitionTask = null;
        }
    }

    private static ShovelEventState GetShovelState(Player player)
    {
        ShovelEventState state = VisitedShovelEvents.GetOrCreateValue(player);
        if (state.ActIndex != player.RunState.CurrentActIndex)
        {
            state.ActIndex = player.RunState.CurrentActIndex;
            state.VisitedIds.Clear();
        }

        return state;
    }

    private static bool IsEligible(EventModel eventModel, RunState runState)
    {
        if (eventModel is AncientEventModel or DeprecatedEvent or FakeMerchant or Neow or TheArchitect)
            return false;
        if (eventModel.IsShared || eventModel.LayoutType == EventLayoutType.Combat || eventModel.CanonicalEncounter != null)
            return false;
        if (!eventModel.IsAllowed(runState))
            return false;
        if (!runState.UnlockState.IsEpochRevealed<Event1Epoch>() && Event1Epoch.Events.Any(e => e.Id == eventModel.Id))
            return false;
        if (!runState.UnlockState.IsEpochRevealed<Event2Epoch>() && Event2Epoch.Events.Any(e => e.Id == eventModel.Id))
            return false;
        if (!runState.UnlockState.IsEpochRevealed<Event3Epoch>() && Event3Epoch.Events.Any(e => e.Id == eventModel.Id))
            return false;
        return true;
    }

    private sealed class ShovelEventState
    {
        internal int ActIndex { get; set; } = -1;
        internal HashSet<string> VisitedIds { get; } = [];
    }
}

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
    private static bool IsShovelEventActive;
    private static bool IsPartyEventFinished;
    private static bool IsLocalCampfireFinished;
    private static Task? PendingTransitionTask;
    private static readonly List<EventModel> TrackedShovelEvents = [];
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
            ClearShovelEventTracking();
            IsLocalCampfireFinished = false;
        }
    }

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.EnterMapCoord))]
    private static class NextMapRoomPatch
    {
        private static bool Prefix(ref Task __result)
        {
            if (!Enabled || (PendingEvent == null && !IsEnteringPendingEvent && !IsShovelEventActive))
                return true;

            if (IsShovelEventActive)
            {
                if (!IsPartyEventFinished)
                {
                    __result = Task.CompletedTask;
                    return false;
                }

                ClearShovelEventTracking();
                return true;
            }

            __result = EnsurePendingEventTransition();
            return false;
        }
    }

    [HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.Open))]
    private static class MapOpenPatch
    {
        private static bool Prefix()
        {
            // A non-shared event gives every player an independent Proceed
            // button. Keep the party off the map until every copy finishes.
            return !IsShovelEventActive || IsPartyEventFinished;
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
            ClearShovelEventTracking();
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
        if (localPlayer == null || IsLocalCampfireFinished)
            return;

        IsLocalCampfireFinished = true;
        synchronizer.BeforeLocalRestSiteExited();
        room.DisableOptions();
        button.Disabled = true;
        button.Text = "等待其他玩家…";

        // BeforeLocalRestSiteExited already broadcasts RestSiteSkippedMessage
        // when options remain. A second manual message caused duplicate finish
        // processing on every peer.

        if (PendingEvent != null || IsEnteringPendingEvent)
        {
            _ = TaskHelper.RunSafely(EnsurePendingEventTransition());
            return;
        }

        // Without a shovel event, this is simply an explicit vanilla skip.
        NMapScreen.Instance?.Open();
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
            EventRoom eventRoom = new(selectedEvent);
            await RunManager.Instance.EnterRoomWithoutExitingCurrentRoom(eventRoom, fadeToBlack: true);
            TrackShovelEvents();
        }
        finally
        {
            IsEnteringPendingEvent = false;
            PendingTransitionTask = null;
        }
    }

    private static void TrackShovelEvents()
    {
        ClearShovelEventTracking();
        IsShovelEventActive = true;

        foreach (EventModel eventModel in RunManager.Instance.EventSynchronizer.Events)
        {
            TrackedShovelEvents.Add(eventModel);
            eventModel.StateChanged += OnShovelEventStateChanged;
        }

        CheckForPartyEventCompletion();
    }

    private static void OnShovelEventStateChanged(EventModel _)
    {
        CheckForPartyEventCompletion();
    }

    private static void CheckForPartyEventCompletion()
    {
        if (!IsShovelEventActive || TrackedShovelEvents.Count == 0 ||
            TrackedShovelEvents.Any(eventModel => !eventModel.IsFinished))
            return;

        IsPartyEventFinished = true;
        NMapScreen.Instance?.SetTravelEnabled(enabled: true);
        NMapScreen.Instance?.Open();
    }

    private static void ClearShovelEventTracking()
    {
        foreach (EventModel eventModel in TrackedShovelEvents)
            eventModel.StateChanged -= OnShovelEventStateChanged;

        TrackedShovelEvents.Clear();
        IsShovelEventActive = false;
        IsPartyEventFinished = false;
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

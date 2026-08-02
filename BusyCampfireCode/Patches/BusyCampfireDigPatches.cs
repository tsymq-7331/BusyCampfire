using System.Runtime.CompilerServices;
using BusyCampfire.BusyCampfireCode.Config;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer.Game;
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
    private static readonly ConditionalWeakTable<Player, ShovelEventState> VisitedShovelEvents = new();

    private static bool Enabled =>
        MainFile.IsInitialized &&
        MainFile.RuntimeMode.GameplayChangesAllowed &&
        SpireConfig.EnableDigEvents;

    [HarmonyPatch(typeof(DigRestSiteOption), nameof(DigRestSiteOption.OnSelect))]
    private static class DigOnSelectPatch
    {
        private static void Postfix(DigRestSiteOption __instance, ref Task<bool> __result)
        {
            if (Enabled)
                __result = CompleteAndChooseEvent(__instance, __result);
        }
    }

    [HarmonyPatch(typeof(RestSiteSynchronizer), "AfterAllRestSitesCompleted")]
    private static class RestSiteCompletionPatch
    {
        private static void Postfix(ref Task __result)
        {
            if (!Enabled || PendingEvent == null)
            {
                return;
            }

            EventModel selectedEvent = PendingEvent;
            PendingEvent = null;
            __result = CompleteRestSiteAndEnterEvent(__result, selectedEvent);
        }
    }

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.CleanUp))]
    private static class RunCleanupPatch
    {
        private static void Postfix() => PendingEvent = null;
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

        if (owner.RunState.Players.Count > 1 && !SpireConfig.EnableDigEventsInMultiplayer)
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

    private static async Task CompleteRestSiteAndEnterEvent(
        Task originalCompletion,
        EventModel selectedEvent)
    {
        await originalCompletion;
        await RunManager.Instance.EnterRoomWithoutExitingCurrentRoom(
            new EventRoom(selectedEvent),
            fadeToBlack: true);
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

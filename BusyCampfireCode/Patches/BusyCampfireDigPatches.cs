using System.Runtime.CompilerServices;
using BusyCampfire.BusyCampfireCode.Config;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
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
    private static readonly ConditionalWeakTable<DigRestSiteOption, EventModel> PendingEvents = new();

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

    [HarmonyPatch(typeof(DigRestSiteOption), nameof(DigRestSiteOption.DoLocalPostSelectVfx))]
    private static class DigLocalVfxPatch
    {
        private static void Postfix(DigRestSiteOption __instance, ref Task __result)
        {
            if (Enabled && PendingEvents.TryGetValue(__instance, out EventModel? selectedEvent))
                __result = CompleteVfxAndEnterEvent(__instance, selectedEvent, __result);
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

        // A non-shared EventRoom still creates an event model for every player.
        // Until a player-scoped synchronizer exists, do not risk granting the
        // event to teammates.
        if (owner.RunState.Players.Count > 1)
        {
            if (SpireConfig.EnableDigEventsInMultiplayer)
                MainFile.Logger.Warn("Skipped Shovel bonus event: player-scoped multiplayer synchronization is not available yet.");
            return true;
        }

        List<EventModel> candidates = runState.Act.AllEvents
            .Where(eventModel => IsEligible(eventModel, runState))
            .OrderBy(eventModel => eventModel.Id.Entry, StringComparer.Ordinal)
            .ToList();
        if (candidates.Count == 0)
            return true;

        ulong mixin = ((ulong)(uint)owner.RunState.CurrentActIndex << 32) |
                      (uint)owner.RunState.TotalFloor;
        Rng rng = new(owner, ModelDb.Relic<Shovel>().Id, mixin);
        EventModel selectedEvent = candidates[rng.NextInt(candidates.Count)];

        runState.AddVisitedEvent(selectedEvent);
        CampfireTestLog.Write(owner, "Dig/挖掘", $"Event={selectedEvent.Id.Entry}, Candidates={candidates.Count}");
        PendingEvents.Remove(option);
        PendingEvents.Add(option, selectedEvent);
        return true;
    }

    private static async Task CompleteVfxAndEnterEvent(
        DigRestSiteOption option,
        EventModel selectedEvent,
        Task originalVfx)
    {
        await originalVfx;
        PendingEvents.Remove(option);
        await RunManager.Instance.EnterRoomWithoutExitingCurrentRoom(
            new EventRoom(selectedEvent),
            fadeToBlack: true);
    }

    private static bool IsEligible(EventModel eventModel, RunState runState)
    {
        if (eventModel is AncientEventModel or DeprecatedEvent or FakeMerchant or Neow or TheArchitect)
            return false;
        if (eventModel.IsShared || eventModel.LayoutType == EventLayoutType.Combat || eventModel.CanonicalEncounter != null)
            return false;
        if (runState.VisitedEventIds.Contains(eventModel.Id) || !eventModel.IsAllowed(runState))
            return false;
        if (!runState.UnlockState.IsEpochRevealed<Event1Epoch>() && Event1Epoch.Events.Any(e => e.Id == eventModel.Id))
            return false;
        if (!runState.UnlockState.IsEpochRevealed<Event2Epoch>() && Event2Epoch.Events.Any(e => e.Id == eventModel.Id))
            return false;
        if (!runState.UnlockState.IsEpochRevealed<Event3Epoch>() && Event3Epoch.Events.Any(e => e.Id == eventModel.Id))
            return false;
        return true;
    }
}

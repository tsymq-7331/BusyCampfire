using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace BusyCampfire.BusyCampfireCode.Patches;

/// <summary>
/// The vanilla Mend option heals the selected teammate but does not dispatch
/// the healer's own rest rewards. Busy Campfire treats the successful action
/// as the healer resting for relic-trigger purposes while leaving the actual
/// teammate heal amount unchanged.
/// </summary>
internal static class BusyCampfireMendPatches
{
    private static bool Enabled => MainFile.IsInitialized && MainFile.RuntimeMode.GameplayChangesAllowed;

    [HarmonyPatch(typeof(MendRestSiteOption), nameof(MendRestSiteOption.OnSelect))]
    private static class MendOnSelectPatch
    {
        private static void Postfix(MendRestSiteOption __instance, ref Task<bool> __result)
        {
            if (Enabled)
                __result = CompleteAndTriggerOwnerRelics(__instance, __result);
        }
    }

    private static async Task<bool> CompleteAndTriggerOwnerRelics(
        MendRestSiteOption option,
        Task<bool> originalTask)
    {
        bool succeeded = await originalTask;
        if (!succeeded)
            return false;

        Player owner = Traverse.Create(option).Property("Owner").GetValue<Player>();

        RegalPillow? pillow = owner.GetRelic<RegalPillow>();
        if (pillow != null)
        {
            await CreatureCmd.Heal(owner.Creature, pillow.DynamicVars.Heal.BaseValue);
            await pillow.AfterRestSiteHeal(owner, isMimicked: false);
        }

        List<Reward> rewards = [];
        owner.GetRelic<StoneHumidifier>()?.TryModifyRestSiteHealRewards(owner, rewards, isMimicked: false);
        owner.GetRelic<TinyMailbox>()?.TryModifyRestSiteHealRewards(owner, rewards, isMimicked: false);
        owner.GetRelic<DreamCatcher>()?.TryModifyRestSiteHealRewards(owner, rewards, isMimicked: false);

        if (rewards.Count > 0)
            await RewardsCmd.OfferCustom(owner, rewards);

        return true;
    }
}

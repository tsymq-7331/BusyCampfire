using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Relics;

namespace BusyCampfire.BusyCampfireCode.Patches;

/// <summary>
/// Balance changes from Busy Campfire that can reuse the game's native relic,
/// reward and multiplayer paths. Keeping these changes at the model boundary
/// also makes descriptions and actual effects use the same values.
/// </summary>
internal static class BusyCampfireRelicPatches
{
    private const decimal CookMaxHpGain = 9m;
    private const decimal VanillaCookMaxHpGain = 5m;
    private static bool Enabled => MainFile.IsInitialized && MainFile.RuntimeMode.GameplayChangesAllowed;

    [HarmonyPatch(typeof(VenerableTeaSet), "get_CanonicalVars")]
    private static class VenerableTeaSetVarsPatch
    {
        private static void Postfix(ref IEnumerable<DynamicVar> __result)
        {
            if (Enabled)
                __result = [new EnergyVar(3)];
        }
    }

    [HarmonyPatch(typeof(FakeVenerableTeaSet), "get_CanonicalVars")]
    private static class FakeVenerableTeaSetVarsPatch
    {
        private static void Postfix(ref IEnumerable<DynamicVar> __result)
        {
            if (Enabled)
                __result = [new EnergyVar(2)];
        }
    }

    [HarmonyPatch(typeof(EternalFeather), "get_CanonicalVars")]
    private static class EternalFeatherVarsPatch
    {
        private static void Postfix(ref IEnumerable<DynamicVar> __result)
        {
            if (Enabled)
                __result = [new CardsVar(5), new HealVar(5m)];
        }
    }

    [HarmonyPatch(typeof(Girya), nameof(Girya.TryModifyRestSiteOptions))]
    private static class UnlimitedGiryaPatch
    {
        private static void Postfix(
            Girya __instance,
            MegaCrit.Sts2.Core.Entities.Players.Player player,
            ICollection<RestSiteOption> options,
            ref bool __result)
        {
            if (!Enabled || player != __instance.Owner || options.Any(option => option is LiftRestSiteOption))
                return;

            options.Add(new LiftRestSiteOption(player));
            __result = true;
        }
    }

    [HarmonyPatch(typeof(CookRestSiteOption), "get_Description")]
    private static class CookDescriptionPatch
    {
        private static void Postfix(CookRestSiteOption __instance, ref MegaCrit.Sts2.Core.Localization.LocString __result)
        {
            if (Enabled && __instance.IsEnabled)
                __result.Add("MaxHp", CookMaxHpGain);
        }
    }

    [HarmonyPatch(typeof(CookRestSiteOption), nameof(CookRestSiteOption.OnSelect))]
    private static class CookMaxHpPatch
    {
        private static void Postfix(CookRestSiteOption __instance, ref Task<bool> __result)
        {
            if (Enabled)
                __result = AddBusyCampfireCookBonus(__instance, __result);
        }
    }

    private static async Task<bool> AddBusyCampfireCookBonus(
        CookRestSiteOption option,
        Task<bool> originalTask)
    {
        bool succeeded = await originalTask;
        if (!succeeded)
            return false;

        var owner = Traverse.Create(option).Property("Owner")
            .GetValue<MegaCrit.Sts2.Core.Entities.Players.Player>();
        await CreatureCmd.GainMaxHp(owner.Creature, CookMaxHpGain - VanillaCookMaxHpGain);
        return true;
    }

}

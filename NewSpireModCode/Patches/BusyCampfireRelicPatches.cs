using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Relics;

namespace NewSpireMod.NewSpireModCode.Patches;

/// <summary>
/// Balance changes from Busy Campfire that can reuse the game's native relic,
/// reward and multiplayer paths. Keeping these changes at the model boundary
/// also makes descriptions and actual effects use the same values.
/// </summary>
internal static class BusyCampfireRelicPatches
{
    [HarmonyPatch(typeof(VenerableTeaSet), "get_CanonicalVars")]
    private static class VenerableTeaSetVarsPatch
    {
        private static void Postfix(ref IEnumerable<DynamicVar> __result) =>
            __result = [new EnergyVar(3)];
    }

    [HarmonyPatch(typeof(FakeVenerableTeaSet), "get_CanonicalVars")]
    private static class FakeVenerableTeaSetVarsPatch
    {
        private static void Postfix(ref IEnumerable<DynamicVar> __result) =>
            __result = [new EnergyVar(2)];
    }

    [HarmonyPatch(typeof(EternalFeather), "get_CanonicalVars")]
    private static class EternalFeatherVarsPatch
    {
        private static void Postfix(ref IEnumerable<DynamicVar> __result) =>
            __result = [new CardsVar(5), new HealVar(5m)];
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
            if (player != __instance.Owner || options.Any(option => option is LiftRestSiteOption))
                return;

            options.Add(new LiftRestSiteOption(player));
            __result = true;
        }
    }
}

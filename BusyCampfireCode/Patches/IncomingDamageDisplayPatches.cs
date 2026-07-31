using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using BusyCampfire.BusyCampfireCode.Combat;

namespace BusyCampfire.BusyCampfireCode.Patches;

[HarmonyPatch]
internal static class IncomingDamageDisplayPatches
{
    [HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
    [HarmonyPostfix]
    private static void AttachLabel(NCreature __instance)
    {
        IncomingDamageDisplay.AttachToLocalPlayer(__instance);
    }

    [HarmonyPatch(typeof(NCreature), nameof(NCreature.UpdateIntent))]
    [HarmonyPostfix]
    private static void AfterIntentUpdated()
    {
        IncomingDamageDisplay.QueueRefresh();
    }

    [HarmonyPatch(typeof(NIntent), "UpdateVisuals")]
    [HarmonyPostfix]
    private static void AfterIntentVisualsUpdated()
    {
        IncomingDamageDisplay.QueueRefresh();
    }
}

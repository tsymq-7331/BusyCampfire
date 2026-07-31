using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using BusyCampfire.BusyCampfireCode.Ui;

namespace BusyCampfire.BusyCampfireCode.Patches;

[HarmonyPatch(typeof(NGame), nameof(NGame._Ready))]
internal static class GlobalStatusHudPatch
{
    [HarmonyPostfix]
    private static void AttachHud(NGame __instance)
    {
        GlobalStatusHud.Attach(__instance);
    }
}

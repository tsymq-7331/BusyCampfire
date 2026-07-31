using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using NewSpireMod.NewSpireModCode.Ui;

namespace NewSpireMod.NewSpireModCode.Patches;

[HarmonyPatch(typeof(NGame), nameof(NGame._Ready))]
internal static class GlobalStatusHudPatch
{
    [HarmonyPostfix]
    private static void AttachHud(NGame __instance)
    {
        GlobalStatusHud.Attach(__instance);
    }
}

using HarmonyLib;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using BusyCampfire.BusyCampfireCode.Config;
using BusyCampfire.BusyCampfireCode.Runtime;

namespace BusyCampfire.BusyCampfireCode.Patches;

/// <summary>
/// Selects one of the game's native speed modes. It does not modify Engine
/// time scale, network timers, RNG, or gameplay actions.
/// </summary>
[HarmonyPatch(typeof(PrefsSave), nameof(PrefsSave.FastMode), MethodType.Getter)]
internal static class AnimationSpeedPatch
{
    [HarmonyPostfix]
    private static void ApplyAnimationSpeed(ref FastModeType __result)
    {
        if (!SpireConfig.EnableMod)
            return;

        switch (SpireConfig.AnimationSpeed)
        {
            case AnimationSpeedOverride.UseGameSetting:
                return;

            case AnimationSpeedOverride.Fast:
                __result = FastModeType.Fast;
                return;

            case AnimationSpeedOverride.Instant:
                bool vanillaCompatibleLobby =
                    MainFile.IsInitialized &&
                    MainFile.RuntimeMode.Current ==
                    RuntimeMode.VanillaCompatibleMultiplayer;

                __result = vanillaCompatibleLobby &&
                    !SpireConfig.AllowInstantSpeedWithVanillaPeers
                        ? FastModeType.Fast
                        : FastModeType.Instant;
                return;
        }
    }
}

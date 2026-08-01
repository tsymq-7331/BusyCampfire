using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using BusyCampfire.BusyCampfireCode.Config;

namespace BusyCampfire.BusyCampfireCode.Patches;

/// <summary>
/// Tracks the actual network service selected by the game. No custom packets are
/// sent, so players running the unmodified game can join normally.
/// </summary>
[HarmonyPatch]
internal static class RunModePatches
{
    [HarmonyPatch(typeof(RunManager), "InitializeRunLobby")]
    [HarmonyPostfix]
    private static void AfterRunLobbyInitialized(INetGameService netService)
    {
        if (!netService.Type.IsMultiplayer())
        {
            MainFile.RuntimeMode.EnterSinglePlayer();
        }
        else
        {
            // BusyCampfire declares affects_gameplay, so the game already requires every
            // multiplayer peer to load the same mod version. A second compatibility gate
            // here would disable every campfire patch even in a fully matched lobby.
            MainFile.RuntimeMode.EnterMultiplayer(everyPeerConfirmedCompatible: true);
        }

        MainFile.Modules.NotifyModeChanged();
        MainFile.Logger.Info($"本局运行模式：{MainFile.RuntimeMode.Current}");
    }

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.CleanUp))]
    [HarmonyPostfix]
    private static void AfterRunCleanUp()
    {
        MainFile.RuntimeMode.EnterSinglePlayer();
        MainFile.Modules.NotifyModeChanged();
    }
}

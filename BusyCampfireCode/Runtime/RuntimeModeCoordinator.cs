using BusyCampfire.BusyCampfireCode.Config;

namespace BusyCampfire.BusyCampfireCode.Runtime;

internal sealed class RuntimeModeCoordinator
{
    public RuntimeMode Current { get; private set; } = RuntimeMode.SinglePlayerFull;

    /// <summary>
    /// True only when it is safe to change cards, rewards, enemies, or run state.
    /// Display-only features never need this permission.
    /// </summary>
    public bool GameplayChangesAllowed =>
        BusyCampfireConfig.EnableMod && Current != RuntimeMode.VanillaCompatibleMultiplayer;

    public void EnterSinglePlayer() => SetMode(RuntimeMode.SinglePlayerFull);

    public void EnterMultiplayer(bool everyPeerConfirmedCompatible)
    {
        // Unknown/unmodded peers always fail closed. This prevents desync and
        // permits joining friends who run the unmodified game.
        SetMode(everyPeerConfirmedCompatible
            ? RuntimeMode.ModdedMultiplayerFull
            : RuntimeMode.VanillaCompatibleMultiplayer);
    }

    private void SetMode(RuntimeMode mode)
    {
        if (Current == mode)
            return;

        Current = mode;
        if (BusyCampfireConfig.LogModeChanges)
            MainFile.Logger.Info($"运行模式已切换：{mode}");
    }
}

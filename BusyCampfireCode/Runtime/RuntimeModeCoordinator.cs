using BusyCampfire.BusyCampfireCode.Config;
using BusyCampfire.BusyCampfireCode.Ui;

namespace BusyCampfire.BusyCampfireCode.Runtime;

internal sealed class RuntimeModeCoordinator
{
    public RuntimeMode Current { get; private set; } = RuntimeMode.SinglePlayerFull;

    /// <summary>
    /// True only when it is safe to change cards, rewards, enemies, or run state.
    /// Display-only features never need this permission.
    /// </summary>
    public bool GameplayChangesAllowed =>
        SpireConfig.EnableMod && Current != RuntimeMode.VanillaCompatibleMultiplayer;

    public void EnterSinglePlayer() => SetMode(RuntimeMode.SinglePlayerFull);

    public void EnterMultiplayer(bool everyPeerConfirmedCompatible)
    {
        // Unknown/unmodded peers always fail closed. This prevents desync and
        // permits joining friends who run the unmodified game.
        SetMode(everyPeerConfirmedCompatible
            ? RuntimeMode.ModdedMultiplayerFull
            : RuntimeMode.VanillaCompatibleMultiplayer);
    }

    public bool MayRun(ModuleKind kind) =>
        SpireConfig.EnableMod && (kind == ModuleKind.ClientOnly || GameplayChangesAllowed);

    private void SetMode(RuntimeMode mode)
    {
        if (Current == mode)
            return;

        Current = mode;
        if (SpireConfig.LogModeChanges)
            MainFile.Logger.Info($"运行模式已切换：{mode}");
        GlobalStatusHud.Refresh();
    }
}

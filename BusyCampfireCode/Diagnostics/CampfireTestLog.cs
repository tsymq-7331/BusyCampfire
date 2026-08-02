using BusyCampfire.BusyCampfireCode.Config;
using MegaCrit.Sts2.Core.Entities.Players;

namespace BusyCampfire.BusyCampfireCode.Diagnostics;

internal static class CampfireTestLog
{
    internal static void Write(Player player, string action, string details)
    {
        if (!BusyCampfireConfig.EnableDetailedTestLogs)
            return;

        int playerIndex = player.RunState.Players.ToList().IndexOf(player) + 1;
        MainFile.Logger.Info(
            $"[测试/Test] 玩家/Player {playerIndex} | {action} | " +
            $"幕/Act {player.RunState.CurrentActIndex}, 层/Floor {player.RunState.TotalFloor} | {details}");
    }
}

using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace BusyCampfire.BusyCampfireCode.Patches;

/// <summary>
/// Keeps unusually large rest-site option sets inside the visible area.
/// Vanilla-sized sets retain their original scale.
/// </summary>
[HarmonyPatch(typeof(NRestSiteRoom), "UpdateRestSiteOptions")]
internal static class RestSiteLayoutPatch
{
    private const int FullSizeOptionCount = 5;
    private const float MinimumScale = 0.58f;

    [HarmonyPostfix]
    private static void FitOptionsToScreen(NRestSiteRoom __instance, Control ____choicesContainer)
    {
        int optionCount = ____choicesContainer.GetChildCount();
        TaskHelper.RunSafely(ApplyAfterContainerLayout(__instance, ____choicesContainer, optionCount));
    }

    private static async Task ApplyAfterContainerLayout(
        NRestSiteRoom room,
        Control container,
        int optionCount)
    {
        // BoxContainer calculates its final size on the following process frame.
        await room.ToSignal(room.GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!GodotObject.IsInstanceValid(container) || container.GetChildCount() != optionCount)
            return;

        float scale = optionCount <= FullSizeOptionCount
            ? 1f
            : Math.Max(MinimumScale, (float)FullSizeOptionCount / optionCount);

        // Scale around the centre so the row stays centred instead of drifting
        // toward one side as options are added or consumed.
        container.PivotOffset = container.Size / 2f;
        container.Scale = Vector2.One * scale;
    }
}

using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using BusyCampfire.BusyCampfireCode.Config;
using BusyCampfire.BusyCampfireCode.Runtime;

namespace BusyCampfire.BusyCampfireCode.Ui;

internal static class GlobalStatusHud
{
    private const string LayerName = "BusyCampfireStatusHud";
    private static Label? _label;

    public static void Attach(NGame game)
    {
        if (game.HasNode(LayerName))
            return;

        CanvasLayer layer = new()
        {
            Name = LayerName,
            Layer = 100
        };

        _label = new Label
        {
            Name = "Status",
            AnchorLeft = 1f,
            AnchorRight = 1f,
            OffsetLeft = -380f,
            OffsetRight = -24f,
            OffsetTop = 18f,
            OffsetBottom = 56f,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _label.AddThemeFontSizeOverride("font_size", 22);
        _label.AddThemeColorOverride("font_color", new Color(0.92f, 0.95f, 1f));
        _label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.95f));
        _label.AddThemeConstantOverride("shadow_offset_x", 2);
        _label.AddThemeConstantOverride("shadow_offset_y", 2);

        Godot.Timer timer = new()
        {
            Name = "RefreshTimer",
            WaitTime = 0.5,
            Autostart = true
        };
        timer.Timeout += Refresh;

        layer.AddChild(_label);
        layer.AddChild(timer);
        game.AddChild(layer);
        Refresh();
    }

    public static void Refresh()
    {
        if (_label == null || !GodotObject.IsInstanceValid(_label))
            return;

        bool showClock = SpireConfig.EnableMod && SpireConfig.Clock;
        bool showMode = SpireConfig.EnableMod && SpireConfig.ShowRuntimeMode;
        _label.Visible = showClock || showMode;
        if (!_label.Visible)
            return;

        string text = string.Empty;
        if (showClock)
            text = DateTime.Now.ToString(SpireConfig.Clock24Hour ? "HH:mm:ss" : "hh:mm:ss tt");

        if (showMode && MainFile.IsInitialized)
        {
            string modeText = MainFile.RuntimeMode.Current switch
            {
                RuntimeMode.SinglePlayerFull => "单人完整",
                RuntimeMode.ModdedMultiplayerFull => "联机完整",
                RuntimeMode.VanillaCompatibleMultiplayer => "联机兼容",
                _ => string.Empty
            };
            text = AppendPart(text, modeText);
        }

        if (SpireConfig.PotionChance && RunManager.Instance.IsInProgress)
        {
            var runState = RunManager.Instance.DebugOnlyGetState();
            var player = runState == null ? null : LocalContext.GetMe(runState);
            if (player != null)
            {
                int potionPercent = Mathf.RoundToInt(
                    Mathf.Clamp(player.PlayerOdds.PotionReward.CurrentValue, 0f, 1f) * 100f);
                text = AppendPart(text, $"药水 {potionPercent}%");
            }
        }

        if (_label.Text != text)
            _label.Text = text;
        _label.AddThemeColorOverride(
            "font_color",
            MainFile.IsInitialized &&
            MainFile.RuntimeMode.Current == RuntimeMode.VanillaCompatibleMultiplayer
                ? new Color(0.55f, 1f, 0.68f)
                : new Color(0.92f, 0.95f, 1f));
    }

    private static string AppendPart(string current, string part) =>
        string.IsNullOrEmpty(current) ? part : $"{current}  ·  {part}";
}

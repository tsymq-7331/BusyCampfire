using BaseLib.Config;

namespace BusyCampfire.BusyCampfireCode.Config;

/// <summary>
/// All options are persisted by BaseLib and exposed in Settings -> Mods.
/// Gameplay-changing options are additionally gated by RuntimeModeCoordinator.
/// </summary>
internal sealed class BusyCampfireConfig : SimpleModConfig
{
    public const double MultiplayerTinyTentShopWeightMultiplier = 3.0;

    [ConfigSection("General")]
    public static bool EnableMod { get; set; } = true;

    [ConfigSection("BusyCampfire")]
    [ConfigSlider(1, 10, 0.5)]
    public static double TinyTentShopWeightMultiplier { get; set; } = 3.0;
    public static bool EnableDetailedTestLogs { get; set; } = true;

    [ConfigSection("Diagnostics")]
    public static bool LogModeChanges { get; set; } = true;
}

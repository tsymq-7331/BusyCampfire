using BaseLib.Config;

namespace BusyCampfire.BusyCampfireCode.Config;

/// <summary>
/// All options are persisted by BaseLib and exposed in Settings -> Mods.
/// Gameplay-changing options are additionally gated by RuntimeModeCoordinator.
/// </summary>
internal sealed class BusyCampfireConfig : SimpleModConfig
{
    [ConfigSection("General")]
    public static bool EnableMod { get; set; } = true;

    [ConfigSection("BusyCampfire")]
    [ConfigSlider(1, 4, 1)]
    public static int CloneUsesPerCampfire { get; set; } = 2;
    [ConfigSlider(1, 10, 0.5)]
    public static double TinyTentShopWeightMultiplier { get; set; } = 3.0;
    public static bool EnableDigEvents { get; set; } = true;
    public static bool EnableDigEventsInMultiplayer { get; set; } = true;
    public static bool EnableDetailedTestLogs { get; set; } = true;

    [ConfigSection("Diagnostics")]
    public static bool LogModeChanges { get; set; } = true;
}

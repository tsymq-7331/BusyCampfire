using BaseLib.Config;

namespace BusyCampfire.BusyCampfireCode.Config;

/// <summary>
/// All options are persisted by BaseLib and exposed in Settings -> Mods.
/// Gameplay-changing options are additionally gated by RuntimeModeCoordinator.
/// </summary>
internal sealed class SpireConfig : SimpleModConfig
{
    [ConfigSection("General")]
    public static bool EnableMod { get; set; } = true;
    public static bool PreferVanillaCompatibleMultiplayer { get; set; } = true;
    public static bool EnableSynchronizedGameplayInMultiplayer { get; set; }
    public static bool ShowRuntimeMode { get; set; } = true;

    [ConfigSection("CombatInfo")]
    public static bool IncomingDamage { get; set; } = true;
    public static bool MultiHitTotals { get; set; } = true;
    public static bool CardPlayCount { get; set; } = true;
    public static bool PotionChance { get; set; } = true;

    [ConfigSection("QualityOfLife")]
    public static bool SkipSplash { get; set; } = true;
    public static bool AutoConfirmSelections { get; set; } = true;
    public static bool QuickRestart { get; set; } = true;
    public static bool Clock { get; set; } = true;
    public static bool Clock24Hour { get; set; } = true;
    public static AnimationSpeedOverride AnimationSpeed { get; set; } =
        AnimationSpeedOverride.Fast;
    public static bool AllowInstantSpeedWithVanillaPeers { get; set; }

    [ConfigSection("BusyCampfire")]
    [ConfigSlider(1, 4, 1)]
    public static int CloneUsesPerCampfire { get; set; } = 2;
    [ConfigSlider(1, 10, 0.5)]
    public static double TinyTentShopWeightMultiplier { get; set; } = 3.0;
    public static bool EnableDigEvents { get; set; } = true;
    public static bool EnableDigEventsInMultiplayer { get; set; } = true;
    public static bool EnableDetailedTestLogs { get; set; } = true;

    [ConfigSection("VisualEffects")]
    public static bool EnhancedEffects { get; set; } = true;
    [ConfigSlider(0, 2, 0.1)]
    public static double EffectIntensity { get; set; } = 1.0;
    [ConfigSlider(0, 2, 0.1)]
    public static double ScreenShakeIntensity { get; set; } = 1.0;

    [ConfigSection("Safety")]
    public static bool DisableFaultedModule { get; set; } = true;
    public static bool LogModeChanges { get; set; } = true;
}

using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Saves;
using NewSpireMod.NewSpireModCode.Config;

namespace NewSpireMod.NewSpireModCode.Patches;

[HarmonyPatch]
internal static class QualityOfLifePatches
{
    /// <summary>
    /// Reuses the game's built-in skip flag without permanently rewriting the
    /// player's vanilla settings file.
    /// </summary>
    [HarmonyPatch(typeof(SettingsSave), nameof(SettingsSave.SkipIntroLogo), MethodType.Getter)]
    [HarmonyPostfix]
    private static void ApplySkipIntroPreference(ref bool __result)
    {
        if (SpireConfig.EnableMod && SpireConfig.SkipSplash)
            __result = true;
    }

    /// <summary>
    /// Auto-confirms only after MaxSelect is reached. Choices with a range
    /// (for example "select up to N") still retain their manual confirm button,
    /// allowing the player to submit fewer cards.
    /// </summary>
    [HarmonyPatch(typeof(NSimpleCardSelectScreen), "OnCardClicked")]
    [HarmonyPostfix]
    private static void AutoConfirmSimpleSelection(
        NSimpleCardSelectScreen __instance,
        HashSet<CardModel> ____selectedCards,
        CardSelectorPrefs ____prefs)
    {
        if (ShouldComplete(
                ____selectedCards.Count,
                ____prefs.MaxSelect,
                ____prefs.RequireManualConfirmation))
            AccessTools.Method(typeof(NSimpleCardSelectScreen), "CompleteSelection")
                .Invoke(__instance, null);
    }

    [HarmonyPatch(typeof(NCombatPileCardSelectScreen), "OnCardClicked")]
    [HarmonyPostfix]
    private static void AutoConfirmCombatPileSelection(
        NCombatPileCardSelectScreen __instance,
        HashSet<CardModel> ____selectedCards,
        CardSelectorPrefs ____prefs)
    {
        if (ShouldComplete(
                ____selectedCards.Count,
                ____prefs.MaxSelect,
                ____prefs.RequireManualConfirmation))
            AccessTools.Method(typeof(NCombatPileCardSelectScreen), "CompleteSelection")
                .Invoke(__instance, null);
    }

    private static bool ShouldComplete(int selected, int maximum, bool normallyManual) =>
        SpireConfig.EnableMod &&
        SpireConfig.AutoConfirmSelections &&
        normallyManual &&
        maximum > 0 &&
        selected >= maximum;
}

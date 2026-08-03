using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using BusyCampfire.BusyCampfireCode.Config;
using BusyCampfire.BusyCampfireCode.Runtime;

namespace BusyCampfire.BusyCampfireCode.Patches;

/// <summary>
/// Reorders only the shop-rarity relic deque with a weighted, deterministic
/// shuffle. Pulling from the back then gives Miniature Tent the configured
/// relative weight without duplicating the relic or allowing extra copies.
/// </summary>
internal static class BusyCampfireShopWeightPatches
{
    private static bool Enabled => MainFile.IsInitialized && MainFile.RuntimeMode.GameplayChangesAllowed;

    [HarmonyPatch(typeof(RelicGrabBag), nameof(RelicGrabBag.Populate), [typeof(Player), typeof(Rng)])]
    private static class RelicGrabBagPopulatePatch
    {
        private static void Postfix(RelicGrabBag __instance, Rng rng)
        {
            if (!Enabled)
                return;

            // Local config files are not synchronized between peers. Multiplayer
            // therefore always uses one fixed value so every peer consumes RNG in
            // exactly the same way; the slider is intentionally single-player only.
            double configuredWeight = MainFile.RuntimeMode.Current == RuntimeMode.SinglePlayerFull
                ? BusyCampfireConfig.TinyTentShopWeightMultiplier
                : BusyCampfireConfig.MultiplayerTinyTentShopWeightMultiplier;
            double tentWeight = Math.Max(1d, configuredWeight);
            if (tentWeight <= 1d)
                return;

            var deques = Traverse.Create(__instance)
                .Field("_deques")
                .GetValue<Dictionary<RelicRarity, List<RelicModel>>>();

            if (!deques.TryGetValue(RelicRarity.Shop, out List<RelicModel>? shopRelics) ||
                shopRelics.Count < 2)
                return;

            List<RelicModel> source = [.. shopRelics];
            shopRelics.Clear();

            // The game pulls shop relics from the back, so build the weighted
            // order from front to back and put each weighted pick at the end.
            while (source.Count > 0)
            {
                double totalWeight = source.Sum(RelicWeight);
                double roll = rng.NextFloat() * totalWeight;
                int selectedIndex = 0;

                for (int index = 0; index < source.Count; index++)
                {
                    roll -= RelicWeight(source[index]);
                    if (roll <= 0d)
                    {
                        selectedIndex = index;
                        break;
                    }
                }

                shopRelics.Insert(0, source[selectedIndex]);
                source.RemoveAt(selectedIndex);
            }

            double RelicWeight(RelicModel relic) =>
                relic is MiniatureTent ? tentWeight : 1d;
        }
    }
}

/// <summary>
/// Stores Pumpkin Candle's bonus in the existing saved KindleCount integer.
/// Values below 1000 are vanilla-compatible. The quotient is the accumulated
/// energy bonus and the remainder is the visible number of combats remaining.
/// </summary>
internal static class BusyCampfirePumpkinCandlePatches
{
    private const int BonusEncodingFactor = 1000;
    private static bool Enabled => MainFile.IsInitialized && MainFile.RuntimeMode.GameplayChangesAllowed;

    private static int RemainingUses(PumpkinCandle relic) =>
        Math.Max(0, relic.KindleCount % BonusEncodingFactor);

    private static int EnergyBonus(PumpkinCandle relic) =>
        Math.Max(0, relic.KindleCount / BonusEncodingFactor);

    [HarmonyPatch(typeof(PumpkinCandle), nameof(PumpkinCandle.Rekindle))]
    private static class RekindlePatch
    {
        private static bool Prefix(PumpkinCandle __instance)
        {
            if (!Enabled)
                return true;

            int remaining = RemainingUses(__instance);
            int bonus = EnergyBonus(__instance);

            if (remaining <= 0)
                __instance.KindleCount = PumpkinCandle.kindleAmount;
            else
                __instance.KindleCount =
                    (bonus + 1) * BonusEncodingFactor + remaining + PumpkinCandle.kindleAmount;

            __instance.Flash();
            return false;
        }
    }

    [HarmonyPatch(typeof(PumpkinCandle), nameof(PumpkinCandle.ModifyMaxEnergy))]
    private static class ModifyMaxEnergyPatch
    {
        private static bool Prefix(PumpkinCandle __instance, Player player, decimal amount, ref decimal __result)
        {
            if (!Enabled)
                return true;

            if (player != __instance.Owner || RemainingUses(__instance) <= 0)
            {
                __result = amount;
                return false;
            }

            __result = amount + __instance.DynamicVars.Energy.IntValue + EnergyBonus(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(PumpkinCandle), nameof(PumpkinCandle.AfterCombatEnd))]
    private static class AfterCombatEndPatch
    {
        private static bool Prefix(PumpkinCandle __instance, ref Task __result)
        {
            if (!Enabled)
                return true;

            int remaining = RemainingUses(__instance) - 1;
            __instance.KindleCount = remaining <= 0
                ? 0
                : EnergyBonus(__instance) * BonusEncodingFactor + remaining;
            __result = Task.CompletedTask;
            return false;
        }
    }

    [HarmonyPatch(typeof(PumpkinCandle), "get_DisplayAmount")]
    private static class DisplayAmountPatch
    {
        private static void Postfix(PumpkinCandle __instance, ref int __result)
        {
            if (Enabled)
                __result = RemainingUses(__instance);
        }
    }
}

using BusyCampfire.BusyCampfireCode.Relics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using BusyCampfire.BusyCampfireCode.Diagnostics;

namespace BusyCampfire.BusyCampfireCode.Patches;

internal static class ForgingHammerPatches
{
    private static bool Enabled => MainFile.IsInitialized && MainFile.RuntimeMode.GameplayChangesAllowed;

    [HarmonyPatch(typeof(SmithRestSiteOption), nameof(SmithRestSiteOption.OnSelect))]
    private static class SmithOnSelectPatch
    {
        private static void Postfix(SmithRestSiteOption __instance, ref Task<bool> __result)
        {
            if (Enabled)
                __result = CompleteAndEnchant(__instance, __result);
        }
    }

    private static async Task<bool> CompleteAndEnchant(SmithRestSiteOption option, Task<bool> originalTask)
    {
        bool succeeded = await originalTask;
        if (!succeeded)
            return false;

        Player owner = Traverse.Create(option).Property("Owner").GetValue<Player>();
        ForgingHammer? hammer = owner.GetRelic<ForgingHammer>();
        if (hammer == null)
            return true;

        IEnumerable<CardModel>? selection = Traverse.Create(option)
            .Field("_selection")
            .GetValue<IEnumerable<CardModel>>();

        foreach (CardModel card in selection ?? [])
        {
            if (card.Enchantment != null)
                continue;

            List<VanillaEnchantmentCandidate> candidates = CreateVanillaCandidates()
                .Where(candidate => candidate.Enchantment.CanEnchant(card))
                .ToList();
            if (candidates.Count == 0)
                continue;

            VanillaEnchantmentCandidate candidate =
                candidates[owner.RunState.Rng.Niche.NextInt(candidates.Count)];
            EnchantmentModel chosen = candidate.Enchantment;
            int amount = candidate.VanillaAmounts[
                owner.RunState.Rng.Niche.NextInt(candidate.VanillaAmounts.Length)];
            CardCmd.Enchant(chosen, card, amount);
            hammer.Flash();
            CampfireTestLog.Write(owner, "ForgingHammer/锻造锤", $"Card={card.Id.Entry}, Enchantment={chosen.Id.Entry}, Amount={amount}");
        }

        return true;
    }

    private static IEnumerable<VanillaEnchantmentCandidate> CreateVanillaCandidates()
    {
        yield return Candidate<Adroit>(3);
        yield return Candidate<Glam>(1);
        yield return Candidate<Imbued>(1);
        yield return Candidate<Instinct>(1);
        yield return Candidate<Momentum>(5);
        yield return Candidate<Nimble>(2);
        yield return Candidate<PerfectFit>(1);
        yield return Candidate<RoyallyApproved>(1);
        yield return Candidate<Sharp>(2, 3);
        yield return Candidate<Slither>(1);
        yield return Candidate<Spiral>(1);
        yield return Candidate<Steady>(1);
        yield return Candidate<Swift>(1, 2);
        yield return Candidate<Vigorous>(8);
    }

    private static VanillaEnchantmentCandidate Candidate<T>(params int[] vanillaAmounts)
        where T : EnchantmentModel =>
        new(ModelDb.Enchantment<T>().ToMutable(), vanillaAmounts);

    private sealed record VanillaEnchantmentCandidate(
        EnchantmentModel Enchantment,
        int[] VanillaAmounts);
}

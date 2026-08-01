using BusyCampfire.BusyCampfireCode.Compatibility;
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
            bool alreadyEnchanted = card.Enchantment != null;
            if (alreadyEnchanted && !MultiEnchantmentCompatibility.IsAvailable)
                continue;

            HashSet<Type> attachedTypes = MultiEnchantmentCompatibility.GetAttachedEnchantmentTypes(card);
            List<EnchantmentModel> candidates = CreateVanillaCandidates()
                .Where(enchantment => !attachedTypes.Contains(enchantment.GetType()))
                .Where(enchantment => enchantment.CanEnchant(card))
                .ToList();
            if (candidates.Count == 0)
                continue;

            EnchantmentModel chosen = candidates[owner.RunState.Rng.Niche.NextInt(candidates.Count)];
            bool applied = alreadyEnchanted
                ? MultiEnchantmentCompatibility.TryEnchant(card, chosen)
                : ApplyVanillaEnchantment(card, chosen);
            if (applied)
            {
                hammer.Flash();
                CampfireTestLog.Write(owner, "ForgingHammer/锻造锤", $"Card={card.Id.Entry}, Enchantment={chosen.Id.Entry}, Multi={alreadyEnchanted}");
            }
        }

        return true;
    }

    private static bool ApplyVanillaEnchantment(CardModel card, EnchantmentModel enchantment)
    {
        CardCmd.Enchant(enchantment, card, 1);
        return true;
    }

    private static IEnumerable<EnchantmentModel> CreateVanillaCandidates()
    {
        yield return ModelDb.Enchantment<Adroit>().ToMutable();
        yield return ModelDb.Enchantment<Glam>().ToMutable();
        yield return ModelDb.Enchantment<Imbued>().ToMutable();
        yield return ModelDb.Enchantment<Instinct>().ToMutable();
        yield return ModelDb.Enchantment<Momentum>().ToMutable();
        yield return ModelDb.Enchantment<Nimble>().ToMutable();
        yield return ModelDb.Enchantment<PerfectFit>().ToMutable();
        yield return ModelDb.Enchantment<RoyallyApproved>().ToMutable();
        yield return ModelDb.Enchantment<Sharp>().ToMutable();
        yield return ModelDb.Enchantment<Slither>().ToMutable();
        yield return ModelDb.Enchantment<Spiral>().ToMutable();
        yield return ModelDb.Enchantment<Steady>().ToMutable();
        yield return ModelDb.Enchantment<Swift>().ToMutable();
        yield return ModelDb.Enchantment<Vigorous>().ToMutable();
    }
}

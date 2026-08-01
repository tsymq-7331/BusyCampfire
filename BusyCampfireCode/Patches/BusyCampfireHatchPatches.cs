using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using BusyCampfire.BusyCampfireCode.Diagnostics;

namespace BusyCampfire.BusyCampfireCode.Patches;

internal static class BusyCampfireHatchPatches
{
    private static readonly SavedSpireField<Player, int> HatchMultiplier =
        new(_ => 0, "BusyCampfireHatchMultiplier");

    private static readonly Dictionary<Player, CampfireUseState> HatchUses = [];
    private static bool _saveFieldsRegistered;

    private static bool Enabled => MainFile.IsInitialized && MainFile.RuntimeMode.GameplayChangesAllowed;

    internal static void RegisterSaveFields()
    {
        if (_saveFieldsRegistered)
            return;

        HatchMultiplier.RegisterCustomSave();
        _saveFieldsRegistered = true;
    }

    [HarmonyPatch(typeof(CardPile), nameof(CardPile.AddInternal))]
    private static class EggAddedPatch
    {
        private static void Postfix(CardPile __instance, CardModel card)
        {
            if (!Enabled || __instance.Type != PileType.Deck || card is not ByrdonisEgg || card.Owner == null)
                return;

            HatchMultiplier[card.Owner] = CountEggs(card.Owner);
        }
    }

    [HarmonyPatch(typeof(CardPile), nameof(CardPile.RemoveInternal))]
    private static class EggRemovedPatch
    {
        private static void Prefix(CardPile __instance, CardModel card)
        {
            if (!Enabled || __instance.Type != PileType.Deck || card is not ByrdonisEgg || card.Owner == null)
                return;

            int countBeforeRemoval = CountEggs(card.Owner);
            HatchMultiplier[card.Owner] = Math.Max(1, countBeforeRemoval - 1);
        }
    }

    [HarmonyPatch(typeof(RestSiteOption), nameof(RestSiteOption.Generate))]
    private static class HatchOptionGenerationPatch
    {
        private static void Postfix(Player player, ref List<RestSiteOption> __result)
        {
            if (!Enabled)
                return;

            int liveEggCount = CountEggs(player);
            if (liveEggCount > 0)
                HatchMultiplier[player] = liveEggCount;

            bool unlocked = HatchMultiplier[player] > 0;
            bool found = false;
            for (int index = 0; index < __result.Count; index++)
            {
                if (__result[index] is not HatchRestSiteOption)
                    continue;

                found = true;
                __result[index] = new PersistentHatchRestSiteOption(player);
            }

            if (unlocked && !found)
                __result.Add(new PersistentHatchRestSiteOption(player));
        }
    }

    private sealed class PersistentHatchRestSiteOption(Player owner) : HatchRestSiteOption(owner)
    {
        public override bool IsEnabled => !WasUsedHere(Owner);

        public override async Task<bool> OnSelect()
        {
            if (!IsEnabled)
                return false;

            int multiplier = Math.Max(HatchMultiplier[Owner], CountEggs(Owner));
            if (multiplier <= 0)
                return false;

            List<CardModel> eggs = PileType.Deck.GetPile(Owner).Cards
                .Where(card => card is ByrdonisEgg)
                .ToList();

            if (Owner.GetRelic<Byrdpip>() == null)
            {
                await base.OnSelect();
            }
            else
            {
                foreach (CardModel egg in eggs)
                    await CardCmd.TransformTo<ByrdSwoop>(egg);
            }

            int missingCards = Math.Max(0, multiplier - eggs.Count);
            List<CardPileAddResult> addedCards = [];
            for (int index = 0; index < missingCards; index++)
            {
                CardModel card = Owner.RunState.CreateCard<ByrdSwoop>(Owner);
                CardPileAddResult result = await CardPileCmd.Add(card, PileType.Deck);
                if (result.success)
                    addedCards.Add(result);
            }

            if (addedCards.Count > 0)
                CardCmd.PreviewCardPileAdd(addedCards, 2f);

            HatchMultiplier[Owner] = multiplier;
            MarkUsedHere(Owner);
            CampfireTestLog.Write(Owner, "Hatch/孵化", $"Multiplier={multiplier}, LiveEggs={eggs.Count}, AddedCards={addedCards.Count}");
            return true;
        }
    }

    private static int CountEggs(Player player) =>
        PileType.Deck.GetPile(player).Cards.Count(card => card is ByrdonisEgg);

    private static bool WasUsedHere(Player player)
    {
        CampfireUseState state = GetState(player);
        return state.Used;
    }

    private static void MarkUsedHere(Player player)
    {
        CampfireUseState state = GetState(player);
        HatchUses[player] = state with { Used = true };
    }

    private static CampfireUseState GetState(Player player)
    {
        CampfireUseState current = new(
            player.RunState.CurrentActIndex,
            player.RunState.TotalFloor,
            Used: false);

        if (!HatchUses.TryGetValue(player, out CampfireUseState saved) ||
            saved.ActIndex != current.ActIndex ||
            saved.TotalFloor != current.TotalFloor)
        {
            HatchUses[player] = current;
            return current;
        }

        return saved;
    }

    private readonly record struct CampfireUseState(int ActIndex, int TotalFloor, bool Used);
}

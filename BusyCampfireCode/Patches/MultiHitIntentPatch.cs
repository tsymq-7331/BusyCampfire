using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.addons.mega_text;
using BusyCampfire.BusyCampfireCode.Config;

namespace BusyCampfire.BusyCampfireCode.Patches;

/// <summary>
/// Appends the total to the game's existing "damage × hits" intent label.
/// This only changes the local UI and is safe with unmodded multiplayer peers.
/// </summary>
[HarmonyPatch(typeof(NIntent), "UpdateVisuals")]
internal static class MultiHitIntentPatch
{
    [HarmonyPostfix]
    private static void AppendTotal(
        AbstractIntent ____intent,
        IEnumerable<Creature> ____targets,
        Creature ____owner,
        MegaRichTextLabel ____valueLabel)
    {
        if (!SpireConfig.EnableMod || !SpireConfig.MultiHitTotals)
            return;

        if (____intent is not MultiAttackIntent multiAttack || multiAttack.Repeats <= 1)
            return;

        int total = multiAttack.GetTotalDamage(____targets, ____owner);
        ____valueLabel.Text = $"{____valueLabel.Text} ({total})";
    }
}

using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace NewSpireMod.NewSpireModCode.Combat;

internal readonly record struct IntentAttack(
    AttackIntent Intent,
    Creature Owner,
    IReadOnlyCollection<Creature> Targets);

internal readonly record struct IncomingDamageForecast(
    int RawDamage,
    int CurrentBlock,
    int HpDamage,
    int ExpectedHp);

/// <summary>
/// Conservative first-pass forecast. It uses the game's own intent damage
/// calculation so Weak, Strength and other standard hooks stay consistent.
/// End-of-turn powers, pets, orbs and delayed effects are added as separate
/// contributors later instead of being guessed here.
/// </summary>
internal static class IncomingDamageCalculator
{
    public static IncomingDamageForecast Calculate(
        Creature player,
        IEnumerable<IntentAttack> attacks)
    {
        int rawDamage = 0;

        foreach (IntentAttack attack in attacks)
        {
            if (!attack.Targets.Contains(player))
                continue;

            rawDamage = checked(rawDamage +
                attack.Intent.GetTotalDamage(attack.Targets, attack.Owner));
        }

        int hpDamage = Math.Max(0, rawDamage - player.Block);
        int expectedHp = Math.Max(0, player.CurrentHp - hpDamage);
        return new IncomingDamageForecast(rawDamage, player.Block, hpDamage, expectedHp);
    }
}

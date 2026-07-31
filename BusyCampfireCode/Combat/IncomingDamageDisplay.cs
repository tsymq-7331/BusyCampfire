using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using BusyCampfire.BusyCampfireCode.Config;

namespace BusyCampfire.BusyCampfireCode.Combat;

internal static class IncomingDamageDisplay
{
    private const string LabelName = "BusyCampfireIncomingDamage";
    private static bool _refreshQueued;

    public static void AttachToLocalPlayer(NCreature creatureNode)
    {
        if (!SpireConfig.EnableMod || !SpireConfig.IncomingDamage)
            return;

        Creature entity = creatureNode.Entity;
        if (entity.CombatState == null ||
            LocalContext.GetMe(entity.CombatState)?.Creature != entity ||
            creatureNode.HasNode(LabelName))
            return;

        Label label = new()
        {
            Name = LabelName,
            Position = new Vector2(-120f, -255f),
            Size = new Vector2(240f, 42f),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 50
        };
        label.AddThemeFontSizeOverride("font_size", 26);
        label.AddThemeColorOverride("font_color", new Color(1f, 0.82f, 0.28f));
        label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.9f));
        label.AddThemeConstantOverride("shadow_offset_x", 2);
        label.AddThemeConstantOverride("shadow_offset_y", 2);
        creatureNode.AddChild(label);
        QueueRefresh();
    }

    public static void QueueRefresh()
    {
        if (_refreshQueued)
            return;

        _refreshQueued = true;
        Callable.From(RefreshNow).CallDeferred();
    }

    private static void RefreshNow()
    {
        _refreshQueued = false;
        if (!SpireConfig.EnableMod || !SpireConfig.IncomingDamage)
            return;

        NCombatRoom? room = NCombatRoom.Instance;
        if (room == null)
            return;

        NCreature? localNode = room.CreatureNodes.FirstOrDefault(node =>
            node.Entity.CombatState != null &&
            LocalContext.GetMe(node.Entity.CombatState)?.Creature == node.Entity);
        Label? label = localNode?.GetNodeOrNull<Label>(LabelName);
        Creature? player = localNode?.Entity;
        if (label == null || player?.CombatState == null)
            return;

        IReadOnlyCollection<Creature> targets =
            player.CombatState.Players.Select(p => p.Creature).ToArray();
        List<IntentAttack> attacks = [];

        foreach (NCreature enemyNode in room.CreatureNodes)
        {
            Creature enemy = enemyNode.Entity;
            if (enemy.Monster == null || enemy.CurrentHp <= 0)
                continue;

            foreach (AttackIntent attack in enemy.Monster.NextMove.Intents.OfType<AttackIntent>())
                attacks.Add(new IntentAttack(attack, enemy, targets));
        }

        IncomingDamageForecast forecast =
            IncomingDamageCalculator.Calculate(player, attacks);
        string text = $"预计伤害 {forecast.HpDamage}  ·  预计生命 {forecast.ExpectedHp}";
        if (label.Text != text)
            label.Text = text;
        label.Modulate = forecast.HpDamage > 0
            ? Colors.White
            : new Color(0.65f, 0.85f, 0.65f, 0.9f);
    }
}

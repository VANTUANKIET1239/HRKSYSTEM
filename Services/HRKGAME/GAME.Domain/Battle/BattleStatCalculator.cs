namespace GAME.Domain.Battle;

public static class BattleStatCalculator
{
    public static int MitigateDamage(decimal rawDamage, decimal defense, decimal constant)
    {
        if (constant <= 0m) throw new ArgumentOutOfRangeException(nameof(constant));
        return Math.Max(1, (int)Math.Round(rawDamage * constant / (constant + Math.Max(0m, defense))));
    }

    public static decimal GetEffectiveStat(BattleCombatant hero, string code)
    {
        var baseValue = code.ToUpperInvariant() switch
        {
            "HP" => hero.MaxHp,
            "ATK" => hero.Atk,
            "DEF" => hero.Def,
            "SPD" => hero.Spd,
            "MAGIC_DAMAGE" => hero.MagicDamage,
            "MAGIC_RESISTANCE" => hero.MagicResistance,
            _ => 0
        };

        decimal flat = 0;
        decimal percent = 0;
        foreach (var status in hero.StatusEffects)
        foreach (var modifier in status.StatModifiers.Where(x =>
                     x.AttributeCode.Equals(code, StringComparison.OrdinalIgnoreCase)))
        {
            var multiplier = status.ScaleModifiersWithStacks ? status.Stacks : 1;
            if (modifier.ValueType.Equals("FLAT", StringComparison.OrdinalIgnoreCase))
                flat += modifier.Value * multiplier;
            else
                percent += modifier.Value * multiplier;
        }

        return Math.Max(0, baseValue + flat + baseValue * percent / 100m);
    }

    public static decimal CalculateEffectValue(BattleSkillEffect effect, BattleCombatant actor) =>
        effect.BaseValue + effect.Scalings.Sum(x =>
            GetEffectiveStat(actor, x.AttributeCode) * x.Coefficient + x.FlatValue);
}

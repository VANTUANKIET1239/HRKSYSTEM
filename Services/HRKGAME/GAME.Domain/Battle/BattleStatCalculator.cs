namespace GAME.Domain.Battle;

public static class BattleStatCalculator
{
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
            if (modifier.ValueType.Equals("FLAT", StringComparison.OrdinalIgnoreCase))
                flat += modifier.Value * status.Stacks;
            else
                percent += modifier.Value * status.Stacks;
        }

        return Math.Max(0, baseValue + flat + baseValue * percent / 100m);
    }

    public static decimal CalculateEffectValue(BattleSkillEffect effect, BattleCombatant actor) =>
        effect.BaseValue + effect.Scalings.Sum(x =>
            GetEffectiveStat(actor, x.AttributeCode) * x.Coefficient + x.FlatValue);
}

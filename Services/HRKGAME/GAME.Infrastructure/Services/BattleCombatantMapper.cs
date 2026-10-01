using GAME.Application.DTOs;
using GAME.Domain.Battle;

namespace GAME.Infrastructure.Services;

public static class BattleCombatantMapper
{
    public static BattleCombatant Map(PlayerHeroDto hero, int team, int initialEnergy, int maxEnergy)
    {
        var activeSkills = hero.Skills.Where(s => s != null).ToList();
        var basic = activeSkills.SingleOrDefault(s => s.SkillTypeCode == BattleCodes.Normal)
            ?? throw new InvalidOperationException($"Hero {hero.Name} must have exactly one active NORMAL skill.");
        var energySkills = activeSkills.Where(s => s.SkillTypeCode == BattleCodes.Energy).ToList();
        if (energySkills.Count > 1)
            throw new InvalidOperationException($"Hero {hero.Name} can have at most one active ENERGY skill.");

        return new BattleCombatant
        {
            Id = team == 0 ? hero.Id : -hero.Id,
            SourceHeroId = hero.Id,
            Team = team,
            Position = hero.Position ?? 1,
            Name = hero.Name,
            MaxHp = hero.Stats.Hp,
            Hp = hero.Stats.Hp,
            Atk = hero.Stats.Atk,
            Def = hero.Stats.Def,
            Spd = hero.Stats.Spd,
            MagicDamage = hero.Stats.MagicDamage,
            MagicResistance = hero.Stats.MagicResistance,
            CritChance = hero.Stats.Crit,
            CritDamage = hero.Stats.CritDmg,
            Energy = initialEnergy,
            MaxEnergy = maxEnergy,
            BasicSkill = MapSkill(basic),
            EnergySkill = energySkills.Count == 1 ? MapSkill(energySkills[0]) : null
        };
    }

    private static BattleSkill MapSkill(SkillTemplateDto skill) => new()
    {
        Id = skill.Id,
        Name = skill.Name,
        SkillTypeCode = skill.SkillTypeCode,
        EnergyCost = skill.EnergyCost,
        Animation = skill.Animation == null ? BattleSkillAnimation.Default : new BattleSkillAnimation
        {
            AnimationKey = skill.Animation.AnimationKey,
            TotalDurationMs = skill.Animation.TotalDurationMs,
            Phases = skill.Animation.Phases.OrderBy(p => p.DisplayOrder)
                .Select(p => new BattleSkillTimelinePhase(p.PhaseCode, p.StartAtMs, p.DurationMs, p.TriggerEventType))
                .ToList()
        },
        Effects = skill.Effects.Select(e => MapEffect(skill.Id, e)).ToList()
    };

    private static BattleSkillEffect MapEffect(string skillId, SkillEffectDto effect)
    {
        if (string.IsNullOrWhiteSpace(effect.EffectTypeCode))
            throw new InvalidOperationException(
                $"Skill '{skillId}' contains effect {effect.Id} without EffectTypeCode.");
        if (string.IsNullOrWhiteSpace(effect.TargetTypeCode))
            throw new InvalidOperationException(
                $"Skill '{skillId}', effect {effect.Id}, has no TargetTypeCode. Check HRK_SkillEffects.TargetTypeId.");

        return new BattleSkillEffect
        {
            EffectTypeCode = effect.EffectTypeCode,
            TargetTypeCode = NormalizeTargetCode(effect.TargetTypeCode),
            DamageSchoolCode = effect.DamageSchoolCode,
            BaseValue = effect.BaseValue,
            DurationTurns = effect.DurationTurns ?? 0,
            ChancePercent = effect.ChancePercent,
            MaxStacks = effect.MaxStacks ?? 1,
            DisplayOrder = effect.DisplayOrder,
            ExecutionGroup = effect.ExecutionGroup,
            ConditionCode = effect.ConditionCode,
            Scalings = effect.Scalings.Select(s =>
                new BattleEffectScaling(s.AttributeTypeCode, s.Coefficient, s.FlatValue)).ToList(),
            StatModifiers = effect.StatModifiers.Select(m =>
                new BattleStatModifier(m.AttributeTypeCode, m.ValueType, m.Value, m.AttributeTypeName)).ToList(),
            Parameters = effect.Parameters.ToDictionary(
                p => p.ParameterCode,
                p => new BattleSkillEffectParameter(p.ParameterCode, p.DecimalValue, p.IntValue, p.BoolValue, p.StringValue),
                StringComparer.OrdinalIgnoreCase)
        };
    }

    private static string NormalizeTargetCode(string code) => code.ToUpperInvariant() switch
    {
        "SINGLE_ENEMY" => BattleCodes.EnemySingle,
        "ALL_ENEMIES" => BattleCodes.EnemyAll,
        "RANDOM_ENEMY" => BattleCodes.EnemyRandom,
        "SELF" => BattleCodes.Self,
        "ALLY_RANDOM" => BattleCodes.AllyRandom,
        "ALL_ALLIES" => BattleCodes.AllyAll,
        var value => value
    };

}

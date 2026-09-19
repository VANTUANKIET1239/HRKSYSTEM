namespace GAME.Domain.Battle;

public static class BattleCodes
{
    public const string Normal = "NORMAL";
    public const string Energy = "ENERGY";
    public const string Damage = "DAMAGE";
    public const string Heal = "HEAL";
    public const string StatBuff = "STAT_BUFF";
    public const string StatDebuff = "STAT_DEBUFF";
    public const string Physical = "PHYSICAL";
    public const string Magic = "MAGIC";
    public const string True = "TRUE";
    public const string Self = "SELF";
    public const string AllyRandom = "ALLY_RANDOM";
    public const string AllyAll = "ALLY_ALL";
    public const string AllyRandom2 = "ALLY_RANDOM_2";
    public const string EnemySingle = "ENEMY_SINGLE";
    public const string EnemyAll = "ENEMY_ALL";
    public const string EnemyRandom = "ENEMY_RANDOM";
    public const string EnemyRandom4 = "ENEMY_RANDOM_4";
    public const string EnemyFrontRow = "ENEMY_FRONT_ROW";
    public const string EnemyBackRow = "ENEMY_BACK_ROW";
    public const string EnemySameLaneBackRow = "ENEMY_SAME_LANE_BACK_ROW";
}

public sealed class BattleSimulationRequest
{
    public int RandomSeed { get; init; }
    public int MaxRounds { get; init; } = 100;
    public required IReadOnlyList<BattleCombatant> Combatants { get; init; }
}

public sealed class BattleCombatant
{
    public long Id { get; init; }
    public long SourceHeroId { get; init; }
    public int Team { get; init; }
    public int Position { get; set; }
    public required string Name { get; init; }
    public int MaxHp { get; init; }
    public int Hp { get; set; }
    public int Atk { get; init; }
    public int Def { get; init; }
    public int Spd { get; init; }
    public int MagicDamage { get; init; }
    public int MagicResistance { get; init; }
    public decimal CritChance { get; init; }
    public decimal CritDamage { get; init; } = 150m;
    public int Energy { get; set; }
    public int MaxEnergy { get; init; } = 100;
    public required BattleSkill BasicSkill { get; init; }
    public BattleSkill? EnergySkill { get; init; }
    public List<BattleStatusEffect> StatusEffects { get; } = [];
    public bool IsAlive => Hp > 0;
}

public sealed class BattleSkill
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string SkillTypeCode { get; init; }
    public int EnergyCost { get; init; }
    public required IReadOnlyList<BattleSkillEffect> Effects { get; init; }
}

public sealed class BattleSkillEffect
{
    public required string EffectTypeCode { get; init; }
    public required string TargetTypeCode { get; init; }
    public string? DamageSchoolCode { get; init; }
    public decimal BaseValue { get; init; }
    public int DurationTurns { get; init; }
    public decimal ChancePercent { get; init; } = 100m;
    public int MaxStacks { get; init; } = 1;
    public IReadOnlyList<BattleEffectScaling> Scalings { get; init; } = [];
    public IReadOnlyList<BattleStatModifier> StatModifiers { get; init; } = [];
}

public sealed record BattleEffectScaling(string AttributeCode, decimal Coefficient, decimal FlatValue = 0m);
public sealed record BattleStatModifier(string AttributeCode, string ValueType, decimal Value);

public sealed class BattleStatusEffect
{
    public required string InstanceId { get; init; }
    public required string EffectTypeCode { get; init; }
    public required string SourceSkillId { get; init; }
    public long SourceHeroId { get; init; }
    public int RemainingTurns { get; set; }
    public int Stacks { get; set; } = 1;
    public int MaxStacks { get; init; } = 1;
    public required IReadOnlyList<BattleStatModifier> StatModifiers { get; init; }
}

public sealed class BattleEvent
{
    public int Sequence { get; init; }
    public int Round { get; init; }
    public int Turn { get; init; }
    public required string EventType { get; init; }
    public long? ActorId { get; init; }
    public long? TargetId { get; init; }
    public string? SkillId { get; init; }
    public string? EffectTypeCode { get; init; }
    public string? DamageSchoolCode { get; init; }
    public int Value { get; init; }
    public int? HpBefore { get; init; }
    public int? HpAfter { get; init; }
    public int? EnergyBefore { get; init; }
    public int? EnergyAfter { get; init; }
    public bool IsCrit { get; init; }
    public int? RemainingTurns { get; init; }
}

public sealed class BattleSimulationResult
{
    public required string Winner { get; init; }
    public int Rounds { get; init; }
    public required IReadOnlyList<BattleEvent> Events { get; init; }
    public required IReadOnlyList<BattleCombatant> FinalCombatants { get; init; }
}

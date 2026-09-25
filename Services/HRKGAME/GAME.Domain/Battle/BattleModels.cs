namespace GAME.Domain.Battle;

public static class BattleCodes
{
    public const string Normal = "NORMAL";
    public const string Energy = "ENERGY";
    public const string Damage = "DAMAGE";
    public const string Heal = "HEAL";
    public const string StatBuff = "STAT_BUFF";
    public const string StatDebuff = "STAT_DEBUFF";
    public const string Stun = "STUN";
    public const string Shield = "SHIELD";
    public const string Mark = "MARK";
    public const string PositionSwap = "POSITION_SWAP";
    public const string Silence = "SILENCE";
    public const string DamageReduction = "DAMAGE_REDUCTION";
    public const string Taunt = "TAUNT";
    public const string DamageReflection = "DAMAGE_REFLECTION";
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
    public const string LowestHpPercent = "LOWEST_HP_PERCENT";
    public const string Bleed = "BLEED";
    public const string Panic = "PANIC";
    public const string ShieldBlock = "SHIELD_BLOCK";
    public const string BleedDamage = "BLEED_DAMAGE";
    public const string BleedDetonate = "BLEED_DETONATE";
    public const string BleedDetonated = "BLEED_DETONATED";
    public const string EnergyChange = "ENERGY_CHANGE";
    public const string EnergyChanged = "ENERGY_CHANGED";
    public const string ActionBarChanged = "ACTION_BAR_CHANGED";
    public const string StatusRefreshed = "STATUS_REFRESHED";
    public const string StatusStackChanged = "STATUS_STACK_CHANGED";
    public const string StatusRemoved = "STATUS_REMOVED";
    public const string Ricardo = "RICARDO";
    public const string RicardoRageReady = "RICARDO_RAGE_READY";
    public const string RicardoApplied = "RICARDO_APPLIED";
    public const string RicardoConsumed = "RICARDO_CONSUMED";
    public const string ChuanMenBasic = "CHUAN_MEN_BASIC";
    public const string RicardoMilos = "RICARDO_MILOS";
}

public sealed class BattleSimulationRequest
{
    public int RandomSeed { get; init; }
    public int MaxRounds { get; init; } = 100;
    public int BasicAttackEnergyGain { get; init; } = 25;
    public int BasicAttackHitEnergyGain { get; init; } = 25;
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
    public BattleSkillAnimation Animation { get; init; } = BattleSkillAnimation.Default;
    public required IReadOnlyList<BattleSkillEffect> Effects { get; init; }
}

public sealed class BattleSkillAnimation
{
    public static BattleSkillAnimation Default { get; } = new();
    public string AnimationKey { get; init; } = "basic";
    public int TotalDurationMs { get; init; } = 2400;
    public IReadOnlyList<BattleSkillTimelinePhase> Phases { get; init; } =
    [
        new("CAST", 0, 800, "SKILL_CAST"),
        new("IMPACT", 800, 800, "DAMAGE"),
        new("STATUS", 1600, 0, "STATUS_APPLIED"),
        new("RECOVERY", 1600, 800, "SKILL_COMPLETED")
    ];

    public BattleSkillTimelinePhase Phase(string code) =>
        Phases.FirstOrDefault(x => x.PhaseCode.Equals(code, StringComparison.OrdinalIgnoreCase))
        ?? new BattleSkillTimelinePhase(code, 0, 0, null);
}

public sealed record BattleSkillTimelinePhase(
    string PhaseCode, int StartAtMs, int DurationMs, string? TriggerEventType);

public sealed class BattleSkillEffect
{
    public required string EffectTypeCode { get; init; }
    public required string TargetTypeCode { get; init; }
    public string? DamageSchoolCode { get; init; }
    public decimal BaseValue { get; init; }
    public int DurationTurns { get; init; }
    public decimal ChancePercent { get; init; } = 100m;
    public int MaxStacks { get; init; } = 1;
    public int DisplayOrder { get; init; }
    public string? ExecutionGroup { get; init; }
    public string? ConditionCode { get; init; }
    public IReadOnlyList<BattleEffectScaling> Scalings { get; init; } = [];
    public IReadOnlyList<BattleStatModifier> StatModifiers { get; init; } = [];
    public IReadOnlyDictionary<string, BattleSkillEffectParameter> Parameters { get; init; } =
        new Dictionary<string, BattleSkillEffectParameter>(StringComparer.OrdinalIgnoreCase);

    public decimal GetRequiredDecimal(string paramCode, string skillId)
    {
        if (Parameters.TryGetValue(paramCode, out var p) && p.DecimalValue.HasValue)
            return p.DecimalValue.Value;
        throw new InvalidOperationException($"Skill '{skillId}', effect '{EffectTypeCode}' is missing parameter '{paramCode}'.");
    }

    public decimal GetDecimal(string paramCode, decimal fallback)
    {
        return Parameters.TryGetValue(paramCode, out var p) && p.DecimalValue.HasValue
            ? p.DecimalValue.Value
            : fallback;
    }

    public int GetRequiredInt(string paramCode, string skillId)
    {
        if (Parameters.TryGetValue(paramCode, out var p) && p.IntValue.HasValue)
            return p.IntValue.Value;
        throw new InvalidOperationException($"Skill '{skillId}', effect '{EffectTypeCode}' is missing parameter '{paramCode}'.");
    }

    public int GetInt(string paramCode, int fallback)
    {
        return Parameters.TryGetValue(paramCode, out var p) && p.IntValue.HasValue
            ? p.IntValue.Value
            : fallback;
    }

    public bool GetBool(string paramCode, bool fallback)
    {
        return Parameters.TryGetValue(paramCode, out var p) && p.BoolValue.HasValue
            ? p.BoolValue.Value
            : fallback;
    }

    public string? GetString(string paramCode, string? fallback = null)
    {
        return Parameters.TryGetValue(paramCode, out var p) && !string.IsNullOrEmpty(p.StringValue)
            ? p.StringValue
            : fallback;
    }
}

public sealed record BattleSkillEffectParameter(
    string ParameterCode,
    decimal? DecimalValue,
    int? IntValue,
    bool? BoolValue,
    string? StringValue);

public sealed record BattleEffectScaling(string AttributeCode, decimal Coefficient, decimal FlatValue = 0m);
public sealed record BattleStatModifier(string AttributeCode, string ValueType, decimal Value, string? AttributeName = null);

public sealed class BattleStatusEffect
{
    public required string InstanceId { get; init; }
    public required string EffectTypeCode { get; init; }
    public required string SourceSkillId { get; init; }
    public long SourceHeroId { get; init; }
    public int RemainingTurns { get; set; }
    public int Stacks { get; set; } = 1;
    public int MaxStacks { get; init; } = 1;
    public decimal Value { get; set; }
    public int ShieldRemaining { get; set; }
    public int AppliedTurn { get; set; }
    public string? DamageSchoolCode { get; init; }
    public decimal ArmorIgnorePercent { get; init; }
    public bool CanCrit { get; init; }
    public bool CanKill { get; init; }
    public bool ConsumeOnHit { get; set; }
    public string? LastProcessedActionId { get; set; }
    public decimal DamageBonusPerStackPercent { get; set; }
    public bool ScaleModifiersWithStacks { get; set; } = true;
    public bool IsPermanent => RemainingTurns == -1;
    public bool IsActive => RemainingTurns > 0 || RemainingTurns == -1;
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
    public int? PreviousStacks { get; init; }
    public int? CurrentStacks { get; init; }
    public int? MaxStacks { get; init; }
    public int? CastSequence { get; init; }
    public int TimelineOffsetMs { get; init; }
    public string? PhaseCode { get; init; }
    public string? ExecutionGroup { get; init; }
    public int? HitIndex { get; init; }
    public IReadOnlyList<BattleStatModifier> StatModifiers { get; init; } = [];
}

public sealed class BattleSimulationResult
{
    public required string Winner { get; init; }
    public int Rounds { get; init; }
    public required IReadOnlyList<BattleEvent> Events { get; init; }
    public required IReadOnlyList<BattleCombatant> FinalCombatants { get; init; }
}

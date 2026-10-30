namespace GAME.Domain.Battle;

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

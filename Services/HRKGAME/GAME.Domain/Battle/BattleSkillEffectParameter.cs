namespace GAME.Domain.Battle;

public sealed record BattleSkillEffectParameter(
    string ParameterCode,
    decimal? DecimalValue,
    int? IntValue,
    bool? BoolValue,
    string? StringValue);

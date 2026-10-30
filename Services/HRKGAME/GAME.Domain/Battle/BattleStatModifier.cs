namespace GAME.Domain.Battle;
public sealed record BattleStatModifier(string AttributeCode, string ValueType, decimal Value, string? AttributeName = null);

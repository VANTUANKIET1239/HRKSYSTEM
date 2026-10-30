namespace GAME.Domain.Battle;

public sealed record BattleEffectScaling(string AttributeCode, decimal Coefficient, decimal FlatValue = 0m);

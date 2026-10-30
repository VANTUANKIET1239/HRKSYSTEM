namespace GAME.Domain.Battle;

public sealed class BattleSkill
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string SkillTypeCode { get; init; }
    public int EnergyCost { get; init; }
    public BattleSkillAnimation Animation { get; init; } = BattleSkillAnimation.Default;
    public required IReadOnlyList<BattleSkillEffect> Effects { get; init; }
}

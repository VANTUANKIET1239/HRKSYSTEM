namespace GAME.Domain.Battle.Targets;

public sealed class BattleTargetSelectorRegistry
{
    private readonly IReadOnlyDictionary<string, IBattleTargetSelector> _selectors;

    public BattleTargetSelectorRegistry(IEnumerable<IBattleTargetSelector> selectors)
    {
        _selectors = selectors.ToDictionary(x => x.TargetTypeCode, StringComparer.OrdinalIgnoreCase);
    }

    public IBattleTargetSelector GetRequired(string targetTypeCode) =>
        !string.IsNullOrWhiteSpace(targetTypeCode) && _selectors.TryGetValue(targetTypeCode, out var selector)
            ? selector
            : throw new NotSupportedException(
                string.IsNullOrWhiteSpace(targetTypeCode)
                    ? "Battle effect has an empty TargetTypeCode. Ensure TargetType is included and HRK_SkillEffects.TargetTypeId is valid."
                    : $"No battle target selector is registered for '{targetTypeCode}'.");

    public static BattleTargetSelectorRegistry CreateDefault() => new(new IBattleTargetSelector[]
    {
        new SelfTargetSelector(),
        new AllyAllTargetSelector(),
        new AllyRandomTargetSelector(),
        new AllyRandom2TargetSelector(),
        new EnemySingleTargetSelector(),
        new EnemyAllTargetSelector(),
        new EnemyRandomTargetSelector(),
        new EnemyRandom4TargetSelector(),
        new EnemyFrontRowTargetSelector(),
        new EnemyBackRowTargetSelector(),
        new EnemySameLaneBackRowTargetSelector(),
        new LowestHpPercentTargetSelector()
    });
}

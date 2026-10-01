namespace GAME.Infrastructure.Services;

public static class BattleMitigationConfig
{
    public const string Code = "DEFENSE_MITIGATION_CONSTANT";
    public const decimal DefaultValue = 1000m;

    public static decimal Read(IReadOnlyDictionary<string, decimal> configs)
    {
        if (!configs.TryGetValue(Code, out var value)) return DefaultValue;
        if (value <= 0m)
            throw new InvalidOperationException($"Battle config {Code} must be greater than zero.");
        return value;
    }
}

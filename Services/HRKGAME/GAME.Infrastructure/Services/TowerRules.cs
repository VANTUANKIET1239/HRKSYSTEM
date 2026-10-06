using System.Text.Json;

namespace GAME.Infrastructure.Services;

/// <summary>Gameplay tuning stored in HRK_GameEvents.RulesJson. Defaults preserve legacy seeds.</summary>
public sealed record TowerRules
{
    public int? MaxFloor { get; init; }
    public int HpPerLevel { get; init; } = 35;
    public int AttackPerLevel { get; init; } = 6;
    public int DefensePerLevel { get; init; } = 3;
    public decimal SpeedPerFloor { get; init; } = .8m;
    public int MaxSpeedBonus { get; init; } = 50;
    public decimal SecondaryPerFloor { get; init; } = .2m;
    public decimal MaxCrit { get; init; } = 35;
    public decimal MaxResistance { get; init; } = 40;
    public int QuickClimbDailyLimit { get; init; } = 3;

    public static TowerRules Parse(string? json)
    {
        var rules = string.IsNullOrWhiteSpace(json) ? new TowerRules() :
            JsonSerializer.Deserialize<TowerRules>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("RulesJson không hợp lệ.");
        if (rules.MaxFloor <= 0 || rules.HpPerLevel < 0 || rules.AttackPerLevel < 0 || rules.DefensePerLevel < 0 ||
            rules.SpeedPerFloor < 0 || rules.MaxSpeedBonus < 0 || rules.SecondaryPerFloor < 0 ||
            rules.MaxCrit is < 0 or > 100 || rules.MaxResistance is < 0 or > 100 ||
            rules.QuickClimbDailyLimit <= 0)
            throw new InvalidOperationException("Cấu hình tăng trưởng tầng không hợp lệ.");
        return rules;
    }
}

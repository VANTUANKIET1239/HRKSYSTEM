namespace GAME.Domain.Battle;

public sealed class BattleCombatant
{
    // Battle-wide rule copied onto simulation-owned combatants, not a hero stat.
    public decimal DefenseMitigationConstant { get; set; } = 1000m;
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
    public decimal CritChance { get; set; }
    public decimal CritDamage { get; set; } = 150m;
    public int Energy { get; set; }
    public int MaxEnergy { get; init; } = 100;
    public required BattleSkill BasicSkill { get; init; }
    public BattleSkill? EnergySkill { get; init; }
    public List<BattleStatusEffect> StatusEffects { get; } = [];
    public Dictionary<string, int> Resources { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> ProcessedActionIds { get; init; } = new(StringComparer.Ordinal);
    public HashSet<long> ProcessedDefeatedCombatantIds { get; init; } = [];
    public int GetResource(string code, int defaultValue = 0) =>
        Resources.TryGetValue(code, out var val) ? val : defaultValue;
    public void SetResource(string code, int value) =>
        Resources[code] = value;
    public bool IsAlive => Hp > 0;
}

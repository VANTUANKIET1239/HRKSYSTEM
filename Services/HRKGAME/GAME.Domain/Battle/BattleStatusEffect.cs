namespace GAME.Domain.Battle;

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
    public bool ScaleArmorIgnoreWithStacks { get; init; }
    public bool CanCrit { get; init; }
    public bool CanKill { get; init; }
    public bool ConsumeOnHit { get; set; }
    public string? LastProcessedActionId { get; set; }
    public decimal DamageBonusPerStackPercent { get; set; }
    public decimal OutgoingDamageBonusPerStackPercent { get; set; }
    public decimal IncomingDamageBonusPerStackPercent { get; set; }
    public bool IncomingDamageBonusRestrictedToSource { get; set; }
    public bool ScaleModifiersWithStacks { get; set; } = true;
    public string? StatusGroup { get; init; }
    public bool Dispellable { get; set; } = true;
    public bool IsPermanent => RemainingTurns == -1;
    public bool IsActive => RemainingTurns > 0 || RemainingTurns == -1;
    public IReadOnlyList<BattleStatModifier> StatModifiers { get; init; } = [];
}

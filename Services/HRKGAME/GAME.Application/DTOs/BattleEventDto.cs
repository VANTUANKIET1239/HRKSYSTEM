using System;
using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class BattleEventDto
    {
        public int Sequence { get; set; }
        public int Round { get; set; }
        public int Turn { get; set; }
        public string EventType { get; set; } = null!;
        public long? ActorId { get; set; }
        public long? TargetId { get; set; }
        public string? SkillId { get; set; }
        public string? EffectTypeCode { get; set; }
        public string? DamageSchoolCode { get; set; }
        public int Value { get; set; }
        public int? HpBefore { get; set; }
        public int? HpAfter { get; set; }
        public int? EnergyBefore { get; set; }
        public int? EnergyAfter { get; set; }
        public bool IsCrit { get; set; }
        public int? RemainingTurns { get; set; }
        public int? PreviousStacks { get; set; }
        public int? CurrentStacks { get; set; }
        public int? MaxStacks { get; set; }
        public int? CastSequence { get; set; }
        public int TimelineOffsetMs { get; set; }
        public string? PhaseCode { get; set; }
        public string? ExecutionGroup { get; set; }
        public int? HitIndex { get; set; }
        public string? ResourceCode { get; set; }
        public int? PreviousValue { get; set; }
        public int? CurrentValue { get; set; }
        public string? ReasonCode { get; set; }
        public string? ActionId { get; set; }
        public string? StatusInstanceId { get; set; }
        public List<BattleStatModifierDto> StatModifiers { get; set; } = new();
        public long? SourceHeroId { get; set; }
        public int? OriginalDamage { get; set; }
        public int? RedirectRequested { get; set; }
        public int? RedirectActual { get; set; }
        public int? AllyDamageAfterRedirect { get; set; }
        public int? GuardianHpBefore { get; set; }
        public int? GuardianHpAfter { get; set; }
    }
}

using System;
using System.Collections.Generic;

namespace GAME.Application.DTOs
{
    public class StartBattleRequestDto
    {
        public string BattleType { get; set; } = "PVE";
        public int? StageId { get; set; }
        public string? FormationCode { get; set; }
        public List<FormationPositionRequestDto>? Positions { get; set; }
        public int? RandomSeed { get; set; }
    }

    public class StartBattleResultDto
    {
        public string BattleId { get; set; } = null!;
        public int RandomSeed { get; set; }
        public string Status { get; set; } = "COMPLETED";
        public string Winner { get; set; } = null!;
        public BattleInitialStateDto InitialState { get; set; } = new();
        public List<BattleEventDto> Events { get; set; } = new();
        public List<BattleHeroStatisticsDto> HeroStatistics { get; set; } = new();
    }

    public class BattleHeroStatisticsDto
    {
        public long CombatantId { get; set; }
        public long SourceHeroId { get; set; }
        public int Team { get; set; }

        public string HeroName { get; set; } = string.Empty;
        public string? Avatar { get; set; }

        public long PhysicalDamageDealt { get; set; }
        public long MagicDamageDealt { get; set; }
        public long HealingDone { get; set; }

        public long PhysicalDamageTaken { get; set; }
        public long MagicDamageTaken { get; set; }
    }

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

    public class BattleStatModifierDto
    {
        public string AttributeCode { get; set; } = null!;
        public string? AttributeName { get; set; }
        public string ValueType { get; set; } = null!;
        public decimal Value { get; set; }
    }

    public class BattleInitialStateDto
    {
        public string BattleId { get; set; } = null!;
        public List<PlayerHeroDto> LeftTeam { get; set; } = new();
        public List<PlayerHeroDto> RightTeam { get; set; } = new();
    }

    public class BattleLogDto
    {
        public long Id { get; set; }
        public string BattleId { get; set; } = null!;
        public int Turn { get; set; }
        public long ActorHeroId { get; set; }
        public long TargetHeroId { get; set; }
        public string? SkillId { get; set; }
        public string? SkillName { get; set; }
        public int Damage { get; set; }
        public bool IsCrit { get; set; }
        public object? BattleDetails { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}

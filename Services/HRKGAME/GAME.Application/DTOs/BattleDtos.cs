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
        public List<BattleStatModifierDto> StatModifiers { get; set; } = new();
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

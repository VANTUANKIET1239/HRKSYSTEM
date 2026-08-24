using System;
using System.Collections.Generic;

namespace GAME.Application.DTOs
{
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

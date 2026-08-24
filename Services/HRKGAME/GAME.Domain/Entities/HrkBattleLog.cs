using System;

namespace GAME.Domain.Entities
{
    public class HrkBattleLog
    {
        public long Id { get; set; }
        public string BattleId { get; set; } = null!;
        public int Turn { get; set; }
        public long ActorHeroId { get; set; }
        public long TargetHeroId { get; set; }
        public string? SkillId { get; set; }
        public int Damage { get; set; } = 0;
        public bool IsCrit { get; set; } = false;
        public string? BattleDetails { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public virtual HrkSkillTemplate? Skill { get; set; }
    }
}

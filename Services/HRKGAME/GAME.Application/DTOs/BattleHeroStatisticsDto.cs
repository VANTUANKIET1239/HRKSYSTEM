using System;
using System.Collections.Generic;

namespace GAME.Application.DTOs
{

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
}

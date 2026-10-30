using System.Collections.Generic;

namespace GAME.Application.DTOs
{
    public class CalculatedStatsDto
    {
        public int Hp { get; set; }
        public int Atk { get; set; }
        public int Def { get; set; }
        public int Spd { get; set; }
        public decimal Crit { get; set; }
        public decimal CritDmg { get; set; }
        public decimal Lifesteal { get; set; }
        public decimal Accuracy { get; set; }
        public decimal Resistance { get; set; }
        public int MagicDamage { get; set; }
        public int MagicResistance { get; set; }
    }
}

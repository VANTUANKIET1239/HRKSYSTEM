using GAME.Application.DTOs;
using GAME.Domain.Entities;
using System.Text.Json;

namespace GAME.Application.Common.Helpers
{
    public static class HeroStatsHelper
    {
        private static readonly JsonSerializerOptions CaseInsensitiveOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Tính toán chỉ số chiến đấu của võ tướng dựa trên Level, Stars và Base Stats của Template.
        /// </summary>
        public static CalculatedStatsDto CalculateStats(HrkPlayerHero? ph)
        {
            if (ph == null) return new CalculatedStatsDto();

            var ht = ph.HeroTemplate;

            if (!string.IsNullOrWhiteSpace(ph.CurrentStats))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<CalculatedStatsDto>(ph.CurrentStats, CaseInsensitiveOptions);
                    if (parsed != null)
                    {
                        if (ht != null && parsed.MagicDamage == 0 && parsed.MagicResistance == 0 && (ht.BaseMagicDamage > 0 || ht.BaseMagicResistance > 0))
                        {
                            decimal lMult = 1.0m + Math.Max(0, ph.Level - 1) * 0.05m;
                            decimal sMult = 1.0m + Math.Max(0, ph.Stars - 1) * 0.15m;
                            decimal tMult = lMult * sMult;
                            parsed.MagicDamage = (int)(ht.BaseMagicDamage * tMult);
                            parsed.MagicResistance = (int)(ht.BaseMagicResistance * tMult);
                        }
                        return parsed;
                    }
                }
                catch { }
            }

            if (ht == null) return new CalculatedStatsDto();

            decimal levelMultiplier = 1.0m + Math.Max(0, ph.Level - 1) * 0.05m;
            decimal starMultiplier = 1.0m + Math.Max(0, ph.Stars - 1) * 0.15m;
            decimal totalMultiplier = levelMultiplier * starMultiplier;

            return new CalculatedStatsDto
            {
                Hp = (int)(ht.BaseHp * totalMultiplier),
                Atk = (int)(ht.BaseAtk * totalMultiplier),
                Def = (int)(ht.BaseDef * totalMultiplier),
                Spd = (int)(ht.BaseSpd * (1.0m + Math.Max(0, ph.Level - 1) * 0.01m)),
                Crit = ht.BaseCrit,
                CritDmg = ht.BaseCritDmg,
                Lifesteal = ht.BaseLifesteal,
                Accuracy = ht.BaseAccuracy,
                Resistance = ht.BaseResistance,
                MagicDamage = (int)(ht.BaseMagicDamage * totalMultiplier),
                MagicResistance = (int)(ht.BaseMagicResistance * totalMultiplier)
            };
        }
    }
}

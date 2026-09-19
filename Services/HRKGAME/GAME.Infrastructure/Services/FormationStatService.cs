using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace GAME.Infrastructure.Services
{
    public class FormationStatService : IFormationStatService
    {
        private readonly IHeroStatCalculationService _heroStatCalculationService;
        private readonly ICombatPowerService _combatPowerService;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public FormationStatService(
            IHeroStatCalculationService heroStatCalculationService,
            ICombatPowerService combatPowerService)
        {
            _heroStatCalculationService = heroStatCalculationService;
            _combatPowerService = combatPowerService;
        }

        public FormationStatBonusDto ParseBonus(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new FormationStatBonusDto();

            try
            {
                return JsonSerializer.Deserialize<FormationStatBonusDto>(json, JsonOptions)
                       ?? new FormationStatBonusDto();
            }
            catch
            {
                return new FormationStatBonusDto();
            }
        }

        public CalculatedStatsDto ApplyFormationBonus(CalculatedStatsDto baseStats, FormationStatBonusDto? bonus)
        {
            if (bonus == null)
            {
                return CloneStats(baseStats);
            }

            decimal hpMult = 1m + (bonus.HpPercent / 100m);
            decimal atkMult = 1m + (bonus.AtkPercent / 100m);
            decimal defMult = 1m + (bonus.DefPercent / 100m);
            decimal spdMult = 1m + (bonus.SpdPercent / 100m);
            decimal mdmgMult = 1m + (bonus.MagicDamagePercent / 100m);
            decimal mresMult = 1m + (bonus.MagicResistancePercent / 100m);

            return new CalculatedStatsDto
            {
                Hp = (int)Math.Round(baseStats.Hp * hpMult, MidpointRounding.AwayFromZero),
                Atk = (int)Math.Round(baseStats.Atk * atkMult, MidpointRounding.AwayFromZero),
                Def = (int)Math.Round(baseStats.Def * defMult, MidpointRounding.AwayFromZero),
                Spd = (int)Math.Round(baseStats.Spd * spdMult, MidpointRounding.AwayFromZero),
                MagicDamage = (int)Math.Round(baseStats.MagicDamage * mdmgMult, MidpointRounding.AwayFromZero),
                MagicResistance = (int)Math.Round(baseStats.MagicResistance * mresMult, MidpointRounding.AwayFromZero),

                // Các chỉ số nâng cao giữ nguyên
                Crit = baseStats.Crit,
                CritDmg = baseStats.CritDmg,
                Lifesteal = baseStats.Lifesteal,
                Accuracy = baseStats.Accuracy,
                Resistance = baseStats.Resistance
            };
        }

        public FormationPowerCalculationResult CalculateFormationPower(
            IEnumerable<(HrkPlayerHero Hero, HrkPlayerEquipment? Equipment)> heroEquipmentList,
            FormationStatBonusDto? bonus,
            IReadOnlyCollection<CombatPowerConfigDto> powerConfigs)
        {
            var result = new FormationPowerCalculationResult();
            int totalBasePower = 0;
            int totalFormationPower = 0;

            foreach (var (hero, equipment) in heroEquipmentList)
            {
                if (hero == null) continue;

                var baseStats = _heroStatCalculationService.CalculateStats(hero, equipment).FinalStats;
                int basePower = _combatPowerService.Calculate(baseStats, powerConfigs);

                var formationStats = ApplyFormationBonus(baseStats, bonus);
                int heroPower = _combatPowerService.Calculate(formationStats, powerConfigs);

                totalBasePower += basePower;
                totalFormationPower += heroPower;

                result.HeroBasePowers[hero.Id] = basePower;
                result.HeroTotalPowers[hero.Id] = heroPower;
                result.HeroBaseStats[hero.Id] = baseStats;
                result.HeroFormationStats[hero.Id] = formationStats;
            }

            result.BaseHeroPower = totalBasePower;
            result.TotalPower = totalFormationPower;
            result.FormationBonusPower = Math.Max(0, totalFormationPower - totalBasePower);

            return result;
        }

        private static CalculatedStatsDto CloneStats(CalculatedStatsDto s) => new()
        {
            Hp = s.Hp,
            Atk = s.Atk,
            Def = s.Def,
            Spd = s.Spd,
            Crit = s.Crit,
            CritDmg = s.CritDmg,
            Lifesteal = s.Lifesteal,
            Accuracy = s.Accuracy,
            Resistance = s.Resistance,
            MagicDamage = s.MagicDamage,
            MagicResistance = s.MagicResistance
        };
    }
}

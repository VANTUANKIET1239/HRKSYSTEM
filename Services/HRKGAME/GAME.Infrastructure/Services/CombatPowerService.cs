using Core.Common.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Core.Common.Caching;
using Microsoft.EntityFrameworkCore;

namespace GAME.Infrastructure.Services
{
    public class CombatPowerService : ICombatPowerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cache;
        private const string CombatPowerConfigCacheKey = "config:combat-power:v1";

        public CombatPowerService(IUnitOfWork unitOfWork, ICacheService cache)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
        }

        public async Task<List<CombatPowerConfigDto>> GetConfigsAsync(CancellationToken cancellationToken = default)
        {
            var cached = await _cache.GetAsync<List<CombatPowerConfigDto>>(CombatPowerConfigCacheKey, cancellationToken);
            if (cached != null) return cached;

            var configs = await _unitOfWork.ReadOnlyRepository<HrkCombatPowerConfig>().Query()
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new CombatPowerConfigDto
                {
                    StatCode = x.StatCode, PowerPerUnit = x.PowerPerUnit,
                    IsEnabled = x.IsEnabled, DisplayOrder = x.DisplayOrder
                }).ToListAsync(cancellationToken);
            await _cache.SetAsync(CombatPowerConfigCacheKey, configs, TimeSpan.FromMinutes(30), cancellationToken);
            return configs;
        }

        public async Task<int> CalculateAsync(CalculatedStatsDto stats, CancellationToken cancellationToken = default)
        {
            var configs = await GetConfigsAsync(cancellationToken);
            return Calculate(stats, configs);
        }

        public int Calculate(CalculatedStatsDto stats, IReadOnlyCollection<CombatPowerConfigDto> configs)
        {
            decimal total = 0;
            var processedStandardStats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var config in configs.Where(x => x.IsEnabled))
            {
                var standardCode = MapToStandardStatCode(config.StatCode);
                if (standardCode == null || processedStandardStats.Contains(standardCode))
                    continue;

                processedStandardStats.Add(standardCode);
                decimal statValue = GetStatValue(stats, standardCode);
                total += statValue * config.PowerPerUnit;
            }

            return Math.Max(0, (int)Math.Round(total, MidpointRounding.AwayFromZero));
        }

        public static decimal NormalizePercentageStat(decimal val)
        {
            if (val > 0 && val <= 1.0m)
                return Math.Round(val * 100m, 2);
            return Math.Round(val, 2);
        }

        public static string? MapToStandardStatCode(string statCode) => (statCode ?? "").Trim().ToUpperInvariant() switch
        {
            "HP" => "HP",
            "ATK" => "ATK",
            "DEF" => "DEF",
            "SPD" => "SPD",
            "CRIT" or "CRIT_RATE" => "CRIT_RATE",
            "CRIT_DMG" or "CRIT_DAMAGE" => "CRIT_DAMAGE",
            "LIFESTEAL" => "LIFESTEAL",
            "ACCURACY" => "ACCURACY",
            "RESISTANCE" => "RESISTANCE",
            "MAGIC_DAMAGE" or "MAGIC_ATK" or "MATK" => "MAGIC_DAMAGE",
            "MAGIC_RESISTANCE" or "MAGIC_RESIST" or "MRES" => "MAGIC_RESISTANCE",
            _ => null
        };

        public static decimal GetStatValue(CalculatedStatsDto stats, string statCode) => (statCode ?? "").Trim().ToUpperInvariant() switch
        {
            "HP" => stats.Hp,
            "ATK" => stats.Atk,
            "DEF" => stats.Def,
            "SPD" => stats.Spd,
            "CRIT" or "CRIT_RATE" => NormalizePercentageStat(stats.Crit),
            "CRIT_DMG" or "CRIT_DAMAGE" => NormalizePercentageStat(stats.CritDmg),
            "LIFESTEAL" => NormalizePercentageStat(stats.Lifesteal),
            "ACCURACY" => NormalizePercentageStat(stats.Accuracy),
            "RESISTANCE" => NormalizePercentageStat(stats.Resistance),
            "MAGIC_DAMAGE" or "MAGIC_ATK" or "MATK" => stats.MagicDamage,
            "MAGIC_RESISTANCE" or "MAGIC_RESIST" or "MRES" => stats.MagicResistance,
            _ => 0
        };
    }
}

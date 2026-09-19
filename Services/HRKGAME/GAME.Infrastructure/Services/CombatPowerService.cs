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
            foreach (var config in configs.Where(x => x.IsEnabled))
                total += GetStatValue(stats, config.StatCode) * config.PowerPerUnit;

            return Math.Max(0, (int)Math.Round(total, MidpointRounding.AwayFromZero));
        }

        private static decimal GetStatValue(CalculatedStatsDto stats, string statCode) => statCode.ToUpperInvariant() switch
        {
            "HP" => stats.Hp,
            "ATK" => stats.Atk,
            "DEF" => stats.Def,
            "SPD" => stats.Spd,
            "CRIT" or "CRIT_RATE" => stats.Crit,
            "CRIT_DMG" or "CRIT_DAMAGE" => stats.CritDmg,
            "LIFESTEAL" => stats.Lifesteal,
            "ACCURACY" => stats.Accuracy,
            "RESISTANCE" => stats.Resistance,
            "MAGIC_DAMAGE" or "MAGIC_ATK" or "MATK" => stats.MagicDamage,
            "MAGIC_RESISTANCE" or "MAGIC_RESIST" or "MRES" => stats.MagicResistance,
            _ => 0
        };
    }
}

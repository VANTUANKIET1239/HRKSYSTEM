using Core.Common.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GAME.Infrastructure.Services
{
    public class HeroUpgradeService : IHeroUpgradeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGamePlayerService _gamePlayerService;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public HeroUpgradeService(IUnitOfWork unitOfWork, IGamePlayerService gamePlayerService)
        {
            _unitOfWork = unitOfWork;
            _gamePlayerService = gamePlayerService;
        }

        public async Task<HeroUpgradePreviewDto> GetPreviewAsync(string userId, long heroId, CancellationToken cancellationToken = default)
        {
            var (_, hero, config) = await LoadHeroAsync(userId, heroId, cancellationToken);
            return BuildPreview(hero, config);
        }

        public async Task<PlayerHeroDetailDto> UpgradeAsync(string userId, long heroId, int levels, CancellationToken cancellationToken = default)
        {
            if (levels is < 1 or > 50) throw new InvalidOperationException("So cap nang phai nam trong khoang 1 den 50.");

            var (player, hero, config) = await LoadHeroAsync(userId, heroId, cancellationToken);
            if (hero.Level >= config.MaxLevel) throw new InvalidOperationException("Vo tuong da dat cap toi da.");

            var actualLevels = Math.Min(levels, config.MaxLevel - hero.Level);
            long goldCost = 0;
            var materialCost = 0;
            for (var i = 0; i < actualLevels; i++)
            {
                goldCost += GetGoldCost(hero.Level + i, config);
                materialCost += GetMaterialCost(hero.Level + i, config);
            }

            var wallet = await _unitOfWork.Repository<HrkPlayerWallet>().Query()
                .FirstOrDefaultAsync(x => x.PlayerId == player.Id, cancellationToken)
                ?? throw new InvalidOperationException("Khong tim thay vi nguoi choi.");
            if (!wallet.HasEnoughGold(goldCost)) throw new InvalidOperationException("Khong du Vang de nang cap vo tuong.");
            if (wallet.UpgradeMaterials < materialCost) throw new InvalidOperationException("Khong du Da nang cap vo tuong.");

            var baseStats = GetHeroOnlyStats(hero, config, hero.Level);
            var nextStats = GetHeroOnlyStats(hero, config, hero.Level + actualLevels);
            wallet.DeductGold(goldCost);
            wallet.UpgradeMaterials -= materialCost;
            wallet.UpdatedOn = DateTime.UtcNow;
            hero.Level += actualLevels;
            hero.Exp = 0;
            hero.MaxExp = GetMaxExp(hero.Level);
            hero.CurrentStats = JsonSerializer.Serialize(nextStats, JsonOptions);
            hero.UpdatedOn = DateTime.UtcNow;

            _unitOfWork.Repository<HrkPlayerWallet>().Update(wallet);
            _unitOfWork.Repository<HrkPlayerHero>().Update(hero);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return await _gamePlayerService.GetPlayerHeroDetailAsync(userId, heroId, cancellationToken)
                ?? throw new KeyNotFoundException("Khong tim thay vo tuong sau khi nang cap.");
        }

        private async Task<(HrkPlayer Player, HrkPlayerHero Hero, HrkHeroRarityUpgradeConfig Config)> LoadHeroAsync(string userId, long heroId, CancellationToken ct)
        {
            var player = await _unitOfWork.Repository<HrkPlayer>().Query().FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive, ct)
                ?? throw new KeyNotFoundException("Khong tim thay nguoi choi.");
            var hero = await _unitOfWork.Repository<HrkPlayerHero>().Query()
                .Include(x => x.HeroTemplate)
                .FirstOrDefaultAsync(x => x.Id == heroId && x.PlayerId == player.Id && x.IsActive, ct)
                ?? throw new KeyNotFoundException("Khong tim thay vo tuong thuoc tai khoan nay.");
            var config = await _unitOfWork.ReadOnlyRepository<HrkHeroRarityUpgradeConfig>().Query()
                .FirstOrDefaultAsync(x => x.RarityId == hero.HeroTemplate.RarityId, ct)
                ?? throw new InvalidOperationException("Chua co cau hinh nang cap cho pham chat cua vo tuong.");
            return (player, hero, config);
        }

        private static HeroUpgradePreviewDto BuildPreview(HrkPlayerHero hero, HrkHeroRarityUpgradeConfig config)
        {
            var nextLevel = Math.Min(hero.Level + 1, config.MaxLevel);
            var current = GetHeroOnlyStats(hero, config, hero.Level);
            var next = GetHeroOnlyStats(hero, config, nextLevel);
            return new HeroUpgradePreviewDto
            {
                HeroId = hero.Id, CurrentLevel = hero.Level, NextLevel = nextLevel, MaxLevel = config.MaxLevel,
                GoldCost = hero.Level >= config.MaxLevel ? 0 : GetGoldCost(hero.Level, config),
                MaterialCost = hero.Level >= config.MaxLevel ? 0 : GetMaterialCost(hero.Level, config),
                CurrentStats = current, NextStats = next, StatIncrease = Subtract(next, current)
            };
        }

        private static long GetGoldCost(int currentLevel, HrkHeroRarityUpgradeConfig c) => c.BaseGoldCost + (long)Math.Max(0, currentLevel - 1) * c.GoldCostPerLevel;
        private static int GetMaterialCost(int currentLevel, HrkHeroRarityUpgradeConfig c) => c.BaseMaterialCost + Math.Max(0, currentLevel - 1) * c.MaterialCostPerLevel;
        private static int GetMaxExp(int level) => level * 100 + 400;

        private static CalculatedStatsDto GetHeroOnlyStats(HrkPlayerHero hero, HrkHeroRarityUpgradeConfig c, int level)
        {
            var t = hero.HeroTemplate;
            var multiplier = 1m + Math.Max(0, level - 1) * c.StatGrowthRate;
            return new CalculatedStatsDto
            {
                Hp = (int)Math.Round(t.BaseHp * multiplier, MidpointRounding.AwayFromZero),
                Atk = (int)Math.Round(t.BaseAtk * multiplier, MidpointRounding.AwayFromZero),
                Def = (int)Math.Round(t.BaseDef * multiplier, MidpointRounding.AwayFromZero),
                Spd = (int)Math.Round(t.BaseSpd * multiplier, MidpointRounding.AwayFromZero),
                Crit = Math.Round(t.BaseCrit * multiplier, 2), CritDmg = Math.Round(t.BaseCritDmg * multiplier, 2),
                Lifesteal = Math.Round(t.BaseLifesteal * multiplier, 2), Accuracy = Math.Round(t.BaseAccuracy * multiplier, 2),
                Resistance = Math.Round(t.BaseResistance * multiplier, 2),
                MagicDamage = (int)Math.Round(t.BaseMagicDamage * multiplier, MidpointRounding.AwayFromZero),
                MagicResistance = (int)Math.Round(t.BaseMagicResistance * multiplier, MidpointRounding.AwayFromZero)
            };
        }

        private static CalculatedStatsDto Subtract(CalculatedStatsDto a, CalculatedStatsDto b) => new()
        {
            Hp = a.Hp - b.Hp, Atk = a.Atk - b.Atk, Def = a.Def - b.Def, Spd = a.Spd - b.Spd,
            Crit = a.Crit - b.Crit, CritDmg = a.CritDmg - b.CritDmg, Lifesteal = a.Lifesteal - b.Lifesteal,
            Accuracy = a.Accuracy - b.Accuracy, Resistance = a.Resistance - b.Resistance,
            MagicDamage = a.MagicDamage - b.MagicDamage, MagicResistance = a.MagicResistance - b.MagicResistance
        };
    }
}

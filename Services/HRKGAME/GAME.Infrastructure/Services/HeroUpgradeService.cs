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
        private readonly ILevelExperienceService _levelExperienceService;
        private readonly IHeroProgressionStatService _progressionStats;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public HeroUpgradeService(
            IUnitOfWork unitOfWork,
            IGamePlayerService gamePlayerService,
            ILevelExperienceService levelExperienceService,
            IHeroProgressionStatService progressionStats)
        {
            _unitOfWork = unitOfWork;
            _gamePlayerService = gamePlayerService;
            _levelExperienceService = levelExperienceService;
            _progressionStats = progressionStats;
        }

        public async Task<HeroUpgradePreviewDto> GetPreviewAsync(string userId, long heroId, CancellationToken cancellationToken = default)
        {
            var (_, hero, config) = await LoadHeroAsync(userId, heroId, cancellationToken);
            var starBonus = await GetStarBonusAsync(hero, cancellationToken);
            return BuildPreview(hero, config, starBonus);
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

            var starBonus = await GetStarBonusAsync(hero, cancellationToken);
            var nextStats = _progressionStats.Calculate(hero.HeroTemplate, hero.Level + actualLevels, config.StatGrowthRate, starBonus);
            wallet.DeductGold(goldCost);
            wallet.UpgradeMaterials -= materialCost;
            wallet.UpdatedOn = DateTime.UtcNow;
            hero.Level += actualLevels;
            hero.Exp = 0;
            var expRequirement = await _levelExperienceService.GetHeroRequirementAsync(hero.Level, cancellationToken);
            hero.MaxExp = expRequirement.IsMaxLevel ? 0 : expRequirement.RequiredExp;
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

        private HeroUpgradePreviewDto BuildPreview(HrkPlayerHero hero, HrkHeroRarityUpgradeConfig config, decimal starBonus)
        {
            var nextLevel = Math.Min(hero.Level + 1, config.MaxLevel);
            var current = _progressionStats.Calculate(hero.HeroTemplate, hero.Level, config.StatGrowthRate, starBonus);
            var next = _progressionStats.Calculate(hero.HeroTemplate, nextLevel, config.StatGrowthRate, starBonus);
            return new HeroUpgradePreviewDto
            {
                HeroId = hero.Id,
                CurrentLevel = hero.Level,
                NextLevel = nextLevel,
                MaxLevel = config.MaxLevel,
                GoldCost = hero.Level >= config.MaxLevel ? 0 : GetGoldCost(hero.Level, config),
                MaterialCost = hero.Level >= config.MaxLevel ? 0 : GetMaterialCost(hero.Level, config),
                CurrentStats = current,
                NextStats = next,
                StatIncrease = _progressionStats.Subtract(next, current)
            };
        }

        private async Task<decimal> GetStarBonusAsync(HrkPlayerHero hero, CancellationToken ct)
        {
            if (hero.Stars <= 1) return 0m;
            return await _unitOfWork.ReadOnlyRepository<HrkHeroStarUpgradeConfig>().Query()
                .Where(x => x.RarityId == hero.HeroTemplate.RarityId && x.NextStar == hero.Stars && x.IsEnabled)
                .Select(x => x.GrowthBonusPercent).FirstOrDefaultAsync(ct);
        }

        private static long GetGoldCost(int currentLevel, HrkHeroRarityUpgradeConfig c) => c.BaseGoldCost + (long)Math.Max(0, currentLevel - 1) * c.GoldCostPerLevel;
        private static int GetMaterialCost(int currentLevel, HrkHeroRarityUpgradeConfig c) => c.BaseMaterialCost + Math.Max(0, currentLevel - 1) * c.MaterialCostPerLevel;
    }
}

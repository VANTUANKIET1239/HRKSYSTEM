using Core.Common.Repositories;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Infrastructure.Services
{
    public class GamePlayerService : IGamePlayerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHeroStatCalculationService _heroStatCalculationService;
        private readonly ICombatPowerService _combatPowerService;

        public GamePlayerService(
            IUnitOfWork unitOfWork,
            IHeroStatCalculationService heroStatCalculationService,
            ICombatPowerService combatPowerService)
        {
            _unitOfWork = unitOfWork;
            _heroStatCalculationService = heroStatCalculationService;
            _combatPowerService = combatPowerService;
        }

        public async Task<HrkPlayer?> GetPlayerByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.ReadOnlyRepository<HrkPlayer>().Query()
                .FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive, cancellationToken);
        }

        public async Task<HrkPlayerWallet?> GetWalletByPlayerIdAsync(long playerId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Repository<HrkPlayerWallet>().Query()
                .FirstOrDefaultAsync(w => w.PlayerId == playerId, cancellationToken);
        }

        public async Task<PlayerProfileDto?> GetPlayerProfileAsync(string userId, CancellationToken cancellationToken = default)
        {
            var player = await GetPlayerByUserIdAsync(userId, cancellationToken);
            if (player == null) return null;

            return new PlayerProfileDto
            {
                Id = player.Id,
                UserId = player.UserId,
                PlayerName = player.PlayerName,
                Level = player.Level,
                CreatedOn = player.CreatedOn,
                UpdatedOn = player.UpdatedOn
            };
        }

        public async Task<PlayerWalletDto?> GetPlayerWalletAsync(string userId, CancellationToken cancellationToken = default)
        {
            var player = await GetPlayerByUserIdAsync(userId, cancellationToken);
            if (player == null) return null;

            var wallet = await GetWalletByPlayerIdAsync(player.Id, cancellationToken) ?? new HrkPlayerWallet
            {
                PlayerId = player.Id,
                Gold = 0,
                Diamonds = 0,
                UpgradeMaterials = 0,
                MaxCapacity = 200,
                UpdatedOn = DateTime.UtcNow
            };

            return new PlayerWalletDto
            {
                PlayerId = wallet.PlayerId,
                Gold = wallet.Gold,
                Diamonds = wallet.Diamonds,
                UpgradeMaterials = wallet.UpgradeMaterials,
                MaxCapacity = wallet.MaxCapacity,
                UpdatedOn = wallet.UpdatedOn
            };
        }

        public async Task<PlayerGameInfoDto?> GetPlayerGameInfoAsync(string userId, CancellationToken cancellationToken = default)
        {
            var player = await GetPlayerByUserIdAsync(userId, cancellationToken);
            if (player == null) return null;
            var wallet = await GetWalletByPlayerIdAsync(player.Id, cancellationToken);
            return new PlayerGameInfoDto
            {
                Profile = new PlayerProfileDto { Id = player.Id, UserId = player.UserId, PlayerName = player.PlayerName, Level = player.Level, CreatedOn = player.CreatedOn, UpdatedOn = player.UpdatedOn },
                Wallet = wallet == null ? null : new PlayerWalletDto { PlayerId = wallet.PlayerId, Gold = wallet.Gold, Diamonds = wallet.Diamonds, UpgradeMaterials = wallet.UpgradeMaterials, MaxCapacity = wallet.MaxCapacity, UpdatedOn = wallet.UpdatedOn }
            };
        }

        public async Task<List<PlayerHeroDto>> GetPlayerHeroesAsync(string userId, CancellationToken cancellationToken = default)
        {
            var player = await GetPlayerByUserIdAsync(userId, cancellationToken);
            if (player == null) return new List<PlayerHeroDto>();

            var playerHeroes = await _unitOfWork.ReadOnlyRepository<HrkPlayerHero>().Query()
                .Where(ph => ph.PlayerId == player.Id && ph.IsActive)
                .Include(ph => ph.HeroTemplate).ThenInclude(ht => ht.Faction)
                .Include(ph => ph.HeroTemplate).ThenInclude(ht => ht.Class)
                .Include(ph => ph.HeroTemplate).ThenInclude(ht => ht.Rarity)
                .Include(ph => ph.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.EffectType)
                .Include(ph => ph.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.TargetType)
                .Include(ph => ph.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.Scalings).ThenInclude(sc => sc.AttributeType)
                .Include(ph => ph.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.StatModifiers).ThenInclude(sm => sm.AttributeType)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            // Batch load every equipped item once. Calling GetPlayerHeroDetailAsync for
            // every card used to cause 2+ SQL queries per hero (the N+1 bottleneck).
            var equipments = await GetEquipmentQuery()
                .Where(e => e.PlayerId == player.Id && playerHeroes.Select(h => h.Id).Contains(e.HeroId))
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            var equipmentByHeroId = equipments.ToDictionary(e => e.HeroId);
            var powerConfigs = await _combatPowerService.GetConfigsAsync(cancellationToken);
            var result = new List<PlayerHeroDto>(playerHeroes.Count);
            foreach (var hero in playerHeroes)
            {
                var dto = GameDtoMapper.MapPlayerHero(hero);
                if (dto == null) continue;
                equipmentByHeroId.TryGetValue(hero.Id, out var equipment);
                var stats = _heroStatCalculationService.CalculateStats(hero, equipment).FinalStats;
                dto.Stats = stats;
                dto.Power = _combatPowerService.Calculate(stats, powerConfigs);
                result.Add(dto);
            }

            return result;
        }

        public async Task<PlayerHeroDetailDto?> GetPlayerHeroDetailAsync(string userId, long heroId, CancellationToken cancellationToken = default)
        {
            var player = await GetPlayerByUserIdAsync(userId, cancellationToken);
            if (player == null) return null;

            var ph = await _unitOfWork.ReadOnlyRepository<HrkPlayerHero>().Query()
                .Where(x => x.Id == heroId && x.PlayerId == player.Id && x.IsActive)
                .Include(x => x.HeroTemplate).ThenInclude(ht => ht.Faction)
                .Include(x => x.HeroTemplate).ThenInclude(ht => ht.Class)
                .Include(x => x.HeroTemplate).ThenInclude(ht => ht.Rarity)
                .Include(x => x.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.EffectType)
                .Include(x => x.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.TargetType)
                .Include(x => x.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.Scalings).ThenInclude(sc => sc.AttributeType)
                .Include(x => x.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.StatModifiers).ThenInclude(sm => sm.AttributeType)
                .FirstOrDefaultAsync(cancellationToken);

            if (ph == null) return null;

            var eq = await GetEquipmentQuery()
                .FirstOrDefaultAsync(e => e.PlayerId == player.Id && e.HeroId == ph.Id, cancellationToken);

            var baseHero = GameDtoMapper.MapPlayerHero(ph);
            if (baseHero == null) return null;

            var statResult = _heroStatCalculationService.CalculateStats(ph, eq);

            return new PlayerHeroDetailDto
            {
                Id = baseHero.Id,
                HeroTemplateId = baseHero.HeroTemplateId,
                Name = baseHero.Name,
                Avatar = baseHero.Avatar,
                FactionCode = baseHero.FactionCode,
                FactionName = baseHero.FactionName,
                ClassCode = baseHero.ClassCode,
                ClassName = baseHero.ClassName,
                RarityId = baseHero.RarityId,
                RarityCode = baseHero.RarityCode,
                RarityName = baseHero.RarityName,
                RarityColorHex = baseHero.RarityColorHex,
                Level = baseHero.Level,
                Exp = baseHero.Exp,
                MaxExp = baseHero.MaxExp,
                Stars = baseHero.Stars,
                Power = await _combatPowerService.CalculateAsync(statResult.FinalStats, cancellationToken),
                AuraTier = baseHero.AuraTier,
                IsLocked = baseHero.IsLocked,
                IsFavorite = baseHero.IsFavorite,
                Stats = statResult.FinalStats,
                StatBreakdowns = statResult.Breakdowns,
                Skills = baseHero.Skills,
                Equipment = GameDtoMapper.MapHeroEquipment(eq, ph.Id)
            };
        }

        public async Task<bool> SetPlayerHeroLockAsync(string userId, long heroId, bool isLocked, CancellationToken cancellationToken = default)
        {
            var hero = await GetOwnedActiveHeroAsync(userId, heroId, cancellationToken);
            hero.IsLocked = isLocked;
            hero.UpdatedOn = DateTime.UtcNow;
            _unitOfWork.Repository<HrkPlayerHero>().Update(hero);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return hero.IsLocked;
        }

        public async Task<bool> SetPlayerHeroFavoriteAsync(string userId, long heroId, bool isFavorite, CancellationToken cancellationToken = default)
        {
            var hero = await GetOwnedActiveHeroAsync(userId, heroId, cancellationToken);
            hero.IsFavorite = isFavorite;
            hero.UpdatedOn = DateTime.UtcNow;
            _unitOfWork.Repository<HrkPlayerHero>().Update(hero);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return hero.IsFavorite;
        }

        private async Task<HrkPlayerHero> GetOwnedActiveHeroAsync(string userId, long heroId, CancellationToken cancellationToken)
        {
            var player = await GetPlayerByUserIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException("Khong tim thay thong tin nguoi choi.");

            return await _unitOfWork.Repository<HrkPlayerHero>().Query()
                .FirstOrDefaultAsync(x => x.Id == heroId && x.PlayerId == player.Id && x.IsActive, cancellationToken)
                ?? throw new KeyNotFoundException("Khong tim thay vo tuong thuoc tai khoan nay.");
        }

        private IQueryable<HrkPlayerEquipment> GetEquipmentQuery() => _unitOfWork.ReadOnlyRepository<HrkPlayerEquipment>().Query()
            .Include(e => e.Weapon).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Rarity)
            .Include(e => e.Weapon).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
            .Include(e => e.Weapon).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Attributes).ThenInclude(x => x.AttributeType)
            .Include(e => e.Armor).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Rarity)
            .Include(e => e.Armor).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
            .Include(e => e.Armor).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Attributes).ThenInclude(x => x.AttributeType)
            .Include(e => e.Helmet).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Rarity)
            .Include(e => e.Helmet).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
            .Include(e => e.Helmet).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Attributes).ThenInclude(x => x.AttributeType)
            .Include(e => e.Boots).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Rarity)
            .Include(e => e.Boots).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
            .Include(e => e.Boots).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Attributes).ThenInclude(x => x.AttributeType)
            .Include(e => e.Ring).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Rarity)
            .Include(e => e.Ring).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
            .Include(e => e.Ring).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Attributes).ThenInclude(x => x.AttributeType)
            .Include(e => e.Artifact).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Rarity)
            .Include(e => e.Artifact).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
            .Include(e => e.Artifact).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Attributes).ThenInclude(x => x.AttributeType);
    }
}

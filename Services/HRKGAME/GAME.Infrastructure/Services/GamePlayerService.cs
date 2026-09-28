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
using System.Security.Cryptography;

namespace GAME.Infrastructure.Services
{
    public class GamePlayerService : IGamePlayerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHeroStatCalculationService _heroStatCalculationService;
        private readonly ICombatPowerService _combatPowerService;
        private readonly IFormationPowerQueryService _formationPowerQueryService;

        public GamePlayerService(
            IUnitOfWork unitOfWork,
            IHeroStatCalculationService heroStatCalculationService,
            ICombatPowerService combatPowerService,
            IFormationPowerQueryService formationPowerQueryService)
        {
            _unitOfWork = unitOfWork;
            _heroStatCalculationService = heroStatCalculationService;
            _combatPowerService = combatPowerService;
            _formationPowerQueryService = formationPowerQueryService;
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

            return await BuildProfileAsync(player, cancellationToken);
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
            var formationPower = await _formationPowerQueryService.GetDefaultFormationPowerAsync(player.Id, cancellationToken);
            var profile = await BuildProfileAsync(player, cancellationToken, formationPower.TotalPower);

            return new PlayerGameInfoDto
            {
                Profile = profile,
                Wallet = wallet == null ? null : new PlayerWalletDto { PlayerId = wallet.PlayerId, Gold = wallet.Gold, Diamonds = wallet.Diamonds, UpgradeMaterials = wallet.UpgradeMaterials, MaxCapacity = wallet.MaxCapacity, UpdatedOn = wallet.UpdatedOn },
                SelectedFormationId = formationPower.FormationId,
                SelectedFormationCode = formationPower.FormationCode,
                SelectedFormationName = formationPower.FormationName,
                FormationPower = formationPower.TotalPower
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
                // List/card endpoint intentionally excludes the skill-effect graph.
                // Hero management loads that graph from the detail endpoint only
                // after a hero is selected. Formation/campaign consumers need stats
                // and power, not effect parameters for every owned hero.
                .AsSplitQuery()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            // Batch load every equipped item once. Calling GetPlayerHeroDetailAsync for
            // every card used to cause 2+ SQL queries per hero (the N+1 bottleneck).
            var heroIds = playerHeroes.Select(h => h.Id).ToList();
            var equipmentByHeroId = await EquipmentBatchLoader.LoadForHeroesAsync(
                _unitOfWork, player.Id, heroIds, cancellationToken);
            var powerConfigs = await _combatPowerService.GetConfigsAsync(cancellationToken);

            // Batch load star auras for player heroes (no N+1 query)
            var templateIds = playerHeroes.Select(h => h.HeroTemplateId).Distinct().ToList();
            var starAuraConfigs = await _unitOfWork.ReadOnlyRepository<HrkHeroStarAuraConfig>().Query()
                .Where(c => c.IsActive && templateIds.Contains(c.HeroTemplateId))
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            var auraLookup = starAuraConfigs.ToDictionary(c => (c.HeroTemplateId, c.StarLevel));

            var result = new List<PlayerHeroDto>(playerHeroes.Count);
            foreach (var hero in playerHeroes)
            {
                var clampedStars = (byte)Math.Clamp(hero.Stars, 0, 5);
                auraLookup.TryGetValue((hero.HeroTemplateId, clampedStars), out var auraCfg);
                var dto = GameDtoMapper.MapPlayerHero(hero, auraCfg, includeSkills: false);
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
                .Include(x => x.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.Parameters)
                .Include(x => x.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.Scalings).ThenInclude(sc => sc.AttributeType)
                .Include(x => x.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.StatModifiers).ThenInclude(sm => sm.AttributeType)
                .AsSplitQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (ph == null) return null;

            var equipments = await EquipmentBatchLoader.LoadForHeroesAsync(
                _unitOfWork, player.Id, new[] { ph.Id }, cancellationToken);
            equipments.TryGetValue(ph.Id, out var eq);

            var detailClampedStars = (byte)Math.Clamp(ph.Stars, 0, 5);
            var starAura = await _unitOfWork.ReadOnlyRepository<HrkHeroStarAuraConfig>().Query()
                .FirstOrDefaultAsync(c => c.HeroTemplateId == ph.HeroTemplateId && c.StarLevel == detailClampedStars && c.IsActive, cancellationToken);

            var baseHero = GameDtoMapper.MapPlayerHero(ph, starAura);
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

        public async Task<List<PlayerAvatarTemplateDto>> GetAvatarTemplatesAsync(string userId, CancellationToken cancellationToken = default)
        {
            var player = await GetRequiredPlayerAsync(userId, cancellationToken);
            return await _unitOfWork.ReadOnlyRepository<HrkAvatarTemplate>().Query()
                .Where(x => x.IsEnabled)
                .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id)
                .Select(x => new PlayerAvatarTemplateDto
                {
                    Id = x.Id, Code = x.Code, Name = x.Name, ImagePath = x.ImagePath,
                    IsSelected = player.AvatarType == "TEMPLATE" && player.AvatarTemplateId == x.Id
                }).ToListAsync(cancellationToken);
        }

        public async Task<PlayerProfileDto> SelectAvatarTemplateAsync(string userId, int avatarTemplateId, CancellationToken cancellationToken = default)
        {
            var player = await GetRequiredPlayerAsync(userId, cancellationToken, tracked: true);
            var templateExists = await _unitOfWork.ReadOnlyRepository<HrkAvatarTemplate>().Query()
                .AnyAsync(x => x.Id == avatarTemplateId && x.IsEnabled, cancellationToken);
            if (!templateExists) throw new KeyNotFoundException("Avatar không tồn tại hoặc đã bị vô hiệu hóa.");
            player.AvatarType = "TEMPLATE";
            player.AvatarTemplateId = avatarTemplateId;
            player.UpdatedOn = DateTime.UtcNow;
            _unitOfWork.Repository<HrkPlayer>().Update(player);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return await BuildProfileAsync(player, cancellationToken);
        }

        public async Task<PlayerProfileDto> SaveCustomAvatarAsync(string userId, CustomAvatarUploadDto upload, CancellationToken cancellationToken = default)
        {
            var player = await GetRequiredPlayerAsync(userId, cancellationToken, tracked: true);
            var repository = _unitOfWork.Repository<HrkPlayerCustomAvatar>();
            var avatar = await repository.Query().FirstOrDefaultAsync(x => x.PlayerId == player.Id, cancellationToken);
            var now = DateTime.UtcNow;
            var isNew = avatar == null;
            if (avatar == null)
            {
                avatar = new HrkPlayerCustomAvatar { PlayerId = player.Id, CreatedOn = now };
                await repository.AddAsync(avatar);
            }
            avatar.ImageData = upload.ImageData;
            avatar.ContentType = upload.ContentType;
            avatar.FileName = upload.FileName;
            avatar.FileSize = upload.ImageData.Length;
            avatar.Width = upload.Width;
            avatar.Height = upload.Height;
            avatar.ContentHash = Convert.ToHexString(SHA256.HashData(upload.ImageData)).ToLowerInvariant();
            avatar.UpdatedOn = now;
            if (!isNew) repository.Update(avatar);
            player.AvatarType = "CUSTOM";
            player.UpdatedOn = now;
            _unitOfWork.Repository<HrkPlayer>().Update(player);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return await BuildProfileAsync(player, cancellationToken);
        }

        public async Task<PlayerCustomAvatarDto?> GetCustomAvatarAsync(long playerId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.ReadOnlyRepository<HrkPlayerCustomAvatar>().Query()
                .Where(x => x.PlayerId == playerId)
                .Select(x => new PlayerCustomAvatarDto { ImageData = x.ImageData, ContentType = x.ContentType, ContentHash = x.ContentHash })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<PlayerProfileDto> DeleteCustomAvatarAsync(string userId, CancellationToken cancellationToken = default)
        {
            var player = await GetRequiredPlayerAsync(userId, cancellationToken, tracked: true);
            var repository = _unitOfWork.Repository<HrkPlayerCustomAvatar>();
            var avatar = await repository.Query().FirstOrDefaultAsync(x => x.PlayerId == player.Id, cancellationToken);
            if (avatar != null) repository.Remove(avatar);
            var defaultTemplateId = await _unitOfWork.ReadOnlyRepository<HrkAvatarTemplate>().Query()
                .Where(x => x.IsEnabled).OrderByDescending(x => x.IsDefault).ThenBy(x => x.DisplayOrder)
                .Select(x => (int?)x.Id).FirstOrDefaultAsync(cancellationToken);
            player.AvatarType = "TEMPLATE";
            player.AvatarTemplateId = defaultTemplateId;
            player.UpdatedOn = DateTime.UtcNow;
            _unitOfWork.Repository<HrkPlayer>().Update(player);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return await BuildProfileAsync(player, cancellationToken);
        }

        private async Task<PlayerProfileDto> BuildProfileAsync(
            HrkPlayer player,
            CancellationToken cancellationToken,
            int? knownFormationPower = null)
        {
            string? avatarUrl = null;
            if (player.AvatarType == "TEMPLATE")
            {
                avatarUrl = await _unitOfWork.ReadOnlyRepository<HrkAvatarTemplate>().Query()
                    .Where(x => x.IsEnabled && (x.Id == player.AvatarTemplateId || (player.AvatarTemplateId == null && x.IsDefault)))
                    .OrderByDescending(x => x.Id == player.AvatarTemplateId).ThenByDescending(x => x.IsDefault)
                    .Select(x => x.ImagePath).FirstOrDefaultAsync(cancellationToken);
            }

            var power = knownFormationPower;
            if (!power.HasValue)
            {
                var formationPower = await _formationPowerQueryService.GetDefaultFormationPowerAsync(player.Id, cancellationToken);
                power = formationPower.TotalPower;
            }

            return new PlayerProfileDto
            {
                Id = player.Id, UserId = player.UserId, PlayerName = player.PlayerName, Level = player.Level,
                Exp = player.Exp, MaxExp = player.MaxExp, Power = power.Value,
                AvatarType = player.AvatarType, AvatarTemplateId = player.AvatarTemplateId, AvatarUrl = avatarUrl,
                AvatarVersion = new DateTimeOffset(player.UpdatedOn).ToUnixTimeSeconds(),
                CreatedOn = player.CreatedOn, UpdatedOn = player.UpdatedOn
            };
        }

        private async Task<HrkPlayer> GetRequiredPlayerAsync(string userId, CancellationToken cancellationToken, bool tracked = false)
        {
            var query = tracked ? _unitOfWork.Repository<HrkPlayer>().Query() : _unitOfWork.ReadOnlyRepository<HrkPlayer>().Query();
            return await query.FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive, cancellationToken)
                ?? throw new KeyNotFoundException("Không tìm thấy thông tin người chơi.");
        }

        private async Task<HrkPlayerHero> GetOwnedActiveHeroAsync(string userId, long heroId, CancellationToken cancellationToken)
        {
            var player = await GetPlayerByUserIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException("Khong tim thay thong tin nguoi choi.");

            return await _unitOfWork.Repository<HrkPlayerHero>().Query()
                .FirstOrDefaultAsync(x => x.Id == heroId && x.PlayerId == player.Id && x.IsActive, cancellationToken)
                ?? throw new KeyNotFoundException("Khong tim thay vo tuong thuoc tai khoan nay.");
        }

    }
}

using Core.Common.Repositories;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAME.Infrastructure.Services
{
    public class GamePlayerService : IGamePlayerService
    {
        private readonly IUnitOfWork<GameDbContext> _unitOfWork;

        public GamePlayerService(IUnitOfWork<GameDbContext> unitOfWork)
        {
            _unitOfWork = unitOfWork;
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

        public async Task<List<PlayerHeroDto>> GetPlayerHeroesAsync(string userId, CancellationToken cancellationToken = default)
        {
            var player = await GetPlayerByUserIdAsync(userId, cancellationToken);
            if (player == null) return new List<PlayerHeroDto>();

            var playerHeroes = await _unitOfWork.ReadOnlyRepository<HrkPlayerHero>().Query()
                .Where(ph => ph.PlayerId == player.Id && ph.IsActive)
                .Include(ph => ph.HeroTemplate).ThenInclude(ht => ht.Faction)
                .Include(ph => ph.HeroTemplate).ThenInclude(ht => ht.Class)
                .Include(ph => ph.HeroTemplate).ThenInclude(ht => ht.Rarity)
                .Include(ph => ph.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.CostType)
                .Include(ph => ph.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Category)
                .Include(ph => ph.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.DamageType)
                .Include(ph => ph.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.EffectType)
                .ToListAsync(cancellationToken);

            return playerHeroes
                .Select(ph => GameDtoMapper.MapPlayerHero(ph)!)
                .Where(dto => dto != null)
                .ToList();
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
                .Include(x => x.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.CostType)
                .Include(x => x.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Category)
                .Include(x => x.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.DamageType)
                .Include(x => x.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.EffectType)
                .FirstOrDefaultAsync(cancellationToken);

            if (ph == null) return null;

            var eq = await _unitOfWork.ReadOnlyRepository<HrkPlayerEquipment>().Query()
                .Include(e => e.Weapon).ThenInclude(w => w!.ItemTemplate).ThenInclude(it => it.Rarity)
                .Include(e => e.Weapon).ThenInclude(w => w!.ItemTemplate).ThenInclude(it => it.Category)
                .Include(e => e.Armor).ThenInclude(a => a!.ItemTemplate).ThenInclude(it => it.Rarity)
                .Include(e => e.Armor).ThenInclude(a => a!.ItemTemplate).ThenInclude(it => it.Category)
                .Include(e => e.Helmet).ThenInclude(h => h!.ItemTemplate).ThenInclude(it => it.Rarity)
                .Include(e => e.Helmet).ThenInclude(h => h!.ItemTemplate).ThenInclude(it => it.Category)
                .Include(e => e.Boots).ThenInclude(b => b!.ItemTemplate).ThenInclude(it => it.Rarity)
                .Include(e => e.Boots).ThenInclude(b => b!.ItemTemplate).ThenInclude(it => it.Category)
                .Include(e => e.Ring).ThenInclude(r => r!.ItemTemplate).ThenInclude(it => it.Rarity)
                .Include(e => e.Ring).ThenInclude(r => r!.ItemTemplate).ThenInclude(it => it.Category)
                .Include(e => e.Artifact).ThenInclude(ar => ar!.ItemTemplate).ThenInclude(it => it.Rarity)
                .Include(e => e.Artifact).ThenInclude(ar => ar!.ItemTemplate).ThenInclude(it => it.Category)
                .FirstOrDefaultAsync(e => e.PlayerId == player.Id && e.HeroId == ph.Id, cancellationToken);

            var baseHero = GameDtoMapper.MapPlayerHero(ph);
            if (baseHero == null) return null;

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
                RarityCode = baseHero.RarityCode,
                RarityName = baseHero.RarityName,
                RarityColorHex = baseHero.RarityColorHex,
                Level = baseHero.Level,
                Exp = baseHero.Exp,
                MaxExp = baseHero.MaxExp,
                Stars = baseHero.Stars,
                Power = baseHero.Power,
                AuraTier = baseHero.AuraTier,
                IsLocked = baseHero.IsLocked,
                IsFavorite = baseHero.IsFavorite,
                Stats = baseHero.Stats,
                Skills = baseHero.Skills,
                Equipment = GameDtoMapper.MapHeroEquipment(eq, ph.Id)
            };
        }
    }
}

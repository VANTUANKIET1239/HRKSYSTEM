using Core.Common.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GAME.Infrastructure.Services
{
    public class GamePlayerService : IGamePlayerService
    {
        private readonly IUnitOfWork<GameDbContext> _unitOfWork;

        public GamePlayerService(IUnitOfWork<GameDbContext> unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PlayerProfileDto?> GetPlayerProfileAsync(string userId, CancellationToken cancellationToken = default)
        {
            var player = await _unitOfWork.Repository<HrkPlayer>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

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
            var player = await _unitOfWork.Repository<HrkPlayer>().Query()
                .AsNoTracking()
                .Include(p => p.Wallet)
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            if (player == null) return null;

            var wallet = player.Wallet ?? new HrkPlayerWallet
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
            var player = await _unitOfWork.Repository<HrkPlayer>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            if (player == null) return new List<PlayerHeroDto>();

            var playerHeroes = await _unitOfWork.Repository<HrkPlayerHero>().Query()
                .AsNoTracking()
                .Where(ph => ph.PlayerId == player.Id)
                .Include(ph => ph.HeroTemplate).ThenInclude(ht => ht.Faction)
                .Include(ph => ph.HeroTemplate).ThenInclude(ht => ht.Class)
                .Include(ph => ph.HeroTemplate).ThenInclude(ht => ht.Rarity)
                .Include(ph => ph.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.CostType)
                .Include(ph => ph.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Category)
                .Include(ph => ph.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.DamageType)
                .Include(ph => ph.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.EffectType)
                .ToListAsync(cancellationToken);

            return playerHeroes.Select(ph => MapPlayerHero(ph)).ToList();
        }

        public async Task<PlayerHeroDetailDto?> GetPlayerHeroDetailAsync(string userId, long heroId, CancellationToken cancellationToken = default)
        {
            var player = await _unitOfWork.Repository<HrkPlayer>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            if (player == null) return null;

            var ph = await _unitOfWork.Repository<HrkPlayerHero>().Query()
                .AsNoTracking()
                .Where(x => x.Id == heroId && x.PlayerId == player.Id)
                .Include(x => x.HeroTemplate).ThenInclude(ht => ht.Faction)
                .Include(x => x.HeroTemplate).ThenInclude(ht => ht.Class)
                .Include(x => x.HeroTemplate).ThenInclude(ht => ht.Rarity)
                .Include(x => x.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.CostType)
                .Include(x => x.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Category)
                .Include(x => x.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.DamageType)
                .Include(x => x.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.EffectType)
                .FirstOrDefaultAsync(cancellationToken);

            if (ph == null) return null;

            var eq = await _unitOfWork.Repository<HrkPlayerEquipment>().Query()
                .AsNoTracking()
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

            var baseHero = MapPlayerHero(ph);

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
                Equipment = new HeroEquipmentDto
                {
                    HeroId = ph.Id,
                    Weapon = MapItem(eq?.Weapon),
                    Armor = MapItem(eq?.Armor),
                    Helmet = MapItem(eq?.Helmet),
                    Boots = MapItem(eq?.Boots),
                    Ring = MapItem(eq?.Ring),
                    Artifact = MapItem(eq?.Artifact)
                }
            };
        }

        private static PlayerHeroDto MapPlayerHero(HrkPlayerHero ph)
        {
            var ht = ph.HeroTemplate;
            return new PlayerHeroDto
            {
                Id = ph.Id,
                HeroTemplateId = ph.HeroTemplateId,
                Name = ht?.Name ?? "",
                Avatar = ht?.Avatar ?? "",
                FactionCode = ht?.Faction?.Code ?? "",
                FactionName = ht?.Faction?.Name ?? "",
                ClassCode = ht?.Class?.Code ?? "",
                ClassName = ht?.Class?.Name ?? "",
                RarityCode = ht?.Rarity?.Code ?? "",
                RarityName = ht?.Rarity?.Name ?? "",
                RarityColorHex = ht?.Rarity?.ColorHex,
                Level = ph.Level,
                Exp = ph.Exp,
                MaxExp = ph.MaxExp,
                Stars = ph.Stars,
                Power = ph.Power,
                AuraTier = ph.AuraTier,
                IsLocked = ph.IsLocked,
                IsFavorite = ph.IsFavorite,
                Stats = CalculateStats(ph),
                Skills = ht?.HeroSkills.OrderBy(hs => hs.SkillOrder).Select(hs => new SkillTemplateDto
                {
                    Id = hs.Skill.Id,
                    Name = hs.Skill.Name,
                    Icon = hs.Skill.Icon,
                    Description = hs.Skill.Description,
                    Cost = hs.Skill.Cost,
                    CostTypeCode = hs.Skill.CostType?.Code ?? "",
                    CostTypeName = hs.Skill.CostType?.Name ?? "",
                    CategoryCode = hs.Skill.Category?.Code ?? "",
                    CategoryName = hs.Skill.Category?.Name ?? "",
                    DamageTypeCode = hs.Skill.DamageType?.Code ?? "",
                    DamageTypeName = hs.Skill.DamageType?.Name ?? "",
                    EffectTypeCode = hs.Skill.EffectType?.Code ?? "",
                    EffectTypeName = hs.Skill.EffectType?.Name ?? "",
                    IsDebuff = hs.Skill.EffectType?.IsDebuff ?? false,
                    DamageMultiplier = hs.Skill.DamageMultiplier,
                    TargetType = hs.Skill.TargetType,
                    Cooldown = hs.Skill.Cooldown,
                    PhaseDurations = ParsePhaseDurations(hs.Skill.PhaseDurations)
                }).ToList() ?? new List<SkillTemplateDto>()
            };
        }

        private static InventoryItemDto? MapItem(HrkPlayerInventory? inv)
        {
            if (inv == null || inv.ItemTemplate == null) return null;
            var it = inv.ItemTemplate;
            return new InventoryItemDto
            {
                Id = inv.Id,
                ItemTemplateId = inv.ItemTemplateId,
                Name = it.Name,
                Icon = it.Icon,
                RarityId = it.RarityId,
                RarityCode = it.Rarity?.Code ?? "",
                RarityName = it.Rarity?.Name ?? "",
                RarityColorHex = it.Rarity?.ColorHex,
                CategoryId = it.CategoryId,
                CategoryCode = it.Category?.Code ?? "",
                CategoryName = it.Category?.Name ?? "",
                IsEquipment = it.Category?.IsEquipment ?? false,
                Count = inv.Count,
                LevelReq = it.LevelReq,
                Description = it.Description,
                Stats = ParseJson(inv.CurrentStats ?? it.BaseStats),
                IsLocked = inv.IsLocked,
                IsEquipped = inv.IsEquipped,
                EquippedHeroId = inv.EquippedHeroId,
                Enhancement = inv.Enhancement,
                Stars = inv.Stars,
                SlotIndex = inv.SlotIndex
            };
        }

        private static CalculatedStatsDto CalculateStats(HrkPlayerHero ph)
        {
            if (!string.IsNullOrWhiteSpace(ph.CurrentStats))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<CalculatedStatsDto>(ph.CurrentStats, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (parsed != null) return parsed;
                }
                catch { }
            }

            var ht = ph.HeroTemplate;
            if (ht == null) return new CalculatedStatsDto();

            decimal levelMultiplier = 1.0m + (ph.Level - 1) * 0.05m;
            decimal starMultiplier = 1.0m + (ph.Stars - 1) * 0.15m;
            decimal totalMultiplier = levelMultiplier * starMultiplier;

            return new CalculatedStatsDto
            {
                Hp = (int)(ht.BaseHp * totalMultiplier),
                Atk = (int)(ht.BaseAtk * totalMultiplier),
                Def = (int)(ht.BaseDef * totalMultiplier),
                Spd = (int)(ht.BaseSpd * (1.0m + (ph.Level - 1) * 0.01m)),
                Crit = ht.BaseCrit,
                CritDmg = ht.BaseCritDmg,
                Lifesteal = ht.BaseLifesteal,
                Accuracy = ht.BaseAccuracy,
                Resistance = ht.BaseResistance
            };
        }

        private static PhaseDurationDto? ParsePhaseDurations(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonSerializer.Deserialize<PhaseDurationDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { return null; }
        }

        private static object? ParseJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return JsonSerializer.Deserialize<object>(json); }
            catch { return json; }
        }
    }
}

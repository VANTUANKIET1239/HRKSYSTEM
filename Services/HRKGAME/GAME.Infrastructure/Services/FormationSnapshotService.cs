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
    public class FormationSnapshotService : IFormationSnapshotService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHeroStatCalculationService _heroStatCalculationService;
        private readonly IFormationStatService _formationStatService;
        private readonly ICombatPowerService _combatPowerService;

        public FormationSnapshotService(
            IUnitOfWork unitOfWork,
            IHeroStatCalculationService heroStatCalculationService,
            IFormationStatService formationStatService,
            ICombatPowerService combatPowerService)
        {
            _unitOfWork = unitOfWork;
            _heroStatCalculationService = heroStatCalculationService;
            _formationStatService = formationStatService;
            _combatPowerService = combatPowerService;
        }

        public async Task<FormationBattleSnapshot> BuildAsync(
            long playerId,
            string? formationCode,
            IReadOnlyCollection<FormationPositionRequestDto>? positions,
            CancellationToken cancellationToken = default)
        {
            // 1. Resolve Formation Template & Player Formation
            HrkPlayerFormation? playerFormation = null;

            if (string.IsNullOrWhiteSpace(formationCode))
            {
                playerFormation = await _unitOfWork.ReadOnlyRepository<HrkPlayerFormation>().Query()
                    .Include(f => f.FormationTemplate)
                    .Where(f => f.PlayerId == playerId && f.IsActive && f.IsSelected)
                    .FirstOrDefaultAsync(cancellationToken);

                formationCode = playerFormation?.FormationTemplate?.Code ?? "LUC_DO";
            }
            else
            {
                playerFormation = await _unitOfWork.ReadOnlyRepository<HrkPlayerFormation>().Query()
                    .Include(f => f.FormationTemplate)
                    .Where(f => f.PlayerId == playerId && f.IsActive && f.FormationTemplate.Code == formationCode)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            var tmpl = await _unitOfWork.ReadOnlyRepository<HrkFormationTemplate>().Query()
                .Where(t => t.Code == formationCode && t.IsEnabled)
                .Include(t => t.Slots.OrderBy(s => s.Slot))
                .Include(t => t.LevelConfigs.OrderBy(c => c.Level))
                .AsSplitQuery()
                .FirstOrDefaultAsync(cancellationToken);

            if (tmpl == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy cấu hình trận pháp '{formationCode}'.");
            }

            int level = playerFormation?.Level ?? 1;
            var levelCfg = tmpl.LevelConfigs.FirstOrDefault(c => c.Level == level)
                           ?? tmpl.LevelConfigs.FirstOrDefault();
            var formationBonus = _formationStatService.ParseBonus(levelCfg?.StatBonusJson);

            // 2. Resolve Positions
            var resolvedPositions = new List<FormationPositionRequestDto>();
            if (positions != null && positions.Count > 0)
            {
                resolvedPositions = positions.ToList();
            }
            else if (playerFormation != null)
            {
                resolvedPositions = new List<FormationPositionRequestDto>
                {
                    new() { Slot = 1, HeroId = playerFormation.Position1 },
                    new() { Slot = 2, HeroId = playerFormation.Position2 },
                    new() { Slot = 3, HeroId = playerFormation.Position3 },
                    new() { Slot = 4, HeroId = playerFormation.Position4 },
                    new() { Slot = 5, HeroId = playerFormation.Position5 },
                };
            }
            else
            {
                // Fallback default: first 5 slots empty
                for (int i = 1; i <= 5; i++)
                {
                    resolvedPositions.Add(new FormationPositionRequestDto { Slot = i, HeroId = null });
                }
            }

            // 3. Validate Positions
            if (resolvedPositions.Any(p => p.Slot < 1 || p.Slot > 5))
            {
                throw new ArgumentException("Vị trí ô chiến thuật chỉ hợp lệ từ 1 đến 5.");
            }

            var assignedHeroIds = resolvedPositions
                .Where(p => p.HeroId.HasValue && p.HeroId.Value > 0)
                .Select(p => p.HeroId!.Value)
                .ToList();

            if (assignedHeroIds.GroupBy(id => id).Any(g => g.Count() > 1))
            {
                throw new InvalidOperationException("Một võ tướng không thể xếp vào nhiều ô trong cùng một đội hình.");
            }

            // 4. Load Active Heroes & Equipments with instance attributes
            var (heroes, equipments) = await LoadHeroesAndEquipmentsAsync(playerId, assignedHeroIds, cancellationToken);

            if (assignedHeroIds.Count > 0 && heroes.Count != assignedHeroIds.Count)
            {
                throw new InvalidOperationException("Võ tướng được chọn không thuộc tài khoản người chơi hoặc không còn hoạt động.");
            }

            // 5. Calculate Hero Base Stats, Formation Bonus Stats, and Combat Power
            var powerConfigs = await _combatPowerService.GetConfigsAsync(cancellationToken);
            var heroEquipmentList = assignedHeroIds
                .Where(id => heroes.ContainsKey(id))
                .Select(id => (heroes[id], equipments.GetValueOrDefault(id)))
                .ToList();

            var powerResult = _formationStatService.CalculateFormationPower(heroEquipmentList, formationBonus, powerConfigs);

            // 6. Build Slot DTOs & Heroes List
            var slotDtos = new List<FormationSlotDto>();
            var heroesList = new List<PlayerHeroDto>();
            var participantIds = new List<long>();

            for (int slotNum = 1; slotNum <= 5; slotNum++)
            {
                var slotTmpl = tmpl.Slots.FirstOrDefault(s => s.Slot == slotNum);
                var posReq = resolvedPositions.FirstOrDefault(p => p.Slot == slotNum);
                long? heroId = posReq?.HeroId;
                PlayerHeroDto? heroDto = null;

                if (heroId.HasValue && heroes.TryGetValue(heroId.Value, out var heroEntity))
                {
                    heroDto = GameDtoMapper.MapPlayerHero(heroEntity);
                    if (heroDto != null)
                    {
                        heroDto.Position = slotNum;
                        heroDto.Stats = powerResult.HeroFormationStats.GetValueOrDefault(heroId.Value)
                                       ?? _formationStatService.ApplyFormationBonus(
                                           _heroStatCalculationService.CalculateStats(heroEntity, equipments.GetValueOrDefault(heroId.Value)).FinalStats,
                                           formationBonus);
                        heroDto.Power = powerResult.HeroTotalPowers.GetValueOrDefault(heroId.Value);

                        heroesList.Add(heroDto);
                        participantIds.Add(heroId.Value);
                    }
                }

                slotDtos.Add(new FormationSlotDto
                {
                    Slot = slotNum,
                    RowType = slotTmpl?.RowType ?? (slotNum % 2 == 1 ? "FRONT" : "BACK"),
                    Lane = slotTmpl?.Lane ?? slotNum,
                    DisplayX = slotTmpl?.DisplayX ?? slotNum,
                    DisplayY = slotTmpl?.DisplayY ?? (slotNum % 2 == 1 ? 2 : 1),
                    Hero = heroDto
                });
            }

            return new FormationBattleSnapshot
            {
                FormationId = playerFormation?.Id ?? 0,
                FormationCode = tmpl.Code,
                FormationName = tmpl.Name,
                FormationLevel = level,
                FormationBonus = formationBonus,
                Slots = slotDtos,
                Heroes = heroesList,
                ParticipantHeroIds = participantIds,
                FinalStatsByHero = powerResult.HeroFormationStats,
                HeroPowers = powerResult.HeroTotalPowers,
                BaseHeroPower = powerResult.BaseHeroPower,
                FormationBonusPower = powerResult.FormationBonusPower,
                TotalPower = powerResult.TotalPower
            };
        }

        public async Task<FormationPreviewResponseDto> PreviewAsync(
            long playerId,
            string formationCode,
            IReadOnlyCollection<FormationPositionRequestDto> positions,
            CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            if (positions == null || positions.Count == 0)
            {
                errors.Add("Danh sách vị trí ô chiến thuật không được để trống.");
            }
            else
            {
                if (positions.Any(p => p.Slot < 1 || p.Slot > 5))
                    errors.Add("Vị trí ô chiến thuật chỉ hợp lệ từ 1 đến 5.");

                if (positions.GroupBy(p => p.Slot).Any(g => g.Count() > 1))
                    errors.Add("Không được gửi trùng lặp số thứ tự ô chiến thuật.");

                var heroIds = positions
                    .Where(p => p.HeroId.HasValue && p.HeroId.Value > 0)
                    .Select(p => p.HeroId!.Value)
                    .ToList();

                if (heroIds.GroupBy(id => id).Any(g => g.Count() > 1))
                    errors.Add("Một võ tướng không thể xếp vào nhiều ô trong cùng một trận pháp.");
            }

            if (errors.Count > 0)
            {
                return new FormationPreviewResponseDto
                {
                    FormationCode = formationCode,
                    FormationName = formationCode,
                    IsValid = false,
                    ValidationErrors = errors
                };
            }

            try
            {
                var snapshot = await BuildAsync(playerId, formationCode, positions, cancellationToken);
                return new FormationPreviewResponseDto
                {
                    FormationCode = snapshot.FormationCode,
                    FormationName = snapshot.FormationName,
                    FormationLevel = snapshot.FormationLevel,
                    CurrentBonus = snapshot.FormationBonus,
                    Slots = snapshot.Slots,
                    BaseHeroPower = snapshot.BaseHeroPower,
                    FormationBonusPower = snapshot.FormationBonusPower,
                    TotalPower = snapshot.TotalPower,
                    IsValid = true,
                    ValidationErrors = new List<string>()
                };
            }
            catch (Exception ex)
            {
                return new FormationPreviewResponseDto
                {
                    FormationCode = formationCode,
                    FormationName = formationCode,
                    IsValid = false,
                    ValidationErrors = new List<string> { ex.Message }
                };
            }
        }

        private async Task<(Dictionary<long, HrkPlayerHero> Heroes, Dictionary<long, HrkPlayerEquipment> Equipments)> LoadHeroesAndEquipmentsAsync(
            long playerId, List<long> heroIds, CancellationToken cancellationToken)
        {
            if (heroIds.Count == 0)
                return (new Dictionary<long, HrkPlayerHero>(), new Dictionary<long, HrkPlayerEquipment>());

            var heroes = await _unitOfWork.ReadOnlyRepository<HrkPlayerHero>().Query()
                .Where(h => h.PlayerId == playerId && h.IsActive && heroIds.Contains(h.Id))
                .Include(h => h.HeroTemplate).ThenInclude(ht => ht.Faction)
                .Include(h => h.HeroTemplate).ThenInclude(ht => ht.Class)
                .Include(h => h.HeroTemplate).ThenInclude(ht => ht.Rarity)
                .Include(h => h.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.EffectType)
                .Include(h => h.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.TargetType)
                .Include(h => h.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.Parameters)
                .Include(h => h.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.Scalings).ThenInclude(sc => sc.AttributeType)
                .Include(h => h.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.StatModifiers).ThenInclude(sm => sm.AttributeType)
                .AsSplitQuery()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var equipments = await _unitOfWork.ReadOnlyRepository<HrkPlayerEquipment>().Query()
                .Where(e => e.PlayerId == playerId && heroIds.Contains(e.HeroId))
                .Include(e => e.Weapon).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Rarity)
                .Include(e => e.Weapon).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
                .Include(e => e.Weapon).ThenInclude(x => x!.Attributes).ThenInclude(x => x.AttributeType)
                .Include(e => e.Weapon).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Attributes).ThenInclude(x => x.AttributeType)

                .Include(e => e.Armor).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Rarity)
                .Include(e => e.Armor).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
                .Include(e => e.Armor).ThenInclude(x => x!.Attributes).ThenInclude(x => x.AttributeType)
                .Include(e => e.Armor).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Attributes).ThenInclude(x => x.AttributeType)

                .Include(e => e.Helmet).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Rarity)
                .Include(e => e.Helmet).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
                .Include(e => e.Helmet).ThenInclude(x => x!.Attributes).ThenInclude(x => x.AttributeType)
                .Include(e => e.Helmet).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Attributes).ThenInclude(x => x.AttributeType)

                .Include(e => e.Boots).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Rarity)
                .Include(e => e.Boots).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
                .Include(e => e.Boots).ThenInclude(x => x!.Attributes).ThenInclude(x => x.AttributeType)
                .Include(e => e.Boots).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Attributes).ThenInclude(x => x.AttributeType)

                .Include(e => e.Ring).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Rarity)
                .Include(e => e.Ring).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
                .Include(e => e.Ring).ThenInclude(x => x!.Attributes).ThenInclude(x => x.AttributeType)
                .Include(e => e.Ring).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Attributes).ThenInclude(x => x.AttributeType)

                .Include(e => e.Artifact).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Rarity)
                .Include(e => e.Artifact).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
                .Include(e => e.Artifact).ThenInclude(x => x!.Attributes).ThenInclude(x => x.AttributeType)
                .Include(e => e.Artifact).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Attributes).ThenInclude(x => x.AttributeType)
                .AsSplitQuery()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return (heroes.ToDictionary(h => h.Id), equipments.ToDictionary(e => e.HeroId));
        }
    }
}

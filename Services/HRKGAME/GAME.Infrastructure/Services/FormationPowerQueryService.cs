using Core.Common.Repositories;
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
    public class FormationPowerQueryService : IFormationPowerQueryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHeroStatCalculationService _heroStatCalculationService;
        private readonly IFormationStatService _formationStatService;
        private readonly ICombatPowerService _combatPowerService;

        public FormationPowerQueryService(
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

        public async Task<FormationPowerResultDto> GetDefaultFormationPowerAsync(long playerId, CancellationToken cancellationToken = default)
        {
            var selectedFormation = await _unitOfWork.ReadOnlyRepository<HrkPlayerFormation>().Query()
                .Include(f => f.FormationTemplate)
                .Where(f => f.PlayerId == playerId && f.IsActive && f.IsSelected)
                .FirstOrDefaultAsync(cancellationToken);

            if (selectedFormation == null)
            {
                return new FormationPowerResultDto();
            }

            var positions = new List<FormationPositionRequestDto>
            {
                new() { Slot = 1, HeroId = selectedFormation.Position1 },
                new() { Slot = 2, HeroId = selectedFormation.Position2 },
                new() { Slot = 3, HeroId = selectedFormation.Position3 },
                new() { Slot = 4, HeroId = selectedFormation.Position4 },
                new() { Slot = 5, HeroId = selectedFormation.Position5 },
            };

            return await CalculateFormationPowerAsync(playerId, selectedFormation.FormationTemplate.Code, positions, cancellationToken);
        }

        public async Task<FormationPowerResultDto> CalculateFormationPowerAsync(
            long playerId,
            string? formationCode,
            IReadOnlyCollection<FormationPositionRequestDto>? positions,
            CancellationToken cancellationToken = default)
        {
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
                .Include(t => t.LevelConfigs)
                .FirstOrDefaultAsync(cancellationToken);

            if (tmpl == null)
            {
                return new FormationPowerResultDto();
            }

            int level = playerFormation?.Level ?? 1;
            var levelCfg = tmpl.LevelConfigs.FirstOrDefault(c => c.Level == level)
                           ?? tmpl.LevelConfigs.FirstOrDefault();
            var formationBonus = _formationStatService.ParseBonus(levelCfg?.StatBonusJson);

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

            var placedHeroIds = resolvedPositions
                .Where(p => p.HeroId.HasValue && p.HeroId.Value > 0)
                .Select(p => p.HeroId!.Value)
                .Distinct()
                .ToList();

            if (placedHeroIds.Count == 0)
            {
                return new FormationPowerResultDto
                {
                    FormationId = playerFormation?.Id ?? 0,
                    FormationCode = tmpl.Code,
                    FormationName = tmpl.Name,
                    FormationLevel = level,
                    HeroCount = 0,
                    BaseHeroPower = 0,
                    FormationBonusPower = 0,
                    TotalPower = 0
                };
            }

            // High performance query: Only load Hero + HeroTemplate and equipment with attributes (no skills/effects/parameters)
            var heroes = await _unitOfWork.ReadOnlyRepository<HrkPlayerHero>().Query()
                .Where(h => h.PlayerId == playerId && h.IsActive && placedHeroIds.Contains(h.Id))
                .Include(h => h.HeroTemplate)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var heroDict = heroes.ToDictionary(h => h.Id);

            var equipments = await _unitOfWork.ReadOnlyRepository<HrkPlayerEquipment>().Query()
                .Where(e => e.PlayerId == playerId && placedHeroIds.Contains(e.HeroId))
                .Include(e => e.Weapon).ThenInclude(x => x!.Attributes).ThenInclude(x => x.AttributeType)
                .Include(e => e.Weapon).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
                .Include(e => e.Armor).ThenInclude(x => x!.Attributes).ThenInclude(x => x.AttributeType)
                .Include(e => e.Armor).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
                .Include(e => e.Helmet).ThenInclude(x => x!.Attributes).ThenInclude(x => x.AttributeType)
                .Include(e => e.Helmet).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
                .Include(e => e.Boots).ThenInclude(x => x!.Attributes).ThenInclude(x => x.AttributeType)
                .Include(e => e.Boots).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
                .Include(e => e.Ring).ThenInclude(x => x!.Attributes).ThenInclude(x => x.AttributeType)
                .Include(e => e.Ring).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
                .Include(e => e.Artifact).ThenInclude(x => x!.Attributes).ThenInclude(x => x.AttributeType)
                .Include(e => e.Artifact).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
                .AsSplitQuery()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var eqDict = equipments.ToDictionary(e => e.HeroId);

            var powerConfigs = await _combatPowerService.GetConfigsAsync(cancellationToken);

            var heroEquipmentList = placedHeroIds
                .Where(id => heroDict.ContainsKey(id))
                .Select(id => (heroDict[id], eqDict.GetValueOrDefault(id)))
                .ToList();

            var powerResult = _formationStatService.CalculateFormationPower(heroEquipmentList, formationBonus, powerConfigs);

            return new FormationPowerResultDto
            {
                FormationId = playerFormation?.Id ?? 0,
                FormationCode = tmpl.Code,
                FormationName = tmpl.Name,
                FormationLevel = level,
                HeroCount = heroEquipmentList.Count,
                BaseHeroPower = powerResult.BaseHeroPower,
                FormationBonusPower = powerResult.FormationBonusPower,
                TotalPower = powerResult.TotalPower,
                PlacedHeroIds = placedHeroIds,
                HeroTotalPowers = powerResult.HeroTotalPowers
            };
        }
    }
}

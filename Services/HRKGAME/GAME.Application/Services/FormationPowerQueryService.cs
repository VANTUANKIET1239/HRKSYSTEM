using GAME.Application.Templates;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Services
{
    public class FormationPowerQueryService : IFormationPowerQueryService
    {
        private readonly IGameTemplateCatalog _templates;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHeroStatCalculationService _heroStatCalculationService;
        private readonly IFormationStatService _formationStatService;
        private readonly ICombatPowerService _combatPowerService;
        public FormationPowerQueryService(IUnitOfWork unitOfWork, IHeroStatCalculationService heroStatCalculationService, IFormationStatService formationStatService, ICombatPowerService combatPowerService, IGameTemplateCatalog? templates = null)
        {
            _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);
            _unitOfWork = unitOfWork;
            _heroStatCalculationService = heroStatCalculationService;
            _formationStatService = formationStatService;
            _combatPowerService = combatPowerService;
        }

        public async Task<FormationPowerResultDto> GetDefaultFormationPowerAsync(long playerId, CancellationToken cancellationToken = default)
        {
            var selectedFormation = await TemplateHydration.FindPlayerFormationAsync(_unitOfWork, _templates, playerId, null, cancellationToken);
            if (selectedFormation == null)
            {
                return new FormationPowerResultDto();
            }

            var positions = new List<FormationPositionRequestDto>
            {
                new()
                {
                    Slot = 1,
                    HeroId = selectedFormation.Position1
                },
                new()
                {
                    Slot = 2,
                    HeroId = selectedFormation.Position2
                },
                new()
                {
                    Slot = 3,
                    HeroId = selectedFormation.Position3
                },
                new()
                {
                    Slot = 4,
                    HeroId = selectedFormation.Position4
                },
                new()
                {
                    Slot = 5,
                    HeroId = selectedFormation.Position5
                },
            };
            return await CalculateFormationPowerAsync(playerId, selectedFormation.FormationTemplate.Code, positions, cancellationToken);
        }

        public async Task<FormationPowerResultDto> CalculateFormationPowerAsync(long playerId, string? formationCode, IReadOnlyCollection<FormationPositionRequestDto>? positions, CancellationToken cancellationToken = default)
        {
            var playerFormation = await TemplateHydration.FindPlayerFormationAsync(_unitOfWork, _templates, playerId, formationCode, cancellationToken);
            formationCode = string.IsNullOrWhiteSpace(formationCode) ? playerFormation?.FormationTemplate?.Code ?? "LUC_DO" : formationCode;

            var tmpl = (await _templates.GetAllAsync<HrkFormationTemplate>(cancellationToken))
                .Where(t => t.Code == formationCode && t.IsEnabled)
                .FirstOrDefault();
            if (tmpl == null)
            {
                return new FormationPowerResultDto();
            }

            int level = playerFormation?.Level ?? 1;
            var levelCfg = tmpl.LevelConfigs.FirstOrDefault(c => c.Level == level) ?? tmpl.LevelConfigs.OrderBy(c => c.Level).FirstOrDefault();
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
                    new()
                    {
                        Slot = 1,
                        HeroId = playerFormation.Position1
                    },
                    new()
                    {
                        Slot = 2,
                        HeroId = playerFormation.Position2
                    },
                    new()
                    {
                        Slot = 3,
                        HeroId = playerFormation.Position3
                    },
                    new()
                    {
                        Slot = 4,
                        HeroId = playerFormation.Position4
                    },
                    new()
                    {
                        Slot = 5,
                        HeroId = playerFormation.Position5
                    },
                };
            }

            var placedHeroIds = resolvedPositions.Where(p => p.HeroId.HasValue && p.HeroId.Value > 0)
                .Select(p => p.HeroId!.Value).Distinct().ToList();
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
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            await TemplateHydration.HeroesAsync(_templates, heroes, cancellationToken);
            var heroDict = heroes.ToDictionary(h => h.Id);
            var eqDict = await EquipmentBatchLoader.LoadForHeroesAsync(_unitOfWork, playerId, placedHeroIds, cancellationToken, _templates);
            var powerConfigs = await _combatPowerService.GetConfigsAsync(cancellationToken);
            var heroEquipmentList = placedHeroIds.Where(id => heroDict.ContainsKey(id))
                .Select(id => (heroDict[id], eqDict.GetValueOrDefault(id))).ToList();
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

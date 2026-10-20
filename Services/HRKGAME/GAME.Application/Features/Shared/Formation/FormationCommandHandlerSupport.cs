using GAME.Application.Templates;
using Core.Persistence.Repositories;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Features.Shared.Formation;

public abstract class FormationCommandHandlerSupport(
    IUnitOfWork unitOfWork,
    IGamePlayerService players,
    IFormationStatService formationStats,
    ICombatPowerService power,
    IHeroStatCalculationService heroStats, IGameTemplateCatalog? templates = null)
    : FormationHandlerSupport(unitOfWork, players, formationStats, power, heroStats, templates)
{
    protected async Task EnsurePlayerFormationsInitializedAsync(HrkPlayer player, CancellationToken cancellationToken)
    {
        var existingPfs = await _unitOfWork.Repository<HrkPlayerFormation>().Query()
            .Where(f => f.PlayerId == player.Id && f.IsActive)
            .ToListAsync(cancellationToken);
        var templates = (await _templates.GetAllAsync<HrkFormationTemplate>(cancellationToken))
            .Where(t => t.IsEnabled)
            .OrderBy(t => t.DisplayOrder)
            .ToList();
        bool hasChanges = false;
        bool anySelected = existingPfs.Any(f => f.IsSelected);
        // Tìm hero của player để gán mặc định nếu formation đầu tiên hoàn toàn rỗng
        var activeHeroes = await _unitOfWork.ReadOnlyRepository<HrkPlayerHero>().Query()
            .Where(h => h.PlayerId == player.Id && h.IsActive)
            .OrderByDescending(h => h.Level).ThenBy(h => h.Id).Take(5)
            .ToListAsync(cancellationToken);
        foreach (var tmpl in templates)
        {
            var pf = existingPfs.FirstOrDefault(f => f.FormationTemplateId == tmpl.Id);
            if (pf == null)
            {
                bool shouldSelect = !anySelected && (tmpl.Code == "LUC_DO" || existingPfs.Count == 0);
                if (shouldSelect)
                    anySelected = true;
                var newPf = new HrkPlayerFormation
                {
                    PlayerId = player.Id,
                    FormationTemplateId = tmpl.Id,
                    Level = 1,
                    FormationName = tmpl.Name,
                    IsSelected = shouldSelect,
                    IsActive = true,
                    UpdatedOn = DateTime.UtcNow
                };
                // Nếu là LUC_DO và chưa có tướng nào được xếp, khởi tạo 5 tướng mẫu nếu có
                if (tmpl.Code == "LUC_DO" && activeHeroes.Count > 0)
                {
                    if (activeHeroes.Count > 0)
                        newPf.Position1 = activeHeroes[0].Id;
                    if (activeHeroes.Count > 1)
                        newPf.Position2 = activeHeroes[1].Id;
                    if (activeHeroes.Count > 2)
                        newPf.Position3 = activeHeroes[2].Id;
                    if (activeHeroes.Count > 3)
                        newPf.Position4 = activeHeroes[3].Id;
                    if (activeHeroes.Count > 4)
                        newPf.Position5 = activeHeroes[4].Id;
                }

                await _unitOfWork.Repository<HrkPlayerFormation>().AddAsync(newPf);
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

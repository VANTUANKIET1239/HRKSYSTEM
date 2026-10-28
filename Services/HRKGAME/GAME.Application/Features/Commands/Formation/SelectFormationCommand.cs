using GAME.Application.Templates;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.Formation;

namespace GAME.Application.Features.Commands.Formation;

public sealed record SelectFormationCommand(string UserId, string FormationCode) : ICommand<BaseResponse<FormationDetailDto>>;

public sealed class SelectFormationCommandHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IFormationStatService formationStatService,
    ICombatPowerService combatPowerService,
    IHeroStatCalculationService heroStatCalculationService, IGameTemplateCatalog? templates = null)
    : FormationCommandHandlerSupport(unitOfWork, gamePlayerService, formationStatService, combatPowerService, heroStatCalculationService, templates), ICommandHandler<SelectFormationCommand, BaseResponse<FormationDetailDto>>
{
    public async Task<BaseResponse<FormationDetailDto>> Handle(SelectFormationCommand message, CancellationToken ct) => BaseResponse<FormationDetailDto>.SuccessResponse(await ExecuteAsync(message.UserId, message.FormationCode, ct));
    private async Task<FormationDetailDto> ExecuteAsync(string userId, string formationCode, CancellationToken cancellationToken = default)
    {
        var player = await GetPlayerRequiredAsync(userId, cancellationToken);
        await EnsurePlayerFormationsInitializedAsync(player, cancellationToken);
        var tmpl = (await _templates.GetAllAsync<HrkFormationTemplate>(cancellationToken))
            .FirstOrDefault(t => t.Code == formationCode && t.IsEnabled) ?? throw BusinessException.Localized("FORMATION_NOT_FOUND", $"Không tìm thấy trận pháp '{formationCode}'.", BusinessErrorKind.NotFound);
        var allPfs = await _unitOfWork.Repository<HrkPlayerFormation>().Query()
            .Where(f => f.PlayerId == player.Id && f.IsActive)
            .ToListAsync(cancellationToken);
        var targetPf = allPfs.FirstOrDefault(f => f.FormationTemplateId == tmpl.Id) ?? throw BusinessException.Localized("FEATURE_LOCKED", $"Người chơi chưa mở khóa trận pháp '{formationCode}'.", BusinessErrorKind.NotFound);
        // Kiểm tra trận pháp có ít nhất 1 hero
        if (targetPf.Position1 == null && targetPf.Position2 == null && targetPf.Position3 == null && targetPf.Position4 == null && targetPf.Position5 == null)
        {
            throw new BusinessException("EMPTY_FORMATION", "Không thể chọn sử dụng trận pháp khi chưa xếp bất kỳ võ tướng nào.", BusinessErrorKind.Conflict);
        }

        // Đổi trạng thái IsSelected
        foreach (var pf in allPfs)
        {
            pf.IsSelected = (pf.Id == targetPf.Id);
            pf.UpdatedOn = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (await GetFormationDetailAsync(userId, formationCode, cancellationToken))!;
    }
}

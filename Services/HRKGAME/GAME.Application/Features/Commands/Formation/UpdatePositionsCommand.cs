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

public sealed record UpdatePositionsCommand(

    string UserId,

    string FormationCode,

    UpdateFormationPositionsRequest Request

)

    : ICommand<BaseResponse<FormationDetailDto>>;

public sealed class UpdatePositionsCommandHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IFormationStatService formationStatService,
    ICombatPowerService combatPowerService,
    IHeroStatCalculationService heroStatCalculationService)
    : FormationCommandHandlerSupport(unitOfWork, gamePlayerService, formationStatService, combatPowerService, heroStatCalculationService), ICommandHandler<UpdatePositionsCommand, BaseResponse<FormationDetailDto>>
{
    public async Task<BaseResponse<FormationDetailDto>> Handle(UpdatePositionsCommand message, CancellationToken ct) => BaseResponse<FormationDetailDto>.SuccessResponse(await ExecuteAsync(message.UserId, message.FormationCode, message.Request, ct));
    private async Task<FormationDetailDto> ExecuteAsync(string userId, string formationCode, UpdateFormationPositionsRequest request, CancellationToken cancellationToken = default)
    {
        var player = await GetPlayerRequiredAsync(userId, cancellationToken);
        await EnsurePlayerFormationsInitializedAsync(player, cancellationToken);
        var tmpl = await _unitOfWork.ReadOnlyRepository<HrkFormationTemplate>().Query()
            .FirstOrDefaultAsync(t => t.Code == formationCode && t.IsEnabled, cancellationToken) ?? throw new BusinessException("FORMATION_NOT_FOUND", $"Không tìm thấy trận pháp '{formationCode}'.", BusinessErrorKind.NotFound);
        var pf = await _unitOfWork.Repository<HrkPlayerFormation>().Query()
            .FirstOrDefaultAsync(f => f.PlayerId == player.Id && f.FormationTemplateId == tmpl.Id && f.IsActive, cancellationToken) ?? throw new BusinessException("FORMATION_NOT_FOUND", $"Không tìm thấy bản ghi đội hình cho trận pháp '{formationCode}'.", BusinessErrorKind.NotFound);
        // Validate slots 1 to 5
        var positions = request?.Positions ?? new List<SlotHeroPositionDto>();
        if (positions.Any(p => p.Slot < 1 || p.Slot > 5))
            throw new BusinessException("INVALID_REQUEST", "Vị trí ô chiến thuật chỉ hợp lệ từ 1 đến 5.", BusinessErrorKind.Validation);
        if (positions.GroupBy(p => p.Slot).Any(g => g.Count() > 1))
            throw new BusinessException("INVALID_REQUEST", "Không được gửi trùng lặp số thứ tự ô chiến thuật.", BusinessErrorKind.Validation);
        var assignedHeroIds = positions.Where(p => p.HeroId.HasValue && p.HeroId.Value > 0)
            .Select(p => p.HeroId!.Value).ToList();
        if (assignedHeroIds.GroupBy(id => id).Any(g => g.Count() > 1))
            throw new BusinessException("DUPLICATE_HERO", "Một võ tướng không thể xếp vào nhiều ô trong cùng một trận pháp.", BusinessErrorKind.Conflict);
        // Validate hero ownership and active status
        if (assignedHeroIds.Count > 0)
        {
            var ownedCount = await _unitOfWork.ReadOnlyRepository<HrkPlayerHero>().Query()
                .Where(h => h.PlayerId == player.Id && h.IsActive && assignedHeroIds.Contains(h.Id)).CountAsync(cancellationToken);
            if (ownedCount != assignedHeroIds.Count)
                throw new BusinessException("INVALID_OWNERSHIP", "Võ tướng được chọn không thuộc tài khoản người chơi hoặc không còn hoạt động.", BusinessErrorKind.Conflict);
        }

        // Gán vị trí
        pf.Position1 = positions.FirstOrDefault(p => p.Slot == 1)?.HeroId;
        pf.Position2 = positions.FirstOrDefault(p => p.Slot == 2)?.HeroId;
        pf.Position3 = positions.FirstOrDefault(p => p.Slot == 3)?.HeroId;
        pf.Position4 = positions.FirstOrDefault(p => p.Slot == 4)?.HeroId;
        pf.Position5 = positions.FirstOrDefault(p => p.Slot == 5)?.HeroId;
        pf.UpdatedOn = DateTime.UtcNow;
        // Tính toán lại power
        var currentLevelCfg = await _unitOfWork.ReadOnlyRepository<HrkFormationLevelConfig>().Query()
            .FirstOrDefaultAsync(c => c.FormationTemplateId == tmpl.Id && c.Level == pf.Level, cancellationToken);
        var currentBonus = _formationStatService.ParseBonus(currentLevelCfg?.StatBonusJson);
        var powerConfigs = await _combatPowerService.GetConfigsAsync(cancellationToken);
        var placedHeroIds = GetPlacedHeroIds(pf);
        var (heroes, equipments) = await LoadHeroesAndEquipmentsAsync(player.Id, placedHeroIds, cancellationToken);
        var heroEquipmentList = placedHeroIds.Where(id => heroes.ContainsKey(id))
            .Select(id => (heroes[id], equipments.GetValueOrDefault(id))).ToList();
        var powerResult = _formationStatService.CalculateFormationPower(heroEquipmentList, currentBonus, powerConfigs);
        pf.TotalPower = powerResult.TotalPower;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (await GetFormationDetailAsync(userId, formationCode, cancellationToken))!;
    }
}

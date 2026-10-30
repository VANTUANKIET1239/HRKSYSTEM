using GAME.Application.Common;
using GAME.Application.Templates;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Persistence.Repositories;
using Core.Application.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Features.Queries.Inventory.GetEquipmentDowngradePreview;
public class GetEquipmentDowngradePreviewQueryHandler : AuthenticatedGameHandler, IQueryHandler<GetEquipmentDowngradePreviewQuery, BaseResponse<EquipmentDowngradePreviewDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGameTemplateCatalog _templates;
    private readonly IGamePlayerService _gamePlayerService;
    private readonly IItemStatCalculationService _statCalculationService;
    public GetEquipmentDowngradePreviewQueryHandler(IUnitOfWork unitOfWork, IGamePlayerService gamePlayerService, IItemStatCalculationService statCalculationService, IGameRequestContext requestContext, IGameTemplateCatalog? templates = null) : base(requestContext)
    {
        _unitOfWork = unitOfWork;
        _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);
        _gamePlayerService = gamePlayerService;
        _statCalculationService = statCalculationService;
    }

    public async Task<BaseResponse<EquipmentDowngradePreviewDto>> Handle(GetEquipmentDowngradePreviewQuery query, CancellationToken cancellationToken)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(GetUserId(), cancellationToken);
        if (player == null)
        {
            return BaseResponse<EquipmentDowngradePreviewDto>.FailResponse("Không tìm thấy người chơi.", statusCode: 404);
        }

        var item = await _unitOfWork.ReadOnlyRepository<HrkPlayerInventory>().Query()
            .Include(x => x.Attributes)
            .FirstOrDefaultAsync(x => x.Id == query.InventoryItemId && x.PlayerId == player.Id && x.IsActive, cancellationToken);
        if (item == null)
        {
            return BaseResponse<EquipmentDowngradePreviewDto>.FailResponse("Không tìm thấy trang bị.", statusCode: 404);
        }

        await TemplateHydration.InventoryAsync(_templates, new[] { item }, cancellationToken);
        var preview = await DowngradeCalculator.BuildAsync(_unitOfWork, _statCalculationService, item, query.TargetLevel, cancellationToken, _templates);
        return BaseResponse<EquipmentDowngradePreviewDto>.SuccessResponse(preview);
    }
}

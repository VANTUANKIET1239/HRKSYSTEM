using Core.Common.Errors;
using GAME.Application.Templates;
using GAME.Application.Common;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Persistence.Repositories;
using Core.Application.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Features.Queries.Inventory.GetEquipmentDowngradePreview;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GAME.Application.Features.Commands.Inventory.DowngradeEquipment;
public class DowngradeEquipmentCommandHandler : AuthenticatedGameHandler, ICommandHandler<DowngradeEquipmentCommand, BaseResponse<EquipmentDowngradePreviewDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGameTemplateCatalog _templates;
    private readonly IGamePlayerService _gamePlayerService;
    private readonly IItemStatCalculationService _statCalculationService;
    public DowngradeEquipmentCommandHandler(IUnitOfWork unitOfWork, IGamePlayerService gamePlayerService, IItemStatCalculationService statCalculationService, IGameRequestContext requestContext, IGameTemplateCatalog? templates = null) : base(requestContext)
    {
        _unitOfWork = unitOfWork;
        _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);
        _gamePlayerService = gamePlayerService;
        _statCalculationService = statCalculationService;
    }

    public async Task<BaseResponse<EquipmentDowngradePreviewDto>> Handle(DowngradeEquipmentCommand command, CancellationToken cancellationToken)
    {
        if (command.Request.RequestId == Guid.Empty)
        {
            return BaseResponse<EquipmentDowngradePreviewDto>.FailResponse("RequestId không hợp lệ.", statusCode: 400);
        }

        var previous = await _unitOfWork.ReadOnlyRepository<HrkEquipmentDowngradeHistory>().Query()
            .FirstOrDefaultAsync(x => x.RequestId == command.Request.RequestId, cancellationToken);
        if (previous != null)
        {
            var cachedResult = new EquipmentDowngradePreviewDto
            {
                InventoryItemId = previous.PlayerInventoryId,
                CurrentEnhancement = previous.OldEnhancement,
                TargetEnhancement = previous.NewEnhancement,
                RefundedGold = previous.RefundedGold,
                RefundedStones = JsonSerializer.Deserialize<List<RefundedMaterialDto>>(previous.RefundedMaterialsJson) ?? new List<RefundedMaterialDto>(),
                CanDowngrade = true
            };
            return BaseResponse<EquipmentDowngradePreviewDto>.SuccessResponse(cachedResult, "Yêu cầu đã được xử lý trước đó.");
        }

        var player = await _gamePlayerService.GetPlayerByUserIdAsync(GetUserId(), cancellationToken);
        if (player == null)
        {
            return BaseResponse<EquipmentDowngradePreviewDto>.FailResponse("Không tìm thấy người chơi.", statusCode: 404);
        }

        EquipmentDowngradePreviewDto? result = null;
        {

            var item = await LoadOwnedEquipmentAsync(player.Id, command.Request.InventoryItemId, cancellationToken);
            var view = await TemplateHydration.InventoryViewAsync(_templates, item, cancellationToken);
            var preview = await DowngradeCalculator.BuildAsync(_unitOfWork, _statCalculationService, view, command.Request.TargetEnhancement, cancellationToken, _templates);
            if (!preview.CanDowngrade)
            {
                throw new BusinessException("UPGRADE_NOT_ALLOWED", preview.Message ?? "Không thể hạ cấp trang bị.", BusinessErrorKind.Conflict);
            }

            var wallet = await _unitOfWork.Repository<HrkPlayerWallet>().Query()
                .SingleAsync(x => x.PlayerId == player.Id, cancellationToken);
            wallet.AddGold(preview.RefundedGold);
            _unitOfWork.Repository<HrkPlayerWallet>().Update(wallet);
            foreach (var refund in preview.RefundedStones)
            {
                await AddRefundedMaterialAsync(player.Id, refund, cancellationToken);
            }

            item.Enhancement = command.Request.TargetEnhancement;
            item.UpdateCurrentStatsCache(_statCalculationService.CalculateCurrentStatsJson(view, command.Request.TargetEnhancement));
            _unitOfWork.Repository<HrkPlayerInventory>().Update(item);
            await _unitOfWork.Repository<HrkEquipmentDowngradeHistory>().AddAsync(new HrkEquipmentDowngradeHistory { RequestId = command.Request.RequestId, PlayerId = player.Id, PlayerInventoryId = item.Id, OldEnhancement = preview.CurrentEnhancement, NewEnhancement = preview.TargetEnhancement, RefundedGold = preview.RefundedGold, RefundedMaterialsJson = JsonSerializer.Serialize(preview.RefundedStones) });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            result = preview;

            return BaseResponse<EquipmentDowngradePreviewDto>.SuccessResponse(result!, "Hạ cấp trang bị thành công.");
        }
    }

    private async Task<HrkPlayerInventory> LoadOwnedEquipmentAsync(long playerId, long inventoryItemId, CancellationToken cancellationToken)
    {
        return await _unitOfWork.Repository<HrkPlayerInventory>().Query()
            .Include(x => x.Attributes)
            .FirstOrDefaultAsync(x => x.Id == inventoryItemId && x.PlayerId == playerId && x.IsActive, cancellationToken) ?? throw new BusinessException("ITEM_NOT_FOUND", "Không tìm thấy trang bị thuộc người chơi.", BusinessErrorKind.NotFound);
    }

    private async Task AddRefundedMaterialAsync(long playerId, RefundedMaterialDto refund, CancellationToken cancellationToken)
    {
        var inventoryRepository = _unitOfWork.Repository<HrkPlayerInventory>();
        var stack = await inventoryRepository.Query()
            .FirstOrDefaultAsync(x => x.PlayerId == playerId && x.ItemTemplateId == refund.ItemTemplateId && x.IsActive && !x.IsEquipped, cancellationToken);
        if (stack == null)
        {
            await inventoryRepository.AddAsync(new HrkPlayerInventory { PlayerId = playerId, ItemTemplateId = refund.ItemTemplateId, Count = refund.Quantity, IsActive = true });
            return;
        }

        stack.Count += refund.Quantity;
        stack.UpdatedOn = DateTime.UtcNow;
        inventoryRepository.Update(stack);
    }
}

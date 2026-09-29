using Core.Common.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Common.Repositories;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Features.Queries.Inventory.GetEquipmentDowngradePreview;

public record GetEquipmentDowngradePreviewQuery(
    long InventoryItemId,
    int TargetLevel)
    : IQuery<BaseResponse<EquipmentDowngradePreviewDto>>;

public class GetEquipmentDowngradePreviewQueryHandler
    : HRKBaseQuery,
      IQueryHandler<GetEquipmentDowngradePreviewQuery, BaseResponse<EquipmentDowngradePreviewDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGamePlayerService _gamePlayerService;
    private readonly IItemStatCalculationService _statCalculationService;

    public GetEquipmentDowngradePreviewQueryHandler(
        IUnitOfWork unitOfWork,
        IGamePlayerService gamePlayerService,
        IItemStatCalculationService statCalculationService,
        IHttpContextAccessor httpContextAccessor)
        : base(httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _gamePlayerService = gamePlayerService;
        _statCalculationService = statCalculationService;
    }

    public async Task<BaseResponse<EquipmentDowngradePreviewDto>> Handle(
        GetEquipmentDowngradePreviewQuery query,
        CancellationToken cancellationToken)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(
            GetUserId(), cancellationToken);
        if (player == null)
        {
            return BaseResponse<EquipmentDowngradePreviewDto>.FailResponse(
                "Không tìm thấy người chơi.", statusCode: 404);
        }

        var item = await _unitOfWork
            .ReadOnlyRepository<HrkPlayerInventory>()
            .Query()
            .Include(x => x.ItemTemplate)
                .ThenInclude(x => x.Category)
            .Include(x => x.ItemTemplate)
                .ThenInclude(x => x.Attributes)
                .ThenInclude(x => x.AttributeType)
            .Include(x => x.Attributes)
                .ThenInclude(x => x.AttributeType)
            .FirstOrDefaultAsync(
                x => x.Id == query.InventoryItemId &&
                     x.PlayerId == player.Id &&
                     x.IsActive,
                cancellationToken);

        if (item == null)
        {
            return BaseResponse<EquipmentDowngradePreviewDto>.FailResponse(
                "Không tìm thấy trang bị.", statusCode: 404);
        }

        var preview = await DowngradeCalculator.BuildAsync(
            _unitOfWork,
            _statCalculationService,
            item,
            query.TargetLevel,
            cancellationToken);

        return BaseResponse<EquipmentDowngradePreviewDto>.SuccessResponse(preview);
    }
}

internal static class DowngradeCalculator
{
    internal static async Task<EquipmentDowngradePreviewDto> BuildAsync(
        IUnitOfWork unitOfWork,
        IItemStatCalculationService statCalculationService,
        HrkPlayerInventory item,
        int targetLevel,
        CancellationToken cancellationToken)
    {
        var preview = new EquipmentDowngradePreviewDto
        {
            InventoryItemId = item.Id,
            CurrentEnhancement = item.Enhancement,
            TargetEnhancement = targetLevel,
            CurrentStats = statCalculationService.CalculateCurrentStats(item),
            NextStats = statCalculationService.CalculateCurrentStats(
                item, targetLevel),
            NonRefundedResources = new List<string>
            {
                "Bùa bảo hộ",
                "Bùa may mắn",
                "Tài nguyên của lần cường hóa thất bại",
                "Tài nguyên vượt chi phí chuẩn"
            }
        };

        if (item.ItemTemplate?.Category?.IsEquipment != true)
        {
            return Fail(preview, "NOT_EQUIPMENT", "Vật phẩm không phải trang bị.");
        }

        if (targetLevel < 0 || targetLevel >= item.Enhancement)
        {
            return Fail(
                preview,
                "INVALID_TARGET",
                "Cấp đích phải từ 0 và nhỏ hơn cấp hiện tại.");
        }

        var refundConfigs = await unitOfWork
            .ReadOnlyRepository<HrkEquipmentDowngradeConfig>()
            .Query()
            .Where(x => x.IsEnabled &&
                        x.FromLevel <= item.Enhancement &&
                        x.ToLevel >= targetLevel)
            .ToListAsync(cancellationToken);
        var levelConfigs = await unitOfWork
            .ReadOnlyRepository<HrkEnhancementLevelConfig>()
            .Query()
            .Where(x => x.CurrentLevel >= targetLevel &&
                        x.CurrentLevel < item.Enhancement)
            .ToListAsync(cancellationToken);

        if (levelConfigs.Count != item.Enhancement - targetLevel)
        {
            return Fail(
                preview,
                "MISSING_LEVEL_CONFIG",
                "Thiếu cấu hình chi phí cường hóa cho khoảng cấp đã chọn.");
        }

        decimal refundedGold = 0;
        decimal refundedStoneValue = 0;

        for (var level = targetLevel + 1; level <= item.Enhancement; level++)
        {
            var refundConfig = refundConfigs.FirstOrDefault(
                x => x.FromLevel == level && x.ToLevel == level - 1)
                ?? refundConfigs.FirstOrDefault(
                    x => x.FromLevel == item.Enhancement &&
                         x.ToLevel == targetLevel);

            if (refundConfig == null)
            {
                return Fail(
                    preview,
                    "MISSING_REFUND_CONFIG",
                    $"Thiếu cấu hình hoàn trả +{level} → +{level - 1}.");
            }

            var levelConfig = levelConfigs.Single(
                x => x.CurrentLevel == level - 1);
            refundedGold += levelConfig.GoldCost *
                            refundConfig.GoldRefundPercent;
            refundedStoneValue += levelConfig.GoldCost *
                                  refundConfig.StoneRefundPercent;
        }

        preview.RefundedGold = (long)Math.Floor(refundedGold);
        preview.RefundedStones = await ConvertStoneValueAsync(
            unitOfWork,
            refundedStoneValue,
            cancellationToken);
        preview.CanDowngrade = true;
        return preview;
    }

    private static async Task<List<RefundedMaterialDto>> ConvertStoneValueAsync(
        IUnitOfWork unitOfWork,
        decimal stoneValue,
        CancellationToken cancellationToken)
    {
        var stones = await unitOfWork
            .ReadOnlyRepository<HrkEnhancementMaterial>()
            .Query()
            .Include(x => x.ItemTemplate)
            .Where(x => x.MaterialType == "STONE" &&
                        x.ItemTemplate.SellPrice > 0)
            .OrderByDescending(x => x.ItemTemplate.SellPrice)
            .ToListAsync(cancellationToken);

        var result = new List<RefundedMaterialDto>();
        var remainingValue = (int)Math.Floor(stoneValue);

        foreach (var stone in stones)
        {
            var quantity = remainingValue / stone.ItemTemplate.SellPrice;
            if (quantity <= 0)
            {
                continue;
            }

            result.Add(new RefundedMaterialDto
            {
                ItemTemplateId = stone.ItemTemplateId,
                Code = stone.ItemTemplate.Code,
                Name = stone.ItemTemplate.Name,
                ImagePath = stone.ItemTemplate.ImagePath,
                Quantity = quantity
            });
            remainingValue -= quantity * stone.ItemTemplate.SellPrice;
        }

        return result;
    }

    private static EquipmentDowngradePreviewDto Fail(
        EquipmentDowngradePreviewDto preview,
        string reasonCode,
        string message)
    {
        preview.CanDowngrade = false;
        preview.ReasonCode = reasonCode;
        preview.Message = message;
        return preview;
    }
}

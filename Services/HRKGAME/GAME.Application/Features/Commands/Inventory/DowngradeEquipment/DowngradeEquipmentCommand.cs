using Core.Common.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Common.Repositories;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Features.Queries.Inventory.GetEquipmentDowngradePreview;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GAME.Application.Features.Commands.Inventory.DowngradeEquipment;

public record DowngradeEquipmentCommand(DowngradeEquipmentRequestDto Request)
    : ICommand<BaseResponse<EquipmentDowngradePreviewDto>>;

public class DowngradeEquipmentCommandHandler
    : HRKBaseCommand,
      ICommandHandler<DowngradeEquipmentCommand, BaseResponse<EquipmentDowngradePreviewDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGamePlayerService _gamePlayerService;
    private readonly IItemStatCalculationService _statCalculationService;

    public DowngradeEquipmentCommandHandler(
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
        DowngradeEquipmentCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Request.RequestId == Guid.Empty)
        {
            return BaseResponse<EquipmentDowngradePreviewDto>.FailResponse(
                "RequestId không hợp lệ.", statusCode: 400);
        }

        var previous = await _unitOfWork
            .ReadOnlyRepository<HrkEquipmentDowngradeHistory>()
            .Query()
            .FirstOrDefaultAsync(
                x => x.RequestId == command.Request.RequestId,
                cancellationToken);

        if (previous != null)
        {
            var cachedResult = new EquipmentDowngradePreviewDto
            {
                InventoryItemId = previous.PlayerInventoryId,
                CurrentEnhancement = previous.OldEnhancement,
                TargetEnhancement = previous.NewEnhancement,
                RefundedGold = previous.RefundedGold,
                RefundedStones = JsonSerializer.Deserialize<List<RefundedMaterialDto>>(
                    previous.RefundedMaterialsJson) ?? new List<RefundedMaterialDto>(),
                CanDowngrade = true
            };

            return BaseResponse<EquipmentDowngradePreviewDto>.SuccessResponse(
                cachedResult, "Yêu cầu đã được xử lý trước đó.");
        }

        var player = await _gamePlayerService.GetPlayerByUserIdAsync(
            GetUserId(), cancellationToken);

        if (player == null)
        {
            return BaseResponse<EquipmentDowngradePreviewDto>.FailResponse(
                "Không tìm thấy người chơi.", statusCode: 404);
        }

        EquipmentDowngradePreviewDto? result = null;

        try
        {
            await _unitOfWork.ExecuteStrategyAsync(async () =>
            {
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    var item = await LoadOwnedEquipmentAsync(
                        player.Id,
                        command.Request.InventoryItemId,
                        cancellationToken);

                    var preview = await DowngradeCalculator.BuildAsync(
                        _unitOfWork,
                        _statCalculationService,
                        item,
                        command.Request.TargetEnhancement,
                        cancellationToken);

                    if (!preview.CanDowngrade)
                    {
                        throw new InvalidOperationException(preview.Message);
                    }

                    var wallet = await _unitOfWork.Repository<HrkPlayerWallet>()
                        .Query()
                        .SingleAsync(x => x.PlayerId == player.Id, cancellationToken);

                    wallet.AddGold(preview.RefundedGold);
                    _unitOfWork.Repository<HrkPlayerWallet>().Update(wallet);

                    foreach (var refund in preview.RefundedStones)
                    {
                        await AddRefundedMaterialAsync(
                            player.Id, refund, cancellationToken);
                    }

                    item.Enhancement = command.Request.TargetEnhancement;
                    item.UpdateCurrentStatsCache(
                        _statCalculationService.CalculateCurrentStatsJson(
                            item, command.Request.TargetEnhancement));
                    _unitOfWork.Repository<HrkPlayerInventory>().Update(item);

                    await _unitOfWork.Repository<HrkEquipmentDowngradeHistory>()
                        .AddAsync(new HrkEquipmentDowngradeHistory
                        {
                            RequestId = command.Request.RequestId,
                            PlayerId = player.Id,
                            PlayerInventoryId = item.Id,
                            OldEnhancement = preview.CurrentEnhancement,
                            NewEnhancement = preview.TargetEnhancement,
                            RefundedGold = preview.RefundedGold,
                            RefundedMaterialsJson = JsonSerializer.Serialize(
                                preview.RefundedStones)
                        });

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await _unitOfWork.CommitTransactionAsync();
                    result = preview;
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw;
                }
            });

            return BaseResponse<EquipmentDowngradePreviewDto>.SuccessResponse(
                result!, "Hạ cấp trang bị thành công.");
        }
        catch (Exception ex)
        {
            return BaseResponse<EquipmentDowngradePreviewDto>.FailResponse(
                ex.Message, statusCode: 400);
        }
    }

    private async Task<HrkPlayerInventory> LoadOwnedEquipmentAsync(
        long playerId,
        long inventoryItemId,
        CancellationToken cancellationToken)
    {
        return await _unitOfWork.Repository<HrkPlayerInventory>()
            .Query()
            .Include(x => x.ItemTemplate)
                .ThenInclude(x => x.Category)
            .Include(x => x.ItemTemplate)
                .ThenInclude(x => x.Attributes)
                .ThenInclude(x => x.AttributeType)
            .Include(x => x.Attributes)
                .ThenInclude(x => x.AttributeType)
            .FirstOrDefaultAsync(
                x => x.Id == inventoryItemId &&
                     x.PlayerId == playerId &&
                     x.IsActive,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Không tìm thấy trang bị thuộc người chơi.");
    }

    private async Task AddRefundedMaterialAsync(
        long playerId,
        RefundedMaterialDto refund,
        CancellationToken cancellationToken)
    {
        var inventoryRepository = _unitOfWork.Repository<HrkPlayerInventory>();
        var stack = await inventoryRepository.Query().FirstOrDefaultAsync(
            x => x.PlayerId == playerId &&
                 x.ItemTemplateId == refund.ItemTemplateId &&
                 x.IsActive &&
                 !x.IsEquipped,
            cancellationToken);

        if (stack == null)
        {
            await inventoryRepository.AddAsync(new HrkPlayerInventory
            {
                PlayerId = playerId,
                ItemTemplateId = refund.ItemTemplateId,
                Count = refund.Quantity,
                IsActive = true
            });
            return;
        }

        stack.Count += refund.Quantity;
        stack.UpdatedOn = DateTime.UtcNow;
        inventoryRepository.Update(stack);
    }
}

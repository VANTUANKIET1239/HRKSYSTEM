using GAME.Application.Templates;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Commands.Inventory.SellItems;

public sealed class SellItemsCommandHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IGameRequestContext actor, IGameTemplateCatalog? templates = null)
    : ICommandHandler<SellItemsCommand, BaseResponse<SellItemsResponseDto>>
{
    private readonly IGameTemplateCatalog _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IGamePlayerService _gamePlayerService = gamePlayerService;
    private readonly IGameRequestContext _actor = actor;
    public async Task<BaseResponse<SellItemsResponseDto>> Handle(SellItemsCommand request, CancellationToken cancellationToken)
    {
        {
            var userId = _actor.UserId;
            var result = await ExecuteAsync(userId, request.Items, cancellationToken);
            return BaseResponse<SellItemsResponseDto>.SuccessResponse(result, "Bán vật phẩm thành công.");
        }
    }

    private async Task<SellItemsResponseDto> ExecuteAsync(string userId, List<SellItemRequestItem> items, CancellationToken cancellationToken = default)
    {
        if (items == null || !items.Any())
        {
            throw new BusinessException("INVALID_REQUEST", "Danh sách vật phẩm cần bán không được để trống.", BusinessErrorKind.Validation);
        }

        // Gộp các yêu cầu trùng lặp và loại bỏ các yêu cầu có số lượng <= 0
        var groupedItems = items.Where(i => i != null && i.Count > 0)
            .GroupBy(i => i.InventoryItemId)
            .Select(g => new SellItemRequestItem { InventoryItemId = g.Key, Count = g.Sum(x => x.Count) }).ToList();
        if (!groupedItems.Any())
        {
            throw new BusinessException("INVALID_REQUEST", "Không có vật phẩm hợp lệ để bán.", BusinessErrorKind.Validation);
        }

        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken);
        if (player == null)
        {
            throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy thông tin người chơi.", BusinessErrorKind.NotFound);
        }

        var wallet = await _gamePlayerService.GetWalletByPlayerIdAsync(player.Id, cancellationToken);
        if (wallet == null)
        {
            throw new BusinessException("PLAYER_WALLET_NOT_FOUND", "Không tìm thấy ví người chơi.", BusinessErrorKind.NotFound);
        }

        var itemIds = groupedItems.Select(i => i.InventoryItemId).ToList();
        var inventories = await _unitOfWork.Repository<HrkPlayerInventory>().Query()
            .Where(i => i.PlayerId == player.Id && itemIds.Contains(i.Id) && i.IsActive)
            .ToListAsync(cancellationToken);
        var itemTemplates = await TemplateHydration.ItemTemplatesAsync(_templates, cancellationToken);
        long totalEarnedGold = 0;
        int totalSoldCount = 0;
        foreach (var req in groupedItems)
        {
            var inv = inventories.FirstOrDefault(i => i.Id == req.InventoryItemId);
            if (inv == null)
            {
                throw BusinessException.Localized("ITEM_NOT_FOUND", $"Không tìm thấy vật phẩm với mã ID {req.InventoryItemId} trong hành trang.", BusinessErrorKind.NotFound);
            }

            var template = itemTemplates[inv.ItemTemplateId];
            if (inv.IsLocked)
            {
                throw BusinessException.Localized("ITEM_LOCKED", $"Vật phẩm '{template.Name}' đang bị khóa, không thể bán.", BusinessErrorKind.Conflict);
            }

            if (inv.IsEquipped)
            {
                throw BusinessException.Localized("ITEM_EQUIPPED", $"Vật phẩm '{template.Name}' đang được trang bị, không thể bán.", BusinessErrorKind.Conflict);
            }

            if (req.Count > inv.Count)
            {
                throw BusinessException.Localized("INSUFFICIENT_ITEM_QUANTITY", $"Số lượng bán ({req.Count}) vượt quá số lượng hiện có ({inv.Count}) của '{template.Name}'.", BusinessErrorKind.Conflict);
            }

            if (template.SellPrice < 0)
            {
                throw BusinessException.Localized("ITEM_NOT_SELLABLE", $"Vật phẩm '{template.Name}' là vật phẩm không thể bán.", BusinessErrorKind.Conflict);
            }

            // Sử dụng trực tiếp giá bán SellPrice được cấu hình trong bảng HRK_ItemTemplates
            long unitPrice = template.SellPrice;
            totalEarnedGold += unitPrice * req.Count;
            totalSoldCount += req.Count;
            // Soft-delete: nếu bán hết thì chuyển IsActive = false, không xóa dòng trong DB
            if (inv.Count == req.Count)
            {
                inv.Count = 0;
                inv.IsActive = false;
                inv.UpdatedOn = DateTime.UtcNow;
                _unitOfWork.Repository<HrkPlayerInventory>().Update(inv);
            }
            else
            {
                inv.Count -= req.Count;
                inv.UpdatedOn = DateTime.UtcNow;
                _unitOfWork.Repository<HrkPlayerInventory>().Update(inv);
            }
        }

        wallet.Gold += totalEarnedGold;
        wallet.UpdatedOn = DateTime.UtcNow;
        _unitOfWork.Repository<HrkPlayerWallet>().Update(wallet);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new SellItemsResponseDto
        {
            EarnedGold = totalEarnedGold,
            CurrentGold = wallet.Gold,
            SoldItemsCount = totalSoldCount
        };
    }
}

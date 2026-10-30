using GAME.Application.Templates;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.PlayerHeroes;

namespace GAME.Application.Features.Commands.PlayerHeroes.EquipHeroItem;

public sealed class EquipHeroItemCommandHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IPlayerReadModels playerReadModels,
    IGameRequestContext actor, IGameTemplateCatalog? templates = null)
    : PlayerHeroesHandlerSupport(), ICommandHandler<EquipHeroItemCommand, BaseResponse<PlayerHeroDetailDto>>
{
    private readonly IGameTemplateCatalog _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IGamePlayerService _gamePlayerService = gamePlayerService;
    private static readonly HashSet<string> ValidSlotCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "WEAPON",
        "ARMOR",
        "HELMET",
        "BOOTS",
        "RING",
        "ARTIFACT"
    };
    private readonly IPlayerReadModels _playerReadModels = playerReadModels;
    private readonly IGameRequestContext _actor = actor;

    public async Task<BaseResponse<PlayerHeroDetailDto>> Handle(EquipHeroItemCommand request, CancellationToken cancellationToken)
    {
        if (request.Request == null || request.Request.InventoryItemId <= 0)
        {
            return BaseResponse<PlayerHeroDetailDto>.FailResponse("ID trang bị trong hành trang không hợp lệ.", statusCode: 400);
        }

        {
            var userId = _actor.UserId;
            var detail = await ExecuteAsync(userId, request.HeroId, request.Request.InventoryItemId, cancellationToken);
            return BaseResponse<PlayerHeroDetailDto>.SuccessResponse(detail, "Mặc trang bị cho võ tướng thành công.");
        }
    }

    private async Task<PlayerHeroDetailDto> ExecuteAsync(string userId, long heroId, long inventoryItemId, CancellationToken cancellationToken = default)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy thông tin người chơi.", BusinessErrorKind.NotFound);
        var hero = await _unitOfWork.Repository<HrkPlayerHero>().Query()
            .FirstOrDefaultAsync(h => h.Id == heroId && h.PlayerId == player.Id && h.IsActive, cancellationToken) ?? throw new BusinessException("HERO_NOT_FOUND", "Không tìm thấy võ tướng hoặc võ tướng không thuộc tài khoản này.", BusinessErrorKind.NotFound);
        var item = await _unitOfWork.Repository<HrkPlayerInventory>().Query()
            .FirstOrDefaultAsync(i => i.Id == inventoryItemId && i.PlayerId == player.Id && i.IsActive, cancellationToken) ?? throw new BusinessException("ITEM_NOT_FOUND", "Không tìm thấy trang bị trong hành trang của người chơi.", BusinessErrorKind.NotFound);
        var template = (await TemplateHydration.ItemTemplatesAsync(_templates, cancellationToken))[item.ItemTemplateId];
        if (template?.Category == null || !template.Category.IsEquipment)
        {
            throw new BusinessException("INVALID_EQUIPMENT", "Vật phẩm này không phải là trang bị có thể mặc.", BusinessErrorKind.Conflict);
        }

        if (item.IsLocked)
        {
            throw new BusinessException("ITEM_LOCKED", "Trang bị đang bị khóa, vui lòng mở khóa trước khi mặc.", BusinessErrorKind.Conflict);
        }

        if (item.IsEquipped && item.EquippedHeroId.HasValue && item.EquippedHeroId.Value != heroId)
        {
            throw BusinessException.Localized("ITEM_EQUIPPED", $"Trang bị đang được mặc bởi võ tướng khác (ID: {item.EquippedHeroId.Value}). Vui lòng tháo trước khi chuyển.", BusinessErrorKind.Conflict);
        }

        var slotCode = (template.Category.Code ?? "").Trim().ToUpperInvariant();
        if (!ValidSlotCodes.Contains(slotCode))
        {
            throw BusinessException.Localized("INVALID_EQUIPMENT_SLOT", $"Danh mục trang bị '{slotCode}' không khớp với 6 vị trí trang bị trên võ tướng.", BusinessErrorKind.Conflict);
        }


        var equipment = await _unitOfWork.Repository<HrkPlayerEquipment>().Query()
            .FirstOrDefaultAsync(e => e.PlayerId == player.Id && e.HeroId == heroId, cancellationToken);
        var isNewEquipmentRecord = equipment == null;
        if (isNewEquipmentRecord)
        {
            equipment = new HrkPlayerEquipment
            {
                PlayerId = player.Id,
                HeroId = heroId
            };
            await _unitOfWork.Repository<HrkPlayerEquipment>().AddAsync(equipment);
        }

        long? oldItemId = GetSlotItemId(equipment!, slotCode);
        if (oldItemId.HasValue && oldItemId.Value != item.Id)
        {
            var oldItem = await _unitOfWork.Repository<HrkPlayerInventory>().Query()
                .FirstOrDefaultAsync(i => i.Id == oldItemId.Value && i.PlayerId == player.Id, cancellationToken);
            if (oldItem != null)
            {
                oldItem.IsEquipped = false;
                oldItem.EquippedHeroId = null;
                oldItem.UpdatedOn = DateTime.UtcNow;
                _unitOfWork.Repository<HrkPlayerInventory>().Update(oldItem);
            }
        }

        SetSlotItemId(equipment!, slotCode, item.Id);
        // AddAsync keeps EF state as Added. Calling Update immediately after AddAsync
        // changes that state to Modified and can make the first equip attempt fail.
        if (!isNewEquipmentRecord)
        {
            _unitOfWork.Repository<HrkPlayerEquipment>().Update(equipment!);
        }

        item.IsEquipped = true;
        item.EquippedHeroId = heroId;
        item.UpdatedOn = DateTime.UtcNow;
        _unitOfWork.Repository<HrkPlayerInventory>().Update(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedDetail = await _playerReadModels.GetPlayerHeroDetailAsync(userId, heroId, cancellationToken);
        return updatedDetail ?? throw new InvalidOperationException("Không thể lấy dữ liệu võ tướng sau khi trang bị.");
    }
}

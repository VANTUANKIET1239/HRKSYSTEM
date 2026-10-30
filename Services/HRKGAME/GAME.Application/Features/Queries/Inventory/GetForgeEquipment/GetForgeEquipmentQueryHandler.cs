using GAME.Application.Templates;
using GAME.Application.Common;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Persistence.Repositories;
using Core.Application.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Features.Queries.Inventory.GetForgeEquipment
{
    public class GetForgeEquipmentQueryHandler : AuthenticatedGameHandler, IQueryHandler<GetForgeEquipmentQuery, BaseResponse<List<ForgeEquipmentItemDto>>>
    {
        private readonly IGameTemplateCatalog _templates;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGamePlayerService _gamePlayerService;
        public GetForgeEquipmentQueryHandler(IUnitOfWork unitOfWork, IGamePlayerService gamePlayerService, IGameRequestContext requestContext, IGameTemplateCatalog? templates = null) : base(requestContext)
        {
            _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);
            _unitOfWork = unitOfWork;
            _gamePlayerService = gamePlayerService;
        }

        public async Task<BaseResponse<List<ForgeEquipmentItemDto>>> Handle(GetForgeEquipmentQuery request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken);
            if (player == null)
            {
                return BaseResponse<List<ForgeEquipmentItemDto>>.FailResponse("Không tìm thấy thông tin người chơi.", statusCode: 404);
            }

            // Truy vấn các trang bị còn active của người chơi (Category.IsEquipment == true)
            var itemTemplates = await TemplateHydration.ItemTemplatesAsync(_templates, cancellationToken);
            var equipmentIds = itemTemplates.Values.Where(t => t.Category.IsEquipment).Select(t => t.Id).ToArray();
            var inventories = await _unitOfWork.ReadOnlyRepository<HrkPlayerInventory>().Query()
                .Where(i => i.PlayerId == player.Id && i.IsActive && equipmentIds.Contains(i.ItemTemplateId))
                .ToListAsync(cancellationToken);
            foreach (var inventory in inventories) inventory.ItemTemplate = itemTemplates[inventory.ItemTemplateId];
            var dtos = inventories.OrderByDescending(i => i.Enhancement).ThenByDescending(i => i.ItemTemplate.Rarity.DisplayOrder).Select(item =>
            {
                var eligibility = item.CheckEnhancementEligibility();
                return new ForgeEquipmentItemDto
                {
                    InventoryItemId = item.Id,
                    ItemTemplateId = item.ItemTemplateId,
                    Code = item.ItemTemplate?.Code ?? string.Empty,
                    Name = item.ItemTemplate?.Name ?? string.Empty,
                    ImagePath = item.ItemTemplate?.ImagePath,
                    Icon = item.ItemTemplate?.Icon,
                    CategoryCode = item.ItemTemplate?.Category?.Code ?? string.Empty,
                    CategoryName = item.ItemTemplate?.Category?.Name ?? string.Empty,
                    RarityCode = item.ItemTemplate?.Rarity?.Code ?? "Common",
                    RarityName = item.ItemTemplate?.Rarity?.Name ?? "Thường",
                    RarityColorHex = item.ItemTemplate?.Rarity?.ColorHex,
                    RarityOrder = item.ItemTemplate?.Rarity?.DisplayOrder ?? 0,
                    LevelReq = item.ItemTemplate?.LevelReq ?? 1,
                    Enhancement = item.Enhancement,
                    Stars = item.Stars,
                    IsEquipped = item.IsEquipped,
                    IsLocked = item.IsLocked,
                    CanEnhance = eligibility.CanEnhance,
                    EnhancementBlockedReasonCode = eligibility.CanEnhance ? null : eligibility.ReasonCode,
                    EnhancementBlockedMessage = eligibility.CanEnhance ? null : eligibility.Message
                };
            }).ToList();
            return BaseResponse<List<ForgeEquipmentItemDto>>.SuccessResponse(dtos, "Tải danh sách trang bị cho Lò Rèn thành công.");
        }
    }
}

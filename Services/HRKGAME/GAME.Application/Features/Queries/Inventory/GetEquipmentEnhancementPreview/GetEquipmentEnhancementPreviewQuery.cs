using Core.Common.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Common.Repositories;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Application.Features.Queries.Inventory.GetEquipmentEnhancementPreview
{
    public record GetEquipmentEnhancementPreviewQuery(long InventoryItemId) : IQuery<BaseResponse<EquipmentEnhancementPreviewDto>>;

    public class GetEquipmentEnhancementPreviewQueryHandler : HRKBaseQuery, IQueryHandler<GetEquipmentEnhancementPreviewQuery, BaseResponse<EquipmentEnhancementPreviewDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGamePlayerService _gamePlayerService;
        private readonly IItemStatCalculationService _statCalculationService;

        public GetEquipmentEnhancementPreviewQueryHandler(
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

        public async Task<BaseResponse<EquipmentEnhancementPreviewDto>> Handle(GetEquipmentEnhancementPreviewQuery request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken);
            if (player == null)
            {
                return BaseResponse<EquipmentEnhancementPreviewDto>.FailResponse("Không tìm thấy thông tin người chơi.", statusCode: 404);
            }

            var item = await _unitOfWork.ReadOnlyRepository<HrkPlayerInventory>().Query()
                .Include(i => i.ItemTemplate).ThenInclude(t => t.Category)
                .Include(i => i.ItemTemplate).ThenInclude(t => t.Attributes).ThenInclude(a => a.AttributeType)
                .FirstOrDefaultAsync(i => i.Id == request.InventoryItemId && i.PlayerId == player.Id && i.IsActive, cancellationToken);

            if (item == null)
            {
                return BaseResponse<EquipmentEnhancementPreviewDto>.FailResponse("Không tìm thấy trang bị trong hành trang.", statusCode: 404);
            }

            var eligibility = item.CheckEnhancementEligibility();
            var currentLevel = item.Enhancement;
            var targetLevel = Math.Min(15, currentLevel + 1);

            var levelConfig = await _unitOfWork.ReadOnlyRepository<HrkEnhancementLevelConfig>().Query()
                .FirstOrDefaultAsync(c => c.CurrentLevel == currentLevel, cancellationToken);

            var currentStats = _statCalculationService.CalculateCurrentStats(item.ItemTemplate, currentLevel, item.Stars);
            var nextStats = _statCalculationService.CalculateCurrentStats(item.ItemTemplate, targetLevel, item.Stars);

            var dto = new EquipmentEnhancementPreviewDto
            {
                InventoryItemId = item.Id,
                CurrentEnhancement = currentLevel,
                TargetEnhancement = targetLevel,
                CurrentStats = currentStats,
                NextStats = nextStats,
                BaseSuccessRate = levelConfig?.BaseSuccessRate ?? 0m,
                GoldCost = levelConfig?.GoldCost ?? 0,
                FailureDropLevels = levelConfig?.FailureDropLevels ?? 0,
                MaxStoneSlots = levelConfig?.MaxStoneSlots ?? 3,
                CanEnhance = eligibility.CanEnhance,
                ReasonCode = eligibility.CanEnhance ? null : eligibility.ReasonCode,
                Message = eligibility.CanEnhance ? null : eligibility.Message
            };

            return BaseResponse<EquipmentEnhancementPreviewDto>.SuccessResponse(dto, "Lấy thông tin xem trước cường hóa thành công.");
        }
    }
}

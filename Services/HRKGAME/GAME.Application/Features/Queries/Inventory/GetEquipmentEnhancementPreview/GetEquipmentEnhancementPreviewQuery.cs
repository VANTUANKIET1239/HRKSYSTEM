using GAME.Application.Templates;
using GAME.Application.Common;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Persistence.Repositories;
using Core.Application.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Features.Queries.Inventory.GetEquipmentEnhancementPreview
{
    public record GetEquipmentEnhancementPreviewQuery(long InventoryItemId) : IQuery<BaseResponse<EquipmentEnhancementPreviewDto>>;
    public class GetEquipmentEnhancementPreviewQueryHandler : AuthenticatedGameHandler, IQueryHandler<GetEquipmentEnhancementPreviewQuery, BaseResponse<EquipmentEnhancementPreviewDto>>
    {
        private readonly IGameTemplateCatalog _templates;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGamePlayerService _gamePlayerService;
        private readonly IItemStatCalculationService _statCalculationService;
        public GetEquipmentEnhancementPreviewQueryHandler(IUnitOfWork unitOfWork, IGamePlayerService gamePlayerService, IItemStatCalculationService statCalculationService, IGameRequestContext requestContext, IGameTemplateCatalog? templates = null) : base(requestContext)
        {
            _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);
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
                .Include(i => i.Attributes)
                .FirstOrDefaultAsync(i => i.Id == request.InventoryItemId && i.PlayerId == player.Id && i.IsActive, cancellationToken);
            if (item == null)
            {
                return BaseResponse<EquipmentEnhancementPreviewDto>.FailResponse("Không tìm thấy trang bị trong hành trang.", statusCode: 404);
            }

            await TemplateHydration.InventoryAsync(_templates, new[] { item }, cancellationToken);
            var eligibility = item.CheckEnhancementEligibility();
            var currentLevel = item.Enhancement;
            var targetLevel = Math.Min(15, currentLevel + 1);
            var levelConfig = (await _templates.GetAllAsync<HrkEnhancementLevelConfig>(cancellationToken))
                .FirstOrDefault(c => c.CurrentLevel == currentLevel);
            var currentStats = _statCalculationService.CalculateCurrentStats(item, currentLevel);
            var nextStats = _statCalculationService.CalculateCurrentStats(item, targetLevel);
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

using GAME.Application.Templates;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Persistence.Repositories;
using Core.Application.CQRS;
using GAME.Application.DTOs;
using GAME.Domain.Entities;

namespace GAME.Application.Features.Queries.Inventory.GetEnhancementConfigs
{
    public record GetEnhancementConfigsQuery() : IQuery<BaseResponse<EnhancementConfigResponseDto>>;
    public class GetEnhancementConfigsQueryHandler : IQueryHandler<GetEnhancementConfigsQuery, BaseResponse<EnhancementConfigResponseDto>>
    {
        private readonly IGameTemplateCatalog _templates;
        private readonly IUnitOfWork _unitOfWork;
        public GetEnhancementConfigsQueryHandler(IUnitOfWork unitOfWork, IGameTemplateCatalog? templates = null)
        {
            _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponse<EnhancementConfigResponseDto>> Handle(GetEnhancementConfigsQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteQueryAsync(cancellationToken);
        }

        private async Task<BaseResponse<EnhancementConfigResponseDto>> ExecuteQueryAsync(CancellationToken cancellationToken)
        {
            var levelConfigs = (await _templates.GetAllAsync<HrkEnhancementLevelConfig>(cancellationToken))
                .OrderBy(c => c.CurrentLevel)
                .Select(c => new EnhancementLevelConfigDto { CurrentLevel = c.CurrentLevel, NextLevel = c.NextLevel, BaseSuccessRate = c.BaseSuccessRate, GoldCost = c.GoldCost, FailureDropLevels = c.FailureDropLevels, MaxStoneSlots = c.MaxStoneSlots })
                .ToList();
            var materials = (await _templates.GetAllAsync<HrkEnhancementMaterial>(cancellationToken))
                .Select(m => new EnhancementMaterialDto { ItemTemplateId = m.ItemTemplateId, Code = m.ItemTemplate.Code, Name = m.ItemTemplate.Name, Icon = m.ItemTemplate.Icon, ImagePath = m.ItemTemplate.ImagePath, RarityCode = m.ItemTemplate.Rarity.Code, RarityName = m.ItemTemplate.Rarity.Name, RarityColorHex = m.ItemTemplate.Rarity.ColorHex, MaterialType = m.MaterialType, SuccessRateBonus = m.SuccessRateBonus, PreventLevelDrop = m.PreventLevelDrop, Description = m.ItemTemplate.Description })
                .ToList();
            var result = new EnhancementConfigResponseDto
            {
                LevelConfigs = levelConfigs,
                Materials = materials
            };
            return BaseResponse<EnhancementConfigResponseDto>.SuccessResponse(result);
        }
    }
}

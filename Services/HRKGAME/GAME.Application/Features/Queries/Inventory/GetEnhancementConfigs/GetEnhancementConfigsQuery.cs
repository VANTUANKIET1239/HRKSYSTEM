using Core.Common.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Common.Repositories;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Application.Features.Queries.Inventory.GetEnhancementConfigs
{
    public record GetEnhancementConfigsQuery() : IQuery<BaseResponse<EnhancementConfigResponseDto>>;

    public class GetEnhancementConfigsQueryHandler : 
        IQueryHandler<GetEnhancementConfigsQuery, BaseResponse<EnhancementConfigResponseDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetEnhancementConfigsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponse<EnhancementConfigResponseDto>> Handle(GetEnhancementConfigsQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteQueryAsync(cancellationToken);
        }

        private async Task<BaseResponse<EnhancementConfigResponseDto>> ExecuteQueryAsync(CancellationToken cancellationToken)
        {
            var levelConfigs = await _unitOfWork.ReadOnlyRepository<HrkEnhancementLevelConfig>().Query()
                .OrderBy(c => c.CurrentLevel)
                .Select(c => new EnhancementLevelConfigDto
                {
                    CurrentLevel = c.CurrentLevel,
                    NextLevel = c.NextLevel,
                    BaseSuccessRate = c.BaseSuccessRate,
                    GoldCost = c.GoldCost,
                    FailureDropLevels = c.FailureDropLevels,
                    MaxStoneSlots = c.MaxStoneSlots
                })
                .ToListAsync(cancellationToken);

            var materials = await _unitOfWork.ReadOnlyRepository<HrkEnhancementMaterial>().Query()
                .Include(m => m.ItemTemplate).ThenInclude(t => t.Rarity)
                .Select(m => new EnhancementMaterialDto
                {
                    ItemTemplateId = m.ItemTemplateId,
                    Code = m.ItemTemplate.Code,
                    Name = m.ItemTemplate.Name,
                    Icon = m.ItemTemplate.Icon,
                    ImagePath = m.ItemTemplate.ImagePath,
                    RarityCode = m.ItemTemplate.Rarity.Code,
                    RarityName = m.ItemTemplate.Rarity.Name,
                    RarityColorHex = m.ItemTemplate.Rarity.ColorHex,
                    MaterialType = m.MaterialType,
                    SuccessRateBonus = m.SuccessRateBonus,
                    PreventLevelDrop = m.PreventLevelDrop,
                    Description = m.ItemTemplate.Description
                })
                .ToListAsync(cancellationToken);

            var result = new EnhancementConfigResponseDto
            {
                LevelConfigs = levelConfigs,
                Materials = materials
            };

            return BaseResponse<EnhancementConfigResponseDto>.SuccessResponse(result);
        }
    }
}

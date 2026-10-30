using GAME.Application.Templates;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Persistence.Repositories;
using Core.Application.CQRS;
using GAME.Application.DTOs;
using GAME.Domain.Entities;

namespace GAME.Application.Features.Queries.Inventory.GetEnhancementConfigs
{
    public record GetEnhancementConfigsQuery() : IQuery<BaseResponse<EnhancementConfigResponseDto>>;
}

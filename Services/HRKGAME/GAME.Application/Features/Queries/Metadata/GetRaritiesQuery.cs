using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Queries.Metadata;

public record GetRaritiesQuery : IQuery<BaseResponse<List<RarityDto>>>;

public sealed class GetRaritiesQueryHandler(IUnitOfWork unitOfWork) : IQueryHandler<GetRaritiesQuery, BaseResponse<List<RarityDto>>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    public async Task<BaseResponse<List<RarityDto>>> Handle(GetRaritiesQuery request, CancellationToken cancellationToken)
    {
        var rarities = await ExecuteAsync(cancellationToken);
        return BaseResponse<List<RarityDto>>.SuccessResponse(rarities, "Retrieved rarities successfully.");
    }

    private async Task<List<RarityDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ReadOnlyRepository<HrkRarity>().Query()
            .OrderBy(r => r.DisplayOrder)
            .Select(r => new RarityDto { Id = r.Id, Code = r.Code, Name = r.Name, ColorHex = r.ColorHex, DisplayOrder = r.DisplayOrder })
            .ToListAsync(cancellationToken);
    }
}

using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Queries.Metadata;

public record GetFactionsQuery : IQuery<BaseResponse<List<HeroFactionDto>>>;

public sealed class GetFactionsQueryHandler(IUnitOfWork unitOfWork) : IQueryHandler<GetFactionsQuery, BaseResponse<List<HeroFactionDto>>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    public async Task<BaseResponse<List<HeroFactionDto>>> Handle(GetFactionsQuery request, CancellationToken cancellationToken)
    {
        var factions = await ExecuteAsync(cancellationToken);
        return BaseResponse<List<HeroFactionDto>>.SuccessResponse(factions, "Retrieved hero factions successfully.");
    }

    private async Task<List<HeroFactionDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ReadOnlyRepository<HrkHeroFaction>().Query()
            .OrderBy(f => f.DisplayOrder)
            .Select(f => new HeroFactionDto { Id = f.Id, Code = f.Code, Name = f.Name, Icon = f.Icon, DisplayOrder = f.DisplayOrder })
            .ToListAsync(cancellationToken);
    }
}

using GAME.Application.Templates;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Queries.Metadata;

public sealed class GetClassesQueryHandler(IUnitOfWork unitOfWork, IGameTemplateCatalog? templates = null) : IQueryHandler<GetClassesQuery, BaseResponse<List<HeroClassDto>>>
{
    private readonly IGameTemplateCatalog _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);
    public async Task<BaseResponse<List<HeroClassDto>>> Handle(GetClassesQuery request, CancellationToken cancellationToken)
    {
        var classes = await ExecuteAsync(cancellationToken);
        return BaseResponse<List<HeroClassDto>>.SuccessResponse(classes, "Retrieved hero classes successfully.");
    }

    private async Task<List<HeroClassDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return (await _templates.GetAllAsync<HrkHeroClass>(cancellationToken))
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new HeroClassDto { Id = c.Id, Code = c.Code, Name = c.Name, Icon = c.Icon, DisplayOrder = c.DisplayOrder })
            .ToList();
    }
}

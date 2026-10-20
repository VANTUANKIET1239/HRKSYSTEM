using GAME.Application.Templates;
using Core.Persistence.Repositories;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Queries.Catalog;

public record GetHeroTemplatesQuery : IQuery<BaseResponse<List<HeroTemplateDto>>>;

public sealed class GetHeroTemplatesQueryHandler(IUnitOfWork unitOfWork, IGameTemplateCatalog? templates = null) : IQueryHandler<GetHeroTemplatesQuery, BaseResponse<List<HeroTemplateDto>>>
{
    private readonly IGameTemplateCatalog _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);
    public async Task<BaseResponse<List<HeroTemplateDto>>> Handle(GetHeroTemplatesQuery request, CancellationToken cancellationToken)
    {
        var heroes = await ExecuteAsync(cancellationToken);
        return BaseResponse<List<HeroTemplateDto>>.SuccessResponse(heroes, "Retrieved hero templates successfully.");
    }

    private async Task<List<HeroTemplateDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var heroes = (await _templates.GetAllAsync<HrkHeroTemplate>(cancellationToken))
            .ToList();
        return heroes.Select(h => GameDtoMapper.MapHeroTemplate(h)!)
            .Where(dto => dto != null).ToList();
    }
}

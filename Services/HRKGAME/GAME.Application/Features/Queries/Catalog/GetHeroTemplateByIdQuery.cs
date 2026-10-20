using GAME.Application.Templates;
using Core.Persistence.Repositories;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Queries.Catalog;

public record GetHeroTemplateByIdQuery(int Id) : IQuery<BaseResponse<HeroTemplateDto>>;

public sealed class GetHeroTemplateByIdQueryHandler(IUnitOfWork unitOfWork, IGameTemplateCatalog? templates = null) : IQueryHandler<GetHeroTemplateByIdQuery, BaseResponse<HeroTemplateDto>>
{
    private readonly IGameTemplateCatalog _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);
    public async Task<BaseResponse<HeroTemplateDto>> Handle(GetHeroTemplateByIdQuery request, CancellationToken cancellationToken)
    {
        var hero = await ExecuteAsync(request.Id, cancellationToken);
        if (hero == null)
        {
            return BaseResponse<HeroTemplateDto>.FailResponse("Hero template not found.", statusCode: 404);
        }

        return BaseResponse<HeroTemplateDto>.SuccessResponse(hero, "Retrieved hero template detail successfully.");
    }

    private async Task<HeroTemplateDto?> ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        var hero = (await _templates.GetAllAsync<HrkHeroTemplate>(cancellationToken))
            .FirstOrDefault(h => h.Id == id);
        return hero != null ? GameDtoMapper.MapHeroTemplate(hero) : null;
    }
}

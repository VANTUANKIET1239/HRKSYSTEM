using GAME.Application.Templates;
using Core.Persistence.Repositories;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Queries.Catalog;

public sealed class GetSkillTemplatesQueryHandler(IUnitOfWork unitOfWork, IGameTemplateCatalog? templates = null) : IQueryHandler<GetSkillTemplatesQuery, BaseResponse<List<SkillTemplateDto>>>
{
    private readonly IGameTemplateCatalog _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);
    public async Task<BaseResponse<List<SkillTemplateDto>>> Handle(GetSkillTemplatesQuery request, CancellationToken cancellationToken)
    {
        var skills = await ExecuteAsync(cancellationToken);
        return BaseResponse<List<SkillTemplateDto>>.SuccessResponse(skills, "Retrieved skill templates successfully.");
    }

    private async Task<List<SkillTemplateDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var skills = (await _templates.GetAllAsync<HrkSkillTemplate>(cancellationToken))
            .Where(s => s.IsActive)
            .ToList();
        return skills.Select(s => GameDtoMapper.MapSkillTemplate(s)!)
            .Where(dto => dto != null).ToList();
    }
}

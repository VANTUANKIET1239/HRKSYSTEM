using Core.Persistence.Repositories;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Queries.Catalog;

public record GetSkillTemplatesQuery : IQuery<BaseResponse<List<SkillTemplateDto>>>;

public sealed class GetSkillTemplatesQueryHandler(IUnitOfWork unitOfWork) : IQueryHandler<GetSkillTemplatesQuery, BaseResponse<List<SkillTemplateDto>>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    public async Task<BaseResponse<List<SkillTemplateDto>>> Handle(GetSkillTemplatesQuery request, CancellationToken cancellationToken)
    {
        var skills = await ExecuteAsync(cancellationToken);
        return BaseResponse<List<SkillTemplateDto>>.SuccessResponse(skills, "Retrieved skill templates successfully.");
    }

    private async Task<List<SkillTemplateDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var skills = await _unitOfWork.ReadOnlyRepository<HrkSkillTemplate>().Query()
            .Where(s => s.IsActive)
            .Include(s => s.Effects).ThenInclude(e => e.EffectType)
            .Include(s => s.Effects).ThenInclude(e => e.TargetType)
            .Include(s => s.Effects).ThenInclude(e => e.Scalings).ThenInclude(sc => sc.AttributeType)
            .Include(s => s.Effects).ThenInclude(e => e.StatModifiers).ThenInclude(sm => sm.AttributeType)
            .Include(s => s.Effects).ThenInclude(e => e.Parameters)
            .Include(s => s.AnimationConfig)!.ThenInclude(a => a.Phases)
            .ToListAsync(cancellationToken);
        return skills.Select(s => GameDtoMapper.MapSkillTemplate(s)!)
            .Where(dto => dto != null).ToList();
    }
}

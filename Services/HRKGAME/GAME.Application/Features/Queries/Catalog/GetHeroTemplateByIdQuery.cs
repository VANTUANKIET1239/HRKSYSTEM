using Core.Persistence.Repositories;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Queries.Catalog;

public record GetHeroTemplateByIdQuery(int Id) : IQuery<BaseResponse<HeroTemplateDto>>;

public sealed class GetHeroTemplateByIdQueryHandler(IUnitOfWork unitOfWork) : IQueryHandler<GetHeroTemplateByIdQuery, BaseResponse<HeroTemplateDto>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
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
        var hero = await _unitOfWork.ReadOnlyRepository<HrkHeroTemplate>().Query()
            .Include(h => h.Faction)
            .Include(h => h.Class)
            .Include(h => h.Rarity)
            .Include(h => h.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.EffectType)
            .Include(h => h.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.TargetType)
            .Include(h => h.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.Scalings).ThenInclude(sc => sc.AttributeType)
            .Include(h => h.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.StatModifiers).ThenInclude(sm => sm.AttributeType)
            .Include(h => h.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.Parameters)
            .Include(h => h.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.AnimationConfig)!.ThenInclude(a => a.Phases)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
        return hero != null ? GameDtoMapper.MapHeroTemplate(hero) : null;
    }
}

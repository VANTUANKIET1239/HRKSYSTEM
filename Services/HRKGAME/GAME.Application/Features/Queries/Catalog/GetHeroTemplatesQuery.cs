using Core.Persistence.Repositories;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Queries.Catalog;

public record GetHeroTemplatesQuery : IQuery<BaseResponse<List<HeroTemplateDto>>>;

public sealed class GetHeroTemplatesQueryHandler(IUnitOfWork unitOfWork) : IQueryHandler<GetHeroTemplatesQuery, BaseResponse<List<HeroTemplateDto>>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    public async Task<BaseResponse<List<HeroTemplateDto>>> Handle(GetHeroTemplatesQuery request, CancellationToken cancellationToken)
    {
        var heroes = await ExecuteAsync(cancellationToken);
        return BaseResponse<List<HeroTemplateDto>>.SuccessResponse(heroes, "Retrieved hero templates successfully.");
    }

    private async Task<List<HeroTemplateDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var heroes = await _unitOfWork.ReadOnlyRepository<HrkHeroTemplate>().Query()
            .Include(h => h.Faction)
            .Include(h => h.Class)
            .Include(h => h.Rarity)
            .Include(h => h.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.EffectType)
            .Include(h => h.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.TargetType)
            .Include(h => h.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.Scalings).ThenInclude(sc => sc.AttributeType)
            .Include(h => h.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.StatModifiers).ThenInclude(sm => sm.AttributeType)
            .Include(h => h.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.Parameters)
            .Include(h => h.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.AnimationConfig)!.ThenInclude(a => a.Phases)
            .ToListAsync(cancellationToken);
        return heroes.Select(h => GameDtoMapper.MapHeroTemplate(h)!)
            .Where(dto => dto != null).ToList();
    }
}

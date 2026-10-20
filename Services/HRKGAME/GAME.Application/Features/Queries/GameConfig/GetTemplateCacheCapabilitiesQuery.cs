using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Templates;

namespace GAME.Application.Features.Queries.GameConfig;

public sealed record TemplateCacheCapabilitiesDto(bool CanRefresh);
public sealed record GetTemplateCacheCapabilitiesQuery : IQuery<BaseResponse<TemplateCacheCapabilitiesDto>>;

public sealed class GetTemplateCacheCapabilitiesQueryHandler(IGameTemplateRefreshAccess access)
    : IQueryHandler<GetTemplateCacheCapabilitiesQuery, BaseResponse<TemplateCacheCapabilitiesDto>>
{
    public Task<BaseResponse<TemplateCacheCapabilitiesDto>> Handle(GetTemplateCacheCapabilitiesQuery request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(BaseResponse<TemplateCacheCapabilitiesDto>.SuccessResponse(new(access.CanRefresh)));
    }
}

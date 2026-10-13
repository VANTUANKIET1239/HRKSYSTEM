using Core.Application.CQRS;
using Core.Caching;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Features.Queries.GameConfig;

public sealed record GetModalConfigsQuery : IQuery<BaseResponse<List<ModalConfigDto>>>;

public sealed class GetModalConfigsQueryHandler(IUnitOfWork unitOfWork, ICacheService cache)
    : IQueryHandler<GetModalConfigsQuery, BaseResponse<List<ModalConfigDto>>>
{
    public const string CacheKey = "game:config:modals:v1";
    public static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<BaseResponse<List<ModalConfigDto>>> Handle(GetModalConfigsQuery request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var result = await cache.GetAsync<List<ModalConfigDto>>(CacheKey, ct);
        if (result == null)
        {
            result = await unitOfWork.ReadOnlyRepository<HrkModalConfig>().Query()
                .Where(config => config.IsActive).OrderBy(config => config.Code)
                .Select(config => new ModalConfigDto(config.Code, config.Kind, config.Title, config.MessageTemplate,
                    config.ConfirmText, config.CancelText, config.IconKey, config.Size, config.Tone,
                    config.CloseOnBackdrop, config.CloseOnEscape, config.Revision))
                .ToListAsync(ct);
            await cache.SetAsync(CacheKey, result, CacheDuration, ct);
        }
        return BaseResponse<List<ModalConfigDto>>.SuccessResponse(result);
    }
}

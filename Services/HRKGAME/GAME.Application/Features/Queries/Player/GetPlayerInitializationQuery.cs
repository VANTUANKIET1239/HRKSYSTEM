using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Persistence.Repositories;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Features.Queries.Player;

public sealed record PlayerInitializationStatusDto(string Status, long? PlayerId);
public sealed record GetPlayerInitializationQuery(string UserId) : IQuery<BaseResponse<PlayerInitializationStatusDto>>;

public sealed class GetPlayerInitializationQueryHandler(IUnitOfWork unitOfWork)
    : IQueryHandler<GetPlayerInitializationQuery, BaseResponse<PlayerInitializationStatusDto>>
{
    public async Task<BaseResponse<PlayerInitializationStatusDto>> Handle(GetPlayerInitializationQuery query, CancellationToken ct)
    {
        var player = await unitOfWork.ReadOnlyRepository<HrkPlayer>().Query()
            .Where(x => x.UserId == query.UserId).Select(x => new { x.Id, x.IsActive }).SingleOrDefaultAsync(ct);
        return BaseResponse<PlayerInitializationStatusDto>.SuccessResponse(player is null
            ? new("Pending", null) : new(player.IsActive ? "Ready" : "Unavailable", player.Id));
    }
}

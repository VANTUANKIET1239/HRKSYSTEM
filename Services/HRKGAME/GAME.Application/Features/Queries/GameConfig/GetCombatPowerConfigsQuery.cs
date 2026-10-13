using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
namespace GAME.Application.Features.Queries.GameConfig;

public sealed record GetCombatPowerConfigsQuery() : IQuery<BaseResponse<List<CombatPowerConfigDto>>>;
public sealed class GetCombatPowerConfigsQueryHandler(ICombatPowerService power) : IQueryHandler<GetCombatPowerConfigsQuery, BaseResponse<List<CombatPowerConfigDto>>>
{
    public async Task<BaseResponse<List<CombatPowerConfigDto>>> Handle(GetCombatPowerConfigsQuery message, CancellationToken ct) => BaseResponse<List<CombatPowerConfigDto>>.SuccessResponse(await power.GetConfigsAsync(ct));
}

using Core.Common.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace GAME.Application.Features.Queries.Battle
{
    public record GetBattleInitialStateQuery : IQuery<BaseResponse<BattleInitialStateDto>>;

    public class GetBattleInitialStateQueryHandler : HRKBaseQuery, IQueryHandler<GetBattleInitialStateQuery, BaseResponse<BattleInitialStateDto>>
    {
        private readonly IBattleService _battleService;

        public GetBattleInitialStateQueryHandler(IBattleService battleService, IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
        {
            _battleService = battleService;
        }

        public async Task<BaseResponse<BattleInitialStateDto>> Handle(GetBattleInitialStateQuery request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var state = await _battleService.GetBattleInitialStateAsync(userId, cancellationToken);
            return BaseResponse<BattleInitialStateDto>.SuccessResponse(state, "Retrieved battle initial state successfully.");
        }
    }
}

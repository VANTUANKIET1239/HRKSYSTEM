using GAME.Application.Common;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Application.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;

namespace GAME.Application.Features.Queries.Battle
{
    public class GetBattleInitialStateQueryHandler : AuthenticatedGameHandler, IQueryHandler<GetBattleInitialStateQuery, BaseResponse<BattleInitialStateDto>>
    {
        private readonly IBattleExecutor _battleService;
        public GetBattleInitialStateQueryHandler(IBattleExecutor battleService, IGameRequestContext requestContext) : base(requestContext)
        {
            _battleService = battleService;
        }

        public async Task<BaseResponse<BattleInitialStateDto>> Handle(GetBattleInitialStateQuery request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var state = await _battleService.BuildInitialStateAsync(userId, cancellationToken);
            return BaseResponse<BattleInitialStateDto>.SuccessResponse(state, "Retrieved battle initial state successfully.");
        }
    }
}

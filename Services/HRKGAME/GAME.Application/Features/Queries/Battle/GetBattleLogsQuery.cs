using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;

namespace GAME.Application.Features.Queries.Battle
{
    public record GetBattleLogsQuery(string BattleId) : IQuery<BaseResponse<List<BattleLogDto>>>;

    public class GetBattleLogsQueryHandler : IQueryHandler<GetBattleLogsQuery, BaseResponse<List<BattleLogDto>>>
    {
        private readonly IBattleService _battleService;

        public GetBattleLogsQueryHandler(IBattleService battleService)
        {
            _battleService = battleService;
        }

        public async Task<BaseResponse<List<BattleLogDto>>> Handle(GetBattleLogsQuery request, CancellationToken cancellationToken)
        {
            var logs = await _battleService.GetBattleLogsAsync(request.BattleId, cancellationToken);
            return BaseResponse<List<BattleLogDto>>.SuccessResponse(logs, "Retrieved battle logs successfully.");
        }
    }
}

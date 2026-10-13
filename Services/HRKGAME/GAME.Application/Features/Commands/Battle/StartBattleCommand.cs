using Core.Common.Errors;
using GAME.Application.Common;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Application.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;

namespace GAME.Application.Features.Commands.Battle;

public record StartBattleCommand(StartBattleRequestDto Request) : ICommand<BaseResponse<StartBattleResultDto>>;

public sealed class StartBattleCommandHandler : AuthenticatedGameHandler, ICommandHandler<StartBattleCommand, BaseResponse<StartBattleResultDto>>
{
    private readonly IBattleExecutor _battleService;
    public StartBattleCommandHandler(IBattleExecutor battleService, IGameRequestContext requestContext) : base(requestContext) => _battleService = battleService;
    public async Task<BaseResponse<StartBattleResultDto>> Handle(StartBattleCommand command, CancellationToken cancellationToken)
    {
        if (string.Equals(command.Request.BattleType, "CAMPAIGN", StringComparison.OrdinalIgnoreCase) || command.Request.StageId.HasValue)
            throw new BusinessException("INVALID_BATTLE_REQUEST", "Phó bản phải được bắt đầu qua API dungeon để kiểm tra mở khóa và thể lực.", BusinessErrorKind.Validation);
        var result = await _battleService.ExecuteAsync(GetUserId(), command.Request, cancellationToken);
        return BaseResponse<StartBattleResultDto>.SuccessResponse(result, "Battle simulated successfully.");
    }
}

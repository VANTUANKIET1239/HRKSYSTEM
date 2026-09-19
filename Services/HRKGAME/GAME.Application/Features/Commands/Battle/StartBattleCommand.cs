using Core.Common.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace GAME.Application.Features.Commands.Battle;

public record StartBattleCommand(StartBattleRequestDto Request) : ICommand<BaseResponse<StartBattleResultDto>>;

public sealed class StartBattleCommandHandler : HRKBaseCommand,
    ICommandHandler<StartBattleCommand, BaseResponse<StartBattleResultDto>>
{
    private readonly IBattleService _battleService;

    public StartBattleCommandHandler(IBattleService battleService, IHttpContextAccessor httpContextAccessor)
        : base(httpContextAccessor) => _battleService = battleService;

    public async Task<BaseResponse<StartBattleResultDto>> Handle(StartBattleCommand command, CancellationToken cancellationToken)
    {
        var result = await _battleService.StartBattleAsync(GetUserId(), command.Request, cancellationToken);
        return BaseResponse<StartBattleResultDto>.SuccessResponse(result, "Battle simulated successfully.");
    }
}

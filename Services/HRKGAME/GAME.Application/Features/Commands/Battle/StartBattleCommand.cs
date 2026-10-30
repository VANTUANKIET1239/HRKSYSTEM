using Core.Common.Errors;
using GAME.Application.Common;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Application.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;

namespace GAME.Application.Features.Commands.Battle;

public record StartBattleCommand(StartBattleRequestDto Request) : ICommand<BaseResponse<StartBattleResultDto>>;

using GAME.Application.Templates;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Domain.Battle;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Services;
using GAME.Application.Features.Shared.BattleLab;

namespace GAME.Application.Features.Commands.BattleLab;

public sealed record RunBattleLabCommand(BattleLabRequestDto Request) : ICommand<BaseResponse<BattleLabReportDto>>;

using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Services;
using GAME.Application.Features.Shared.Tower;
using GAME.Application.Events;

namespace GAME.Application.Features.Commands.Tower;

public sealed record StartFloorBattleCommand(

    string UserId,

    int FloorNumber,

    StartTowerBattleRequestDto Request

)

    : ICommand<BaseResponse<StartTowerBattleResultDto>>;

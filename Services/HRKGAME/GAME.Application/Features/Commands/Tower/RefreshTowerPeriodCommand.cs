using Core.Application.CQRS;
using Core.Common.Errors;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Persistence.Repositories;
using GAME.Application.Interfaces;
using GAME.Application.Services;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Features.Commands.Tower;

public sealed record RefreshTowerPeriodCommand(string UserId, string? EventCode = "TOWER_CLIMB") : ICommand<BaseResponse<bool>>;

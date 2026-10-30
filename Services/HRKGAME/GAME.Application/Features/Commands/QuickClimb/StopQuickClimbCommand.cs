using static GAME.Application.Features.Shared.QuickClimb.QuickClimbProjection;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using Core.TransactionalMessaging.Outbox;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Services;
using GAME.Application.Features.Shared.QuickClimb;

namespace GAME.Application.Features.Commands.QuickClimb;

public sealed record StopQuickClimbCommand(string UserId, string JobId) : ICommand<BaseResponse<QuickClimbJobStatusDto>>;

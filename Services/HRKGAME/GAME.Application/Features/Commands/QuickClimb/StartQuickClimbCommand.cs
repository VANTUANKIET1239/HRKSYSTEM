using static GAME.Application.Features.Shared.QuickClimb.QuickClimbProjection;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using Core.TransactionalMessaging.Outbox;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Application.Events;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Services;
using GAME.Application.Features.Shared.QuickClimb;

namespace GAME.Application.Features.Commands.QuickClimb;

public sealed record StartQuickClimbCommand(string UserId, StartQuickClimbRequestDto Request) : ICommand<BaseResponse<QuickClimbJobStatusDto>>;

using static GAME.Application.Features.Shared.QuickClimb.QuickClimbProjection;
using Core.Persistence.Repositories;
using Core.TransactionalMessaging.Inbox;
using Core.TransactionalMessaging.Outbox;
using Microsoft.Data.SqlClient;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Oservability.Tracing;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Services;
using GAME.Application.Features.Shared.QuickClimb;
using GAME.Application.Events;

namespace GAME.Application.Features.Commands.QuickClimb;

public sealed record ProcessQuickClimbFloorCommand(

    Guid MessageId,

    string JobId,

    string UserId,

    int ExpectedFloor,

    long ExpectedVersion,

    string WorkerId

)

    : ICommand<BaseResponse<bool>>;

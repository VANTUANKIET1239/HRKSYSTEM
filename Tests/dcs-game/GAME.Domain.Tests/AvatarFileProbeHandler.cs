using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Core.Application.DependencyInjection;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Common.Errors;
using Core.Messaging.Contracts;
using Core.RabbitMQ.Interfaces;
using Core.Web.Responses;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Features.Commands.Formation;
using GAME.Application.Features.Commands.CheckIn;
using GAME.Application.Features.Queries.CheckIn;
using GAME.Application.Features.Commands.QuickClimb;
using GAME.Application.Features.Commands.Tower;
using GAME.Application.Features.Queries.Formation;
using GAME.Application.Features.Queries.Player;
using GAME.Application.Features.Queries.Leaderboard;
using GAME.Application.DTOs.Leaderboard;
using GAME.Application.Features.Queries.Tower;
using GAME.Infrastructure.Messaging.Consumers;
using HRK.GAME.Controllers;
using HRK.GAME.Filters;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAME.Domain.Tests;

public sealed class AvatarFileProbeHandler(CqrsTransportProbe probe) : IRequestHandler<GetPlayerCustomAvatarQuery, BaseResponse<PlayerCustomAvatarDto?>>
{
    public Task<BaseResponse<PlayerCustomAvatarDto?>> Handle(GetPlayerCustomAvatarQuery request, CancellationToken ct)
    {
        probe.Requests.Add(request);
        return Task.FromResult(BaseResponse<PlayerCustomAvatarDto?>.SuccessResponse(request.PlayerId == 42 ? new() { ImageData = [1, 2, 3], ContentType = "image/png", ContentHash = "hash" } : null));
    }
}

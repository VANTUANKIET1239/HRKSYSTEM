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

public sealed class PublicHeroProbeHandler(CqrsTransportProbe probe) : Core.Application.CQRS.IQueryHandler<GetPublicPlayerHeroQuery, BaseResponse<PublicHeroDetailResponse>>
{
    public Task<BaseResponse<PublicHeroDetailResponse>> Handle(GetPublicPlayerHeroQuery request, CancellationToken ct)
    {
        probe.Requests.Add(request);
        return Task.FromResult(BaseResponse<PublicHeroDetailResponse>.SuccessResponse(new(request.HeroId, "Public hero", null, "RARE", 1, 1, 0, "Faction", "Class", new(), [], [])));
    }
}

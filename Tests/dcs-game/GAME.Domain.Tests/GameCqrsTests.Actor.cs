using System.Security.Claims;
using Core.Common.Errors;
using GAME.Application.Features.Commands.Battle;
using GAME.Application.Features.Commands.PlayerHeroes;
using GAME.Application.Features.Queries.Dungeon;
using GAME.Application.Features.Queries.Formation;
using GAME.Application.Features.Queries.Tower;
using GAME.Application.Interfaces;
using GAME.Application.Services;
using GAME.Domain.Entities;
using HRK.GAME.Configuration;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Xunit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Core.Web.Compression;
using GAME.Infrastructure.Scheduling;

namespace GAME.Domain.Tests;
public sealed partial class GameCqrsTests
{
    private sealed record Actor(string UserId) : IGameRequestContext;
}

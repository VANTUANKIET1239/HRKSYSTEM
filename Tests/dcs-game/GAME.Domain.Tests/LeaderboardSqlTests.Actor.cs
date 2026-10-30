using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Services;
using GAME.Application.Configuration;
using GAME.Application.Features.Commands.Dungeon;
using GAME.Application.Features.Commands.Inventory.ExpandCapacity;
using GAME.Application.Interfaces;
using GAME.Application.Features.Commands.Formation;
using GAME.Application.Templates;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace GAME.Domain.Tests;
public sealed partial class LeaderboardSqlTests
{
    private sealed record Actor(string UserId) : IGameRequestContext;
}

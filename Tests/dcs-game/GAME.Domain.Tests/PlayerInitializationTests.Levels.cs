using Core.Messaging.Contracts;
using Core.Persistence.Repositories;
using Core.TransactionalMessaging.Entities;
using Core.TransactionalMessaging.Inbox;
using Core.TransactionalMessaging.Outbox;
using GAME.Application.DTOs;
using GAME.Application.Events;
using GAME.Application.Features.Commands.Player;
using GAME.Application.Features.Queries.Player;
using GAME.Application.Interfaces;
using GAME.Application.Services;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using GAME.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAME.Domain.Tests;
public sealed partial class PlayerInitializationTests
{
    private sealed class Levels : ILevelExperienceService
    {
        public Task<LevelExperienceRequirement> GetHeroRequirementAsync(int level, CancellationToken ct = default) => Task.FromResult(new LevelExperienceRequirement(level, 123, false));
        public Task<LevelExperienceRequirement> GetPlayerRequirementAsync(int level, CancellationToken ct = default) => Task.FromResult(new LevelExperienceRequirement(level, 100, false));
    }
}

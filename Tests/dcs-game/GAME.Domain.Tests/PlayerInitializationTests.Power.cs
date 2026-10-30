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
    private sealed class Power : ICombatPowerService
    {
        public Task<List<CombatPowerConfigDto>> GetConfigsAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<CombatPowerConfigDto>());
        public int Calculate(CalculatedStatsDto stats, IReadOnlyCollection<CombatPowerConfigDto> configs) => 999;
        public Task<int> CalculateAsync(CalculatedStatsDto stats, CancellationToken cancellationToken = default) => Task.FromResult(999);
    }
}

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
    private sealed class FailedActivity : IPlayerActivityEvents
    {
        public void Raise(PlayerActivityEvent activity) => throw new InvalidOperationException("Injected failure after all grants.");
    }
}

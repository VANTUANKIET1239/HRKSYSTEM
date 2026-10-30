using System.Text.Json;
using Core.TransactionalMessaging.Entities;
using Core.TransactionalMessaging.Outbox;
using GAME.Application.Events;
using GAME.Application.Interfaces;
using GAME.Application.Services;
using GAME.Domain.Entities;
using GAME.Infrastructure.Messaging;
using GAME.Infrastructure.Scheduling;
using Xunit;

namespace GAME.Domain.Tests;
public partial class EventResetTests
{
    private sealed class ActivityCapture : IPlayerActivityEvents
    {
        public List<PlayerActivityEvent> Events { get; } = [];

        public void Raise(PlayerActivityEvent activity) => Events.Add(activity);
    }
}

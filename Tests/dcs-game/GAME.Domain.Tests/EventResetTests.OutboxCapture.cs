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
    private sealed class OutboxCapture : IOutboxWriter
    {
        public List<object> Messages { get; } = [];

        public OutboxMessage Add<T>(T message, string eventName, string publisherName, string routingKey, int eventVersion = 1, string? partitionKey = null, long? sequence = null)
        {
            Messages.Add(message!);
            return new();
        }
    }
}

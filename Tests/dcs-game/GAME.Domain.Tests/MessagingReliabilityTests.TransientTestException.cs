using Core.Messaging.Contracts;
using Core.RabbitMQ.Consumer;
using Core.RabbitMQ.Entities;
using Core.RabbitMQ.Interfaces;
using Core.TransactionalMessaging.Entities;
using Core.TransactionalMessaging.Outbox;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using GAME.Application.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAME.Domain.Tests;
public sealed partial class MessagingReliabilityTests
{
    private sealed class TransientTestException : Exception, ITransientMessageException;
}

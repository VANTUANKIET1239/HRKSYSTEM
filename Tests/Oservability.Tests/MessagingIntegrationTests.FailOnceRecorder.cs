using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Activity.Application;
using Activity.Infrastructure;
using Core.Messaging.Contracts;
using Core.RabbitMQ.DependencyInjection;
using Core.RabbitMQ.Entities;
using Core.RabbitMQ.Interfaces;
using Core.TransactionalMessaging.Entities;
using Core.TransactionalMessaging.Inbox;
using Core.TransactionalMessaging.Outbox;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oservability;
using Oservability.Correlation;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace Oservability.Tests;
public partial class MessagingIntegrationTests
{
    private sealed class FailOnceRecorder(ActivityStore store, RetryState state, ActivityDbContext db, IInboxExecutor inbox) : IActivityRecorder
    {
        public async Task RecordAsync(RecordPlayerActivityCommand command, CancellationToken ct)
        {
            if (Interlocked.Increment(ref state.Count) == 1)
                await inbox.ExecuteAsync<bool>(ActivityStore.ConsumerName, command.MessageId, async token =>
                {
                    db.PlayerActivities.Add(ActivityMapping.Map(command));
                    await db.SaveChangesAsync(token);
                    throw new IntentionalTransientFailure();
                }, ct);
            else
                await store.RecordAsync(command, ct);
        }
    }
}

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

[CollectionDefinition("Docker", DisableParallelization = true)]
public sealed class DockerCollection;

public sealed class DockerFactAttribute : FactAttribute
{
    public DockerFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("HRK_RUN_INTEGRATION") != "1")
            Skip = "Set HRK_RUN_INTEGRATION=1 to run isolated SQL Server/RabbitMQ containers.";
    }
}

[Collection("Docker")]
public class MessagingIntegrationTests
{
    [Fact]
    public void Outbox_CapturesRequestContextSeparatelyFromPartitionKey()
    {
        using var db = new ActivityDbContext(new DbContextOptionsBuilder<ActivityDbContext>()
            .UseSqlServer("Server=unused;Database=unused;Integrated Security=True").Options);
        var correlation = new CorrelationContext();
        correlation.Initialize("enhance-operation");
        using var activity = new System.Diagnostics.Activity("request").SetIdFormat(ActivityIdFormat.W3C).Start();
        var writer = new EfOutboxWriter<ActivityDbContext>(db, correlation);
        var id = Guid.NewGuid();
        var row = writer.Add(new { EventId = id }, "test.v1", "events", "test.v1", partitionKey: "job-123");
        Assert.Equal(id, row.Id);
        Assert.Equal("enhance-operation", row.CorrelationId);
        Assert.Equal("job-123", row.PartitionKey);
        Assert.Equal(activity.Id, row.TraceParent);
    }

    [DockerFact]
    public async Task RabbitRetryAndDuplicate_CommitOneActivityAndAckBothDeliveries()
    {
        await using var sql = new MsSqlBuilder().Build();
        await using var rabbit = new RabbitMqBuilder().WithImage("rabbitmq:4-management-alpine")
            .WithPortBinding(15672, true).Build();
        await Task.WhenAll(sql.StartAsync(), rabbit.StartAsync());
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["RabbitMq:HostName"] = rabbit.Hostname,
            ["RabbitMq:Port"] = rabbit.GetMappedPublicPort(5672).ToString(),
            ["RabbitMq:UserName"] = "rabbitmq", ["RabbitMq:Password"] = "rabbitmq",
            ["RabbitMq:Publishers:Events:Exchange"] = "test.activities",
            ["RabbitMq:Publishers:Events:ExchangeType"] = "topic",
            ["RabbitMq:Consumers:ActivityTimeline:Exchange"] = "test.activities",
            ["RabbitMq:Consumers:ActivityTimeline:Queue"] = "test.timeline",
            ["RabbitMq:Consumers:ActivityTimeline:RoutingKeys:0"] = "game.activity.recorded.v1",
            ["RabbitMq:Consumers:ActivityTimeline:Retry:MaxRetryCount"] = "1",
            ["RabbitMq:Consumers:ActivityTimeline:Retry:DelaysMs:0"] = "100"
        });
        builder.AddHrkObservability("IntegrationTest");
        builder.Services.AddDbContext<ActivityDbContext>(o => o.UseSqlServer(sql.GetConnectionString(),
            s => s.EnableRetryOnFailure()));
        builder.Services.AddScoped<IInboxStore, SqlServerInboxStore<ActivityDbContext>>();
        builder.Services.AddScoped<IInboxExecutor, InboxExecutor<ActivityDbContext>>();
        builder.Services.AddScoped<IOutboxStore, SqlServerOutboxStore<ActivityDbContext>>();
        builder.Services.AddScoped<ActivityStore>();
        var attempts = new RetryState();
        builder.Services.AddSingleton(attempts);
        builder.Services.AddScoped<IActivityRecorder, FailOnceRecorder>();
        builder.Services.AddRabbitMqMessaging(builder.Configuration).EnsureRabbitTopology()
            .AddNamedRabbitConsumer<PlayerActivityRecordedV1, ActivityMessageHandler>(ActivityStore.ConsumerName);
        await using var app = builder.Build();
        using (var scope = app.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<ActivityDbContext>().Database.EnsureCreatedAsync();
        await app.StartAsync();
        var id = Guid.NewGuid();
        var message = new PlayerActivityRecordedV1(id, "ItemEnhanced", 42, "test-user", "Item", "5",
            "HRK.GAME", DateTimeOffset.UtcNow, JsonSerializer.SerializeToElement(new { Success = true }));
        var publisher = app.Services.GetRequiredService<IMessagePublisher>();
        string expectedTraceId;
        using (var trace = new System.Diagnostics.Activity("enhance").SetIdFormat(ActivityIdFormat.W3C).Start())
        {
            expectedTraceId = trace.TraceId.ToHexString();
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ActivityDbContext>();
            var correlation = scope.ServiceProvider.GetRequiredService<CorrelationContext>();
            correlation.Initialize("enhance-test");
            new EfOutboxWriter<ActivityDbContext>(db, correlation).Add(message,
                ActivityContractNames.PlayerActivityRecordedV1, "Events", ActivityContractNames.PlayerActivityRecordedV1,
                partitionKey: "partition-is-not-correlation");
            await db.SaveChangesAsync();
            var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
            var claimed = Assert.Single(await store.ClaimAsync(1, "test-lock", 60, default));
            Assert.Equal(trace.Id, claimed.TraceParent);
            Assert.Equal("enhance-test", claimed.CorrelationId);
            var metadata = new RabbitPublishMetadata { MessageId = id.ToString("N"), CorrelationId = "enhance-test" };
            await publisher.PublishRawJsonAsync("Events", claimed.Payload, claimed.RoutingKey, metadata);
            Assert.Equal(1, await store.MarkPublishedAsync(claimed.Id, "test-lock", default));
            await publisher.PublishJsonAsync("Events", message, ActivityContractNames.PlayerActivityRecordedV1, metadata);
        }
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes("rabbitmq:rabbitmq")));
        while (true)
        {
            timeout.Token.ThrowIfCancellationRequested();
            using var response = await http.GetAsync($"http://{rabbit.Hostname}:{rabbit.GetMappedPublicPort(15672)}/api/queues/%2f/test.timeline", timeout.Token);
            response.EnsureSuccessStatusCode();
            using var data = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
            if (Volatile.Read(ref attempts.Count) >= 3 &&
                data.RootElement.TryGetProperty("messages", out var messages) && messages.GetInt32() == 0 &&
                data.RootElement.GetProperty("messages_unacknowledged").GetInt32() == 0) break;
            await Task.Delay(200, timeout.Token);
        }
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ActivityDbContext>();
            var row = Assert.Single(await db.PlayerActivities.ToListAsync());
            Assert.Equal(id, row.ActivityId);
            Assert.Equal("enhance-test", row.CorrelationId);
            Assert.Equal(expectedTraceId, row.TraceId);
            Assert.Single(await db.Set<InboxMessage>().ToListAsync());
            Assert.Equal(OutboxStatus.Published, (await db.Set<OutboxMessage>().SingleAsync()).Status);
        }
        // Invalid business contract is permanent and must reach DLQ without creating history.
        var invalidId = Guid.NewGuid();
        await publisher.PublishJsonAsync("Events", message with { EventId = invalidId, ActivityType = "Unsupported" },
            ActivityContractNames.PlayerActivityRecordedV1,
            new RabbitPublishMetadata { MessageId = invalidId.ToString("N"), CorrelationId = "invalid-test" });
        var channel = await app.Services.GetRequiredService<IRabbitMqChannel>().GetChannelAsync();
        RabbitMQ.Client.BasicGetResult? deadLetter;
        do
        {
            timeout.Token.ThrowIfCancellationRequested();
            deadLetter = await channel.BasicGetAsync("test.timeline.dlq", true, timeout.Token);
            if (deadLetter is null) await Task.Delay(100, timeout.Token);
        } while (deadLetter is null);
        Assert.Equal(invalidId.ToString("N"), deadLetter.BasicProperties.MessageId);
        Assert.Equal("invalid-test", deadLetter.BasicProperties.CorrelationId);
        using (var scope = app.Services.CreateScope())
            Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<ActivityDbContext>().PlayerActivities.CountAsync());
        await app.StopAsync();
    }

    public sealed class RetryState { public int Count; }
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
            else await store.RecordAsync(command, ct);
        }
    }
    private sealed class IntentionalTransientFailure()
        : Exception("Intentional failure after INSERT, before COMMIT"), ITransientMessageException;
}

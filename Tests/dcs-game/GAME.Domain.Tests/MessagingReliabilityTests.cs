using Core.Messaging.Contracts;
using Core.RabbitMQ.Consumer;
using Core.RabbitMQ.Entities;
using Core.RabbitMQ.Interfaces;
using Core.TransactionalMessaging.Entities;
using Core.TransactionalMessaging.Outbox;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using GAME.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAME.Domain.Tests;

public sealed class MessagingReliabilityTests
{
    [Fact]
    public void OutboxWriter_UsesContractEventIdAsStableMessageId()
    {
        var options = new DbContextOptionsBuilder<GameDbContext>()
            .UseSqlServer("Server=(local);Database=unused;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var db = new GameDbContext(options);
        var writer = new EfOutboxWriter<GameDbContext>(db);
        var eventId = Guid.NewGuid();
        var message = new ProcessQuickClimbFloorRequestedV1(
            eventId,
            "job-1",
            "user-1",
            7,
            3);

        var outbox = writer.Add(
            message,
            MessagingContractNames.QuickClimbFloorRequestedV1,
            MessagingPublisherNames.GameEvents,
            MessagingContractNames.QuickClimbFloorRequestedV1,
            partitionKey: message.JobId,
            sequence: message.Version);

        Assert.Equal(eventId, outbox.Id);
        Assert.Equal(OutboxStatus.Pending, outbox.Status);
        Assert.Equal("job-1", outbox.PartitionKey);
        Assert.Contains("\"expectedFloor\":7", outbox.Payload);
    }

    [Fact]
    public void ProcessedMessage_UsesConsumerAndMessageCompositeKey()
    {
        var options = new DbContextOptionsBuilder<GameDbContext>()
            .UseSqlServer("Server=(local);Database=unused;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var db = new GameDbContext(options);

        var key = db.Model.FindEntityType(typeof(InboxMessage))!.FindPrimaryKey()!;

        Assert.Equal(
            [nameof(InboxMessage.ConsumerName), nameof(InboxMessage.MessageId)],
            key.Properties.Select(property => property.Name));
    }

    [Fact]
    public void TransactionalMessaging_UsesServiceOwnedTablesAndInboxCleanupIndex()
    {
        var options = new DbContextOptionsBuilder<GameDbContext>()
            .UseSqlServer("Server=(local);Database=unused;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var db = new GameDbContext(options);

        var outbox = db.Model.FindEntityType(typeof(OutboxMessage))!;
        var inbox = db.Model.FindEntityType(typeof(InboxMessage))!;

        Assert.Equal("HRK_OutboxMessages", outbox.GetTableName());
        Assert.Equal("HRK_ProcessedMessages", inbox.GetTableName());
        Assert.Contains(
            inbox.GetIndexes(),
            index => index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(InboxMessage.ProcessedAt)]));
    }

    [Fact]
    public void TowerQuickClimb_DailyLimitDefaultsToThreeAndCanBeConfiguredFromDatabaseJson()
    {
        Assert.Equal(3, TowerRules.Parse(null).QuickClimbDailyLimit);
        Assert.Equal(5, TowerRules.Parse("{\"quickClimbDailyLimit\":5}").QuickClimbDailyLimit);
    }

    [Fact]
    public void TowerQuickClimb_PersistsDailyUsageAndJobRunNumber()
    {
        var options = new DbContextOptionsBuilder<GameDbContext>()
            .UseSqlServer("Server=(local);Database=unused;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var db = new GameDbContext(options);

        Assert.NotNull(db.Model.FindEntityType(typeof(HrkPlayerEventPeriodProgress))!
            .FindProperty(nameof(HrkPlayerEventPeriodProgress.QuickClimbRunsUsed)));
        Assert.NotNull(db.Model.FindEntityType(typeof(HrkTowerQuickClimbJob))!
            .FindProperty(nameof(HrkTowerQuickClimbJob.DailyRunNumber)));
    }

    [Fact]
    public void RetryOptions_RespectConfiguredRetryCountAndDelays()
    {
        var options = new RabbitMqOptions.ConsumerOptions
        {
            Retry = new RabbitMqOptions.RetryOptions
            {
                MaxRetryCount = 3,
                DelaysMs = [1000, 5000, 15000, 30000]
            }
        };

        Assert.Equal([1000, 5000, 15000], options.ResolveRetryDelays());
    }

    [Fact]
    public void RetryOptions_RepeatLastDelayWhenRetryCountExceedsConfiguredBuckets()
    {
        var options = new RabbitMqOptions.ConsumerOptions
        {
            Retry = new RabbitMqOptions.RetryOptions
            {
                MaxRetryCount = 4,
                DelaysMs = [1000, 5000]
            }
        };

        Assert.Equal([1000, 5000, 5000, 5000], options.ResolveRetryDelays());
    }

    [Theory]
    [InlineData(typeof(TransientTestException), MessageFailureCategory.Transient)]
    [InlineData(typeof(PermanentTestException), MessageFailureCategory.Permanent)]
    [InlineData(typeof(DuplicateTestException), MessageFailureCategory.Duplicate)]
    public void FailureClassifier_UsesExplicitExceptionMarkers(
        Type exceptionType,
        MessageFailureCategory expected)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;
        var classifier = new DefaultMessageFailureClassifier();

        Assert.Equal(expected, classifier.Classify(exception));
    }

    private sealed class TransientTestException : Exception, ITransientMessageException;

    private sealed class PermanentTestException : Exception, IPermanentMessageException;

    private sealed class DuplicateTestException : Exception, IDuplicateMessageException;
}

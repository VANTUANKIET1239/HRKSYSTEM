using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Core.Messaging.Contracts;
using GAME.Application.Events;

namespace GAME.Infrastructure.Messaging;

public sealed record MappedPlayerActivity(PlayerActivityRecordedV1 Message, string? PartitionKey, long? Sequence);

public sealed class PlayerActivityEventMapper
{
    public MappedPlayerActivity Map(PlayerActivityEvent activity)
    {
        return activity switch
        {
            ItemEnhancedEvent e => Create(e, "ItemEnhanced", "Item", e.ItemId.ToString(),
                $"enhance:{e.PlayerId}:{e.RequestId:N}", new
                {
                    e.RequestId, e.ItemId, e.ItemTemplateId, e.OldLevel, e.NewLevel, e.Success, e.GoldCost
                }),
            QuickClimbStartedEvent e => Create(e, "QuickClimbStarted", "QuickClimb", e.JobId,
                $"quickclimb:{e.JobId}:started", new
                {
                    Status = "QUEUED", e.StartFloor, e.CurrentFloor, e.TargetFloor,
                    e.ClearedFloorsCount, StopReason = (string?)null, e.Version
                }, e.JobId, e.Version),
            QuickClimbEndedEvent e => Create(e, ResolveEndType(e.Status), "QuickClimb", e.JobId,
                $"quickclimb:{e.JobId}:ended", new
                {
                    e.Status, e.StartFloor, e.CurrentFloor, e.TargetFloor,
                    e.ClearedFloorsCount, e.StopReason, e.Version
                }, e.JobId, e.Version),
            _ => throw new ArgumentException($"Unsupported activity event type: {activity.GetType().Name}")
        };
    }

    private static string ResolveEndType(string status) => status switch
    {
        "COMPLETED" or "STOPPED_DEFEAT" or "EXPIRED" => "QuickClimbCompleted",
        "ERROR" => "QuickClimbFailed",
        "CANCELLED" => "QuickClimbCancelled",
        _ => throw new ArgumentException("Only terminal jobs can emit QuickClimbEnded.")
    };

    private static MappedPlayerActivity Create(PlayerActivityEvent e, string type, string entityType,
        string entityId, string key, object payload, string? partition = null, long? sequence = null)
    {
        // Namespaced identity survives execution-strategy retries for the same business event.
        var id = new Guid(SHA256.HashData(Encoding.UTF8.GetBytes("HRK.GAME.activity.v1:" + key)).AsSpan(0, 16));
        return new(new(id, type, e.PlayerId, e.UserId, entityType, entityId, "HRK.GAME",
            DateTimeOffset.UtcNow, JsonSerializer.SerializeToElement(payload)), partition, sequence);
    }
}

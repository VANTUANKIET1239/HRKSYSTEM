namespace GAME.Application.Events;

public sealed record ItemEnhancedEvent(
    long PlayerId, string UserId, Guid RequestId, long ItemId, long ItemTemplateId,
    int OldLevel, int NewLevel, bool Success, long GoldCost) : PlayerActivityEvent(PlayerId, UserId);

namespace GAME.Application.Events;

public sealed record PlayerInitializedEvent(long PlayerId, string UserId, Guid RegistrationEventId,
    long ConfigId, int ConfigVersion, object Rewards) : PlayerActivityEvent(PlayerId, UserId);

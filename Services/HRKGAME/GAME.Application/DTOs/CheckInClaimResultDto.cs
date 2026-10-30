namespace GAME.Application.DTOs;
public sealed record CheckInClaimResultDto(Guid RequestId, long CalendarId, DateOnly CheckInDate,
    DateTime ClaimedAtUtc, int ConfigVersion, bool AlreadyClaimed, IReadOnlyList<GenericRewardItemDto> Rewards);

namespace GAME.Application.DTOs;
public sealed record CheckInDayDto(int DayOfMonth, string Status, IReadOnlyList<GenericRewardItemDto> Rewards, DateTime? ClaimedAtUtc);

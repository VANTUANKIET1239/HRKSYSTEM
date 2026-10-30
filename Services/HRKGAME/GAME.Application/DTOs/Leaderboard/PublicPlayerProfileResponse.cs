namespace GAME.Application.DTOs.Leaderboard;

/// <summary>Read-only public profile. The displayed formation is current, not a historical peak snapshot.</summary>
public sealed record PublicPlayerProfileResponse(long PlayerId, string PlayerName, int Level, string AvatarType,
    string? AvatarUrl, long AvatarVersion, long CurrentFormationPower, long PeakFormationPower,
    DateTimeOffset? PeakPowerAchievedAtUtc, long TotalDiamondsSpent, DateTimeOffset? TrackingStartedAtUtc,
    string FormationName, IReadOnlyList<PublicHeroSummaryResponse> Heroes);

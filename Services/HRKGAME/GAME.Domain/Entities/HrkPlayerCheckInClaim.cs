namespace GAME.Domain.Entities;

public sealed class HrkPlayerCheckInClaim
{
    public long Id { get; set; }
    public long PlayerId { get; set; }
    public long CalendarId { get; set; }
    public DateOnly CheckInDate { get; set; }
    public Guid RequestId { get; set; }
    public int ConfigVersion { get; set; }
    public DateTime ClaimedAtUtc { get; set; }
    // Full receipt: retries and historical calendar cells use the reward actually granted.
    public string ResultJson { get; set; } = null!;
}

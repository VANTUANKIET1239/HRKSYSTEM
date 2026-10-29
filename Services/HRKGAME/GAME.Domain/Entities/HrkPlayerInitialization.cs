namespace GAME.Domain.Entities;

// Permanent business receipt: inbox retention must never allow a second starter grant.
public sealed class HrkPlayerInitialization
{
    public long Id { get; set; }
    public string UserId { get; set; } = "";
    public long? PlayerId { get; set; }
    public long? ConfigId { get; set; }
    public Guid RegistrationEventId { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}

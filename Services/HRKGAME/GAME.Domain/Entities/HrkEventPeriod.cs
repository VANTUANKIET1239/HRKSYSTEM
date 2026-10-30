namespace GAME.Domain.Entities;

public class HrkEventPeriod
{
    public long Id { get; set; }
    public int EventId { get; set; }
    public string PeriodKey { get; set; } = null!; // e.g. "2026-10-01"
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public string? ChestSnapshotJson { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public virtual HrkGameEvent Event { get; set; } = null!;
    public virtual ICollection<HrkPlayerEventPeriodProgress> PlayerProgresses { get; set; } = new List<HrkPlayerEventPeriodProgress>();
}

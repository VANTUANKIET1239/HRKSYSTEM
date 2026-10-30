namespace GAME.Domain.Entities;

public class HrkPlayerTowerRun
{
    public long Id { get; set; }
    public long PlayerId { get; set; }
    public long EventPeriodId { get; set; }
    public int RunNumber { get; set; }
    public int StartFloor { get; set; } = 1;
    public int EndFloor { get; set; } = 1;
    public int LivesRemaining { get; set; } = 3;
    public string Status { get; set; } = "IN_PROGRESS"; // IN_PROGRESS, DEFEATED, COMPLETED, EXPIRED
    public DateTime StartAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndAtUtc { get; set; }

    public virtual HrkPlayer Player { get; set; } = null!;
    public virtual HrkEventPeriod EventPeriod { get; set; } = null!;
}

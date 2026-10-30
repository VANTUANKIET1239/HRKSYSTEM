namespace GAME.Domain.Entities;

public class HrkPlayerEventAllTimeRecord
{
    public long Id { get; set; }
    public long PlayerId { get; set; }
    public int EventId { get; set; }
    public int HighestFloorAllTime { get; set; } = 0;
    public int TotalClears { get; set; } = 0;
    public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

    public virtual HrkPlayer Player { get; set; } = null!;
    public virtual HrkGameEvent Event { get; set; } = null!;
}

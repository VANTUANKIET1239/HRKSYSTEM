namespace GAME.Domain.Entities;

public class HrkPlayerTowerBattle
{
    public long Id { get; set; }
    public string BattleId { get; set; } = null!;
    public long PlayerId { get; set; }
    public long EventPeriodId { get; set; }
    public int FloorNumber { get; set; }
    public long? RunId { get; set; }
    public long? QuickClimbJobId { get; set; }
    public string Result { get; set; } = null!; // VICTORY, DEFEAT
    public int LivesBefore { get; set; }
    public int LivesAfter { get; set; }
    public int PlayerPower { get; set; }
    public int EnemyPower { get; set; }
    public int RandomSeed { get; set; }
    public string? ResultJson { get; set; }
    public string? HeroStatisticsJson { get; set; }
    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

    public virtual HrkPlayer Player { get; set; } = null!;
    public virtual HrkEventPeriod EventPeriod { get; set; } = null!;
}

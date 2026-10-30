namespace GAME.Domain.Entities;

public class HrkPlayerDungeonStageProgress
{
    public long Id { get; set; }
    public long PlayerId { get; set; }
    public int StageId { get; set; }
    public int ClearCount { get; set; }
    public int BestTurns { get; set; }
    public int BestStars { get; set; }
    public decimal BestRemainingHpRate { get; set; }
    public DateTime FirstClearedOn { get; set; }
    public DateTime LastClearedOn { get; set; }
    public virtual HrkPlayer Player { get; set; } = null!;
    public virtual HrkDungeonStage Stage { get; set; } = null!;
}

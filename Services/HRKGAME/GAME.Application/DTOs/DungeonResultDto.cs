namespace GAME.Application.DTOs;

public sealed class DungeonResultDto
{
    public long RunId { get; set; }
    public string Result { get; set; } = null!;
    public int EarnedStars { get; set; }
    public int PreviousBestStars { get; set; }
    public int BestStars { get; set; }
    public bool IsNewStarRecord { get; set; }
    public decimal RemainingHpRate { get; set; }
    public int TotalMapStars { get; set; }
    public int StaminaSpent { get; set; }
    public int StaminaRemaining { get; set; }
    public long GoldGained { get; set; }
    public bool IsFirstClear { get; set; }
    public int PlayerExpGained { get; set; }
    public int OldPlayerLevel { get; set; }
    public int NewPlayerLevel { get; set; }
    public int OldPlayerExp { get; set; }
    public int NewPlayerExp { get; set; }
    public int? UnlockedStageId { get; set; }
    public List<HeroExpResultDto> Heroes { get; set; } = new();
    public DungeonDroppedEquipmentDto? DroppedEquipment { get; set; }
    public bool IsBagFull { get; set; }
}

namespace GAME.Application.DTOs;

public class StartTowerBattleResultDto
{
    public StartBattleResultDto Battle { get; set; } = null!;
    public int FloorNumber { get; set; }
    public int? NextFloorNumber { get; set; }
    public int LivesBefore { get; set; }
    public int LivesAfter { get; set; }
    public bool IsVictory { get; set; }
    public bool IsRunEnded { get; set; }
    public bool IsTowerCompleted { get; set; }
    public bool IsMilestoneChestUnlocked { get; set; }
    public TowerChestDto? UnlockedChest { get; set; }
    public List<GenericRewardItemDto> EarnedRewards { get; set; } = new();
    public List<GenericRewardItemDto> PendingRewards { get; set; } = new();
    public int PlayerExpGained { get; set; }
    public int HeroExpGained { get; set; }
    public int NewPlayerLevel { get; set; }
    public int NewPlayerExp { get; set; }
    public List<HeroExpResultDto> Heroes { get; set; } = new();
}

namespace GAME.Domain.Entities;

public class HrkHeroStarUpgradeConfig
{
    public int Id { get; set; }
    public int RarityId { get; set; }
    public int CurrentStar { get; set; }
    public int NextStar { get; set; }
    public long GoldCost { get; set; }
    public int UniversalStarStoneItemTemplateId { get; set; }
    public int UniversalStarStoneQuantity { get; set; }
    public int HeroStoneQuantity { get; set; }
    public decimal GrowthBonusPercent { get; set; }
    public int ExtraAttributeRollCount { get; set; } = 1;
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
}

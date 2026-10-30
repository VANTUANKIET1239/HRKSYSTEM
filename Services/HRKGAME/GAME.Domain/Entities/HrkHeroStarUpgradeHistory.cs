namespace GAME.Domain.Entities;

public class HrkHeroStarUpgradeHistory
{
    public long Id { get; set; }
    public Guid RequestId { get; set; }
    public long PlayerId { get; set; }
    public long PlayerHeroId { get; set; }
    public int OldStar { get; set; }
    public int NewStar { get; set; }
    public long GoldCost { get; set; }
    public int UniversalStoneQuantity { get; set; }
    public int HeroStoneItemTemplateId { get; set; }
    public int HeroStoneQuantity { get; set; }
    public string RolledAttributesJson { get; set; } = "[]";
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}

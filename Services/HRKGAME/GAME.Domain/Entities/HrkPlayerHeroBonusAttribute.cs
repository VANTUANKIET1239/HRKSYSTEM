namespace GAME.Domain.Entities;

public class HrkPlayerHeroBonusAttribute
{
    public long Id { get; set; }
    public long PlayerHeroId { get; set; }
    public HrkPlayerHero PlayerHero { get; set; } = null!;
    public int UnlockedAtStar { get; set; }
    public int AttributeTypeId { get; set; }
    public decimal Value { get; set; }
    public bool IsPercentage { get; set; }
    public Guid RollSeed { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}

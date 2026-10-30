namespace GAME.Domain.Entities;

public class HrkHeroStarAttributePool
{
    public int Id { get; set; }
    public int RarityId { get; set; }
    public int AttributeTypeId { get; set; }
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public int Weight { get; set; }
    public bool IsPercentage { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
}

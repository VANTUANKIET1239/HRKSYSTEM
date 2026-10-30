namespace GAME.Domain.Entities;

public class HrkHeroStoneConfig
{
    public int Id { get; set; }
    public int HeroTemplateId { get; set; }
    public int ItemTemplateId { get; set; }
    public int DuplicateConversionQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
}

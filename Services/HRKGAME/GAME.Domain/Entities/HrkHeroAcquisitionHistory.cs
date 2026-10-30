namespace GAME.Domain.Entities;

public class HrkHeroAcquisitionHistory
{
    public long Id { get; set; }
    public Guid RequestId { get; set; }
    public long PlayerId { get; set; }
    public int HeroTemplateId { get; set; }
    public string SourceType { get; set; } = null!;
    public string? SourceReferenceId { get; set; }
    public bool WasDuplicate { get; set; }
    public int ConvertedHeroStoneQuantity { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}

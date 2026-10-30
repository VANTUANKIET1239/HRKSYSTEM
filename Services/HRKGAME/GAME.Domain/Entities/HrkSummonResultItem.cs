namespace GAME.Domain.Entities;

public class HrkSummonResultItem
{
    public long Id { get; set; }
    public long BatchId { get; set; }
    public int DrawIndex { get; set; }
    public int HeroTemplateId { get; set; }
    public bool WasDuplicate { get; set; }
    public int ConvertedStoneQuantity { get; set; }
    public HrkSummonBatch Batch { get; set; } = null!;
}

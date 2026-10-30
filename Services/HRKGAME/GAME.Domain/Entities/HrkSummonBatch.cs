namespace GAME.Domain.Entities;

public class HrkSummonBatch
{
    public long Id { get; set; }
    public long PlayerId { get; set; }
    public Guid RequestId { get; set; }
    public string PoolCode { get; set; } = null!;
    public int Count { get; set; }
    public string Currency { get; set; } = null!;
    public long Cost { get; set; }
    // Immutable response snapshot allows replay even if catalog/configuration changes.
    public string ResultJson { get; set; } = null!;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public ICollection<HrkSummonResultItem> Items { get; set; } = new List<HrkSummonResultItem>();
}

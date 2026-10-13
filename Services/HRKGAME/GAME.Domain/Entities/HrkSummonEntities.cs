namespace GAME.Domain.Entities;

public class HrkSummonPool
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Currency { get; set; } = null!;
    public long SingleCost { get; set; }
    public long TenCost { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int DisplayOrder { get; set; }
    public ICollection<HrkSummonPoolEntry> Entries { get; set; } = new List<HrkSummonPoolEntry>();
}

public class HrkSummonPoolEntry
{
    public int Id { get; set; }
    public int PoolId { get; set; }
    public int HeroTemplateId { get; set; }
    public int Weight { get; set; }
    public bool IsEnabled { get; set; } = true;
    public HrkSummonPool Pool { get; set; } = null!;
    public HrkHeroTemplate HeroTemplate { get; set; } = null!;
}

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

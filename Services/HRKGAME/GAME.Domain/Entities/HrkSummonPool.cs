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

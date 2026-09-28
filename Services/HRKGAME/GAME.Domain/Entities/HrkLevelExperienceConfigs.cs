namespace GAME.Domain.Entities;

public sealed class HrkHeroLevelConfig
{
    public int Level { get; set; }
    public int RequiredExp { get; set; }
    public long GoldCost { get; set; }
    public int MaterialCost { get; set; }
    public int PowerIncrease { get; set; }
    public bool IsMaxLevel { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
}

public sealed class HrkPlayerLevelConfig
{
    public int Level { get; set; }
    public int RequiredExp { get; set; }
    public bool IsMaxLevel { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
}

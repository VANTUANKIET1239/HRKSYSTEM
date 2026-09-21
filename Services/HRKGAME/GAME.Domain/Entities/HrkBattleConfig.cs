namespace GAME.Domain.Entities;

public sealed class HrkBattleConfig
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public decimal Value { get; set; }
    public string ValueType { get; set; } = "NUMBER";
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
}

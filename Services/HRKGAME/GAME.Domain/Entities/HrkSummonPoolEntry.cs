namespace GAME.Domain.Entities;

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

namespace GAME.Domain.Entities;

public class HrkTowerFloorEnemy
{
    public long Id { get; set; }
    public int TowerFloorId { get; set; }
    public int Position { get; set; }
    public int HeroTemplateId { get; set; }
    public int Level { get; set; }
    public byte Stars { get; set; } = 1;
    public string? DisplayName { get; set; }
    public string? ImagePath { get; set; }
    public decimal StatMultiplier { get; set; } = 1.0m;

    public virtual HrkTowerFloor TowerFloor { get; set; } = null!;
    public virtual HrkHeroTemplate HeroTemplate { get; set; } = null!;
}

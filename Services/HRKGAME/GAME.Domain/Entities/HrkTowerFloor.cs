namespace GAME.Domain.Entities;

public class HrkTowerFloor
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int FloorNumber { get; set; }
    public string Name { get; set; } = null!;
    public string FloorType { get; set; } = "NORMAL"; // NORMAL, ELITE, BOSS
    public int RecommendedPower { get; set; }
    public string? BackgroundPath { get; set; }
    public int? RepresentativeHeroTemplateId { get; set; }
    public decimal StatMultiplier { get; set; } = 1.0m;
    public decimal HpMultiplier { get; set; } = 1.0m;
    public decimal AtkMultiplier { get; set; } = 1.0m;
    public decimal DefMultiplier { get; set; } = 1.0m;
    public long GoldReward { get; set; }
    public int PlayerExpReward { get; set; }
    public int HeroExpReward { get; set; }
    public string? RewardsJson { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual HrkGameEvent Event { get; set; } = null!;
    public virtual HrkHeroTemplate? RepresentativeHeroTemplate { get; set; }
    public virtual ICollection<HrkTowerFloorEnemy> Enemies { get; set; } = new List<HrkTowerFloorEnemy>();
}

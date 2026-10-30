namespace GAME.Domain.Entities;

public class HrkDungeonMapStarChest
{
    public int Id { get; set; }
    public int DungeonMapId { get; set; }
    public int RequiredStars { get; set; }
    public long GoldReward { get; set; }
    public int DiamondReward { get; set; }
    public int UpgradeMaterialsReward { get; set; }
    public int? GuaranteedItemTemplateId { get; set; }
    public int DisplayOrder { get; set; } = 1;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public virtual HrkDungeonMap DungeonMap { get; set; } = null!;
    public virtual HrkItemTemplate? GuaranteedItemTemplate { get; set; }
    public virtual ICollection<HrkPlayerDungeonStarChestClaim> Claims { get; set; } = new List<HrkPlayerDungeonStarChestClaim>();
}

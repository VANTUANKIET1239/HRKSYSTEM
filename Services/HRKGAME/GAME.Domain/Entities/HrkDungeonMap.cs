namespace GAME.Domain.Entities;

public class HrkDungeonMap
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string ImagePath { get; set; } = null!;
    public string BackgroundPath { get; set; } = null!;
    public int DisplayOrder { get; set; }
    public int RequiredPlayerLevel { get; set; } = 1;
    public int? PreviousMapId { get; set; }
    public int? MaxEquipmentRarityId { get; set; }
    public bool IsActive { get; set; } = true;
    public virtual HrkDungeonMap? PreviousMap { get; set; }
    public virtual HrkRarity? MaxEquipmentRarity { get; set; }
    public virtual ICollection<HrkDungeonStage> Stages { get; set; } = new List<HrkDungeonStage>();
    public virtual ICollection<HrkDungeonMapStarChest> StarChests { get; set; } = new List<HrkDungeonMapStarChest>();
    public virtual ICollection<HrkDungeonStarRatingConfig> StarRatingConfigs { get; set; } = new List<HrkDungeonStarRatingConfig>();
}

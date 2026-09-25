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

public class HrkDungeonStarRatingConfig
{
    public int Id { get; set; }
    public int? DungeonMapId { get; set; }
    public int Stars { get; set; }
    public decimal MinRemainingHpRate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
    public virtual HrkDungeonMap? DungeonMap { get; set; }
}

public class HrkDungeonStage
{
    public int Id { get; set; }
    public int DungeonMapId { get; set; }
    public int StageNumber { get; set; }
    public string Name { get; set; } = null!;
    public string StageType { get; set; } = "NORMAL";
    public int StaminaCost { get; set; } = 6;
    public int RecommendedPower { get; set; }
    public long GoldReward { get; set; }
    public int PlayerExpReward { get; set; }
    public int HeroExpReward { get; set; }
    public long FirstClearGoldReward { get; set; }
    public string? BackgroundPath { get; set; }
    public bool IsActive { get; set; } = true;
    public virtual HrkDungeonMap DungeonMap { get; set; } = null!;
    public virtual ICollection<HrkDungeonStageEnemy> Enemies { get; set; } = new List<HrkDungeonStageEnemy>();
    public virtual ICollection<HrkDungeonStageDropPool> DropPools { get; set; } = new List<HrkDungeonStageDropPool>();
}

public class HrkDungeonStageEnemy
{
    public int Id { get; set; }
    public int StageId { get; set; }
    public int Position { get; set; }
    public int HeroTemplateId { get; set; }
    public string? DisplayName { get; set; }
    public string? ImagePath { get; set; }
    public int Level { get; set; }
    public byte Stars { get; set; }
    public decimal StatMultiplier { get; set; } = 1m;
    public bool IsBoss { get; set; }
    public virtual HrkDungeonStage Stage { get; set; } = null!;
    public virtual HrkHeroTemplate HeroTemplate { get; set; } = null!;
}

public class HrkPlayerDungeonStageProgress
{
    public long Id { get; set; }
    public long PlayerId { get; set; }
    public int StageId { get; set; }
    public int ClearCount { get; set; }
    public int BestTurns { get; set; }
    public int BestStars { get; set; }
    public decimal BestRemainingHpRate { get; set; }
    public DateTime FirstClearedOn { get; set; }
    public DateTime LastClearedOn { get; set; }
    public virtual HrkPlayer Player { get; set; } = null!;
    public virtual HrkDungeonStage Stage { get; set; } = null!;
}

public class HrkDungeonRun
{
    public long Id { get; set; }
    public string BattleId { get; set; } = null!;
    public string ClientRequestId { get; set; } = null!;
    public long PlayerId { get; set; }
    public int StageId { get; set; }
    public string Result { get; set; } = null!;
    public int StaminaSpent { get; set; }
    public int PlayerPower { get; set; }
    public int EnemyPower { get; set; }
    public string? FormationCode { get; set; }
    public string? FormationSnapshotJson { get; set; }
    public string? ParticipantHeroIdsJson { get; set; }
    public int RandomSeed { get; set; }
    public long GoldReward { get; set; }
    public int PlayerExpReward { get; set; }
    public int HeroExpReward { get; set; }
    public DateTime StartedOn { get; set; } = DateTime.UtcNow;
    public DateTime CompletedOn { get; set; } = DateTime.UtcNow;
    public virtual HrkPlayer Player { get; set; } = null!;
    public virtual HrkDungeonStage Stage { get; set; } = null!;
}

public class HrkDungeonStageDropPool
{
    public int Id { get; set; }
    public int StageId { get; set; }
    public int ItemTemplateId { get; set; }
    public decimal DropRate { get; set; }
    public int Weight { get; set; } = 100;
    public int MinQuantity { get; set; } = 1;
    public int MaxQuantity { get; set; } = 1;
    public bool IsFirstClearOnly { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public virtual HrkDungeonStage Stage { get; set; } = null!;
    public virtual HrkItemTemplate ItemTemplate { get; set; } = null!;
}

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

public class HrkPlayerDungeonStarChestClaim
{
    public long Id { get; set; }
    public long PlayerId { get; set; }
    public int StarChestId { get; set; }
    public DateTime ClaimedOn { get; set; } = DateTime.UtcNow;
    public virtual HrkPlayer Player { get; set; } = null!;
    public virtual HrkDungeonMapStarChest StarChest { get; set; } = null!;
}

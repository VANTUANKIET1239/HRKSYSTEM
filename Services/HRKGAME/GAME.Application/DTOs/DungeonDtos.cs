namespace GAME.Application.DTOs;

public class DungeonMapDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string ImagePath { get; set; } = null!;
    public string BackgroundPath { get; set; } = null!;
    public int RequiredPlayerLevel { get; set; }
    public int ClearedStages { get; set; }
    public int TotalStages { get; set; }
    public string State { get; set; } = null!;
}

public sealed class DungeonMapDetailDto : DungeonMapDto
{
    public int TotalStars { get; set; }
    public List<DungeonStarChestDto> StarChests { get; set; } = new();
    public List<DungeonStageDto> Stages { get; set; } = new();
}

public sealed class DungeonStageDto
{
    public int Id { get; set; }
    public int StageNumber { get; set; }
    public string Name { get; set; } = null!;
    public string StageType { get; set; } = null!;
    public string State { get; set; } = null!;
    public int BestStars { get; set; }
    public int StaminaCost { get; set; }
    public int RecommendedPower { get; set; }
    public int PlayerPower { get; set; }
    public int EnemyPower { get; set; }
    public long GoldReward { get; set; }
    public long FirstClearGoldReward { get; set; }
    public int PlayerExpReward { get; set; }
    public int HeroExpReward { get; set; }
    public string? BackgroundPath { get; set; }
    public List<DungeonEnemyDto> Enemies { get; set; } = new();
    public List<DungeonPossibleDropDto> PossibleDrops { get; set; } = new();
}

public sealed class DungeonPossibleDropDto
{
    public int ItemTemplateId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? ImagePath { get; set; }
    public string RarityCode { get; set; } = null!;
    public string RarityName { get; set; } = null!;
    public string? RarityColorHex { get; set; }
    public string CategoryCode { get; set; } = null!;
    public string? CategoryName { get; set; }
    public decimal DropRatePercent { get; set; }
}

public sealed class EquipmentRolledAttributeDto
{
    public int AttributeTypeId { get; set; }
    public string AttributeCode { get; set; } = null!;
    public string AttributeName { get; set; } = null!;
    public bool IsPercentage { get; set; }
    public string ValueType => IsPercentage ? "PERCENT" : "FLAT";
    public decimal BaseRolledValue { get; set; }
    public decimal EnhancementValue { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal RollMinValue { get; set; }
    public decimal RollMaxValue { get; set; }
    public decimal MinValue => RollMinValue;
    public decimal MaxValue => RollMaxValue;
    public decimal RollQualityPercent { get; set; }
    public decimal RollPercent => RollQualityPercent;
    public int DisplayOrder { get; set; }
}

public sealed class DungeonDroppedEquipmentDto
{
    public long InventoryItemId { get; set; }
    public int ItemTemplateId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? ImagePath { get; set; }
    public string RarityCode { get; set; } = null!;
    public string RarityName { get; set; } = null!;
    public string? RarityColorHex { get; set; }
    public string CategoryCode { get; set; } = null!;
    public string? CategoryName { get; set; }
    public int Count { get; set; } = 1;
    public decimal EnhancementGrowthPercent { get; set; }
    public decimal EnhancementGrowthMinPercent { get; set; }
    public decimal EnhancementGrowthMaxPercent { get; set; }
    public decimal OverallRollPercent { get; set; }
    public List<EquipmentRolledAttributeDto> RolledAttributes { get; set; } = new();
    public Dictionary<string, decimal> CurrentStats { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int CombatPower { get; set; }
}

public sealed class DungeonStarChestDto
{
    public int Id { get; set; }
    public int DungeonMapId { get; set; }
    public int RequiredStars { get; set; }
    public string State { get; set; } = "LOCKED"; // LOCKED, CLAIMABLE, CLAIMED
    public long GoldReward { get; set; }
    public int DiamondReward { get; set; }
    public int UpgradeMaterialsReward { get; set; }
    public DungeonPossibleDropDto? GuaranteedItem { get; set; }
    public int DisplayOrder { get; set; }
    public string? Description { get; set; }
    public DateTime? ClaimedOn { get; set; }
}

public sealed class ClaimStarChestResultDto
{
    public int ChestId { get; set; }
    public int RequiredStars { get; set; }
    public long GoldGained { get; set; }
    public int DiamondsGained { get; set; }
    public int UpgradeMaterialsGained { get; set; }
    public DungeonDroppedEquipmentDto? DroppedEquipment { get; set; }
    public DungeonStarChestDto Chest { get; set; } = null!;
}

public sealed class DungeonEnemyDto
{
    public int Position { get; set; }
    public string Name { get; set; } = null!;
    public string ImagePath { get; set; } = null!;
    public int Level { get; set; }
    public byte Stars { get; set; }
    public bool IsBoss { get; set; }
    public int Power { get; set; }
}

public sealed class DungeonStaminaDto
{
    public int Current { get; set; }
    public int Max { get; set; }
    public int NextRecoverySeconds { get; set; }
    public int PurchaseCount { get; set; }
    public int NextPurchaseCost { get; set; }
    public int PurchaseAmount { get; set; } = 50;
}

public sealed class StartDungeonStageRequestDto
{
    public string FormationCode { get; set; } = "DEFAULT";
    public List<FormationPositionRequestDto> Positions { get; set; } = new();
    public string ClientRequestId { get; set; } = Guid.NewGuid().ToString("N");
}

public sealed class HeroExpResultDto
{
    public long PlayerHeroId { get; set; }
    public string HeroName { get; set; } = null!;
    public int ExpGained { get; set; }
    public int OldLevel { get; set; }
    public int NewLevel { get; set; }
    public int OldExp { get; set; }
    public int NewExp { get; set; }
}

public sealed class DungeonResultDto
{
    public long RunId { get; set; }
    public string Result { get; set; } = null!;
    public int EarnedStars { get; set; }
    public int PreviousBestStars { get; set; }
    public int BestStars { get; set; }
    public bool IsNewStarRecord { get; set; }
    public decimal RemainingHpRate { get; set; }
    public int TotalMapStars { get; set; }
    public int StaminaSpent { get; set; }
    public int StaminaRemaining { get; set; }
    public long GoldGained { get; set; }
    public bool IsFirstClear { get; set; }
    public int PlayerExpGained { get; set; }
    public int OldPlayerLevel { get; set; }
    public int NewPlayerLevel { get; set; }
    public int OldPlayerExp { get; set; }
    public int NewPlayerExp { get; set; }
    public int? UnlockedStageId { get; set; }
    public List<HeroExpResultDto> Heroes { get; set; } = new();
    public DungeonDroppedEquipmentDto? DroppedEquipment { get; set; }
    public bool IsBagFull { get; set; }
}

public sealed class StartDungeonStageResultDto
{
    public StartBattleResultDto Battle { get; set; } = null!;
    public DungeonResultDto Result { get; set; } = null!;
}

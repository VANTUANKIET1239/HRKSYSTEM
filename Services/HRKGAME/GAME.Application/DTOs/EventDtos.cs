namespace GAME.Application.DTOs;

public class GameEventDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string EventType { get; set; } = "TOWER";
    public string? Description { get; set; }
    public string BannerImagePath { get; set; } = null!;
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public int MinPlayerLevel { get; set; } = 1;
    public bool IsOpen { get; set; }
    public string Status { get; set; } = "OPEN"; // OPEN, LOCKED, ENDED
    public string? LockReason { get; set; }
    public long RemainingSecondsToReset { get; set; }
    public DateTime NextResetTimeUtc { get; set; }
    public int InitialLives { get; set; } = 3;
    public int MaxFloor { get; set; } = 60;
    public PlayerEventProgressSummaryDto? PlayerProgress { get; set; }
    public List<GenericRewardItemDto> HighlightRewards { get; set; } = new();
}

public class PlayerEventProgressSummaryDto
{
    public int CurrentFloor { get; set; } = 1;
    public int RemainingLives { get; set; } = 3;
    public int InitialLives { get; set; } = 3;
    public int CurrentRunNumber { get; set; } = 1;
    public int HighestFloorInPeriod { get; set; } = 0;
    public int HighestFloorAllTime { get; set; } = 0;
    public bool IsCompleted { get; set; }
    public bool CanStartNewRun { get; set; }
    public string PeriodKey { get; set; } = null!;
    public int PendingRewardsCount { get; set; }
}

public class PlayerEventProgressDto : PlayerEventProgressSummaryDto
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int MaxFloor { get; set; }
    public long SecondsUntilReset { get; set; }
    public int PlayerPower { get; set; }
    public PlayerHeroDto? LeadHero { get; set; }
    public List<TowerFloorDto> Floors { get; set; } = new();
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public List<TowerChestDto> MilestoneChests { get; set; } = new();
    public List<PlayerPendingRewardDto> PendingRewards { get; set; } = new();
    public bool HasActiveQuickClimb { get; set; }
    public string? ActiveQuickClimbJobId { get; set; }
}

public class TowerFloorDto
{
    public int FloorNumber { get; set; }
    public string Name { get; set; } = null!;
    public string FloorType { get; set; } = "NORMAL"; // NORMAL, ELITE, BOSS
    public int RecommendedPower { get; set; }
    public string? BackgroundPath { get; set; }
    public string State { get; set; } = "LOCKED"; // CLEARED, CURRENT, LOCKED
    public TowerEnemyDto? RepresentativeEnemy { get; set; }
    public bool HasMilestoneChest { get; set; }
    public TowerChestDto? ChestInfo { get; set; }
    public List<GenericRewardItemDto> Rewards { get; set; } = new();
}

public class TowerFloorDetailDto : TowerFloorDto
{
    public int PlayerPower { get; set; }
    public int EnemyPower { get; set; }
    public List<TowerEnemyDto> Enemies { get; set; } = new();
    public int RemainingLives { get; set; }
    public int MaxLives { get; set; } = 3;
    public bool CanStart { get; set; }
    public string? LockedReason { get; set; }
}

public class TowerEnemyDto
{
    public int Position { get; set; }
    public int HeroTemplateId { get; set; }
    public string Name { get; set; } = null!;
    public string ImagePath { get; set; } = null!;
    public int Level { get; set; }
    public byte Stars { get; set; }
    public int Power { get; set; }
    public bool IsBoss { get; set; }
    public string? RarityCode { get; set; }
    public string? RarityColorHex { get; set; }
    public string? ClassCode { get; set; }
    public string? FactionCode { get; set; }
}

public class TowerChestDto
{
    public int Id { get; set; }
    public int FloorNumber { get; set; }
    public string ChestName { get; set; } = null!;
    public string? ChestIcon { get; set; }
    public string? Description { get; set; }
    public string State { get; set; } = "LOCKED"; // LOCKED, UNLOCKED, CLAIMED
    public List<GenericRewardItemDto> Rewards { get; set; } = new();
}

public class GenericRewardItemDto
{
    public string Type { get; set; } = "GOLD"; // GOLD, DIAMOND, ENHANCEMENT_STONE, CHARM, HERO_STONE, UNIVERSAL_STAR_STONE, MATERIAL, EQUIPMENT, PLAYER_EXP, HERO_EXP
    public int? ItemTemplateId { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = null!;
    public string? ImagePath { get; set; }
    public string? RarityCode { get; set; }
    public string? RarityColorHex { get; set; }
    public string? CategoryName { get; set; }
    public int Quantity { get; set; } = 1;
    public int? StoneGrade { get; set; }
    public string? CharmType { get; set; }
    public string? Description { get; set; }
    public DungeonDroppedEquipmentDto? DroppedEquipment { get; set; }
}

public class StartTowerBattleRequestDto
{
    public string ClientRequestId { get; set; } = null!;
    public string? FormationCode { get; set; }
    public List<FormationPositionRequestDto>? Positions { get; set; }
}

public class StartTowerBattleResultDto
{
    public StartBattleResultDto Battle { get; set; } = null!;
    public int FloorNumber { get; set; }
    public int? NextFloorNumber { get; set; }
    public int LivesBefore { get; set; }
    public int LivesAfter { get; set; }
    public bool IsVictory { get; set; }
    public bool IsRunEnded { get; set; }
    public bool IsTowerCompleted { get; set; }
    public bool IsMilestoneChestUnlocked { get; set; }
    public TowerChestDto? UnlockedChest { get; set; }
    public List<GenericRewardItemDto> EarnedRewards { get; set; } = new();
    public List<GenericRewardItemDto> PendingRewards { get; set; } = new();
    public int PlayerExpGained { get; set; }
    public int HeroExpGained { get; set; }
    public int NewPlayerLevel { get; set; }
    public int NewPlayerExp { get; set; }
    public List<HeroExpResultDto> Heroes { get; set; } = new();
}

public class StartQuickClimbRequestDto
{
    public string? FormationCode { get; set; }
    public List<FormationPositionRequestDto>? Positions { get; set; }
}

public class QuickClimbJobStatusDto
{
    public string JobId { get; set; } = null!;
    public string Status { get; set; } = "QUEUED"; // QUEUED, PROCESSING, COMPLETED, STOPPED_DEFEAT, CANCELLED, EXPIRED, ERROR
    public string? StopReason { get; set; } // FIRST_DEFEAT, TOWER_COMPLETED, USER_CANCELLED, PERIOD_EXPIRED, EVENT_CLOSED, ERROR
    public int StartFloor { get; set; }
    public int CurrentFloor { get; set; }
    public int TargetFloor { get; set; }
    public int InitialLives { get; set; }
    public int RemainingLives { get; set; }
    public int ClearedFloorsCount { get; set; }
    public int? FailedFloor { get; set; }
    public List<GenericRewardItemDto> AccumulatedRewards { get; set; } = new();
    public List<QuickClimbFloorLogDto> Logs { get; set; } = new();
    public string? LastBattleId { get; set; }
    public List<TowerChestDto> UnlockedChests { get; set; } = new();
    public DateTime CreatedOnUtc { get; set; }
    public DateTime UpdatedOnUtc { get; set; }
    public DateTime? CompletedOnUtc { get; set; }
}

public class QuickClimbFloorLogDto
{
    public int FloorNumber { get; set; }
    public string FloorName { get; set; } = null!;
    public bool IsVictory { get; set; }
    public int TotalTurns { get; set; }
    public int LivesRemaining { get; set; }
    public List<GenericRewardItemDto> RewardsGained { get; set; } = new();
    public DateTime TimestampUtc { get; set; }
}

public class PlayerPendingRewardDto
{
    public long Id { get; set; }
    public string SourceType { get; set; } = null!;
    public int SourceRefId { get; set; }
    public string Description { get; set; } = null!;
    public List<GenericRewardItemDto> RewardItems { get; set; } = new();
    public string Status { get; set; } = "PENDING";
    public DateTime CreatedOnUtc { get; set; }
    public DateTime? ClaimedOnUtc { get; set; }
}

public class ClaimRewardResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public List<GenericRewardItemDto> ClaimedRewards { get; set; } = new();
    public bool IsBagFull { get; set; }
}

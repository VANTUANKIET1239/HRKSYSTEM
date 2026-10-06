-- ==============================================================================================
-- Script: Migration_EventTowerClimbSystem.sql
-- Description: Triển khai toàn diện hệ thống Sự kiện Chiến dịch & Sự kiện Leo Tháp (Tower Climb)
-- Idempotent: Có thể chạy nhiều lần an toàn mà không làm mất dữ liệu.
-- ==============================================================================================

SET NOCOUNT ON;

-- 1. BẢNG SỰ KIỆN CHUNG: HRK_GameEvents
IF OBJECT_ID(N'dbo.HRK_GameEvents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_GameEvents
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HRK_GameEvents PRIMARY KEY,
        Code NVARCHAR(50) NOT NULL CONSTRAINT UQ_HRK_GameEvents_Code UNIQUE,
        Name NVARCHAR(150) NOT NULL,
        EventType NVARCHAR(50) NOT NULL, -- TOWER, WORLD_BOSS, ARENA, v.v.
        Description NVARCHAR(500) NULL,
        BannerImagePath NVARCHAR(500) NOT NULL,
        Icon NVARCHAR(100) NULL,
        DisplayOrder INT NOT NULL CONSTRAINT DF_HRK_GameEvents_DisplayOrder DEFAULT (0),
        MinPlayerLevel INT NOT NULL CONSTRAINT DF_HRK_GameEvents_MinPlayerLevel DEFAULT (1),
        IsOpen BIT NOT NULL CONSTRAINT DF_HRK_GameEvents_IsOpen DEFAULT (1),
        StartTimeUtc DATETIME2 NULL,
        EndTimeUtc DATETIME2 NULL,
        ResetType NVARCHAR(30) NOT NULL CONSTRAINT DF_HRK_GameEvents_ResetType DEFAULT ('DAILY'),
        ResetTime TIME NOT NULL CONSTRAINT DF_HRK_GameEvents_ResetTime DEFAULT ('12:00:00'),
        TimeZoneId NVARCHAR(50) NOT NULL CONSTRAINT DF_HRK_GameEvents_TimeZoneId DEFAULT ('Asia/Ho_Chi_Minh'),
        InitialLives INT NOT NULL CONSTRAINT DF_HRK_GameEvents_InitialLives DEFAULT (3),
        LifeConsumeMode NVARCHAR(30) NOT NULL CONSTRAINT DF_HRK_GameEvents_LifeConsumeMode DEFAULT ('ON_DEFEAT'),
        MaxDailyRuns INT NULL,
        RulesJson NVARCHAR(MAX) NULL,
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_GameEvents_CreatedOn DEFAULT (SYSUTCDATETIME()),
        UpdatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_GameEvents_UpdatedOn DEFAULT (SYSUTCDATETIME())
    );
END;

-- 2. BẢNG KỲ SỰ KIỆN: HRK_EventPeriods
IF OBJECT_ID(N'dbo.HRK_EventPeriods', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_EventPeriods
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HRK_EventPeriods PRIMARY KEY,
        EventId INT NOT NULL CONSTRAINT FK_HRK_EventPeriods_Event FOREIGN KEY REFERENCES dbo.HRK_GameEvents(Id) ON DELETE CASCADE,
        PeriodKey NVARCHAR(50) NOT NULL, -- Định dạng yyyy-MM-dd theo mốc 12:00 trưa
        StartAtUtc DATETIME2 NOT NULL,
        EndAtUtc DATETIME2 NOT NULL,
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_EventPeriods_CreatedOn DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_HRK_EventPeriods_Event_PeriodKey UNIQUE (EventId, PeriodKey)
    );
END;

-- 3. BẢNG TIẾN ĐỘ NGƯỜI CHƠI TRONG KỲ: HRK_PlayerEventPeriodProgresses
IF OBJECT_ID(N'dbo.HRK_PlayerEventPeriodProgresses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_PlayerEventPeriodProgresses
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HRK_PlayerEventPeriodProgresses PRIMARY KEY,
        PlayerId BIGINT NOT NULL CONSTRAINT FK_HRK_PlayerEventPeriodProgresses_Player FOREIGN KEY REFERENCES dbo.HRK_Players(Id),
        EventId INT NOT NULL CONSTRAINT FK_HRK_PlayerEventPeriodProgresses_Event FOREIGN KEY REFERENCES dbo.HRK_GameEvents(Id),
        EventPeriodId BIGINT NOT NULL CONSTRAINT FK_HRK_PlayerEventPeriodProgresses_Period FOREIGN KEY REFERENCES dbo.HRK_EventPeriods(Id) ON DELETE CASCADE,
        PeriodKey NVARCHAR(50) NOT NULL,
        CurrentFloor INT NOT NULL CONSTRAINT DF_HRK_PlayerEventPeriodProgresses_CurrentFloor DEFAULT (1),
        RemainingLives INT NOT NULL CONSTRAINT DF_HRK_PlayerEventPeriodProgresses_RemainingLives DEFAULT (3),
        CurrentRunNumber INT NOT NULL CONSTRAINT DF_HRK_PlayerEventPeriodProgresses_CurrentRunNumber DEFAULT (1),
        QuickClimbRunsUsed INT NOT NULL CONSTRAINT DF_HRK_PlayerEventPeriodProgresses_QuickClimbRunsUsed DEFAULT (0),
        HighestFloorInPeriod INT NOT NULL CONSTRAINT DF_HRK_PlayerEventPeriodProgresses_HighestFloor DEFAULT (0),
        IsCompleted BIT NOT NULL CONSTRAINT DF_HRK_PlayerEventPeriodProgresses_IsCompleted DEFAULT (0),
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_PlayerEventPeriodProgresses_CreatedOn DEFAULT (SYSUTCDATETIME()),
        UpdatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_PlayerEventPeriodProgresses_UpdatedOn DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_HRK_PlayerEventPeriodProgresses_Unique UNIQUE (PlayerId, EventId, EventPeriodId)
    );
END;

-- 4. BẢNG KỶ LỤC MỌI THỜI ĐIỂM: HRK_PlayerEventAllTimeRecords
IF OBJECT_ID(N'dbo.HRK_PlayerEventAllTimeRecords', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_PlayerEventAllTimeRecords
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HRK_PlayerEventAllTimeRecords PRIMARY KEY,
        PlayerId BIGINT NOT NULL CONSTRAINT FK_HRK_PlayerEventAllTimeRecords_Player FOREIGN KEY REFERENCES dbo.HRK_Players(Id),
        EventId INT NOT NULL CONSTRAINT FK_HRK_PlayerEventAllTimeRecords_Event FOREIGN KEY REFERENCES dbo.HRK_GameEvents(Id),
        HighestFloorAllTime INT NOT NULL CONSTRAINT DF_HRK_PlayerEventAllTimeRecords_HighestFloor DEFAULT (0),
        TotalClears INT NOT NULL CONSTRAINT DF_HRK_PlayerEventAllTimeRecords_TotalClears DEFAULT (0),
        UpdatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_PlayerEventAllTimeRecords_UpdatedOn DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_HRK_PlayerEventAllTimeRecords_Unique UNIQUE (PlayerId, EventId)
    );
END;

-- 5. BẢNG LƯỢT LEO CỦA NGƯỜI CHƠI: HRK_PlayerTowerRuns
IF OBJECT_ID(N'dbo.HRK_PlayerTowerRuns', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_PlayerTowerRuns
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HRK_PlayerTowerRuns PRIMARY KEY,
        PlayerId BIGINT NOT NULL CONSTRAINT FK_HRK_PlayerTowerRuns_Player FOREIGN KEY REFERENCES dbo.HRK_Players(Id),
        EventPeriodId BIGINT NOT NULL CONSTRAINT FK_HRK_PlayerTowerRuns_Period FOREIGN KEY REFERENCES dbo.HRK_EventPeriods(Id) ON DELETE CASCADE,
        RunNumber INT NOT NULL,
        StartFloor INT NOT NULL CONSTRAINT DF_HRK_PlayerTowerRuns_StartFloor DEFAULT (1),
        EndFloor INT NOT NULL CONSTRAINT DF_HRK_PlayerTowerRuns_EndFloor DEFAULT (1),
        LivesRemaining INT NOT NULL CONSTRAINT DF_HRK_PlayerTowerRuns_LivesRemaining DEFAULT (3),
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_HRK_PlayerTowerRuns_Status DEFAULT ('IN_PROGRESS'), -- IN_PROGRESS, DEFEATED, COMPLETED, EXPIRED
        StartAtUtc DATETIME2 NOT NULL CONSTRAINT DF_HRK_PlayerTowerRuns_StartAt DEFAULT (SYSUTCDATETIME()),
        EndAtUtc DATETIME2 NULL
    );
    CREATE NONCLUSTERED INDEX IX_HRK_PlayerTowerRuns_Player_Period ON dbo.HRK_PlayerTowerRuns(PlayerId, EventPeriodId, RunNumber);
END;

-- 6. BẢNG CẤU HÌNH TẦNG THÁP: HRK_TowerFloors
IF OBJECT_ID(N'dbo.HRK_TowerFloors', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_TowerFloors
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HRK_TowerFloors PRIMARY KEY,
        EventId INT NOT NULL CONSTRAINT FK_HRK_TowerFloors_Event FOREIGN KEY REFERENCES dbo.HRK_GameEvents(Id) ON DELETE CASCADE,
        FloorNumber INT NOT NULL,
        Name NVARCHAR(150) NOT NULL,
        FloorType NVARCHAR(30) NOT NULL CONSTRAINT DF_HRK_TowerFloors_FloorType DEFAULT ('NORMAL'), -- NORMAL, ELITE, BOSS
        RecommendedPower INT NOT NULL,
        BackgroundPath NVARCHAR(500) NULL,
        RepresentativeHeroTemplateId INT NULL CONSTRAINT FK_HRK_TowerFloors_RepresentativeHero FOREIGN KEY REFERENCES dbo.HRK_HeroTemplates(Id),
        StatMultiplier DECIMAL(8,4) NOT NULL CONSTRAINT DF_HRK_TowerFloors_StatMultiplier DEFAULT (1.0),
        HpMultiplier DECIMAL(8,4) NOT NULL CONSTRAINT DF_HRK_TowerFloors_HpMultiplier DEFAULT (1.0),
        AtkMultiplier DECIMAL(8,4) NOT NULL CONSTRAINT DF_HRK_TowerFloors_AtkMultiplier DEFAULT (1.0),
        DefMultiplier DECIMAL(8,4) NOT NULL CONSTRAINT DF_HRK_TowerFloors_DefMultiplier DEFAULT (1.0),
        GoldReward BIGINT NOT NULL CONSTRAINT DF_HRK_TowerFloors_GoldReward DEFAULT (0),
        PlayerExpReward INT NOT NULL CONSTRAINT DF_HRK_TowerFloors_PlayerExp DEFAULT (0),
        HeroExpReward INT NOT NULL CONSTRAINT DF_HRK_TowerFloors_HeroExp DEFAULT (0),
        RewardsJson NVARCHAR(MAX) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_HRK_TowerFloors_IsActive DEFAULT (1),
        CONSTRAINT UQ_HRK_TowerFloors_Event_FloorNumber UNIQUE (EventId, FloorNumber)
    );
END;

-- 7. BẢNG ĐỘI HÌNH ĐỊCH TỪNG TẦNG: HRK_TowerFloorEnemies
IF OBJECT_ID(N'dbo.HRK_TowerFloorEnemies', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_TowerFloorEnemies
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HRK_TowerFloorEnemies PRIMARY KEY,
        TowerFloorId INT NOT NULL CONSTRAINT FK_HRK_TowerFloorEnemies_Floor FOREIGN KEY REFERENCES dbo.HRK_TowerFloors(Id) ON DELETE CASCADE,
        Position INT NOT NULL,
        HeroTemplateId INT NOT NULL CONSTRAINT FK_HRK_TowerFloorEnemies_Hero FOREIGN KEY REFERENCES dbo.HRK_HeroTemplates(Id),
        Level INT NOT NULL CONSTRAINT DF_HRK_TowerFloorEnemies_Level DEFAULT (1),
        Stars TINYINT NOT NULL CONSTRAINT DF_HRK_TowerFloorEnemies_Stars DEFAULT (1),
        DisplayName NVARCHAR(150) NULL,
        ImagePath NVARCHAR(500) NULL,
        StatMultiplier DECIMAL(8,4) NOT NULL CONSTRAINT DF_HRK_TowerFloorEnemies_StatMultiplier DEFAULT (1.0),
        CONSTRAINT UQ_HRK_TowerFloorEnemies_Floor_Position UNIQUE (TowerFloorId, Position)
    );
END;

-- 8. BẢNG CẤU HÌNH RƯƠNG MỐC: HRK_TowerChestConfigs
IF OBJECT_ID(N'dbo.HRK_TowerChestConfigs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_TowerChestConfigs
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HRK_TowerChestConfigs PRIMARY KEY,
        EventId INT NOT NULL CONSTRAINT FK_HRK_TowerChestConfigs_Event FOREIGN KEY REFERENCES dbo.HRK_GameEvents(Id) ON DELETE CASCADE,
        FloorNumber INT NOT NULL, -- 15, 30, 45, 60
        ChestName NVARCHAR(150) NOT NULL,
        ChestIcon NVARCHAR(100) NULL,
        Description NVARCHAR(500) NULL,
        GoldReward BIGINT NOT NULL CONSTRAINT DF_HRK_TowerChestConfigs_GoldReward DEFAULT (0),
        DiamondReward INT NOT NULL CONSTRAINT DF_HRK_TowerChestConfigs_DiamondReward DEFAULT (0),
        GuaranteedItemTemplateId INT NULL CONSTRAINT FK_HRK_TowerChestConfigs_Item FOREIGN KEY REFERENCES dbo.HRK_ItemTemplates(Id),
        GuaranteedItemCount INT NOT NULL CONSTRAINT DF_HRK_TowerChestConfigs_ItemCount DEFAULT (1),
        RewardsJson NVARCHAR(MAX) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_HRK_TowerChestConfigs_IsActive DEFAULT (1),
        CONSTRAINT UQ_HRK_TowerChestConfigs_Event_FloorNumber UNIQUE (EventId, FloorNumber)
    );
END;

-- 9. BẢNG LỊCH SỬ NHẬN THƯỞNG: HRK_PlayerEventRewardClaims
IF OBJECT_ID(N'dbo.HRK_PlayerEventRewardClaims', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_PlayerEventRewardClaims
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HRK_PlayerEventRewardClaims PRIMARY KEY,
        PlayerId BIGINT NOT NULL CONSTRAINT FK_HRK_PlayerEventRewardClaims_Player FOREIGN KEY REFERENCES dbo.HRK_Players(Id),
        EventPeriodId BIGINT NOT NULL CONSTRAINT FK_HRK_PlayerEventRewardClaims_Period FOREIGN KEY REFERENCES dbo.HRK_EventPeriods(Id) ON DELETE CASCADE,
        ClaimType NVARCHAR(30) NOT NULL, -- 'FLOOR_REWARD', 'MILESTONE_CHEST', 'PENDING_REWARD'
        TargetId INT NOT NULL, -- FloorNumber or ChestId
        ClaimedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_HRK_PlayerEventRewardClaims_ClaimedAt DEFAULT (SYSUTCDATETIME()),
        RewardSummaryJson NVARCHAR(MAX) NULL,
        CONSTRAINT UQ_HRK_PlayerEventRewardClaims_Unique UNIQUE (PlayerId, EventPeriodId, ClaimType, TargetId)
    );
END;

-- 10. BẢNG PHẦN THƯỞNG CHỜ NHẬN: HRK_PlayerPendingRewards
IF OBJECT_ID(N'dbo.HRK_PlayerPendingRewards', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_PlayerPendingRewards
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HRK_PlayerPendingRewards PRIMARY KEY,
        PlayerId BIGINT NOT NULL CONSTRAINT FK_HRK_PlayerPendingRewards_Player FOREIGN KEY REFERENCES dbo.HRK_Players(Id),
        EventPeriodId BIGINT NOT NULL CONSTRAINT FK_HRK_PlayerPendingRewards_Period FOREIGN KEY REFERENCES dbo.HRK_EventPeriods(Id) ON DELETE CASCADE,
        SourceType NVARCHAR(30) NOT NULL, -- 'UNCLAIMED_MILESTONE_CHEST', 'BAG_FULL_REWARD'
        SourceRefId INT NOT NULL,
        Description NVARCHAR(255) NOT NULL,
        RewardItemsJson NVARCHAR(MAX) NOT NULL,
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_HRK_PlayerPendingRewards_Status DEFAULT ('PENDING'), -- PENDING, CLAIMED, EXPIRED
        CreatedOnUtc DATETIME2 NOT NULL CONSTRAINT DF_HRK_PlayerPendingRewards_CreatedOn DEFAULT (SYSUTCDATETIME()),
        ClaimedOnUtc DATETIME2 NULL
    );
    CREATE NONCLUSTERED INDEX IX_HRK_PlayerPendingRewards_Player_Status ON dbo.HRK_PlayerPendingRewards(PlayerId, Status);
END;

-- 11. BẢNG PHIÊN LEO NHANH: HRK_TowerQuickClimbJobs
IF OBJECT_ID(N'dbo.HRK_TowerQuickClimbJobs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_TowerQuickClimbJobs
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HRK_TowerQuickClimbJobs PRIMARY KEY,
        JobId NVARCHAR(64) NOT NULL CONSTRAINT UQ_HRK_TowerQuickClimbJobs_JobId UNIQUE,
        PlayerId BIGINT NOT NULL CONSTRAINT FK_HRK_TowerQuickClimbJobs_Player FOREIGN KEY REFERENCES dbo.HRK_Players(Id),
        EventPeriodId BIGINT NOT NULL CONSTRAINT FK_HRK_TowerQuickClimbJobs_Period FOREIGN KEY REFERENCES dbo.HRK_EventPeriods(Id) ON DELETE CASCADE,
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_HRK_TowerQuickClimbJobs_Status DEFAULT ('QUEUED'), -- QUEUED, PROCESSING, COMPLETED, STOPPED_DEFEAT, CANCELLED, EXPIRED, ERROR
        StopReason NVARCHAR(50) NULL, -- FIRST_DEFEAT, TOWER_COMPLETED, USER_CANCELLED, PERIOD_EXPIRED, EVENT_CLOSED, ERROR
        StartFloor INT NOT NULL,
        CurrentFloor INT NOT NULL,
        TargetFloor INT NOT NULL,
        InitialLives INT NOT NULL,
        RemainingLives INT NOT NULL,
        ClearedFloorsCount INT NOT NULL CONSTRAINT DF_HRK_TowerQuickClimbJobs_ClearedFloorsCount DEFAULT (0),
        DailyRunNumber INT NOT NULL CONSTRAINT DF_HRK_TowerQuickClimbJobs_DailyRunNumber DEFAULT (0),
        FailedFloor INT NULL,
        FormationCode NVARCHAR(50) NOT NULL,
        FormationSnapshotJson NVARCHAR(MAX) NOT NULL,
        AccumulatedRewardsJson NVARCHAR(MAX) NULL,
        LogsJson NVARCHAR(MAX) NULL,
        LastBattleId NVARCHAR(64) NULL,
        WorkerId NVARCHAR(100) NULL,
        LockedUntilUtc DATETIME2 NULL,
        CreatedOnUtc DATETIME2 NOT NULL CONSTRAINT DF_HRK_TowerQuickClimbJobs_CreatedOn DEFAULT (SYSUTCDATETIME()),
        UpdatedOnUtc DATETIME2 NOT NULL CONSTRAINT DF_HRK_TowerQuickClimbJobs_UpdatedOn DEFAULT (SYSUTCDATETIME()),
        CompletedOnUtc DATETIME2 NULL
    );
    CREATE NONCLUSTERED INDEX IX_HRK_TowerQuickClimbJobs_Player_Status ON dbo.HRK_TowerQuickClimbJobs(PlayerId, Status);
END;

-- 12. BẢNG LỊCH SỬ TRẬN ĐÁNH THÁP: HRK_PlayerTowerBattles
IF OBJECT_ID(N'dbo.HRK_PlayerTowerBattles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_PlayerTowerBattles
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HRK_PlayerTowerBattles PRIMARY KEY,
        BattleId NVARCHAR(64) NOT NULL CONSTRAINT UQ_HRK_PlayerTowerBattles_BattleId UNIQUE,
        PlayerId BIGINT NOT NULL CONSTRAINT FK_HRK_PlayerTowerBattles_Player FOREIGN KEY REFERENCES dbo.HRK_Players(Id),
        EventPeriodId BIGINT NOT NULL CONSTRAINT FK_HRK_PlayerTowerBattles_Period FOREIGN KEY REFERENCES dbo.HRK_EventPeriods(Id) ON DELETE CASCADE,
        FloorNumber INT NOT NULL,
        RunId BIGINT NULL,
        QuickClimbJobId BIGINT NULL,
        Result NVARCHAR(20) NOT NULL, -- VICTORY, DEFEAT
        LivesBefore INT NOT NULL,
        LivesAfter INT NOT NULL,
        PlayerPower INT NOT NULL,
        EnemyPower INT NOT NULL,
        RandomSeed INT NOT NULL,
        HeroStatisticsJson NVARCHAR(MAX) NULL,
        CreatedOnUtc DATETIME2 NOT NULL CONSTRAINT DF_HRK_PlayerTowerBattles_CreatedOn DEFAULT (SYSUTCDATETIME())
    );
    CREATE NONCLUSTERED INDEX IX_HRK_PlayerTowerBattles_Player_Floor ON dbo.HRK_PlayerTowerBattles(PlayerId, EventPeriodId, FloorNumber);
END;

-- ==============================================================================================
-- 13. SEED DỮ LIỆU SỰ KIỆN: LEO THÁP (TOWER_CLIMB)
-- ==============================================================================================

MERGE dbo.HRK_GameEvents AS target
USING (
    VALUES
    (
        N'TOWER_CLIMB',
        N'Leo Tháp Thí Luyện',
        N'TOWER',
        N'Vượt 60 tầng tháp huyền bí, đối mặt ma quân hung tợn để đoạt lấy bí bảo và vinh quang!',
        N'/assets/images/dcs-game/campaign/events/tower-banner.jpg',
        N'bi-ladder',
        1,
        1,
        1,
        N'DAILY',
        CAST('12:00:00' AS TIME),
        N'Asia/Ho_Chi_Minh',
        3,
        N'ON_DEFEAT',
        NULL,
        N'{"maxFloor": 60, "quickClimbDailyLimit": 3, "bossMilestones": [15, 30, 45, 60], "eliteMilestones": [5, 10, 20, 25, 35, 40, 50, 55]}'
    )
) AS source (Code, Name, EventType, Description, BannerImagePath, Icon, DisplayOrder, MinPlayerLevel, IsOpen, ResetType, ResetTime, TimeZoneId, InitialLives, LifeConsumeMode, MaxDailyRuns, RulesJson)
ON target.Code = source.Code
WHEN MATCHED THEN
    UPDATE SET
        target.Name = source.Name,
        target.EventType = source.EventType,
        target.Description = source.Description,
        target.BannerImagePath = source.BannerImagePath,
        target.Icon = source.Icon,
        target.DisplayOrder = source.DisplayOrder,
        target.MinPlayerLevel = source.MinPlayerLevel,
        target.IsOpen = source.IsOpen,
        target.ResetType = source.ResetType,
        target.ResetTime = source.ResetTime,
        target.TimeZoneId = source.TimeZoneId,
        target.InitialLives = source.InitialLives,
        target.LifeConsumeMode = source.LifeConsumeMode,
        target.RulesJson = source.RulesJson,
        target.UpdatedOn = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (Code, Name, EventType, Description, BannerImagePath, Icon, DisplayOrder, MinPlayerLevel, IsOpen, ResetType, ResetTime, TimeZoneId, InitialLives, LifeConsumeMode, MaxDailyRuns, RulesJson)
    VALUES (source.Code, source.Name, source.EventType, source.Description, source.BannerImagePath, source.Icon, source.DisplayOrder, source.MinPlayerLevel, source.IsOpen, source.ResetType, source.ResetTime, source.TimeZoneId, source.InitialLives, source.LifeConsumeMode, source.MaxDailyRuns, source.RulesJson);

DECLARE @TowerEventId INT = (SELECT Id FROM dbo.HRK_GameEvents WHERE Code = 'TOWER_CLIMB');

-- ==============================================================================================
-- 14. SEED RƯƠNG MỐC: TẦNG 15, 30, 45, 60
-- ==============================================================================================

MERGE dbo.HRK_TowerChestConfigs AS target
USING (
    VALUES
    (
        @TowerEventId, 15, N'Rương Mốc Tầng 15', N'bi-box-seam-fill',
        N'Phần thưởng hoàn thành mốc Tầng 15: Vàng, Kim Cương, Đá Cường Hóa và Bùa May Mắn.',
        50000, 200, NULL, 1,
        N'[{"type":"GOLD","quantity":50000},{"type":"DIAMOND","quantity":200},{"type":"ENHANCEMENT_STONE","stoneGrade":2,"quantity":5},{"type":"CHARM","charmType":"LUCKY_CHARM","quantity":2}]'
    ),
    (
        @TowerEventId, 30, N'Rương Mốc Tầng 30', N'bi-box-seam-fill',
        N'Phần thưởng hoàn thành mốc Tầng 30: Vàng phong phú, Kim Cương, Đá Cường Hóa cao cấp và Bùa Hộ Mệnh.',
        150000, 500, NULL, 1,
        N'[{"type":"GOLD","quantity":150000},{"type":"DIAMOND","quantity":500},{"type":"ENHANCEMENT_STONE","stoneGrade":3,"quantity":8},{"type":"CHARM","charmType":"PROTECTION_CHARM","quantity":1}]'
    ),
    (
        @TowerEventId, 45, N'Rương Mốc Tầng 45', N'bi-box-seam-fill',
        N'Phần thưởng hoàn thành mốc Tầng 45: Đại lượng Vàng, Kim Cương, Đá Cường Hóa Siêu Cấp và Đại Bùa May Mắn.',
        300000, 1000, NULL, 1,
        N'[{"type":"GOLD","quantity":300000},{"type":"DIAMOND","quantity":1000},{"type":"ENHANCEMENT_STONE","stoneGrade":4,"quantity":10},{"type":"CHARM","charmType":"GREATER_LUCKY_CHARM","quantity":2}]'
    ),
    (
        @TowerEventId, 60, N'Rương Chí Tôn Đỉnh Tháp', N'bi-trophy-fill',
        N'Phần thưởng xưng bá đỉnh tháp 60 tầng: 1.000.000 Vàng, 2.500 Kim Cương, Đá Tăng Sao Mythic Dùng Chung và Thần Thạch Cường Hóa.',
        1000000, 2500, NULL, 1,
        N'[{"type":"GOLD","quantity":1000000},{"type":"DIAMOND","quantity":2500},{"type":"UNIVERSAL_STAR_STONE","quantity":5},{"type":"ENHANCEMENT_STONE","stoneGrade":5,"quantity":15},{"type":"CHARM","charmType":"PROTECTION_CHARM","quantity":3}]'
    )
) AS source (EventId, FloorNumber, ChestName, ChestIcon, Description, GoldReward, DiamondReward, GuaranteedItemTemplateId, GuaranteedItemCount, RewardsJson)
ON target.EventId = source.EventId AND target.FloorNumber = source.FloorNumber
WHEN MATCHED THEN
    UPDATE SET
        target.ChestName = source.ChestName,
        target.ChestIcon = source.ChestIcon,
        target.Description = source.Description,
        target.GoldReward = source.GoldReward,
        target.DiamondReward = source.DiamondReward,
        target.RewardsJson = source.RewardsJson,
        target.IsActive = 1
WHEN NOT MATCHED THEN
    INSERT (EventId, FloorNumber, ChestName, ChestIcon, Description, GoldReward, DiamondReward, GuaranteedItemTemplateId, GuaranteedItemCount, RewardsJson, IsActive)
    VALUES (source.EventId, source.FloorNumber, source.ChestName, source.ChestIcon, source.Description, source.GoldReward, source.DiamondReward, source.GuaranteedItemTemplateId, source.GuaranteedItemCount, source.RewardsJson, 1);

-- ==============================================================================================
-- 15. SEED 60 TẦNG THÁP (HRK_TowerFloors) VÀ ĐỘI HÌNH ĐỊCH (HRK_TowerFloorEnemies)
-- ==============================================================================================

DECLARE @Floor INT = 1;
DECLARE @FloorType NVARCHAR(30);
DECLARE @FloorName NVARCHAR(150);
DECLARE @RecPower INT;
DECLARE @StatMul DECIMAL(8,4);
DECLARE @HpMul DECIMAL(8,4);
DECLARE @AtkMul DECIMAL(8,4);
DECLARE @DefMul DECIMAL(8,4);
DECLARE @GoldReward BIGINT;
DECLARE @PlayerExpReward INT;
DECLARE @HeroExpReward INT;
DECLARE @FloorRewardsJson NVARCHAR(MAX);
DECLARE @RepHeroId INT;
DECLARE @FloorId INT;
DECLARE @EnemyLevel INT;
DECLARE @EnemyStars TINYINT;
DECLARE @Pos INT;
DECLARE @HeroId INT;
DECLARE @PosStatMul DECIMAL(8,4);

WHILE @Floor <= 60
BEGIN
    SET @FloorType = CASE
        WHEN @Floor IN (15, 30, 45, 60) THEN 'BOSS'
        WHEN @Floor IN (5, 10, 20, 25, 35, 40, 50, 55) THEN 'ELITE'
        ELSE 'NORMAL'
    END;

    SET @FloorName = CASE
        WHEN @Floor = 60 THEN N'Tầng 60 - Thần Vực Tối Cao [CHÍ TÔN]'
        WHEN @Floor = 45 THEN N'Tầng 45 - Huyết Ma Điện [BOSS]'
        WHEN @Floor = 30 THEN N'Tầng 30 - Hổ Lao Cổ Trận [BOSS]'
        WHEN @Floor = 15 THEN N'Tầng 15 - Sa Trường Vạn Dặm [BOSS]'
        WHEN @FloorType = 'ELITE' THEN CONCAT(N'Tầng ', @Floor, N' - Tinh Anh Ma Trận')
        ELSE CONCAT(N'Tầng ', @Floor, N' - Thí Luyện')
    END;

    -- Đường tăng lũy tiến độ khó mượt mà, hợp lý
    SET @RecPower = CAST(1200 + (@Floor * 450) + (POWER(@Floor, 1.45) * 65) AS INT);
    SET @StatMul = CAST(1.0 + (@Floor * 0.055) + (CASE WHEN @FloorType = 'BOSS' THEN 0.35 WHEN @FloorType = 'ELITE' THEN 0.15 ELSE 0 END) AS DECIMAL(8,4));
    SET @HpMul = CAST(1.0 + (@Floor * 0.065) + (CASE WHEN @FloorType = 'BOSS' THEN 0.40 WHEN @FloorType = 'ELITE' THEN 0.20 ELSE 0 END) AS DECIMAL(8,4));
    SET @AtkMul = CAST(1.0 + (@Floor * 0.050) + (CASE WHEN @FloorType = 'BOSS' THEN 0.25 WHEN @FloorType = 'ELITE' THEN 0.10 ELSE 0 END) AS DECIMAL(8,4));
    SET @DefMul = CAST(1.0 + (@Floor * 0.045) AS DECIMAL(8,4));

    SET @GoldReward = 1000 + (@Floor * 350) + (CASE WHEN @FloorType = 'BOSS' THEN 10000 WHEN @FloorType = 'ELITE' THEN 4000 ELSE 0 END);
    SET @PlayerExpReward = 50 + (@Floor * 10);
    SET @HeroExpReward = 100 + (@Floor * 25);

    -- Phần thưởng chi tiết dạng JSON cho mỗi tầng
    SET @FloorRewardsJson = CASE
        WHEN @FloorType = 'BOSS' THEN
            CONCAT(N'[{"type":"GOLD","quantity":', @GoldReward, N'},{"type":"PLAYER_EXP","quantity":', @PlayerExpReward, N'},{"type":"HERO_EXP","quantity":', @HeroExpReward, N'},{"type":"ENHANCEMENT_STONE","stoneGrade":', CASE WHEN @Floor >= 45 THEN 4 WHEN @Floor >= 30 THEN 3 ELSE 2 END, N',"quantity":5}]')
        WHEN @FloorType = 'ELITE' THEN
            CONCAT(N'[{"type":"GOLD","quantity":', @GoldReward, N'},{"type":"PLAYER_EXP","quantity":', @PlayerExpReward, N'},{"type":"HERO_EXP","quantity":', @HeroExpReward, N'},{"type":"ENHANCEMENT_STONE","stoneGrade":', CASE WHEN @Floor >= 35 THEN 3 ELSE 1 END, N',"quantity":3}]')
        ELSE
            CONCAT(N'[{"type":"GOLD","quantity":', @GoldReward, N'},{"type":"PLAYER_EXP","quantity":', @PlayerExpReward, N'},{"type":"HERO_EXP","quantity":', @HeroExpReward, N'}]')
    END;

    -- Chọn đại diện tầng (hero template id 1..10 xoay vòng)
    SET @RepHeroId = ((@Floor * 3) % 10) + 1;

    MERGE dbo.HRK_TowerFloors AS target
    USING (
        SELECT @TowerEventId AS EventId, @Floor AS FloorNumber
    ) AS source
    ON target.EventId = source.EventId AND target.FloorNumber = source.FloorNumber
    WHEN MATCHED THEN
        UPDATE SET
            target.Name = @FloorName,
            target.FloorType = @FloorType,
            target.RecommendedPower = @RecPower,
            target.RepresentativeHeroTemplateId = @RepHeroId,
            target.StatMultiplier = @StatMul,
            target.HpMultiplier = @HpMul,
            target.AtkMultiplier = @AtkMul,
            target.DefMultiplier = @DefMul,
            target.GoldReward = @GoldReward,
            target.PlayerExpReward = @PlayerExpReward,
            target.HeroExpReward = @HeroExpReward,
            target.RewardsJson = @FloorRewardsJson,
            target.IsActive = 1
    WHEN NOT MATCHED THEN
        INSERT (EventId, FloorNumber, Name, FloorType, RecommendedPower, RepresentativeHeroTemplateId, StatMultiplier, HpMultiplier, AtkMultiplier, DefMultiplier, GoldReward, PlayerExpReward, HeroExpReward, RewardsJson, IsActive)
        VALUES (@TowerEventId, @Floor, @FloorName, @FloorType, @RecPower, @RepHeroId, @StatMul, @HpMul, @AtkMul, @DefMul, @GoldReward, @PlayerExpReward, @HeroExpReward, @FloorRewardsJson, 1);

    SET @FloorId = (SELECT Id FROM dbo.HRK_TowerFloors WHERE EventId = @TowerEventId AND FloorNumber = @Floor);

    -- Đội hình địch 5 vị trí: cấp độ và sao tăng tiến theo tầng
    SET @EnemyLevel = 10 + (@Floor * 1);
    SET @EnemyStars = CASE
        WHEN @Floor >= 55 THEN 5
        WHEN @Floor >= 40 THEN 4
        WHEN @Floor >= 25 THEN 3
        WHEN @Floor >= 10 THEN 2
        ELSE 1
    END;

    -- Vị trí 1..5: xoay vòng template ID 1..10 hợp lệ
    SET @Pos = 1;
    WHILE @Pos <= 5
    BEGIN
        SET @HeroId = (((@Floor + @Pos * 2) % 10) + 1);
        SET @PosStatMul = @StatMul;
        IF @Pos = 1 AND @FloorType = 'BOSS' SET @PosStatMul = @StatMul * 1.25; -- Boss chủ chốt

        MERGE dbo.HRK_TowerFloorEnemies AS target
        USING (SELECT @FloorId AS TowerFloorId, @Pos AS Position) AS source
        ON target.TowerFloorId = source.TowerFloorId AND target.Position = source.Position
        WHEN MATCHED THEN
            UPDATE SET
                target.HeroTemplateId = @HeroId,
                target.Level = @EnemyLevel,
                target.Stars = @EnemyStars,
                target.StatMultiplier = @PosStatMul
        WHEN NOT MATCHED THEN
            INSERT (TowerFloorId, Position, HeroTemplateId, Level, Stars, StatMultiplier)
            VALUES (@FloorId, @Pos, @HeroId, @EnemyLevel, @EnemyStars, @PosStatMul);

        SET @Pos = @Pos + 1;
    END;

    SET @Floor = @Floor + 1;
END;

PRINT N'Hoàn thành Migration_EventTowerClimbSystem.sql thành công!';

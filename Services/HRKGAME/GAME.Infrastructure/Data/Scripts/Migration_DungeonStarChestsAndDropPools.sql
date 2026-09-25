/*
    Migration: Dungeon Star Chests and Boss Equipment Drop Pools
    - Adds BestStars and BestRemainingHpRate to dbo.HRK_PlayerDungeonStageProgress
    - Creates dbo.HRK_DungeonStageDropPools with dynamic item lookup by Code
    - Creates dbo.HRK_DungeonMapStarChests and dbo.HRK_PlayerDungeonStarChestClaims
    - Seeds drop pools for Boss stages (5, 10, 15) across Maps 1-3 with Common equipment
    - Seeds 3 star chests (15, 30, 45 stars) per map with rewards
    - Idempotent and safe to run multiple times
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY

    -- 1. Cập nhật bảng dbo.HRK_PlayerDungeonStageProgress
    IF COL_LENGTH('dbo.HRK_PlayerDungeonStageProgress', 'BestStars') IS NULL
    BEGIN
        ALTER TABLE dbo.HRK_PlayerDungeonStageProgress 
        ADD BestStars INT NOT NULL CONSTRAINT DF_HRK_PlayerDungeonProgress_BestStars DEFAULT 0;
    END

    IF COL_LENGTH('dbo.HRK_PlayerDungeonStageProgress', 'BestRemainingHpRate') IS NULL
    BEGIN
        ALTER TABLE dbo.HRK_PlayerDungeonStageProgress 
        ADD BestRemainingHpRate DECIMAL(5,4) NOT NULL CONSTRAINT DF_HRK_PlayerDungeonProgress_BestHpRate DEFAULT 0;
    END

    -- 2. Tạo bảng dbo.HRK_DungeonStageDropPools
    IF OBJECT_ID('dbo.HRK_DungeonStageDropPools', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.HRK_DungeonStageDropPools(
            Id INT IDENTITY(1,1) PRIMARY KEY,
            StageId INT NOT NULL,
            ItemTemplateId INT NOT NULL,
            DropRate DECIMAL(5,4) NOT NULL,
            Weight INT NOT NULL DEFAULT 100,
            MinQuantity INT NOT NULL DEFAULT 1,
            MaxQuantity INT NOT NULL DEFAULT 1,
            IsFirstClearOnly BIT NOT NULL DEFAULT 0,
            IsActive BIT NOT NULL DEFAULT 1,
            CreatedOn DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            CONSTRAINT FK_DropPools_Stage FOREIGN KEY(StageId) REFERENCES dbo.HRK_DungeonStages(Id) ON DELETE CASCADE,
            CONSTRAINT FK_DropPools_ItemTemplate FOREIGN KEY(ItemTemplateId) REFERENCES dbo.HRK_ItemTemplates(Id)
        );

        CREATE INDEX IX_DungeonStageDropPools_Stage ON dbo.HRK_DungeonStageDropPools(StageId, IsActive);
    END

    -- 3. Tạo bảng dbo.HRK_DungeonMapStarChests
    IF OBJECT_ID('dbo.HRK_DungeonMapStarChests', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.HRK_DungeonMapStarChests(
            Id INT IDENTITY(1,1) PRIMARY KEY,
            DungeonMapId INT NOT NULL,
            RequiredStars INT NOT NULL,
            GoldReward BIGINT NOT NULL DEFAULT 0,
            DiamondReward INT NOT NULL DEFAULT 0,
            UpgradeMaterialsReward INT NOT NULL DEFAULT 0,
            GuaranteedItemTemplateId INT NULL,
            DisplayOrder INT NOT NULL DEFAULT 1,
            Description NVARCHAR(500) NULL,
            IsActive BIT NOT NULL DEFAULT 1,
            CreatedOn DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            CONSTRAINT UQ_MapStarChests_MapRequired UNIQUE (DungeonMapId, RequiredStars),
            CONSTRAINT FK_MapStarChests_Map FOREIGN KEY(DungeonMapId) REFERENCES dbo.HRK_DungeonMaps(Id) ON DELETE CASCADE,
            CONSTRAINT FK_MapStarChests_ItemTemplate FOREIGN KEY(GuaranteedItemTemplateId) REFERENCES dbo.HRK_ItemTemplates(Id)
        );

        CREATE INDEX IX_DungeonMapStarChests_Map ON dbo.HRK_DungeonMapStarChests(DungeonMapId, DisplayOrder);
    END

    -- 4. Tạo bảng dbo.HRK_PlayerDungeonStarChestClaims
    IF OBJECT_ID('dbo.HRK_PlayerDungeonStarChestClaims', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.HRK_PlayerDungeonStarChestClaims(
            Id BIGINT IDENTITY(1,1) PRIMARY KEY,
            PlayerId BIGINT NOT NULL,
            StarChestId INT NOT NULL,
            ClaimedOn DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            CONSTRAINT UQ_PlayerStarChestClaims UNIQUE (PlayerId, StarChestId),
            CONSTRAINT FK_PlayerStarChestClaims_Player FOREIGN KEY(PlayerId) REFERENCES dbo.HRK_Players(Id),
            CONSTRAINT FK_PlayerStarChestClaims_Chest FOREIGN KEY(StarChestId) REFERENCES dbo.HRK_DungeonMapStarChests(Id)
        );

        CREATE INDEX IX_PlayerStarChestClaims_PlayerMap ON dbo.HRK_PlayerDungeonStarChestClaims(PlayerId, StarChestId);
    END

    -- 5. Seed Drop Pool cho Boss màn 5, 10, 15 của 3 Map đầu (Tra cứu bằng Code)
    DECLARE @WeaponCommonId INT = (SELECT TOP 1 Id FROM dbo.HRK_ItemTemplates WHERE Code = 'WEAPON_RUSTED_SWORD');
    DECLARE @ArmorCommonId INT = (SELECT TOP 1 Id FROM dbo.HRK_ItemTemplates WHERE Code = 'LIGHTNING_RAINCOAT');
    DECLARE @HelmetCommonId INT = (SELECT TOP 1 Id FROM dbo.HRK_ItemTemplates WHERE Code = 'GUARDIAN_ALUMINUM_POT');
    DECLARE @BootsCommonId INT = (SELECT TOP 1 Id FROM dbo.HRK_ItemTemplates WHERE Code = 'TACTICAL_HONEYCOMB_SANDALS');
    DECLARE @RingCommonId INT = (SELECT TOP 1 Id FROM dbo.HRK_ItemTemplates WHERE Code = 'BOTTLE_OPENER_RING');
    DECLARE @ArtifactCommonId INT = (SELECT TOP 1 Id FROM dbo.HRK_ItemTemplates WHERE Code = 'OLD_REPAIR_NOTEBOOK');

    -- Bảng tạm cấu hình Boss Drop Pool
    DECLARE @BossDropConfig TABLE (
        MapCode NVARCHAR(50),
        StageNumber INT,
        DropRate DECIMAL(5,4),
        ItemTemplateId INT,
        Weight INT
    );

    -- Map 1: BUG_FOREST
    -- Boss 5: 25% (Vũ khí, Nhẫn)
    INSERT INTO @BossDropConfig VALUES ('BUG_FOREST', 5, 0.2500, @WeaponCommonId, 60);
    INSERT INTO @BossDropConfig VALUES ('BUG_FOREST', 5, 0.2500, @RingCommonId, 40);
    -- Boss 10: 35% (Áo giáp, Mũ)
    INSERT INTO @BossDropConfig VALUES ('BUG_FOREST', 10, 0.3500, @ArmorCommonId, 50);
    INSERT INTO @BossDropConfig VALUES ('BUG_FOREST', 10, 0.3500, @HelmetCommonId, 50);
    -- Boss 15: 50% (Đầy đủ pool Common: Giày, Pháp bảo, Vũ khí, Áo)
    INSERT INTO @BossDropConfig VALUES ('BUG_FOREST', 15, 0.5000, @BootsCommonId, 30);
    INSERT INTO @BossDropConfig VALUES ('BUG_FOREST', 15, 0.5000, @ArtifactCommonId, 30);
    INSERT INTO @BossDropConfig VALUES ('BUG_FOREST', 15, 0.5000, @WeaponCommonId, 20);
    INSERT INTO @BossDropConfig VALUES ('BUG_FOREST', 15, 0.5000, @ArmorCommonId, 20);

    -- Map 2: LEGACY_DUNGEON
    -- Boss 5: 30%
    INSERT INTO @BossDropConfig VALUES ('LEGACY_DUNGEON', 5, 0.3000, @HelmetCommonId, 50);
    INSERT INTO @BossDropConfig VALUES ('LEGACY_DUNGEON', 5, 0.3000, @BootsCommonId, 50);
    -- Boss 10: 40%
    INSERT INTO @BossDropConfig VALUES ('LEGACY_DUNGEON', 10, 0.4000, @WeaponCommonId, 50);
    INSERT INTO @BossDropConfig VALUES ('LEGACY_DUNGEON', 10, 0.4000, @ArtifactCommonId, 50);
    -- Boss 15: 55%
    INSERT INTO @BossDropConfig VALUES ('LEGACY_DUNGEON', 15, 0.5500, @ArmorCommonId, 30);
    INSERT INTO @BossDropConfig VALUES ('LEGACY_DUNGEON', 15, 0.5500, @RingCommonId, 30);
    INSERT INTO @BossDropConfig VALUES ('LEGACY_DUNGEON', 15, 0.5500, @WeaponCommonId, 20);
    INSERT INTO @BossDropConfig VALUES ('LEGACY_DUNGEON', 15, 0.5500, @HelmetCommonId, 20);

    -- Map 3: PRODUCTION_CITADEL
    -- Boss 5: 40%
    INSERT INTO @BossDropConfig VALUES ('PRODUCTION_CITADEL', 5, 0.4000, @RingCommonId, 50);
    INSERT INTO @BossDropConfig VALUES ('PRODUCTION_CITADEL', 5, 0.4000, @ArmorCommonId, 50);
    -- Boss 10: 50%
    INSERT INTO @BossDropConfig VALUES ('PRODUCTION_CITADEL', 10, 0.5000, @BootsCommonId, 50);
    INSERT INTO @BossDropConfig VALUES ('PRODUCTION_CITADEL', 10, 0.5000, @HelmetCommonId, 50);
    -- Boss 15: 70%
    INSERT INTO @BossDropConfig VALUES ('PRODUCTION_CITADEL', 15, 0.7000, @WeaponCommonId, 35);
    INSERT INTO @BossDropConfig VALUES ('PRODUCTION_CITADEL', 15, 0.7000, @ArtifactCommonId, 25);
    INSERT INTO @BossDropConfig VALUES ('PRODUCTION_CITADEL', 15, 0.7000, @ArmorCommonId, 20);
    INSERT INTO @BossDropConfig VALUES ('PRODUCTION_CITADEL', 15, 0.7000, @BootsCommonId, 20);

    -- Chèn vào dbo.HRK_DungeonStageDropPools nếu chưa có
    MERGE INTO dbo.HRK_DungeonStageDropPools AS target
    USING (
        SELECT s.Id AS StageId, cfg.ItemTemplateId, cfg.DropRate, cfg.Weight
        FROM @BossDropConfig cfg
        JOIN dbo.HRK_DungeonMaps m ON m.Code = cfg.MapCode
        JOIN dbo.HRK_DungeonStages s ON s.DungeonMapId = m.Id AND s.StageNumber = cfg.StageNumber
        WHERE cfg.ItemTemplateId IS NOT NULL
    ) AS src
    ON target.StageId = src.StageId AND target.ItemTemplateId = src.ItemTemplateId
    WHEN MATCHED THEN
        UPDATE SET target.DropRate = src.DropRate, target.Weight = src.Weight, target.IsActive = 1
    WHEN NOT MATCHED THEN
        INSERT (StageId, ItemTemplateId, DropRate, Weight, MinQuantity, MaxQuantity, IsFirstClearOnly, IsActive)
        VALUES (src.StageId, src.ItemTemplateId, src.DropRate, src.Weight, 1, 1, 0, 1);

    -- 6. Seed 3 Rương Sao (15, 30, 45 sao) cho từng Map
    DECLARE @ChestConfig TABLE (
        MapCode NVARCHAR(50),
        RequiredStars INT,
        DisplayOrder INT,
        GoldReward BIGINT,
        DiamondReward INT,
        UpgradeMaterialsReward INT,
        GuaranteedItemTemplateId INT,
        Description NVARCHAR(500)
    );

    -- Map 1: BUG_FOREST
    INSERT INTO @ChestConfig VALUES ('BUG_FOREST', 15, 1, 5000, 50, 20, NULL, N'Rương 15 Sao - Khởi đầu hành trình Rừng Bug');
    INSERT INTO @ChestConfig VALUES ('BUG_FOREST', 30, 2, 12000, 100, 40, NULL, N'Rương 30 Sao - Chinh phục nửa chặng đường Rừng Bug');
    INSERT INTO @ChestConfig VALUES ('BUG_FOREST', 45, 3, 30000, 250, 80, @ArmorCommonId, N'Rương Toàn Thắng 45 Sao - Tặng Áo Mưa Chống Sét Common');

    -- Map 2: LEGACY_DUNGEON
    INSERT INTO @ChestConfig VALUES ('LEGACY_DUNGEON', 15, 1, 15000, 80, 40, NULL, N'Rương 15 Sao - Khám phá tàn tích Hầm Ngục Legacy');
    INSERT INTO @ChestConfig VALUES ('LEGACY_DUNGEON', 30, 2, 35000, 180, 80, NULL, N'Rương 30 Sao - Vượt qua cạm bẫy Hầm Ngục Legacy');
    INSERT INTO @ChestConfig VALUES ('LEGACY_DUNGEON', 45, 3, 75000, 400, 150, @BootsCommonId, N'Rương Toàn Thắng 45 Sao - Tặng Dép Tổ Ong Chiến Thuật Common');

    -- Map 3: PRODUCTION_CITADEL
    INSERT INTO @ChestConfig VALUES ('PRODUCTION_CITADEL', 15, 1, 30000, 120, 60, NULL, N'Rương 15 Sao - Tiếp cận Thành Trì Production');
    INSERT INTO @ChestConfig VALUES ('PRODUCTION_CITADEL', 30, 2, 70000, 250, 120, NULL, N'Rương 30 Sao - Đột kích nội thành Production');
    INSERT INTO @ChestConfig VALUES ('PRODUCTION_CITADEL', 45, 3, 150000, 600, 250, @WeaponCommonId, N'Rương Toàn Thắng 45 Sao - Tặng Kiếm Rỉ Sét Common');

    MERGE INTO dbo.HRK_DungeonMapStarChests AS target
    USING (
        SELECT m.Id AS DungeonMapId, c.RequiredStars, c.DisplayOrder, c.GoldReward, c.DiamondReward,
               c.UpgradeMaterialsReward, c.GuaranteedItemTemplateId, c.Description
        FROM @ChestConfig c
        JOIN dbo.HRK_DungeonMaps m ON m.Code = c.MapCode
    ) AS src
    ON target.DungeonMapId = src.DungeonMapId AND target.RequiredStars = src.RequiredStars
    WHEN MATCHED THEN
        UPDATE SET target.GoldReward = src.GoldReward,
                   target.DiamondReward = src.DiamondReward,
                   target.UpgradeMaterialsReward = src.UpgradeMaterialsReward,
                   target.GuaranteedItemTemplateId = src.GuaranteedItemTemplateId,
                   target.Description = src.Description,
                   target.DisplayOrder = src.DisplayOrder,
                   target.IsActive = 1
    WHEN NOT MATCHED THEN
        INSERT (DungeonMapId, RequiredStars, GoldReward, DiamondReward, UpgradeMaterialsReward, GuaranteedItemTemplateId, DisplayOrder, Description, IsActive)
        VALUES (src.DungeonMapId, src.RequiredStars, src.GoldReward, src.DiamondReward, src.UpgradeMaterialsReward, src.GuaranteedItemTemplateId, src.DisplayOrder, src.Description, 1);

    COMMIT TRANSACTION;
    PRINT N'MIGRATION THÀNH CÔNG: Đã cấu hình sao, rương tích lũy và drop pool cho 3 bản đồ phó bản!';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;

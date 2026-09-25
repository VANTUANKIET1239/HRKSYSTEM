-- ==============================================================================
-- Script: Migration_EquipmentInstances_CombatPower_FormationDraft.sql
-- Description:
--   1. Thêm MinValue, MaxValue vào HRK_ItemTemplateAttributes + Check constraint.
--   2. Tạo bảng cấu hình tăng trưởng cường hóa theo phẩm chất HRK_EquipmentRarityRollConfigs + Seed data.
--   3. Thêm EnhancementGrowthPercent, EnhancementGrowthMinPercent, EnhancementGrowthMaxPercent vào HRK_PlayerInventory.
--   4. Tạo bảng thuộc tính instance HRK_PlayerInventoryAttributes.
--   5. Thêm FormationCode, FormationSnapshotJson, ParticipantHeroIdsJson vào HRK_DungeonRuns.
--   6. Backfill dữ liệu cũ một cách deterministic và idempotent.
-- ==============================================================================

SET NOCOUNT ON;
BEGIN TRANSACTION;

BEGIN TRY
    PRINT N'==> 1. Cập nhật HRK_ItemTemplateAttributes: Thêm MinValue, MaxValue...';

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'HRK_ItemTemplateAttributes' AND COLUMN_NAME = 'MinValue'
    )
    BEGIN
        ALTER TABLE dbo.HRK_ItemTemplateAttributes
        ADD MinValue DECIMAL(18, 4) NULL;
        PRINT N'   + Đã thêm cột MinValue vào HRK_ItemTemplateAttributes.';
    END

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'HRK_ItemTemplateAttributes' AND COLUMN_NAME = 'MaxValue'
    )
    BEGIN
        ALTER TABLE dbo.HRK_ItemTemplateAttributes
        ADD MaxValue DECIMAL(18, 4) NULL;
        PRINT N'   + Đã thêm cột MaxValue vào HRK_ItemTemplateAttributes.';
    END

    -- Check constraint MinValue <= MaxValue (dùng sp_executesql để tránh lỗi parse cột mới)
    IF NOT EXISTS (
        SELECT 1 FROM sys.check_constraints WHERE name = 'CK_HRK_ItemTemplateAttributes_MinMax'
    )
    BEGIN
        EXEC sp_executesql N'
            ALTER TABLE dbo.HRK_ItemTemplateAttributes
            ADD CONSTRAINT CK_HRK_ItemTemplateAttributes_MinMax
            CHECK (MinValue IS NULL OR MaxValue IS NULL OR MinValue <= MaxValue);
        ';
        PRINT N'   + Đã tạo constraint CK_HRK_ItemTemplateAttributes_MinMax.';
    END

    -- Backfill MinValue và MaxValue cho các ItemTemplateAttributes hiện có (nếu đang NULL) bằng dynamic SQL
    EXEC sp_executesql N'
        UPDATE ita
        SET 
            MinValue = CASE 
                WHEN at.IsPercentage = 1 THEN ROUND(ita.Value * 0.85, 4)
                ELSE ROUND(ita.Value * 0.85, 0)
            END,
            MaxValue = CASE 
                WHEN at.IsPercentage = 1 THEN ROUND(ita.Value * 1.15, 4)
                ELSE ROUND(ita.Value * 1.15, 0)
            END
        FROM dbo.HRK_ItemTemplateAttributes ita
        JOIN dbo.HRK_AttributeTypes at ON ita.AttributeTypeId = at.Id
        WHERE ita.MinValue IS NULL OR ita.MaxValue IS NULL;
    ';

    PRINT N'   + Đã backfill MinValue, MaxValue cho HRK_ItemTemplateAttributes.';

    -- ==============================================================================
    PRINT N'==> 2. Tạo bảng HRK_EquipmentRarityRollConfigs...';

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'HRK_EquipmentRarityRollConfigs')
    BEGIN
        CREATE TABLE dbo.HRK_EquipmentRarityRollConfigs
        (
            Id INT IDENTITY(1, 1) NOT NULL,
            RarityId INT NOT NULL,
            EnhancementGrowthMinPercent DECIMAL(8, 4) NOT NULL,
            EnhancementGrowthMaxPercent DECIMAL(8, 4) NOT NULL,
            IsActive BIT NOT NULL CONSTRAINT DF_HRK_EquipmentRarityRollConfigs_IsActive DEFAULT 1,
            CreatedOn DATETIME NOT NULL CONSTRAINT DF_HRK_EquipmentRarityRollConfigs_CreatedOn DEFAULT GETDATE(),
            UpdatedOn DATETIME NOT NULL CONSTRAINT DF_HRK_EquipmentRarityRollConfigs_UpdatedOn DEFAULT GETDATE(),
            CONSTRAINT PK_HRK_EquipmentRarityRollConfigs PRIMARY KEY CLUSTERED (Id ASC),
            CONSTRAINT UQ_HRK_EquipmentRarityRollConfigs_RarityId UNIQUE NONCLUSTERED (RarityId ASC),
            CONSTRAINT FK_HRK_EquipmentRarityRollConfigs_Rarity FOREIGN KEY (RarityId) REFERENCES dbo.HRK_Rarities (Id),
            CONSTRAINT CK_HRK_EquipmentRarityRollConfigs_Range CHECK (
                EnhancementGrowthMinPercent >= 0 AND EnhancementGrowthMaxPercent >= EnhancementGrowthMinPercent
            )
        );
        PRINT N'   + Đã tạo bảng HRK_EquipmentRarityRollConfigs.';
    END

    -- Seed cấu hình tỷ lệ tăng trưởng theo phẩm chất qua sp_executesql
    EXEC sp_executesql N'
        MERGE dbo.HRK_EquipmentRarityRollConfigs AS target
        USING (
            SELECT r.Id AS RarityId, v.MinPercent, v.MaxPercent
            FROM (
                VALUES 
                    (N''Common'', 2.0000, 5.0000),
                    (N''Rare'', 4.0000, 8.0000),
                    (N''Epic'', 5.0000, 15.0000),
                    (N''Legendary'', 10.0000, 18.0000),
                    (N''Mythic'', 14.0000, 22.0000)
            ) AS v(RarityCode, MinPercent, MaxPercent)
            JOIN dbo.HRK_Rarities r ON UPPER(r.Code) = UPPER(v.RarityCode)
        ) AS source
        ON target.RarityId = source.RarityId
        WHEN MATCHED THEN
            UPDATE SET 
                EnhancementGrowthMinPercent = source.MinPercent,
                EnhancementGrowthMaxPercent = source.MaxPercent,
                IsActive = 1,
                UpdatedOn = GETDATE()
        WHEN NOT MATCHED THEN
            INSERT (RarityId, EnhancementGrowthMinPercent, EnhancementGrowthMaxPercent, IsActive, CreatedOn, UpdatedOn)
            VALUES (source.RarityId, source.MinPercent, source.MaxPercent, 1, GETDATE(), GETDATE());
    ';

    PRINT N'   + Đã seed dữ liệu tăng trưởng cường hóa cho các phẩm chất.';

    -- ==============================================================================
    PRINT N'==> 3. Cập nhật HRK_PlayerInventory: Thêm EnhancementGrowth...';

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'HRK_PlayerInventory' AND COLUMN_NAME = 'EnhancementGrowthPercent'
    )
    BEGIN
        ALTER TABLE dbo.HRK_PlayerInventory
        ADD EnhancementGrowthPercent DECIMAL(8, 4) NULL;
        PRINT N'   + Đã thêm cột EnhancementGrowthPercent.';
    END

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'HRK_PlayerInventory' AND COLUMN_NAME = 'EnhancementGrowthMinPercent'
    )
    BEGIN
        ALTER TABLE dbo.HRK_PlayerInventory
        ADD EnhancementGrowthMinPercent DECIMAL(8, 4) NULL;
        PRINT N'   + Đã thêm cột EnhancementGrowthMinPercent.';
    END

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'HRK_PlayerInventory' AND COLUMN_NAME = 'EnhancementGrowthMaxPercent'
    )
    BEGIN
        ALTER TABLE dbo.HRK_PlayerInventory
        ADD EnhancementGrowthMaxPercent DECIMAL(8, 4) NULL;
        PRINT N'   + Đã thêm cột EnhancementGrowthMaxPercent.';
    END

    -- Backfill EnhancementGrowthPercent cho trang bị hiện có qua sp_executesql
    EXEC sp_executesql N'
        UPDATE pi
        SET 
            pi.EnhancementGrowthMinPercent = cfg.EnhancementGrowthMinPercent,
            pi.EnhancementGrowthMaxPercent = cfg.EnhancementGrowthMaxPercent,
            pi.EnhancementGrowthPercent = (cfg.EnhancementGrowthMinPercent + cfg.EnhancementGrowthMaxPercent) / 2.0
        FROM dbo.HRK_PlayerInventory pi
        JOIN dbo.HRK_ItemTemplates it ON pi.ItemTemplateId = it.Id
        JOIN dbo.HRK_ItemCategories ic ON it.CategoryId = ic.Id
        JOIN dbo.HRK_EquipmentRarityRollConfigs cfg ON it.RarityId = cfg.RarityId
        WHERE ic.IsEquipment = 1 AND pi.EnhancementGrowthPercent IS NULL;
    ';

    PRINT N'   + Đã backfill EnhancementGrowthPercent cho trang bị hiện có.';

    -- ==============================================================================
    PRINT N'==> 4. Tạo bảng HRK_PlayerInventoryAttributes...';

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'HRK_PlayerInventoryAttributes')
    BEGIN
        CREATE TABLE dbo.HRK_PlayerInventoryAttributes
        (
            Id BIGINT IDENTITY(1, 1) NOT NULL,
            PlayerInventoryId BIGINT NOT NULL,
            AttributeTypeId INT NOT NULL,
            BaseRolledValue DECIMAL(18, 4) NOT NULL,
            CurrentValue DECIMAL(18, 4) NOT NULL,
            RollMinValue DECIMAL(18, 4) NOT NULL,
            RollMaxValue DECIMAL(18, 4) NOT NULL,
            RollQualityPercent DECIMAL(5, 2) NOT NULL,
            CreatedOn DATETIME NOT NULL CONSTRAINT DF_HRK_PlayerInventoryAttributes_CreatedOn DEFAULT GETDATE(),
            UpdatedOn DATETIME NOT NULL CONSTRAINT DF_HRK_PlayerInventoryAttributes_UpdatedOn DEFAULT GETDATE(),
            CONSTRAINT PK_HRK_PlayerInventoryAttributes PRIMARY KEY CLUSTERED (Id ASC),
            CONSTRAINT UQ_HRK_PlayerInventoryAttributes_Inv_Attr UNIQUE NONCLUSTERED (PlayerInventoryId ASC, AttributeTypeId ASC),
            CONSTRAINT FK_HRK_PlayerInventoryAttributes_Inventory FOREIGN KEY (PlayerInventoryId) REFERENCES dbo.HRK_PlayerInventory (Id) ON DELETE CASCADE,
            CONSTRAINT FK_HRK_PlayerInventoryAttributes_AttributeType FOREIGN KEY (AttributeTypeId) REFERENCES dbo.HRK_AttributeTypes (Id)
        );
        PRINT N'   + Đã tạo bảng HRK_PlayerInventoryAttributes.';
    END

    -- Backfill thuộc tính instance cho các trang bị hiện có qua sp_executesql
    EXEC sp_executesql N'
        INSERT INTO dbo.HRK_PlayerInventoryAttributes (
            PlayerInventoryId,
            AttributeTypeId,
            BaseRolledValue,
            CurrentValue,
            RollMinValue,
            RollMaxValue,
            RollQualityPercent,
            CreatedOn,
            UpdatedOn
        )
        SELECT 
            pi.Id AS PlayerInventoryId,
            ita.AttributeTypeId,
            ita.Value AS BaseRolledValue,
            CASE 
                WHEN at.IsPercentage = 1 THEN 
                    ROUND(ita.Value * (1.0 + (pi.Enhancement * ISNULL(pi.EnhancementGrowthPercent, 10.0) / 100.0)), 4)
                ELSE 
                    ROUND(ita.Value * (1.0 + (pi.Enhancement * ISNULL(pi.EnhancementGrowthPercent, 10.0) / 100.0)) * (1.0 + (pi.Stars * 0.10)), 2)
            END AS CurrentValue,
            ISNULL(ita.MinValue, ita.Value) AS RollMinValue,
            ISNULL(ita.MaxValue, ita.Value) AS RollMaxValue,
            50.00 AS RollQualityPercent,
            GETDATE(),
            GETDATE()
        FROM dbo.HRK_PlayerInventory pi
        JOIN dbo.HRK_ItemTemplates it ON pi.ItemTemplateId = it.Id
        JOIN dbo.HRK_ItemCategories ic ON it.CategoryId = ic.Id
        JOIN dbo.HRK_ItemTemplateAttributes ita ON it.Id = ita.ItemTemplateId
        JOIN dbo.HRK_AttributeTypes at ON ita.AttributeTypeId = at.Id
        LEFT JOIN dbo.HRK_PlayerInventoryAttributes existing 
            ON existing.PlayerInventoryId = pi.Id AND existing.AttributeTypeId = ita.AttributeTypeId
        WHERE ic.IsEquipment = 1 AND existing.Id IS NULL;
    ';

    PRINT N'   + Đã backfill HRK_PlayerInventoryAttributes cho tất cả trang bị hiện có.';

    -- ==============================================================================
    PRINT N'==> 5. Cập nhật HRK_DungeonRuns: Thêm FormationCode, Snapshot, Participants...';

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'HRK_DungeonRuns' AND COLUMN_NAME = 'FormationCode'
    )
    BEGIN
        ALTER TABLE dbo.HRK_DungeonRuns
        ADD FormationCode NVARCHAR(50) NULL;
        PRINT N'   + Đã thêm cột FormationCode vào HRK_DungeonRuns.';
    END

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'HRK_DungeonRuns' AND COLUMN_NAME = 'FormationSnapshotJson'
    )
    BEGIN
        ALTER TABLE dbo.HRK_DungeonRuns
        ADD FormationSnapshotJson NVARCHAR(MAX) NULL;
        PRINT N'   + Đã thêm cột FormationSnapshotJson vào HRK_DungeonRuns.';
    END

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'HRK_DungeonRuns' AND COLUMN_NAME = 'ParticipantHeroIdsJson'
    )
    BEGIN
        ALTER TABLE dbo.HRK_DungeonRuns
        ADD ParticipantHeroIdsJson NVARCHAR(MAX) NULL;
        PRINT N'   + Đã thêm cột ParticipantHeroIdsJson vào HRK_DungeonRuns.';
    END

    COMMIT TRANSACTION;
    PRINT N'==> GIAO DỊCH MIGRATION THÀNH CÔNG RỰC RỠ!';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    PRINT N'LỖI TRONG QUÁ TRÌNH MIGRATION: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
GO

-- ==============================================================================
-- Query xác nhận kết quả sau migration
-- ==============================================================================
SELECT 'RarityConfigs' AS Section, r.Code, c.EnhancementGrowthMinPercent, c.EnhancementGrowthMaxPercent, c.IsActive
FROM dbo.HRK_EquipmentRarityRollConfigs c
JOIN dbo.HRK_Rarities r ON c.RarityId = r.Id;

SELECT 'BackfilledItemsCount' AS Section, COUNT(*) AS TotalAttributes
FROM dbo.HRK_PlayerInventoryAttributes;

SELECT TOP 5 'SampleInventoryAttributes' AS Section,
    pia.PlayerInventoryId,
    it.Name AS ItemName,
    at.Code AS AttrCode,
    pia.BaseRolledValue,
    pia.CurrentValue,
    pia.RollMinValue,
    pia.RollMaxValue,
    pia.RollQualityPercent,
    pi.Enhancement,
    pi.EnhancementGrowthPercent
FROM dbo.HRK_PlayerInventoryAttributes pia
JOIN dbo.HRK_PlayerInventory pi ON pia.PlayerInventoryId = pi.Id
JOIN dbo.HRK_ItemTemplates it ON pi.ItemTemplateId = it.Id
JOIN dbo.HRK_AttributeTypes at ON pia.AttributeTypeId = at.Id;
GO

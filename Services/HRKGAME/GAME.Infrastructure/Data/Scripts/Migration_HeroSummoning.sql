-- Run after Migration_HeroStarsAndEquipmentDowngrade.sql and level experience configuration.
-- Prices/weights below are editable starting values, not a fixed economy contract.
SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('dbo.HRK_HeroStoneConfigs', 'U') IS NULL
        THROW 51000, 'Apply hero star / hero stone migrations before hero summoning.', 1;

    IF COL_LENGTH('dbo.HRK_PlayerWallets', 'RowVersion') IS NULL
        ALTER TABLE dbo.HRK_PlayerWallets ADD RowVersion ROWVERSION NOT NULL;

    IF OBJECT_ID('dbo.HRK_SummonPools', 'U') IS NULL
        CREATE TABLE dbo.HRK_SummonPools (
            Id INT IDENTITY PRIMARY KEY, Code NVARCHAR(30) NOT NULL UNIQUE,
            Name NVARCHAR(150) NOT NULL, Currency NVARCHAR(20) NOT NULL,
            SingleCost BIGINT NOT NULL, TenCost BIGINT NOT NULL,
            IsEnabled BIT NOT NULL DEFAULT 1, DisplayOrder INT NOT NULL DEFAULT 0,
            CONSTRAINT CK_SummonPool_Price CHECK (SingleCost > 0 AND TenCost > 0),
            CONSTRAINT CK_SummonPool_Currency CHECK (Currency IN ('GOLD','DIAMOND'))
        );
    IF OBJECT_ID('dbo.HRK_SummonPoolEntries', 'U') IS NULL
        CREATE TABLE dbo.HRK_SummonPoolEntries (
            Id INT IDENTITY PRIMARY KEY, PoolId INT NOT NULL, HeroTemplateId INT NOT NULL,
            Weight INT NOT NULL CHECK (Weight > 0), IsEnabled BIT NOT NULL DEFAULT 1,
            CONSTRAINT UQ_SummonEntry UNIQUE (PoolId, HeroTemplateId),
            CONSTRAINT FK_SummonEntry_Pool FOREIGN KEY (PoolId) REFERENCES dbo.HRK_SummonPools(Id) ON DELETE CASCADE,
            CONSTRAINT FK_SummonEntry_Hero FOREIGN KEY (HeroTemplateId) REFERENCES dbo.HRK_HeroTemplates(Id)
        );
    IF OBJECT_ID('dbo.HRK_SummonBatches', 'U') IS NULL
        CREATE TABLE dbo.HRK_SummonBatches (
            Id BIGINT IDENTITY PRIMARY KEY, PlayerId BIGINT NOT NULL, RequestId UNIQUEIDENTIFIER NOT NULL,
            PoolCode NVARCHAR(30) NOT NULL, Count INT NOT NULL CHECK (Count IN (1,10)),
            Currency NVARCHAR(20) NOT NULL, Cost BIGINT NOT NULL,
            ResultJson NVARCHAR(MAX) NOT NULL, CreatedOn DATETIME2 NOT NULL,
            CONSTRAINT UQ_SummonBatch_Request UNIQUE (PlayerId, RequestId),
            CONSTRAINT FK_SummonBatch_Player FOREIGN KEY (PlayerId) REFERENCES dbo.HRK_Players(Id)
        );
    IF OBJECT_ID('dbo.HRK_SummonResultItems', 'U') IS NULL
        CREATE TABLE dbo.HRK_SummonResultItems (
            Id BIGINT IDENTITY PRIMARY KEY, BatchId BIGINT NOT NULL,
            DrawIndex INT NOT NULL CHECK (DrawIndex BETWEEN 0 AND 9), HeroTemplateId INT NOT NULL,
            WasDuplicate BIT NOT NULL, ConvertedStoneQuantity INT NOT NULL,
            CONSTRAINT UQ_SummonResult_Draw UNIQUE (BatchId, DrawIndex),
            CONSTRAINT FK_SummonResult_Batch FOREIGN KEY (BatchId) REFERENCES dbo.HRK_SummonBatches(Id) ON DELETE CASCADE,
            CONSTRAINT FK_SummonResult_Hero FOREIGN KEY (HeroTemplateId) REFERENCES dbo.HRK_HeroTemplates(Id)
        );

    INSERT dbo.HRK_SummonPools(Code, Name, Currency, SingleCost, TenCost, IsEnabled, DisplayOrder)
    SELECT v.Code,v.Name,v.Currency,v.SingleCost,v.TenCost,1,v.DisplayOrder
    FROM (VALUES ('NORMAL',N'Chiêu mộ thường','GOLD',10000,100000,10),
                 ('PREMIUM',N'Chiêu mộ cao cấp','DIAMOND',200,2000,20)) v(Code,Name,Currency,SingleCost,TenCost,DisplayOrder)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.HRK_SummonPools p WHERE p.Code=v.Code);

    -- Equal weights inside each rarity. Approximate initial rarity totals:
    -- NORMAL Rare 80%, Epic 19%, Legendary 1%; PREMIUM Rare 65%, Epic 30%, Legendary 5%.
    -- UI probabilities are calculated from the actual current entries, including any rounding.
    ;WITH HeroCounts AS (
        SELECT h.Id,h.RarityId,r.Code,COUNT(*) OVER (PARTITION BY h.RarityId) HeroCount
        FROM dbo.HRK_HeroTemplates h JOIN dbo.HRK_Rarities r ON r.Id=h.RarityId
        WHERE r.Code IN ('RARE','EPIC','LEGENDARY')
    )
    INSERT dbo.HRK_SummonPoolEntries(PoolId,HeroTemplateId,Weight,IsEnabled)
    SELECT p.Id,h.Id,
        CASE p.Code WHEN 'NORMAL' THEN CASE h.Code WHEN 'RARE' THEN 80000000 WHEN 'EPIC' THEN 19000000 ELSE 1000000 END
                    ELSE CASE h.Code WHEN 'RARE' THEN 65000000 WHEN 'EPIC' THEN 30000000 ELSE 5000000 END END / h.HeroCount,1
    FROM dbo.HRK_SummonPools p CROSS JOIN HeroCounts h
    WHERE p.Code IN ('NORMAL','PREMIUM')
      AND NOT EXISTS (SELECT 1 FROM dbo.HRK_SummonPoolEntries e WHERE e.PoolId=p.Id AND e.HeroTemplateId=h.Id);

    -- Backfill conversion configuration for templates added after the original star migration.
    DECLARE @MaterialId INT=(SELECT Id FROM dbo.HRK_ItemCategories WHERE Code='MATERIAL');
    INSERT dbo.HRK_ItemTemplates(Code,CategoryId,RarityId,Name,LevelReq,Description,IsStackable,MaxStackSize,SellPrice,CreatedOn,UpdatedOn)
    SELECT CONCAT('HERO_',h.Id,'_STONE'),@MaterialId,h.RarityId,CONCAT(N'Đá ',h.Name),1,
           CONCAT(N'Đá linh hồn của ',h.Name),1,9999,100,SYSUTCDATETIME(),SYSUTCDATETIME()
    FROM dbo.HRK_HeroTemplates h
    WHERE EXISTS (SELECT 1 FROM dbo.HRK_SummonPoolEntries e WHERE e.HeroTemplateId=h.Id)
      AND NOT EXISTS (SELECT 1 FROM dbo.HRK_ItemTemplates i WHERE i.Code=CONCAT('HERO_',h.Id,'_STONE'));
    INSERT dbo.HRK_HeroStoneConfigs(HeroTemplateId,ItemTemplateId,DuplicateConversionQuantity,IsActive,CreatedOn,UpdatedOn)
    SELECT h.Id,i.Id,CASE r.Code WHEN 'LEGENDARY' THEN 50 WHEN 'EPIC' THEN 30 ELSE 20 END,1,SYSUTCDATETIME(),SYSUTCDATETIME()
    FROM dbo.HRK_HeroTemplates h JOIN dbo.HRK_Rarities r ON r.Id=h.RarityId
    JOIN dbo.HRK_ItemTemplates i ON i.Code=CONCAT('HERO_',h.Id,'_STONE')
    WHERE EXISTS (SELECT 1 FROM dbo.HRK_SummonPoolEntries e WHERE e.HeroTemplateId=h.Id)
      AND NOT EXISTS (SELECT 1 FROM dbo.HRK_HeroStoneConfigs c WHERE c.HeroTemplateId=h.Id);

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_GameFeatureConfigs WHERE Code='SUMMON')
        INSERT dbo.HRK_GameFeatureConfigs(Code,Name,Icon,Placement,ActionCode,DisplayOrder,IsEnabled,IsLocked,HasNotification,CreatedOn,UpdatedOn)
        VALUES('SUMMON',N'Chiêu mộ','bi-people-fill','TOP_EVENT','SUMMON',20,1,0,0,SYSUTCDATETIME(),SYSUTCDATETIME());
    ELSE
        UPDATE dbo.HRK_GameFeatureConfigs SET ActionCode='SUMMON',IsEnabled=1,IsLocked=0,UpdatedOn=SYSUTCDATETIME() WHERE Code='SUMMON';

    COMMIT;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    THROW;
END CATCH;

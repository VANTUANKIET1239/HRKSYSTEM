SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.HRK_HeroRarityUpgradeConfigs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_HeroRarityUpgradeConfigs
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HRK_HeroRarityUpgradeConfigs PRIMARY KEY,
        RarityId INT NOT NULL,
        StatGrowthRate DECIMAL(10,6) NOT NULL,
        BaseGoldCost BIGINT NOT NULL,
        GoldCostPerLevel BIGINT NOT NULL,
        BaseMaterialCost INT NOT NULL,
        MaterialCostPerLevel INT NOT NULL,
        MaxLevel INT NOT NULL CONSTRAINT DF_HRK_HeroRarityUpgradeConfigs_MaxLevel DEFAULT (100),
        UpdatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_HeroRarityUpgradeConfigs_UpdatedOn DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_HRK_HeroRarityUpgradeConfigs_RarityId UNIQUE (RarityId),
        CONSTRAINT FK_HRK_HeroRarityUpgradeConfigs_Rarity FOREIGN KEY (RarityId) REFERENCES dbo.HRK_Rarities(Id)
    );
END
GO

/* Existing rarity IDs: 2 = Rare, 3 = Epic, 4 = Legendary. Higher rarity grows more per level. */
MERGE dbo.HRK_HeroRarityUpgradeConfigs AS target
USING (VALUES
    (2, CAST(0.020000 AS DECIMAL(10,6)), 1000, 250, 10, 2, 100),
    (3, CAST(0.030000 AS DECIMAL(10,6)), 1200, 300, 12, 2, 100),
    (4, CAST(0.040000 AS DECIMAL(10,6)), 1500, 350, 15, 3, 100)
) AS source(RarityId, StatGrowthRate, BaseGoldCost, GoldCostPerLevel, BaseMaterialCost, MaterialCostPerLevel, MaxLevel)
ON target.RarityId = source.RarityId
WHEN MATCHED THEN UPDATE SET StatGrowthRate = source.StatGrowthRate, BaseGoldCost = source.BaseGoldCost,
    GoldCostPerLevel = source.GoldCostPerLevel, BaseMaterialCost = source.BaseMaterialCost,
    MaterialCostPerLevel = source.MaterialCostPerLevel, MaxLevel = source.MaxLevel, UpdatedOn = SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT (RarityId, StatGrowthRate, BaseGoldCost, GoldCostPerLevel, BaseMaterialCost, MaterialCostPerLevel, MaxLevel)
    VALUES (source.RarityId, source.StatGrowthRate, source.BaseGoldCost, source.GoldCostPerLevel, source.BaseMaterialCost, source.MaterialCostPerLevel, source.MaxLevel);
GO

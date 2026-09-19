/*
  Hero Management support schema (SQL Server).
  Existing core tables already cover heroes, templates, skills, ownership and equipment.
  This migration adds progression/aura configuration and enforces the current one-skill-per-hero rule.
*/

SET XACT_ABORT ON;
BEGIN TRANSACTION;

/* Keep the earliest configured skill when legacy data has more than one skill for a hero template. */
;WITH DuplicateSkills AS
(
    SELECT HeroTemplateId,
           SkillId,
           ROW_NUMBER() OVER (PARTITION BY HeroTemplateId ORDER BY SkillOrder, SkillId) AS RowNumber
    FROM dbo.HRK_HeroSkills
)
DELETE FROM DuplicateSkills WHERE RowNumber > 1;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_HRK_HeroSkills_HeroTemplateId'
      AND object_id = OBJECT_ID('dbo.HRK_HeroSkills')
)
BEGIN
    CREATE UNIQUE INDEX UX_HRK_HeroSkills_HeroTemplateId
        ON dbo.HRK_HeroSkills(HeroTemplateId);
END;

IF OBJECT_ID('dbo.HRK_HeroLevelConfigs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_HeroLevelConfigs
    (
        Level INT NOT NULL CONSTRAINT PK_HRK_HeroLevelConfigs PRIMARY KEY,
        RequiredExp INT NOT NULL,
        GoldCost BIGINT NOT NULL CONSTRAINT DF_HRK_HeroLevelConfigs_GoldCost DEFAULT (0),
        MaterialCost INT NOT NULL CONSTRAINT DF_HRK_HeroLevelConfigs_MaterialCost DEFAULT (0),
        PowerIncrease INT NOT NULL CONSTRAINT DF_HRK_HeroLevelConfigs_PowerIncrease DEFAULT (0),
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_HeroLevelConfigs_CreatedOn DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT CK_HRK_HeroLevelConfigs_Level CHECK (Level >= 1),
        CONSTRAINT CK_HRK_HeroLevelConfigs_Cost CHECK (RequiredExp >= 0 AND GoldCost >= 0 AND MaterialCost >= 0)
    );
END;

IF OBJECT_ID('dbo.HRK_HeroAuraConfigs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_HeroAuraConfigs
    (
        AuraTier TINYINT NOT NULL CONSTRAINT PK_HRK_HeroAuraConfigs PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        RequiredStars INT NOT NULL CONSTRAINT DF_HRK_HeroAuraConfigs_RequiredStars DEFAULT (1),
        GoldCost BIGINT NOT NULL CONSTRAINT DF_HRK_HeroAuraConfigs_GoldCost DEFAULT (0),
        MaterialCost INT NOT NULL CONSTRAINT DF_HRK_HeroAuraConfigs_MaterialCost DEFAULT (0),
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_HeroAuraConfigs_CreatedOn DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT CK_HRK_HeroAuraConfigs_Tier CHECK (AuraTier BETWEEN 1 AND 4),
        CONSTRAINT CK_HRK_HeroAuraConfigs_Cost CHECK (RequiredStars >= 1 AND GoldCost >= 0 AND MaterialCost >= 0)
    );

    INSERT INTO dbo.HRK_HeroAuraConfigs (AuraTier, Name, Description, RequiredStars, GoldCost, MaterialCost)
    VALUES
      (1, N'Hào Quang Xanh Băng', N'Tầng hào quang cơ bản.', 1, 0, 0),
      (2, N'Hào Quang Tím Hắc Ám', N'Tầng hào quang nâng cao.', 3, 50000, 50),
      (3, N'Hoàng Kim Thần Điện', N'Tầng hào quang hiếm.', 5, 150000, 150),
      (4, N'Lôi Quang Xích Thần', N'Tầng hào quang cao nhất.', 6, 500000, 500);
END;

COMMIT TRANSACTION;

/*
    Database-driven experience requirements for heroes and players.
    Existing hero level values are preserved; only missing levels are seeded.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('dbo.HRK_HeroLevelConfigs', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.HRK_HeroLevelConfigs
        (
            Level INT NOT NULL CONSTRAINT PK_HRK_HeroLevelConfigs PRIMARY KEY,
            RequiredExp INT NOT NULL,
            GoldCost BIGINT NOT NULL CONSTRAINT DF_HRK_HeroLevelConfigs_GoldCost DEFAULT (0),
            MaterialCost INT NOT NULL CONSTRAINT DF_HRK_HeroLevelConfigs_MaterialCost DEFAULT (0),
            PowerIncrease INT NOT NULL CONSTRAINT DF_HRK_HeroLevelConfigs_PowerIncrease DEFAULT (0),
            IsMaxLevel BIT NOT NULL CONSTRAINT DF_HRK_HeroLevelConfigs_IsMaxLevel DEFAULT (0),
            IsActive BIT NOT NULL CONSTRAINT DF_HRK_HeroLevelConfigs_IsActive DEFAULT (1),
            CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_HeroLevelConfigs_CreatedOn DEFAULT SYSUTCDATETIME(),
            UpdatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_HeroLevelConfigs_UpdatedOn DEFAULT SYSUTCDATETIME(),
            CONSTRAINT CK_HRK_HeroLevelConfigs_Level CHECK (Level >= 1),
            CONSTRAINT CK_HRK_HeroLevelConfigs_RequiredExp CHECK (RequiredExp >= 0)
        );
    END
    ELSE
    BEGIN
        IF COL_LENGTH('dbo.HRK_HeroLevelConfigs', 'IsMaxLevel') IS NULL
            ALTER TABLE dbo.HRK_HeroLevelConfigs ADD IsMaxLevel BIT NOT NULL
                CONSTRAINT DF_HRK_HeroLevelConfigs_IsMaxLevel DEFAULT (0);
        IF COL_LENGTH('dbo.HRK_HeroLevelConfigs', 'IsActive') IS NULL
            ALTER TABLE dbo.HRK_HeroLevelConfigs ADD IsActive BIT NOT NULL
                CONSTRAINT DF_HRK_HeroLevelConfigs_IsActive DEFAULT (1);
        IF COL_LENGTH('dbo.HRK_HeroLevelConfigs', 'UpdatedOn') IS NULL
            ALTER TABLE dbo.HRK_HeroLevelConfigs ADD UpdatedOn DATETIME2 NOT NULL
                CONSTRAINT DF_HRK_HeroLevelConfigs_UpdatedOn DEFAULT SYSUTCDATETIME();
    END;

    IF OBJECT_ID('dbo.HRK_PlayerLevelConfigs', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.HRK_PlayerLevelConfigs
        (
            Level INT NOT NULL CONSTRAINT PK_HRK_PlayerLevelConfigs PRIMARY KEY,
            RequiredExp INT NOT NULL,
            IsMaxLevel BIT NOT NULL CONSTRAINT DF_HRK_PlayerLevelConfigs_IsMaxLevel DEFAULT (0),
            IsActive BIT NOT NULL CONSTRAINT DF_HRK_PlayerLevelConfigs_IsActive DEFAULT (1),
            CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_PlayerLevelConfigs_CreatedOn DEFAULT SYSUTCDATETIME(),
            UpdatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_PlayerLevelConfigs_UpdatedOn DEFAULT SYSUTCDATETIME(),
            CONSTRAINT CK_HRK_PlayerLevelConfigs_Level CHECK (Level >= 1),
            CONSTRAINT CK_HRK_PlayerLevelConfigs_RequiredExp CHECK (RequiredExp >= 0)
        );
    END;

    ;WITH Levels AS
    (
        SELECT 1 AS Level
        UNION ALL
        SELECT Level + 1 FROM Levels WHERE Level < 100
    )
    INSERT INTO dbo.HRK_HeroLevelConfigs
        (Level, RequiredExp, GoldCost, MaterialCost, PowerIncrease,
         IsMaxLevel, IsActive, CreatedOn, UpdatedOn)
    SELECT Level,
           CASE WHEN Level = 100 THEN 0 ELSE Level * 100 + 400 END,
           0, 0, 0,
           CASE WHEN Level = 100 THEN 1 ELSE 0 END,
           1, SYSUTCDATETIME(), SYSUTCDATETIME()
    FROM Levels source
    WHERE NOT EXISTS
        (SELECT 1 FROM dbo.HRK_HeroLevelConfigs target WHERE target.Level = source.Level)
    OPTION (MAXRECURSION 100);

    ;WITH Levels AS
    (
        SELECT 1 AS Level
        UNION ALL
        SELECT Level + 1 FROM Levels WHERE Level < 100
    )
    INSERT INTO dbo.HRK_PlayerLevelConfigs
        (Level, RequiredExp, IsMaxLevel, IsActive, CreatedOn, UpdatedOn)
    SELECT Level,
           CASE WHEN Level = 100 THEN 0
                ELSE CONVERT(INT,
                    CASE
                        WHEN 1000.0 * POWER(CONVERT(FLOAT, 1.2), Level - 1) > 2000000000.0
                            THEN 2000000000.0
                        ELSE ROUND(1000.0 * POWER(CONVERT(FLOAT, 1.2), Level - 1), 0)
                    END)
           END,
           CASE WHEN Level = 100 THEN 1 ELSE 0 END,
           1, SYSUTCDATETIME(), SYSUTCDATETIME()
    FROM Levels source
    WHERE NOT EXISTS
        (SELECT 1 FROM dbo.HRK_PlayerLevelConfigs target WHERE target.Level = source.Level)
    OPTION (MAXRECURSION 100);

    UPDATE dbo.HRK_HeroLevelConfigs
    SET IsMaxLevel = CASE WHEN Level = 100 THEN 1 ELSE 0 END,
        RequiredExp = CASE WHEN Level = 100 THEN 0 ELSE RequiredExp END,
        UpdatedOn = SYSUTCDATETIME()
    WHERE Level BETWEEN 1 AND 100;

    UPDATE dbo.HRK_PlayerLevelConfigs
    SET IsMaxLevel = CASE WHEN Level = 100 THEN 1 ELSE 0 END,
        RequiredExp = CASE WHEN Level = 100 THEN 0 ELSE RequiredExp END,
        UpdatedOn = SYSUTCDATETIME()
    WHERE Level BETWEEN 1 AND 100;

    -- Make the persisted progress denominator immediately agree with configuration.
    UPDATE hero
    SET MaxExp = CASE WHEN config.IsMaxLevel = 1 THEN 0 ELSE config.RequiredExp END,
        Exp = CASE WHEN config.IsMaxLevel = 1 THEN 0 ELSE hero.Exp END,
        UpdatedOn = SYSUTCDATETIME()
    FROM dbo.HRK_PlayerHeroes hero
    INNER JOIN dbo.HRK_HeroLevelConfigs config ON config.Level = hero.Level AND config.IsActive = 1;

    UPDATE player
    SET MaxExp = CASE WHEN config.IsMaxLevel = 1 THEN 0 ELSE config.RequiredExp END,
        Exp = CASE WHEN config.IsMaxLevel = 1 THEN 0 ELSE player.Exp END,
        UpdatedOn = SYSUTCDATETIME()
    FROM dbo.HRK_Players player
    INNER JOIN dbo.HRK_PlayerLevelConfigs config ON config.Level = player.Level AND config.IsActive = 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

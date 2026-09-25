SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.HRK_DungeonMaps', N'U') IS NULL
        THROW 51000, 'Missing dbo.HRK_DungeonMaps. Run the dungeon campaign migration first.', 1;
    IF OBJECT_ID(N'dbo.HRK_Rarities', N'U') IS NULL
        THROW 51000, 'Missing dbo.HRK_Rarities.', 1;

    IF COL_LENGTH(N'dbo.HRK_DungeonMaps', N'MaxEquipmentRarityId') IS NULL
        ALTER TABLE dbo.HRK_DungeonMaps ADD MaxEquipmentRarityId INT NULL;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = N'FK_DungeonMaps_MaxEquipmentRarity'
          AND parent_object_id = OBJECT_ID(N'dbo.HRK_DungeonMaps')
    )
        ALTER TABLE dbo.HRK_DungeonMaps WITH CHECK
        ADD CONSTRAINT FK_DungeonMaps_MaxEquipmentRarity
            FOREIGN KEY (MaxEquipmentRarityId) REFERENCES dbo.HRK_Rarities(Id);

    DECLARE @CommonRarityId INT =
        (SELECT TOP (1) Id FROM dbo.HRK_Rarities WHERE UPPER(Code) = N'COMMON');
    DECLARE @EpicRarityId INT =
        (SELECT TOP (1) Id FROM dbo.HRK_Rarities WHERE UPPER(Code) = N'EPIC');

    IF @CommonRarityId IS NULL OR @EpicRarityId IS NULL
        THROW 51000, 'COMMON and EPIC rarities are required.', 1;

    UPDATE dbo.HRK_DungeonMaps
    SET MaxEquipmentRarityId = CASE WHEN DisplayOrder <= 3 THEN @CommonRarityId ELSE @EpicRarityId END
    WHERE MaxEquipmentRarityId IS NULL;

    IF OBJECT_ID(N'dbo.HRK_DungeonStarRatingConfigs', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.HRK_DungeonStarRatingConfigs
        (
            Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DungeonStarRatingConfigs PRIMARY KEY,
            DungeonMapId INT NULL,
            Stars INT NOT NULL,
            MinRemainingHpRate DECIMAL(5,4) NOT NULL,
            IsActive BIT NOT NULL CONSTRAINT DF_DungeonStarRatingConfigs_IsActive DEFAULT (1),
            UpdatedOn DATETIME2 NOT NULL CONSTRAINT DF_DungeonStarRatingConfigs_UpdatedOn DEFAULT SYSUTCDATETIME(),
            CONSTRAINT FK_DungeonStarRatingConfigs_Map FOREIGN KEY (DungeonMapId)
                REFERENCES dbo.HRK_DungeonMaps(Id) ON DELETE CASCADE,
            CONSTRAINT CK_DungeonStarRatingConfigs_Stars CHECK (Stars > 0),
            CONSTRAINT CK_DungeonStarRatingConfigs_HpRate CHECK (MinRemainingHpRate >= 0 AND MinRemainingHpRate <= 1)
        );

        CREATE UNIQUE INDEX UX_DungeonStarRatingConfigs_DefaultStars
            ON dbo.HRK_DungeonStarRatingConfigs(Stars)
            WHERE DungeonMapId IS NULL;
        CREATE UNIQUE INDEX UX_DungeonStarRatingConfigs_MapStars
            ON dbo.HRK_DungeonStarRatingConfigs(DungeonMapId, Stars)
            WHERE DungeonMapId IS NOT NULL;
    END;

    MERGE dbo.HRK_DungeonStarRatingConfigs AS target
    USING
    (
        VALUES
            (1, CAST(0.0000 AS DECIMAL(5,4))),
            (2, CAST(0.4000 AS DECIMAL(5,4))),
            (3, CAST(0.7000 AS DECIMAL(5,4)))
    ) AS source(Stars, MinRemainingHpRate)
    ON target.DungeonMapId IS NULL AND target.Stars = source.Stars
    WHEN MATCHED THEN UPDATE SET
        MinRemainingHpRate = source.MinRemainingHpRate,
        IsActive = 1,
        UpdatedOn = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN INSERT
        (DungeonMapId, Stars, MinRemainingHpRate, IsActive, UpdatedOn)
    VALUES
        (NULL, source.Stars, source.MinRemainingHpRate, 1, SYSUTCDATETIME());

    COMMIT TRANSACTION;

    SELECT m.Code, m.Name, r.Code AS MaxEquipmentRarity
    FROM dbo.HRK_DungeonMaps m
    LEFT JOIN dbo.HRK_Rarities r ON r.Id = m.MaxEquipmentRarityId
    ORDER BY m.DisplayOrder;

    SELECT DungeonMapId, Stars, MinRemainingHpRate, IsActive
    FROM dbo.HRK_DungeonStarRatingConfigs
    ORDER BY DungeonMapId, Stars;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

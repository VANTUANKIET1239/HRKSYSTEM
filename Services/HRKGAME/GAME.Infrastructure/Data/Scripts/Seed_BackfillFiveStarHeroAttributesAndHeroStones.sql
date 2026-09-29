/*
    Backfill dữ liệu tăng sao cho môi trường test.

    1. Tướng đang ở 5 sao được bổ sung đủ 1 thuộc tính phụ cho từng mốc sao 1..5.
    2. CurrentStats và Power của các tướng vừa được backfill được tính lại.
    3. Bảo đảm người chơi test có ít nhất 500 đá của mỗi nhân vật hiện có.

    Script có thể chạy lại an toàn:
    - Không tạo trùng thuộc tính đã tồn tại tại cùng mốc sao.
    - Không cộng dồn đá vô hạn; chỉ bù phần còn thiếu để tổng active đạt 500.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @TargetPlayerId BIGINT = 1;
DECLARE @HeroStoneTestQuantity INT = 500;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.HRK_Players
        WHERE Id = @TargetPlayerId
          AND IsActive = 1
    )
    BEGIN
        THROW 51000, 'Không tìm thấy người chơi test đang hoạt động.', 1;
    END;

    IF OBJECT_ID(N'dbo.HRK_PlayerHeroBonusAttributes', N'U') IS NULL
       OR OBJECT_ID(N'dbo.HRK_HeroStarAttributePools', N'U') IS NULL
       OR OBJECT_ID(N'dbo.HRK_HeroStoneConfigs', N'U') IS NULL
    BEGIN
        THROW 51001, 'Chưa chạy Migration_HeroStarsAndEquipmentDowngrade.sql.', 1;
    END;

    DECLARE @MaterialCategoryId INT =
    (
        SELECT TOP (1) Id
        FROM dbo.HRK_ItemCategories
        WHERE Code = N'MATERIAL'
    );

    IF @MaterialCategoryId IS NULL
    BEGIN
        THROW 51002, 'Thiếu item category MATERIAL.', 1;
    END;

    /*
        Bổ sung item/config đá cho các hero được thêm sau migration gốc.
    */
    INSERT INTO dbo.HRK_ItemTemplates
    (
        Code,
        CategoryId,
        RarityId,
        Name,
        ImagePath,
        LevelReq,
        Description,
        IsStackable,
        MaxStackSize,
        SellPrice,
        CreatedOn,
        UpdatedOn
    )
    SELECT
        CONCAT(N'HERO_', hero.Id, N'_STONE'),
        @MaterialCategoryId,
        hero.RarityId,
        CONCAT(N'Đá ', hero.Name),
        NULL,
        1,
        CONCAT(N'Đá linh hồn của ', hero.Name),
        1,
        9999,
        100,
        SYSUTCDATETIME(),
        SYSUTCDATETIME()
    FROM dbo.HRK_HeroTemplates AS hero
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.HRK_ItemTemplates AS item
        WHERE item.Code = CONCAT(N'HERO_', hero.Id, N'_STONE')
    );

    INSERT INTO dbo.HRK_HeroStoneConfigs
    (
        HeroTemplateId,
        ItemTemplateId,
        DuplicateConversionQuantity,
        IsActive,
        CreatedOn,
        UpdatedOn
    )
    SELECT
        hero.Id,
        item.Id,
        CASE rarity.Code
            WHEN N'MYTHIC' THEN 80
            WHEN N'LEGENDARY' THEN 50
            WHEN N'EPIC' THEN 30
            ELSE 20
        END,
        1,
        SYSUTCDATETIME(),
        SYSUTCDATETIME()
    FROM dbo.HRK_HeroTemplates AS hero
    INNER JOIN dbo.HRK_Rarities AS rarity
        ON rarity.Id = hero.RarityId
    INNER JOIN dbo.HRK_ItemTemplates AS item
        ON item.Code = CONCAT(N'HERO_', hero.Id, N'_STONE')
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.HRK_HeroStoneConfigs AS config
        WHERE config.HeroTemplateId = hero.Id
    );

    /*
        Mỗi tướng 5 sao phải có 5 dòng thuộc tính phụ, ứng với mốc 1..5.
        NEWID được lưu làm RollSeed; thuộc tính và giá trị chỉ roll cho mốc còn thiếu.
    */
    DECLARE @InsertedBonuses TABLE
    (
        PlayerHeroId BIGINT NOT NULL
    );

    ;WITH StarLevels AS
    (
        SELECT StarLevel
        FROM (VALUES (1), (2), (3), (4), (5)) AS levels(StarLevel)
    ),
    MissingStarBonuses AS
    (
        SELECT
            playerHero.Id AS PlayerHeroId,
            heroTemplate.RarityId,
            levels.StarLevel,
            NEWID() AS RollSeed
        FROM dbo.HRK_PlayerHeroes AS playerHero
        INNER JOIN dbo.HRK_HeroTemplates AS heroTemplate
            ON heroTemplate.Id = playerHero.HeroTemplateId
        CROSS JOIN StarLevels AS levels
        WHERE playerHero.IsActive = 1
          AND playerHero.Stars = 5
          AND NOT EXISTS
          (
              SELECT 1
              FROM dbo.HRK_PlayerHeroBonusAttributes AS bonus
              WHERE bonus.PlayerHeroId = playerHero.Id
                AND bonus.UnlockedAtStar = levels.StarLevel
          )
    )
    INSERT INTO dbo.HRK_PlayerHeroBonusAttributes
    (
        PlayerHeroId,
        UnlockedAtStar,
        AttributeTypeId,
        Value,
        IsPercentage,
        RollSeed,
        CreatedOn
    )
    OUTPUT inserted.PlayerHeroId INTO @InsertedBonuses(PlayerHeroId)
    SELECT
        missing.PlayerHeroId,
        missing.StarLevel,
        selectedPool.AttributeTypeId,
        ROUND
        (
            selectedPool.MinValue
            + (selectedPool.MaxValue - selectedPool.MinValue)
              * randomValue.Fraction,
            2
        ),
        selectedPool.IsPercentage,
        missing.RollSeed,
        SYSUTCDATETIME()
    FROM MissingStarBonuses AS missing
    CROSS APPLY
    (
        SELECT SUM(pool.Weight) AS TotalWeight
        FROM dbo.HRK_HeroStarAttributePools AS pool
        WHERE pool.RarityId = missing.RarityId
          AND pool.IsEnabled = 1
          AND pool.Weight > 0
    ) AS poolWeight
    CROSS APPLY
    (
        SELECT
            CONVERT
            (
                INT,
                CONVERT(BIGINT, CHECKSUM(missing.RollSeed)) & 2147483647
            ) % poolWeight.TotalWeight AS WeightCursor,
            CONVERT
            (
                DECIMAL(18, 8),
                (CONVERT(BIGINT, CHECKSUM(NEWID())) & 2147483647) % 10001
            ) / 10000.0 AS Fraction
    ) AS randomValue
    CROSS APPLY
    (
        SELECT TOP (1)
            weightedPool.AttributeTypeId,
            weightedPool.MinValue,
            weightedPool.MaxValue,
            weightedPool.IsPercentage
        FROM
        (
            SELECT
                pool.AttributeTypeId,
                pool.MinValue,
                pool.MaxValue,
                pool.IsPercentage,
                SUM(pool.Weight) OVER
                (
                    ORDER BY pool.Id
                    ROWS UNBOUNDED PRECEDING
                ) AS CumulativeWeight
            FROM dbo.HRK_HeroStarAttributePools AS pool
            WHERE pool.RarityId = missing.RarityId
              AND pool.IsEnabled = 1
              AND pool.Weight > 0
        ) AS weightedPool
        WHERE weightedPool.CumulativeWeight > randomValue.WeightCursor
        ORDER BY weightedPool.CumulativeWeight
    ) AS selectedPool;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.HRK_PlayerHeroes AS playerHero
        INNER JOIN dbo.HRK_HeroTemplates AS heroTemplate
            ON heroTemplate.Id = playerHero.HeroTemplateId
        WHERE playerHero.IsActive = 1
          AND playerHero.Stars = 5
          AND NOT EXISTS
          (
              SELECT 1
              FROM dbo.HRK_HeroStarAttributePools AS pool
              WHERE pool.RarityId = heroTemplate.RarityId
                AND pool.IsEnabled = 1
                AND pool.Weight > 0
          )
    )
    BEGIN
        THROW 51003, 'Có tướng 5 sao chưa được cấu hình pool thuộc tính.', 1;
    END;

    /*
        Tính lại snapshot chỉ số giống HeroProgressionStatService:
        multiplier = 1 + (level - 1) * rarityGrowth * (1 + starGrowthBonus).
    */
    CREATE TABLE #RecalculatedHeroStats
    (
        PlayerHeroId BIGINT NOT NULL PRIMARY KEY,
        Hp INT NOT NULL,
        Atk INT NOT NULL,
        Def INT NOT NULL,
        Spd INT NOT NULL,
        Crit DECIMAL(18, 4) NOT NULL,
        CritDmg DECIMAL(18, 4) NOT NULL,
        Lifesteal DECIMAL(18, 4) NOT NULL,
        Accuracy DECIMAL(18, 4) NOT NULL,
        Resistance DECIMAL(18, 4) NOT NULL,
        MagicDamage INT NOT NULL,
        MagicResistance INT NOT NULL
    );

    INSERT INTO #RecalculatedHeroStats
    (
        PlayerHeroId,
        Hp,
        Atk,
        Def,
        Spd,
        Crit,
        CritDmg,
        Lifesteal,
        Accuracy,
        Resistance,
        MagicDamage,
        MagicResistance
    )
    SELECT
        playerHero.Id,
        CONVERT(INT, ROUND(heroTemplate.BaseHp * growth.Multiplier, 0)),
        CONVERT(INT, ROUND(heroTemplate.BaseAtk * growth.Multiplier, 0)),
        CONVERT(INT, ROUND(heroTemplate.BaseDef * growth.Multiplier, 0)),
        CONVERT(INT, ROUND(heroTemplate.BaseSpd * growth.Multiplier, 0)),
        heroTemplate.BaseCrit + bonus.Crit,
        heroTemplate.BaseCritDmg + bonus.CritDmg,
        heroTemplate.BaseLifesteal + bonus.Lifesteal,
        heroTemplate.BaseAccuracy + bonus.Accuracy,
        heroTemplate.BaseResistance + bonus.Resistance,
        CONVERT(INT, ROUND(heroTemplate.BaseMagicDamage * growth.Multiplier, 0)),
        CONVERT(INT, ROUND(heroTemplate.BaseMagicResistance * growth.Multiplier, 0))
    FROM dbo.HRK_PlayerHeroes AS playerHero
    INNER JOIN dbo.HRK_HeroTemplates AS heroTemplate
        ON heroTemplate.Id = playerHero.HeroTemplateId
    INNER JOIN dbo.HRK_HeroRarityUpgradeConfigs AS rarityGrowth
        ON rarityGrowth.RarityId = heroTemplate.RarityId
    INNER JOIN dbo.HRK_HeroStarUpgradeConfigs AS starGrowth
        ON starGrowth.RarityId = heroTemplate.RarityId
       AND starGrowth.NextStar = playerHero.Stars
       AND starGrowth.IsEnabled = 1
    CROSS APPLY
    (
        SELECT
            1.0
            + (playerHero.Level - 1)
              * rarityGrowth.StatGrowthRate
              * (1.0 + starGrowth.GrowthBonusPercent) AS Multiplier
    ) AS growth
    CROSS APPLY
    (
        SELECT
            COALESCE(SUM(CASE WHEN attributeType.Code = N'CRIT_RATE' THEN value.Value ELSE 0 END), 0) AS Crit,
            COALESCE(SUM(CASE WHEN attributeType.Code = N'CRIT_DAMAGE' THEN value.Value ELSE 0 END), 0) AS CritDmg,
            COALESCE(SUM(CASE WHEN attributeType.Code = N'LIFESTEAL' THEN value.Value ELSE 0 END), 0) AS Lifesteal,
            COALESCE(SUM(CASE WHEN attributeType.Code = N'ACCURACY' THEN value.Value ELSE 0 END), 0) AS Accuracy,
            COALESCE(SUM(CASE WHEN attributeType.Code = N'RESISTANCE' THEN value.Value ELSE 0 END), 0) AS Resistance
        FROM dbo.HRK_PlayerHeroBonusAttributes AS value
        INNER JOIN dbo.HRK_AttributeTypes AS attributeType
            ON attributeType.Id = value.AttributeTypeId
        WHERE value.PlayerHeroId = playerHero.Id
    ) AS bonus
    WHERE playerHero.IsActive = 1
      AND playerHero.Stars = 5
      AND EXISTS
      (
          SELECT 1
          FROM @InsertedBonuses AS insertedBonus
          WHERE insertedBonus.PlayerHeroId = playerHero.Id
      );

    UPDATE playerHero
    SET
        CurrentStats = statsJson.CurrentStats,
        Power = powerValue.Power,
        -- AuraTier là hệ thống legacy và DB chỉ cho phép 1..4.
        -- Tướng 5 sao hiện được ánh xạ sang AuraTier cao nhất là 4.
        AuraTier = 4,
        UpdatedOn = SYSUTCDATETIME()
    FROM dbo.HRK_PlayerHeroes AS playerHero
    INNER JOIN #RecalculatedHeroStats AS stats
        ON stats.PlayerHeroId = playerHero.Id
    CROSS APPLY
    (
        SELECT
            stats.Hp AS Hp,
            stats.Atk AS Atk,
            stats.Def AS Def,
            stats.Spd AS Spd,
            stats.Crit AS Crit,
            stats.CritDmg AS CritDmg,
            stats.Lifesteal AS Lifesteal,
            stats.Accuracy AS Accuracy,
            stats.Resistance AS Resistance,
            stats.MagicDamage AS MagicDamage,
            stats.MagicResistance AS MagicResistance
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    ) AS statsJson(CurrentStats)
    CROSS APPLY
    (
        SELECT CONVERT
        (
            INT,
            ROUND
            (
                COALESCE(SUM(statValue.Value * combatConfig.PowerPerUnit), 0),
                0
            )
        ) AS Power
        FROM
        (
            SELECT
                config.StatCode,
                config.PowerPerUnit,
                ROW_NUMBER() OVER
                (
                    PARTITION BY
                        CASE UPPER(config.StatCode)
                            WHEN N'CRIT' THEN N'CRIT_RATE'
                            WHEN N'CRIT_DMG' THEN N'CRIT_DAMAGE'
                            WHEN N'MAGIC_ATK' THEN N'MAGIC_DAMAGE'
                            WHEN N'MATK' THEN N'MAGIC_DAMAGE'
                            WHEN N'MAGIC_RESIST' THEN N'MAGIC_RESISTANCE'
                            WHEN N'MRES' THEN N'MAGIC_RESISTANCE'
                            ELSE UPPER(config.StatCode)
                        END
                    ORDER BY config.DisplayOrder, config.Id
                ) AS ConfigOrder
            FROM dbo.HRK_CombatPowerConfigs AS config
            WHERE config.IsEnabled = 1
        ) AS combatConfig
        CROSS APPLY
        (
            SELECT
                CASE UPPER(combatConfig.StatCode)
                    WHEN N'HP' THEN CONVERT(DECIMAL(18, 4), stats.Hp)
                    WHEN N'ATK' THEN stats.Atk
                    WHEN N'DEF' THEN stats.Def
                    WHEN N'SPD' THEN stats.Spd
                    WHEN N'CRIT' THEN CASE WHEN stats.Crit > 0 AND stats.Crit <= 1 THEN stats.Crit * 100 ELSE stats.Crit END
                    WHEN N'CRIT_RATE' THEN CASE WHEN stats.Crit > 0 AND stats.Crit <= 1 THEN stats.Crit * 100 ELSE stats.Crit END
                    WHEN N'CRIT_DMG' THEN CASE WHEN stats.CritDmg > 0 AND stats.CritDmg <= 1 THEN stats.CritDmg * 100 ELSE stats.CritDmg END
                    WHEN N'CRIT_DAMAGE' THEN CASE WHEN stats.CritDmg > 0 AND stats.CritDmg <= 1 THEN stats.CritDmg * 100 ELSE stats.CritDmg END
                    WHEN N'LIFESTEAL' THEN CASE WHEN stats.Lifesteal > 0 AND stats.Lifesteal <= 1 THEN stats.Lifesteal * 100 ELSE stats.Lifesteal END
                    WHEN N'ACCURACY' THEN CASE WHEN stats.Accuracy > 0 AND stats.Accuracy <= 1 THEN stats.Accuracy * 100 ELSE stats.Accuracy END
                    WHEN N'RESISTANCE' THEN CASE WHEN stats.Resistance > 0 AND stats.Resistance <= 1 THEN stats.Resistance * 100 ELSE stats.Resistance END
                    WHEN N'MAGIC_DAMAGE' THEN stats.MagicDamage
                    WHEN N'MAGIC_ATK' THEN stats.MagicDamage
                    WHEN N'MATK' THEN stats.MagicDamage
                    WHEN N'MAGIC_RESISTANCE' THEN stats.MagicResistance
                    WHEN N'MAGIC_RESIST' THEN stats.MagicResistance
                    WHEN N'MRES' THEN stats.MagicResistance
                    ELSE 0
                END AS Value
        ) AS statValue
        WHERE combatConfig.ConfigOrder = 1
    ) AS powerValue;

    /*
        Bù đủ 500 đá của từng hero cho player test.
        Nếu đã có >= 500 thì giữ nguyên; nếu thiếu thì cộng phần chênh lệch vào
        stack active đầu tiên, hoặc tạo stack mới nếu chưa có.
    */
    ;WITH StoneTotals AS
    (
        SELECT
            config.ItemTemplateId,
            COALESCE(SUM(inventory.Count), 0) AS OwnedQuantity,
            MIN(inventory.Id) AS FirstInventoryId
        FROM dbo.HRK_HeroStoneConfigs AS config
        LEFT JOIN dbo.HRK_PlayerInventory AS inventory
            ON inventory.PlayerId = @TargetPlayerId
           AND inventory.ItemTemplateId = config.ItemTemplateId
           AND inventory.IsActive = 1
        WHERE config.IsActive = 1
        GROUP BY config.ItemTemplateId
    )
    UPDATE inventory
    SET
        Count = inventory.Count
            + (@HeroStoneTestQuantity - totals.OwnedQuantity),
        UpdatedOn = SYSUTCDATETIME()
    FROM dbo.HRK_PlayerInventory AS inventory
    INNER JOIN StoneTotals AS totals
        ON totals.FirstInventoryId = inventory.Id
    WHERE totals.OwnedQuantity < @HeroStoneTestQuantity;

    INSERT INTO dbo.HRK_PlayerInventory
    (
        PlayerId,
        ItemTemplateId,
        Count,
        Enhancement,
        Stars,
        IsEquipped,
        EquippedHeroId,
        IsLocked,
        IsActive,
        SlotIndex,
        CurrentStats,
        AcquiredOn,
        UpdatedOn
    )
    SELECT
        @TargetPlayerId,
        config.ItemTemplateId,
        @HeroStoneTestQuantity,
        0,
        0,
        0,
        NULL,
        0,
        1,
        NULL,
        NULL,
        SYSUTCDATETIME(),
        SYSUTCDATETIME()
    FROM dbo.HRK_HeroStoneConfigs AS config
    WHERE config.IsActive = 1
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.HRK_PlayerInventory AS inventory
          WHERE inventory.PlayerId = @TargetPlayerId
            AND inventory.ItemTemplateId = config.ItemTemplateId
            AND inventory.IsActive = 1
      );

    COMMIT TRANSACTION;

    SELECT
        playerHero.Id AS PlayerHeroId,
        heroTemplate.Name AS HeroName,
        playerHero.Stars,
        COUNT(bonus.Id) AS BonusAttributeCount
    FROM dbo.HRK_PlayerHeroes AS playerHero
    INNER JOIN dbo.HRK_HeroTemplates AS heroTemplate
        ON heroTemplate.Id = playerHero.HeroTemplateId
    LEFT JOIN dbo.HRK_PlayerHeroBonusAttributes AS bonus
        ON bonus.PlayerHeroId = playerHero.Id
    WHERE playerHero.IsActive = 1
      AND playerHero.Stars = 5
    GROUP BY playerHero.Id, heroTemplate.Name, playerHero.Stars
    ORDER BY playerHero.Id;

    SELECT
        heroTemplate.Id AS HeroTemplateId,
        heroTemplate.Name AS HeroName,
        item.Name AS StoneName,
        SUM(inventory.Count) AS OwnedQuantity
    FROM dbo.HRK_HeroStoneConfigs AS config
    INNER JOIN dbo.HRK_HeroTemplates AS heroTemplate
        ON heroTemplate.Id = config.HeroTemplateId
    INNER JOIN dbo.HRK_ItemTemplates AS item
        ON item.Id = config.ItemTemplateId
    INNER JOIN dbo.HRK_PlayerInventory AS inventory
        ON inventory.PlayerId = @TargetPlayerId
       AND inventory.ItemTemplateId = config.ItemTemplateId
       AND inventory.IsActive = 1
    WHERE config.IsActive = 1
    GROUP BY heroTemplate.Id, heroTemplate.Name, item.Name
    ORDER BY heroTemplate.Id;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;

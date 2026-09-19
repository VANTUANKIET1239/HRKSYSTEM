/*
  Adds BASIC_RANDOM_HEAL and normalizes the skills of HeroTemplateId 1..10.

  BASIC_RANDOM_HEAL is a basic action (NORMAL/ON_ATTACK, zero energy) that
  heals one random living ally for 20% of the caster's maximum HP.

  Unassigned skill templates are deliberately retained because historical
  battle logs or catalog data may still reference them.
*/
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('dbo.HRK_SkillTemplates', 'U') IS NULL
        OR OBJECT_ID('dbo.HRK_HeroSkills', 'U') IS NULL
        OR OBJECT_ID('dbo.HRK_SkillTargetTypes', 'U') IS NULL
        OR OBJECT_ID('dbo.HRK_SkillEffectTypes', 'U') IS NULL
        OR OBJECT_ID('dbo.HRK_SkillEffects', 'U') IS NULL
        OR OBJECT_ID('dbo.HRK_SkillEffectScalings', 'U') IS NULL
        THROW 51010, 'Run Migration_SkillSystemRefactor.sql first.', 1;

    MERGE dbo.HRK_SkillTargetTypes AS target
    USING (SELECT
        CAST('ALLY_RANDOM' AS NVARCHAR(50)) AS Code,
        CAST(N'1 đồng minh ngẫu nhiên' AS NVARCHAR(100)) AS Name,
        CAST('ALLY' AS NVARCHAR(20)) AS TargetSide,
        CAST('RANDOM' AS NVARCHAR(50)) AS SelectionRule,
        12 AS DisplayOrder
    ) AS source
    ON target.Code = source.Code
    WHEN MATCHED THEN UPDATE SET
        Name = source.Name,
        TargetSide = source.TargetSide,
        SelectionRule = source.SelectionRule,
        DisplayOrder = source.DisplayOrder,
        IsActive = 1,
        UpdatedOn = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (Code, Name, TargetSide, SelectionRule, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
        VALUES (source.Code, source.Name, source.TargetSide, source.SelectionRule,
                source.DisplayOrder, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    /* Supports schemas both before and after SkillSystemLegacyCleanup. */
    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'BASIC_RANDOM_HEAL')
    BEGIN
        IF COL_LENGTH('dbo.HRK_SkillTemplates', 'Cost') IS NOT NULL
        BEGIN
            EXEC sp_executesql N'
                INSERT INTO dbo.HRK_SkillTemplates
                (
                    Id, Name, Icon, Description,
                    Cost, CostTypeId, CategoryId, DamageTypeId, EffectTypeId,
                    DamageMultiplier, TargetType, Cooldown, PhaseDurations,
                    CreatedOn, ImagePath, SkillTypeCode, TriggerCode,
                    EnergyCost, DisplayOrder, IsActive, UpdatedOn
                )
                VALUES
                (
                    ''BASIC_RANDOM_HEAL'', N''Hồi Máu Cơ Bản'', ''bi-heart-pulse-fill'',
                    N''Hồi phục cho một đồng minh còn sống được chọn ngẫu nhiên.'',
                    0,
                    (SELECT TOP 1 Id FROM dbo.HRK_SkillCostTypes ORDER BY CASE WHEN Code = ''MP'' THEN 0 ELSE 1 END, Id),
                    (SELECT TOP 1 Id FROM dbo.HRK_SkillCategories ORDER BY CASE WHEN Code = ''basic'' THEN 0 ELSE 1 END, Id),
                    (SELECT TOP 1 Id FROM dbo.HRK_SkillDamageTypes ORDER BY CASE WHEN Code = ''Support'' THEN 0 ELSE 1 END, Id),
                    (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes ORDER BY CASE WHEN UPPER(Code) = ''HEAL'' THEN 0 ELSE 1 END, Id),
                    0, ''friendly_random'', ''0s'', NULL,
                    GETDATE(), NULL, ''NORMAL'', ''ON_ATTACK'', 0, 1, 1, SYSUTCDATETIME()
                );';
        END
        ELSE
        BEGIN
            INSERT INTO dbo.HRK_SkillTemplates
            (
                Id, Name, Icon, Description, ImagePath, SkillTypeCode,
                TriggerCode, EnergyCost, DisplayOrder, IsActive, CreatedOn, UpdatedOn
            )
            VALUES
            (
                'BASIC_RANDOM_HEAL', N'Hồi Máu Cơ Bản', 'bi-heart-pulse-fill',
                N'Hồi phục cho một đồng minh còn sống được chọn ngẫu nhiên.',
                NULL, 'NORMAL', 'ON_ATTACK', 0, 1, 1, GETDATE(), SYSUTCDATETIME()
            );
        END
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_SkillTemplates
        SET Name = N'Hồi Máu Cơ Bản',
            Icon = 'bi-heart-pulse-fill',
            Description = N'Hồi phục cho một đồng minh còn sống được chọn ngẫu nhiên.',
            SkillTypeCode = 'NORMAL', TriggerCode = 'ON_ATTACK', EnergyCost = 0,
            DisplayOrder = 1, IsActive = 1, UpdatedOn = SYSUTCDATETIME()
        WHERE Id = 'BASIC_RANDOM_HEAL';
    END

    DECLARE @HealEffectTypeId INT =
        (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'HEAL');
    DECLARE @AllyRandomTargetTypeId INT =
        (SELECT Id FROM dbo.HRK_SkillTargetTypes WHERE Code = 'ALLY_RANDOM');
    DECLARE @HpAttributeTypeId INT =
        (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes
         WHERE UPPER(Code) IN ('HP', 'HEALTH', 'MAXHP')
         ORDER BY CASE WHEN UPPER(Code) = 'HP' THEN 0 ELSE 1 END);

    IF @HealEffectTypeId IS NULL OR @AllyRandomTargetTypeId IS NULL OR @HpAttributeTypeId IS NULL
        THROW 51012, 'HEAL, ALLY_RANDOM, or HP metadata is missing.', 1;

    DELETE scaling
    FROM dbo.HRK_SkillEffectScalings scaling
    JOIN dbo.HRK_SkillEffects effect ON effect.Id = scaling.SkillEffectId
    WHERE effect.SkillId = 'BASIC_RANDOM_HEAL';

    DELETE FROM dbo.HRK_SkillEffects WHERE SkillId = 'BASIC_RANDOM_HEAL';

    INSERT INTO dbo.HRK_SkillEffects
    (
        SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue,
        DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive,
        CreatedOn, UpdatedOn
    )
    VALUES
    (
        'BASIC_RANDOM_HEAL', @HealEffectTypeId, @AllyRandomTargetTypeId, NULL, 0,
        NULL, 100, NULL, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    );

    DECLARE @BasicHealEffectId BIGINT = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings
        (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES
        (@BasicHealEffectId, @HpAttributeTypeId, 0.200000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_HRK_HeroSkills_HeroTemplateId'
               AND object_id = OBJECT_ID('dbo.HRK_HeroSkills'))
        DROP INDEX UX_HRK_HeroSkills_HeroTemplateId ON dbo.HRK_HeroSkills;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_HRK_HeroSkills_HeroTemplateId_SkillId'
                   AND object_id = OBJECT_ID('dbo.HRK_HeroSkills'))
        CREATE UNIQUE INDEX UX_HRK_HeroSkills_HeroTemplateId_SkillId
            ON dbo.HRK_HeroSkills(HeroTemplateId, SkillId);

    DECLARE @Mappings TABLE
    (
        HeroTemplateId INT NOT NULL,
        SkillId NVARCHAR(100) NOT NULL,
        SkillOrder TINYINT NOT NULL,
        PRIMARY KEY (HeroTemplateId, SkillId)
    );

    INSERT INTO @Mappings (HeroTemplateId, SkillId, SkillOrder) VALUES
        (1,  'NORMAL_ATTACK',                     1),
        (1,  'HEAVENLY_JUDGMENT',                2),
        (2,  'NORMAL_ATTACK',                     1),
        (2,  'VAX_A_MILLION_SANITZATION',        2),
        (3,  'NORMAL_ATTACK',                     1),
        (3,  'RICARDO_MILOS',                     2),
        (4,  'NORMAL_ATTACK',                     1),
        (4,  'RANDOM_KNOWLEDGE_DROP',             2),
        (5,  'NORMAL_ATTACK',                     1),
        (5,  'DARK_KNOWLEDGE_SHIELD_CONVERSION', 2),
        (6,  'NORMAL_ATTACK',                     1),
        (6,  'TACTICAL_AIR_STRIKE',               2),
        (7,  'NORMAL_ATTACK',                     1),
        (7,  'DOI_NGOI_DAU_DOC',                  2),
        (8,  'NORMAL_ATTACK',                     1),
        (8,  'FATAL_ALL_IN_DIRECTIVE',            2),
        (9,  'BASIC_RANDOM_HEAL',                 1),
        (9,  'WINTER_NIGHT_BLESSINGS',            2),
        (10, 'NORMAL_ATTACK',                     1),
        (10, 'DEADLIFT_DIA_CHAN',                 2);

    IF EXISTS (
        SELECT 1 FROM @Mappings m
        LEFT JOIN dbo.HRK_HeroTemplates h ON h.Id = m.HeroTemplateId
        LEFT JOIN dbo.HRK_SkillTemplates s ON s.Id = m.SkillId
        WHERE h.Id IS NULL OR s.Id IS NULL
    )
        THROW 51013, 'A required hero or skill template is missing.', 1;

    DELETE FROM dbo.HRK_HeroSkills WHERE HeroTemplateId BETWEEN 1 AND 10;
    INSERT INTO dbo.HRK_HeroSkills (HeroTemplateId, SkillId, SkillOrder)
    SELECT HeroTemplateId, SkillId, SkillOrder FROM @Mappings;

    IF EXISTS (
        SELECT hs.HeroTemplateId
        FROM dbo.HRK_HeroSkills hs
        JOIN dbo.HRK_SkillTemplates s ON s.Id = hs.SkillId
        WHERE hs.HeroTemplateId BETWEEN 1 AND 10
          AND s.SkillTypeCode = 'NORMAL'
          AND s.TriggerCode = 'ON_ATTACK'
          AND s.IsActive = 1
        GROUP BY hs.HeroTemplateId
        HAVING COUNT(*) <> 1
    )
        THROW 51014, 'Each seeded hero must have exactly one active basic skill.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT hs.HeroTemplateId, h.Name AS HeroName, hs.SkillId, s.Name AS SkillName,
       s.SkillTypeCode, s.TriggerCode, hs.SkillOrder
FROM dbo.HRK_HeroSkills hs
JOIN dbo.HRK_HeroTemplates h ON h.Id = hs.HeroTemplateId
JOIN dbo.HRK_SkillTemplates s ON s.Id = hs.SkillId
WHERE hs.HeroTemplateId BETWEEN 1 AND 10
ORDER BY hs.HeroTemplateId, hs.SkillOrder;

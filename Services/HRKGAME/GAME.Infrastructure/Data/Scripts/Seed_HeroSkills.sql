/*
  Canonical skill mapping for the 10 current DCS heroes.
  Run Migration_BasicAttackAndHealSkills.sql first.

  Every hero has exactly one NORMAL/ON_ATTACK skill at order 1.
  HeroTemplateId 9 heals one random ally; the others attack normally.
*/
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'BASIC_RANDOM_HEAL')
        THROW 51000, 'Missing BASIC_RANDOM_HEAL. Run Migration_BasicAttackAndHealSkills.sql first.', 1;

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
        SELECT 1
        FROM @Mappings m
        LEFT JOIN dbo.HRK_HeroTemplates h ON h.Id = m.HeroTemplateId
        LEFT JOIN dbo.HRK_SkillTemplates s ON s.Id = m.SkillId
        WHERE h.Id IS NULL OR s.Id IS NULL
    )
        THROW 51001, 'A required hero or skill template is missing.', 1;

    /* Preserve mappings for future heroes outside the current 1..10 set. */
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
        THROW 51002, 'Each seeded hero must have exactly one active basic skill.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT hs.HeroTemplateId, hs.SkillId, hs.SkillOrder
FROM dbo.HRK_HeroSkills hs
WHERE hs.HeroTemplateId BETWEEN 1 AND 10
ORDER BY hs.HeroTemplateId, hs.SkillOrder;

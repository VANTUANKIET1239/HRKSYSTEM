/*
  Reset and seed HRK_HeroSkills from INITIAL_HEROES.skills in mock-battle.data.ts.
*/
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_HRK_HeroSkills_HeroTemplateId'
           AND object_id = OBJECT_ID('dbo.HRK_HeroSkills'))
BEGIN
    DROP INDEX UX_HRK_HeroSkills_HeroTemplateId ON dbo.HRK_HeroSkills;
END;

/* A hero may own many skills, but a skill can be assigned once per hero. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_HRK_HeroSkills_HeroTemplateId_SkillId'
               AND object_id = OBJECT_ID('dbo.HRK_HeroSkills'))
BEGIN
    CREATE UNIQUE INDEX UX_HRK_HeroSkills_HeroTemplateId_SkillId
        ON dbo.HRK_HeroSkills(HeroTemplateId, SkillId);
END;

/* Intentional full reset requested: mappings not listed below are removed. */
DELETE FROM dbo.HRK_HeroSkills;

/*
  Mapping uses hero/skill names so it is portable across databases where IDs differ.
  A row is inserted only when both the hero template and skill template already exist.
  Add/change rows here when HRK_SkillTemplates receives new skills.
*/
DECLARE @Mappings TABLE (HeroName NVARCHAR(200), SkillId NVARCHAR(100), SkillOrder TINYINT);
INSERT INTO @Mappings (HeroName, SkillId, SkillOrder) VALUES
  (N'K Cởi Trần',          'HEAVENLY_JUDGMENT', 1),
  (N'K Cởi Trần',          'ULTIMATE_SIXPACK', 2),
  (N'Nam Deadline',        'NORMAL_ATTACK', 1),
  (N'Nam Deadline',        'HEAVY_SLASH', 2),
  (N'Nam Deadline',        'VAX_A_MILLION_SANITZATION', 3),
  (N'Chuẩn Men',           'NORMAL_ATTACK', 1),
  (N'Chuẩn Men',           'SLASH', 2),
  (N'Chuẩn Men',           'SWORD_DANCE', 3),
  (N'Chuẩn Men',           'RICARDO_MILOS', 4),
  (N'Coder Bảnh',          'NORMAL_ATTACK', 1),
  (N'Coder Bảnh',          'RANDOM_KNOWLEDGE_DROP', 2),
  (N'Coder Bảnh',          'REFACTOR_CODE', 3),
  (N'Coder Bảnh',          'DEPLOY_PROD', 4),
  (N'Tester Đẹp',          'NORMAL_ATTACK', 1),
  (N'Tester Đẹp',          'AUTOMATION_TEST', 2),
  (N'Tester Đẹp',          'DARK_KNOWLEDGE_SHIELD_CONVERSION', 3),
  (N'Tướng Long Quân Đội', 'NORMAL_ATTACK', 1),
  (N'Tướng Long Quân Đội', 'TACTICAL_AIR_STRIKE', 2),
  (N'PM Hối Hả',           'NORMAL_ATTACK', 1),
  (N'PM Hối Hả',           'DOI_NGOI_DAU_DOC', 2),
  (N'QA Kỹ Tính',          'NORMAL_ATTACK', 1),
  (N'QA Kỹ Tính',          'FATAL_ALL_IN_DIRECTIVE', 2),
  (N'Kiet Noel',           'NORMAL_ATTACK', 1),
  (N'Kiet Noel',           'WINTER_NIGHT_BLESSINGS', 2),
  (N'Hoàng Nguyên',        'NORMAL_ATTACK', 1),
  (N'Hoàng Nguyên',        'DEADLIFT_DIA_CHAN', 2);

INSERT INTO dbo.HRK_HeroSkills (HeroTemplateId, SkillId, SkillOrder)
SELECT h.Id, s.Id, m.SkillOrder
FROM @Mappings m
JOIN dbo.HRK_HeroTemplates h ON h.Name = m.HeroName
JOIN dbo.HRK_SkillTemplates s ON s.Id = m.SkillId;

COMMIT TRANSACTION;

/* Report missing mappings so seed data can be corrected without silently failing. */
SELECT m.HeroName, m.SkillId
FROM @Mappings m
WHERE NOT EXISTS (SELECT 1 FROM dbo.HRK_HeroTemplates h WHERE h.Name = m.HeroName)
   OR NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates s WHERE s.Id = m.SkillId);

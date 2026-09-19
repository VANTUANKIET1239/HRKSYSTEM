/*
  Normalizes the data-driven skill schema without deleting skill templates.
  Unassigned templates are deactivated to preserve history and allow reuse.
*/
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('dbo.HRK_SkillTemplates', 'U') IS NULL
       OR OBJECT_ID('dbo.HRK_HeroSkills', 'U') IS NULL
       OR OBJECT_ID('dbo.HRK_SkillEffects', 'U') IS NULL
       OR OBJECT_ID('dbo.HRK_SkillEffectTypes', 'U') IS NULL
       OR OBJECT_ID('dbo.HRK_SkillTargetTypes', 'U') IS NULL
       OR OBJECT_ID('dbo.HRK_SkillEffectScalings', 'U') IS NULL
       OR OBJECT_ID('dbo.HRK_SkillEffectStatModifiers', 'U') IS NULL
        THROW 51100, 'The data-driven skill schema is incomplete.', 1;

    /* Codes are API contracts: use uppercase snake case consistently. */
    UPDATE dbo.HRK_SkillEffectTypes
    SET Code = UPPER(REPLACE(Code, ' ', '_')), UpdatedOn = SYSUTCDATETIME()
    WHERE Code <> UPPER(REPLACE(Code, ' ', '_'));

    UPDATE dbo.HRK_SkillTargetTypes
    SET Code = UPPER(Code), TargetSide = UPPER(TargetSide),
        SelectionRule = UPPER(SelectionRule), UpdatedOn = SYSUTCDATETIME()
    WHERE Code <> UPPER(Code) OR TargetSide <> UPPER(TargetSide)
       OR SelectionRule <> UPPER(SelectionRule);

    UPDATE dbo.HRK_SkillTemplates
    SET SkillTypeCode = UPPER(SkillTypeCode), TriggerCode = UPPER(TriggerCode),
        UpdatedOn = SYSUTCDATETIME()
    WHERE SkillTypeCode <> UPPER(SkillTypeCode) OR TriggerCode <> UPPER(TriggerCode);

    UPDATE dbo.HRK_SkillEffects
    SET DamageSchoolCode = UPPER(DamageSchoolCode), UpdatedOn = SYSUTCDATETIME()
    WHERE DamageSchoolCode IS NOT NULL AND DamageSchoolCode <> UPPER(DamageSchoolCode);

    UPDATE dbo.HRK_SkillEffectStatModifiers
    SET ValueType = UPPER(ValueType), UpdatedOn = SYSUTCDATETIME()
    WHERE ValueType <> UPPER(ValueType);

    /* Harmful, beneficial and neutral are not simple boolean opposites. */
    UPDATE dbo.HRK_SkillEffectTypes
    SET IsDebuff = CASE WHEN Code IN
        ('BURN', 'POISON', 'STUN', 'FREEZE', 'SILENCE', 'TAUNT',
         'MARK', 'STAT_DEBUFF', 'ENERGY_DRAIN', 'POSITION_SWAP')
        THEN 1 ELSE 0 END,
        IsBeneficial = CASE WHEN Code IN
        ('HEAL', 'SHIELD', 'STAT_BUFF', 'DAMAGE_REDUCTION',
         'DAMAGE_REFLECTION', 'ENERGY_GAIN', 'REVIVE')
        THEN 1 ELSE 0 END,
        UpdatedOn = SYSUTCDATETIME();

    /* Normalize executable action metadata. */
    UPDATE dbo.HRK_SkillTemplates
    SET EnergyCost = 0, TriggerCode = 'ON_ATTACK', DisplayOrder = 1,
        UpdatedOn = SYSUTCDATETIME()
    WHERE SkillTypeCode = 'NORMAL';

    UPDATE dbo.HRK_SkillTemplates
    SET EnergyCost = 100, TriggerCode = 'MANUAL_ENERGY_FULL', DisplayOrder = 2,
        UpdatedOn = SYSUTCDATETIME()
    WHERE SkillTypeCode = 'ENERGY';

    /* Current catalog exposes only skills assigned to at least one hero. */
    UPDATE skill
    SET IsActive = CASE WHEN EXISTS
        (SELECT 1 FROM dbo.HRK_HeroSkills hs WHERE hs.SkillId = skill.Id)
        THEN 1 ELSE 0 END,
        UpdatedOn = SYSUTCDATETIME()
    FROM dbo.HRK_SkillTemplates skill;

    /* Basic skill first, remaining skills keep stable relative ordering. */
    ;WITH OrderedSkills AS
    (
        SELECT hs.HeroTemplateId, hs.SkillId,
               ROW_NUMBER() OVER
               (
                   PARTITION BY hs.HeroTemplateId
                   ORDER BY CASE WHEN skill.SkillTypeCode = 'NORMAL' THEN 0 ELSE 1 END,
                            hs.SkillOrder, hs.SkillId
               ) AS NewOrder
        FROM dbo.HRK_HeroSkills hs
        JOIN dbo.HRK_SkillTemplates skill ON skill.Id = hs.SkillId
    )
    UPDATE hs
    SET SkillOrder = ordered.NewOrder
    FROM dbo.HRK_HeroSkills hs
    JOIN OrderedSkills ordered ON ordered.HeroTemplateId = hs.HeroTemplateId
                              AND ordered.SkillId = hs.SkillId;

    /* PhaseDurations stores animation timing only; behavior lives in effects. */
    UPDATE dbo.HRK_SkillTemplates
    SET PhaseDurations = JSON_MODIFY(
                            JSON_MODIFY(
                              JSON_MODIFY(PhaseDurations, '$.healMultiplier', NULL),
                              '$.damageReductionMultiplier', NULL),
                            '$.redirectRatio', NULL),
        UpdatedOn = SYSUTCDATETIME()
    WHERE PhaseDurations IS NOT NULL AND ISJSON(PhaseDurations) = 1
      AND (JSON_VALUE(PhaseDurations, '$.healMultiplier') IS NOT NULL
        OR JSON_VALUE(PhaseDurations, '$.damageReductionMultiplier') IS NOT NULL
        OR JSON_VALUE(PhaseDurations, '$.redirectRatio') IS NOT NULL);

    /* Legacy generic rows duplicate target types or the standardized effects. */
    DELETE effectType
    FROM dbo.HRK_SkillEffectTypes effectType
    WHERE effectType.Code IN ('AOE', 'SINGLE_TARGET', 'BUFF', 'DEBUFF', 'SPECIAL')
      AND NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffects effect
                      WHERE effect.EffectTypeId = effectType.Id);

    /* Structural guards. */
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = 'UX_HRK_HeroSkills_HeroTemplateId_SkillOrder'
                     AND object_id = OBJECT_ID('dbo.HRK_HeroSkills'))
        CREATE UNIQUE INDEX UX_HRK_HeroSkills_HeroTemplateId_SkillOrder
            ON dbo.HRK_HeroSkills(HeroTemplateId, SkillOrder);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = 'UX_HRK_SkillEffects_SkillId_DisplayOrder'
                     AND object_id = OBJECT_ID('dbo.HRK_SkillEffects'))
        CREATE UNIQUE INDEX UX_HRK_SkillEffects_SkillId_DisplayOrder
            ON dbo.HRK_SkillEffects(SkillId, DisplayOrder);

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_HRK_SkillTemplates_SkillTypeCode')
        ALTER TABLE dbo.HRK_SkillTemplates WITH CHECK
        ADD CONSTRAINT CHK_HRK_SkillTemplates_SkillTypeCode
            CHECK (SkillTypeCode IN ('NORMAL', 'ENERGY', 'PASSIVE'));

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_HRK_SkillTemplates_EnergyCost')
        ALTER TABLE dbo.HRK_SkillTemplates WITH CHECK
        ADD CONSTRAINT CHK_HRK_SkillTemplates_EnergyCost
            CHECK (EnergyCost >= 0
               AND (SkillTypeCode <> 'NORMAL' OR EnergyCost = 0)
               AND (SkillTypeCode <> 'ENERGY' OR EnergyCost = 100));

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_HRK_SkillEffects_DamageSchoolCode')
        ALTER TABLE dbo.HRK_SkillEffects WITH CHECK
        ADD CONSTRAINT CHK_HRK_SkillEffects_DamageSchoolCode
            CHECK (DamageSchoolCode IS NULL OR DamageSchoolCode IN ('PHYSICAL', 'MAGIC', 'TRUE'));

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_HRK_SkillEffects_ChancePercent')
        ALTER TABLE dbo.HRK_SkillEffects WITH CHECK
        ADD CONSTRAINT CHK_HRK_SkillEffects_ChancePercent
            CHECK (ChancePercent >= 0 AND ChancePercent <= 100);

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_HRK_SkillEffects_DurationTurns')
        ALTER TABLE dbo.HRK_SkillEffects WITH CHECK
        ADD CONSTRAINT CHK_HRK_SkillEffects_DurationTurns
            CHECK (DurationTurns IS NULL OR DurationTurns > 0);

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_HRK_SkillEffectStatModifiers_ValueType')
        ALTER TABLE dbo.HRK_SkillEffectStatModifiers WITH CHECK
        ADD CONSTRAINT CHK_HRK_SkillEffectStatModifiers_ValueType
            CHECK (ValueType IN ('FLAT', 'PERCENT'));

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_HRK_SkillTargetTypes_TargetSide')
        ALTER TABLE dbo.HRK_SkillTargetTypes WITH CHECK
        ADD CONSTRAINT CHK_HRK_SkillTargetTypes_TargetSide
            CHECK (TargetSide IN ('SELF', 'ALLY', 'ENEMY'));

    /* Domain audits: fail and roll back rather than silently retain bad data. */
    IF EXISTS
    (
        SELECT hs.HeroTemplateId
        FROM dbo.HRK_HeroSkills hs
        JOIN dbo.HRK_SkillTemplates skill ON skill.Id = hs.SkillId
        GROUP BY hs.HeroTemplateId
        HAVING SUM(CASE WHEN skill.SkillTypeCode = 'NORMAL'
                         AND skill.TriggerCode = 'ON_ATTACK'
                         AND skill.IsActive = 1 THEN 1 ELSE 0 END) <> 1
    )
        THROW 51101, 'Each configured hero must have exactly one active basic skill.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.HRK_SkillEffects effect
        JOIN dbo.HRK_SkillEffectTypes effectType ON effectType.Id = effect.EffectTypeId
        WHERE effect.IsActive = 1
          AND effectType.Code IN ('DAMAGE', 'HEAL', 'SHIELD')
          AND effect.BaseValue = 0
          AND NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectScalings scaling
                          WHERE scaling.SkillEffectId = effect.Id)
    )
        THROW 51102, 'A DAMAGE, HEAL, or SHIELD effect has neither scaling nor base value.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

/* Post-migration audit. */
SELECT skill.Id, skill.Name, skill.SkillTypeCode, skill.TriggerCode,
       skill.EnergyCost, skill.IsActive,
       COUNT(DISTINCT hs.HeroTemplateId) AS AssignedHeroCount,
       COUNT(DISTINCT effect.Id) AS EffectCount
FROM dbo.HRK_SkillTemplates skill
LEFT JOIN dbo.HRK_HeroSkills hs ON hs.SkillId = skill.Id
LEFT JOIN dbo.HRK_SkillEffects effect ON effect.SkillId = skill.Id AND effect.IsActive = 1
GROUP BY skill.Id, skill.Name, skill.SkillTypeCode, skill.TriggerCode,
         skill.EnergyCost, skill.IsActive
ORDER BY skill.IsActive DESC, skill.SkillTypeCode, skill.Id;

SELECT effectType.Code, effectType.Name, effectType.EffectGroup,
       effectType.IsDebuff, effectType.IsBeneficial, COUNT(effect.Id) AS UsageCount
FROM dbo.HRK_SkillEffectTypes effectType
LEFT JOIN dbo.HRK_SkillEffects effect ON effect.EffectTypeId = effectType.Id
GROUP BY effectType.Code, effectType.Name, effectType.EffectGroup,
         effectType.IsDebuff, effectType.IsBeneficial
ORDER BY effectType.EffectGroup, effectType.Code;

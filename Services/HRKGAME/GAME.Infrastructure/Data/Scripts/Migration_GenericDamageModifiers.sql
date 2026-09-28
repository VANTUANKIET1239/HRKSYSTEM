SET XACT_ABORT ON;
BEGIN TRANSACTION;

/*
  Rename the old hero-specific Ricardo escape hatch to the generic outgoing
  status modifier contract used by DamageEffectHandler.
*/
UPDATE oldParameter
SET ParameterCode = 'IGNORE_OUTGOING_STATUS_DAMAGE_BONUS',
    UpdatedOn = SYSUTCDATETIME()
FROM dbo.HRK_SkillEffectParameters oldParameter
WHERE oldParameter.ParameterCode = 'IGNORE_RICARDO_DAMAGE_BONUS'
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.HRK_SkillEffectParameters existing
      WHERE existing.SkillEffectId = oldParameter.SkillEffectId
        AND existing.ParameterCode = 'IGNORE_OUTGOING_STATUS_DAMAGE_BONUS'
  );

DELETE oldParameter
FROM dbo.HRK_SkillEffectParameters oldParameter
WHERE oldParameter.ParameterCode = 'IGNORE_RICARDO_DAMAGE_BONUS'
  AND EXISTS
  (
      SELECT 1
      FROM dbo.HRK_SkillEffectParameters existing
      WHERE existing.SkillEffectId = oldParameter.SkillEffectId
        AND existing.ParameterCode = 'IGNORE_OUTGOING_STATUS_DAMAGE_BONUS'
  );

COMMIT TRANSACTION;

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @EffectId BIGINT =
(
    SELECT TOP 1 effect.Id
    FROM dbo.HRK_SkillEffects effect
    INNER JOIN dbo.HRK_SkillEffectTypes effectType ON effectType.Id = effect.EffectTypeId
    WHERE effect.SkillId = 'THANH_THAI_CRIMSON_BROOM_LIGHTNING'
      AND effectType.Code = 'DAMAGE'
      AND effect.DamageSchoolCode = 'PHYSICAL'
      AND effect.IsActive = 1
    ORDER BY effect.DisplayOrder
);

IF @EffectId IS NULL
    THROW 51120, 'Missing Thanh Thai Aura ultimate physical effect.', 1;

UPDATE dbo.HRK_SkillTemplates
SET Description = N'Phóng sét đỏ gây sát thương vật lý và phép, mặc định lan 3 lượt và khi đủ 100 Bá Khí lan 4 lượt rồi tiêu hao 100 Bá Khí. Lượt lan thiếu được chia đều vào các mục tiêu đã trúng. Mỗi mục tiêu có 40% nhận một tầng Mất Tự Tin; đủ 3 tầng sẽ phát nổ.',
    UpdatedOn = SYSUTCDATETIME()
WHERE Id = 'THANH_THAI_CRIMSON_BROOM_LIGHTNING';

UPDATE dbo.HRK_SkillEffectTypes
SET Description = N'Mỗi tầng tăng 10% sát thương nhận vào từ Thanh Thái Aura. Khi đạt đủ 3 tầng sẽ kích nổ và tiêu thụ toàn bộ tầng.',
    UpdatedOn = SYSUTCDATETIME()
WHERE Code = 'LOSS_OF_CONFIDENCE';

DELETE FROM dbo.HRK_SkillEffectParameters
WHERE SkillEffectId = @EffectId
  AND ParameterCode IN
  (
      'AURA_SKILL_THRESHOLD', 'AURA_CONSUMPTION',
      'LOW_AURA_MAX_TARGETS', 'HIGH_AURA_MAX_TARGETS'
  );

MERGE dbo.HRK_SkillEffectParameters AS target
USING
(
    VALUES
      (@EffectId, 'FULL_AURA_THRESHOLD',                    CAST(NULL AS DECIMAL(18,4)), 100, CAST(NULL AS BIT)),
      (@EffectId, 'FULL_AURA_CONSUMPTION',                  CAST(NULL AS DECIMAL(18,4)), 100, CAST(NULL AS BIT)),
      (@EffectId, 'DEFAULT_MAX_TARGETS',                    CAST(NULL AS DECIMAL(18,4)),   3, CAST(NULL AS BIT)),
      (@EffectId, 'FULL_AURA_MAX_TARGETS',                  CAST(NULL AS DECIMAL(18,4)),   4, CAST(NULL AS BIT)),
      (@EffectId, 'LOSS_OF_CONFIDENCE_SKILL_APPLY_CHANCE', CAST(40.0000 AS DECIMAL(18,4)), NULL, CAST(NULL AS BIT)),
      (@EffectId, 'LOSS_OF_CONFIDENCE_MAX_STACKS',         CAST(NULL AS DECIMAL(18,4)),   3, CAST(NULL AS BIT)),
      (@EffectId, 'LOSS_OF_CONFIDENCE_DURATION_TURNS',     CAST(NULL AS DECIMAL(18,4)),   6, CAST(NULL AS BIT)),
      (@EffectId, 'LOSS_OF_CONFIDENCE_DAMAGE_TAKEN_PER_STACK', CAST(10.0000 AS DECIMAL(18,4)), NULL, CAST(NULL AS BIT))
) AS source (SkillEffectId, ParameterCode, DecimalValue, IntValue, BoolValue)
ON target.SkillEffectId = source.SkillEffectId
AND target.ParameterCode = source.ParameterCode
WHEN MATCHED THEN UPDATE SET
    DecimalValue = source.DecimalValue,
    IntValue = source.IntValue,
    BoolValue = source.BoolValue,
    UpdatedOn = SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT
    (SkillEffectId, ParameterCode, DecimalValue, IntValue, BoolValue, CreatedOn, UpdatedOn)
VALUES
    (source.SkillEffectId, source.ParameterCode, source.DecimalValue, source.IntValue,
     source.BoolValue, SYSUTCDATETIME(), SYSUTCDATETIME());

COMMIT TRANSACTION;

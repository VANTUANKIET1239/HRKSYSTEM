/*
    Tao là nhất - combat balance/VFX event contract V2
    - Ultimate: 100% + 125% + 175% ATK
    - Bleed: 50%, 2 stacks, refresh duration
    - Each stack: 18% ATK/tick and 15% armor ignore
    - Ultimate detonates when a hit raises Bleed from 1 to 2 stacks
    - Remove TARGET_SURVIVED self DEF penalty
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @BleedEffectTypeId INT =
        (SELECT TOP (1) Id FROM dbo.HRK_SkillEffectTypes WHERE Code = 'BLEED');
    DECLARE @DamageEffectTypeId INT =
        (SELECT TOP (1) Id FROM dbo.HRK_SkillEffectTypes WHERE Code = 'DAMAGE');
    DECLARE @DetonateEffectTypeId INT =
        (SELECT TOP (1) Id FROM dbo.HRK_SkillEffectTypes WHERE Code = 'BLEED_DETONATE');
    DECLARE @StatDebuffEffectTypeId INT =
        (SELECT TOP (1) Id FROM dbo.HRK_SkillEffectTypes WHERE Code = 'STAT_DEBUFF');

    IF @BleedEffectTypeId IS NULL OR @DamageEffectTypeId IS NULL OR @DetonateEffectTypeId IS NULL
        THROW 51000, 'Missing required Hai Last Smile effect types.', 1;

    UPDATE dbo.HRK_SkillTemplates
    SET Description = N'Lao tới mục tiêu có % HP thấp nhất và chém 3 nhát, gây tổng cộng 400% ATK. Mỗi nhát có 50% gây Chảy Máu. Chảy Máu cộng tối đa 2 tầng và làm mới thời gian; khi một nhát cuối nâng từ tầng 1 lên tầng 2, toàn bộ sát thương Chảy Máu còn lại phát nổ và hiệu ứng bị xóa. Hạ gục mục tiêu hồi 25 năng lượng và nhận giảm sát thương.',
        UpdatedOn = SYSUTCDATETIME()
    WHERE Id = 'HAI_LAST_LAUGH';

    UPDATE dbo.HRK_SkillTemplates
    SET Description = N'Chém mục tiêu có % HP thấp nhất, gây sát thương vật lý và có 50% gây Chảy Máu. Chảy Máu cộng tối đa 2 tầng và làm mới thời gian khi áp dụng thành công.',
        UpdatedOn = SYSUTCDATETIME()
    WHERE Id = 'HAI_BUG_SLASH';

    UPDATE dbo.HRK_SkillEffects
    SET ChancePercent = 50.00,
        MaxStacks = 2,
        DurationTurns = 2,
        UpdatedOn = SYSUTCDATETIME()
    WHERE SkillId IN ('HAI_BUG_SLASH', 'HAI_LAST_LAUGH')
      AND EffectTypeId = @BleedEffectTypeId;

    DECLARE @BleedParams TABLE
    (
        SkillEffectId BIGINT NOT NULL,
        ParameterCode VARCHAR(100) NOT NULL,
        DecimalValue DECIMAL(18,4) NULL,
        BoolValue BIT NULL
    );

    INSERT INTO @BleedParams (SkillEffectId, ParameterCode, DecimalValue, BoolValue)
    SELECT Id, 'ARMOR_IGNORE_PERCENT', 15.0000, NULL
    FROM dbo.HRK_SkillEffects
    WHERE SkillId IN ('HAI_BUG_SLASH', 'HAI_LAST_LAUGH') AND EffectTypeId = @BleedEffectTypeId
    UNION ALL
    SELECT Id, 'STACK_ON_REAPPLY', NULL, 1
    FROM dbo.HRK_SkillEffects
    WHERE SkillId IN ('HAI_BUG_SLASH', 'HAI_LAST_LAUGH') AND EffectTypeId = @BleedEffectTypeId
    UNION ALL
    SELECT Id, 'REFRESH_ON_REAPPLY', NULL, 1
    FROM dbo.HRK_SkillEffects
    WHERE SkillId IN ('HAI_BUG_SLASH', 'HAI_LAST_LAUGH') AND EffectTypeId = @BleedEffectTypeId
    UNION ALL
    SELECT Id, 'SCALE_ARMOR_IGNORE_WITH_STACKS', NULL, 1
    FROM dbo.HRK_SkillEffects
    WHERE SkillId IN ('HAI_BUG_SLASH', 'HAI_LAST_LAUGH') AND EffectTypeId = @BleedEffectTypeId;

    MERGE dbo.HRK_SkillEffectParameters AS target
    USING @BleedParams AS source
      ON target.SkillEffectId = source.SkillEffectId
     AND target.ParameterCode = source.ParameterCode
    WHEN MATCHED THEN UPDATE SET
        DecimalValue = source.DecimalValue,
        BoolValue = source.BoolValue,
        UpdatedOn = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN INSERT
        (SkillEffectId, ParameterCode, DecimalValue, BoolValue, CreatedOn, UpdatedOn)
    VALUES
        (source.SkillEffectId, source.ParameterCode, source.DecimalValue, source.BoolValue,
         SYSUTCDATETIME(), SYSUTCDATETIME());

    UPDATE scaling
    SET Coefficient = CASE effect.ExecutionGroup
        WHEN 'HIT_1' THEN 1.000000
        WHEN 'HIT_2' THEN 1.250000
        WHEN 'HIT_3' THEN 1.750000
        ELSE scaling.Coefficient
    END,
    UpdatedOn = SYSUTCDATETIME()
    FROM dbo.HRK_SkillEffectScalings scaling
    INNER JOIN dbo.HRK_SkillEffects effect ON effect.Id = scaling.SkillEffectId
    WHERE effect.SkillId = 'HAI_LAST_LAUGH'
      AND effect.EffectTypeId = @DamageEffectTypeId
      AND effect.ExecutionGroup IN ('HIT_1', 'HIT_2', 'HIT_3');

    UPDATE parameter
    SET DecimalValue = 1.0000,
        UpdatedOn = SYSUTCDATETIME()
    FROM dbo.HRK_SkillEffectParameters parameter
    INNER JOIN dbo.HRK_SkillEffects effect ON effect.Id = parameter.SkillEffectId
    WHERE effect.SkillId = 'HAI_LAST_LAUGH'
      AND effect.EffectTypeId = @DetonateEffectTypeId
      AND parameter.ParameterCode = 'DETONATION_MULTIPLIER';

    -- Retain the effect row as data-driven detonation configuration, but the
    -- hero handler no longer executes it during SETUP.
    UPDATE dbo.HRK_SkillEffects
    SET IsActive = 0,
        UpdatedOn = SYSUTCDATETIME()
    WHERE SkillId = 'HAI_LAST_LAUGH'
      AND EffectTypeId = @StatDebuffEffectTypeId
      AND ConditionCode = 'TARGET_SURVIVED';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

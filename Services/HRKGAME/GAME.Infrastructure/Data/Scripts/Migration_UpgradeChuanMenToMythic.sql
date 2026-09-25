-- ============================================================================
-- Migration: Migration_UpgradeChuanMenToMythic.sql
-- Description:
--   1. Nâng phẩm chất Chuẩn Men (HeroTemplateId = 3) lên MYTHIC (Thần Thoại)
--   2. Tạo kỹ năng cơ bản riêng CHUAN_MEN_BASIC (110% ATK, Physical, Single Target, CanCrit)
--   3. Cập nhật kỹ năng RICARDO_MILOS với hiệu ứng thường và hiệu ứng cường hóa
--   4. Đăng ký Effect Type RICARDO (Phong Thái Nam Thần, Buff, MaxStacks 6)
--   5. Thiết lập đầy đủ Scalings, StatModifiers, Parameters theo chuẩn data-driven
--   6. Gán đúng 2 kỹ năng cho Chuẩn Men trong HRK_HeroSkills
-- Idempotent: Có thể chạy lại nhiều lần mà không gây lỗi hoặc trùng dữ liệu
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    ---------------------------------------------------------------------------
    -- 1. Tìm Chuẩn Men an toàn
    ---------------------------------------------------------------------------
    DECLARE @HeroTemplateId INT = (
        SELECT TOP 1 Id 
        FROM dbo.HRK_HeroTemplates 
        WHERE Id = 3 OR UPPER(Name) = N'CHUẨN MEN' OR Avatar LIKE '%ricardo-milos%'
    );

    IF @HeroTemplateId IS NULL
    BEGIN
        RAISERROR (N'Lỗi: Không tìm thấy HeroTemplate của Chuẩn Men trong dbo.HRK_HeroTemplates.', 16, 1);
        RETURN;
    END;

    ---------------------------------------------------------------------------
    -- 2. Tìm phẩm chất MYTHIC an toàn
    ---------------------------------------------------------------------------
    DECLARE @MythicRarityId INT = (
        SELECT TOP 1 Id 
        FROM dbo.HRK_Rarities 
        WHERE UPPER(Code) = 'MYTHIC'
    );

    IF @MythicRarityId IS NULL
    BEGIN
        SET @MythicRarityId = (SELECT TOP 1 Id FROM dbo.HRK_Rarities ORDER BY Id DESC);
    END;

    IF @MythicRarityId IS NULL
    BEGIN
        RAISERROR (N'Lỗi: Không tìm thấy phẩm chất MYTHIC trong dbo.HRK_Rarities.', 16, 1);
        RETURN;
    END;

    -- Cập nhật phẩm chất Chuẩn Men lên MYTHIC
    UPDATE dbo.HRK_HeroTemplates
    SET RarityId = @MythicRarityId
    WHERE Id = @HeroTemplateId;

    PRINT N'-> Đã nâng phẩm chất Chuẩn Men (Id: ' + CAST(@HeroTemplateId AS NVARCHAR(10)) + N') lên MYTHIC (RarityId: ' + CAST(@MythicRarityId AS NVARCHAR(10)) + N').';

    ---------------------------------------------------------------------------
    -- 3. Lookup các Target Type, Attribute Type, Effect Type bắt buộc
    ---------------------------------------------------------------------------
    DECLARE @AtkAttrId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE UPPER(Code) = 'ATK');
    DECLARE @DefAttrId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE UPPER(Code) = 'DEF');
    DECLARE @MagicResAttrId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE UPPER(Code) = 'MAGIC_RESISTANCE');

    IF @AtkAttrId IS NULL BEGIN RAISERROR (N'Lỗi: Thiếu AttributeType ATK.', 16, 1); RETURN; END;
    IF @DefAttrId IS NULL BEGIN RAISERROR (N'Lỗi: Thiếu AttributeType DEF.', 16, 1); RETURN; END;
    IF @MagicResAttrId IS NULL SET @MagicResAttrId = @DefAttrId;

    DECLARE @EnemySingleTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) IN ('ENEMY_SINGLE', 'SINGLE_ENEMY'));
    IF @EnemySingleTargetId IS NULL SET @EnemySingleTargetId = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) LIKE '%SINGLE%');

    DECLARE @SameLaneBackRowTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) = 'ENEMY_SAME_LANE_BACK_ROW');
    IF @SameLaneBackRowTargetId IS NULL SET @SameLaneBackRowTargetId = @EnemySingleTargetId;

    DECLARE @EnemyAllTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) IN ('ENEMY_ALL', 'ALL_ENEMIES'));
    IF @EnemyAllTargetId IS NULL SET @EnemyAllTargetId = @EnemySingleTargetId;

    DECLARE @EnemyRandomTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) IN ('ENEMY_RANDOM', 'RANDOM_ENEMY'));
    IF @EnemyRandomTargetId IS NULL SET @EnemyRandomTargetId = @EnemySingleTargetId;

    DECLARE @SelfTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) = 'SELF');
    IF @SelfTargetId IS NULL SET @SelfTargetId = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes ORDER BY Id ASC);

    DECLARE @DamageEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'DAMAGE');
    IF @DamageEffectTypeId IS NULL BEGIN RAISERROR (N'Lỗi: Thiếu SkillEffectType DAMAGE.', 16, 1); RETURN; END;

    DECLARE @StunEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'STUN');
    IF @StunEffectTypeId IS NULL BEGIN RAISERROR (N'Lỗi: Thiếu SkillEffectType STUN.', 16, 1); RETURN; END;

    ---------------------------------------------------------------------------
    -- 4. Đảm bảo Effect Type RICARDO tồn tại trong dbo.HRK_SkillEffectTypes
    ---------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'RICARDO')
    BEGIN
        INSERT INTO dbo.HRK_SkillEffectTypes
        (
            Code, Name, IsDebuff, EffectGroup, IsBeneficial, IsStackable,
            DefaultStackLimit, Description, ImagePath, ColorHex, CreatedOn, UpdatedOn
        )
        VALUES
        (
            'RICARDO', N'Phong Thái Nam Thần', 0, 'BUFF', 1, 1,
            6, N'Tăng 30% DEF và 30% Kháng Phép. Mỗi stack tăng 10% sát thương gây ra (tối đa 6 stack). Khi đạt 6 stack, Ricardo Milos! được cường hóa thành Thiên Giáng Nam Thần.',
            '/assets/images/dcs-game/effects/ricardo.png', '#dc2626', GETDATE(), SYSUTCDATETIME()
        );
        PRINT N'-> Đã thêm Effect Type RICARDO.';
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_SkillEffectTypes
        SET Name = N'Phong Thái Nam Thần',
            IsDebuff = 0,
            EffectGroup = 'BUFF',
            IsBeneficial = 1,
            IsStackable = 1,
            DefaultStackLimit = 6,
            Description = N'Tăng 30% DEF và 30% Kháng Phép. Mỗi stack tăng 10% sát thương gây ra (tối đa 6 stack). Khi đạt 6 stack, Ricardo Milos! được cường hóa thành Thiên Giáng Nam Thần.',
            ImagePath = '/assets/images/dcs-game/effects/ricardo.png',
            ColorHex = '#dc2626',
            UpdatedOn = SYSUTCDATETIME()
        WHERE UPPER(Code) = 'RICARDO';
        PRINT N'-> Đã cập nhật Effect Type RICARDO.';
    END;

    DECLARE @RicardoEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'RICARDO');

    ---------------------------------------------------------------------------
    -- 5. Tạo hoặc Cập nhật Kỹ năng Cơ bản CHUAN_MEN_BASIC
    ---------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'CHUAN_MEN_BASIC')
    BEGIN
        INSERT INTO dbo.HRK_SkillTemplates
        (
            Id, Name, Icon, Description, PhaseDurations,
            CreatedOn, ImagePath, SkillTypeCode, TriggerCode, EnergyCost,
            DisplayOrder, IsActive, UpdatedOn
        )
        VALUES
        (
            'CHUAN_MEN_BASIC', N'Cú Đấm Nam Thần', 'bi-hand-index-thumb-fill',
            N'Chuẩn Men tấn công một mục tiêu, gây 110% ATK sát thương vật lý. Đòn đánh có thể chí mạng.',
            NULL, GETDATE(), NULL, 'NORMAL', 'ON_ATTACK', 0,
            1, 1, SYSUTCDATETIME()
        );
        PRINT N'-> Đã tạo SkillTemplate CHUAN_MEN_BASIC.';
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_SkillTemplates
        SET Name = N'Cú Đấm Nam Thần',
            Description = N'Chuẩn Men tấn công một mục tiêu, gây 110% ATK sát thương vật lý. Đòn đánh có thể chí mạng.',
            SkillTypeCode = 'NORMAL',
            TriggerCode = 'ON_ATTACK',
            EnergyCost = 0,
            DisplayOrder = 1,
            IsActive = 1,
            UpdatedOn = SYSUTCDATETIME()
        WHERE Id = 'CHUAN_MEN_BASIC';
        PRINT N'-> Đã cập nhật SkillTemplate CHUAN_MEN_BASIC.';
    END;

    ---------------------------------------------------------------------------
    -- 6. Tạo hoặc Cập nhật Kỹ năng Nộ RICARDO_MILOS
    ---------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'RICARDO_MILOS')
    BEGIN
        INSERT INTO dbo.HRK_SkillTemplates
        (
            Id, Name, Icon, Description, PhaseDurations,
            CreatedOn, ImagePath, SkillTypeCode, TriggerCode, EnergyCost,
            DisplayOrder, IsActive, UpdatedOn
        )
        VALUES
        (
            'RICARDO_MILOS', N'Ricardo Milos!', 'bi-heart-pulse-fill',
            N'Chuẩn Men tung cú đấm Crimson uy lực gây 250% ATK sát thương vật lý, làm Choáng 1 lượt mục tiêu hàng sau cùng làn và nhận Phong Thái Nam Thần (+30% DEF, +30% Kháng Phép, +10% sát thương/tầng). Khi đủ 6 tầng, giải phóng Thiên Giáng Nam Thần gây 175% ATK AoE toàn đội và Choáng ngẫu nhiên 1 mục tiêu.',
            NULL, GETDATE(), NULL, 'ENERGY', 'MANUAL_ENERGY_FULL', 100,
            2, 1, SYSUTCDATETIME()
        );
        PRINT N'-> Đã tạo SkillTemplate RICARDO_MILOS.';
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_SkillTemplates
        SET Name = N'Ricardo Milos!',
            Description = N'Chuẩn Men tung cú đấm Crimson uy lực gây 250% ATK sát thương vật lý, làm Choáng 1 lượt mục tiêu hàng sau cùng làn và nhận Phong Thái Nam Thần (+30% DEF, +30% Kháng Phép, +10% sát thương/tầng). Khi đủ 6 tầng, giải phóng Thiên Giáng Nam Thần gây 175% ATK AoE toàn đội và Choáng ngẫu nhiên 1 mục tiêu.',
            SkillTypeCode = 'ENERGY',
            TriggerCode = 'MANUAL_ENERGY_FULL',
            EnergyCost = 100,
            DisplayOrder = 2,
            IsActive = 1,
            UpdatedOn = SYSUTCDATETIME()
        WHERE Id = 'RICARDO_MILOS';
        PRINT N'-> Đã cập nhật SkillTemplate RICARDO_MILOS.';
    END;

    ---------------------------------------------------------------------------
    -- 7. Gán chính xác 2 kỹ năng vào HRK_HeroSkills cho Chuẩn Men
    ---------------------------------------------------------------------------
    DELETE FROM dbo.HRK_HeroSkills WHERE HeroTemplateId = @HeroTemplateId;

    INSERT INTO dbo.HRK_HeroSkills (HeroTemplateId, SkillId, SkillOrder)
    VALUES
        (@HeroTemplateId, 'CHUAN_MEN_BASIC', 1),
        (@HeroTemplateId, 'RICARDO_MILOS', 2);

    PRINT N'-> Đã liên kết kỹ năng CHUAN_MEN_BASIC (1) và RICARDO_MILOS (2) cho Chuẩn Men.';

    ---------------------------------------------------------------------------
    -- 8. Làm sạch dữ liệu hiệu ứng cũ của 2 kỹ năng này trước khi gán lại
    ---------------------------------------------------------------------------
    DELETE p
    FROM dbo.HRK_SkillEffectParameters p
    INNER JOIN dbo.HRK_SkillEffects se ON se.Id = p.SkillEffectId
    WHERE se.SkillId IN ('CHUAN_MEN_BASIC', 'RICARDO_MILOS');

    DELETE sc
    FROM dbo.HRK_SkillEffectScalings sc
    INNER JOIN dbo.HRK_SkillEffects se ON se.Id = sc.SkillEffectId
    WHERE se.SkillId IN ('CHUAN_MEN_BASIC', 'RICARDO_MILOS');

    DELETE sm
    FROM dbo.HRK_SkillEffectStatModifiers sm
    INNER JOIN dbo.HRK_SkillEffects se ON se.Id = sm.SkillEffectId
    WHERE se.SkillId IN ('CHUAN_MEN_BASIC', 'RICARDO_MILOS');

    DELETE se
    FROM dbo.HRK_SkillEffects se
    WHERE se.SkillId IN ('CHUAN_MEN_BASIC', 'RICARDO_MILOS');

    ---------------------------------------------------------------------------
    -- 9. Gán hiệu ứng cho CHUAN_MEN_BASIC (110% ATK, Physical, Single Target, CanCrit)
    ---------------------------------------------------------------------------
    INSERT INTO dbo.HRK_SkillEffects
    (
        SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode,
        BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder,
        ExecutionGroup, ConditionCode, IsActive, CreatedOn, UpdatedOn
    )
    VALUES
    (
        'CHUAN_MEN_BASIC', @DamageEffectTypeId, @EnemySingleTargetId, 'PHYSICAL',
        0, NULL, 100.00, 1, 1,
        NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @BasicDmgEffectId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@BasicDmgEffectId, @AtkAttrId, 1.100000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@BasicDmgEffectId, 'CAN_CRIT', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    ---------------------------------------------------------------------------
    -- 10. Gán hiệu ứng cho RICARDO_MILOS
    ---------------------------------------------------------------------------
    
    -- 10.1 NORMAL GROUP: Damage 250% ATK lên hàng sau cùng làn
    INSERT INTO dbo.HRK_SkillEffects
    (
        SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode,
        BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder,
        ExecutionGroup, ConditionCode, IsActive, CreatedOn, UpdatedOn
    )
    VALUES
    (
        'RICARDO_MILOS', @DamageEffectTypeId, @SameLaneBackRowTargetId, 'PHYSICAL',
        0, NULL, 100.00, 1, 1,
        'NORMAL', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @NormalDmgEffectId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@NormalDmgEffectId, @AtkAttrId, 2.500000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, DecimalValue, BoolValue, CreatedOn, UpdatedOn)
    VALUES
        (@NormalDmgEffectId, 'CAN_CRIT', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@NormalDmgEffectId, 'NORMAL_DAMAGE_COEFFICIENT', 2.5000, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- 10.2 NORMAL GROUP: Stun 1 lượt với xác suất 100% lên hàng sau cùng làn
    INSERT INTO dbo.HRK_SkillEffects
    (
        SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode,
        BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder,
        ExecutionGroup, ConditionCode, IsActive, CreatedOn, UpdatedOn
    )
    VALUES
    (
        'RICARDO_MILOS', @StunEffectTypeId, @SameLaneBackRowTargetId, NULL,
        0, 1, 100.00, 1, 2,
        'NORMAL', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @NormalStunEffectId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, DecimalValue, IntValue, CreatedOn, UpdatedOn)
    VALUES
        (@NormalStunEffectId, 'NORMAL_STUN_CHANCE_PERCENT', 100.0000, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@NormalStunEffectId, 'STUN_DURATION_TURNS', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- 10.3 NORMAL GROUP: Áp dụng hoặc duy trì trạng thái RICARDO (Phong Thái Nam Thần)
    INSERT INTO dbo.HRK_SkillEffects
    (
        SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode,
        BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder,
        ExecutionGroup, ConditionCode, IsActive, CreatedOn, UpdatedOn
    )
    VALUES
    (
        'RICARDO_MILOS', @RicardoEffectTypeId, @SelfTargetId, NULL,
        10.00, NULL, 100.00, 6, 3,
        'NORMAL', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @RicardoBuffEffectId BIGINT = SCOPE_IDENTITY();

    -- Stat modifiers: DEF +30%, MAGIC_RESISTANCE +30% (cố định, không nhân theo stack)
    INSERT INTO dbo.HRK_SkillEffectStatModifiers (SkillEffectId, AttributeTypeId, ValueType, Value, CreatedOn, UpdatedOn)
    VALUES
        (@RicardoBuffEffectId, @DefAttrId, 'PERCENT', 30.00, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@RicardoBuffEffectId, @MagicResAttrId, 'PERCENT', 30.00, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Parameters cấu hình trạng thái RICARDO
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, DecimalValue, IntValue, BoolValue, CreatedOn, UpdatedOn)
    VALUES
        (@RicardoBuffEffectId, 'RICARDO_INITIAL_STACKS', NULL, 1, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@RicardoBuffEffectId, 'RICARDO_MAX_STACKS', NULL, 6, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@RicardoBuffEffectId, 'RICARDO_DAMAGE_PER_STACK_PERCENT', 10.0000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@RicardoBuffEffectId, 'RICARDO_DEF_PERCENT', 30.0000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@RicardoBuffEffectId, 'RICARDO_MAGIC_RESISTANCE_PERCENT', 30.0000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@RicardoBuffEffectId, 'ONE_STACK_PER_ENEMY_ACTION', NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@RicardoBuffEffectId, 'IGNORE_DOT_FOR_STACK', NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@RicardoBuffEffectId, 'REQUIRE_HP_LOSS_FOR_STACK', NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- 10.4 EMPOWERED GROUP: AoE Damage 175% ATK lên toàn bộ kẻ địch còn sống
    INSERT INTO dbo.HRK_SkillEffects
    (
        SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode,
        BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder,
        ExecutionGroup, ConditionCode, IsActive, CreatedOn, UpdatedOn
    )
    VALUES
    (
        'RICARDO_MILOS', @DamageEffectTypeId, @EnemyAllTargetId, 'PHYSICAL',
        0, NULL, 100.00, 1, 4,
        'EMPOWERED', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @EmpoweredDmgEffectId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EmpoweredDmgEffectId, @AtkAttrId, 1.750000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, DecimalValue, IntValue, BoolValue, CreatedOn, UpdatedOn)
    VALUES
        (@EmpoweredDmgEffectId, 'CAN_CRIT', NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EmpoweredDmgEffectId, 'IGNORE_RICARDO_DAMAGE_BONUS', NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EmpoweredDmgEffectId, 'REQUIRED_RICARDO_STACKS', NULL, 6, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EmpoweredDmgEffectId, 'CONSUME_RICARDO_AFTER_EXECUTION', NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EmpoweredDmgEffectId, 'EMPOWERED_DAMAGE_COEFFICIENT', 1.7500, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- 10.5 EMPOWERED GROUP: Choáng 1 lượt ngẫu nhiên một mục tiêu còn sống sau AoE
    INSERT INTO dbo.HRK_SkillEffects
    (
        SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode,
        BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder,
        ExecutionGroup, ConditionCode, IsActive, CreatedOn, UpdatedOn
    )
    VALUES
    (
        'RICARDO_MILOS', @StunEffectTypeId, @EnemyRandomTargetId, NULL,
        0, 1, 100.00, 1, 5,
        'EMPOWERED', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @EmpoweredStunEffectId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, DecimalValue, IntValue, CreatedOn, UpdatedOn)
    VALUES
        (@EmpoweredStunEffectId, 'EMPOWERED_RANDOM_STUN_CHANCE_PERCENT', 100.0000, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EmpoweredStunEffectId, 'STUN_DURATION_TURNS', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    PRINT N'-> Đã cấu hình đầy đủ effects, scalings, stat modifiers và parameters cho CHUAN_MEN_BASIC và RICARDO_MILOS.';

    COMMIT TRANSACTION;
    PRINT N'=== THÀNH CÔNG: Đã nâng cấp Chuẩn Men lên MYTHIC và cập nhật toàn bộ hệ thống kỹ năng! ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
GO

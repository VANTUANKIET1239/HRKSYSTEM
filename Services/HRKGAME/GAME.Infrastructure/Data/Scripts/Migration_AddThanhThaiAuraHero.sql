/*
    Migration: Add New Highest-Rarity Hybrid DPS Hero "Thanh Thái Aura"
    - Hero Code / Alias: THANH_THAI_AURA
    - Display Name: N'Thanh Thái Aura'
    - Rarity: Mythic (Highest tier currently in game)
    - Avatar: '/assets/images/dcs-game/thanh-thai-aura.png'
    - Resource: AURA (Bá Khí: 0 - 100)
    - Status Effects: FULL_AURA_FARMING, LOSS_OF_CONFIDENCE (Mất Tự Tin)
    - Skills:
        * Basic: THANH_THAI_DISRESPECTFUL_SWEEP (Quét Cho Có)
        * Energy: THANH_THAI_CRIMSON_BROOM_LIGHTNING (Lôi Chổi Xích Hồng)
    - Grant to PlayerId = 1
    Idempotent & Transaction-safe.
*/

USE [HRK];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    ---------------------------------------------------------------------------
    -- 1. Validate / Resolve mandatory reference data
    ---------------------------------------------------------------------------
    DECLARE @RarityId INT = (SELECT TOP 1 Id FROM dbo.HRK_Rarities WHERE UPPER(Code) = 'MYTHIC');
    IF @RarityId IS NULL
        SET @RarityId = (SELECT TOP 1 Id FROM dbo.HRK_Rarities ORDER BY Id DESC);

    IF @RarityId IS NULL
        THROW 50010, N'Lỗi: Bảng dbo.HRK_Rarities không có dữ liệu phẩm chất.', 1;

    DECLARE @ClassId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroClasses WHERE UPPER(Code) IN ('MAGE', 'WARRIOR', 'ASSASSIN'));
    IF @ClassId IS NULL
        SET @ClassId = (SELECT TOP 1 Id FROM dbo.HRK_HeroClasses ORDER BY Id ASC);

    DECLARE @FactionId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroFactions WHERE UPPER(Code) IN ('QUAN', 'NEUTRAL'));
    IF @FactionId IS NULL
        SET @FactionId = (SELECT TOP 1 Id FROM dbo.HRK_HeroFactions ORDER BY Id ASC);

    DECLARE @AtkAttrId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE UPPER(Code) = 'ATK');
    DECLARE @DefAttrId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE UPPER(Code) = 'DEF');
    DECLARE @MagicDmgAttrId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE UPPER(Code) = 'MAGIC_DAMAGE');
    DECLARE @MagicResAttrId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE UPPER(Code) = 'MAGIC_RESISTANCE');

    IF @AtkAttrId IS NULL THROW 50012, N'Lỗi: Thiếu AttributeType ATK.', 1;
    IF @DefAttrId IS NULL THROW 50013, N'Lỗi: Thiếu AttributeType DEF.', 1;
    IF @MagicDmgAttrId IS NULL SET @MagicDmgAttrId = @AtkAttrId;
    IF @MagicResAttrId IS NULL SET @MagicResAttrId = @DefAttrId;

    DECLARE @DamageEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'DAMAGE');
    IF @DamageEffectTypeId IS NULL THROW 50014, N'Lỗi: Thiếu SkillEffectType DAMAGE.', 1;

    DECLARE @EnemySingleTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) IN ('ENEMY_SINGLE', 'SINGLE_ENEMY'));
    IF @EnemySingleTargetId IS NULL SET @EnemySingleTargetId = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) LIKE '%SINGLE%');

    ---------------------------------------------------------------------------
    -- 2. Ensure Skill Effect Types exist: FULL_AURA_FARMING, LOSS_OF_CONFIDENCE
    ---------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'FULL_AURA_FARMING')
    BEGIN
        INSERT INTO dbo.HRK_SkillEffectTypes
        (
            Code, Name, IsDebuff, EffectGroup, IsBeneficial, IsStackable,
            DefaultStackLimit, Description, ImagePath, ColorHex, CreatedOn, UpdatedOn
        )
        VALUES
        (
            'FULL_AURA_FARMING', N'Full Aura Farming', 0, 'BUFF', 1, 0,
            1, N'Trạng thái đỉnh cao khi tích đủ 100 Bá Khí. Đánh thường có Độ Chính Xác Tuyệt Đối và áp dụng dấu Mất Tự Tin lên mục tiêu.',
            '/assets/images/dcs-game/effects/full-aura.png', '#dc2626', GETDATE(), SYSUTCDATETIME()
        );
        PRINT N'-> Đã thêm Effect Type FULL_AURA_FARMING.';
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'LOSS_OF_CONFIDENCE')
    BEGIN
        INSERT INTO dbo.HRK_SkillEffectTypes
        (
            Code, Name, IsDebuff, EffectGroup, IsBeneficial, IsStackable,
            DefaultStackLimit, Description, ImagePath, ColorHex, CreatedOn, UpdatedOn
        )
        VALUES
        (
            'LOSS_OF_CONFIDENCE', N'Mất Tự Tin', 1, 'DEBUFF', 0, 1,
            3, N'Mỗi tầng tăng 10% sát thương nhận vào từ Thanh Thái Aura (tối đa 3 tầng: +30%). Khi đạt đủ 3 tầng sẽ kích nổ và tiêu thụ toàn bộ tầng.',
            '/assets/images/dcs-game/effects/loss-of-confidence.png', '#b91c1c', GETDATE(), SYSUTCDATETIME()
        );
        PRINT N'-> Đã thêm Effect Type LOSS_OF_CONFIDENCE.';
    END;

    DECLARE @FullAuraEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'FULL_AURA_FARMING');
    DECLARE @LossOfConfidenceEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'LOSS_OF_CONFIDENCE');

    ---------------------------------------------------------------------------
    -- 3. Create or Update Hero Template "Thanh Thái Aura"
    ---------------------------------------------------------------------------
    DECLARE @HeroTemplateId INT = (
        SELECT TOP 1 Id 
        FROM dbo.HRK_HeroTemplates 
        WHERE UPPER(Name) = N'THANH THÁI AURA' OR Avatar LIKE '%thanh-thai-aura%'
    );

    IF @HeroTemplateId IS NULL
    BEGIN
        INSERT INTO dbo.HRK_HeroTemplates
        (
            Name, Avatar, FactionId, ClassId, RarityId,
            BaseHp, BaseAtk, BaseDef, BaseSpd, BaseCrit, BaseCritDmg,
            BaseLifesteal, BaseAccuracy, BaseResistance,
            BaseMagicDamage, BaseMagicResistance, CreatedOn
        )
        VALUES
        (
            N'Thanh Thái Aura',
            '/assets/images/dcs-game/thanh-thai-aura.png',
            @FactionId,
            @ClassId,
            @RarityId,
            750,     -- BaseHp (Trung bình)
            180,     -- BaseAtk (Cao)
            52,      -- BaseDef (Trung bình thấp)
            115,     -- BaseSpd (Trung bình)
            20.00,   -- BaseCrit (Khá)
            150.00,  -- BaseCritDmg
            0.00,    -- BaseLifesteal
            85.00,   -- BaseAccuracy (Bình thường khi chưa Full Aura)
            10.00,   -- BaseResistance (Trung bình)
            175,     -- BaseMagicDamage (Cao - Hybrid)
            58,      -- BaseMagicResistance (Trung bình)
            SYSUTCDATETIME()
        );
        SET @HeroTemplateId = SCOPE_IDENTITY();
        PRINT N'-> Đã tạo mới HeroTemplate Thanh Thái Aura (Id: ' + CAST(@HeroTemplateId AS NVARCHAR(10)) + N').';
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_HeroTemplates
        SET
            Name = N'Thanh Thái Aura',
            Avatar = '/assets/images/dcs-game/thanh-thai-aura.png',
            FactionId = @FactionId,
            ClassId = @ClassId,
            RarityId = @RarityId,
            BaseHp = 750,
            BaseAtk = 180,
            BaseDef = 52,
            BaseSpd = 115,
            BaseCrit = 20.00,
            BaseCritDmg = 150.00,
            BaseLifesteal = 0.00,
            BaseAccuracy = 85.00,
            BaseResistance = 10.00,
            BaseMagicDamage = 175,
            BaseMagicResistance = 58
        WHERE Id = @HeroTemplateId;
        PRINT N'-> Đã cập nhật HeroTemplate Thanh Thái Aura (Id: ' + CAST(@HeroTemplateId AS NVARCHAR(10)) + N').';
    END;

    ---------------------------------------------------------------------------
    -- 4. Create or Update Skill Templates:
    --    - THANH_THAI_DISRESPECTFUL_SWEEP (NORMAL)
    --    - THANH_THAI_CRIMSON_BROOM_LIGHTNING (ENERGY)
    ---------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'THANH_THAI_DISRESPECTFUL_SWEEP')
    BEGIN
        INSERT INTO dbo.HRK_SkillTemplates
        (
            Id, Name, Icon, Description, PhaseDurations,
            CreatedOn, ImagePath, SkillTypeCode, TriggerCode, EnergyCost,
            DisplayOrder, IsActive, UpdatedOn
        )
        VALUES
        (
            'THANH_THAI_DISRESPECTFUL_SWEEP', N'Quét Cho Có', 'bi-brush',
            N'Thanh Thái Aura vẫn giữ một tay che mặt, dùng tay còn lại quét cây chổi một cách hờ hững như đang phủi một thứ không đáng quan tâm. Gây 70% ATK Sát thương Vật lý và 60% Magic Damage Sát thương Phép. Nhận +10 Bá Khí. Ở trạng thái Full Aura Farming, đòn đánh có Độ Chính Xác Tuyệt Đối và áp dụng 1 tầng Mất Tự Tin.',
            NULL, GETDATE(), NULL, 'NORMAL', 'ON_ATTACK', 0,
            1, 1, SYSUTCDATETIME()
        );
        PRINT N'-> Đã tạo SkillTemplate THANH_THAI_DISRESPECTFUL_SWEEP.';
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_SkillTemplates
        SET Name = N'Quét Cho Có',
            Description = N'Thanh Thái Aura vẫn giữ một tay che mặt, dùng tay còn lại quét cây chổi một cách hờ hững như đang phủi một thứ không đáng quan tâm. Gây 70% ATK Sát thương Vật lý và 60% Magic Damage Sát thương Phép. Nhận +10 Bá Khí. Ở trạng thái Full Aura Farming, đòn đánh có Độ Chính Xác Tuyệt Đối và áp dụng 1 tầng Mất Tự Tin.',
            SkillTypeCode = 'NORMAL',
            TriggerCode = 'ON_ATTACK',
            EnergyCost = 0,
            DisplayOrder = 1,
            IsActive = 1,
            UpdatedOn = SYSUTCDATETIME()
        WHERE Id = 'THANH_THAI_DISRESPECTFUL_SWEEP';
        PRINT N'-> Đã cập nhật SkillTemplate THANH_THAI_DISRESPECTFUL_SWEEP.';
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'THANH_THAI_CRIMSON_BROOM_LIGHTNING')
    BEGIN
        INSERT INTO dbo.HRK_SkillTemplates
        (
            Id, Name, Icon, Description, PhaseDurations,
            CreatedOn, ImagePath, SkillTypeCode, TriggerCode, EnergyCost,
            DisplayOrder, IsActive, UpdatedOn
        )
        VALUES
        (
            'THANH_THAI_CRIMSON_BROOM_LIGHTNING', N'Lôi Chổi Xích Hồng', 'bi-lightning-charge-fill',
            N'Phóng sét đỏ gây sát thương vật lý và phép, mặc định lan 3 lượt và khi đủ 100 Bá Khí lan 4 lượt rồi tiêu hao 100 Bá Khí. Lượt lan thiếu được chia đều vào các mục tiêu đã trúng. Mỗi mục tiêu có 40% nhận một tầng Mất Tự Tin; đủ 3 tầng sẽ phát nổ.',
            NULL, GETDATE(), NULL, 'ENERGY', 'MANUAL_ENERGY_FULL', 100,
            2, 1, SYSUTCDATETIME()
        );
        PRINT N'-> Đã tạo SkillTemplate THANH_THAI_CRIMSON_BROOM_LIGHTNING.';
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_SkillTemplates
        SET Name = N'Lôi Chổi Xích Hồng',
            Description = N'Phóng sét đỏ gây sát thương vật lý và phép, mặc định lan 3 lượt và khi đủ 100 Bá Khí lan 4 lượt rồi tiêu hao 100 Bá Khí. Lượt lan thiếu được chia đều vào các mục tiêu đã trúng. Mỗi mục tiêu có 40% nhận một tầng Mất Tự Tin; đủ 3 tầng sẽ phát nổ.',
            SkillTypeCode = 'ENERGY',
            TriggerCode = 'MANUAL_ENERGY_FULL',
            EnergyCost = 100,
            DisplayOrder = 2,
            IsActive = 1,
            UpdatedOn = SYSUTCDATETIME()
        WHERE Id = 'THANH_THAI_CRIMSON_BROOM_LIGHTNING';
        PRINT N'-> Đã cập nhật SkillTemplate THANH_THAI_CRIMSON_BROOM_LIGHTNING.';
    END;

    ---------------------------------------------------------------------------
    -- 5. Link Skills to Hero in HRK_HeroSkills
    ---------------------------------------------------------------------------
    DELETE FROM dbo.HRK_HeroSkills WHERE HeroTemplateId = @HeroTemplateId;

    INSERT INTO dbo.HRK_HeroSkills (HeroTemplateId, SkillId, SkillOrder)
    VALUES
        (@HeroTemplateId, 'THANH_THAI_DISRESPECTFUL_SWEEP', 1),
        (@HeroTemplateId, 'THANH_THAI_CRIMSON_BROOM_LIGHTNING', 2);

    PRINT N'-> Đã liên kết kỹ năng THANH_THAI_DISRESPECTFUL_SWEEP (1) và THANH_THAI_CRIMSON_BROOM_LIGHTNING (2) cho Thanh Thái Aura.';

    ---------------------------------------------------------------------------
    -- 6. Clean and reseed Skill Effects, Scalings, Parameters
    ---------------------------------------------------------------------------
    DELETE p
    FROM dbo.HRK_SkillEffectParameters p
    INNER JOIN dbo.HRK_SkillEffects se ON se.Id = p.SkillEffectId
    WHERE se.SkillId IN ('THANH_THAI_DISRESPECTFUL_SWEEP', 'THANH_THAI_CRIMSON_BROOM_LIGHTNING');

    DELETE sc
    FROM dbo.HRK_SkillEffectScalings sc
    INNER JOIN dbo.HRK_SkillEffects se ON se.Id = sc.SkillEffectId
    WHERE se.SkillId IN ('THANH_THAI_DISRESPECTFUL_SWEEP', 'THANH_THAI_CRIMSON_BROOM_LIGHTNING');

    DELETE sm
    FROM dbo.HRK_SkillEffectStatModifiers sm
    INNER JOIN dbo.HRK_SkillEffects se ON se.Id = sm.SkillEffectId
    WHERE se.SkillId IN ('THANH_THAI_DISRESPECTFUL_SWEEP', 'THANH_THAI_CRIMSON_BROOM_LIGHTNING');

    DELETE se
    FROM dbo.HRK_SkillEffects se
    WHERE se.SkillId IN ('THANH_THAI_DISRESPECTFUL_SWEEP', 'THANH_THAI_CRIMSON_BROOM_LIGHTNING');

    ---------------------------------------------------------------------------
    -- 6.1 Seed Effects for THANH_THAI_DISRESPECTFUL_SWEEP
    ---------------------------------------------------------------------------
    -- Effect 1: Physical Damage 70% ATK
    INSERT INTO dbo.HRK_SkillEffects
    (
        SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode,
        BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder,
        ExecutionGroup, ConditionCode, IsActive, CreatedOn, UpdatedOn
    )
    VALUES
    (
        'THANH_THAI_DISRESPECTFUL_SWEEP', @DamageEffectTypeId, @EnemySingleTargetId, 'PHYSICAL',
        0, NULL, 100.00, 1, 1,
        NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @BasicPhysEffectId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@BasicPhysEffectId, @AtkAttrId, 0.700000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, DecimalValue, IntValue, BoolValue, CreatedOn, UpdatedOn)
    VALUES
        (@BasicPhysEffectId, 'CAN_CRIT', NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@BasicPhysEffectId, 'AURA_GAIN_BASIC', NULL, 10, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@BasicPhysEffectId, 'LOSS_OF_CONFIDENCE_MAX_STACKS', NULL, 3, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@BasicPhysEffectId, 'LOSS_OF_CONFIDENCE_DURATION_TURNS', NULL, 6, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@BasicPhysEffectId, 'LOSS_OF_CONFIDENCE_DAMAGE_TAKEN_PER_STACK', 10.0000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@BasicPhysEffectId, 'ABSOLUTE_ACCURACY', NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@BasicPhysEffectId, 'UNDISPELLABLE', NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 2: Magic Damage 60% MagicDamage
    INSERT INTO dbo.HRK_SkillEffects
    (
        SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode,
        BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder,
        ExecutionGroup, ConditionCode, IsActive, CreatedOn, UpdatedOn
    )
    VALUES
    (
        'THANH_THAI_DISRESPECTFUL_SWEEP', @DamageEffectTypeId, @EnemySingleTargetId, 'MAGIC',
        0, NULL, 100.00, 1, 2,
        NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @BasicMagEffectId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@BasicMagEffectId, @MagicDmgAttrId, 0.600000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@BasicMagEffectId, 'CAN_CRIT', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    ---------------------------------------------------------------------------
    -- 6.2 Seed Effects for THANH_THAI_CRIMSON_BROOM_LIGHTNING
    ---------------------------------------------------------------------------
    -- Effect 1: Physical Damage 90% ATK with Chain & Detonation parameters
    INSERT INTO dbo.HRK_SkillEffects
    (
        SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode,
        BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder,
        ExecutionGroup, ConditionCode, IsActive, CreatedOn, UpdatedOn
    )
    VALUES
    (
        'THANH_THAI_CRIMSON_BROOM_LIGHTNING', @DamageEffectTypeId, @EnemySingleTargetId, 'PHYSICAL',
        0, NULL, 100.00, 1, 1,
        NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @UltPhysEffectId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@UltPhysEffectId, @AtkAttrId, 0.900000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, DecimalValue, IntValue, BoolValue, CreatedOn, UpdatedOn)
    VALUES
        (@UltPhysEffectId, 'CAN_CRIT', NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltPhysEffectId, 'FULL_AURA_THRESHOLD', NULL, 100, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltPhysEffectId, 'FULL_AURA_CONSUMPTION', NULL, 100, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltPhysEffectId, 'DEFAULT_MAX_TARGETS', NULL, 3, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltPhysEffectId, 'FULL_AURA_MAX_TARGETS', NULL, 4, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltPhysEffectId, 'CHAIN_RANGE', 1.5000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltPhysEffectId, 'CHAIN_DAMAGE_DECAY_PERCENT', 15.0000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltPhysEffectId, 'AURA_GAIN_DEFEAT', NULL, 15, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltPhysEffectId, 'LOSS_OF_CONFIDENCE_MAX_STACKS', NULL, 3, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltPhysEffectId, 'LOSS_OF_CONFIDENCE_DURATION_TURNS', NULL, 6, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltPhysEffectId, 'LOSS_OF_CONFIDENCE_DAMAGE_TAKEN_PER_STACK', 10.0000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltPhysEffectId, 'LOSS_OF_CONFIDENCE_SKILL_APPLY_CHANCE', 40.0000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltPhysEffectId, 'DETONATION_PHYSICAL_STACK_1', 0.3000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltPhysEffectId, 'DETONATION_MAGIC_STACK_1', 0.2000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltPhysEffectId, 'DETONATION_PHYSICAL_STACK_2', 0.6000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltPhysEffectId, 'DETONATION_MAGIC_STACK_2', 0.4000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltPhysEffectId, 'DETONATION_PHYSICAL_STACK_3', 1.0000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltPhysEffectId, 'DETONATION_MAGIC_STACK_3', 0.7000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltPhysEffectId, 'DETONATION_CAN_CRIT', NULL, NULL, 0, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltPhysEffectId, 'DETONATION_CAN_KILL', NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 2: Magic Damage 70% MagicDamage
    INSERT INTO dbo.HRK_SkillEffects
    (
        SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode,
        BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder,
        ExecutionGroup, ConditionCode, IsActive, CreatedOn, UpdatedOn
    )
    VALUES
    (
        'THANH_THAI_CRIMSON_BROOM_LIGHTNING', @DamageEffectTypeId, @EnemySingleTargetId, 'MAGIC',
        0, NULL, 100.00, 1, 2,
        NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @UltMagEffectId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@UltMagEffectId, @MagicDmgAttrId, 0.700000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@UltMagEffectId, 'CAN_CRIT', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    PRINT N'-> Đã cấu hình xong hiệu ứng và parameters cho kỹ năng của Thanh Thái Aura.';

    ---------------------------------------------------------------------------
    -- 7. Seed Skill Animation Configs & Timeline Phases
    ---------------------------------------------------------------------------
    -- 7.1 Basic sweep animation config
    MERGE dbo.HRK_SkillAnimationConfigs AS target
    USING (VALUES ('THANH_THAI_DISRESPECTFUL_SWEEP', 'thanh-thai-sweep', 2000, CAST(1.0 AS DECIMAL(5,2))))
        AS source (SkillId, AnimationKey, TotalDurationMs, DefaultPlaybackSpeed)
    ON target.SkillId = source.SkillId
    WHEN MATCHED THEN UPDATE SET TotalDurationMs = source.TotalDurationMs, AnimationKey = source.AnimationKey, UpdatedOn = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN INSERT (SkillId, AnimationKey, TotalDurationMs, DefaultPlaybackSpeed)
        VALUES (source.SkillId, source.AnimationKey, source.TotalDurationMs, source.DefaultPlaybackSpeed);

    DECLARE @BasicAnimConfigId INT = (SELECT Id FROM dbo.HRK_SkillAnimationConfigs WHERE SkillId = 'THANH_THAI_DISRESPECTFUL_SWEEP');
    DELETE FROM dbo.HRK_SkillTimelinePhases WHERE SkillAnimationConfigId = @BasicAnimConfigId;

    INSERT INTO dbo.HRK_SkillTimelinePhases (SkillAnimationConfigId, PhaseCode, StartAtMs, DurationMs, TriggerEventType, DisplayOrder)
    VALUES
        (@BasicAnimConfigId, 'PREPARE', 0, 400, 'SKILL_CAST', 1),
        (@BasicAnimConfigId, 'SWEEP', 400, 400, NULL, 2),
        (@BasicAnimConfigId, 'IMPACT', 800, 400, 'DAMAGE', 3),
        (@BasicAnimConfigId, 'RESOURCE_GAIN', 1200, 300, 'AURA_GAINED', 4),
        (@BasicAnimConfigId, 'RETURN', 1500, 500, 'SKILL_COMPLETED', 5);

    -- 7.2 Ultimate lightning broom animation config
    MERGE dbo.HRK_SkillAnimationConfigs AS target
    USING (VALUES ('THANH_THAI_CRIMSON_BROOM_LIGHTNING', 'thanh-thai-lightning', 3200, CAST(1.0 AS DECIMAL(5,2))))
        AS source (SkillId, AnimationKey, TotalDurationMs, DefaultPlaybackSpeed)
    ON target.SkillId = source.SkillId
    WHEN MATCHED THEN UPDATE SET TotalDurationMs = source.TotalDurationMs, AnimationKey = source.AnimationKey, UpdatedOn = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN INSERT (SkillId, AnimationKey, TotalDurationMs, DefaultPlaybackSpeed)
        VALUES (source.SkillId, source.AnimationKey, source.TotalDurationMs, source.DefaultPlaybackSpeed);

    DECLARE @UltAnimConfigId INT = (SELECT Id FROM dbo.HRK_SkillAnimationConfigs WHERE SkillId = 'THANH_THAI_CRIMSON_BROOM_LIGHTNING');
    DELETE FROM dbo.HRK_SkillTimelinePhases WHERE SkillAnimationConfigId = @UltAnimConfigId;

    INSERT INTO dbo.HRK_SkillTimelinePhases (SkillAnimationConfigId, PhaseCode, StartAtMs, DurationMs, TriggerEventType, DisplayOrder)
    VALUES
        (@UltAnimConfigId, 'AURA_CHECK', 0, 300, 'SKILL_CAST', 1),
        (@UltAnimConfigId, 'PREPARE', 300, 300, NULL, 2),
        (@UltAnimConfigId, 'BROOM_THROW', 600, 300, NULL, 3),
        (@UltAnimConfigId, 'PRIMARY_IMPACT', 900, 350, 'DAMAGE', 4),
        (@UltAnimConfigId, 'CHAIN_1', 1250, 350, NULL, 5),
        (@UltAnimConfigId, 'CHAIN_2', 1600, 350, NULL, 6),
        (@UltAnimConfigId, 'CHAIN_3', 1950, 350, NULL, 7),
        (@UltAnimConfigId, 'DETONATE', 2300, 400, 'LOSS_OF_CONFIDENCE_DETONATED', 8),
        (@UltAnimConfigId, 'AURA_CONSUME', 2700, 200, 'AURA_CONSUMED', 9),
        (@UltAnimConfigId, 'BROOM_RETURN', 2900, 300, 'SKILL_COMPLETED', 10);

    PRINT N'-> Đã cấu hình xong animation timeline phases cho kỹ năng của Thanh Thái Aura.';

    ---------------------------------------------------------------------------
    -- 8. Seed Hero Star Aura Configurations (2, 3, 4, 5 stars)
    ---------------------------------------------------------------------------
    MERGE dbo.HRK_HeroStarAuraConfigs AS target
    USING (VALUES
        (@HeroTemplateId, CAST(2 AS TINYINT), N'THANH_THAI_STAR_2', N'thanh-thai-aura', N'Khói Ám Sơ Khởi', N'Vòng khí đỏ mờ dưới chân, khói bóng tối nhẹ.', '#991b1b', '#0f172a', CAST(0.90 AS DECIMAL(5,2)), CAST(1 AS TINYINT), 1),
        (@HeroTemplateId, CAST(3 AS TINYINT), N'THANH_THAI_STAR_3', N'thanh-thai-aura', N'Huyết Lôi Kích Tỏa', N'Sóng năng lượng crimson và các tia sét đỏ thẫm xuất hiện quanh chân.', '#b91c1c', '#450a0a', CAST(1.35 AS DECIMAL(5,2)), CAST(2 AS TINYINT), 1),
        (@HeroTemplateId, CAST(4 AS TINYINT), N'THANH_THAI_STAR_4', N'thanh-thai-aura', N'Bá Khí Trào Dâng', N'Bóng khí tối phía sau nhân vật mở rộng, lightning chạy dọc cán chổi.', '#dc2626', '#18181b', CAST(1.80 AS DECIMAL(5,2)), CAST(3 AS TINYINT), 1),
        (@HeroTemplateId, CAST(5 AS TINYINT), N'THANH_THAI_STAR_5', N'thanh-thai-aura', N'Tuyệt Đối Bá Khí', N'Full Aura Farming hoàn chỉnh, crimson pulse bao trùm, sấm sét đỏ rực rỡ và các mảnh vỡ năng lượng.', '#ef4444', '#7f1d1d', CAST(2.30 AS DECIMAL(5,2)), CAST(4 AS TINYINT), 1)
    ) AS source (HeroTemplateId, StarLevel, AuraCode, VisualKey, Name, Description, PrimaryColorHex, SecondaryColorHex, Intensity, ParticleLevel, IsActive)
    ON target.HeroTemplateId = source.HeroTemplateId AND target.StarLevel = source.StarLevel
    WHEN MATCHED THEN
        UPDATE SET
            AuraCode = source.AuraCode,
            VisualKey = source.VisualKey,
            Name = source.Name,
            Description = source.Description,
            PrimaryColorHex = source.PrimaryColorHex,
            SecondaryColorHex = source.SecondaryColorHex,
            Intensity = source.Intensity,
            ParticleLevel = source.ParticleLevel,
            IsActive = source.IsActive,
            UpdatedOn = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (HeroTemplateId, StarLevel, AuraCode, VisualKey, Name, Description, PrimaryColorHex, SecondaryColorHex, Intensity, ParticleLevel, IsActive, CreatedOn, UpdatedOn)
        VALUES (source.HeroTemplateId, source.StarLevel, source.AuraCode, source.VisualKey, source.Name, source.Description, source.PrimaryColorHex, source.SecondaryColorHex, source.Intensity, source.ParticleLevel, source.IsActive, SYSUTCDATETIME(), SYSUTCDATETIME());

    PRINT N'-> Đã cấu hình xong Star Aura cho Thanh Thái Aura.';

    ---------------------------------------------------------------------------
    -- 9. Grant Hero to PlayerId = 1 (5 Stars, Level 1)
    ---------------------------------------------------------------------------
    DECLARE @TargetPlayerId BIGINT = 1;
    IF EXISTS (SELECT 1 FROM dbo.HRK_Players WHERE Id = @TargetPlayerId AND IsActive = 1)
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.HRK_PlayerHeroes WHERE PlayerId = @TargetPlayerId AND HeroTemplateId = @HeroTemplateId AND IsActive = 1)
        BEGIN
            INSERT INTO dbo.HRK_PlayerHeroes
            (
                PlayerId, HeroTemplateId, Level, Exp, MaxExp, Stars, Power, AuraTier,
                IsLocked, IsFavorite, IsActive, CurrentStats, CreatedOn, UpdatedOn
            )
            VALUES
            (
                @TargetPlayerId,
                @HeroTemplateId,
                1,
                0,
                500,
                5,    -- 5 Stars
                (750 / 10) + (180 * 3) + (175 * 3) + (52 * 2) + 115, -- Calculated initial power
                4,    -- AuraTier for 5 stars
                0,
                1,    -- Favorite
                1,
                NULL,
                SYSUTCDATETIME(),
                SYSUTCDATETIME()
            );
            PRINT N'-> Đã cấp Thanh Thái Aura cho PlayerId = 1.';
        END
        ELSE
        BEGIN
            UPDATE dbo.HRK_PlayerHeroes
            SET Stars = 5,
                AuraTier = 4,
                Power = (750 / 10) + (180 * 3) + (175 * 3) + (52 * 2) + 115,
                UpdatedOn = SYSUTCDATETIME()
            WHERE PlayerId = @TargetPlayerId AND HeroTemplateId = @HeroTemplateId;
            PRINT N'-> Đã cập nhật Thanh Thái Aura cho PlayerId = 1.';
        END;
    END;

    COMMIT TRANSACTION;
    PRINT N'=== THÀNH CÔNG: Đã triển khai hoàn chỉnh nhân vật Thanh Thái Aura vào Database! ===';

    ---------------------------------------------------------------------------
    -- 10. Summary verification
    ---------------------------------------------------------------------------
    SELECT
        ht.Id AS HeroTemplateId,
        ht.Name AS HeroName,
        r.Name AS RarityName,
        ht.Avatar,
        ht.BaseAtk,
        ht.BaseMagicDamage,
        ht.BaseHp,
        ht.BaseDef,
        ht.BaseMagicResistance,
        ht.BaseSpd,
        ht.BaseCrit
    FROM dbo.HRK_HeroTemplates ht
    INNER JOIN dbo.HRK_Rarities r ON r.Id = ht.RarityId
    WHERE ht.Id = @HeroTemplateId;

    SELECT
        hs.HeroTemplateId,
        hs.SkillOrder,
        st.Id AS SkillId,
        st.Name AS SkillName,
        st.SkillTypeCode,
        st.EnergyCost
    FROM dbo.HRK_HeroSkills hs
    INNER JOIN dbo.HRK_SkillTemplates st ON st.Id = hs.SkillId
    WHERE hs.HeroTemplateId = @HeroTemplateId
    ORDER BY hs.SkillOrder;

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

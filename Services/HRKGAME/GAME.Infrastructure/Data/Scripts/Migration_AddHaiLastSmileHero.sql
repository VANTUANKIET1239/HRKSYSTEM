/*
    Migration: Add New Mythic Assassin Hero "Tao là nhất" (Hải “Nụ Cười Cuối”)
    - Hero Code / Alias: HAI_LAST_SMILE
    - Display Name: N'Tao là nhất'
    - Rarity: Mythic (Highest tier)
    - Class: Assassin (Physical)
    - Skills: HAI_BUG_SLASH (Dao Rạch Bug), HAI_LAST_LAUGH (Cười Đi, Sắp Hết Lượt Rồi)
    - Status Effects: BLEED, PANIC, SHIELD_BLOCK
    - Target Type: LOWEST_HP_PERCENT
    - Star Aura: Star tiers 2-5
    - Player Grant: Automatically added to PlayerId = 1
    Idempotent & Transaction-safe.
*/

USE [HRK];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

---------------------------------------------------------------------------
-- 0. Schema Extensions: Add ExecutionGroup & ConditionCode to HRK_SkillEffects
---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HRK_SkillEffects') AND name = 'ExecutionGroup')
BEGIN
    ALTER TABLE dbo.HRK_SkillEffects ADD ExecutionGroup NVARCHAR(50) NULL;
    PRINT N'-> Đã thêm cột ExecutionGroup vào dbo.HRK_SkillEffects.';
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HRK_SkillEffects') AND name = 'ConditionCode')
BEGIN
    ALTER TABLE dbo.HRK_SkillEffects ADD ConditionCode NVARCHAR(50) NULL;
    PRINT N'-> Đã thêm cột ConditionCode vào dbo.HRK_SkillEffects.';
END;
GO

---------------------------------------------------------------------------
-- 0.1 Schema Extensions: Create HRK_SkillEffectParameters table
---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HRK_SkillEffectParameters' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.HRK_SkillEffectParameters
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HRK_SkillEffectParameters PRIMARY KEY,
        SkillEffectId BIGINT NOT NULL,
        ParameterCode NVARCHAR(50) NOT NULL,
        DecimalValue DECIMAL(18,4) NULL,
        IntValue INT NULL,
        BoolValue BIT NULL,
        StringValue NVARCHAR(255) NULL,
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_SkillEffectParameters_CreatedOn DEFAULT SYSUTCDATETIME(),
        UpdatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_SkillEffectParameters_UpdatedOn DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_HRK_SkillEffectParameters_SkillEffect FOREIGN KEY (SkillEffectId)
            REFERENCES dbo.HRK_SkillEffects(Id) ON DELETE CASCADE,
        CONSTRAINT UQ_HRK_SkillEffectParameters_Effect_Code UNIQUE (SkillEffectId, ParameterCode)
    );
    CREATE INDEX IX_HRK_SkillEffectParameters_SkillEffectId ON dbo.HRK_SkillEffectParameters(SkillEffectId);
    PRINT N'-> Đã tạo bảng dbo.HRK_SkillEffectParameters.';
END;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    ---------------------------------------------------------------------------
    -- 1. Validate mandatory reference data
    ---------------------------------------------------------------------------
    DECLARE @RarityId INT = (SELECT TOP 1 Id FROM dbo.HRK_Rarities WHERE Code = 'Mythic');
    IF @RarityId IS NULL
        SET @RarityId = (SELECT TOP 1 Id FROM dbo.HRK_Rarities ORDER BY Id DESC);

    IF @RarityId IS NULL
        THROW 50010, N'Lỗi: Bảng dbo.HRK_Rarities không có dữ liệu phẩm chất.', 1;

    DECLARE @ClassId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroClasses WHERE Code = 'Assassin');
    IF @ClassId IS NULL
        THROW 50011, N'Lỗi: Không tìm thấy hệ phái Assassin trong dbo.HRK_HeroClasses.', 1;

    DECLARE @FactionId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroFactions WHERE Code = 'Quan');
    IF @FactionId IS NULL
        SET @FactionId = (SELECT TOP 1 Id FROM dbo.HRK_HeroFactions ORDER BY Id ASC);

    DECLARE @AtkAttrId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE Code = 'ATK');
    DECLARE @DefAttrId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE Code = 'DEF');

    ---------------------------------------------------------------------------
    -- 2. Ensure Skill Effect Types exist (BLEED, PANIC, SHIELD_BLOCK, BLEED_DETONATE, ENERGY_CHANGE)
    ---------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE Code = 'BLEED')
    BEGIN
        INSERT INTO dbo.HRK_SkillEffectTypes
        (
            Code, Name, IsDebuff, EffectGroup, IsBeneficial, IsStackable,
            DefaultStackLimit, Description, ImagePath, ColorHex, CreatedOn, UpdatedOn
        )
        VALUES
        (
            'BLEED', N'Chảy Máu', 1, 'DOT', 0, 0,
            1, N'Mỗi đầu lượt nhận sát thương Chảy Máu bỏ qua một phần phòng thủ, tối thiểu còn 1 HP.',
            '/assets/images/dcs-game/effects/bleed.png', '#dc2626', GETDATE(), SYSUTCDATETIME()
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE Code = 'PANIC')
    BEGIN
        INSERT INTO dbo.HRK_SkillEffectTypes
        (
            Code, Name, IsDebuff, EffectGroup, IsBeneficial, IsStackable,
            DefaultStackLimit, Description, ImagePath, ColorHex, CreatedOn, UpdatedOn
        )
        VALUES
        (
            'PANIC', N'Hoảng Loạn', 1, 'STAT', 0, 0,
            1, N'Giảm phòng thủ và không thể nhận Khiên mới trong thời gian hiệu lực.',
            '/assets/images/dcs-game/effects/panic.png', '#f97316', GETDATE(), SYSUTCDATETIME()
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE Code = 'SHIELD_BLOCK')
    BEGIN
        INSERT INTO dbo.HRK_SkillEffectTypes
        (
            Code, Name, IsDebuff, EffectGroup, IsBeneficial, IsStackable,
            DefaultStackLimit, Description, ImagePath, ColorHex, CreatedOn, UpdatedOn
        )
        VALUES
        (
            'SHIELD_BLOCK', N'Cấm Nhận Khiên', 1, 'CONTROL', 0, 0,
            1, N'Không thể nhận Khiên mới trong thời gian hiệu lực.',
            '/assets/images/dcs-game/effects/shield-block.png', '#ef4444', GETDATE(), SYSUTCDATETIME()
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE Code = 'BLEED_DETONATE')
    BEGIN
        INSERT INTO dbo.HRK_SkillEffectTypes
        (
            Code, Name, IsDebuff, EffectGroup, IsBeneficial, IsStackable,
            DefaultStackLimit, Description, ImagePath, ColorHex, CreatedOn, UpdatedOn
        )
        VALUES
        (
            'BLEED_DETONATE', N'Kích Nổ Chảy Máu', 0, 'SPECIAL', 0, 0,
            1, N'Kích nổ tổng sát thương Chảy Máu còn lại trên mục tiêu.',
            '/assets/images/dcs-game/effects/bleed-detonate.png', '#b91c1c', GETDATE(), SYSUTCDATETIME()
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE Code = 'ENERGY_CHANGE')
    BEGIN
        INSERT INTO dbo.HRK_SkillEffectTypes
        (
            Code, Name, IsDebuff, EffectGroup, IsBeneficial, IsStackable,
            DefaultStackLimit, Description, ImagePath, ColorHex, CreatedOn, UpdatedOn
        )
        VALUES
        (
            'ENERGY_CHANGE', N'Thay Đổi Năng Lượng', 0, 'SPECIAL', 1, 0,
            1, N'Hồi phục hoặc tiêu hao năng lượng của mục tiêu.',
            '/assets/images/dcs-game/effects/energy.png', '#eab308', GETDATE(), SYSUTCDATETIME()
        );
    END;

    DECLARE @DamageEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE Code = 'DAMAGE');
    DECLARE @BleedEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE Code = 'BLEED');
    DECLARE @PanicEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE Code = 'PANIC');
    DECLARE @BleedDetonateEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE Code = 'BLEED_DETONATE');
    DECLARE @EnergyChangeEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE Code = 'ENERGY_CHANGE');
    DECLARE @DamageReductionEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE Code = 'DAMAGE_REDUCTION');
    DECLARE @StatDebuffEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE Code = 'STAT_DEBUFF');

    ---------------------------------------------------------------------------
    -- 3. Ensure Target Type exists (LOWEST_HP_PERCENT, SELF)
    ---------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillTargetTypes WHERE Code = 'LOWEST_HP_PERCENT')
    BEGIN
        INSERT INTO dbo.HRK_SkillTargetTypes
        (
            Code, Name, TargetSide, SelectionRule, DisplayOrder, IsActive, CreatedOn, UpdatedOn
        )
        VALUES
        (
            'LOWEST_HP_PERCENT', N'Kẻ địch % HP thấp nhất', 'ENEMY', 'LOWEST_HP_PERCENT',
            25, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
        );
    END;

    DECLARE @LowestHpTargetTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE Code = 'LOWEST_HP_PERCENT');
    DECLARE @SelfTargetTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE Code = 'SELF');
    IF @SelfTargetTypeId IS NULL
        SET @SelfTargetTypeId = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE SelectionRule = 'SELF');

    ---------------------------------------------------------------------------
    -- 4. Create or Update Hero Template "Tao là nhất"
    ---------------------------------------------------------------------------
    DECLARE @HeroTemplateId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE Name = N'Tao là nhất' OR Avatar LIKE '%nhan-cuoi-xam-lon%' OR Avatar LIKE '%tao-la-nhat%');

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
            N'Tao là nhất',
            '/assets/images/dcs-game/heroes/tao-la-nhat-transparent.png',
            @FactionId,
            @ClassId,
            @RarityId,
            680,     -- BaseHp (~75% baseline assassin)
            198,     -- BaseAtk (~141% baseline assassin)
            56,      -- BaseDef (~80% baseline assassin)
            152,     -- BaseSpd (~132% baseline assassin)
            25.00,   -- BaseCrit (Cao)
            175.00,  -- BaseCritDmg (Khá cao)
            0.00,    -- BaseLifesteal
            92.00,   -- BaseAccuracy (Khá cao)
            5.00,    -- BaseResistance (Thấp)
            40,      -- BaseMagicDamage
            40,      -- BaseMagicResistance
            SYSUTCDATETIME()
        );
        SET @HeroTemplateId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_HeroTemplates
        SET
            Name = N'Tao là nhất',
            Avatar = '/assets/images/dcs-game/heroes/tao-la-nhat-transparent.png',
            FactionId = @FactionId,
            ClassId = @ClassId,
            RarityId = @RarityId,
            BaseHp = 680,
            BaseAtk = 198,
            BaseDef = 56,
            BaseSpd = 152,
            BaseCrit = 25.00,
            BaseCritDmg = 175.00,
            BaseLifesteal = 0.00,
            BaseAccuracy = 92.00,
            BaseResistance = 5.00,
            BaseMagicDamage = 40,
            BaseMagicResistance = 40
        WHERE Id = @HeroTemplateId;
    END;

    ---------------------------------------------------------------------------
    -- 5. Create or Update Skill Templates: HAI_BUG_SLASH & HAI_LAST_LAUGH
    ---------------------------------------------------------------------------
    -- 5.1 Basic Skill: HAI_BUG_SLASH (Dao Rạch Bug)
    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'HAI_BUG_SLASH')
    BEGIN
        INSERT INTO dbo.HRK_SkillTemplates
        (
            Id, Name, Icon, Description, ImagePath, SkillTypeCode,
            TriggerCode, EnergyCost, DisplayOrder, IsActive, CreatedOn, UpdatedOn
        )
        VALUES
        (
            'HAI_BUG_SLASH',
            N'Dao Rạch Bug',
            '/assets/images/dcs-game/skills/hai-bug-slash.png',
            N'Lướt tới chém chéo mục tiêu có % HP thấp nhất, gây sát thương vật lý theo ATK. Nếu gây sát thương thành công, có xác suất gây Chảy Máu (BLEED). Mọi thông số được quản lý qua cấu hình dữ liệu.',
            '/assets/images/dcs-game/skills/hai-bug-slash.png',
            'NORMAL',
            'ACTIVE',
            0,
            1,
            1,
            GETDATE(),
            SYSUTCDATETIME()
        );
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_SkillTemplates
        SET
            Name = N'Dao Rạch Bug',
            Icon = '/assets/images/dcs-game/skills/hai-bug-slash.png',
            Description = N'Lướt tới chém chéo mục tiêu có % HP thấp nhất, gây sát thương vật lý theo ATK. Nếu gây sát thương thành công, có xác suất gây Chảy Máu (BLEED). Mọi thông số được quản lý qua cấu hình dữ liệu.',
            ImagePath = '/assets/images/dcs-game/skills/hai-bug-slash.png',
            SkillTypeCode = 'NORMAL',
            EnergyCost = 0,
            IsActive = 1,
            UpdatedOn = SYSUTCDATETIME()
        WHERE Id = 'HAI_BUG_SLASH';
    END;

    -- 5.2 Ultimate Skill: HAI_LAST_LAUGH (Cười Đi, Sắp Hết Lượt Rồi)
    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'HAI_LAST_LAUGH')
    BEGIN
        INSERT INTO dbo.HRK_SkillTemplates
        (
            Id, Name, Icon, Description, ImagePath, SkillTypeCode,
            TriggerCode, EnergyCost, DisplayOrder, IsActive, CreatedOn, UpdatedOn
        )
        VALUES
        (
            'HAI_LAST_LAUGH',
            N'Cười Đi, Sắp Hết Lượt Rồi',
            '/assets/images/dcs-game/skills/hai-last-laugh.png',
            N'Khóa mục tiêu có % HP thấp nhất, gây Hoảng Loạn (giảm DEF, cấm nhận khiên). Nếu mục tiêu có Chảy Máu trước đó, kích nổ ngay lập tức gây sát thương Chảy Máu còn lại (bỏ qua một phần DEF). Sau đó tung 3 nhát đâm chớp nhoáng (HIT_1, HIT_2, HIT_3), mỗi nhát có xác suất gây Chảy Máu độc lập. Hạ gục mục tiêu hồi Năng Lượng và nhận buff giảm sát thương đòn đánh kế tiếp; nếu không hạ gục được bị giảm DEF.',
            '/assets/images/dcs-game/skills/hai-last-laugh.png',
            'ENERGY',
            'ACTIVE',
            100,
            2,
            1,
            GETDATE(),
            SYSUTCDATETIME()
        );
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_SkillTemplates
        SET
            Name = N'Cười Đi, Sắp Hết Lượt Rồi',
            Icon = '/assets/images/dcs-game/skills/hai-last-laugh.png',
            Description = N'Khóa mục tiêu có % HP thấp nhất, gây Hoảng Loạn (giảm DEF, cấm nhận khiên). Nếu mục tiêu có Chảy Máu trước đó, kích nổ ngay lập tức gây sát thương Chảy Máu còn lại (bỏ qua một phần DEF). Sau đó tung 3 nhát đâm chớp nhoáng (HIT_1, HIT_2, HIT_3), mỗi nhát có xác suất gây Chảy Máu độc lập. Hạ gục mục tiêu hồi Năng Lượng và nhận buff giảm sát thương đòn đánh kế tiếp; nếu không hạ gục được bị giảm DEF.',
            ImagePath = '/assets/images/dcs-game/skills/hai-last-laugh.png',
            SkillTypeCode = 'ENERGY',
            EnergyCost = 100,
            IsActive = 1,
            UpdatedOn = SYSUTCDATETIME()
        WHERE Id = 'HAI_LAST_LAUGH';
    END;

    ---------------------------------------------------------------------------
    -- 6. Link Skills to Hero in HRK_HeroSkills
    ---------------------------------------------------------------------------
    DELETE FROM dbo.HRK_HeroSkills WHERE HeroTemplateId = @HeroTemplateId;

    INSERT INTO dbo.HRK_HeroSkills (HeroTemplateId, SkillId, SkillOrder)
    VALUES
        (@HeroTemplateId, 'HAI_BUG_SLASH', 1),
        (@HeroTemplateId, 'HAI_LAST_LAUGH', 2);

    ---------------------------------------------------------------------------
    -- 7. Configure Data-Driven Skill Effects & Parameters
    ---------------------------------------------------------------------------
    -- Clean previous parameters, scalings, stat modifiers, and effects for these skills
    DELETE p
    FROM dbo.HRK_SkillEffectParameters p
    INNER JOIN dbo.HRK_SkillEffects se ON se.Id = p.SkillEffectId
    WHERE se.SkillId IN ('HAI_BUG_SLASH', 'HAI_LAST_LAUGH');

    DELETE sc
    FROM dbo.HRK_SkillEffectScalings sc
    INNER JOIN dbo.HRK_SkillEffects se ON se.Id = sc.SkillEffectId
    WHERE se.SkillId IN ('HAI_BUG_SLASH', 'HAI_LAST_LAUGH');

    DELETE sm
    FROM dbo.HRK_SkillEffectStatModifiers sm
    INNER JOIN dbo.HRK_SkillEffects se ON se.Id = sm.SkillEffectId
    WHERE se.SkillId IN ('HAI_BUG_SLASH', 'HAI_LAST_LAUGH');

    DELETE se
    FROM dbo.HRK_SkillEffects se
    WHERE se.SkillId IN ('HAI_BUG_SLASH', 'HAI_LAST_LAUGH');

    ---------------------------------------------------------------------------
    -- 7.1 Effects for HAI_BUG_SLASH:
    ---------------------------------------------------------------------------
    -- 1) DAMAGE (1.00 ATK, PHYSICAL, LOWEST_HP_PERCENT)
    INSERT INTO dbo.HRK_SkillEffects
    (
        SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode,
        BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder,
        ExecutionGroup, ConditionCode, IsActive, CreatedOn, UpdatedOn
    )
    VALUES
    (
        'HAI_BUG_SLASH', @DamageEffectTypeId, @LowestHpTargetTypeId, 'PHYSICAL',
        0, NULL, 100.00, 1, 1,
        NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @BugSlashDmgEffectId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@BugSlashDmgEffectId, @AtkAttrId, 1.000000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@BugSlashDmgEffectId, 'CAN_CRIT', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- 2) BLEED (30% chance, 2 turns, 0.18 ATK scaling, 30% Armor Ignore, CanCrit=false, CanKill=false, RefreshOnReapply=true)
    INSERT INTO dbo.HRK_SkillEffects
    (
        SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode,
        BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder,
        ExecutionGroup, ConditionCode, IsActive, CreatedOn, UpdatedOn
    )
    VALUES
    (
        'HAI_BUG_SLASH', @BleedEffectTypeId, @LowestHpTargetTypeId, 'PHYSICAL',
        0, 2, 30.00, 1, 2,
        NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @BugSlashBleedEffectId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@BugSlashBleedEffectId, @AtkAttrId, 0.180000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, DecimalValue, BoolValue, CreatedOn, UpdatedOn)
    VALUES
        (@BugSlashBleedEffectId, 'ARMOR_IGNORE_PERCENT', 30.0000, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@BugSlashBleedEffectId, 'CAN_CRIT', NULL, 0, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@BugSlashBleedEffectId, 'CAN_KILL', NULL, 0, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@BugSlashBleedEffectId, 'REFRESH_ON_REAPPLY', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    ---------------------------------------------------------------------------
    -- 7.2 Effects for HAI_LAST_LAUGH:
    ---------------------------------------------------------------------------
    -- 1) PANIC (DisplayOrder 1): -20% DEF, 1 turn, 100% chance, ShieldBlock=true
    INSERT INTO dbo.HRK_SkillEffects
    (
        SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode,
        BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder,
        ExecutionGroup, ConditionCode, IsActive, CreatedOn, UpdatedOn
    )
    VALUES
    (
        'HAI_LAST_LAUGH', @PanicEffectTypeId, @LowestHpTargetTypeId, NULL,
        0, 1, 100.00, 1, 1,
        NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @LastLaughPanicEffectId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.HRK_SkillEffectStatModifiers (SkillEffectId, AttributeTypeId, ValueType, Value, CreatedOn, UpdatedOn)
    VALUES (@LastLaughPanicEffectId, @DefAttrId, 'PERCENT', -20.00, SYSUTCDATETIME(), SYSUTCDATETIME());

    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@LastLaughPanicEffectId, 'SHIELD_BLOCK', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- 2) BLEED_DETONATE (DisplayOrder 2): Detonate preexisting Bleed at 130%, 30% Armor Ignore, remove status
    INSERT INTO dbo.HRK_SkillEffects
    (
        SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode,
        BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder,
        ExecutionGroup, ConditionCode, IsActive, CreatedOn, UpdatedOn
    )
    VALUES
    (
        'HAI_LAST_LAUGH', @BleedDetonateEffectTypeId, @LowestHpTargetTypeId, 'PHYSICAL',
        0, NULL, 100.00, 1, 2,
        NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @LastLaughDetonateEffectId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, DecimalValue, BoolValue, StringValue, CreatedOn, UpdatedOn)
    VALUES
        (@LastLaughDetonateEffectId, 'DETONATION_MULTIPLIER', 1.3000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@LastLaughDetonateEffectId, 'ARMOR_IGNORE_PERCENT', 30.0000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@LastLaughDetonateEffectId, 'CAN_CRIT', NULL, 0, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@LastLaughDetonateEffectId, 'REMOVE_STATUS_AFTER_EXECUTION', NULL, 1, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@LastLaughDetonateEffectId, 'ONLY_PREEXISTING_STATUS', NULL, 1, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@LastLaughDetonateEffectId, 'REQUIRED_STATUS_CODE', NULL, NULL, 'BLEED', SYSUTCDATETIME(), SYSUTCDATETIME());

    -- 3) HIT_1 DAMAGE (DisplayOrder 3, ExecutionGroup 'HIT_1'): 0.80 ATK
    INSERT INTO dbo.HRK_SkillEffects
    (
        SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode,
        BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder,
        ExecutionGroup, ConditionCode, IsActive, CreatedOn, UpdatedOn
    )
    VALUES
    (
        'HAI_LAST_LAUGH', @DamageEffectTypeId, @LowestHpTargetTypeId, 'PHYSICAL',
        0, NULL, 100.00, 1, 3,
        'HIT_1', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @Hit1DmgEffectId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@Hit1DmgEffectId, @AtkAttrId, 0.800000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@Hit1DmgEffectId, 'CAN_CRIT', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- 4) HIT_1 BLEED (DisplayOrder 4, ExecutionGroup 'HIT_1'): 30% chance, 2 turns, 0.18 ATK
    INSERT INTO dbo.HRK_SkillEffects
    (
        SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode,
        BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder,
        ExecutionGroup, ConditionCode, IsActive, CreatedOn, UpdatedOn
    )
    VALUES
    (
        'HAI_LAST_LAUGH', @BleedEffectTypeId, @LowestHpTargetTypeId, 'PHYSICAL',
        0, 2, 30.00, 1, 4,
        'HIT_1', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @Hit1BleedEffectId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@Hit1BleedEffectId, @AtkAttrId, 0.180000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, DecimalValue, BoolValue, CreatedOn, UpdatedOn)
    VALUES
        (@Hit1BleedEffectId, 'ARMOR_IGNORE_PERCENT', 30.0000, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@Hit1BleedEffectId, 'CAN_CRIT', NULL, 0, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@Hit1BleedEffectId, 'CAN_KILL', NULL, 0, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@Hit1BleedEffectId, 'REFRESH_ON_REAPPLY', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- 5) HIT_2 DAMAGE (DisplayOrder 5, ExecutionGroup 'HIT_2'): 0.90 ATK
    INSERT INTO dbo.HRK_SkillEffects
    (
        SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode,
        BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder,
        ExecutionGroup, ConditionCode, IsActive, CreatedOn, UpdatedOn
    )
    VALUES
    (
        'HAI_LAST_LAUGH', @DamageEffectTypeId, @LowestHpTargetTypeId, 'PHYSICAL',
        0, NULL, 100.00, 1, 5,
        'HIT_2', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @Hit2DmgEffectId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@Hit2DmgEffectId, @AtkAttrId, 0.900000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@Hit2DmgEffectId, 'CAN_CRIT', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- 6) HIT_2 BLEED (DisplayOrder 6, ExecutionGroup 'HIT_2'): 30% chance, 2 turns, 0.18 ATK
    INSERT INTO dbo.HRK_SkillEffects
    (
        SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode,
        BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder,
        ExecutionGroup, ConditionCode, IsActive, CreatedOn, UpdatedOn
    )
    VALUES
    (
        'HAI_LAST_LAUGH', @BleedEffectTypeId, @LowestHpTargetTypeId, 'PHYSICAL',
        0, 2, 30.00, 1, 6,
        'HIT_2', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @Hit2BleedEffectId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@Hit2BleedEffectId, @AtkAttrId, 0.180000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, DecimalValue, BoolValue, CreatedOn, UpdatedOn)
    VALUES
        (@Hit2BleedEffectId, 'ARMOR_IGNORE_PERCENT', 30.0000, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@Hit2BleedEffectId, 'CAN_CRIT', NULL, 0, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@Hit2BleedEffectId, 'CAN_KILL', NULL, 0, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@Hit2BleedEffectId, 'REFRESH_ON_REAPPLY', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- 7) HIT_3 DAMAGE (DisplayOrder 7, ExecutionGroup 'HIT_3'): 1.30 ATK
    INSERT INTO dbo.HRK_SkillEffects
    (
        SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode,
        BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder,
        ExecutionGroup, ConditionCode, IsActive, CreatedOn, UpdatedOn
    )
    VALUES
    (
        'HAI_LAST_LAUGH', @DamageEffectTypeId, @LowestHpTargetTypeId, 'PHYSICAL',
        0, NULL, 100.00, 1, 7,
        'HIT_3', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @Hit3DmgEffectId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@Hit3DmgEffectId, @AtkAttrId, 1.300000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@Hit3DmgEffectId, 'CAN_CRIT', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- 8) HIT_3 BLEED (DisplayOrder 8, ExecutionGroup 'HIT_3'): 30% chance, 2 turns, 0.18 ATK
    INSERT INTO dbo.HRK_SkillEffects
    (
        SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode,
        BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder,
        ExecutionGroup, ConditionCode, IsActive, CreatedOn, UpdatedOn
    )
    VALUES
    (
        'HAI_LAST_LAUGH', @BleedEffectTypeId, @LowestHpTargetTypeId, 'PHYSICAL',
        0, 2, 30.00, 1, 8,
        'HIT_3', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @Hit3BleedEffectId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@Hit3BleedEffectId, @AtkAttrId, 0.180000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, DecimalValue, BoolValue, CreatedOn, UpdatedOn)
    VALUES
        (@Hit3BleedEffectId, 'ARMOR_IGNORE_PERCENT', 30.0000, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@Hit3BleedEffectId, 'CAN_CRIT', NULL, 0, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@Hit3BleedEffectId, 'CAN_KILL', NULL, 0, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@Hit3BleedEffectId, 'REFRESH_ON_REAPPLY', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- 9) TARGET_DEFEATED: ENERGY_CHANGE +25 Energy (DisplayOrder 9)
    INSERT INTO dbo.HRK_SkillEffects
    (
        SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode,
        BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder,
        ExecutionGroup, ConditionCode, IsActive, CreatedOn, UpdatedOn
    )
    VALUES
    (
        'HAI_LAST_LAUGH', @EnergyChangeEffectTypeId, @SelfTargetTypeId, NULL,
        25.00, NULL, 100.00, 1, 9,
        NULL, 'TARGET_DEFEATED', 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @KillEnergyEffectId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, IntValue, CreatedOn, UpdatedOn)
    VALUES (@KillEnergyEffectId, 'ENERGY_GAIN', 25, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- 10) TARGET_DEFEATED: DAMAGE_REDUCTION 40%, 1 turn, consume on hit (DisplayOrder 10)
    INSERT INTO dbo.HRK_SkillEffects
    (
        SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode,
        BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder,
        ExecutionGroup, ConditionCode, IsActive, CreatedOn, UpdatedOn
    )
    VALUES
    (
        'HAI_LAST_LAUGH', @DamageReductionEffectTypeId, @SelfTargetTypeId, NULL,
        40.00, 1, 100.00, 1, 10,
        NULL, 'TARGET_DEFEATED', 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @KillDrEffectId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.HRK_SkillEffectStatModifiers (SkillEffectId, AttributeTypeId, ValueType, Value, CreatedOn, UpdatedOn)
    VALUES (@KillDrEffectId, @DefAttrId, 'PERCENT', 40.00, SYSUTCDATETIME(), SYSUTCDATETIME());

    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@KillDrEffectId, 'CONSUME_ON_HIT', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- 11) TARGET_SURVIVED: STAT_DEBUFF -15% DEF on Actor, 1 turn (DisplayOrder 11)
    INSERT INTO dbo.HRK_SkillEffects
    (
        SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode,
        BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder,
        ExecutionGroup, ConditionCode, IsActive, CreatedOn, UpdatedOn
    )
    VALUES
    (
        'HAI_LAST_LAUGH', @StatDebuffEffectTypeId, @SelfTargetTypeId, NULL,
        -15.00, 1, 100.00, 1, 11,
        NULL, 'TARGET_SURVIVED', 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @FailDebuffEffectId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.HRK_SkillEffectStatModifiers (SkillEffectId, AttributeTypeId, ValueType, Value, CreatedOn, UpdatedOn)
    VALUES (@FailDebuffEffectId, @DefAttrId, 'PERCENT', -15.00, SYSUTCDATETIME(), SYSUTCDATETIME());

    ---------------------------------------------------------------------------
    -- 8. Seed Hero Star Aura Configurations (2, 3, 4, 5 stars)
    ---------------------------------------------------------------------------
    MERGE dbo.HRK_HeroStarAuraConfigs AS target
    USING (VALUES
        (@HeroTemplateId, CAST(2 AS TINYINT), N'HAI_LAST_SMILE_STAR_2', N'hai-last-smile-aura', N'Hắc Ảnh Khởi Động', N'Vòng sáng xanh đen mỏng dưới chân, ám khí sơ khởi.', '#0d9488', '#042f2e', CAST(0.85 AS DECIMAL(5,2)), CAST(1 AS TINYINT), 1),
        (@HeroTemplateId, CAST(3 AS TINYINT), N'HAI_LAST_SMILE_STAR_3', N'hai-last-smile-aura', N'Vi Mạch Ám Sát', N'Đường mạch vi tính công nghệ phát sáng xanh và các hạt phân tử nano bay lên.', '#0d9488', '#06b6d4', CAST(1.30 AS DECIMAL(5,2)), CAST(2 AS TINYINT), 1),
        (@HeroTemplateId, CAST(4 AS TINYINT), N'HAI_LAST_SMILE_STAR_4', N'hai-last-smile-aura', N'Hư Vô Đoạt Mệnh', N'Bóng dao găm xoay chậm xung quanh người kết hợp xung lực năng lượng dâng trào.', '#14b8a6', '#0891b2', CAST(1.75 AS DECIMAL(5,2)), CAST(3 AS TINYINT), 1),
        (@HeroTemplateId, CAST(5 AS TINYINT), N'HAI_LAST_SMILE_STAR_5', N'hai-last-smile-aura', N'Nụ Cười Tối Thượng', N'Vòng rune công nghệ ma trận hoàn chỉnh, bóng dao xoay tít, tia sáng xanh lam rực rỡ và hạt huyết sắc Chảy Máu.', '#2dd4bf', '#dc2626', CAST(2.20 AS DECIMAL(5,2)), CAST(4 AS TINYINT), 1)
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
                5,    -- 5 Stars to showcase supreme star aura immediately
                (680 / 10) + (198 * 3) + (56 * 2) + 152, -- Calculated initial power
                4,    -- AuraTier for 5 stars
                0,
                1,    -- Favorite
                1,
                NULL,
                SYSUTCDATETIME(),
                SYSUTCDATETIME()
            );
        END
        ELSE
        BEGIN
            -- Update to 5 stars if already exists
            UPDATE dbo.HRK_PlayerHeroes
            SET Stars = 5,
                AuraTier = 4,
                Power = (680 / 10) + (198 * 3) + (56 * 2) + 152,
                UpdatedOn = SYSUTCDATETIME()
            WHERE PlayerId = @TargetPlayerId AND HeroTemplateId = @HeroTemplateId;
        END;
    END;

    COMMIT TRANSACTION;

    ---------------------------------------------------------------------------
    -- 10. Output confirmation
    ---------------------------------------------------------------------------
    SELECT
        ht.Id AS HeroTemplateId,
        ht.Name AS HeroName,
        r.Name AS RarityName,
        c.Name AS ClassName,
        f.Name AS FactionName,
        ht.Avatar,
        ht.BaseAtk,
        ht.BaseHp,
        ht.BaseDef,
        ht.BaseSpd,
        ht.BaseCrit,
        ht.BaseCritDmg
    FROM dbo.HRK_HeroTemplates ht
    INNER JOIN dbo.HRK_Rarities r ON r.Id = ht.RarityId
    INNER JOIN dbo.HRK_HeroClasses c ON c.Id = ht.ClassId
    INNER JOIN dbo.HRK_HeroFactions f ON f.Id = ht.FactionId
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

    SELECT
        ph.Id AS PlayerHeroId,
        ph.PlayerId,
        ph.HeroTemplateId,
        ph.Level,
        ph.Stars,
        ph.Power,
        ph.AuraTier
    FROM dbo.HRK_PlayerHeroes ph
    WHERE ph.PlayerId = @TargetPlayerId AND ph.HeroTemplateId = @HeroTemplateId;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
GO

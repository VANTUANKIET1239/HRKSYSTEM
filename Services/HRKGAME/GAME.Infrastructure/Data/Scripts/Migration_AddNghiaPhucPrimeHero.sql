/*
    Migration: Add New Highest-Rarity Defensive Tank Hero "Nghĩa Phục Prime"
    - Hero Code / Alias: NGHIA_PHUC_PRIME
    - Display Name: N'Nghĩa Phục Prime'
    - Rarity: Highest tier currently in game (Mythic)
    - Avatar: '/assets/images/dcs-game/nghia-phuc-prime.png'
    - Role: Tanker / Phòng Thủ (Bảo vệ đồng đội, chịu sát thương thay, tạo khiên, tích áp lực phản công theo DEF)
    - Status Effects:
        * PRIME_FORTITUDE (Kiên Cố): +5% DEF, +5% Kháng Phép / tầng (tối đa 4 tầng)
        * PRIME_GUARDIAN (Hộ Vệ Prime): Chuyển hướng 35% sát thương trực tiếp từ đồng minh
        * PRIME_PRESSURE (Áp Lực): Tích lũy khi nhận damage thay đồng đội (tối đa 5 tầng)
        * PRIME_STAGGER (Lung Lay): -15 điểm action bar, -10% SPD trong 1 lượt
        * PRIME_BROKEN_MORALE (Vỡ Trận): -15% sát thương gây ra trong 2 lượt
    - Skills:
        * Basic: PRIME_SHIELD_WARRANTY (Khiên Này Có Bảo Hành)
        * Energy: PRIME_FORTRESS_CHARGE (Thành Trì Prime: Không Ai Được Phép Ngã)
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

    DECLARE @ClassId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroClasses WHERE UPPER(Code) IN ('TANK', 'WARRIOR', 'GUARDIAN'));
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

    DECLARE @EnemyAllTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) IN ('ENEMY_ALL', 'ALL_ENEMIES'));
    IF @EnemyAllTargetId IS NULL SET @EnemyAllTargetId = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) LIKE '%ALL%');

    ---------------------------------------------------------------------------
    -- 2. Ensure Skill Effect Types exist:
    --    - PRIME_FORTITUDE
    --    - PRIME_GUARDIAN
    --    - PRIME_PRESSURE
    --    - PRIME_STAGGER
    --    - PRIME_BROKEN_MORALE
    ---------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'PRIME_FORTITUDE')
    BEGIN
        INSERT INTO dbo.HRK_SkillEffectTypes
        (
            Code, Name, IsDebuff, EffectGroup, IsBeneficial, IsStackable,
            DefaultStackLimit, Description, ImagePath, ColorHex, CreatedOn, UpdatedOn
        )
        VALUES
        (
            'PRIME_FORTITUDE', N'Kiên Cố', 0, 'BUFF', 1, 1,
            4, N'Mỗi tầng tăng 5% DEF và 5% Kháng Phép (tối đa 4 tầng). Khi đạt 4 tầng sẽ tiêu thụ toàn bộ và tạo khiên bằng 12% Max HP.',
            '/assets/images/dcs-game/effects/prime-fortitude.png', '#3b82f6', GETDATE(), SYSUTCDATETIME()
        );
        PRINT N'-> Đã thêm Effect Type PRIME_FORTITUDE.';
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'PRIME_GUARDIAN')
    BEGIN
        INSERT INTO dbo.HRK_SkillEffectTypes
        (
            Code, Name, IsDebuff, EffectGroup, IsBeneficial, IsStackable,
            DefaultStackLimit, Description, ImagePath, ColorHex, CreatedOn, UpdatedOn
        )
        VALUES
        (
            'PRIME_GUARDIAN', N'Hộ Vệ Prime', 0, 'BUFF', 1, 0,
            1, N'Chuyển hướng 35% sát thương trực tiếp từ đồng minh về Nghĩa Phục Prime. Không để máu Prime giảm dưới 1 HP. Mỗi lần bảo vệ tích 1 tầng Áp Lực.',
            '/assets/images/dcs-game/effects/prime-guardian.png', '#2563eb', GETDATE(), SYSUTCDATETIME()
        );
        PRINT N'-> Đã thêm Effect Type PRIME_GUARDIAN.';
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'PRIME_PRESSURE')
    BEGIN
        INSERT INTO dbo.HRK_SkillEffectTypes
        (
            Code, Name, IsDebuff, EffectGroup, IsBeneficial, IsStackable,
            DefaultStackLimit, Description, ImagePath, ColorHex, CreatedOn, UpdatedOn
        )
        VALUES
        (
            'PRIME_PRESSURE', N'Áp Lực', 0, 'BUFF', 1, 1,
            5, N'Tích lũy khi nhận sát thương thay đồng đội (tối đa 5 tầng). Khi đạt 5 tầng hoặc kết thúc Hộ Vệ sẽ giải phóng: hồi 2% Max HP và gây 20% DEF sát thương vật lý lên toàn bộ kẻ địch mỗi tầng.',
            '/assets/images/dcs-game/effects/prime-pressure.png', '#60a5fa', GETDATE(), SYSUTCDATETIME()
        );
        PRINT N'-> Đã thêm Effect Type PRIME_PRESSURE.';
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'PRIME_STAGGER')
    BEGIN
        INSERT INTO dbo.HRK_SkillEffectTypes
        (
            Code, Name, IsDebuff, EffectGroup, IsBeneficial, IsStackable,
            DefaultStackLimit, Description, ImagePath, ColorHex, CreatedOn, UpdatedOn
        )
        VALUES
        (
            'PRIME_STAGGER', N'Lung Lay', 1, 'DEBUFF', 0, 0,
            1, N'Giảm 15 điểm thanh hành động và giảm 10% Tốc độ trong 1 lượt.',
            '/assets/images/dcs-game/effects/prime-stagger.png', '#94a3b8', GETDATE(), SYSUTCDATETIME()
        );
        PRINT N'-> Đã thêm Effect Type PRIME_STAGGER.';
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'PRIME_BROKEN_MORALE')
    BEGIN
        INSERT INTO dbo.HRK_SkillEffectTypes
        (
            Code, Name, IsDebuff, EffectGroup, IsBeneficial, IsStackable,
            DefaultStackLimit, Description, ImagePath, ColorHex, CreatedOn, UpdatedOn
        )
        VALUES
        (
            'PRIME_BROKEN_MORALE', N'Vỡ Trận', 1, 'DEBUFF', 0, 0,
            1, N'Giảm 15% sát thương gây ra trong 2 lượt.',
            '/assets/images/dcs-game/effects/prime-broken-morale.png', '#64748b', GETDATE(), SYSUTCDATETIME()
        );
        PRINT N'-> Đã thêm Effect Type PRIME_BROKEN_MORALE.';
    END;

    ---------------------------------------------------------------------------
    -- 3. Create or Update Hero Template "Nghĩa Phục Prime"
    ---------------------------------------------------------------------------
    DECLARE @HeroTemplateId INT = (
        SELECT TOP 1 Id 
        FROM dbo.HRK_HeroTemplates 
        WHERE UPPER(Name) = N'NGHĨA PHỤC PRIME' OR Avatar LIKE '%nghia-phuc-prime%'
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
            N'Nghĩa Phục Prime',
            '/assets/images/dcs-game/nghia-phuc-prime.png',
            @FactionId,
            @ClassId,
            @RarityId,
            1100,    -- BaseHp (Rất cao - Tanker)
            95,      -- BaseAtk (Tương đối thấp)
            95,      -- BaseDef (Rất cao)
            90,      -- BaseSpd (Thấp)
            5.00,    -- BaseCrit
            150.00,  -- BaseCritDmg
            0.00,    -- BaseLifesteal
            95.00,   -- BaseAccuracy
            30.00,   -- BaseResistance (Kháng hiệu ứng cao)
            40,      -- BaseMagicDamage (Thấp)
            85,      -- BaseMagicResistance (Rất cao)
            SYSUTCDATETIME()
        );
        SET @HeroTemplateId = SCOPE_IDENTITY();
        PRINT N'-> Đã tạo mới HeroTemplate Nghĩa Phục Prime (Id: ' + CAST(@HeroTemplateId AS NVARCHAR(10)) + N').';
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_HeroTemplates
        SET
            Name = N'Nghĩa Phục Prime',
            Avatar = '/assets/images/dcs-game/nghia-phuc-prime.png',
            FactionId = @FactionId,
            ClassId = @ClassId,
            RarityId = @RarityId,
            BaseHp = 1100,
            BaseAtk = 95,
            BaseDef = 95,
            BaseSpd = 90,
            BaseCrit = 5.00,
            BaseCritDmg = 150.00,
            BaseLifesteal = 0.00,
            BaseAccuracy = 95.00,
            BaseResistance = 30.00,
            BaseMagicDamage = 40,
            BaseMagicResistance = 85
        WHERE Id = @HeroTemplateId;
        PRINT N'-> Đã cập nhật HeroTemplate Nghĩa Phục Prime (Id: ' + CAST(@HeroTemplateId AS NVARCHAR(10)) + N').';
    END;

    ---------------------------------------------------------------------------
    -- 4. Create or Update Skill Templates:
    --    - PRIME_SHIELD_WARRANTY (NORMAL)
    --    - PRIME_FORTRESS_CHARGE (ENERGY)
    ---------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'PRIME_SHIELD_WARRANTY')
    BEGIN
        INSERT INTO dbo.HRK_SkillTemplates
        (
            Id, Name, Icon, Description, PhaseDurations,
            CreatedOn, ImagePath, SkillTypeCode, TriggerCode, EnergyCost,
            DisplayOrder, IsActive, UpdatedOn
        )
        VALUES
        (
            'PRIME_SHIELD_WARRANTY', N'Khiên Này Có Bảo Hành', 'bi-shield-check',
            N'Nghĩa Phục Prime dùng khiên đánh một kẻ địch, gây 90% ATK sát thương vật lý. Khi đánh trúng, tạo khiên bằng 8% Max HP của Nghĩa Phục Prime cho đồng minh có phần trăm máu thấp nhất trong 2 lượt (nếu đã có khiên thì hồi thêm 4% Max HP và làm mới thời gian). Đồng thời Nghĩa Phục Prime nhận 1 tầng Kiên Cố (+5% DEF và +5% Kháng Phép, tối đa 4 tầng). Khi đạt 4 tầng Kiên Cố, tạo khiên bằng 12% Max HP cho bản thân và giải phóng toàn bộ tầng Kiên Cố.',
            NULL, GETDATE(), NULL, 'NORMAL', 'ON_ATTACK', 0,
            1, 1, SYSUTCDATETIME()
        );
        PRINT N'-> Đã tạo SkillTemplate PRIME_SHIELD_WARRANTY.';
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_SkillTemplates
        SET Name = N'Khiên Này Có Bảo Hành',
            Description = N'Nghĩa Phục Prime dùng khiên đánh một kẻ địch, gây 90% ATK sát thương vật lý. Khi đánh trúng, tạo khiên bằng 8% Max HP của Nghĩa Phục Prime cho đồng minh có phần trăm máu thấp nhất trong 2 lượt (nếu đã có khiên thì hồi thêm 4% Max HP và làm mới thời gian). Đồng thời Nghĩa Phục Prime nhận 1 tầng Kiên Cố (+5% DEF và +5% Kháng Phép, tối đa 4 tầng). Khi đạt 4 tầng Kiên Cố, tạo khiên bằng 12% Max HP cho bản thân và giải phóng toàn bộ tầng Kiên Cố.',
            SkillTypeCode = 'NORMAL',
            TriggerCode = 'ON_ATTACK',
            EnergyCost = 0,
            DisplayOrder = 1,
            IsActive = 1,
            UpdatedOn = SYSUTCDATETIME()
        WHERE Id = 'PRIME_SHIELD_WARRANTY';
        PRINT N'-> Đã cập nhật SkillTemplate PRIME_SHIELD_WARRANTY.';
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'PRIME_FORTRESS_CHARGE')
    BEGIN
        INSERT INTO dbo.HRK_SkillTemplates
        (
            Id, Name, Icon, Description, PhaseDurations,
            CreatedOn, ImagePath, SkillTypeCode, TriggerCode, EnergyCost,
            DisplayOrder, IsActive, UpdatedOn
        )
        VALUES
        (
            'PRIME_FORTRESS_CHARGE', N'Thành Trì Prime: Không Ai Được Phép Ngã', 'bi-shield-shaded',
            N'Dựng thành trì năng lượng khổng lồ lao thẳng sang đội hình đối phương, gây 60% ATK sát thương vật lý lên toàn bộ kẻ địch (không crit). Kẻ địch trúng đòn nhận hiệu ứng Lung Lay (trừ 15 điểm thanh hành động và giảm 10% Tốc độ trong 1 lượt). Chọn ngẫu nhiên tối đa 3 kẻ địch và gây hiệu ứng Vỡ Trận (giảm 15% sát thương gây ra trong 2 lượt). Sau đó thành trì quay về bao phủ toàn bộ đồng minh, tạo khiên bằng 10% Max HP + 120% DEF của Nghĩa Phục Prime (tối đa 25% Max HP của mục tiêu) trong 2 lượt, và Nghĩa Phục Prime nhận trạng thái Hộ Vệ Prime trong 2 lượt: chuyển hướng 35% sát thương trực tiếp từ đồng minh về bản thân (không để máu tụt dưới 1 HP). Mỗi lần nhận sát thương thay tích 1 tầng Áp Lực (tối đa 5 tầng). Khi đạt 5 tầng hoặc khi Hộ Vệ Prime kết thúc, giải phóng Áp Lực: hồi 2% Max HP và gây 20% DEF sát thương vật lý lên toàn bộ kẻ địch với mỗi tầng Áp Lực tích lũy.',
            NULL, GETDATE(), NULL, 'ENERGY', 'MANUAL_ENERGY_FULL', 100,
            2, 1, SYSUTCDATETIME()
        );
        PRINT N'-> Đã tạo SkillTemplate PRIME_FORTRESS_CHARGE.';
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_SkillTemplates
        SET Name = N'Thành Trì Prime: Không Ai Được Phép Ngã',
            Description = N'Dựng thành trì năng lượng khổng lồ lao thẳng sang đội hình đối phương, gây 60% ATK sát thương vật lý lên toàn bộ kẻ địch (không crit). Kẻ địch trúng đòn nhận hiệu ứng Lung Lay (trừ 15 điểm thanh hành động và giảm 10% Tốc độ trong 1 lượt). Chọn ngẫu nhiên tối đa 3 kẻ địch và gây hiệu ứng Vỡ Trận (giảm 15% sát thương gây ra trong 2 lượt). Sau đó thành trì quay về bao phủ toàn bộ đồng minh, tạo khiên bằng 10% Max HP + 120% DEF của Nghĩa Phục Prime (tối đa 25% Max HP của mục tiêu) trong 2 lượt, và Nghĩa Phục Prime nhận trạng thái Hộ Vệ Prime trong 2 lượt: chuyển hướng 35% sát thương trực tiếp từ đồng minh về bản thân (không để máu tụt dưới 1 HP). Mỗi lần nhận sát thương thay tích 1 tầng Áp Lực (tối đa 5 tầng). Khi đạt 5 tầng hoặc khi Hộ Vệ Prime kết thúc, giải phóng Áp Lực: hồi 2% Max HP và gây 20% DEF sát thương vật lý lên toàn bộ kẻ địch với mỗi tầng Áp Lực tích lũy.',
            SkillTypeCode = 'ENERGY',
            TriggerCode = 'MANUAL_ENERGY_FULL',
            EnergyCost = 100,
            DisplayOrder = 2,
            IsActive = 1,
            UpdatedOn = SYSUTCDATETIME()
        WHERE Id = 'PRIME_FORTRESS_CHARGE';
        PRINT N'-> Đã cập nhật SkillTemplate PRIME_FORTRESS_CHARGE.';
    END;

    ---------------------------------------------------------------------------
    -- 5. Link Skills to Hero in HRK_HeroSkills
    ---------------------------------------------------------------------------
    DELETE FROM dbo.HRK_HeroSkills WHERE HeroTemplateId = @HeroTemplateId;

    INSERT INTO dbo.HRK_HeroSkills (HeroTemplateId, SkillId, SkillOrder)
    VALUES
        (@HeroTemplateId, 'PRIME_SHIELD_WARRANTY', 1),
        (@HeroTemplateId, 'PRIME_FORTRESS_CHARGE', 2);

    PRINT N'-> Đã liên kết kỹ năng PRIME_SHIELD_WARRANTY (1) và PRIME_FORTRESS_CHARGE (2) cho Nghĩa Phục Prime.';

    ---------------------------------------------------------------------------
    -- 6. Clean and reseed Skill Effects, Scalings, Parameters
    ---------------------------------------------------------------------------
    DELETE p
    FROM dbo.HRK_SkillEffectParameters p
    INNER JOIN dbo.HRK_SkillEffects se ON se.Id = p.SkillEffectId
    WHERE se.SkillId IN ('PRIME_SHIELD_WARRANTY', 'PRIME_FORTRESS_CHARGE');

    DELETE sc
    FROM dbo.HRK_SkillEffectScalings sc
    INNER JOIN dbo.HRK_SkillEffects se ON se.Id = sc.SkillEffectId
    WHERE se.SkillId IN ('PRIME_SHIELD_WARRANTY', 'PRIME_FORTRESS_CHARGE');

    DELETE sm
    FROM dbo.HRK_SkillEffectStatModifiers sm
    INNER JOIN dbo.HRK_SkillEffects se ON se.Id = sm.SkillEffectId
    WHERE se.SkillId IN ('PRIME_SHIELD_WARRANTY', 'PRIME_FORTRESS_CHARGE');

    DELETE se
    FROM dbo.HRK_SkillEffects se
    WHERE se.SkillId IN ('PRIME_SHIELD_WARRANTY', 'PRIME_FORTRESS_CHARGE');

    ---------------------------------------------------------------------------
    -- 6.1 Seed Effects for PRIME_SHIELD_WARRANTY
    ---------------------------------------------------------------------------
    INSERT INTO dbo.HRK_SkillEffects
    (
        SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode,
        BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder,
        ExecutionGroup, ConditionCode, IsActive, CreatedOn, UpdatedOn
    )
    VALUES
    (
        'PRIME_SHIELD_WARRANTY', @DamageEffectTypeId, @EnemySingleTargetId, 'PHYSICAL',
        0, NULL, 100.00, 1, 1,
        NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @BasicEffectId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@BasicEffectId, @AtkAttrId, 0.900000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, DecimalValue, IntValue, BoolValue, CreatedOn, UpdatedOn)
    VALUES
        (@BasicEffectId, 'DAMAGE_COEFFICIENT', 0.9000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@BasicEffectId, 'ALLY_SHIELD_MAX_HP_PERCENT', 8.0000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@BasicEffectId, 'EXISTING_SHIELD_RESTORE_PERCENT', 4.0000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@BasicEffectId, 'SHIELD_DURATION_TURNS', NULL, 2, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@BasicEffectId, 'FORTITUDE_MAX_STACKS', NULL, 4, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@BasicEffectId, 'FORTITUDE_DEF_PERCENT_PER_STACK', 5.0000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@BasicEffectId, 'FORTITUDE_MAGIC_RESISTANCE_PERCENT_PER_STACK', 5.0000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@BasicEffectId, 'SELF_SHIELD_ON_MAX_STACK_PERCENT', 12.0000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@BasicEffectId, 'CAN_CRIT', NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    ---------------------------------------------------------------------------
    -- 6.2 Seed Effects for PRIME_FORTRESS_CHARGE
    ---------------------------------------------------------------------------
    INSERT INTO dbo.HRK_SkillEffects
    (
        SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode,
        BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder,
        ExecutionGroup, ConditionCode, IsActive, CreatedOn, UpdatedOn
    )
    VALUES
    (
        'PRIME_FORTRESS_CHARGE', @DamageEffectTypeId, @EnemyAllTargetId, 'PHYSICAL',
        0, NULL, 100.00, 1, 1,
        NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @UltEffectId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@UltEffectId, @AtkAttrId, 0.600000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, DecimalValue, IntValue, BoolValue, CreatedOn, UpdatedOn)
    VALUES
        (@UltEffectId, 'FORTRESS_DAMAGE_COEFFICIENT', 0.6000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltEffectId, 'CAN_CRIT', NULL, NULL, 0, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltEffectId, 'STAGGER_DURATION_TURNS', NULL, 1, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltEffectId, 'ACTION_BAR_REDUCTION', NULL, 15, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltEffectId, 'SPEED_REDUCTION_PERCENT', 10.0000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltEffectId, 'REFRESH_ON_REAPPLY', NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltEffectId, 'STACK_ON_REAPPLY', NULL, NULL, 0, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltEffectId, 'BROKEN_MORALE_TARGET_COUNT', NULL, 3, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltEffectId, 'OUTGOING_DAMAGE_REDUCTION_PERCENT', 15.0000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltEffectId, 'BROKEN_MORALE_DURATION_TURNS', NULL, 2, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltEffectId, 'TEAM_SHIELD_CASTER_MAX_HP_PERCENT', 10.0000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltEffectId, 'TEAM_SHIELD_CASTER_DEF_PERCENT', 120.0000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltEffectId, 'TEAM_SHIELD_TARGET_MAX_HP_CAP_PERCENT', 25.0000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltEffectId, 'TEAM_SHIELD_DURATION_TURNS', NULL, 2, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltEffectId, 'GUARDIAN_DURATION_TURNS', NULL, 2, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltEffectId, 'DAMAGE_REDIRECT_PERCENT', 35.0000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltEffectId, 'GUARDIAN_MIN_HP', NULL, 1, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltEffectId, 'PRESSURE_MAX_STACKS', NULL, 5, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltEffectId, 'PRESSURE_HEAL_MAX_HP_PERCENT_PER_STACK', 2.0000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltEffectId, 'PRESSURE_DAMAGE_DEF_PERCENT_PER_STACK', 20.0000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltEffectId, 'PRESSURE_CAN_CRIT', NULL, NULL, 0, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@UltEffectId, 'PRESSURE_RELEASE_AT_MAX', NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    PRINT N'-> Đã cấu hình xong hiệu ứng và parameters cho kỹ năng của Nghĩa Phục Prime.';

    ---------------------------------------------------------------------------
    -- 7. Seed Skill Animation Configs & Timeline Phases
    ---------------------------------------------------------------------------
    MERGE dbo.HRK_SkillAnimationConfigs AS target
    USING (VALUES ('PRIME_SHIELD_WARRANTY', 'prime-shield-warranty', 2000, CAST(1.0 AS DECIMAL(5,2))))
        AS source (SkillId, AnimationKey, TotalDurationMs, DefaultPlaybackSpeed)
    ON target.SkillId = source.SkillId
    WHEN MATCHED THEN UPDATE SET TotalDurationMs = source.TotalDurationMs, AnimationKey = source.AnimationKey, UpdatedOn = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN INSERT (SkillId, AnimationKey, TotalDurationMs, DefaultPlaybackSpeed)
        VALUES (source.SkillId, source.AnimationKey, source.TotalDurationMs, source.DefaultPlaybackSpeed);

    DECLARE @BasicAnimConfigId INT = (SELECT Id FROM dbo.HRK_SkillAnimationConfigs WHERE SkillId = 'PRIME_SHIELD_WARRANTY');
    DELETE FROM dbo.HRK_SkillTimelinePhases WHERE SkillAnimationConfigId = @BasicAnimConfigId;

    INSERT INTO dbo.HRK_SkillTimelinePhases (SkillAnimationConfigId, PhaseCode, StartAtMs, DurationMs, TriggerEventType, DisplayOrder)
    VALUES
        (@BasicAnimConfigId, 'CAST', 0, 800, 'SKILL_CAST', 1),
        (@BasicAnimConfigId, 'IMPACT', 800, 400, 'DAMAGE', 2),
        (@BasicAnimConfigId, 'STATUS', 1200, 400, 'SHIELD_APPLIED', 3),
        (@BasicAnimConfigId, 'RECOVERY', 1600, 400, 'SKILL_COMPLETED', 4);

    MERGE dbo.HRK_SkillAnimationConfigs AS target
    USING (VALUES ('PRIME_FORTRESS_CHARGE', 'prime-fortress-charge', 3200, CAST(1.0 AS DECIMAL(5,2))))
        AS source (SkillId, AnimationKey, TotalDurationMs, DefaultPlaybackSpeed)
    ON target.SkillId = source.SkillId
    WHEN MATCHED THEN UPDATE SET TotalDurationMs = source.TotalDurationMs, AnimationKey = source.AnimationKey, UpdatedOn = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN INSERT (SkillId, AnimationKey, TotalDurationMs, DefaultPlaybackSpeed)
        VALUES (source.SkillId, source.AnimationKey, source.TotalDurationMs, source.DefaultPlaybackSpeed);

    DECLARE @UltAnimConfigId INT = (SELECT Id FROM dbo.HRK_SkillAnimationConfigs WHERE SkillId = 'PRIME_FORTRESS_CHARGE');
    DELETE FROM dbo.HRK_SkillTimelinePhases WHERE SkillAnimationConfigId = @UltAnimConfigId;

    INSERT INTO dbo.HRK_SkillTimelinePhases (SkillAnimationConfigId, PhaseCode, StartAtMs, DurationMs, TriggerEventType, DisplayOrder)
    VALUES
        (@UltAnimConfigId, 'CAST', 0, 900, 'PRIME_FORTRESS_CHARGE_STARTED', 1),
        (@UltAnimConfigId, 'TRAVEL', 900, 550, 'SKILL_CAST', 2),
        (@UltAnimConfigId, 'IMPACT', 1450, 600, 'PRIME_FORTRESS_IMPACT', 3),
        (@UltAnimConfigId, 'RETURN', 2050, 400, 'PRIME_FORTRESS_RETURNED', 4),
        (@UltAnimConfigId, 'RECOVERY', 2450, 750, 'SKILL_COMPLETED', 5);

    PRINT N'-> Đã cấu hình xong animation timeline phases cho kỹ năng của Nghĩa Phục Prime.';

    ---------------------------------------------------------------------------
    -- 8. Seed Hero Star Aura Configurations (2, 3, 4, 5 stars)
    ---------------------------------------------------------------------------
    MERGE dbo.HRK_HeroStarAuraConfigs AS target
    USING (VALUES
        (@HeroTemplateId, CAST(2 AS TINYINT), N'PRIME_STAR_2', N'nghia-phuc-prime', N'Khiên Quang Sơ Cấp', N'Vòng năng lượng lam nhạt dưới chân và các tia sáng khiên mờ.', '#3b82f6', '#1e3a8a', CAST(0.90 AS DECIMAL(5,2)), CAST(1 AS TINYINT), 1),
        (@HeroTemplateId, CAST(3 AS TINYINT), N'PRIME_STAR_3', N'nghia-phuc-prime', N'Kiên Cố Thành Trì', N'Các mảnh lục giác lam ngọc xoay quanh thân, tia điện xanh chạy trên giáp.', '#2563eb', '#1d4ed8', CAST(1.35 AS DECIMAL(5,2)), CAST(2 AS TINYINT), 1),
        (@HeroTemplateId, CAST(4 AS TINYINT), N'PRIME_STAR_4', N'nghia-phuc-prime', N'Bất Diệt Hộ Vệ', N'Rune năng lượng cổ ngữ xoay dưới chân, khiên năng lượng rực sáng bảo vệ.', '#1d4ed8', '#0284c7', CAST(1.80 AS DECIMAL(5,2)), CAST(3 AS TINYINT), 1),
        (@HeroTemplateId, CAST(5 AS TINYINT), N'PRIME_STAR_5', N'nghia-phuc-prime', N'Thành Trì Tuyệt Đối', N'Vầng hào quang Prime hoàn chỉnh, mái vòm năng lượng lam chấn động, điện quang và tinh thể giáp hộ thể vĩnh cửu.', '#60a5fa', '#1e40af', CAST(2.30 AS DECIMAL(5,2)), CAST(4 AS TINYINT), 1)
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

    PRINT N'-> Đã cấu hình xong Star Aura cho Nghĩa Phục Prime.';

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
                (1100 / 10) + (95 * 3) + (40 * 3) + (95 * 2) + 90, -- Calculated initial power
                4,    -- AuraTier for 5 stars
                0,
                1,    -- Favorite
                1,
                NULL,
                SYSUTCDATETIME(),
                SYSUTCDATETIME()
            );
            PRINT N'-> Đã cấp Nghĩa Phục Prime cho PlayerId = 1.';
        END
        ELSE
        BEGIN
            UPDATE dbo.HRK_PlayerHeroes
            SET Stars = 5,
                AuraTier = 4,
                Power = (1100 / 10) + (95 * 3) + (40 * 3) + (95 * 2) + 90,
                UpdatedOn = SYSUTCDATETIME()
            WHERE PlayerId = @TargetPlayerId AND HeroTemplateId = @HeroTemplateId;
            PRINT N'-> Đã cập nhật Nghĩa Phục Prime cho PlayerId = 1.';
        END;
    END;

    COMMIT TRANSACTION;
    PRINT N'=== THÀNH CÔNG: Đã triển khai hoàn chỉnh nhân vật Nghĩa Phục Prime vào Database! ===';

    ---------------------------------------------------------------------------
    -- 10. Summary verification
    ---------------------------------------------------------------------------
    SELECT
        ht.Id AS HeroTemplateId,
        ht.Name AS HeroName,
        r.Name AS RarityName,
        ht.Avatar,
        ht.BaseAtk,
        ht.BaseDef,
        ht.BaseHp,
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

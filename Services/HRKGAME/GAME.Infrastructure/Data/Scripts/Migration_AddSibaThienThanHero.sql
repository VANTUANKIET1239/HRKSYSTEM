/*
    Migration: Add Siba Thiên Thần (Mythic Support Hero)
    --------------------------------------------------------------------------------
    Hero:
      Name: Siba Thiên Thần
      Code: siba-thien-than
      Rarity: MYTHIC
      Faction: QUAN
      Class: SUPPORT
      Damage Type: Magic
      Avatar: /assets/images/dcs-game/siba-thien-than-mythic.png

    Skills:
      1. SIBA_ANGEL_GENTLE_WING (Cánh Nhẹ Cổ Vũ) - NORMAL (EnergyCost: 0)
         - Target: ALLY_LOWEST_HP_PREFER_WITHOUT_STATUS (preferred missing: ENCOURAGEMENT)
         - Heal 140% Magic Damage (CAN_CRIT = 0)
         - Energy +10 to target
         - 50% chance Cổ Vũ 2 turns (Back row: ENCOURAGEMENT_OFFENSE, Front row: ENCOURAGEMENT_DEFENSE)
         - +1 Ân Phúc (ANGEL_BLESSING) to Siba (max 5)
      2. SIBA_CELESTIAL_PROTECTION (Thiên Hộ Giáng Thế) - ENERGY (EnergyCost: 100)
         - Target: ALLY_ALL
         - Normal:
           * Apply CELESTIAL_PROTECTION (+20% SPD, +20% Resistance for 2 turns) to all living allies
           * Dispel up to 1 random dispellable debuff per living ally (DISPEL_DEBUFF)
         - Empowered (when Siba has 5 Ân Phúc at cast start):
           * Full normal effects above
           * Heal 110% Magic Damage to all living allies (CAN_CRIT = 0)
           * Refresh Encouragement duration to 2 turns for allies having it
           * +15 Energy to allies having Encouragement
           * Consumes 5 Ân Phúc

    Idempotent, Transaction-safe, Data-driven.
*/

USE [HRK];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    PRINT N'=== BẮT ĐẦU MIGRATION HERO MYTHIC: SIBA THIÊN THẦN ===';

    ---------------------------------------------------------------------------
    -- 1. Resolve Reference Data
    ---------------------------------------------------------------------------
    DECLARE @RarityMythicId INT = (SELECT TOP 1 Id FROM dbo.HRK_Rarities WHERE UPPER(Code) = 'MYTHIC');
    IF @RarityMythicId IS NULL SET @RarityMythicId = 5;

    DECLARE @FactionQuanId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroFactions WHERE UPPER(Code) = 'QUAN');
    IF @FactionQuanId IS NULL SET @FactionQuanId = 4;

    DECLARE @ClassSupportId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroClasses WHERE UPPER(Code) = 'SUPPORT');
    IF @ClassSupportId IS NULL SET @ClassSupportId = 5;

    DECLARE @HpAttrId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE UPPER(Code) = 'HP');
    DECLARE @AtkAttrId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE UPPER(Code) = 'ATK');
    DECLARE @DefAttrId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE UPPER(Code) = 'DEF');
    DECLARE @SpdAttrId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE UPPER(Code) = 'SPD');
    DECLARE @ResistanceAttrId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE UPPER(Code) IN ('RESISTANCE', 'RES'));
    DECLARE @MagicDmgAttrId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE UPPER(Code) = 'MAGIC_DAMAGE');
    DECLARE @MagicResAttrId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE UPPER(Code) = 'MAGIC_RESISTANCE');

    IF @AtkAttrId IS NULL THROW 51001, N'Lỗi: Thiếu AttributeType ATK.', 1;
    IF @DefAttrId IS NULL THROW 51002, N'Lỗi: Thiếu AttributeType DEF.', 1;
    IF @HpAttrId IS NULL SET @HpAttrId = @DefAttrId;
    IF @SpdAttrId IS NULL SET @SpdAttrId = @AtkAttrId;
    IF @ResistanceAttrId IS NULL SET @ResistanceAttrId = @DefAttrId;
    IF @MagicDmgAttrId IS NULL SET @MagicDmgAttrId = @AtkAttrId;
    IF @MagicResAttrId IS NULL SET @MagicResAttrId = @DefAttrId;

    ---------------------------------------------------------------------------
    -- 2. Upsert Target Types
    ---------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) = 'ALLY_LOWEST_HP_PREFER_WITHOUT_STATUS')
    BEGIN
        DECLARE @MaxTargetOrder INT = ISNULL((SELECT MAX(DisplayOrder) FROM dbo.HRK_SkillTargetTypes), 20);
        INSERT INTO dbo.HRK_SkillTargetTypes (Code, Name, TargetSide, SelectionRule, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
        VALUES ('ALLY_LOWEST_HP_PREFER_WITHOUT_STATUS', N'Đồng minh thấp máu nhất ưu tiên chưa có trạng thái', 'ALLY', 'LOWEST_HP_PREFER_WITHOUT_STATUS', @MaxTargetOrder + 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
        PRINT N'-> Đã thêm TargetType ALLY_LOWEST_HP_PREFER_WITHOUT_STATUS.';
    END;

    DECLARE @AllyLowestHpPreferTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) = 'ALLY_LOWEST_HP_PREFER_WITHOUT_STATUS');
    DECLARE @AllyAllTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) IN ('ALLY_ALL', 'ALL_ALLIES'));
    DECLARE @SelfTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) = 'SELF');

    IF @AllyLowestHpPreferTargetId IS NULL THROW 51003, N'Lỗi: Thiếu TargetType ALLY_LOWEST_HP_PREFER_WITHOUT_STATUS.', 1;
    IF @AllyAllTargetId IS NULL THROW 51004, N'Lỗi: Thiếu TargetType ALLY_ALL.', 1;

    ---------------------------------------------------------------------------
    -- 3. Upsert Effect Types
    ---------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'DISPEL_DEBUFF')
    BEGIN
        INSERT INTO dbo.HRK_SkillEffectTypes (Code, Name, EffectGroup, IsBeneficial, IsStackable, DefaultStackLimit, Description, ImagePath, ColorHex, CreatedOn, UpdatedOn)
        VALUES ('DISPEL_DEBUFF', N'Hóa giải bất lợi', 'UTILITY', 1, 0, NULL, N'Xóa bỏ hiệu ứng bất lợi có thể giải trừ trên mục tiêu', '/assets/images/dcs-game/effects/stat-buff.png', '#38bdf8', GETDATE(), SYSUTCDATETIME());
        PRINT N'-> Đã thêm EffectType DISPEL_DEBUFF.';
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'ENCOURAGEMENT_OFFENSE')
    BEGIN
        INSERT INTO dbo.HRK_SkillEffectTypes (Code, Name, EffectGroup, IsBeneficial, IsStackable, DefaultStackLimit, Description, ImagePath, ColorHex, CreatedOn, UpdatedOn)
        VALUES ('ENCOURAGEMENT_OFFENSE', N'Cổ Vũ Công', 'BUFF', 1, 0, 1, N'Tăng 20% Sát thương Vật lý và 20% Sát thương Phép trong 2 lượt', '/assets/images/dcs-game/effects/stat-buff.png', '#f59e0b', GETDATE(), SYSUTCDATETIME());
        PRINT N'-> Đã thêm EffectType ENCOURAGEMENT_OFFENSE.';
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'ENCOURAGEMENT_DEFENSE')
    BEGIN
        INSERT INTO dbo.HRK_SkillEffectTypes (Code, Name, EffectGroup, IsBeneficial, IsStackable, DefaultStackLimit, Description, ImagePath, ColorHex, CreatedOn, UpdatedOn)
        VALUES ('ENCOURAGEMENT_DEFENSE', N'Cổ Vũ Thủ', 'BUFF', 1, 0, 1, N'Tăng 20% DEF và 20% Kháng Phép trong 2 lượt', '/assets/images/dcs-game/effects/shield.png', '#06b6d4', GETDATE(), SYSUTCDATETIME());
        PRINT N'-> Đã thêm EffectType ENCOURAGEMENT_DEFENSE.';
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'CELESTIAL_PROTECTION')
    BEGIN
        INSERT INTO dbo.HRK_SkillEffectTypes (Code, Name, EffectGroup, IsBeneficial, IsStackable, DefaultStackLimit, Description, ImagePath, ColorHex, CreatedOn, UpdatedOn)
        VALUES ('CELESTIAL_PROTECTION', N'Thiên Hộ', 'BUFF', 1, 0, 1, N'Tăng 20% Tốc độ và 20% Kháng hiệu ứng trong 2 lượt', '/assets/images/dcs-game/effects/stat-buff.png', '#eab308', GETDATE(), SYSUTCDATETIME());
        PRINT N'-> Đã thêm EffectType CELESTIAL_PROTECTION.';
    END;

    DECLARE @HealEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'HEAL');
    DECLARE @EnergyChangeEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'ENERGY_CHANGE');
    DECLARE @DispelDebuffEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'DISPEL_DEBUFF');
    DECLARE @EncouragementOffenseEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'ENCOURAGEMENT_OFFENSE');
    DECLARE @EncouragementDefenseEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'ENCOURAGEMENT_DEFENSE');
    DECLARE @CelestialProtectionEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'CELESTIAL_PROTECTION');
    DECLARE @StatBuffEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'STAT_BUFF');

    ---------------------------------------------------------------------------
    -- 4. Upsert Hero Template: Siba Thiên Thần
    ---------------------------------------------------------------------------
    DECLARE @SibaHeroId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE UPPER(Name) = N'SIBA THIÊN THẦN' OR Avatar LIKE '%siba-thien-than%');

    IF @SibaHeroId IS NULL
    BEGIN
        INSERT INTO dbo.HRK_HeroTemplates
        (
            Name, Avatar, FactionId, ClassId, RarityId,
            BaseHp, BaseAtk, BaseDef, BaseSpd, BaseCrit, BaseCritDmg,
            BaseLifesteal, BaseAccuracy, BaseResistance, BaseMagicDamage, BaseMagicResistance, CreatedOn
        )
        VALUES
        (
            N'Siba Thiên Thần', '/assets/images/dcs-game/siba-thien-than-mythic.png', @FactionQuanId, @ClassSupportId, @RarityMythicId,
            1050, 45, 75, 118, 5.00, 150.00, 0.00, 85.00, 30.00, 220, 150, SYSUTCDATETIME()
        );
        SET @SibaHeroId = SCOPE_IDENTITY();
        PRINT N'-> Đã tạo mới HeroTemplate Siba Thiên Thần (Id: ' + CAST(@SibaHeroId AS NVARCHAR) + N').';
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_HeroTemplates
        SET Name = N'Siba Thiên Thần',
            Avatar = '/assets/images/dcs-game/siba-thien-than-mythic.png',
            FactionId = @FactionQuanId, ClassId = @ClassSupportId, RarityId = @RarityMythicId,
            BaseHp = 1050, BaseAtk = 45, BaseDef = 75, BaseSpd = 118, BaseCrit = 5.00, BaseCritDmg = 150.00,
            BaseLifesteal = 0.00, BaseAccuracy = 85.00, BaseResistance = 30.00, BaseMagicDamage = 220, BaseMagicResistance = 150
        WHERE Id = @SibaHeroId;
        PRINT N'-> Đã cập nhật HeroTemplate Siba Thiên Thần (Id: ' + CAST(@SibaHeroId AS NVARCHAR) + N').';
    END;

    ---------------------------------------------------------------------------
    -- 5. Upsert Skill Templates
    ---------------------------------------------------------------------------
    DECLARE @SkillDefs TABLE (
        Id NVARCHAR(50),
        Name NVARCHAR(100),
        Icon NVARCHAR(50),
        Description NVARCHAR(1000),
        ImagePath NVARCHAR(255),
        SkillTypeCode NVARCHAR(30),
        TriggerCode NVARCHAR(30),
        EnergyCost INT,
        DisplayOrder INT
    );

    INSERT INTO @SkillDefs (Id, Name, Icon, Description, ImagePath, SkillTypeCode, TriggerCode, EnergyCost, DisplayOrder) VALUES
    ('SIBA_ANGEL_GENTLE_WING', N'Cánh Nhẹ Cổ Vũ', 'bi-feather',
     N'Dang đôi cánh thiên thần hồi phục 140% Magic Damage cho 1 đồng minh có % HP thấp nhất (ưu tiên người chưa có Cổ Vũ), đồng thời tăng 10 Năng lượng. Có 50% cơ hội áp dụng Cổ Vũ trong 2 lượt (Hàng sau: +20% Sát thương; Hàng trước: +20% Phòng thủ & Kháng phép). Siba nhận 1 tầng Ân Phúc (tối đa 5 tầng).',
     '/assets/images/dcs-game/siba-thien-than-mythic.png', 'NORMAL', 'ON_ATTACK', 0, 1),

    ('SIBA_CELESTIAL_PROTECTION', N'Thiên Hộ Giáng Thế', 'bi-shield-fill-check',
     N'Mở rộng đôi cánh tỏa hào quang thiên hộ lên toàn đội trong 2 lượt (+20% Tốc độ, +20% Kháng hiệu ứng) và hóa giải ngẫu nhiên tối đa 1 hiệu ứng bất lợi cho mỗi đồng minh. CƯỜNG HÓA (khi có 5 Ân Phúc): Hồi phục thêm 110% Magic Damage cho toàn đội, làm mới Cổ Vũ về 2 lượt và hồi 15 Năng lượng cho những ai đang có Cổ Vũ, sau đó tiêu thụ 5 Ân Phúc.',
     '/assets/images/dcs-game/siba-thien-than-mythic.png', 'ENERGY', 'MANUAL_ENERGY_FULL', 100, 2);

    MERGE dbo.HRK_SkillTemplates AS target
    USING @SkillDefs AS source
    ON target.Id = source.Id
    WHEN MATCHED THEN
        UPDATE SET
            target.Name = source.Name,
            target.Icon = source.Icon,
            target.Description = source.Description,
            target.ImagePath = source.ImagePath,
            target.SkillTypeCode = source.SkillTypeCode,
            target.TriggerCode = source.TriggerCode,
            target.EnergyCost = source.EnergyCost,
            target.DisplayOrder = source.DisplayOrder,
            target.IsActive = 1,
            target.UpdatedOn = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (Id, Name, Icon, Description, CreatedOn, ImagePath, SkillTypeCode, TriggerCode, EnergyCost, DisplayOrder, IsActive, UpdatedOn)
        VALUES (source.Id, source.Name, source.Icon, source.Description, GETDATE(), source.ImagePath, source.SkillTypeCode, source.TriggerCode, source.EnergyCost, source.DisplayOrder, 1, SYSUTCDATETIME());

    PRINT N'-> Đã upsert 2 Skill Templates của Siba Thiên Thần.';

    ---------------------------------------------------------------------------
    -- 6. Map Exactly 2 Skills to Siba in HRK_HeroSkills
    ---------------------------------------------------------------------------
    DELETE FROM dbo.HRK_HeroSkills WHERE HeroTemplateId = @SibaHeroId;

    INSERT INTO dbo.HRK_HeroSkills (HeroTemplateId, SkillId, SkillOrder)
    VALUES
        (@SibaHeroId, 'SIBA_ANGEL_GENTLE_WING', 1),
        (@SibaHeroId, 'SIBA_CELESTIAL_PROTECTION', 2);

    PRINT N'-> Đã gán 2 kỹ năng cho HeroTemplate Siba Thiên Thần trong HRK_HeroSkills.';

    ---------------------------------------------------------------------------
    -- 7. Clean and Seed Skill Effects, Scalings, Parameters, StatModifiers
    ---------------------------------------------------------------------------
    DECLARE @SibaSkillIds TABLE (SkillId NVARCHAR(50));
    INSERT INTO @SibaSkillIds VALUES
        ('SIBA_ANGEL_GENTLE_WING'),
        ('SIBA_CELESTIAL_PROTECTION');

    DELETE p FROM dbo.HRK_SkillEffectParameters p
    INNER JOIN dbo.HRK_SkillEffects e ON p.SkillEffectId = e.Id
    INNER JOIN @SibaSkillIds s ON e.SkillId = s.SkillId;

    DELETE sc FROM dbo.HRK_SkillEffectScalings sc
    INNER JOIN dbo.HRK_SkillEffects e ON sc.SkillEffectId = e.Id
    INNER JOIN @SibaSkillIds s ON e.SkillId = s.SkillId;

    DELETE sm FROM dbo.HRK_SkillEffectStatModifiers sm
    INNER JOIN dbo.HRK_SkillEffects e ON sm.SkillEffectId = e.Id
    INNER JOIN @SibaSkillIds s ON e.SkillId = s.SkillId;

    DELETE e FROM dbo.HRK_SkillEffects e
    INNER JOIN @SibaSkillIds s ON e.SkillId = s.SkillId;

    DECLARE @EffId BIGINT;

    ---------------------------------------------------------------------------
    -- 7.1 SIBA_ANGEL_GENTLE_WING (Basic)
    ---------------------------------------------------------------------------
    -- Effect 1: HEAL 140% Magic Damage, Target: ALLY_LOWEST_HP_PREFER_WITHOUT_STATUS, CanCrit = 0
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('SIBA_ANGEL_GENTLE_WING', @HealEffectTypeId, @AllyLowestHpPreferTargetId, NULL, 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @MagicDmgAttrId, 1.400000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, StringValue, BoolValue, IntValue, CreatedOn, UpdatedOn)
    VALUES
        (@EffId, 'CAN_CRIT', NULL, 0, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'TARGET_SIDE', 'ALLY', NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'PREFERRED_MISSING_STATUS_GROUP', 'ENCOURAGEMENT', NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'TARGET_COUNT', NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 2: ENERGY_CHANGE +10
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('SIBA_ANGEL_GENTLE_WING', @EnergyChangeEffectTypeId, @AllyLowestHpPreferTargetId, NULL, 10, NULL, 100.00, 1, 2, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, IntValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'ENERGY_DELTA', 10, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 3: ENCOURAGEMENT (50% Chance, 2 turns duration, 20% buff)
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('SIBA_ANGEL_GENTLE_WING', @StatBuffEffectTypeId, @AllyLowestHpPreferTargetId, NULL, 20, 2, 50.00, 1, 3, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, StringValue, IntValue, CreatedOn, UpdatedOn)
    VALUES
        (@EffId, 'STATUS_GROUP', 'ENCOURAGEMENT', NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'BUFF_PERCENT', NULL, 20, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'DURATION_TURNS', NULL, 2, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 4: Resource Configuration (ANGEL_BLESSING +1, Max 5)
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('SIBA_ANGEL_GENTLE_WING', @StatBuffEffectTypeId, @SelfTargetId, NULL, 1, NULL, 100.00, 5, 4, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, StringValue, IntValue, CreatedOn, UpdatedOn)
    VALUES
        (@EffId, 'RESOURCE_CODE', 'ANGEL_BLESSING', NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'MAX_BLESSING_STACKS', NULL, 5, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'BLESSING_GAIN_PER_BASIC', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    ---------------------------------------------------------------------------
    -- 7.2 SIBA_CELESTIAL_PROTECTION (Energy)
    ---------------------------------------------------------------------------
    -- Effect 1: CELESTIAL_PROTECTION (+20% SPD, +20% Resistance, 2 turns, ALLY_ALL)
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('SIBA_CELESTIAL_PROTECTION', @CelestialProtectionEffectTypeId, @AllyAllTargetId, NULL, 20, 2, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectStatModifiers (SkillEffectId, AttributeTypeId, ValueType, Value, CreatedOn, UpdatedOn)
    VALUES
        (@EffId, @SpdAttrId, 'PERCENT', 20.000000, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, @ResistanceAttrId, 'PERCENT', 20.000000, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, StringValue, IntValue, CreatedOn, UpdatedOn)
    VALUES
        (@EffId, 'STATUS_CODE', 'CELESTIAL_PROTECTION', NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'DURATION_TURNS', NULL, 2, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 2: DISPEL_DEBUFF (Up to 1 random dispellable debuff, ALLY_ALL)
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('SIBA_CELESTIAL_PROTECTION', @DispelDebuffEffectTypeId, @AllyAllTargetId, NULL, 1, NULL, 100.00, 1, 2, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, StringValue, IntValue, BoolValue, CreatedOn, UpdatedOn)
    VALUES
        (@EffId, 'DISPEL_COUNT', NULL, 1, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'SELECTION_MODE', 'RANDOM', NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'DISPELLABLE_ONLY', NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 3: Resource Empowered Threshold Configuration
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('SIBA_CELESTIAL_PROTECTION', @StatBuffEffectTypeId, @SelfTargetId, NULL, 5, NULL, 100.00, 1, 3, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, StringValue, IntValue, CreatedOn, UpdatedOn)
    VALUES
        (@EffId, 'RESOURCE_CODE', 'ANGEL_BLESSING', NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'BLESSING_COST_FOR_EMPOWERED', NULL, 5, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 4 (EMPOWERED): HEAL 110% Magic Damage, ALLY_ALL, CAN_CRIT = 0
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn, ExecutionGroup)
    VALUES ('SIBA_CELESTIAL_PROTECTION', @HealEffectTypeId, @AllyAllTargetId, NULL, 0, NULL, 100.00, 1, 4, 1, SYSUTCDATETIME(), SYSUTCDATETIME(), 'EMPOWERED');
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @MagicDmgAttrId, 1.100000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'CAN_CRIT', 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 5 (EMPOWERED): Apply Encouragement to all; existing holders refresh and gain +15 Energy
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn, ExecutionGroup)
    VALUES ('SIBA_CELESTIAL_PROTECTION', @EnergyChangeEffectTypeId, @AllyAllTargetId, NULL, 15, NULL, 100.00, 1, 5, 1, SYSUTCDATETIME(), SYSUTCDATETIME(), 'EMPOWERED');
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, StringValue, IntValue, CreatedOn, UpdatedOn)
    VALUES
        (@EffId, 'STATUS_GROUP', 'ENCOURAGEMENT', NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'BUFF_PERCENT', NULL, 20, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'REFRESH_DURATION', NULL, 2, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'EMPOWERED_ENERGY_GAIN', NULL, 15, SYSUTCDATETIME(), SYSUTCDATETIME());

    PRINT N'-> Đã seed toàn bộ SkillEffects, Scalings, Parameters, StatModifiers cho Siba Thiên Thần.';

    ---------------------------------------------------------------------------
    -- 8. Grant Siba to Player 1 (If exists)
    ---------------------------------------------------------------------------
    IF EXISTS (SELECT 1 FROM dbo.HRK_Players WHERE Id = 1)
    BEGIN
        INSERT INTO dbo.HRK_PlayerHeroes
        (
            PlayerId, HeroTemplateId, Level, Exp, MaxExp, Stars, Power, AuraTier,
            IsLocked, IsFavorite, IsActive, CurrentStats, CreatedOn, UpdatedOn
        )
        SELECT
            1,
            @SibaHeroId,
            1 AS Level,
            0 AS Exp,
            500 AS MaxExp,
            1 AS Stars,
            (ht.BaseHp / 10) + (ht.BaseAtk * 3) + (ht.BaseDef * 2) + ht.BaseSpd + (ht.BaseMagicDamage * 2) AS Power,
            1 AS AuraTier,
            0 AS IsLocked,
            0 AS IsFavorite,
            1 AS IsActive,
            NULL AS CurrentStats,
            GETDATE(),
            GETDATE()
        FROM dbo.HRK_HeroTemplates ht
        WHERE ht.Id = @SibaHeroId
          AND NOT EXISTS (
              SELECT 1 FROM dbo.HRK_PlayerHeroes ph
              WHERE ph.PlayerId = 1 AND ph.HeroTemplateId = @SibaHeroId
          );

        PRINT N'-> Đã cấp Hero Siba Thiên Thần cho PlayerId = 1.';
    END;

    ---------------------------------------------------------------------------
    -- 9. Post-Condition Validation
    ---------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_HeroTemplates WHERE Id = @SibaHeroId AND Avatar = '/assets/images/dcs-game/siba-thien-than-mythic.png')
    BEGIN
        THROW 51099, N'Hậu kiểm thất bại: Hero Siba Thiên Thần chưa được thiết lập đúng Avatar.', 1;
    END;

    IF (SELECT COUNT(*) FROM dbo.HRK_HeroSkills WHERE HeroTemplateId = @SibaHeroId) <> 2
    BEGIN
        THROW 51098, N'Hậu kiểm thất bại: Hero Siba Thiên Thần không có đúng 2 kỹ năng.', 1;
    END;

    COMMIT TRANSACTION;
    PRINT N'=== [HOÀN TẤT THÀNH CÔNG] Migration_AddSibaThienThanHero ===';

    -- Display result
    SELECT 
        ht.Id,
        ht.Name,
        r.Name AS Rarity,
        f.Name AS Faction,
        c.Name AS Class,
        ht.Avatar,
        ht.BaseHp,
        ht.BaseMagicDamage,
        ht.BaseMagicResistance,
        ht.BaseSpd
    FROM dbo.HRK_HeroTemplates ht
    INNER JOIN dbo.HRK_Rarities r ON r.Id = ht.RarityId
    INNER JOIN dbo.HRK_HeroFactions f ON f.Id = ht.FactionId
    INNER JOIN dbo.HRK_HeroClasses c ON c.Id = ht.ClassId
    WHERE ht.Id = @SibaHeroId;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR (@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;
GO

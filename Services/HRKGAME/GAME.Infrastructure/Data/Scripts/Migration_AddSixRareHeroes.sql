/*
    Migration: Add Six New Rare Heroes and Twelve Skills
    --------------------------------------------------------------------------------
    Heroes:
    1. Trọng Chưa Nổ (trong-chua-no) - Assassin / Ngụy (Physical)
       - Basic: TRONG_CHUA_NO_BASIC (Rút Chốt Thử Thôi) - 105% ATK
       - Energy: TRONG_CHUA_NO_THREE_SECONDS (Ba Giây Chưa Nổ) - 175% ATK, 30% PANIC 1 turn
    2. Quang Vinh THCS (quang-vinh-thcs) - Tanker / Thục (Physical)
       - Basic: QUANG_VINH_THCS_BASIC (Trực Nhật Kiên Cường) - 90% ATK, DEF +10% 1 turn
       - Energy: QUANG_VINH_THCS_HONOR_BARRIER (Hàng Rào Danh Dự) - 65% ATK AoE, Shield 10% Max HP all allies 2 turns
    3. Nguyên Xàm Lớn (nguyen-xam-lon) - Warrior / Quần (Physical)
       - Basic: NGUYEN_XAM_LON_BASIC (Nói Một Là Một) - 110% ATK, 20% DEF -15% 1 turn
       - Energy: NGUYEN_XAM_LON_BOSS_ENTERS (Đại Ca Xuống Sân) - 105% ATK front row, ATK +10% 2 turns
    4. Tiến Dũng Xuân (tien-dung-xuan) - Mage / Ngô (Magic)
       - Basic: TIEN_DUNG_XUAN_BASIC (Khai Bút Đầu Xuân) - 100% MagicDamage, 20% SILENCE 1 turn
       - Energy: TIEN_DUNG_XUAN_TEN_THOUSAND_GLYPHS (Vạn Tự Khai Hoa) - 70% MagicDamage AoE, 30% MagicRes -15% 2 turns
    5. Văn Trọng Điện Vàng (van-trong-dien-vang) - Mage / Quần (Magic)
       - Basic: VAN_TRONG_DIEN_VANG_BASIC (Tĩnh Điện Má Hồng) - 95% MagicDamage, 25% Action Bar -10
       - Energy: VAN_TRONG_DIEN_VANG_CHAIN_LIGHTNING (Điện Vàng Liên Hoàn) - 85% MagicDamage up to 3 distinct enemies, 30% STUN 1 turn
    6. Tường Long Cấp 3 (tuong-long-cap-3) - Support / Thục (Physical/Heal)
       - Basic: TUONG_LONG_CAP_3_BASIC (Giao Bài Tận Nơi) - 90% ATK, +10 Energy ally lowest energy
       - Energy: TUONG_LONG_CAP_3_CLASS_BELL (Chuông Vào Tiết) - Heal 16% Max HP all allies, SPD +10% 1 turn

    Idempotent, Transaction-safe, Data-driven.
*/

USE [HRK];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    ---------------------------------------------------------------------------
    -- 1. Validate / Resolve reference data
    ---------------------------------------------------------------------------
    DECLARE @RarityRareId INT = (SELECT TOP 1 Id FROM dbo.HRK_Rarities WHERE UPPER(Code) = 'RARE');
    IF @RarityRareId IS NULL SET @RarityRareId = 2;

    DECLARE @FactionThucId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroFactions WHERE UPPER(Code) = 'THUC');
    IF @FactionThucId IS NULL SET @FactionThucId = 1;

    DECLARE @FactionNguyId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroFactions WHERE UPPER(Code) = 'NGUY');
    IF @FactionNguyId IS NULL SET @FactionNguyId = 2;

    DECLARE @FactionNgoId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroFactions WHERE UPPER(Code) = 'NGO');
    IF @FactionNgoId IS NULL SET @FactionNgoId = 3;

    DECLARE @FactionQuanId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroFactions WHERE UPPER(Code) = 'QUAN');
    IF @FactionQuanId IS NULL SET @FactionQuanId = 4;

    DECLARE @ClassTankerId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroClasses WHERE UPPER(Code) = 'TANKER');
    IF @ClassTankerId IS NULL SET @ClassTankerId = 1;

    DECLARE @ClassWarriorId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroClasses WHERE UPPER(Code) = 'WARRIOR');
    IF @ClassWarriorId IS NULL SET @ClassWarriorId = 2;

    DECLARE @ClassAssassinId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroClasses WHERE UPPER(Code) = 'ASSASSIN');
    IF @ClassAssassinId IS NULL SET @ClassAssassinId = 3;

    DECLARE @ClassMageId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroClasses WHERE UPPER(Code) = 'MAGE');
    IF @ClassMageId IS NULL SET @ClassMageId = 4;

    DECLARE @ClassSupportId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroClasses WHERE UPPER(Code) = 'SUPPORT');
    IF @ClassSupportId IS NULL SET @ClassSupportId = 5;

    DECLARE @AtkAttrId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE UPPER(Code) = 'ATK');
    DECLARE @DefAttrId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE UPPER(Code) = 'DEF');
    DECLARE @HpAttrId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE UPPER(Code) = 'HP');
    DECLARE @SpdAttrId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE UPPER(Code) = 'SPD');
    DECLARE @MagicDmgAttrId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE UPPER(Code) = 'MAGIC_DAMAGE');
    DECLARE @MagicResAttrId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE UPPER(Code) = 'MAGIC_RESISTANCE');

    IF @AtkAttrId IS NULL THROW 50012, N'Lỗi: Thiếu AttributeType ATK.', 1;
    IF @DefAttrId IS NULL THROW 50013, N'Lỗi: Thiếu AttributeType DEF.', 1;
    IF @HpAttrId IS NULL SET @HpAttrId = @DefAttrId;
    IF @SpdAttrId IS NULL SET @SpdAttrId = @AtkAttrId;
    IF @MagicDmgAttrId IS NULL SET @MagicDmgAttrId = @AtkAttrId;
    IF @MagicResAttrId IS NULL SET @MagicResAttrId = @DefAttrId;

    ---------------------------------------------------------------------------
    -- 2. Ensure Skill Target Types exist
    ---------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) = 'ENEMY_RANDOM_DISTINCT_N')
    BEGIN
        DECLARE @MaxTargetOrder INT = ISNULL((SELECT MAX(DisplayOrder) FROM dbo.HRK_SkillTargetTypes), 12);
        INSERT INTO dbo.HRK_SkillTargetTypes (Code, Name, TargetSide, SelectionRule, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
        VALUES ('ENEMY_RANDOM_DISTINCT_N', N'N mục tiêu địch ngẫu nhiên không trùng', 'ENEMY', 'RANDOM_DISTINCT_N', @MaxTargetOrder + 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
        PRINT N'-> Đã thêm TargetType ENEMY_RANDOM_DISTINCT_N.';
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) = 'ALLY_LOWEST_ENERGY')
    BEGIN
        DECLARE @MaxTargetOrder2 INT = ISNULL((SELECT MAX(DisplayOrder) FROM dbo.HRK_SkillTargetTypes), 13);
        INSERT INTO dbo.HRK_SkillTargetTypes (Code, Name, TargetSide, SelectionRule, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
        VALUES ('ALLY_LOWEST_ENERGY', N'Đồng minh năng lượng thấp nhất', 'ALLY', 'LOWEST_ENERGY', @MaxTargetOrder2 + 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
        PRINT N'-> Đã thêm TargetType ALLY_LOWEST_ENERGY.';
    END;

    DECLARE @EnemySingleTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) IN ('ENEMY_SINGLE', 'SINGLE_ENEMY'));
    DECLARE @EnemyAllTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) IN ('ENEMY_ALL', 'ALL_ENEMIES'));
    DECLARE @EnemyFrontRowTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) IN ('ENEMY_FRONT_ROW', 'FRONT_ROW_ENEMY'));
    DECLARE @EnemyRandomDistinctNTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) = 'ENEMY_RANDOM_DISTINCT_N');
    DECLARE @SelfTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) = 'SELF');
    DECLARE @AllyAllTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) IN ('ALLY_ALL', 'ALL_ALLIES'));
    DECLARE @AllyLowestEnergyTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) = 'ALLY_LOWEST_ENERGY');

    ---------------------------------------------------------------------------
    -- 3. Ensure Skill Effect Types exist
    ---------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'ACTION_BAR_CHANGE')
    BEGIN
        INSERT INTO dbo.HRK_SkillEffectTypes (Code, Name, EffectGroup, IsBeneficial, IsStackable, DefaultStackLimit, Description, ImagePath, ColorHex, CreatedOn, UpdatedOn)
        VALUES ('ACTION_BAR_CHANGE', N'Thay đổi thanh hành động', 'UTILITY', 0, 0, NULL, N'Tăng hoặc giảm điểm thanh hành động / năng lượng của mục tiêu', '/assets/images/dcs-game/effects/action-bar-change.png', '#eab308', GETDATE(), SYSUTCDATETIME());
        PRINT N'-> Đã thêm EffectType ACTION_BAR_CHANGE.';
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'PANIC')
    BEGIN
        INSERT INTO dbo.HRK_SkillEffectTypes (Code, Name, EffectGroup, IsBeneficial, IsStackable, DefaultStackLimit, Description, ImagePath, ColorHex, CreatedOn, UpdatedOn)
        VALUES ('PANIC', N'Hoảng Loạn', 'CONTROL', 0, 0, 1, N'Mục tiêu hoảng sợ, không thể nhận thêm khiên', '/assets/images/dcs-game/effects/panic.png', '#ef4444', GETDATE(), SYSUTCDATETIME());
        PRINT N'-> Đã thêm EffectType PANIC.';
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'ENERGY_CHANGE')
    BEGIN
        INSERT INTO dbo.HRK_SkillEffectTypes (Code, Name, EffectGroup, IsBeneficial, IsStackable, DefaultStackLimit, Description, ImagePath, ColorHex, CreatedOn, UpdatedOn)
        VALUES ('ENERGY_CHANGE', N'Thay đổi năng lượng', 'UTILITY', 1, 0, NULL, N'Hồi phục hoặc khấu trừ năng lượng của mục tiêu', '/assets/images/dcs-game/effects/energy-change.png', '#3b82f6', GETDATE(), SYSUTCDATETIME());
        PRINT N'-> Đã thêm EffectType ENERGY_CHANGE.';
    END;

    DECLARE @DamageEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'DAMAGE');
    DECLARE @HealEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'HEAL');
    DECLARE @ShieldEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'SHIELD');
    DECLARE @StatBuffEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'STAT_BUFF');
    DECLARE @StatDebuffEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'STAT_DEBUFF');
    DECLARE @StunEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'STUN');
    DECLARE @SilenceEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'SILENCE');
    DECLARE @PanicEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'PANIC');
    DECLARE @EnergyChangeEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'ENERGY_CHANGE');
    DECLARE @ActionBarChangeEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'ACTION_BAR_CHANGE');

    ---------------------------------------------------------------------------
    -- 4. Upsert 6 Hero Templates
    ---------------------------------------------------------------------------

    -- 4.1 Trọng Chưa Nổ
    DECLARE @TrongId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE UPPER(Name) = N'TRỌNG CHƯA NỔ' OR Avatar LIKE '%trong-chua-no%');
    IF @TrongId IS NULL
    BEGIN
        INSERT INTO dbo.HRK_HeroTemplates
        (
            Name, Avatar, FactionId, ClassId, RarityId,
            BaseHp, BaseAtk, BaseDef, BaseSpd, BaseCrit, BaseCritDmg,
            BaseLifesteal, BaseAccuracy, BaseResistance, BaseMagicDamage, BaseMagicResistance, CreatedOn
        )
        VALUES
        (
            N'Trọng Chưa Nổ', '/assets/images/dcs-game/trong-chua-no-rare.png', @FactionNguyId, @ClassAssassinId, @RarityRareId,
            800, 150, 50, 125, 15.00, 150.00, 0.00, 85.00, 5.00, 30, 40, SYSUTCDATETIME()
        );
        SET @TrongId = SCOPE_IDENTITY();
        PRINT N'-> Đã tạo mới HeroTemplate Trọng Chưa Nổ.';
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_HeroTemplates
        SET Name = N'Trọng Chưa Nổ', Avatar = '/assets/images/dcs-game/trong-chua-no-rare.png',
            FactionId = @FactionNguyId, ClassId = @ClassAssassinId, RarityId = @RarityRareId,
            BaseHp = 800, BaseAtk = 150, BaseDef = 50, BaseSpd = 125, BaseCrit = 15.00, BaseCritDmg = 150.00,
            BaseLifesteal = 0.00, BaseAccuracy = 85.00, BaseResistance = 5.00, BaseMagicDamage = 30, BaseMagicResistance = 40
        WHERE Id = @TrongId;
        PRINT N'-> Đã cập nhật HeroTemplate Trọng Chưa Nổ.';
    END;

    -- 4.2 Quang Vinh THCS
    DECLARE @QuangVinhId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE UPPER(Name) = N'QUANG VINH THCS' OR Avatar LIKE '%quang-vinh-thcs%');
    IF @QuangVinhId IS NULL
    BEGIN
        INSERT INTO dbo.HRK_HeroTemplates
        (
            Name, Avatar, FactionId, ClassId, RarityId,
            BaseHp, BaseAtk, BaseDef, BaseSpd, BaseCrit, BaseCritDmg,
            BaseLifesteal, BaseAccuracy, BaseResistance, BaseMagicDamage, BaseMagicResistance, CreatedOn
        )
        VALUES
        (
            N'Quang Vinh THCS', '/assets/images/dcs-game/quang-vinh-thcs-rare.png', @FactionThucId, @ClassTankerId, @RarityRareId,
            1050, 110, 95, 90, 5.00, 150.00, 0.00, 80.00, 15.00, 30, 90, SYSUTCDATETIME()
        );
        SET @QuangVinhId = SCOPE_IDENTITY();
        PRINT N'-> Đã tạo mới HeroTemplate Quang Vinh THCS.';
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_HeroTemplates
        SET Name = N'Quang Vinh THCS', Avatar = '/assets/images/dcs-game/quang-vinh-thcs-rare.png',
            FactionId = @FactionThucId, ClassId = @ClassTankerId, RarityId = @RarityRareId,
            BaseHp = 1050, BaseAtk = 110, BaseDef = 95, BaseSpd = 90, BaseCrit = 5.00, BaseCritDmg = 150.00,
            BaseLifesteal = 0.00, BaseAccuracy = 80.00, BaseResistance = 15.00, BaseMagicDamage = 30, BaseMagicResistance = 90
        WHERE Id = @QuangVinhId;
        PRINT N'-> Đã cập nhật HeroTemplate Quang Vinh THCS.';
    END;

    -- 4.3 Nguyên Xàm Lớn
    DECLARE @NguyenId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE UPPER(Name) = N'NGUYÊN XÀM LỚN' OR Avatar LIKE '%nguyen-xam-lon%');
    IF @NguyenId IS NULL
    BEGIN
        INSERT INTO dbo.HRK_HeroTemplates
        (
            Name, Avatar, FactionId, ClassId, RarityId,
            BaseHp, BaseAtk, BaseDef, BaseSpd, BaseCrit, BaseCritDmg,
            BaseLifesteal, BaseAccuracy, BaseResistance, BaseMagicDamage, BaseMagicResistance, CreatedOn
        )
        VALUES
        (
            N'Nguyên Xàm Lớn', '/assets/images/dcs-game/nguyen-xam-lon-rare.png', @FactionQuanId, @ClassWarriorId, @RarityRareId,
            950, 155, 75, 95, 8.00, 150.00, 0.00, 80.00, 8.00, 30, 45, SYSUTCDATETIME()
        );
        SET @NguyenId = SCOPE_IDENTITY();
        PRINT N'-> Đã tạo mới HeroTemplate Nguyên Xàm Lớn.';
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_HeroTemplates
        SET Name = N'Nguyên Xàm Lớn', Avatar = '/assets/images/dcs-game/nguyen-xam-lon-rare.png',
            FactionId = @FactionQuanId, ClassId = @ClassWarriorId, RarityId = @RarityRareId,
            BaseHp = 950, BaseAtk = 155, BaseDef = 75, BaseSpd = 95, BaseCrit = 8.00, BaseCritDmg = 150.00,
            BaseLifesteal = 0.00, BaseAccuracy = 80.00, BaseResistance = 8.00, BaseMagicDamage = 30, BaseMagicResistance = 45
        WHERE Id = @NguyenId;
        PRINT N'-> Đã cập nhật HeroTemplate Nguyên Xàm Lớn.';
    END;

    -- 4.4 Tiến Dũng Xuân
    DECLARE @TienDungId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE UPPER(Name) = N'TIẾN DŨNG XUÂN' OR Avatar LIKE '%tien-dung-xuan%');
    IF @TienDungId IS NULL
    BEGIN
        INSERT INTO dbo.HRK_HeroTemplates
        (
            Name, Avatar, FactionId, ClassId, RarityId,
            BaseHp, BaseAtk, BaseDef, BaseSpd, BaseCrit, BaseCritDmg,
            BaseLifesteal, BaseAccuracy, BaseResistance, BaseMagicDamage, BaseMagicResistance, CreatedOn
        )
        VALUES
        (
            N'Tiến Dũng Xuân', '/assets/images/dcs-game/tien-dung-xuan-rare.png', @FactionNgoId, @ClassMageId, @RarityRareId,
            780, 90, 50, 105, 5.00, 150.00, 0.00, 85.00, 10.00, 210, 80, SYSUTCDATETIME()
        );
        SET @TienDungId = SCOPE_IDENTITY();
        PRINT N'-> Đã tạo mới HeroTemplate Tiến Dũng Xuân.';
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_HeroTemplates
        SET Name = N'Tiến Dũng Xuân', Avatar = '/assets/images/dcs-game/tien-dung-xuan-rare.png',
            FactionId = @FactionNgoId, ClassId = @ClassMageId, RarityId = @RarityRareId,
            BaseHp = 780, BaseAtk = 90, BaseDef = 50, BaseSpd = 105, BaseCrit = 5.00, BaseCritDmg = 150.00,
            BaseLifesteal = 0.00, BaseAccuracy = 85.00, BaseResistance = 10.00, BaseMagicDamage = 210, BaseMagicResistance = 80
        WHERE Id = @TienDungId;
        PRINT N'-> Đã cập nhật HeroTemplate Tiến Dũng Xuân.';
    END;

    -- 4.5 Văn Trọng Điện Vàng
    DECLARE @VanTrongId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE UPPER(Name) = N'VĂN TRỌNG ĐIỆN VÀNG' OR Avatar LIKE '%van-trong-dien-vang%' OR Avatar LIKE '%van-trong-pikachu%');
    IF @VanTrongId IS NULL
    BEGIN
        INSERT INTO dbo.HRK_HeroTemplates
        (
            Name, Avatar, FactionId, ClassId, RarityId,
            BaseHp, BaseAtk, BaseDef, BaseSpd, BaseCrit, BaseCritDmg,
            BaseLifesteal, BaseAccuracy, BaseResistance, BaseMagicDamage, BaseMagicResistance, CreatedOn
        )
        VALUES
        (
            N'Văn Trọng Điện Vàng', '/assets/images/dcs-game/van-trong-dien-vang-rare.png', @FactionQuanId, @ClassMageId, @RarityRareId,
            760, 85, 45, 115, 10.00, 150.00, 0.00, 80.00, 8.00, 220, 70, SYSUTCDATETIME()
        );
        SET @VanTrongId = SCOPE_IDENTITY();
        PRINT N'-> Đã tạo mới HeroTemplate Văn Trọng Điện Vàng.';
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_HeroTemplates
        SET Name = N'Văn Trọng Điện Vàng', Avatar = '/assets/images/dcs-game/van-trong-dien-vang-rare.png',
            FactionId = @FactionQuanId, ClassId = @ClassMageId, RarityId = @RarityRareId,
            BaseHp = 760, BaseAtk = 85, BaseDef = 45, BaseSpd = 115, BaseCrit = 10.00, BaseCritDmg = 150.00,
            BaseLifesteal = 0.00, BaseAccuracy = 80.00, BaseResistance = 8.00, BaseMagicDamage = 220, BaseMagicResistance = 70
        WHERE Id = @VanTrongId;
        PRINT N'-> Đã cập nhật HeroTemplate Văn Trọng Điện Vàng.';
    END;

    -- 4.6 Tường Long Cấp 3
    DECLARE @TuongLongId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE UPPER(Name) = N'TƯỜNG LONG CẤP 3' OR Avatar LIKE '%tuong-long-cap-3%');
    IF @TuongLongId IS NULL
    BEGIN
        INSERT INTO dbo.HRK_HeroTemplates
        (
            Name, Avatar, FactionId, ClassId, RarityId,
            BaseHp, BaseAtk, BaseDef, BaseSpd, BaseCrit, BaseCritDmg,
            BaseLifesteal, BaseAccuracy, BaseResistance, BaseMagicDamage, BaseMagicResistance, CreatedOn
        )
        VALUES
        (
            N'Tường Long Cấp 3', '/assets/images/dcs-game/tuong-long-cap-3-rare.png', @FactionThucId, @ClassSupportId, @RarityRareId,
            880, 100, 65, 120, 5.00, 150.00, 0.00, 80.00, 20.00, 60, 95, SYSUTCDATETIME()
        );
        SET @TuongLongId = SCOPE_IDENTITY();
        PRINT N'-> Đã tạo mới HeroTemplate Tường Long Cấp 3.';
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_HeroTemplates
        SET Name = N'Tường Long Cấp 3', Avatar = '/assets/images/dcs-game/tuong-long-cap-3-rare.png',
            FactionId = @FactionThucId, ClassId = @ClassSupportId, RarityId = @RarityRareId,
            BaseHp = 880, BaseAtk = 100, BaseDef = 65, BaseSpd = 120, BaseCrit = 5.00, BaseCritDmg = 150.00,
            BaseLifesteal = 0.00, BaseAccuracy = 80.00, BaseResistance = 20.00, BaseMagicDamage = 60, BaseMagicResistance = 95
        WHERE Id = @TuongLongId;
        PRINT N'-> Đã cập nhật HeroTemplate Tường Long Cấp 3.';
    END;

    ---------------------------------------------------------------------------
    -- 5. Upsert 12 Skill Templates
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
    ('TRONG_CHUA_NO_BASIC', N'Rút Chốt Thử Thôi', 'bi-lightning', N'Rút chốt thử một khối kích nổ công nghệ rồi lướt nhanh tới tấn công mục tiêu, gây 105% ATK Sát thương Vật lý.', '/assets/images/dcs-game/skills/trong-chua-no/timed-charge.png', 'NORMAL', 'ON_ATTACK', 0, 1),
    ('TRONG_CHUA_NO_THREE_SECONDS', N'Ba Giây Chưa Nổ', 'bi-clock-history', N'Ném khối chất nổ hẹn giờ vào chân mục tiêu rồi lùi lại kích hoạt. Gây 175% ATK Sát thương Vật lý và có 30% cơ hội khiến mục tiêu rơi vào trạng thái Hoảng Loạn trong 1 lượt.', '/assets/images/dcs-game/skills/trong-chua-no/timed-charge.png', 'ENERGY', 'MANUAL_ENERGY_FULL', 100, 2),

    ('QUANG_VINH_THCS_BASIC', N'Trực Nhật Kiên Cường', 'bi-shield-check', N'Dựng khiên học sinh húc mạnh vào mục tiêu gây 90% ATK Sát thương Vật lý. Sau đó tự tăng 10% DEF trong 1 lượt.', '/assets/images/dcs-game/skills/quang-vinh-thcs/school-barrier.png', 'NORMAL', 'ON_ATTACK', 0, 1),
    ('QUANG_VINH_THCS_HONOR_BARRIER', N'Hàng Rào Danh Dự', 'bi-shield-shaded', N'Đập mạnh tấm khiên xuống đất tạo chấn động gây 65% ATK Sát thương Vật lý lên toàn bộ kẻ địch, đồng thời tạo một lớp khiên bảo hộ bằng 10% Max HP của bản thân cho toàn bộ đồng minh trong 2 lượt.', '/assets/images/dcs-game/skills/quang-vinh-thcs/school-barrier.png', 'ENERGY', 'MANUAL_ENERGY_FULL', 100, 2),

    ('NGUYEN_XAM_LON_BASIC', N'Nói Một Là Một', 'bi-hand-index-thumb-fill', N'Tung cú đấm âm thanh uy lực gây 110% ATK Sát thương Vật lý lên mục tiêu, có 20% giảm 15% DEF của mục tiêu trong 1 lượt.', '/assets/images/dcs-game/skills/nguyen-xam-lon/boss-stamp.png', 'NORMAL', 'ON_ATTACK', 0, 1),
    ('NGUYEN_XAM_LON_BOSS_ENTERS', N'Đại Ca Xuống Sân', 'bi-tsunami', N'Dậm chân thị uy giáng dấu ấn áp chế lên toàn bộ hàng trước kẻ địch gây 105% ATK Sát thương Vật lý mỗi mục tiêu, đồng thời tự tăng 10% ATK trong 2 lượt.', '/assets/images/dcs-game/skills/nguyen-xam-lon/boss-stamp.png', 'ENERGY', 'MANUAL_ENERGY_FULL', 100, 2),

    ('TIEN_DUNG_XUAN_BASIC', N'Khai Bút Đầu Xuân', 'bi-brush', N'Vung bút vẽ nét mực vàng đen hóa thành phiến giấy phép thuật gây 100% Magic Damage Sát thương Phép lên mục tiêu, có 20% khiến mục tiêu bị Câm Lặng trong 1 lượt.', '/assets/images/dcs-game/skills/tien-dung-xuan/spring-calligraphy-seal.png', 'NORMAL', 'ON_ATTACK', 0, 1),
    ('TIEN_DUNG_XUAN_TEN_THOUSAND_GLYPHS', N'Vạn Tự Khai Hoa', 'bi-flower1', N'Vẽ vòng trận thư pháp tỏa ra muôn ấn phép mùa xuân giáng xuống toàn bộ kẻ địch gây 70% Magic Damage Sát thương Phép. Mỗi mục tiêu có 30% bị giảm 15% Kháng Phép trong 2 lượt.', '/assets/images/dcs-game/skills/tien-dung-xuan/spring-calligraphy-seal.png', 'ENERGY', 'MANUAL_ENERGY_FULL', 100, 2),

    ('VAN_TRONG_DIEN_VANG_BASIC', N'Tĩnh Điện Má Hồng', 'bi-lightning-charge', N'Phóng tia điện vàng từ linh cầu sấm sét gây 95% Magic Damage Sát thương Phép lên mục tiêu, có 25% làm lùi 10 điểm thanh hành động của mục tiêu.', '/assets/images/dcs-game/skills/van-trong-dien-vang/electric-mascot-orb.png', 'NORMAL', 'ON_ATTACK', 0, 1),
    ('VAN_TRONG_DIEN_VANG_CHAIN_LIGHTNING', N'Điện Vàng Liên Hoàn', 'bi-lightning-charge-fill', N'Khai hỏa luồng điện vàng liên hoàn giật qua tối đa 3 kẻ địch khác nhau, gây 85% Magic Damage Sát thương Phép mỗi mục tiêu. Mỗi mục tiêu trúng đòn có 30% độc lập bị Choáng trong 1 lượt.', '/assets/images/dcs-game/skills/van-trong-dien-vang/electric-mascot-orb.png', 'ENERGY', 'MANUAL_ENERGY_FULL', 100, 2),

    ('TUONG_LONG_CAP_3_BASIC', N'Giao Bài Tận Nơi', 'bi-envelope-paper-heart-fill', N'Ném phiến thẻ tiếp tế gây 90% ATK Sát thương Vật lý lên mục tiêu. Sau đó phiến thẻ hóa thành ánh sáng hồi 10 Năng lượng cho đồng minh còn sống có năng lượng thấp nhất.', '/assets/images/dcs-game/skills/tuong-long-cap-3/school-bell.png', 'NORMAL', 'ON_ATTACK', 0, 1),
    ('TUONG_LONG_CAP_3_CLASS_BELL', N'Chuông Vào Tiết', 'bi-bell-fill', N'Rung hồi chuông đồng học đường tiếp thêm sĩ khí, hồi máu cho toàn bộ đồng minh bằng 16% Max HP của bản thân và tăng 10% SPD cho toàn đội trong 1 lượt.', '/assets/images/dcs-game/skills/tuong-long-cap-3/school-bell.png', 'ENERGY', 'MANUAL_ENERGY_FULL', 100, 2);

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

    PRINT N'-> Đã upsert 12 Skill Templates.';

    ---------------------------------------------------------------------------
    -- 6. Link Skills to Heroes in HRK_HeroSkills
    ---------------------------------------------------------------------------
    DELETE FROM dbo.HRK_HeroSkills WHERE HeroTemplateId IN (@TrongId, @QuangVinhId, @NguyenId, @TienDungId, @VanTrongId, @TuongLongId);

    INSERT INTO dbo.HRK_HeroSkills (HeroTemplateId, SkillId, SkillOrder) VALUES
        (@TrongId, 'TRONG_CHUA_NO_BASIC', 1),
        (@TrongId, 'TRONG_CHUA_NO_THREE_SECONDS', 2),

        (@QuangVinhId, 'QUANG_VINH_THCS_BASIC', 1),
        (@QuangVinhId, 'QUANG_VINH_THCS_HONOR_BARRIER', 2),

        (@NguyenId, 'NGUYEN_XAM_LON_BASIC', 1),
        (@NguyenId, 'NGUYEN_XAM_LON_BOSS_ENTERS', 2),

        (@TienDungId, 'TIEN_DUNG_XUAN_BASIC', 1),
        (@TienDungId, 'TIEN_DUNG_XUAN_TEN_THOUSAND_GLYPHS', 2),

        (@VanTrongId, 'VAN_TRONG_DIEN_VANG_BASIC', 1),
        (@VanTrongId, 'VAN_TRONG_DIEN_VANG_CHAIN_LIGHTNING', 2),

        (@TuongLongId, 'TUONG_LONG_CAP_3_BASIC', 1),
        (@TuongLongId, 'TUONG_LONG_CAP_3_CLASS_BELL', 2);

    PRINT N'-> Đã gán đúng 2 kỹ năng cho từng hero trong 6 hero mới.';

    ---------------------------------------------------------------------------
    -- 7. Clean and reseed Skill Effects, Scalings, Parameters, Stat Modifiers
    ---------------------------------------------------------------------------
    DECLARE @SixHeroSkillIds TABLE (SkillId NVARCHAR(50));
    INSERT INTO @SixHeroSkillIds VALUES
        ('TRONG_CHUA_NO_BASIC'), ('TRONG_CHUA_NO_THREE_SECONDS'),
        ('QUANG_VINH_THCS_BASIC'), ('QUANG_VINH_THCS_HONOR_BARRIER'),
        ('NGUYEN_XAM_LON_BASIC'), ('NGUYEN_XAM_LON_BOSS_ENTERS'),
        ('TIEN_DUNG_XUAN_BASIC'), ('TIEN_DUNG_XUAN_TEN_THOUSAND_GLYPHS'),
        ('VAN_TRONG_DIEN_VANG_BASIC'), ('VAN_TRONG_DIEN_VANG_CHAIN_LIGHTNING'),
        ('TUONG_LONG_CAP_3_BASIC'), ('TUONG_LONG_CAP_3_CLASS_BELL');

    DELETE p FROM dbo.HRK_SkillEffectParameters p
    INNER JOIN dbo.HRK_SkillEffects se ON se.Id = p.SkillEffectId
    WHERE se.SkillId IN (SELECT SkillId FROM @SixHeroSkillIds);

    DELETE sc FROM dbo.HRK_SkillEffectScalings sc
    INNER JOIN dbo.HRK_SkillEffects se ON se.Id = sc.SkillEffectId
    WHERE se.SkillId IN (SELECT SkillId FROM @SixHeroSkillIds);

    DELETE sm FROM dbo.HRK_SkillEffectStatModifiers sm
    INNER JOIN dbo.HRK_SkillEffects se ON se.Id = sm.SkillEffectId
    WHERE se.SkillId IN (SELECT SkillId FROM @SixHeroSkillIds);

    DELETE FROM dbo.HRK_SkillEffects
    WHERE SkillId IN (SELECT SkillId FROM @SixHeroSkillIds);

    -- Helper variables for created effect IDs
    DECLARE @EffId BIGINT;

    ---------------------------------------------------------------------------
    -- 7.1 TRONG_CHUA_NO_BASIC
    ---------------------------------------------------------------------------
    -- Effect 1: Physical Damage 105% ATK, CanCrit = 1
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('TRONG_CHUA_NO_BASIC', @DamageEffectTypeId, @EnemySingleTargetId, 'PHYSICAL', 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @AtkAttrId, 1.050000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'CAN_CRIT', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    ---------------------------------------------------------------------------
    -- 7.2 TRONG_CHUA_NO_THREE_SECONDS
    ---------------------------------------------------------------------------
    -- Effect 1: Physical Damage 175% ATK, CanCrit = 1
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('TRONG_CHUA_NO_THREE_SECONDS', @DamageEffectTypeId, @EnemySingleTargetId, 'PHYSICAL', 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @AtkAttrId, 1.750000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'CAN_CRIT', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 2: PANIC 30%, 1 turn
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('TRONG_CHUA_NO_THREE_SECONDS', @PanicEffectTypeId, @EnemySingleTargetId, NULL, 0, 1, 30.00, 1, 2, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'CAN_CRIT', 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    ---------------------------------------------------------------------------
    -- 7.3 QUANG_VINH_THCS_BASIC
    ---------------------------------------------------------------------------
    -- Effect 1: Physical Damage 90% ATK, CanCrit = 1
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('QUANG_VINH_THCS_BASIC', @DamageEffectTypeId, @EnemySingleTargetId, 'PHYSICAL', 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @AtkAttrId, 0.900000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'CAN_CRIT', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 2: STAT_BUFF SELF DEF +10%, 1 turn
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('QUANG_VINH_THCS_BASIC', @StatBuffEffectTypeId, @SelfTargetId, NULL, 10, 1, 100.00, 1, 2, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectStatModifiers (SkillEffectId, AttributeTypeId, ValueType, Value, CreatedOn, UpdatedOn)
    VALUES (@EffId, @DefAttrId, 'PERCENT', 10.000000, SYSUTCDATETIME(), SYSUTCDATETIME());

    ---------------------------------------------------------------------------
    -- 7.4 QUANG_VINH_THCS_HONOR_BARRIER
    ---------------------------------------------------------------------------
    -- Effect 1: Physical Damage 65% ATK, ENEMY_ALL, CanCrit = 0
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('QUANG_VINH_THCS_HONOR_BARRIER', @DamageEffectTypeId, @EnemyAllTargetId, 'PHYSICAL', 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @AtkAttrId, 0.650000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'CAN_CRIT', 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 2: SHIELD ALLY_ALL, 10% Max HP caster, 2 turns
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('QUANG_VINH_THCS_HONOR_BARRIER', @ShieldEffectTypeId, @AllyAllTargetId, NULL, 0, 2, 100.00, 1, 2, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @HpAttrId, 0.100000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'CAN_CRIT', 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    ---------------------------------------------------------------------------
    -- 7.5 NGUYEN_XAM_LON_BASIC
    ---------------------------------------------------------------------------
    -- Effect 1: Physical Damage 110% ATK, CanCrit = 1
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('NGUYEN_XAM_LON_BASIC', @DamageEffectTypeId, @EnemySingleTargetId, 'PHYSICAL', 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @AtkAttrId, 1.100000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'CAN_CRIT', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 2: STAT_DEBUFF DEF -15%, 20% chance, 1 turn
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('NGUYEN_XAM_LON_BASIC', @StatDebuffEffectTypeId, @EnemySingleTargetId, NULL, -15, 1, 20.00, 1, 2, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectStatModifiers (SkillEffectId, AttributeTypeId, ValueType, Value, CreatedOn, UpdatedOn)
    VALUES (@EffId, @DefAttrId, 'PERCENT', -15.000000, SYSUTCDATETIME(), SYSUTCDATETIME());

    ---------------------------------------------------------------------------
    -- 7.6 NGUYEN_XAM_LON_BOSS_ENTERS
    ---------------------------------------------------------------------------
    -- Effect 1: Physical Damage 105% ATK, ENEMY_FRONT_ROW, CanCrit = 1
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('NGUYEN_XAM_LON_BOSS_ENTERS', @DamageEffectTypeId, @EnemyFrontRowTargetId, 'PHYSICAL', 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @AtkAttrId, 1.050000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'CAN_CRIT', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 2: STAT_BUFF SELF ATK +10%, 2 turns
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('NGUYEN_XAM_LON_BOSS_ENTERS', @StatBuffEffectTypeId, @SelfTargetId, NULL, 10, 2, 100.00, 1, 2, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectStatModifiers (SkillEffectId, AttributeTypeId, ValueType, Value, CreatedOn, UpdatedOn)
    VALUES (@EffId, @AtkAttrId, 'PERCENT', 10.000000, SYSUTCDATETIME(), SYSUTCDATETIME());

    ---------------------------------------------------------------------------
    -- 7.7 TIEN_DUNG_XUAN_BASIC
    ---------------------------------------------------------------------------
    -- Effect 1: Magic Damage 100% MagicDamage, CanCrit = 1
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('TIEN_DUNG_XUAN_BASIC', @DamageEffectTypeId, @EnemySingleTargetId, 'MAGIC', 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @MagicDmgAttrId, 1.000000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'CAN_CRIT', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 2: SILENCE 20%, 1 turn
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('TIEN_DUNG_XUAN_BASIC', @SilenceEffectTypeId, @EnemySingleTargetId, NULL, 0, 1, 20.00, 1, 2, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'CAN_CRIT', 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    ---------------------------------------------------------------------------
    -- 7.8 TIEN_DUNG_XUAN_TEN_THOUSAND_GLYPHS
    ---------------------------------------------------------------------------
    -- Effect 1: Magic Damage 70% MagicDamage, ENEMY_ALL, CanCrit = 0
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('TIEN_DUNG_XUAN_TEN_THOUSAND_GLYPHS', @DamageEffectTypeId, @EnemyAllTargetId, 'MAGIC', 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @MagicDmgAttrId, 0.700000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'CAN_CRIT', 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 2: STAT_DEBUFF MAGIC_RESISTANCE -15%, ENEMY_ALL, 30% chance per target, 2 turns
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('TIEN_DUNG_XUAN_TEN_THOUSAND_GLYPHS', @StatDebuffEffectTypeId, @EnemyAllTargetId, NULL, -15, 2, 30.00, 1, 2, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectStatModifiers (SkillEffectId, AttributeTypeId, ValueType, Value, CreatedOn, UpdatedOn)
    VALUES (@EffId, @MagicResAttrId, 'PERCENT', -15.000000, SYSUTCDATETIME(), SYSUTCDATETIME());

    ---------------------------------------------------------------------------
    -- 7.9 VAN_TRONG_DIEN_VANG_BASIC
    ---------------------------------------------------------------------------
    -- Effect 1: Magic Damage 95% MagicDamage, CanCrit = 1
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('VAN_TRONG_DIEN_VANG_BASIC', @DamageEffectTypeId, @EnemySingleTargetId, 'MAGIC', 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @MagicDmgAttrId, 0.950000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'CAN_CRIT', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 2: ACTION_BAR_CHANGE -10 points, 25% chance
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('VAN_TRONG_DIEN_VANG_BASIC', @ActionBarChangeEffectTypeId, @EnemySingleTargetId, NULL, -10, NULL, 25.00, 1, 2, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, IntValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'ACTION_BAR_DELTA', -10, SYSUTCDATETIME(), SYSUTCDATETIME());

    ---------------------------------------------------------------------------
    -- 7.10 VAN_TRONG_DIEN_VANG_CHAIN_LIGHTNING
    ---------------------------------------------------------------------------
    -- Effect 1: Magic Damage 85% MagicDamage, ENEMY_RANDOM_DISTINCT_N (N=3), CanCrit = 0
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('VAN_TRONG_DIEN_VANG_CHAIN_LIGHTNING', @DamageEffectTypeId, @EnemyRandomDistinctNTargetId, 'MAGIC', 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @MagicDmgAttrId, 0.850000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, IntValue, BoolValue, CreatedOn, UpdatedOn)
    VALUES
        (@EffId, 'TARGET_COUNT', 3, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'CAN_CRIT', NULL, 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 2: STUN 30% per target, 1 turn
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('VAN_TRONG_DIEN_VANG_CHAIN_LIGHTNING', @StunEffectTypeId, @EnemyRandomDistinctNTargetId, NULL, 0, 1, 30.00, 1, 2, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, IntValue, BoolValue, CreatedOn, UpdatedOn)
    VALUES
        (@EffId, 'TARGET_COUNT', 3, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'CAN_CRIT', NULL, 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    ---------------------------------------------------------------------------
    -- 7.11 TUONG_LONG_CAP_3_BASIC
    ---------------------------------------------------------------------------
    -- Effect 1: Physical Damage 90% ATK, CanCrit = 1
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('TUONG_LONG_CAP_3_BASIC', @DamageEffectTypeId, @EnemySingleTargetId, 'PHYSICAL', 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @AtkAttrId, 0.900000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'CAN_CRIT', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 2: ENERGY_CHANGE +10 Energy, ALLY_LOWEST_ENERGY
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('TUONG_LONG_CAP_3_BASIC', @EnergyChangeEffectTypeId, @AllyLowestEnergyTargetId, NULL, 10, NULL, 100.00, 1, 2, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, IntValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'ENERGY_GAIN', 10, SYSUTCDATETIME(), SYSUTCDATETIME());

    ---------------------------------------------------------------------------
    -- 7.12 TUONG_LONG_CAP_3_CLASS_BELL
    ---------------------------------------------------------------------------
    -- Effect 1: HEAL ALLY_ALL, 16% Max HP caster
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('TUONG_LONG_CAP_3_CLASS_BELL', @HealEffectTypeId, @AllyAllTargetId, NULL, 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @HpAttrId, 0.160000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'CAN_CRIT', 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 2: STAT_BUFF ALLY_ALL SPD +10%, 1 turn
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('TUONG_LONG_CAP_3_CLASS_BELL', @StatBuffEffectTypeId, @AllyAllTargetId, NULL, 10, 1, 100.00, 1, 2, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectStatModifiers (SkillEffectId, AttributeTypeId, ValueType, Value, CreatedOn, UpdatedOn)
    VALUES (@EffId, @SpdAttrId, 'PERCENT', 10.000000, SYSUTCDATETIME(), SYSUTCDATETIME());

    PRINT N'-> Đã seed toàn bộ SkillEffects, Scalings, Parameters, StatModifiers cho 12 kỹ năng.';

    ---------------------------------------------------------------------------
    -- 8. Grant heroes to Player 1 (If exists)
    ---------------------------------------------------------------------------
    IF EXISTS (SELECT 1 FROM dbo.HRK_Players WHERE Id = 1)
    BEGIN
        DECLARE @NewHeroTemplateIds TABLE (TemplateId INT);
        INSERT INTO @NewHeroTemplateIds VALUES
            (@TrongId), (@QuangVinhId), (@NguyenId), (@TienDungId), (@VanTrongId), (@TuongLongId);

        INSERT INTO dbo.HRK_PlayerHeroes
        (
            PlayerId, HeroTemplateId, Level, Exp, MaxExp, Stars, Power, AuraTier,
            IsLocked, IsFavorite, IsActive, CurrentStats, CreatedOn, UpdatedOn
        )
        SELECT
            1,
            t.TemplateId,
            1 AS Level,
            0 AS Exp,
            500 AS MaxExp,
            1 AS Stars,
            (ht.BaseHp / 10) + (ht.BaseAtk * 3) + (ht.BaseDef * 2) + ht.BaseSpd AS Power,
            1 AS AuraTier,
            0 AS IsLocked,
            0 AS IsFavorite,
            1 AS IsActive,
            NULL AS CurrentStats,
            SYSUTCDATETIME(),
            SYSUTCDATETIME()
        FROM @NewHeroTemplateIds t
        INNER JOIN dbo.HRK_HeroTemplates ht ON ht.Id = t.TemplateId
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.HRK_PlayerHeroes ph
            WHERE ph.PlayerId = 1 AND ph.HeroTemplateId = t.TemplateId
        );

        PRINT N'-> Đã cấp 6 hero Rare mới cho PlayerId = 1.';
    END;

    COMMIT TRANSACTION;
    PRINT N'=== [HOÀN TẤT THÀNH CÔNG] Migration_AddSixRareHeroes ===';
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

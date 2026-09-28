-- ============================================================================
-- Migration: Migration_AddFiveEpicHeroes.sql
-- Mục đích: Thêm 5 Hero Epic mới và 10 Skill Templates hoàn chỉnh
-- Ngày tạo: 2026-09-29
-- Danh sách 5 Hero Epic:
--   1. Kiệt Bác Sĩ (kiet-bac-si) - Support / Thục (Magic/Heal)
--   2. Trường Kiệt Chu Mỏ (truong-kiet-chu-mo) - Assassin / Ngụy (Physical)
--   3. Tiến Dũng Tổng Đài (tien-dung-tong-dai) - Mage / Ngô (Magic)
--   4. Quốc Nhân Giả Diện (quoc-nhan-gia-dien) - Assassin / Ngụy (Physical)
--   5. Cậu Vàng Mặt Lạnh (cau-vang-mat-lanh) - Tanker / Quần (Physical)
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    PRINT N'=== BẮT ĐẦU MIGRATION 5 HERO EPIC (29/09/2026) ===';

    ---------------------------------------------------------------------------
    -- 1. Dynamic Resolution: Rarities, Classes, Factions, Attributes
    ---------------------------------------------------------------------------
    DECLARE @RarityEpicId INT = (SELECT TOP 1 Id FROM dbo.HRK_Rarities WHERE UPPER(Code) = 'EPIC');
    IF @RarityEpicId IS NULL SET @RarityEpicId = 3;

    DECLARE @FactionNguyId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroFactions WHERE UPPER(Code) = 'NGUY');
    IF @FactionNguyId IS NULL SET @FactionNguyId = 2;

    DECLARE @FactionThucId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroFactions WHERE UPPER(Code) = 'THUC');
    IF @FactionThucId IS NULL SET @FactionThucId = 1;

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
    DECLARE @CritAttrId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE UPPER(Code) IN ('CRIT', 'CRIT_CHANCE', 'CRIT_RATE'));
    DECLARE @AccuracyAttrId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE UPPER(Code) IN ('ACCURACY', 'ACC'));
    DECLARE @ResistanceAttrId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE UPPER(Code) IN ('RESISTANCE', 'RES'));
    DECLARE @MagicDmgAttrId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE UPPER(Code) = 'MAGIC_DAMAGE');
    DECLARE @MagicResAttrId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE UPPER(Code) = 'MAGIC_RESISTANCE');

    IF @AtkAttrId IS NULL THROW 50012, N'Lỗi: Thiếu AttributeType ATK.', 1;
    IF @DefAttrId IS NULL THROW 50013, N'Lỗi: Thiếu AttributeType DEF.', 1;
    IF @HpAttrId IS NULL SET @HpAttrId = @DefAttrId;
    IF @SpdAttrId IS NULL SET @SpdAttrId = @AtkAttrId;
    IF @CritAttrId IS NULL SET @CritAttrId = @AtkAttrId;
    IF @AccuracyAttrId IS NULL SET @AccuracyAttrId = @AtkAttrId;
    IF @ResistanceAttrId IS NULL SET @ResistanceAttrId = @DefAttrId;
    IF @MagicDmgAttrId IS NULL SET @MagicDmgAttrId = @AtkAttrId;
    IF @MagicResAttrId IS NULL SET @MagicResAttrId = @DefAttrId;

    ---------------------------------------------------------------------------
    -- 2. Target Types Resolution
    ---------------------------------------------------------------------------
    DECLARE @SelfTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) = 'SELF');
    DECLARE @AllyAllTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) IN ('ALLY_ALL', 'ALL_ALLIES'));
    DECLARE @LowestHpPercentTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) = 'LOWEST_HP_PERCENT');
    DECLARE @EnemySingleTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) IN ('ENEMY_SINGLE', 'SINGLE_ENEMY'));
    DECLARE @EnemyAllTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) IN ('ENEMY_ALL', 'ALL_ENEMIES'));
    DECLARE @EnemyBackRowTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) IN ('ENEMY_BACK_ROW', 'BACK_ROW_ENEMY'));

    IF @SelfTargetId IS NULL THROW 50014, N'Lỗi: Thiếu TargetType SELF.', 1;
    IF @AllyAllTargetId IS NULL THROW 50015, N'Lỗi: Thiếu TargetType ALLY_ALL.', 1;
    IF @LowestHpPercentTargetId IS NULL THROW 50016, N'Lỗi: Thiếu TargetType LOWEST_HP_PERCENT.', 1;
    IF @EnemySingleTargetId IS NULL THROW 50017, N'Lỗi: Thiếu TargetType ENEMY_SINGLE.', 1;
    IF @EnemyAllTargetId IS NULL THROW 50018, N'Lỗi: Thiếu TargetType ENEMY_ALL.', 1;
    IF @EnemyBackRowTargetId IS NULL THROW 50019, N'Lỗi: Thiếu TargetType ENEMY_BACK_ROW.', 1;

    ---------------------------------------------------------------------------
    -- 3. Effect Types Resolution
    ---------------------------------------------------------------------------
    DECLARE @DamageEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'DAMAGE');
    DECLARE @HealEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'HEAL');
    DECLARE @ShieldEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'SHIELD');
    DECLARE @StatBuffEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'STAT_BUFF');
    DECLARE @StatDebuffEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'STAT_DEBUFF');
    DECLARE @PanicEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'PANIC');
    DECLARE @SilenceEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'SILENCE');
    DECLARE @BleedEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'BLEED');
    DECLARE @DamageReductionEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'DAMAGE_REDUCTION');
    DECLARE @ActionBarChangeEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'ACTION_BAR_CHANGE');

    IF @DamageEffectTypeId IS NULL THROW 50020, N'Lỗi: Thiếu EffectType DAMAGE.', 1;
    IF @HealEffectTypeId IS NULL THROW 50021, N'Lỗi: Thiếu EffectType HEAL.', 1;
    IF @ShieldEffectTypeId IS NULL THROW 50022, N'Lỗi: Thiếu EffectType SHIELD.', 1;
    IF @StatBuffEffectTypeId IS NULL THROW 50023, N'Lỗi: Thiếu EffectType STAT_BUFF.', 1;
    IF @StatDebuffEffectTypeId IS NULL THROW 50024, N'Lỗi: Thiếu EffectType STAT_DEBUFF.', 1;
    IF @PanicEffectTypeId IS NULL THROW 50025, N'Lỗi: Thiếu EffectType PANIC.', 1;
    IF @SilenceEffectTypeId IS NULL THROW 50026, N'Lỗi: Thiếu EffectType SILENCE.', 1;
    IF @BleedEffectTypeId IS NULL THROW 50027, N'Lỗi: Thiếu EffectType BLEED.', 1;
    IF @DamageReductionEffectTypeId IS NULL THROW 50028, N'Lỗi: Thiếu EffectType DAMAGE_REDUCTION.', 1;
    IF @ActionBarChangeEffectTypeId IS NULL THROW 50029, N'Lỗi: Thiếu EffectType ACTION_BAR_CHANGE.', 1;

    ---------------------------------------------------------------------------
    -- 4. Upsert 5 Epic Hero Templates
    ---------------------------------------------------------------------------

    -- 4.1 Kiệt Bác Sĩ (kiet-bac-si)
    DECLARE @KietBacSiId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE UPPER(Name) = N'KIỆT BÁC SĨ' OR Avatar LIKE '%kiet-bac-si%');
    IF @KietBacSiId IS NULL
    BEGIN
        INSERT INTO dbo.HRK_HeroTemplates
        (
            Name, Avatar, FactionId, ClassId, RarityId,
            BaseHp, BaseAtk, BaseDef, BaseSpd, BaseCrit, BaseCritDmg,
            BaseLifesteal, BaseAccuracy, BaseResistance, BaseMagicDamage, BaseMagicResistance, CreatedOn
        )
        VALUES
        (
            N'Kiệt Bác Sĩ', '/assets/images/dcs-game/kiet-bac-si.png', @FactionThucId, @ClassSupportId, @RarityEpicId,
            960, 95, 68, 114, 5.00, 150.00, 0.00, 85.00, 25.00, 180, 120, SYSUTCDATETIME()
        );
        SET @KietBacSiId = SCOPE_IDENTITY();
        PRINT N'-> Đã tạo mới HeroTemplate Kiệt Bác Sĩ.';
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_HeroTemplates
        SET Name = N'Kiệt Bác Sĩ', 
            Avatar = CASE WHEN Avatar LIKE '%-epic.png' THEN Avatar ELSE '/assets/images/dcs-game/kiet-bac-si.png' END,
            FactionId = @FactionThucId, ClassId = @ClassSupportId, RarityId = @RarityEpicId,
            BaseHp = 960, BaseAtk = 95, BaseDef = 68, BaseSpd = 114, BaseCrit = 5.00, BaseCritDmg = 150.00,
            BaseLifesteal = 0.00, BaseAccuracy = 85.00, BaseResistance = 25.00, BaseMagicDamage = 180, BaseMagicResistance = 120
        WHERE Id = @KietBacSiId;
        PRINT N'-> Đã cập nhật HeroTemplate Kiệt Bác Sĩ.';
    END;

    -- 4.2 Trường Kiệt Chu Mỏ (truong-kiet-chu-mo)
    DECLARE @TruongKietId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE UPPER(Name) = N'TRƯỜNG KIỆT CHU MỎ' OR Avatar LIKE '%truong-kiet-chu-mo%');
    IF @TruongKietId IS NULL
    BEGIN
        INSERT INTO dbo.HRK_HeroTemplates
        (
            Name, Avatar, FactionId, ClassId, RarityId,
            BaseHp, BaseAtk, BaseDef, BaseSpd, BaseCrit, BaseCritDmg,
            BaseLifesteal, BaseAccuracy, BaseResistance, BaseMagicDamage, BaseMagicResistance, CreatedOn
        )
        VALUES
        (
            N'Trường Kiệt Chu Mỏ', '/assets/images/dcs-game/truong-kiet-chu-mo.png', @FactionNguyId, @ClassAssassinId, @RarityEpicId,
            860, 168, 58, 128, 18.00, 150.00, 0.00, 90.00, 6.00, 30, 50, SYSUTCDATETIME()
        );
        SET @TruongKietId = SCOPE_IDENTITY();
        PRINT N'-> Đã tạo mới HeroTemplate Trường Kiệt Chu Mỏ.';
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_HeroTemplates
        SET Name = N'Trường Kiệt Chu Mỏ', 
            Avatar = CASE WHEN Avatar LIKE '%-epic.png' THEN Avatar ELSE '/assets/images/dcs-game/truong-kiet-chu-mo.png' END,
            FactionId = @FactionNguyId, ClassId = @ClassAssassinId, RarityId = @RarityEpicId,
            BaseHp = 860, BaseAtk = 168, BaseDef = 58, BaseSpd = 128, BaseCrit = 18.00, BaseCritDmg = 150.00,
            BaseLifesteal = 0.00, BaseAccuracy = 90.00, BaseResistance = 6.00, BaseMagicDamage = 30, BaseMagicResistance = 50
        WHERE Id = @TruongKietId;
        PRINT N'-> Đã cập nhật HeroTemplate Trường Kiệt Chu Mỏ.';
    END;

    -- 4.3 Tiến Dũng Tổng Đài (tien-dung-tong-dai)
    DECLARE @TienDungTongDaiId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE UPPER(Name) = N'TIẾN DŨNG TỔNG ĐÀI' OR Avatar LIKE '%tien-dung-call-video%' OR Avatar LIKE '%tien-dung-tong-dai%');
    IF @TienDungTongDaiId IS NULL
    BEGIN
        INSERT INTO dbo.HRK_HeroTemplates
        (
            Name, Avatar, FactionId, ClassId, RarityId,
            BaseHp, BaseAtk, BaseDef, BaseSpd, BaseCrit, BaseCritDmg,
            BaseLifesteal, BaseAccuracy, BaseResistance, BaseMagicDamage, BaseMagicResistance, CreatedOn
        )
        VALUES
        (
            N'Tiến Dũng Tổng Đài', '/assets/images/dcs-game/tien-dung-call-video.png', @FactionNgoId, @ClassMageId, @RarityEpicId,
            820, 80, 52, 122, 8.00, 150.00, 0.00, 88.00, 12.00, 260, 90, SYSUTCDATETIME()
        );
        SET @TienDungTongDaiId = SCOPE_IDENTITY();
        PRINT N'-> Đã tạo mới HeroTemplate Tiến Dũng Tổng Đài.';
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_HeroTemplates
        SET Name = N'Tiến Dũng Tổng Đài', 
            Avatar = CASE WHEN Avatar LIKE '%-epic.png' THEN Avatar ELSE '/assets/images/dcs-game/tien-dung-call-video.png' END,
            FactionId = @FactionNgoId, ClassId = @ClassMageId, RarityId = @RarityEpicId,
            BaseHp = 820, BaseAtk = 80, BaseDef = 52, BaseSpd = 122, BaseCrit = 8.00, BaseCritDmg = 150.00,
            BaseLifesteal = 0.00, BaseAccuracy = 88.00, BaseResistance = 12.00, BaseMagicDamage = 260, BaseMagicResistance = 90
        WHERE Id = @TienDungTongDaiId;
        PRINT N'-> Đã cập nhật HeroTemplate Tiến Dũng Tổng Đài.';
    END;

    -- 4.4 Quốc Nhân Giả Diện (quoc-nhan-gia-dien)
    DECLARE @QuocNhanId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE UPPER(Name) = N'QUỐC NHÂN GIẢ DIỆN' OR Avatar LIKE '%quoc-nhan-fake%' OR Avatar LIKE '%quoc-nhan-gia-dien%');
    IF @QuocNhanId IS NULL
    BEGIN
        INSERT INTO dbo.HRK_HeroTemplates
        (
            Name, Avatar, FactionId, ClassId, RarityId,
            BaseHp, BaseAtk, BaseDef, BaseSpd, BaseCrit, BaseCritDmg,
            BaseLifesteal, BaseAccuracy, BaseResistance, BaseMagicDamage, BaseMagicResistance, CreatedOn
        )
        VALUES
        (
            N'Quốc Nhân Giả Diện', '/assets/images/dcs-game/quoc-nhan-fake.png', @FactionNguyId, @ClassAssassinId, @RarityEpicId,
            890, 165, 60, 126, 16.00, 150.00, 0.00, 86.00, 8.00, 30, 48, SYSUTCDATETIME()
        );
        SET @QuocNhanId = SCOPE_IDENTITY();
        PRINT N'-> Đã tạo mới HeroTemplate Quốc Nhân Giả Diện.';
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_HeroTemplates
        SET Name = N'Quốc Nhân Giả Diện', 
            Avatar = CASE WHEN Avatar LIKE '%-epic.png' THEN Avatar ELSE '/assets/images/dcs-game/quoc-nhan-fake.png' END,
            FactionId = @FactionNguyId, ClassId = @ClassAssassinId, RarityId = @RarityEpicId,
            BaseHp = 890, BaseAtk = 165, BaseDef = 60, BaseSpd = 126, BaseCrit = 16.00, BaseCritDmg = 150.00,
            BaseLifesteal = 0.00, BaseAccuracy = 86.00, BaseResistance = 8.00, BaseMagicDamage = 30, BaseMagicResistance = 48
        WHERE Id = @QuocNhanId;
        PRINT N'-> Đã cập nhật HeroTemplate Quốc Nhân Giả Diện.';
    END;

    -- 4.5 Cậu Vàng Mặt Lạnh (cau-vang-mat-lanh)
    DECLARE @CauVangId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE UPPER(Name) = N'CẬU VÀNG MẶT LẠNH' OR Avatar LIKE '%meme-cho-hai-huoc%' OR Avatar LIKE '%cau-vang-mat-lanh%');
    IF @CauVangId IS NULL
    BEGIN
        INSERT INTO dbo.HRK_HeroTemplates
        (
            Name, Avatar, FactionId, ClassId, RarityId,
            BaseHp, BaseAtk, BaseDef, BaseSpd, BaseCrit, BaseCritDmg,
            BaseLifesteal, BaseAccuracy, BaseResistance, BaseMagicDamage, BaseMagicResistance, CreatedOn
        )
        VALUES
        (
            N'Cậu Vàng Mặt Lạnh', '/assets/images/dcs-game/meme-cho-hai-huoc.jpg', @FactionQuanId, @ClassTankerId, @RarityEpicId,
            1200, 105, 115, 78, 4.00, 150.00, 0.00, 75.00, 25.00, 30, 160, SYSUTCDATETIME()
        );
        SET @CauVangId = SCOPE_IDENTITY();
        PRINT N'-> Đã tạo mới HeroTemplate Cậu Vàng Mặt Lạnh.';
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_HeroTemplates
        SET Name = N'Cậu Vàng Mặt Lạnh', 
            Avatar = CASE WHEN Avatar LIKE '%-epic.png' THEN Avatar ELSE '/assets/images/dcs-game/meme-cho-hai-huoc.jpg' END,
            FactionId = @FactionQuanId, ClassId = @ClassTankerId, RarityId = @RarityEpicId,
            BaseHp = 1200, BaseAtk = 105, BaseDef = 115, BaseSpd = 78, BaseCrit = 4.00, BaseCritDmg = 150.00,
            BaseLifesteal = 0.00, BaseAccuracy = 75.00, BaseResistance = 25.00, BaseMagicDamage = 30, BaseMagicResistance = 160
        WHERE Id = @CauVangId;
        PRINT N'-> Đã cập nhật HeroTemplate Cậu Vàng Mặt Lạnh.';
    END;

    ---------------------------------------------------------------------------
    -- 5. Upsert 10 Skill Templates
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
    -- Kiệt Bác Sĩ
    ('KIET_BAC_SI_BASIC', N'Chẩn Mạch Từ Xa', 'bi-heart-pulse', N'Thả máy quét sinh lực từ xa hồi phục 14% Max HP của bản thân cho đồng minh có % HP thấp nhất, đồng thời tăng 10% Kháng hiệu ứng cho mục tiêu trong 1 lượt.', '/assets/images/dcs-game/skills/kiet-bac-si/vital-scan-orb.png', 'NORMAL', 'ON_ATTACK', 0, 1),
    ('KIET_BAC_SI_EMERGENCY_PROTOCOL', N'Phác Đồ Cấp Cứu', 'bi-hospital-fill', N'Kích hoạt phác đồ cấp cứu toàn diện, hồi phục 20% Max HP của bản thân cho tất cả đồng minh và tăng 15% Kháng hiệu ứng trong 2 lượt.', '/assets/images/dcs-game/skills/kiet-bac-si/vital-scan-orb.png', 'ENERGY', 'MANUAL_ENERGY_FULL', 100, 2),

    -- Trường Kiệt Chu Mỏ
    ('TRUONG_KIET_CHU_MO_BASIC', N'Hôn Gió Cảnh Cáo', 'bi-soundwave', N'Phóng luồng sóng âm từ nụ hôn gió gây 115% ATK Sát thương Vật lý lên mục tiêu, có 30% giảm 15% Độ Chính Xác của mục tiêu trong 1 lượt.', '/assets/images/dcs-game/skills/truong-kiet-chu-mo/resonance-kiss-seal.png', 'NORMAL', 'ON_ATTACK', 0, 1),
    ('TRUONG_KIET_SOUL_KISS', N'Nụ Hôn Đoạt Hồn', 'bi-heartbreak-fill', N'Dồn năng lượng hội tụ thành nụ hôn cộng hưởng âm ba cực đại, gây 205% ATK Sát thương Vật lý lên mục tiêu đơn và có 45% khiến mục tiêu Hoảng Loạn trong 1 lượt (không thể nhận khiên).', '/assets/images/dcs-game/skills/truong-kiet-chu-mo/resonance-kiss-seal.png', 'ENERGY', 'MANUAL_ENERGY_FULL', 100, 2),

    -- Tiến Dũng Tổng Đài
    ('TIEN_DUNG_TONG_DAI_BASIC', N'Ping Cuộc Gọi', 'bi-broadcast-pin', N'Phóng tín hiệu viễn thông dữ liệu gây 105% Magic Damage Sát thương Phép lên mục tiêu, có 25% làm Câm Lặng mục tiêu trong 1 lượt.', '/assets/images/dcs-game/skills/tien-dung-tong-dai/holo-call-panel.png', 'NORMAL', 'ON_ATTACK', 0, 1),
    ('TIEN_DUNG_EMERGENCY_CONFERENCE', N'Hội Nghị Khẩn Cấp', 'bi-telephone-fill', N'Mở bảng hội nghị viễn thông khẩn cấp, phóng chùm tia dữ liệu gây 90% Magic Damage Sát thương Phép lên toàn bộ kẻ địch. Mỗi mục tiêu độc lập có 40% bị lùi 15 điểm thanh hành động.', '/assets/images/dcs-game/skills/tien-dung-tong-dai/holo-call-panel.png', 'ENERGY', 'MANUAL_ENERGY_FULL', 100, 2),

    -- Quốc Nhân Giả Diện
    ('QUOC_NHAN_GIA_DIEN_BASIC', N'Vết Cắt Ngụy Trang', 'bi-slash-circle', N'Ẩn mình lướt chém bất ngờ gây 120% ATK Sát thương Vật lý lên mục tiêu, có 35% áp dụng Chảy Máu gây 30% ATK mỗi đầu lượt trong 2 lượt.', '/assets/images/dcs-game/skills/quoc-nhan-gia-dien/phantom-mask.png', 'NORMAL', 'ON_ATTACK', 0, 1),
    ('QUOC_NHAN_NIGHT_PHANTOMS', N'Dạ Hành Phân Ảnh', 'bi-person-lines-fill', N'Phân tách thành các dư ảnh đêm truy kích hàng sau kẻ địch, gây 125% ATK Sát thương Vật lý lên mỗi kẻ địch hàng sau. Sau đòn đánh, bản thân nhận 15% Tỷ lệ Chí mạng trong 2 lượt.', '/assets/images/dcs-game/skills/quoc-nhan-gia-dien/phantom-mask.png', 'ENERGY', 'MANUAL_ENERGY_FULL', 100, 2),

    -- Cậu Vàng Mặt Lạnh
    ('CAU_VANG_MAT_LANH_BASIC', N'Ngồi Im Phán Xét', 'bi-eye-fill', N'Điềm nhiên ngồi nhìn và rung chuông ngọc gây 100% ATK Sát thương Vật lý lên mục tiêu. Sau đòn đánh, bản thân tự tăng 15% DEF trong 2 lượt.', '/assets/images/dcs-game/skills/cau-vang-mat-lanh/jade-guard-bell.png', 'NORMAL', 'ON_ATTACK', 0, 1),
    ('CAU_VANG_CALM_GUARD', N'Bình Thản Che Chở', 'bi-shield-shaded', N'Rung chuông ngọc bảo hộ tạo dấu chân ánh sáng che chở toàn đội, cung cấp Khiên bằng 12% Max HP của bản thân trong 2 lượt và giảm 12% Sát thương phải nhận cho toàn đội trong 2 lượt.', '/assets/images/dcs-game/skills/cau-vang-mat-lanh/jade-guard-bell.png', 'ENERGY', 'MANUAL_ENERGY_FULL', 100, 2);

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

    PRINT N'-> Đã upsert 10 Skill Templates.';

    ---------------------------------------------------------------------------
    -- 6. Map Exactly 2 Skills to Each Epic Hero
    ---------------------------------------------------------------------------
    DELETE FROM dbo.HRK_HeroSkills
    WHERE HeroTemplateId IN (@KietBacSiId, @TruongKietId, @TienDungTongDaiId, @QuocNhanId, @CauVangId);

    INSERT INTO dbo.HRK_HeroSkills (HeroTemplateId, SkillId, SkillOrder)
    VALUES
        -- Kiệt Bác Sĩ
        (@KietBacSiId, 'KIET_BAC_SI_BASIC', 1),
        (@KietBacSiId, 'KIET_BAC_SI_EMERGENCY_PROTOCOL', 2),
        -- Trường Kiệt Chu Mỏ
        (@TruongKietId, 'TRUONG_KIET_CHU_MO_BASIC', 1),
        (@TruongKietId, 'TRUONG_KIET_SOUL_KISS', 2),
        -- Tiến Dũng Tổng Đài
        (@TienDungTongDaiId, 'TIEN_DUNG_TONG_DAI_BASIC', 1),
        (@TienDungTongDaiId, 'TIEN_DUNG_EMERGENCY_CONFERENCE', 2),
        -- Quốc Nhân Giả Diện
        (@QuocNhanId, 'QUOC_NHAN_GIA_DIEN_BASIC', 1),
        (@QuocNhanId, 'QUOC_NHAN_NIGHT_PHANTOMS', 2),
        -- Cậu Vàng Mặt Lạnh
        (@CauVangId, 'CAU_VANG_MAT_LANH_BASIC', 1),
        (@CauVangId, 'CAU_VANG_CALM_GUARD', 2);

    PRINT N'-> Đã gán đúng 2 kỹ năng (Slot 1: Normal, Slot 2: Energy) cho 5 hero Epic mới.';

    ---------------------------------------------------------------------------
    -- 7. Clean up and Seed Skill Effects, Scalings, Parameters, StatModifiers
    ---------------------------------------------------------------------------
    DECLARE @EpicSkillIds TABLE (SkillId NVARCHAR(50));
    INSERT INTO @EpicSkillIds VALUES
        ('KIET_BAC_SI_BASIC'), ('KIET_BAC_SI_EMERGENCY_PROTOCOL'),
        ('TRUONG_KIET_CHU_MO_BASIC'), ('TRUONG_KIET_SOUL_KISS'),
        ('TIEN_DUNG_TONG_DAI_BASIC'), ('TIEN_DUNG_EMERGENCY_CONFERENCE'),
        ('QUOC_NHAN_GIA_DIEN_BASIC'), ('QUOC_NHAN_NIGHT_PHANTOMS'),
        ('CAU_VANG_MAT_LANH_BASIC'), ('CAU_VANG_CALM_GUARD');

    DELETE p FROM dbo.HRK_SkillEffectParameters p
    INNER JOIN dbo.HRK_SkillEffects e ON p.SkillEffectId = e.Id
    INNER JOIN @EpicSkillIds s ON e.SkillId = s.SkillId;

    DELETE sc FROM dbo.HRK_SkillEffectScalings sc
    INNER JOIN dbo.HRK_SkillEffects e ON sc.SkillEffectId = e.Id
    INNER JOIN @EpicSkillIds s ON e.SkillId = s.SkillId;

    DELETE sm FROM dbo.HRK_SkillEffectStatModifiers sm
    INNER JOIN dbo.HRK_SkillEffects e ON sm.SkillEffectId = e.Id
    INNER JOIN @EpicSkillIds s ON e.SkillId = s.SkillId;

    DELETE e FROM dbo.HRK_SkillEffects e
    INNER JOIN @EpicSkillIds s ON e.SkillId = s.SkillId;

    DECLARE @EffId BIGINT;

    ---------------------------------------------------------------------------
    -- 7.1 KIET_BAC_SI_BASIC
    ---------------------------------------------------------------------------
    -- Effect 1: HEAL 14% Caster Max HP, LOWEST_HP_PERCENT (Ally), CanCrit = 0
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('KIET_BAC_SI_BASIC', @HealEffectTypeId, @LowestHpPercentTargetId, NULL, 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @HpAttrId, 0.140000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, StringValue, BoolValue, CreatedOn, UpdatedOn)
    VALUES
        (@EffId, 'CAN_CRIT', NULL, 0, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'TARGET_SIDE', 'ALLY', NULL, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 2: STAT_BUFF Resistance +10%, 1 turn, refresh, max 1
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('KIET_BAC_SI_BASIC', @StatBuffEffectTypeId, @LowestHpPercentTargetId, NULL, 10, 1, 100.00, 1, 2, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectStatModifiers (SkillEffectId, AttributeTypeId, ValueType, Value, CreatedOn, UpdatedOn)
    VALUES (@EffId, @ResistanceAttrId, 'PERCENT', 10.000000, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, StringValue, BoolValue, CreatedOn, UpdatedOn)
    VALUES
        (@EffId, 'TARGET_SIDE', 'ALLY', NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'REFRESH_ON_REAPPLY', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    ---------------------------------------------------------------------------
    -- 7.2 KIET_BAC_SI_EMERGENCY_PROTOCOL
    ---------------------------------------------------------------------------
    -- Effect 1: HEAL 20% Caster Max HP, ALLY_ALL, CanCrit = 0
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('KIET_BAC_SI_EMERGENCY_PROTOCOL', @HealEffectTypeId, @AllyAllTargetId, NULL, 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @HpAttrId, 0.200000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'CAN_CRIT', 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 2: STAT_BUFF Resistance +15%, 2 turns, ALLY_ALL
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('KIET_BAC_SI_EMERGENCY_PROTOCOL', @StatBuffEffectTypeId, @AllyAllTargetId, NULL, 15, 2, 100.00, 1, 2, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectStatModifiers (SkillEffectId, AttributeTypeId, ValueType, Value, CreatedOn, UpdatedOn)
    VALUES (@EffId, @ResistanceAttrId, 'PERCENT', 15.000000, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'REFRESH_ON_REAPPLY', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    ---------------------------------------------------------------------------
    -- 7.3 TRUONG_KIET_CHU_MO_BASIC
    ---------------------------------------------------------------------------
    -- Effect 1: Physical Damage 115% ATK, ENEMY_SINGLE, CanCrit = 1
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('TRUONG_KIET_CHU_MO_BASIC', @DamageEffectTypeId, @EnemySingleTargetId, 'PHYSICAL', 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @AtkAttrId, 1.150000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'CAN_CRIT', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 2: STAT_DEBUFF Accuracy -15%, 1 turn, 30% chance
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('TRUONG_KIET_CHU_MO_BASIC', @StatDebuffEffectTypeId, @EnemySingleTargetId, NULL, -15, 1, 30.00, 1, 2, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectStatModifiers (SkillEffectId, AttributeTypeId, ValueType, Value, CreatedOn, UpdatedOn)
    VALUES (@EffId, @AccuracyAttrId, 'PERCENT', -15.000000, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'REFRESH_ON_REAPPLY', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    ---------------------------------------------------------------------------
    -- 7.4 TRUONG_KIET_SOUL_KISS
    ---------------------------------------------------------------------------
    -- Effect 1: Physical Damage 205% ATK, ENEMY_SINGLE, CanCrit = 1
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('TRUONG_KIET_SOUL_KISS', @DamageEffectTypeId, @EnemySingleTargetId, 'PHYSICAL', 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @AtkAttrId, 2.050000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'CAN_CRIT', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 2: PANIC 45% chance, 1 turn
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('TRUONG_KIET_SOUL_KISS', @PanicEffectTypeId, @EnemySingleTargetId, NULL, 0, 1, 45.00, 1, 2, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'REFRESH_ON_REAPPLY', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    ---------------------------------------------------------------------------
    -- 7.5 TIEN_DUNG_TONG_DAI_BASIC
    ---------------------------------------------------------------------------
    -- Effect 1: Magic Damage 105% MagicDamage, ENEMY_SINGLE, CanCrit = 1
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('TIEN_DUNG_TONG_DAI_BASIC', @DamageEffectTypeId, @EnemySingleTargetId, 'MAGIC', 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @MagicDmgAttrId, 1.050000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'CAN_CRIT', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 2: SILENCE 25% chance, 1 turn
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('TIEN_DUNG_TONG_DAI_BASIC', @SilenceEffectTypeId, @EnemySingleTargetId, NULL, 0, 1, 25.00, 1, 2, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'REFRESH_ON_REAPPLY', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    ---------------------------------------------------------------------------
    -- 7.6 TIEN_DUNG_EMERGENCY_CONFERENCE
    ---------------------------------------------------------------------------
    -- Effect 1: Magic Damage 90% MagicDamage, ENEMY_ALL, CanCrit = 0
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('TIEN_DUNG_EMERGENCY_CONFERENCE', @DamageEffectTypeId, @EnemyAllTargetId, 'MAGIC', 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @MagicDmgAttrId, 0.900000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'CAN_CRIT', 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 2: ACTION_BAR_CHANGE -15, 40% chance per target (roll independent)
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('TIEN_DUNG_EMERGENCY_CONFERENCE', @ActionBarChangeEffectTypeId, @EnemyAllTargetId, NULL, -15, NULL, 40.00, 1, 2, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, IntValue, BoolValue, CreatedOn, UpdatedOn)
    VALUES
        (@EffId, 'ACTION_BAR_DELTA', -15, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'ROLL_ONCE_PER_EFFECT', NULL, 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    ---------------------------------------------------------------------------
    -- 7.7 QUOC_NHAN_GIA_DIEN_BASIC
    ---------------------------------------------------------------------------
    -- Effect 1: Physical Damage 120% ATK, ENEMY_SINGLE, CanCrit = 1
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('QUOC_NHAN_GIA_DIEN_BASIC', @DamageEffectTypeId, @EnemySingleTargetId, 'PHYSICAL', 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @AtkAttrId, 1.200000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'CAN_CRIT', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 2: BLEED 30% ATK per turn, 2 turns, 35% chance, max 1 stack
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('QUOC_NHAN_GIA_DIEN_BASIC', @BleedEffectTypeId, @EnemySingleTargetId, 'PHYSICAL', 0, 2, 35.00, 1, 2, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @AtkAttrId, 0.300000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES
        (@EffId, 'REFRESH_ON_REAPPLY', 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'CAN_CRIT', 0, SYSUTCDATETIME(), SYSUTCDATETIME());

    ---------------------------------------------------------------------------
    -- 7.8 QUOC_NHAN_NIGHT_PHANTOMS
    ---------------------------------------------------------------------------
    -- Effect 1: Physical Damage 125% ATK, ENEMY_BACK_ROW, CanCrit = 1
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('QUOC_NHAN_NIGHT_PHANTOMS', @DamageEffectTypeId, @EnemyBackRowTargetId, 'PHYSICAL', 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @AtkAttrId, 1.250000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'CAN_CRIT', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 2: STAT_BUFF Crit +15%, 2 turns, SELF
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('QUOC_NHAN_NIGHT_PHANTOMS', @StatBuffEffectTypeId, @SelfTargetId, NULL, 15, 2, 100.00, 1, 2, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectStatModifiers (SkillEffectId, AttributeTypeId, ValueType, Value, CreatedOn, UpdatedOn)
    VALUES (@EffId, @CritAttrId, 'PERCENT', 15.000000, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'REFRESH_ON_REAPPLY', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    ---------------------------------------------------------------------------
    -- 7.9 CAU_VANG_MAT_LANH_BASIC
    ---------------------------------------------------------------------------
    -- Effect 1: Physical Damage 100% ATK, ENEMY_SINGLE, CanCrit = 1
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('CAU_VANG_MAT_LANH_BASIC', @DamageEffectTypeId, @EnemySingleTargetId, 'PHYSICAL', 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @AtkAttrId, 1.000000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'CAN_CRIT', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 2: STAT_BUFF DEF +15%, 2 turns, SELF
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('CAU_VANG_MAT_LANH_BASIC', @StatBuffEffectTypeId, @SelfTargetId, NULL, 15, 2, 100.00, 1, 2, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectStatModifiers (SkillEffectId, AttributeTypeId, ValueType, Value, CreatedOn, UpdatedOn)
    VALUES (@EffId, @DefAttrId, 'PERCENT', 15.000000, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'REFRESH_ON_REAPPLY', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    ---------------------------------------------------------------------------
    -- 7.10 CAU_VANG_CALM_GUARD
    ---------------------------------------------------------------------------
    -- Effect 1: SHIELD 12% Caster Max HP, 2 turns, ALLY_ALL
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('CAU_VANG_CALM_GUARD', @ShieldEffectTypeId, @AllyAllTargetId, NULL, 0, 2, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @HpAttrId, 0.120000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'REFRESH_ON_REAPPLY', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Effect 2: DAMAGE_REDUCTION 12%, 2 turns, ALLY_ALL
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('CAU_VANG_CALM_GUARD', @DamageReductionEffectTypeId, @AllyAllTargetId, NULL, 12, 2, 100.00, 1, 2, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'REFRESH_ON_REAPPLY', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    PRINT N'-> Đã seed toàn bộ SkillEffects, Scalings, Parameters, StatModifiers cho 10 kỹ năng Epic.';

    ---------------------------------------------------------------------------
    -- 8. Grant heroes to Player 1 (If exists)
    ---------------------------------------------------------------------------
    IF EXISTS (SELECT 1 FROM dbo.HRK_Players WHERE Id = 1)
    BEGIN
        DECLARE @NewHeroTemplateIds TABLE (TemplateId INT);
        INSERT INTO @NewHeroTemplateIds VALUES
            (@KietBacSiId), (@TruongKietId), (@TienDungTongDaiId), (@QuocNhanId), (@CauVangId);

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

        PRINT N'-> Đã cấp 5 hero Epic mới cho PlayerId = 1.';
    END;

    COMMIT TRANSACTION;
    PRINT N'=== [HOÀN TẤT THÀNH CÔNG] Migration_AddFiveEpicHeroes ===';
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

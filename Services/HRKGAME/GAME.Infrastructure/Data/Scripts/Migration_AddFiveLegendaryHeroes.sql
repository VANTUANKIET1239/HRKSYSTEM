-- ============================================================================
-- Migration: Migration_AddFiveLegendaryHeroes.sql
-- Mục đích: Thêm 5 Hero Legendary mới và 10 Skill Templates hoàn chỉnh
-- Ngày tạo: 2026-09-29
-- Danh sách 5 Hero Legendary:
--   1. Kiệt Mái Xéo (kiet-mai-xeo) - Assassin / Ngụy (Physical)
--   2. Trường Kiệt Tốt Nghiệp Cấp 3 (truong-kiet-tot-nghiep-cap-3) - Tanker / Ngụy (Physical)
--   3. Quốc Nhân Tốt Nghiệp Cấp 3 (quoc-nhan-tot-nghiep-cap-3) - Mage / Ngụy (Magic)
--   4. Long Lê Con Mèo (long-le-con-meo) - Support / Thục (Physical)
--   5. Quốc Nhân Chạy Ngay Đi (quoc-nhan-chay-ngay-di) - Mage / Quần (Magic)
-- ============================================================================

USE [HRK];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    PRINT N'=== BẮT ĐẦU MIGRATION 5 HERO LEGENDARY (29/09/2026) ===';

    ---------------------------------------------------------------------------
    -- 1. Dynamic Resolution: Rarities, Classes, Factions, Attributes
    ---------------------------------------------------------------------------
    DECLARE @RarityLegendaryId INT = (SELECT TOP 1 Id FROM dbo.HRK_Rarities WHERE UPPER(Code) = 'LEGENDARY');
    IF @RarityLegendaryId IS NULL SET @RarityLegendaryId = 4;

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
    -- 2. Target Types Resolution (and ensure missing ones exist)
    ---------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) = 'ENEMY_SAME_VERTICAL_LANE')
        INSERT INTO dbo.HRK_SkillTargetTypes (Code, Name, TargetSide, SelectionRule, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
        VALUES ('ENEMY_SAME_VERTICAL_LANE', N'Kẻ địch cùng hàng dọc', 'ENEMY', 'SAME_VERTICAL_LANE', 90, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) = 'ENEMY_FRONT_ROW_WITH_BACK_ROW_FALLBACK')
        INSERT INTO dbo.HRK_SkillTargetTypes (Code, Name, TargetSide, SelectionRule, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
        VALUES ('ENEMY_FRONT_ROW_WITH_BACK_ROW_FALLBACK', N'Hàng trước ưu tiên (fallback hàng sau)', 'ENEMY', 'FRONT_ROW_WITH_BACK_ROW_FALLBACK', 91, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) = 'RANDOM_ELIGIBLE_ALLIES_N')
        INSERT INTO dbo.HRK_SkillTargetTypes (Code, Name, TargetSide, SelectionRule, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
        VALUES ('RANDOM_ELIGIBLE_ALLIES_N', N'N đồng minh hợp lệ ngẫu nhiên', 'ALLY', 'RANDOM_ELIGIBLE_N', 92, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    DECLARE @SelfTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) = 'SELF');
    DECLARE @AllyAllTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) IN ('ALLY_ALL', 'ALL_ALLIES'));
    DECLARE @LowestHpPercentTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) = 'LOWEST_HP_PERCENT');
    DECLARE @EnemySingleTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) IN ('ENEMY_SINGLE', 'SINGLE_ENEMY'));
    DECLARE @EnemyAllTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) IN ('ENEMY_ALL', 'ALL_ENEMIES'));
    DECLARE @EnemyFrontRowTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) IN ('ENEMY_FRONT_ROW', 'FRONT_ROW_ENEMY', 'ENEMY_FRONT_ROW_WITH_BACK_ROW_FALLBACK'));
    DECLARE @EnemySameVerticalLaneTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) = 'ENEMY_SAME_VERTICAL_LANE');
    DECLARE @EnemyFrontRowFallbackTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) = 'ENEMY_FRONT_ROW_WITH_BACK_ROW_FALLBACK');
    DECLARE @RandomEligibleAlliesTargetId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillTargetTypes WHERE UPPER(Code) = 'RANDOM_ELIGIBLE_ALLIES_N');

    IF @EnemySingleTargetId IS NULL THROW 50017, N'Lỗi: Thiếu TargetType ENEMY_SINGLE.', 1;
    IF @LowestHpPercentTargetId IS NULL SET @LowestHpPercentTargetId = @EnemySingleTargetId;
    IF @EnemySameVerticalLaneTargetId IS NULL SET @EnemySameVerticalLaneTargetId = @EnemySingleTargetId;
    IF @EnemyFrontRowFallbackTargetId IS NULL SET @EnemyFrontRowFallbackTargetId = @EnemyFrontRowTargetId;
    IF @RandomEligibleAlliesTargetId IS NULL SET @RandomEligibleAlliesTargetId = @AllyAllTargetId;

    ---------------------------------------------------------------------------
    -- 3. Effect Types Resolution (and ensure custom ones exist)
    ---------------------------------------------------------------------------
    DECLARE @DamageEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'DAMAGE');
    DECLARE @HealEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'HEAL');
    DECLARE @ShieldEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'SHIELD');
    DECLARE @StatBuffEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'STAT_BUFF');
    DECLARE @StatDebuffEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'STAT_DEBUFF');
    DECLARE @StunEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'STUN');
    DECLARE @SilenceEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'SILENCE');
    DECLARE @DamageReductionEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'DAMAGE_REDUCTION');

    IF @DamageEffectTypeId IS NULL THROW 50020, N'Lỗi: Thiếu EffectType DAMAGE.', 1;
    IF @HealEffectTypeId IS NULL THROW 50021, N'Lỗi: Thiếu EffectType HEAL.', 1;
    IF @ShieldEffectTypeId IS NULL THROW 50022, N'Lỗi: Thiếu EffectType SHIELD.', 1;
    IF @StatBuffEffectTypeId IS NULL THROW 50023, N'Lỗi: Thiếu EffectType STAT_BUFF.', 1;
    IF @StatDebuffEffectTypeId IS NULL THROW 50024, N'Lỗi: Thiếu EffectType STAT_DEBUFF.', 1;

    -- Ensure custom status effect types exist in HRK_SkillEffectTypes
    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'PHONG_AN')
        INSERT INTO dbo.HRK_SkillEffectTypes (Code, Name, IsDebuff, EffectGroup, IsBeneficial, IsStackable, DefaultStackLimit, Description, ImagePath, ColorHex, CreatedOn, UpdatedOn)
        VALUES ('PHONG_AN', N'Phong Ấn', 0, 'RESOURCE', 1, 1, 3, N'Mỗi tầng tăng 5% Tốc độ. Tiêu thụ khi thi triển kỹ năng nộ.', '/assets/images/dcs-game/effects/phong-an.png', '#14b8a6', SYSUTCDATETIME(), SYSUTCDATETIME());

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'TIN_CHI_DANH_DU')
        INSERT INTO dbo.HRK_SkillEffectTypes (Code, Name, IsDebuff, EffectGroup, IsBeneficial, IsStackable, DefaultStackLimit, Description, ImagePath, ColorHex, CreatedOn, UpdatedOn)
        VALUES ('TIN_CHI_DANH_DU', N'Tín Chỉ Danh Dự', 0, 'RESOURCE', 1, 1, 3, N'Mỗi tầng tăng 6% DEF và 6% Kháng Phép. Tiêu thụ khi thi triển kỹ năng nộ.', '/assets/images/dcs-game/effects/tin-chi-danh-du.png', '#f59e0b', SYSUTCDATETIME(), SYSUTCDATETIME());

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'LUAN_DIEM')
        INSERT INTO dbo.HRK_SkillEffectTypes (Code, Name, IsDebuff, EffectGroup, IsBeneficial, IsStackable, DefaultStackLimit, Description, ImagePath, ColorHex, CreatedOn, UpdatedOn)
        VALUES ('LUAN_DIEM', N'Luận Điểm', 1, 'DEBUFF', 0, 1, 3, N'Mỗi tầng tăng 4% sát thương phép nhận từ riêng Quốc Nhân. Tiêu thụ để gây nổ sát thương.', '/assets/images/dcs-game/effects/luan-diem.png', '#b91c1c', SYSUTCDATETIME(), SYSUTCDATETIME());

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'CAT_SCRATCH')
        INSERT INTO dbo.HRK_SkillEffectTypes (Code, Name, IsDebuff, EffectGroup, IsBeneficial, IsStackable, DefaultStackLimit, Description, ImagePath, ColorHex, CreatedOn, UpdatedOn)
        VALUES ('CAT_SCRATCH', N'Vết Cào', 1, 'DEBUFF', 0, 0, 1, N'Đồng minh đánh vào tăng 10% sát thương và hồi phục 5% sát thương thực tế.', '/assets/images/dcs-game/effects/cat-scratch.png', '#eab308', SYSUTCDATETIME(), SYSUTCDATETIME());

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'DEEP_CAT_SCRATCH')
        INSERT INTO dbo.HRK_SkillEffectTypes (Code, Name, IsDebuff, EffectGroup, IsBeneficial, IsStackable, DefaultStackLimit, Description, ImagePath, ColorHex, CreatedOn, UpdatedOn)
        VALUES ('DEEP_CAT_SCRATCH', N'Vết Cào Sâu', 1, 'DEBUFF', 0, 0, 1, N'Đồng minh đánh vào tăng 20% sát thương và hồi phục 10% sát thương thực tế.', '/assets/images/dcs-game/effects/deep-cat-scratch.png', '#dc2626', SYSUTCDATETIME(), SYSUTCDATETIME());

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'CAT_COMPANION')
        INSERT INTO dbo.HRK_SkillEffectTypes (Code, Name, IsDebuff, EffectGroup, IsBeneficial, IsStackable, DefaultStackLimit, Description, ImagePath, ColorHex, CreatedOn, UpdatedOn)
        VALUES ('CAT_COMPANION', N'Mèo Đồng Hành', 0, 'BUFF', 1, 0, 1, N'Mèo đồng hành hỗ trợ tấn công và đặt Vết Cào Sâu lên mục tiêu.', '/assets/images/dcs-game/effects/cat-companion.png', '#f59e0b', SYSUTCDATETIME(), SYSUTCDATETIME());

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'CHAY_NGAY_DI')
        INSERT INTO dbo.HRK_SkillEffectTypes (Code, Name, IsDebuff, EffectGroup, IsBeneficial, IsStackable, DefaultStackLimit, Description, ImagePath, ColorHex, CreatedOn, UpdatedOn)
        VALUES ('CHAY_NGAY_DI', N'Chạy Ngay Đi', 1, 'DEBUFF', 0, 0, 1, N'Giảm 30% Kháng Phép trong 3 lượt. Kích hoạt vệt lửa quét dọc khi trúng đòn cơ bản.', '/assets/images/dcs-game/effects/chay-ngay-di.png', '#ef4444', SYSUTCDATETIME(), SYSUTCDATETIME());

    DECLARE @PhongAnEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'PHONG_AN');
    DECLARE @TinChiEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'TIN_CHI_DANH_DU');
    DECLARE @LuanDiemEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'LUAN_DIEM');
    DECLARE @CatScratchEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'CAT_SCRATCH');
    DECLARE @DeepCatScratchEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'DEEP_CAT_SCRATCH');
    DECLARE @CatCompanionEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'CAT_COMPANION');
    DECLARE @ChayNgayDiEffectTypeId INT = (SELECT TOP 1 Id FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'CHAY_NGAY_DI');

    ---------------------------------------------------------------------------
    -- 4. Upsert 5 Legendary Hero Templates
    ---------------------------------------------------------------------------

    -- 4.1 Kiệt Mái Xéo (kiet-mai-xeo)
    DECLARE @KietMaiXeoId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE UPPER(Name) = N'KIỆT MÁI XÉO' OR Avatar LIKE '%kiet-mai-xeo%');
    IF @KietMaiXeoId IS NULL
    BEGIN
        INSERT INTO dbo.HRK_HeroTemplates
        (
            Name, Avatar, FactionId, ClassId, RarityId,
            BaseHp, BaseAtk, BaseDef, BaseSpd, BaseCrit, BaseCritDmg,
            BaseLifesteal, BaseAccuracy, BaseResistance, BaseMagicDamage, BaseMagicResistance, CreatedOn
        )
        VALUES
        (
            N'Kiệt Mái Xéo', '/assets/images/dcs-game/kiet-mai-xeo.jpg', @FactionNguyId, @ClassAssassinId, @RarityLegendaryId,
            1050, 215, 72, 135, 20.00, 155.00, 0.00, 95.00, 12.00, 40, 60, SYSUTCDATETIME()
        );
        SET @KietMaiXeoId = SCOPE_IDENTITY();
        PRINT N'-> Đã tạo mới HeroTemplate Kiệt Mái Xéo.';
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_HeroTemplates
        SET Name = N'Kiệt Mái Xéo',
            Avatar = CASE WHEN Avatar LIKE '%-legendary.png' THEN Avatar ELSE '/assets/images/dcs-game/kiet-mai-xeo.jpg' END,
            FactionId = @FactionNguyId, ClassId = @ClassAssassinId, RarityId = @RarityLegendaryId,
            BaseHp = 1050, BaseAtk = 215, BaseDef = 72, BaseSpd = 135, BaseCrit = 20.00, BaseCritDmg = 155.00,
            BaseLifesteal = 0.00, BaseAccuracy = 95.00, BaseResistance = 12.00, BaseMagicDamage = 40, BaseMagicResistance = 60
        WHERE Id = @KietMaiXeoId;
        PRINT N'-> Đã cập nhật HeroTemplate Kiệt Mái Xéo.';
    END;

    -- 4.2 Trường Kiệt Tốt Nghiệp Cấp 3 (truong-kiet-tot-nghiep-cap-3)
    DECLARE @TruongKietGradId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE UPPER(Name) = N'TRƯỜNG KIỆT TỐT NGHIỆP CẤP 3' OR Avatar LIKE '%truong-kiet-tot-nghiep-cap-3%');
    IF @TruongKietGradId IS NULL
    BEGIN
        INSERT INTO dbo.HRK_HeroTemplates
        (
            Name, Avatar, FactionId, ClassId, RarityId,
            BaseHp, BaseAtk, BaseDef, BaseSpd, BaseCrit, BaseCritDmg,
            BaseLifesteal, BaseAccuracy, BaseResistance, BaseMagicDamage, BaseMagicResistance, CreatedOn
        )
        VALUES
        (
            N'Trường Kiệt Tốt Nghiệp Cấp 3', '/assets/images/dcs-game/truong-kiet-tot-nghiep-cap-3.png', @FactionNguyId, @ClassTankerId, @RarityLegendaryId,
            1550, 120, 140, 85, 5.00, 150.00, 0.00, 80.00, 35.00, 40, 190, SYSUTCDATETIME()
        );
        SET @TruongKietGradId = SCOPE_IDENTITY();
        PRINT N'-> Đã tạo mới HeroTemplate Trường Kiệt Tốt Nghiệp Cấp 3.';
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_HeroTemplates
        SET Name = N'Trường Kiệt Tốt Nghiệp Cấp 3',
            Avatar = CASE WHEN Avatar LIKE '%-legendary.png' THEN Avatar ELSE '/assets/images/dcs-game/truong-kiet-tot-nghiep-cap-3.png' END,
            FactionId = @FactionNguyId, ClassId = @ClassTankerId, RarityId = @RarityLegendaryId,
            BaseHp = 1550, BaseAtk = 120, BaseDef = 140, BaseSpd = 85, BaseCrit = 5.00, BaseCritDmg = 150.00,
            BaseLifesteal = 0.00, BaseAccuracy = 80.00, BaseResistance = 35.00, BaseMagicDamage = 40, BaseMagicResistance = 190
        WHERE Id = @TruongKietGradId;
        PRINT N'-> Đã cập nhật HeroTemplate Trường Kiệt Tốt Nghiệp Cấp 3.';
    END;

    -- 4.3 Quốc Nhân Tốt Nghiệp Cấp 3 (quoc-nhan-tot-nghiep-cap-3)
    DECLARE @QuocNhanGradId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE UPPER(Name) = N'QUỐC NHÂN TỐT NGHIỆP CẤP 3' OR Avatar LIKE '%quoc-nhan-tot-nghiep-cap-3%');
    IF @QuocNhanGradId IS NULL
    BEGIN
        INSERT INTO dbo.HRK_HeroTemplates
        (
            Name, Avatar, FactionId, ClassId, RarityId,
            BaseHp, BaseAtk, BaseDef, BaseSpd, BaseCrit, BaseCritDmg,
            BaseLifesteal, BaseAccuracy, BaseResistance, BaseMagicDamage, BaseMagicResistance, CreatedOn
        )
        VALUES
        (
            N'Quốc Nhân Tốt Nghiệp Cấp 3', '/assets/images/dcs-game/quoc-nhan-tot-nghiep-cap-3.png', @FactionNguyId, @ClassMageId, @RarityLegendaryId,
            1020, 90, 65, 128, 10.00, 150.00, 0.00, 92.00, 18.00, 320, 110, SYSUTCDATETIME()
        );
        SET @QuocNhanGradId = SCOPE_IDENTITY();
        PRINT N'-> Đã tạo mới HeroTemplate Quốc Nhân Tốt Nghiệp Cấp 3.';
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_HeroTemplates
        SET Name = N'Quốc Nhân Tốt Nghiệp Cấp 3',
            Avatar = CASE WHEN Avatar LIKE '%-legendary.png' THEN Avatar ELSE '/assets/images/dcs-game/quoc-nhan-tot-nghiep-cap-3.png' END,
            FactionId = @FactionNguyId, ClassId = @ClassMageId, RarityId = @RarityLegendaryId,
            BaseHp = 1020, BaseAtk = 90, BaseDef = 65, BaseSpd = 128, BaseCrit = 10.00, BaseCritDmg = 150.00,
            BaseLifesteal = 0.00, BaseAccuracy = 92.00, BaseResistance = 18.00, BaseMagicDamage = 320, BaseMagicResistance = 110
        WHERE Id = @QuocNhanGradId;
        PRINT N'-> Đã cập nhật HeroTemplate Quốc Nhân Tốt Nghiệp Cấp 3.';
    END;

    -- 4.4 Long Lê Con Mèo (long-le-con-meo)
    DECLARE @LongLeCatId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE UPPER(Name) = N'LONG LÊ CON MÈO' OR Avatar LIKE '%long-le-con-meo%');
    IF @LongLeCatId IS NULL
    BEGIN
        INSERT INTO dbo.HRK_HeroTemplates
        (
            Name, Avatar, FactionId, ClassId, RarityId,
            BaseHp, BaseAtk, BaseDef, BaseSpd, BaseCrit, BaseCritDmg,
            BaseLifesteal, BaseAccuracy, BaseResistance, BaseMagicDamage, BaseMagicResistance, CreatedOn
        )
        VALUES
        (
            N'Long Lê Con Mèo', '/assets/images/dcs-game/long-le-con-meo.jpg', @FactionThucId, @ClassSupportId, @RarityLegendaryId,
            1250, 125, 85, 120, 8.00, 150.00, 0.00, 90.00, 30.00, 70, 130, SYSUTCDATETIME()
        );
        SET @LongLeCatId = SCOPE_IDENTITY();
        PRINT N'-> Đã tạo mới HeroTemplate Long Lê Con Mèo.';
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_HeroTemplates
        SET Name = N'Long Lê Con Mèo',
            Avatar = CASE WHEN Avatar LIKE '%-legendary.png' THEN Avatar ELSE '/assets/images/dcs-game/long-le-con-meo.jpg' END,
            FactionId = @FactionThucId, ClassId = @ClassSupportId, RarityId = @RarityLegendaryId,
            BaseHp = 1250, BaseAtk = 125, BaseDef = 85, BaseSpd = 120, BaseCrit = 8.00, BaseCritDmg = 150.00,
            BaseLifesteal = 0.00, BaseAccuracy = 90.00, BaseResistance = 30.00, BaseMagicDamage = 70, BaseMagicResistance = 130
        WHERE Id = @LongLeCatId;
        PRINT N'-> Đã cập nhật HeroTemplate Long Lê Con Mèo.';
    END;

    -- 4.5 Quốc Nhân Chạy Ngay Đi (quoc-nhan-chay-ngay-di)
    DECLARE @QuocNhanRunId INT = (SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE UPPER(Name) = N'QUỐC NHÂN CHẠY NGAY ĐI' OR Avatar LIKE '%quoc-nhan-chay-ngay-di%');
    IF @QuocNhanRunId IS NULL
    BEGIN
        INSERT INTO dbo.HRK_HeroTemplates
        (
            Name, Avatar, FactionId, ClassId, RarityId,
            BaseHp, BaseAtk, BaseDef, BaseSpd, BaseCrit, BaseCritDmg,
            BaseLifesteal, BaseAccuracy, BaseResistance, BaseMagicDamage, BaseMagicResistance, CreatedOn
        )
        VALUES
        (
            N'Quốc Nhân Chạy Ngay Đi', '/assets/images/dcs-game/quoc-nhan-chay-ngay-di.png', @FactionQuanId, @ClassMageId, @RarityLegendaryId,
            1000, 85, 62, 132, 12.00, 150.00, 0.00, 95.00, 15.00, 330, 105, SYSUTCDATETIME()
        );
        SET @QuocNhanRunId = SCOPE_IDENTITY();
        PRINT N'-> Đã tạo mới HeroTemplate Quốc Nhân Chạy Ngay Đi.';
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_HeroTemplates
        SET Name = N'Quốc Nhân Chạy Ngay Đi',
            Avatar = CASE WHEN Avatar LIKE '%-legendary.png' THEN Avatar ELSE '/assets/images/dcs-game/quoc-nhan-chay-ngay-di.png' END,
            FactionId = @FactionQuanId, ClassId = @ClassMageId, RarityId = @RarityLegendaryId,
            BaseHp = 1000, BaseAtk = 85, BaseDef = 62, BaseSpd = 132, BaseCrit = 12.00, BaseCritDmg = 150.00,
            BaseLifesteal = 0.00, BaseAccuracy = 95.00, BaseResistance = 15.00, BaseMagicDamage = 330, BaseMagicResistance = 105
        WHERE Id = @QuocNhanRunId;
        PRINT N'-> Đã cập nhật HeroTemplate Quốc Nhân Chạy Ngay Đi.';
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
    -- Kiệt Mái Xéo
    ('KIET_MAI_XEO_BASIC', N'Đường Kiếm Mái Xéo', 'bi-slash-lg', N'Gây 125% ATK sát thương vật lý lên một kẻ địch. Đòn đánh có thể chí mạng. Nếu mục tiêu ở hàng sau, nhận thêm 15% tỉ lệ chí mạng. Nếu chí mạng, nhận 1 tầng Phong Ấn (+5% SPD/tầng, tối đa 3 tầng).', '/assets/images/dcs-game/skills/kiet-mai-xeo/slash.png', 'NORMAL', 'ON_ATTACK', 0, 1),
    ('KIET_MAI_XEO_ENERGY', N'Tam Phong Đoạn Ảnh', 'bi-lightning-charge-fill', N'Tấn công kẻ địch có % HP thấp nhất 3 lần, mỗi hit gây 70% ATK sát thương vật lý. Tiêu thụ toàn bộ Phong Ấn, mỗi tầng tăng 12% tổng sát thương. Nếu đủ 3 tầng, hit cuối bỏ qua 20% DEF.', '/assets/images/dcs-game/skills/kiet-mai-xeo/triple-slash.png', 'ENERGY', 'MANUAL_ENERGY_FULL', 100, 2),

    -- Trường Kiệt Tốt Nghiệp Cấp 3
    ('TRUONG_KIET_GRADUATION_BASIC', N'Bảo Vệ Lễ Tốt Nghiệp', 'bi-shield-check', N'Gây 115% ATK sát thương vật lý lên mục tiêu. Tạo khiên bằng 8% Max HP của bản thân cho đồng minh có % HP thấp nhất trong 2 lượt (tăng thành 11% Max HP nếu đang có đủ 3 Tín Chỉ). Khi nhận đòn trực tiếp nhận 1 Tín Chỉ (+6% DEF & Kháng Phép/tầng, tối đa 3).', '/assets/images/dcs-game/skills/truong-kiet-graduation/shield.png', 'NORMAL', 'ON_ATTACK', 0, 1),
    ('TRUONG_KIET_GRADUATION_ENERGY', N'Thủ Khoa Đứng Tuyến Đầu', 'bi-shield-fill-plus', N'Gây 105% ATK sát thương vật lý lên toàn bộ hàng trước đối phương. Tiêu thụ toàn bộ Tín Chỉ và tạo khiên cho toàn bộ đồng minh còn sống bằng 9% Max HP (+2% Max HP mỗi Tín Chỉ đã tiêu thụ) trong 2 lượt. Nếu tiêu thụ đủ 3 tầng, nhận 20% Giảm Sát Thương trong 2 lượt.', '/assets/images/dcs-game/skills/truong-kiet-graduation/team-shield.png', 'ENERGY', 'MANUAL_ENERGY_FULL', 100, 2),

    -- Quốc Nhân Tốt Nghiệp Cấp 3
    ('QUOC_NHAN_GRADUATION_BASIC', N'Nhận Xét Bên Lề', 'bi-journal-bookmark-fill', N'Gây 120% Magic Damage sát thương phép lên một kẻ địch và khắc 1 tầng Luận Điểm trong 3 lượt (mỗi tầng tăng 4% sát thương phép nhận từ riêng Quốc Nhân, tối đa 12%). Nếu mục tiêu đã đủ 3 tầng, giảm 10% Kháng Phép trong 2 lượt (chỉ refresh, không stack).', '/assets/images/dcs-game/skills/quoc-nhan-graduation/thesis-book.png', 'NORMAL', 'ON_ATTACK', 0, 1),
    ('QUOC_NHAN_GRADUATION_ENERGY', N'Hội Đồng Phản Biện', 'bi-book-half', N'Gây 115% Magic Damage sát thương phép lên tối đa 3 kẻ địch còn sống. Sau đó tiêu thụ toàn bộ Luận Điểm trên từng mục tiêu, mỗi tầng gây thêm 18% Magic Damage. Nếu tiêu thụ đủ 3 tầng trên mục tiêu, có 50% tỉ lệ làm Câm Lặng mục tiêu trong 1 lượt.', '/assets/images/dcs-game/skills/quoc-nhan-graduation/thesis-defense.png', 'ENERGY', 'MANUAL_ENERGY_FULL', 100, 2),

    -- Long Lê Con Mèo
    ('LONG_LE_CAT_SCRATCH_BASIC', N'Mèo Cào Đánh Dấu', 'bi-flower1', N'Con mèo lao tới gây 100% ATK sát thương vật lý lên mục tiêu và đặt Vết Cào màu vàng trong 2 lượt. Khi đồng minh đánh trực tiếp mục tiêu có Vết Cào: tăng 10% sát thương và hồi HP bằng 5% sát thương thực tế.', '/assets/images/dcs-game/skills/long-le-cat/cat-scratch.png', 'NORMAL', 'ON_ATTACK', 0, 1),
    ('LONG_LE_CAT_COMPANIONS', N'Tam Miêu Hộ Trận', 'bi-suit-heart-fill', N'Triệu hồi mèo đồng hành cho tối đa 3 đồng minh ngẫu nhiên (không chọn Long Lê, không chọn tướng có kỹ năng cơ bản hồi máu) trong 2 lượt của đồng minh đó. Khi đồng minh có mèo đánh trực tiếp gây damage, mèo cào hỗ trợ đặt Vết Cào Sâu (+20% sát thương, hồi 10% sát thương thực tế) thay thế Vết Cào thường.', '/assets/images/dcs-game/skills/long-le-cat/cat-companions.png', 'ENERGY', 'MANUAL_ENERGY_FULL', 100, 2),

    -- Quốc Nhân Chạy Ngay Đi
    ('QUOC_NHAN_RUN_NOW_BASIC', N'Lửa Nến Xuyên Hàng', 'bi-fire', N'Phóng luồng lửa ma thuật xuyên hàng dọc trước mặt. Nếu hàng có >= 2 kẻ địch, gây 60% Magic Damage cho mỗi người; nếu chỉ có 1 kẻ địch, gây 120% Magic Damage. Nếu mục tiêu trúng đòn mang dấu Chạy Ngay Đi, tiêu thụ dấu và kích hoạt vệt lửa quét dọc gây thêm 75% Magic Damage và Choáng 1 lượt.', '/assets/images/dcs-game/skills/quoc-nhan-run-now/candle-flame.png', 'NORMAL', 'ON_ATTACK', 0, 1),
    ('QUOC_NHAN_RUN_NOW_ENERGY', N'Chạy Ngay Đi', 'bi-brightness-high-fill', N'Thắp nến đỏ ma mị đặt dấu ấn Chạy Ngay Đi lên toàn bộ kẻ địch còn sống ở hàng trước (nếu hàng trước hết người thì fallback sang hàng sau). Dấu ấn giảm 30% Kháng Phép trong 3 lượt và sẵn sàng phát nổ khi trúng đòn cơ bản.', '/assets/images/dcs-game/skills/quoc-nhan-run-now/red-candle.png', 'ENERGY', 'MANUAL_ENERGY_FULL', 100, 2);

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

    PRINT N'-> Đã upsert 10 Skill Templates cho 5 Hero Legendary.';

    ---------------------------------------------------------------------------
    -- 6. Map Exactly 2 Skills to Each Legendary Hero
    ---------------------------------------------------------------------------
    DELETE FROM dbo.HRK_HeroSkills
    WHERE HeroTemplateId IN (@KietMaiXeoId, @TruongKietGradId, @QuocNhanGradId, @LongLeCatId, @QuocNhanRunId);

    INSERT INTO dbo.HRK_HeroSkills (HeroTemplateId, SkillId, SkillOrder)
    VALUES
        -- Kiệt Mái Xéo
        (@KietMaiXeoId, 'KIET_MAI_XEO_BASIC', 1),
        (@KietMaiXeoId, 'KIET_MAI_XEO_ENERGY', 2),
        -- Trường Kiệt Tốt Nghiệp Cấp 3
        (@TruongKietGradId, 'TRUONG_KIET_GRADUATION_BASIC', 1),
        (@TruongKietGradId, 'TRUONG_KIET_GRADUATION_ENERGY', 2),
        -- Quốc Nhân Tốt Nghiệp Cấp 3
        (@QuocNhanGradId, 'QUOC_NHAN_GRADUATION_BASIC', 1),
        (@QuocNhanGradId, 'QUOC_NHAN_GRADUATION_ENERGY', 2),
        -- Long Lê Con Mèo
        (@LongLeCatId, 'LONG_LE_CAT_SCRATCH_BASIC', 1),
        (@LongLeCatId, 'LONG_LE_CAT_COMPANIONS', 2),
        -- Quốc Nhân Chạy Ngay Đi
        (@QuocNhanRunId, 'QUOC_NHAN_RUN_NOW_BASIC', 1),
        (@QuocNhanRunId, 'QUOC_NHAN_RUN_NOW_ENERGY', 2);

    PRINT N'-> Đã liên kết kỹ năng cho 5 Hero Legendary.';

    ---------------------------------------------------------------------------
    -- 7. Clean up and Seed Skill Effects, Scalings, Parameters, StatModifiers
    ---------------------------------------------------------------------------
    DECLARE @LegendarySkillIds TABLE (SkillId NVARCHAR(50));
    INSERT INTO @LegendarySkillIds VALUES
        ('KIET_MAI_XEO_BASIC'), ('KIET_MAI_XEO_ENERGY'),
        ('TRUONG_KIET_GRADUATION_BASIC'), ('TRUONG_KIET_GRADUATION_ENERGY'),
        ('QUOC_NHAN_GRADUATION_BASIC'), ('QUOC_NHAN_GRADUATION_ENERGY'),
        ('LONG_LE_CAT_SCRATCH_BASIC'), ('LONG_LE_CAT_COMPANIONS'),
        ('QUOC_NHAN_RUN_NOW_BASIC'), ('QUOC_NHAN_RUN_NOW_ENERGY');

    DELETE p FROM dbo.HRK_SkillEffectParameters p
    INNER JOIN dbo.HRK_SkillEffects e ON p.SkillEffectId = e.Id
    INNER JOIN @LegendarySkillIds s ON e.SkillId = s.SkillId;

    DELETE sc FROM dbo.HRK_SkillEffectScalings sc
    INNER JOIN dbo.HRK_SkillEffects e ON sc.SkillEffectId = e.Id
    INNER JOIN @LegendarySkillIds s ON e.SkillId = s.SkillId;

    DELETE sm FROM dbo.HRK_SkillEffectStatModifiers sm
    INNER JOIN dbo.HRK_SkillEffects e ON sm.SkillEffectId = e.Id
    INNER JOIN @LegendarySkillIds s ON e.SkillId = s.SkillId;

    DELETE e FROM dbo.HRK_SkillEffects e
    INNER JOIN @LegendarySkillIds s ON e.SkillId = s.SkillId;

    DECLARE @EffId BIGINT;

    -- 7.1 KIET_MAI_XEO_BASIC: 125% ATK Physical, Single Enemy
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('KIET_MAI_XEO_BASIC', @DamageEffectTypeId, @EnemySingleTargetId, 'PHYSICAL', 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @AtkAttrId, 1.250000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, DecimalValue, BoolValue, CreatedOn, UpdatedOn)
    VALUES
        (@EffId, 'CAN_CRIT', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'BACK_ROW_CRIT_BONUS_PERCENT', 15.0000, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- 7.2 KIET_MAI_XEO_ENERGY: 70% ATK Physical x 3 hits, Lowest HP%
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('KIET_MAI_XEO_ENERGY', @DamageEffectTypeId, @LowestHpPercentTargetId, 'PHYSICAL', 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @AtkAttrId, 0.700000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, DecimalValue, IntValue, BoolValue, CreatedOn, UpdatedOn)
    VALUES
        (@EffId, 'HIT_COUNT', NULL, 3, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'DAMAGE_BONUS_PER_STACK_PERCENT', 12.0000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'MAX_STACK_ARMOR_IGNORE_PERCENT', 20.0000, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'CAN_CRIT', NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- 7.3 TRUONG_KIET_GRADUATION_BASIC: 115% ATK Physical, Single Enemy
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('TRUONG_KIET_GRADUATION_BASIC', @DamageEffectTypeId, @EnemySingleTargetId, 'PHYSICAL', 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @AtkAttrId, 1.150000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, DecimalValue, BoolValue, CreatedOn, UpdatedOn)
    VALUES
        (@EffId, 'BASE_SHIELD_MAX_HP_PERCENT', 0.0800, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'EMPOWERED_SHIELD_MAX_HP_PERCENT', 0.1100, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'CAN_CRIT', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- 7.4 TRUONG_KIET_GRADUATION_ENERGY: 105% ATK Physical, Front Row Enemies
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('TRUONG_KIET_GRADUATION_ENERGY', @DamageEffectTypeId, @EnemyFrontRowTargetId, 'PHYSICAL', 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @AtkAttrId, 1.050000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, DecimalValue, BoolValue, CreatedOn, UpdatedOn)
    VALUES
        (@EffId, 'BASE_TEAM_SHIELD_PERCENT', 0.0900, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'SHIELD_PER_STACK_PERCENT', 0.0200, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'DAMAGE_REDUCTION_PERCENT', 20.0000, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'CAN_CRIT', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- 7.5 QUOC_NHAN_GRADUATION_BASIC: 120% Magic Damage, Single Enemy
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('QUOC_NHAN_GRADUATION_BASIC', @DamageEffectTypeId, @EnemySingleTargetId, 'MAGIC', 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @MagicDmgAttrId, 1.200000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, DecimalValue, BoolValue, CreatedOn, UpdatedOn)
    VALUES
        (@EffId, 'LUAN_DIEM_BONUS_PER_STACK', 4.0000, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'CAN_CRIT', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- 7.6 QUOC_NHAN_GRADUATION_ENERGY: 115% Magic Damage, up to 3 Enemies
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('QUOC_NHAN_GRADUATION_ENERGY', @DamageEffectTypeId, @EnemyAllTargetId, 'MAGIC', 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @MagicDmgAttrId, 1.150000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, DecimalValue, BoolValue, CreatedOn, UpdatedOn)
    VALUES
        (@EffId, 'EXTRA_DAMAGE_PER_STACK_PERCENT', 0.1800, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'SILENCE_CHANCE_PERCENT', 50.0000, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'CAN_CRIT', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- 7.7 LONG_LE_CAT_SCRATCH_BASIC: 100% ATK Physical, Single Enemy
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('LONG_LE_CAT_SCRATCH_BASIC', @DamageEffectTypeId, @EnemySingleTargetId, 'PHYSICAL', 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @AtkAttrId, 1.000000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, BoolValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, 'CAN_CRIT', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- 7.8 LONG_LE_CAT_COMPANIONS: Summon Cat Companions for up to 3 allies
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('LONG_LE_CAT_COMPANIONS', @CatCompanionEffectTypeId, @RandomEligibleAlliesTargetId, NULL, 0, 2, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, IntValue, BoolValue, StringValue, CreatedOn, UpdatedOn)
    VALUES
        (@EffId, 'TARGET_COUNT', 3, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'EXCLUDE_ACTOR', NULL, 1, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'EXCLUDE_HEALER_BASICS', NULL, 1, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'REQUIRED_ABSENT_STATUS_CODE', NULL, NULL, 'CAT_COMPANION', SYSUTCDATETIME(), SYSUTCDATETIME());

    -- 7.9 QUOC_NHAN_RUN_NOW_BASIC: Same Vertical Lane Magic Damage
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('QUOC_NHAN_RUN_NOW_BASIC', @DamageEffectTypeId, @EnemySameVerticalLaneTargetId, 'MAGIC', 0, NULL, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue, CreatedOn, UpdatedOn)
    VALUES (@EffId, @MagicDmgAttrId, 1.200000, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, DecimalValue, BoolValue, CreatedOn, UpdatedOn)
    VALUES
        (@EffId, 'SINGLE_TARGET_COEFF', 1.2000, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'MULTI_TARGET_COEFF', 0.6000, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'TRAIL_DAMAGE_COEFF', 0.7500, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'CAN_CRIT', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- 7.10 QUOC_NHAN_RUN_NOW_ENERGY: Place Chay Ngay Di on Front Row (fallback Back Row)
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DurationTurns, ChancePercent, MaxStacks, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES ('QUOC_NHAN_RUN_NOW_ENERGY', @ChayNgayDiEffectTypeId, @EnemyFrontRowFallbackTargetId, NULL, -30, 3, 100.00, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @EffId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectStatModifiers (SkillEffectId, AttributeTypeId, ValueType, Value, CreatedOn, UpdatedOn)
    VALUES (@EffId, @MagicResAttrId, 'PERCENT', -30.000000, SYSUTCDATETIME(), SYSUTCDATETIME());
    INSERT INTO dbo.HRK_SkillEffectParameters (SkillEffectId, ParameterCode, DecimalValue, BoolValue, CreatedOn, UpdatedOn)
    VALUES
        (@EffId, 'MAGIC_RESISTANCE_DEBUFF_PERCENT', -30.0000, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (@EffId, 'REFRESH_ON_REAPPLY', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    PRINT N'-> Đã seed toàn bộ SkillEffects, Scalings, Parameters, StatModifiers cho 10 kỹ năng Legendary.';

    ---------------------------------------------------------------------------
    -- 8. Grant heroes to Player 1 (If exists)
    ---------------------------------------------------------------------------
    IF EXISTS (SELECT 1 FROM dbo.HRK_Players WHERE Id = 1)
    BEGIN
        DECLARE @NewHeroTemplateIds TABLE (TemplateId INT);
        INSERT INTO @NewHeroTemplateIds VALUES
            (@KietMaiXeoId), (@TruongKietGradId), (@QuocNhanGradId), (@LongLeCatId), (@QuocNhanRunId);

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

        PRINT N'-> Đã cấp 5 hero Legendary mới cho PlayerId = 1.';
    END;

    COMMIT TRANSACTION;
    PRINT N'=== [HOÀN TẤT THÀNH CÔNG] Migration_AddFiveLegendaryHeroes ===';
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

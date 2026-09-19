/*
=========================================================================================
MIGRATION SCRIPT: MAGIC DAMAGE & MAGIC RESISTANCE ATTRIBUTES SYSTEM
=========================================================================================
Project: HRK Game Service
Mục đích:
1. Thêm 2 cột BaseMagicDamage và BaseMagicResistance vào bảng HRK_HeroTemplates (DEFAULT 0).
2. Seed / Merge 2 thuộc tính MAGIC_DAMAGE và MAGIC_RESISTANCE vào HRK_AttributeTypes.
3. Cập nhật CategoryAllowedAttributes cho các slot trang bị tương ứng (RING, ARTIFACT, ARMOR, HELMET).
4. Seed chỉ số BaseMagicDamage và BaseMagicResistance ban đầu cho các hero trong HRK_HeroTemplates theo class (không ghi đè dữ liệu đã khác 0).
5. Seed thuộc tính MAGIC_DAMAGE và MAGIC_RESISTANCE vào một số trang bị mẫu (HRK_ItemTemplateAttributes) để kiểm thử.
6. Cấu hình hệ số lực chiến trong HRK_CombatPowerConfigs cho MAGIC_DAMAGE và MAGIC_RESISTANCE.
=========================================================================================
Script này là idempotent: có thể chạy nhiều lần an toàn mà không phát sinh lỗi hoặc dữ liệu trùng lặp.
=========================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

PRINT '=========================================================================================';
PRINT 'BẮT ĐẦU MIGRATION: HỆ THỐNG THUỘC TÍNH SÁT THƯƠNG PHÉP VÀ KHÁNG PHÉP';
PRINT '=========================================================================================';

-- -------------------------------------------------------------------------------------
-- 1. THÊM 2 CỘT VÀO HRK_HeroTemplates
-- -------------------------------------------------------------------------------------
PRINT '1. Kiểm tra và thêm cột BaseMagicDamage, BaseMagicResistance vào HRK_HeroTemplates...';

IF OBJECT_ID(N'dbo.HRK_HeroTemplates', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.HRK_HeroTemplates', N'BaseMagicDamage') IS NULL
    BEGIN
        ALTER TABLE dbo.HRK_HeroTemplates 
            ADD BaseMagicDamage INT NOT NULL CONSTRAINT DF_HRK_HeroTemplates_BaseMagicDamage DEFAULT (0);
        PRINT '-> Đã thêm cột BaseMagicDamage.';
    END
    ELSE
    BEGIN
        PRINT '-> Cột BaseMagicDamage đã tồn tại.';
    END

    IF COL_LENGTH(N'dbo.HRK_HeroTemplates', N'BaseMagicResistance') IS NULL
    BEGIN
        ALTER TABLE dbo.HRK_HeroTemplates 
            ADD BaseMagicResistance INT NOT NULL CONSTRAINT DF_HRK_HeroTemplates_BaseMagicResistance DEFAULT (0);
        PRINT '-> Đã thêm cột BaseMagicResistance.';
    END
    ELSE
    BEGIN
        PRINT '-> Cột BaseMagicResistance đã tồn tại.';
    END
END
GO

-- -------------------------------------------------------------------------------------
-- 2. SEED / MERGE HRK_AttributeTypes
-- -------------------------------------------------------------------------------------
PRINT '2. Seed và chuẩn hóa HRK_AttributeTypes...';

IF OBJECT_ID(N'dbo.HRK_AttributeTypes', N'U') IS NOT NULL
BEGIN
    MERGE dbo.HRK_AttributeTypes AS Target
    USING (VALUES
        ('MAGIC_DAMAGE',     N'Sát thương phép', 0, 25, N'Chỉ số sát thương phép cơ bản của nhân vật hoặc trang bị'),
        ('MAGIC_RESISTANCE', N'Kháng phép',       0, 35, N'Chỉ số giảm trừ sát thương phép nhận vào')
    ) AS Source ([Code], [Name], [IsPercentage], [DisplayOrder], [Description])
    ON Target.[Code] = Source.[Code]
    WHEN MATCHED THEN
        UPDATE SET Target.[Name] = Source.[Name],
                   Target.[IsPercentage] = Source.[IsPercentage],
                   Target.[DisplayOrder] = Source.[DisplayOrder],
                   Target.[Description] = Source.[Description],
                   Target.[UpdatedOn] = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT ([Code], [Name], [IsPercentage], [DisplayOrder], [Description], [CreatedOn], [UpdatedOn])
        VALUES (Source.[Code], Source.[Name], Source.[IsPercentage], Source.[DisplayOrder], Source.[Description], SYSUTCDATETIME(), SYSUTCDATETIME());

    PRINT '-> Đã seed thuộc tính MAGIC_DAMAGE và MAGIC_RESISTANCE.';
END
GO

-- -------------------------------------------------------------------------------------
-- 3. CẬP NHẬT HRK_CategoryAllowedAttributes CHO CÁC SLOT TRANG BỊ
-- -------------------------------------------------------------------------------------
PRINT '3. Cấu hình CategoryAllowedAttributes cho trang bị...';

IF OBJECT_ID(N'dbo.HRK_CategoryAllowedAttributes', N'U') IS NOT NULL 
   AND OBJECT_ID(N'dbo.HRK_ItemCategories', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.HRK_AttributeTypes', N'U') IS NOT NULL
BEGIN
    DECLARE @AttrMagicDmg INT = (SELECT Id FROM dbo.HRK_AttributeTypes WHERE Code = 'MAGIC_DAMAGE');
    DECLARE @AttrMagicRes INT = (SELECT Id FROM dbo.HRK_AttributeTypes WHERE Code = 'MAGIC_RESISTANCE');

    DECLARE @CatRing INT = (SELECT Id FROM dbo.HRK_ItemCategories WHERE Code = 'RING');
    DECLARE @CatArtifact INT = (SELECT Id FROM dbo.HRK_ItemCategories WHERE Code = 'ARTIFACT');
    DECLARE @CatArmor INT = (SELECT Id FROM dbo.HRK_ItemCategories WHERE Code = 'ARMOR');
    DECLARE @CatHelmet INT = (SELECT Id FROM dbo.HRK_ItemCategories WHERE Code = 'HELMET');

    -- RING: Cho phép MAGIC_DAMAGE
    IF @CatRing IS NOT NULL AND @AttrMagicDmg IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.HRK_CategoryAllowedAttributes WHERE CategoryId = @CatRing AND AttributeTypeId = @AttrMagicDmg)
            INSERT INTO dbo.HRK_CategoryAllowedAttributes (CategoryId, AttributeTypeId, IsMainStat, IsSubStat, MinValue, MaxValue, DisplayOrder)
            VALUES (@CatRing, @AttrMagicDmg, 1, 1, 10, 500, 25);
    END

    -- ARTIFACT: Cho phép cả MAGIC_DAMAGE và MAGIC_RESISTANCE
    IF @CatArtifact IS NOT NULL
    BEGIN
        IF @AttrMagicDmg IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.HRK_CategoryAllowedAttributes WHERE CategoryId = @CatArtifact AND AttributeTypeId = @AttrMagicDmg)
            INSERT INTO dbo.HRK_CategoryAllowedAttributes (CategoryId, AttributeTypeId, IsMainStat, IsSubStat, MinValue, MaxValue, DisplayOrder)
            VALUES (@CatArtifact, @AttrMagicDmg, 1, 1, 20, 800, 25);

        IF @AttrMagicRes IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.HRK_CategoryAllowedAttributes WHERE CategoryId = @CatArtifact AND AttributeTypeId = @AttrMagicRes)
            INSERT INTO dbo.HRK_CategoryAllowedAttributes (CategoryId, AttributeTypeId, IsMainStat, IsSubStat, MinValue, MaxValue, DisplayOrder)
            VALUES (@CatArtifact, @AttrMagicRes, 1, 1, 20, 600, 35);
    END

    -- ARMOR & HELMET: Cho phép MAGIC_RESISTANCE
    IF @CatArmor IS NOT NULL AND @AttrMagicRes IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.HRK_CategoryAllowedAttributes WHERE CategoryId = @CatArmor AND AttributeTypeId = @AttrMagicRes)
            INSERT INTO dbo.HRK_CategoryAllowedAttributes (CategoryId, AttributeTypeId, IsMainStat, IsSubStat, MinValue, MaxValue, DisplayOrder)
            VALUES (@CatArmor, @AttrMagicRes, 1, 1, 10, 400, 35);
    END

    IF @CatHelmet IS NOT NULL AND @AttrMagicRes IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.HRK_CategoryAllowedAttributes WHERE CategoryId = @CatHelmet AND AttributeTypeId = @AttrMagicRes)
            INSERT INTO dbo.HRK_CategoryAllowedAttributes (CategoryId, AttributeTypeId, IsMainStat, IsSubStat, MinValue, MaxValue, DisplayOrder)
            VALUES (@CatHelmet, @AttrMagicRes, 1, 1, 10, 400, 35);
    END

    PRINT '-> Đã cập nhật CategoryAllowedAttributes cho các slot phù hợp.';
END
GO

-- -------------------------------------------------------------------------------------
-- 4. SEED DỮ LIỆU BAN ĐẦU CHO CÁC HERO TRONG HRK_HeroTemplates THEO CLASS
-- -------------------------------------------------------------------------------------
PRINT '4. Seed BaseMagicDamage và BaseMagicResistance cho HRK_HeroTemplates theo Class...';

IF OBJECT_ID(N'dbo.HRK_HeroTemplates', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.HRK_HeroClasses', N'U') IS NOT NULL
BEGIN
    -- Chỉ cập nhật khi cả hai giá trị đều là 0 (tránh ghi đè dữ liệu người dùng đã chỉnh sửa)
    UPDATE ht
    SET ht.BaseMagicDamage = CASE 
            WHEN c.Name LIKE N'%Pháp Sư%' OR c.Code IN ('MAGE', 'WIZARD') THEN 280
            WHEN c.Name LIKE N'%Hỗ Trợ%' OR c.Code IN ('SUPPORT') THEN 120
            WHEN c.Name LIKE N'%Sát Thủ%' OR c.Code IN ('ASSASSIN') THEN 50
            WHEN c.Name LIKE N'%Chiến Sĩ%' OR c.Code IN ('WARRIOR') THEN 40
            ELSE 30
        END,
        ht.BaseMagicResistance = CASE 
            WHEN c.Name LIKE N'%Đỡ Đòn%' OR c.Code IN ('TANK') THEN 180
            WHEN c.Name LIKE N'%Hỗ Trợ%' OR c.Code IN ('SUPPORT') THEN 140
            WHEN c.Name LIKE N'%Pháp Sư%' OR c.Code IN ('MAGE', 'WIZARD') THEN 100
            WHEN c.Name LIKE N'%Chiến Sĩ%' OR c.Code IN ('WARRIOR') THEN 70
            ELSE 50
        END
    FROM dbo.HRK_HeroTemplates ht
    LEFT JOIN dbo.HRK_HeroClasses c ON ht.ClassId = c.Id
    WHERE (ht.BaseMagicDamage = 0 AND ht.BaseMagicResistance = 0);

    PRINT '-> Đã seed chỉ số phép cơ bản cho các hero templates.';
END
GO

-- -------------------------------------------------------------------------------------
-- 5. SEED THUỘC TÍNH TEST CHO TRANG BỊ (HRK_ItemTemplateAttributes)
-- -------------------------------------------------------------------------------------
PRINT '5. Seed thuộc tính MAGIC_DAMAGE và MAGIC_RESISTANCE cho trang bị test...';

IF OBJECT_ID(N'dbo.HRK_ItemTemplateAttributes', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.HRK_ItemTemplates', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.HRK_AttributeTypes', N'U') IS NOT NULL
BEGIN
    DECLARE @AttrMDmgId INT = (SELECT Id FROM dbo.HRK_AttributeTypes WHERE Code = 'MAGIC_DAMAGE');
    DECLARE @AttrMResId INT = (SELECT Id FROM dbo.HRK_AttributeTypes WHERE Code = 'MAGIC_RESISTANCE');

    IF @AttrMDmgId IS NOT NULL AND @AttrMResId IS NOT NULL
    BEGIN
        -- Nhẫn: Thêm MagicDamage
        -- RING_DRAGON_EYE_DIVINE (Id 502) hoặc nhẫn bất kỳ
        DECLARE @RingId INT = (SELECT TOP 1 Id FROM dbo.HRK_ItemTemplates WHERE Id = 502 OR Code = 'RING_DRAGON_EYE_DIVINE');
        IF @RingId IS NOT NULL
        BEGIN
            MERGE dbo.HRK_ItemTemplateAttributes AS Target
            USING (VALUES (@RingId, @AttrMDmgId, CAST(65.0000 AS DECIMAL(18,4)))) AS Source (ItemTemplateId, AttributeTypeId, [Value])
            ON Target.ItemTemplateId = Source.ItemTemplateId AND Target.AttributeTypeId = Source.AttributeTypeId
            WHEN MATCHED THEN UPDATE SET Target.[Value] = Source.[Value]
            WHEN NOT MATCHED THEN INSERT (ItemTemplateId, AttributeTypeId, [Value]) VALUES (Source.ItemTemplateId, Source.AttributeTypeId, Source.[Value]);
        END

        -- Thần Binh: Thêm MagicDamage và MagicResistance
        -- ARTIFACT_SEVEN_STARS_MIRROR (Id 602): MagicDamage
        DECLARE @MirrorArtifactId INT = (SELECT TOP 1 Id FROM dbo.HRK_ItemTemplates WHERE Id = 602 OR Code = 'ARTIFACT_SEVEN_STARS_MIRROR');
        IF @MirrorArtifactId IS NOT NULL
        BEGIN
            MERGE dbo.HRK_ItemTemplateAttributes AS Target
            USING (VALUES (@MirrorArtifactId, @AttrMDmgId, CAST(120.0000 AS DECIMAL(18,4)))) AS Source (ItemTemplateId, AttributeTypeId, [Value])
            ON Target.ItemTemplateId = Source.ItemTemplateId AND Target.AttributeTypeId = Source.AttributeTypeId
            WHEN MATCHED THEN UPDATE SET Target.[Value] = Source.[Value]
            WHEN NOT MATCHED THEN INSERT (ItemTemplateId, AttributeTypeId, [Value]) VALUES (Source.ItemTemplateId, Source.AttributeTypeId, Source.[Value]);
        END

        -- ARTIFACT_IMPERIAL_SEAL (Id 601): MagicResistance
        DECLARE @SealArtifactId INT = (SELECT TOP 1 Id FROM dbo.HRK_ItemTemplates WHERE Id = 601 OR Code = 'ARTIFACT_IMPERIAL_SEAL');
        IF @SealArtifactId IS NOT NULL
        BEGIN
            MERGE dbo.HRK_ItemTemplateAttributes AS Target
            USING (VALUES (@SealArtifactId, @AttrMResId, CAST(85.0000 AS DECIMAL(18,4)))) AS Source (ItemTemplateId, AttributeTypeId, [Value])
            ON Target.ItemTemplateId = Source.ItemTemplateId AND Target.AttributeTypeId = Source.AttributeTypeId
            WHEN MATCHED THEN UPDATE SET Target.[Value] = Source.[Value]
            WHEN NOT MATCHED THEN INSERT (ItemTemplateId, AttributeTypeId, [Value]) VALUES (Source.ItemTemplateId, Source.AttributeTypeId, Source.[Value]);
        END

        -- Giáp: Thêm MagicResistance
        -- ARMOR_NINE_HEAVENS (Id 202) hoặc ARMOR_RED_DRAGON_SCALE (Id 203)
        DECLARE @ArmorId INT = (SELECT TOP 1 Id FROM dbo.HRK_ItemTemplates WHERE Id = 202 OR Code = 'ARMOR_NINE_HEAVENS');
        IF @ArmorId IS NOT NULL
        BEGIN
            MERGE dbo.HRK_ItemTemplateAttributes AS Target
            USING (VALUES (@ArmorId, @AttrMResId, CAST(45.0000 AS DECIMAL(18,4)))) AS Source (ItemTemplateId, AttributeTypeId, [Value])
            ON Target.ItemTemplateId = Source.ItemTemplateId AND Target.AttributeTypeId = Source.AttributeTypeId
            WHEN MATCHED THEN UPDATE SET Target.[Value] = Source.[Value]
            WHEN NOT MATCHED THEN INSERT (ItemTemplateId, AttributeTypeId, [Value]) VALUES (Source.ItemTemplateId, Source.AttributeTypeId, Source.[Value]);
        END

        -- Mũ: Thêm MagicResistance
        -- HELMET_GOLDEN_PHOENIX (Id 302)
        DECLARE @HelmetId INT = (SELECT TOP 1 Id FROM dbo.HRK_ItemTemplates WHERE Id = 302 OR Code = 'HELMET_GOLDEN_PHOENIX');
        IF @HelmetId IS NOT NULL
        BEGIN
            MERGE dbo.HRK_ItemTemplateAttributes AS Target
            USING (VALUES (@HelmetId, @AttrMResId, CAST(55.0000 AS DECIMAL(18,4)))) AS Source (ItemTemplateId, AttributeTypeId, [Value])
            ON Target.ItemTemplateId = Source.ItemTemplateId AND Target.AttributeTypeId = Source.AttributeTypeId
            WHEN MATCHED THEN UPDATE SET Target.[Value] = Source.[Value]
            WHEN NOT MATCHED THEN INSERT (ItemTemplateId, AttributeTypeId, [Value]) VALUES (Source.ItemTemplateId, Source.AttributeTypeId, Source.[Value]);
        END

        PRINT '-> Đã seed thuộc tính phép cho trang bị kiểm thử.';
    END
END
GO

-- -------------------------------------------------------------------------------------
-- 6. BỔ SUNG CẤU HÌNH LỰC CHIẾN (HRK_CombatPowerConfigs)
-- -------------------------------------------------------------------------------------
PRINT '6. Cập nhật bảng HRK_CombatPowerConfigs cho MAGIC_DAMAGE và MAGIC_RESISTANCE...';

IF OBJECT_ID(N'dbo.HRK_CombatPowerConfigs', N'U') IS NOT NULL
BEGIN
    MERGE dbo.HRK_CombatPowerConfigs AS Target
    USING (VALUES
        ('MAGIC_DAMAGE',     CAST(1.5000 AS DECIMAL(18,4)), 25, 1),
        ('MAGIC_RESISTANCE', CAST(1.0000 AS DECIMAL(18,4)), 35, 1)
    ) AS Source (StatCode, PowerPerUnit, DisplayOrder, IsEnabled)
    ON Target.StatCode = Source.StatCode
    WHEN MATCHED THEN
        UPDATE SET Target.PowerPerUnit = Source.PowerPerUnit,
                   Target.DisplayOrder = Source.DisplayOrder,
                   Target.IsEnabled = Source.IsEnabled,
                   Target.UpdatedOn = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (StatCode, PowerPerUnit, DisplayOrder, IsEnabled, UpdatedOn)
        VALUES (Source.StatCode, Source.PowerPerUnit, Source.DisplayOrder, Source.IsEnabled, SYSUTCDATETIME());

    PRINT '-> Đã cập nhật cấu hình lực chiến cho MAGIC_DAMAGE và MAGIC_RESISTANCE.';
END
GO

PRINT '=========================================================================================';
PRINT 'HOÀN TẤT MIGRATION HỆ THỐNG MAGIC DAMAGE & MAGIC RESISTANCE THÀNH CÔNG!';
PRINT '=========================================================================================';

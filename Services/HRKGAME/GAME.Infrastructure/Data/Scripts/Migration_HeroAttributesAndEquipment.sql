-- =========================================================================================
-- MIGRATION SCRIPT: HERO ATTRIBUTES BREAKDOWN & EQUIPMENT ENHANCEMENT SUPPORT
-- =========================================================================================
-- Project: HRK Game Service
-- Mục đích:
-- 1. Chuẩn hóa & bổ sung các thuộc tính cốt lõi trong HRK_AttributeTypes (HP, ATK, DEF, SPD,
--    CRIT_RATE, CRIT_DAMAGE, LIFESTEAL, ACCURACY, RESISTANCE).
-- 2. Cập nhật cấu hình CategoryAllowedAttributes cho 6 slot trang bị (WEAPON, ARMOR, HELMET, BOOTS, RING, ARTIFACT).
-- 3. Đảm bảo Index hiệu năng cho bảng HRK_PlayerEquipment và HRK_PlayerInventory.
-- 4. Đảm bảo dữ liệu liên kết kỹ năng mẫu (HRK_HeroSkills) cho các tướng mẫu.
-- =========================================================================================

SET XACT_ABORT ON;
BEGIN TRANSACTION;

PRINT '1. Dang kiem tra va chuan hoa HRK_AttributeTypes...';

-- Seed / Merge day du cac thuoc tinh cot loi can thiet
MERGE [dbo].[HRK_AttributeTypes] AS Target
USING (VALUES
    ('HP',           N'Sinh Mệnh',             0, 1,  N'Điểm máu cơ bản của nhân vật'),
    ('PHYSICAL_ATK', N'Sát Thương Vật Lý',     0, 2,  N'Gây sát thương vật lý lên kẻ địch'),
    ('MAGIC_ATK',    N'Sát Thương Phép',       0, 3,  N'Gây sát thương phép thuật lên kẻ địch'),
    ('ATK',          N'Công Kích Toàn Năng',   0, 4,  N'Chỉ số tấn công chung'),
    ('ARMOR',        N'Giáp Vật Lý',           0, 5,  N'Giảm trừ sát thương vật lý nhận vào'),
    ('DEF',          N'Phòng Thủ Toàn Năng',   0, 6,  N'Chỉ số phòng thủ chung'),
    ('SPEED',        N'Tốc Độ Ra Chiêu',       0, 7,  N'Quyết định thứ tự hành động trong lượt đấu'),
    ('SPD',          N'Tốc Độ',                0, 8,  N'Chỉ số tốc độ hành động'),
    ('CRIT_RATE',    N'Tỷ Lệ Chí Mạng',        1, 9,  N'Tỷ lệ phát kích sát thương chí mạng (0.05 = 5%)'),
    ('CRIT_DAMAGE',  N'Sát Thương Chí Mạng',   1, 10, N'Hệ số sát thương gây ra khi bạo kích (1.50 = 150%)'),
    ('ACCURACY',     N'Chính Xác',             1, 11, N'Khả năng đánh trúng mục tiêu (0.80 = 80%)'),
    ('LIFESTEAL',    N'Hút Máu',               1, 12, N'Tỷ lệ chuyển hóa sát thương gây ra thành máu (0.05 = 5%)'),
    ('RESISTANCE',   N'Kháng Hiệu Ứng',        1, 13, N'Khả năng giảm hoặc kháng các hiệu ứng bất lợi (0.10 = 10%)'),
    ('MAGIC_RESIST', N'Kháng Phép',            0, 14, N'Giảm trừ sát thương phép thuật nhận vào'),
    ('DODGE_RATE',   N'Tỷ Lệ Né Tránh',        1, 15, N'Tỷ lệ né hoàn toàn đòn tấn công của đối thủ'),
    ('CRIT_RESIST',  N'Kháng Chí Mạng',        1, 16, N'Giảm tỷ lệ bị phát kích chí mạng từ đối thủ')
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

PRINT '2. Dang kiem tra CategoryAllowedAttributes cho cac thuoc tinh moi...';

IF OBJECT_ID('dbo.HRK_CategoryAllowedAttributes', 'U') IS NOT NULL
BEGIN
    -- WEAPON: Cho phep LIFESTEAL, CRIT_RATE, CRIT_DAMAGE
    INSERT INTO [dbo].[HRK_CategoryAllowedAttributes] ([CategoryId], [AttributeTypeId], [IsMainStat], [IsSubStat], [MinValue], [MaxValue], [DisplayOrder])
    SELECT c.[Id], a.[Id], 0, 1, 0.0100, 0.2500, 10
    FROM [dbo].[HRK_ItemCategories] c
    CROSS JOIN [dbo].[HRK_AttributeTypes] a
    WHERE c.[Code] = 'WEAPON' AND a.[Code] IN ('LIFESTEAL', 'CRIT_RATE', 'CRIT_DAMAGE')
      AND NOT EXISTS (
          SELECT 1 FROM [dbo].[HRK_CategoryAllowedAttributes] x
          WHERE x.[CategoryId] = c.[Id] AND x.[AttributeTypeId] = a.[Id]
      );

    -- ARMOR, HELMET, BOOTS: Cho phep RESISTANCE
    INSERT INTO [dbo].[HRK_CategoryAllowedAttributes] ([CategoryId], [AttributeTypeId], [IsMainStat], [IsSubStat], [MinValue], [MaxValue], [DisplayOrder])
    SELECT c.[Id], a.[Id], 0, 1, 0.0100, 0.3000, 10
    FROM [dbo].[HRK_ItemCategories] c
    CROSS JOIN [dbo].[HRK_AttributeTypes] a
    WHERE c.[Code] IN ('ARMOR', 'HELMET', 'BOOTS') AND a.[Code] = 'RESISTANCE'
      AND NOT EXISTS (
          SELECT 1 FROM [dbo].[HRK_CategoryAllowedAttributes] x
          WHERE x.[CategoryId] = c.[Id] AND x.[AttributeTypeId] = a.[Id]
      );

    -- RING, ARTIFACT: Cho phep LIFESTEAL, RESISTANCE, ACCURACY
    INSERT INTO [dbo].[HRK_CategoryAllowedAttributes] ([CategoryId], [AttributeTypeId], [IsMainStat], [IsSubStat], [MinValue], [MaxValue], [DisplayOrder])
    SELECT c.[Id], a.[Id], 0, 1, 0.0100, 0.3000, 10
    FROM [dbo].[HRK_ItemCategories] c
    CROSS JOIN [dbo].[HRK_AttributeTypes] a
    WHERE c.[Code] IN ('RING', 'ARTIFACT') AND a.[Code] IN ('LIFESTEAL', 'RESISTANCE', 'ACCURACY')
      AND NOT EXISTS (
          SELECT 1 FROM [dbo].[HRK_CategoryAllowedAttributes] x
          WHERE x.[CategoryId] = c.[Id] AND x.[AttributeTypeId] = a.[Id]
      );
END;

PRINT '3. Dang kiem tra Index tren HRK_PlayerInventory va HRK_PlayerEquipment...';

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_HRK_PlayerInventory_EquippedHero' 
      AND object_id = OBJECT_ID('dbo.HRK_PlayerInventory')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_HRK_PlayerInventory_EquippedHero
        ON dbo.HRK_PlayerInventory (PlayerId, IsEquipped, EquippedHeroId, IsActive)
        INCLUDE (ItemTemplateId, Enhancement, Stars, CurrentStats);
    PRINT '-> Da tao Index IX_HRK_PlayerInventory_EquippedHero.';
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_HRK_PlayerEquipment_HeroId' 
      AND object_id = OBJECT_ID('dbo.HRK_PlayerEquipment')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_HRK_PlayerEquipment_HeroId
        ON dbo.HRK_PlayerEquipment (PlayerId, HeroId);
    PRINT '-> Da tao Index IX_HRK_PlayerEquipment_HeroId.';
END;

PRINT '4. Kiem tra va dam bao rang buoc 1 ky nang duy nhat cho moi Hero Template...';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HRK_HeroSkills')
BEGIN
    ;WITH DuplicateSkills AS
    (
        SELECT HeroTemplateId,
               SkillId,
               ROW_NUMBER() OVER (PARTITION BY HeroTemplateId ORDER BY SkillOrder, SkillId) AS RowNumber
        FROM dbo.HRK_HeroSkills
    )
    DELETE FROM DuplicateSkills WHERE RowNumber > 1;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'UX_HRK_HeroSkills_HeroTemplateId'
          AND object_id = OBJECT_ID('dbo.HRK_HeroSkills')
    )
    BEGIN
        CREATE UNIQUE INDEX UX_HRK_HeroSkills_HeroTemplateId
            ON dbo.HRK_HeroSkills(HeroTemplateId);
        PRINT '-> Da tao Unique Index UX_HRK_HeroSkills_HeroTemplateId.';
    END;
END;

COMMIT TRANSACTION;
PRINT N'==> HOÀN TẤT MIGRATION HERO ATTRIBUTES & EQUIPMENT SUPPORT.';

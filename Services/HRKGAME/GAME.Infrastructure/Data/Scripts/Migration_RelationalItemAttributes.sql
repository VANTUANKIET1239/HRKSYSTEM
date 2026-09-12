-- =========================================================================================
-- HRK GAME DATABASE MIGRATION SCRIPT
-- Mục tiêu: Chuyển đổi từ BaseStats JSON sang mô hình quan hệ Relational Base Item + Attributes
-- Chuẩn hóa:
--   1. HRK_ItemCategories: 6 slot trang bị (WEAPON, ARMOR, HELMET, BOOTS, RING, ARTIFACT)
--      và các danh mục phi trang bị (HERO_SHARD, CONSUMABLE, MATERIAL)
--   2. HRK_AttributeTypes: 13 thuộc tính cốt lõi (HP, PHYSICAL_ATK, MAGIC_ATK, CRIT, DEF, SPD, v.v.)
--   3. HRK_CategoryAllowedAttributes: Ràng buộc thuộc tính chính/phụ theo slot trang bị
--   4. HRK_ItemTemplateAttributes: Nguồn sự thật (Source of Truth) cho chỉ số gốc của trang bị
--   5. HRK_ItemTemplates: Thêm Code (Unique), UpdatedOn, MetadataJson; Xóa Rarity text và BaseStats
--   6. HRK_PlayerInventory: Tính toán lại snapshot/cache CurrentStats JSON
-- =========================================================================================

SET NOCOUNT ON;

PRINT '=========================================================================================';
PRINT 'BẮT ĐẦU MIGRATION: RELATIONAL BASE ITEM + ATTRIBUTES MODEL';
PRINT '=========================================================================================';

BEGIN TRANSACTION;

BEGIN TRY

    -- -------------------------------------------------------------------------------------
    -- 1. CHUẨN HÓA BẢNG HRK_ItemCategories (Explicit Categories / Equipment Slots)
    -- -------------------------------------------------------------------------------------
    PRINT '1. Đang chuẩn hóa bảng HRK_ItemCategories...';

    -- Đảm bảo cột IsEquipment tồn tại
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HRK_ItemCategories]') AND name = 'IsEquipment')
    BEGIN
        ALTER TABLE [dbo].[HRK_ItemCategories] ADD [IsEquipment] BIT NOT NULL CONSTRAINT [DF_HRK_ItemCategories_IsEquipment] DEFAULT 0;
    END

    -- Cập nhật các danh mục trang bị hiện có về mã viết hoa chuẩn (WEAPON, ARMOR, HELMET, BOOTS, RING, ARTIFACT)
    UPDATE [dbo].[HRK_ItemCategories] SET [Code] = 'WEAPON',   [Name] = N'Vũ Khí',            [IsEquipment] = 1, [DisplayOrder] = 1 WHERE [Code] IN ('weapon', 'weapons', 'WEAPON');
    UPDATE [dbo].[HRK_ItemCategories] SET [Code] = 'ARMOR',    [Name] = N'Giáp Vai / Ngực',   [IsEquipment] = 1, [DisplayOrder] = 2 WHERE [Code] IN ('armor', 'armors', 'ARMOR');
    UPDATE [dbo].[HRK_ItemCategories] SET [Code] = 'HELMET',   [Name] = N'Mũ Khải Giáp',      [IsEquipment] = 1, [DisplayOrder] = 3 WHERE [Code] IN ('helmet', 'helmets', 'HELMET');
    UPDATE [dbo].[HRK_ItemCategories] SET [Code] = 'BOOTS',    [Name] = N'Giày Chiến Hài',    [IsEquipment] = 1, [DisplayOrder] = 4 WHERE [Code] IN ('boot', 'boots', 'BOOTS');
    UPDATE [dbo].[HRK_ItemCategories] SET [Code] = 'RING',     [Name] = N'Nhẫn Khảm Ngọc',    [IsEquipment] = 1, [DisplayOrder] = 5 WHERE [Code] IN ('ring', 'rings', 'RING');
    UPDATE [dbo].[HRK_ItemCategories] SET [Code] = 'ARTIFACT', [Name] = N'Thần Binh Pháp Bảo',[IsEquipment] = 1, [DisplayOrder] = 6 WHERE [Code] IN ('artifact', 'artifacts', 'ARTIFACT');

    -- Cập nhật các danh mục phi trang bị (HERO_SHARD, CONSUMABLE, MATERIAL, SKILL_BOOK, QUEST)
    UPDATE [dbo].[HRK_ItemCategories] SET [Code] = 'HERO_SHARD', [Name] = N'Mảnh Võ Tướng',       [IsEquipment] = 0, [DisplayOrder] = 7 WHERE [Code] IN ('hero_fragment', 'hero_fragments', 'HERO_SHARD');
    UPDATE [dbo].[HRK_ItemCategories] SET [Code] = 'CONSUMABLE', [Name] = N'Vật Phẩm Tiêu Hao',   [IsEquipment] = 0, [DisplayOrder] = 8 WHERE [Code] IN ('consumable', 'consumables', 'CONSUMABLE');
    UPDATE [dbo].[HRK_ItemCategories] SET [Code] = 'MATERIAL',   [Name] = N'Nguyên Liệu Rèn',     [IsEquipment] = 0, [DisplayOrder] = 9 WHERE [Code] IN ('material', 'materials', 'MATERIAL');
    UPDATE [dbo].[HRK_ItemCategories] SET [Code] = 'SKILL_BOOK', [Name] = N'Bí Kíp Kỹ Năng',      [IsEquipment] = 0, [DisplayOrder] = 10 WHERE [Code] IN ('skill_book', 'skill_books', 'SKILL_BOOK');
    UPDATE [dbo].[HRK_ItemCategories] SET [Code] = 'QUEST',      [Name] = N'Vật Phẩm Nhiệm Vụ',   [IsEquipment] = 0, [DisplayOrder] = 11 WHERE [Code] IN ('quest', 'quests', 'QUEST');

    -- Bổ sung nếu chưa tồn tại bất kỳ slot nào trong 6 slot trang bị
    IF NOT EXISTS (SELECT 1 FROM [dbo].[HRK_ItemCategories] WHERE [Code] = 'WEAPON')
        INSERT INTO [dbo].[HRK_ItemCategories] ([Code], [Name], [Icon], [DisplayOrder], [IsEquipment]) VALUES ('WEAPON', N'Vũ Khí', 'bi-sword', 1, 1);
    IF NOT EXISTS (SELECT 1 FROM [dbo].[HRK_ItemCategories] WHERE [Code] = 'ARMOR')
        INSERT INTO [dbo].[HRK_ItemCategories] ([Code], [Name], [Icon], [DisplayOrder], [IsEquipment]) VALUES ('ARMOR', N'Giáp Vai / Ngực', 'bi-suit-armor', 2, 1);
    IF NOT EXISTS (SELECT 1 FROM [dbo].[HRK_ItemCategories] WHERE [Code] = 'HELMET')
        INSERT INTO [dbo].[HRK_ItemCategories] ([Code], [Name], [Icon], [DisplayOrder], [IsEquipment]) VALUES ('HELMET', N'Mũ Khải Giáp', 'bi-shield-shaded', 3, 1);
    IF NOT EXISTS (SELECT 1 FROM [dbo].[HRK_ItemCategories] WHERE [Code] = 'BOOTS')
        INSERT INTO [dbo].[HRK_ItemCategories] ([Code], [Name], [Icon], [DisplayOrder], [IsEquipment]) VALUES ('BOOTS', N'Giày Chiến Hài', 'bi-archive-fill', 4, 1);
    IF NOT EXISTS (SELECT 1 FROM [dbo].[HRK_ItemCategories] WHERE [Code] = 'RING')
        INSERT INTO [dbo].[HRK_ItemCategories] ([Code], [Name], [Icon], [DisplayOrder], [IsEquipment]) VALUES ('RING', N'Nhẫn Khảm Ngọc', 'bi-gem', 5, 1);
    IF NOT EXISTS (SELECT 1 FROM [dbo].[HRK_ItemCategories] WHERE [Code] = 'ARTIFACT')
        INSERT INTO [dbo].[HRK_ItemCategories] ([Code], [Name], [Icon], [DisplayOrder], [IsEquipment]) VALUES ('ARTIFACT', N'Thần Binh Pháp Bảo', 'bi-trophy-fill', 6, 1);

    IF NOT EXISTS (SELECT 1 FROM [dbo].[HRK_ItemCategories] WHERE [Code] = 'HERO_SHARD')
        INSERT INTO [dbo].[HRK_ItemCategories] ([Code], [Name], [Icon], [DisplayOrder], [IsEquipment]) VALUES ('HERO_SHARD', N'Mảnh Võ Tướng', 'bi-people-fill', 7, 0);
    IF NOT EXISTS (SELECT 1 FROM [dbo].[HRK_ItemCategories] WHERE [Code] = 'CONSUMABLE')
        INSERT INTO [dbo].[HRK_ItemCategories] ([Code], [Name], [Icon], [DisplayOrder], [IsEquipment]) VALUES ('CONSUMABLE', N'Vật Phẩm Tiêu Hao', 'bi-droplet-fill', 8, 0);
    IF NOT EXISTS (SELECT 1 FROM [dbo].[HRK_ItemCategories] WHERE [Code] = 'MATERIAL')
        INSERT INTO [dbo].[HRK_ItemCategories] ([Code], [Name], [Icon], [DisplayOrder], [IsEquipment]) VALUES ('MATERIAL', N'Nguyên Liệu Rèn', 'bi-lightning-charge-fill', 9, 0);


    -- -------------------------------------------------------------------------------------
    -- 2. TẠO BẢNG HRK_AttributeTypes & SEED DỮ LIỆU
    -- -------------------------------------------------------------------------------------
    PRINT '2. Đang tạo bảng HRK_AttributeTypes...';

    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HRK_AttributeTypes')
    BEGIN
        CREATE TABLE [dbo].[HRK_AttributeTypes]
        (
            [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            [Code] NVARCHAR(50) NOT NULL UNIQUE,
            [Name] NVARCHAR(100) NOT NULL,
            [IsPercentage] BIT NOT NULL DEFAULT 0,
            [DisplayOrder] INT NOT NULL DEFAULT 0,
            [Description] NVARCHAR(255) NULL,
            [CreatedOn] DATETIME NOT NULL DEFAULT GETDATE(),
            [UpdatedOn] DATETIME NOT NULL DEFAULT GETDATE()
        );
    END

    -- Seed 13 thuộc tính cốt lõi
    MERGE [dbo].[HRK_AttributeTypes] AS Target
    USING (VALUES
        ('HP',           N'Sinh Mệnh',             0, 1,  N'Điểm máu cơ bản của nhân vật'),
        ('PHYSICAL_ATK', N'Sát Thương Vật Lý',     0, 2,  N'Gây sát thương vật lý lên kẻ địch'),
        ('MAGIC_ATK',    N'Sát Thương Phép',       0, 3,  N'Gây sát thương phép thuật lên kẻ địch'),
        ('CRIT_RATE',    N'Tỷ Lệ Chí Mạng',        1, 4,  N'Tỷ lệ đòn đánh phát kích sát thương chí mạng (0.05 = 5%)'),
        ('CRIT_DAMAGE',  N'Sát Thương Chí Mạng',   1, 5,  N'Hệ số sát thương gây ra khi bạo kích (1.50 = 150%)'),
        ('ACCURACY',     N'Chính Xác',             1, 6,  N'Khả năng đánh trúng mục tiêu (0.80 = 80%)'),
        ('ARMOR_PEN',    N'Xuyên Giáp',            0, 7,  N'Bỏ qua giáp vật lý của mục tiêu'),
        ('MAGIC_PEN',    N'Xuyên Kháng Phép',      0, 8,  N'Bỏ qua kháng phép của mục tiêu'),
        ('ARMOR',        N'Giáp Vật Lý',           0, 9,  N'Giảm trừ sát thương vật lý nhận vào'),
        ('MAGIC_RESIST', N'Kháng Phép',            0, 10, N'Giảm trừ sát thương phép thuật nhận vào'),
        ('DODGE_RATE',   N'Tỷ Lệ Né Tránh',        1, 11, N'Tỷ lệ né hoàn toàn đòn tấn công của đối thủ'),
        ('CRIT_RESIST',  N'Kháng Chí Mạng',        1, 12, N'Giảm tỷ lệ bị phát kích chí mạng từ đối thủ'),
        ('SPEED',        N'Tốc Độ Ra Chiêu',       0, 13, N'Quyết định thứ tự hành động trong lượt đấu')
    ) AS Source ([Code], [Name], [IsPercentage], [DisplayOrder], [Description])
    ON Target.[Code] = Source.[Code]
    WHEN MATCHED THEN
        UPDATE SET Target.[Name] = Source.[Name],
                   Target.[IsPercentage] = Source.[IsPercentage],
                   Target.[DisplayOrder] = Source.[DisplayOrder],
                   Target.[Description] = Source.[Description],
                   Target.[UpdatedOn] = GETDATE()
    WHEN NOT MATCHED THEN
        INSERT ([Code], [Name], [IsPercentage], [DisplayOrder], [Description])
        VALUES (Source.[Code], Source.[Name], Source.[IsPercentage], Source.[DisplayOrder], Source.[Description]);


    -- -------------------------------------------------------------------------------------
    -- 3. TẠO BẢNG HRK_CategoryAllowedAttributes & SEED QUY TẮC RÀNG BUỘC
    -- -------------------------------------------------------------------------------------
    PRINT '3. Đang tạo bảng HRK_CategoryAllowedAttributes và seed quy tắc cấu hình...';

    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HRK_CategoryAllowedAttributes')
    BEGIN
        CREATE TABLE [dbo].[HRK_CategoryAllowedAttributes]
        (
            [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            [CategoryId] INT NOT NULL,
            [AttributeTypeId] INT NOT NULL,
            [IsMainStat] BIT NOT NULL DEFAULT 0,
            [IsSubStat] BIT NOT NULL DEFAULT 0,
            [MinValue] DECIMAL(18,4) NULL,
            [MaxValue] DECIMAL(18,4) NULL,
            [DisplayOrder] INT NOT NULL DEFAULT 0,

            CONSTRAINT [FK_HRK_CategoryAllowedAttributes_Category]
                FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[HRK_ItemCategories]([Id]) ON DELETE CASCADE,

            CONSTRAINT [FK_HRK_CategoryAllowedAttributes_AttributeType]
                FOREIGN KEY ([AttributeTypeId]) REFERENCES [dbo].[HRK_AttributeTypes]([Id]),

            CONSTRAINT [UQ_HRK_CategoryAllowedAttributes]
                UNIQUE ([CategoryId], [AttributeTypeId])
        );
    END

    -- Seed cấu hình quy tắc cho 6 slot trang bị
    DECLARE @CatWeapon INT = (SELECT [Id] FROM [dbo].[HRK_ItemCategories] WHERE [Code] = 'WEAPON');
    DECLARE @CatArmor INT = (SELECT [Id] FROM [dbo].[HRK_ItemCategories] WHERE [Code] = 'ARMOR');
    DECLARE @CatHelmet INT = (SELECT [Id] FROM [dbo].[HRK_ItemCategories] WHERE [Code] = 'HELMET');
    DECLARE @CatBoots INT = (SELECT [Id] FROM [dbo].[HRK_ItemCategories] WHERE [Code] = 'BOOTS');
    DECLARE @CatRing INT = (SELECT [Id] FROM [dbo].[HRK_ItemCategories] WHERE [Code] = 'RING');
    DECLARE @CatArtifact INT = (SELECT [Id] FROM [dbo].[HRK_ItemCategories] WHERE [Code] = 'ARTIFACT');

    -- WEAPON (Main: PHYSICAL_ATK, HP; Sub: CRIT_RATE, CRIT_DAMAGE, ACCURACY, ARMOR_PEN, SPEED)
    IF @CatWeapon IS NOT NULL
    BEGIN
        MERGE [dbo].[HRK_CategoryAllowedAttributes] AS Target
        USING (
            SELECT @CatWeapon AS [CatId], a.[Id] AS [AttrId],
                   CASE WHEN a.[Code] IN ('PHYSICAL_ATK', 'HP') THEN 1 ELSE 0 END AS [Main],
                   CASE WHEN a.[Code] IN ('CRIT_RATE', 'CRIT_DAMAGE', 'ACCURACY', 'ARMOR_PEN', 'SPEED') THEN 1 ELSE 0 END AS [Sub]
            FROM [dbo].[HRK_AttributeTypes] a
            WHERE a.[Code] IN ('PHYSICAL_ATK', 'HP', 'CRIT_RATE', 'CRIT_DAMAGE', 'ACCURACY', 'ARMOR_PEN', 'SPEED')
        ) AS Source ON Target.[CategoryId] = Source.[CatId] AND Target.[AttributeTypeId] = Source.[AttrId]
        WHEN MATCHED THEN UPDATE SET Target.[IsMainStat] = Source.[Main], Target.[IsSubStat] = Source.[Sub]
        WHEN NOT MATCHED THEN INSERT ([CategoryId], [AttributeTypeId], [IsMainStat], [IsSubStat]) VALUES (Source.[CatId], Source.[AttrId], Source.[Main], Source.[Sub]);
    END

    -- ARTIFACT (Main: MAGIC_ATK, HP; Sub: CRIT_RATE, CRIT_DAMAGE, MAGIC_PEN, SPEED)
    IF @CatArtifact IS NOT NULL
    BEGIN
        MERGE [dbo].[HRK_CategoryAllowedAttributes] AS Target
        USING (
            SELECT @CatArtifact AS [CatId], a.[Id] AS [AttrId],
                   CASE WHEN a.[Code] IN ('MAGIC_ATK', 'HP') THEN 1 ELSE 0 END AS [Main],
                   CASE WHEN a.[Code] IN ('CRIT_RATE', 'CRIT_DAMAGE', 'MAGIC_PEN', 'SPEED') THEN 1 ELSE 0 END AS [Sub]
            FROM [dbo].[HRK_AttributeTypes] a
            WHERE a.[Code] IN ('MAGIC_ATK', 'HP', 'CRIT_RATE', 'CRIT_DAMAGE', 'MAGIC_PEN', 'SPEED')
        ) AS Source ON Target.[CategoryId] = Source.[CatId] AND Target.[AttributeTypeId] = Source.[AttrId]
        WHEN MATCHED THEN UPDATE SET Target.[IsMainStat] = Source.[Main], Target.[IsSubStat] = Source.[Sub]
        WHEN NOT MATCHED THEN INSERT ([CategoryId], [AttributeTypeId], [IsMainStat], [IsSubStat]) VALUES (Source.[CatId], Source.[AttrId], Source.[Main], Source.[Sub]);
    END

    -- ARMOR (Main: ARMOR, HP; Sub: MAGIC_RESIST, DODGE_RATE, CRIT_RESIST)
    IF @CatArmor IS NOT NULL
    BEGIN
        MERGE [dbo].[HRK_CategoryAllowedAttributes] AS Target
        USING (
            SELECT @CatArmor AS [CatId], a.[Id] AS [AttrId],
                   CASE WHEN a.[Code] IN ('ARMOR', 'HP') THEN 1 ELSE 0 END AS [Main],
                   CASE WHEN a.[Code] IN ('MAGIC_RESIST', 'DODGE_RATE', 'CRIT_RESIST') THEN 1 ELSE 0 END AS [Sub]
            FROM [dbo].[HRK_AttributeTypes] a
            WHERE a.[Code] IN ('ARMOR', 'HP', 'MAGIC_RESIST', 'DODGE_RATE', 'CRIT_RESIST')
        ) AS Source ON Target.[CategoryId] = Source.[CatId] AND Target.[AttributeTypeId] = Source.[AttrId]
        WHEN MATCHED THEN UPDATE SET Target.[IsMainStat] = Source.[Main], Target.[IsSubStat] = Source.[Sub]
        WHEN NOT MATCHED THEN INSERT ([CategoryId], [AttributeTypeId], [IsMainStat], [IsSubStat]) VALUES (Source.[CatId], Source.[AttrId], Source.[Main], Source.[Sub]);
    END

    -- HELMET (Main: MAGIC_RESIST, HP; Sub: ARMOR, DODGE_RATE, CRIT_RESIST)
    IF @CatHelmet IS NOT NULL
    BEGIN
        MERGE [dbo].[HRK_CategoryAllowedAttributes] AS Target
        USING (
            SELECT @CatHelmet AS [CatId], a.[Id] AS [AttrId],
                   CASE WHEN a.[Code] IN ('MAGIC_RESIST', 'HP') THEN 1 ELSE 0 END AS [Main],
                   CASE WHEN a.[Code] IN ('ARMOR', 'DODGE_RATE', 'CRIT_RESIST') THEN 1 ELSE 0 END AS [Sub]
            FROM [dbo].[HRK_AttributeTypes] a
            WHERE a.[Code] IN ('MAGIC_RESIST', 'HP', 'ARMOR', 'DODGE_RATE', 'CRIT_RESIST')
        ) AS Source ON Target.[CategoryId] = Source.[CatId] AND Target.[AttributeTypeId] = Source.[AttrId]
        WHEN MATCHED THEN UPDATE SET Target.[IsMainStat] = Source.[Main], Target.[IsSubStat] = Source.[Sub]
        WHEN NOT MATCHED THEN INSERT ([CategoryId], [AttributeTypeId], [IsMainStat], [IsSubStat]) VALUES (Source.[CatId], Source.[AttrId], Source.[Main], Source.[Sub]);
    END

    -- BOOTS (Main: SPEED, HP; Sub: DODGE_RATE, ACCURACY, ARMOR, MAGIC_RESIST)
    IF @CatBoots IS NOT NULL
    BEGIN
        MERGE [dbo].[HRK_CategoryAllowedAttributes] AS Target
        USING (
            SELECT @CatBoots AS [CatId], a.[Id] AS [AttrId],
                   CASE WHEN a.[Code] IN ('SPEED', 'HP') THEN 1 ELSE 0 END AS [Main],
                   CASE WHEN a.[Code] IN ('DODGE_RATE', 'ACCURACY', 'ARMOR', 'MAGIC_RESIST') THEN 1 ELSE 0 END AS [Sub]
            FROM [dbo].[HRK_AttributeTypes] a
            WHERE a.[Code] IN ('SPEED', 'HP', 'DODGE_RATE', 'ACCURACY', 'ARMOR', 'MAGIC_RESIST')
        ) AS Source ON Target.[CategoryId] = Source.[CatId] AND Target.[AttributeTypeId] = Source.[AttrId]
        WHEN MATCHED THEN UPDATE SET Target.[IsMainStat] = Source.[Main], Target.[IsSubStat] = Source.[Sub]
        WHEN NOT MATCHED THEN INSERT ([CategoryId], [AttributeTypeId], [IsMainStat], [IsSubStat]) VALUES (Source.[CatId], Source.[AttrId], Source.[Main], Source.[Sub]);
    END

    -- RING (Main or Sub: CRIT_RATE, CRIT_DAMAGE, DODGE_RATE, ACCURACY, ARMOR_PEN, MAGIC_PEN, HP)
    IF @CatRing IS NOT NULL
    BEGIN
        MERGE [dbo].[HRK_CategoryAllowedAttributes] AS Target
        USING (
            SELECT @CatRing AS [CatId], a.[Id] AS [AttrId],
                   CASE WHEN a.[Code] IN ('CRIT_RATE', 'HP') THEN 1 ELSE 0 END AS [Main],
                   CASE WHEN a.[Code] IN ('CRIT_DAMAGE', 'DODGE_RATE', 'ACCURACY', 'ARMOR_PEN', 'MAGIC_PEN') THEN 1 ELSE 0 END AS [Sub]
            FROM [dbo].[HRK_AttributeTypes] a
            WHERE a.[Code] IN ('CRIT_RATE', 'CRIT_DAMAGE', 'DODGE_RATE', 'ACCURACY', 'ARMOR_PEN', 'MAGIC_PEN', 'HP')
        ) AS Source ON Target.[CategoryId] = Source.[CatId] AND Target.[AttributeTypeId] = Source.[AttrId]
        WHEN MATCHED THEN UPDATE SET Target.[IsMainStat] = Source.[Main], Target.[IsSubStat] = Source.[Sub]
        WHEN NOT MATCHED THEN INSERT ([CategoryId], [AttributeTypeId], [IsMainStat], [IsSubStat]) VALUES (Source.[CatId], Source.[AttrId], Source.[Main], Source.[Sub]);
    END


    -- -------------------------------------------------------------------------------------
    -- 4. TẠO BẢNG HRK_ItemTemplateAttributes (Source of Truth for Base Equipment Stats)
    -- -------------------------------------------------------------------------------------
    PRINT '4. Đang tạo bảng HRK_ItemTemplateAttributes...';

    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HRK_ItemTemplateAttributes')
    BEGIN
        CREATE TABLE [dbo].[HRK_ItemTemplateAttributes]
        (
            [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            [ItemTemplateId] INT NOT NULL,
            [AttributeTypeId] INT NOT NULL,
            [Value] DECIMAL(18,4) NOT NULL,

            CONSTRAINT [FK_HRK_ItemTemplateAttributes_ItemTemplate]
                FOREIGN KEY ([ItemTemplateId]) REFERENCES [dbo].[HRK_ItemTemplates]([Id]) ON DELETE CASCADE,

            CONSTRAINT [FK_HRK_ItemTemplateAttributes_AttributeType]
                FOREIGN KEY ([AttributeTypeId]) REFERENCES [dbo].[HRK_AttributeTypes]([Id]),

            CONSTRAINT [UQ_HRK_ItemTemplateAttributes]
                UNIQUE ([ItemTemplateId], [AttributeTypeId])
        );
    END


    -- -------------------------------------------------------------------------------------
    -- 5. BỔ SUNG CỘT Code, UpdatedOn, MetadataJson VÀO HRK_ItemTemplates
    -- -------------------------------------------------------------------------------------
    PRINT '5. Đang bổ sung Code, UpdatedOn, MetadataJson vào HRK_ItemTemplates...';

    -- Thêm Code nếu chưa có
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HRK_ItemTemplates]') AND name = 'Code')
    BEGIN
        ALTER TABLE [dbo].[HRK_ItemTemplates] ADD [Code] NVARCHAR(100) NULL;
    END

    -- Thêm UpdatedOn nếu chưa có
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HRK_ItemTemplates]') AND name = 'UpdatedOn')
    BEGIN
        ALTER TABLE [dbo].[HRK_ItemTemplates] ADD [UpdatedOn] DATETIME NOT NULL CONSTRAINT [DF_HRK_ItemTemplates_UpdatedOn] DEFAULT GETDATE();
    END

    -- Thêm MetadataJson nếu chưa có
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HRK_ItemTemplates]') AND name = 'MetadataJson')
    BEGIN
        ALTER TABLE [dbo].[HRK_ItemTemplates] ADD [MetadataJson] NVARCHAR(MAX) NULL;
    END

    -- Sử dụng dynamic SQL để cập nhật Code nhằm tránh lỗi biên dịch Msg 207 (do cột Code vừa được ALTER ADD trong cùng batch)
    EXEC sp_executesql N'
        UPDATE t
        SET t.[Code] = UPPER(c.[Code]) + ''_'' + CAST(t.[Id] AS NVARCHAR(10)) + ''_'' + 
                       REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(t.[Name], '' '', ''_''), ''('', ''''), '')'', ''''), ''-'', ''_''), ''__'', ''_''),
            t.[UpdatedOn] = GETDATE()
        FROM [dbo].[HRK_ItemTemplates] t
        INNER JOIN [dbo].[HRK_ItemCategories] c ON t.[CategoryId] = c.[Id]
        WHERE t.[Code] IS NULL OR t.[Code] = '''';

        UPDATE [dbo].[HRK_ItemTemplates] SET [Code] = ''WEAPON_BERSERKER_GREATSWORD'' WHERE [Id] = 101 AND ([Code] IS NULL OR [Code] LIKE ''%101%'');
        UPDATE [dbo].[HRK_ItemTemplates] SET [Code] = ''WEAPON_SHADOW_DAGGER''        WHERE [Id] = 102 AND ([Code] IS NULL OR [Code] LIKE ''%102%'');
        UPDATE [dbo].[HRK_ItemTemplates] SET [Code] = ''WEAPON_FIRE_DRAGON_SPEAR''     WHERE [Id] = 103 AND ([Code] IS NULL OR [Code] LIKE ''%103%'');
        UPDATE [dbo].[HRK_ItemTemplates] SET [Code] = ''WEAPON_RUSTED_SWORD''          WHERE [Id] = 104 AND ([Code] IS NULL OR [Code] LIKE ''%104%'');
        UPDATE [dbo].[HRK_ItemTemplates] SET [Code] = ''ARMOR_BLACK_IRON''             WHERE [Id] = 201 AND ([Code] IS NULL OR [Code] LIKE ''%201%'');
        UPDATE [dbo].[HRK_ItemTemplates] SET [Code] = ''ARMOR_NINE_HEAVENS''           WHERE [Id] = 202 AND ([Code] IS NULL OR [Code] LIKE ''%202%'');
        UPDATE [dbo].[HRK_ItemTemplates] SET [Code] = ''ARMOR_RED_DRAGON_SCALE''       WHERE [Id] = 203 AND ([Code] IS NULL OR [Code] LIKE ''%203%'');
        UPDATE [dbo].[HRK_ItemTemplates] SET [Code] = ''HELMET_FLOATING_CLOUD''        WHERE [Id] = 301 AND ([Code] IS NULL OR [Code] LIKE ''%301%'');
        UPDATE [dbo].[HRK_ItemTemplates] SET [Code] = ''HELMET_GOLDEN_PHOENIX''        WHERE [Id] = 302 AND ([Code] IS NULL OR [Code] LIKE ''%302%'');
        UPDATE [dbo].[HRK_ItemTemplates] SET [Code] = ''BOOTS_SOLDIER_WAR''            WHERE [Id] = 401 AND ([Code] IS NULL OR [Code] LIKE ''%401%'');
        UPDATE [dbo].[HRK_ItemTemplates] SET [Code] = ''BOOTS_GALE_WIND''              WHERE [Id] = 402 AND ([Code] IS NULL OR [Code] LIKE ''%402%'');
        UPDATE [dbo].[HRK_ItemTemplates] SET [Code] = ''RING_JADE_EMERALD''            WHERE [Id] = 501 AND ([Code] IS NULL OR [Code] LIKE ''%501%'');
        UPDATE [dbo].[HRK_ItemTemplates] SET [Code] = ''RING_DRAGON_EYE_DIVINE''       WHERE [Id] = 502 AND ([Code] IS NULL OR [Code] LIKE ''%502%'');
        UPDATE [dbo].[HRK_ItemTemplates] SET [Code] = ''ARTIFACT_IMPERIAL_SEAL''       WHERE [Id] = 601 AND ([Code] IS NULL OR [Code] LIKE ''%601%'');
        UPDATE [dbo].[HRK_ItemTemplates] SET [Code] = ''ARTIFACT_SEVEN_STARS_MIRROR''  WHERE [Id] = 602 AND ([Code] IS NULL OR [Code] LIKE ''%602%'');
    ';

    -- Đặt Code thành NOT NULL và tạo UNIQUE INDEX
    EXEC('ALTER TABLE [dbo].[HRK_ItemTemplates] ALTER COLUMN [Code] NVARCHAR(100) NOT NULL;');

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_HRK_ItemTemplates_Code' AND object_id = OBJECT_ID(N'[dbo].[HRK_ItemTemplates]'))
    BEGIN
        EXEC('CREATE UNIQUE NONCLUSTERED INDEX [UQ_HRK_ItemTemplates_Code] ON [dbo].[HRK_ItemTemplates]([Code]);');
    END


    -- -------------------------------------------------------------------------------------
    -- 6. DI TRÚ DỮ LIỆU BaseStats JSON SANG HRK_ItemTemplateAttributes
    -- -------------------------------------------------------------------------------------
    PRINT '6. Đang di trú dữ liệu BaseStats JSON sang HRK_ItemTemplateAttributes...';

    -- Kiểm tra xem cột BaseStats có tồn tại để trích xuất không
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HRK_ItemTemplates]') AND name = 'BaseStats')
    BEGIN
        -- Sao chép BaseStats cũ vào MetadataJson để không bao giờ mất thông tin phi cấu trúc
        EXEC('
            UPDATE [dbo].[HRK_ItemTemplates]
            SET [MetadataJson] = [BaseStats]
            WHERE [BaseStats] IS NOT NULL AND [MetadataJson] IS NULL;
        ');

        -- Định nghĩa bảng tạm chứa giá trị JSON đã phân tách
        ;WITH ParsedStats AS (
            SELECT 
                t.[Id] AS [ItemTemplateId],
                LOWER(j.[key]) AS [StatKey],
                TRY_CAST(j.[value] AS DECIMAL(18,4)) AS [StatVal]
            FROM [dbo].[HRK_ItemTemplates] t
            CROSS APPLY OPENJSON(t.[BaseStats]) j
            WHERE t.[BaseStats] IS NOT NULL 
              AND ISJSON(t.[BaseStats]) = 1
              AND TRY_CAST(j.[value] AS DECIMAL(18,4)) IS NOT NULL
        ),
        MappedAttributes AS (
            SELECT 
                ps.[ItemTemplateId],
                CASE 
                    WHEN ps.[StatKey] IN ('atk', 'physical_atk', 'physicalatk') THEN 'PHYSICAL_ATK'
                    WHEN ps.[StatKey] IN ('matk', 'magic_atk', 'magicatk')       THEN 'MAGIC_ATK'
                    WHEN ps.[StatKey] IN ('def', 'armor')                        THEN 'ARMOR'
                    WHEN ps.[StatKey] IN ('hp', 'health')                        THEN 'HP'
                    WHEN ps.[StatKey] IN ('crit', 'critrate', 'crit_rate')       THEN 'CRIT_RATE'
                    WHEN ps.[StatKey] IN ('critdmg', 'crit_damage')              THEN 'CRIT_DAMAGE'
                    WHEN ps.[StatKey] IN ('spd', 'speed')                        THEN 'SPEED'
                    WHEN ps.[StatKey] IN ('accuracy')                            THEN 'ACCURACY'
                    WHEN ps.[StatKey] IN ('armorpen', 'armor_pen')               THEN 'ARMOR_PEN'
                    WHEN ps.[StatKey] IN ('magicpen', 'magic_pen')               THEN 'MAGIC_PEN'
                    WHEN ps.[StatKey] IN ('magicresist', 'magic_resist')         THEN 'MAGIC_RESIST'
                    WHEN ps.[StatKey] IN ('dodge', 'dodgerate', 'dodge_rate')    THEN 'DODGE_RATE'
                    WHEN ps.[StatKey] IN ('critresist', 'crit_resist')           THEN 'CRIT_RESIST'
                    ELSE NULL
                END AS [AttrCode],
                -- Chuyển đổi % (nếu giá trị nhập dạng 15% -> 0.15 cho CRIT_RATE, ACCURACY, DODGE_RATE)
                CASE 
                    WHEN ps.[StatKey] IN ('crit', 'critrate', 'crit_rate', 'accuracy', 'dodge', 'dodgerate', 'dodge_rate', 'critresist', 'crit_resist') 
                         AND ps.[StatVal] > 1.0 THEN ps.[StatVal] / 100.0
                    WHEN ps.[StatKey] IN ('critdmg', 'crit_damage') AND ps.[StatVal] > 2.0 THEN ps.[StatVal] / 100.0
                    ELSE ps.[StatVal]
                END AS [AttrValue]
            FROM ParsedStats ps
        )
        INSERT INTO [dbo].[HRK_ItemTemplateAttributes] ([ItemTemplateId], [AttributeTypeId], [Value])
        SELECT 
            m.[ItemTemplateId],
            a.[Id],
            m.[AttrValue]
        FROM MappedAttributes m
        INNER JOIN [dbo].[HRK_AttributeTypes] a ON a.[Code] = m.[AttrCode]
        WHERE m.[AttrCode] IS NOT NULL
          AND NOT EXISTS (
              SELECT 1 FROM [dbo].[HRK_ItemTemplateAttributes] ita
              WHERE ita.[ItemTemplateId] = m.[ItemTemplateId] AND ita.[AttributeTypeId] = a.[Id]
          );

        PRINT N'Đã hoàn tất trích xuất BaseStats JSON sang HRK_ItemTemplateAttributes.';
    END


    -- -------------------------------------------------------------------------------------
    -- 7. LOẠI BỎ CỘT Rarity TEXT VÀ CỘT BaseStats TRONG HRK_ItemTemplates
    -- -------------------------------------------------------------------------------------
    PRINT '7. Đang dọn dẹp cột Rarity text và BaseStats trong HRK_ItemTemplates...';

    -- Đồng bộ RarityId nếu có dòng nào chưa có
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HRK_ItemTemplates]') AND name = 'Rarity')
    BEGIN
        UPDATE t
        SET t.[RarityId] = r.[Id]
        FROM [dbo].[HRK_ItemTemplates] t
        INNER JOIN [dbo].[HRK_Rarities] r ON LOWER(t.[Rarity]) = LOWER(r.[Code]) OR LOWER(t.[Rarity]) = LOWER(r.[Name])
        WHERE t.[RarityId] IS NULL;

        -- Gỡ bỏ CHECK CONSTRAINTS trên cột Rarity
        DECLARE @ChkName NVARCHAR(255);
        DECLARE curChk CURSOR FOR 
            SELECT name FROM sys.check_constraints 
            WHERE parent_object_id = OBJECT_ID(N'[dbo].[HRK_ItemTemplates]') 
              AND definition LIKE '%Rarity%';
        OPEN curChk;
        FETCH NEXT FROM curChk INTO @ChkName;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            EXEC('ALTER TABLE [dbo].[HRK_ItemTemplates] DROP CONSTRAINT [' + @ChkName + ']');
            FETCH NEXT FROM curChk INTO @ChkName;
        END
        CLOSE curChk;
        DEALLOCATE curChk;

        -- Gỡ bỏ DEFAULT CONSTRAINTS trên cột Rarity
        DECLARE @DefName NVARCHAR(255);
        SELECT @DefName = d.name
        FROM sys.default_constraints d
        INNER JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
        WHERE d.parent_object_id = OBJECT_ID(N'[dbo].[HRK_ItemTemplates]') AND c.name = 'Rarity';

        IF @DefName IS NOT NULL
        BEGIN
            EXEC('ALTER TABLE [dbo].[HRK_ItemTemplates] DROP CONSTRAINT [' + @DefName + ']');
        END

        -- Xóa cột Rarity
        ALTER TABLE [dbo].[HRK_ItemTemplates] DROP COLUMN [Rarity];
        PRINT N'Đã xóa cột Rarity text trùng lặp khỏi HRK_ItemTemplates.';
    END

    -- Gỡ bỏ cột BaseStats sau khi đã sao lưu sang MetadataJson & HRK_ItemTemplateAttributes
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HRK_ItemTemplates]') AND name = 'BaseStats')
    BEGIN
        DECLARE @DefBaseStats NVARCHAR(255);
        SELECT @DefBaseStats = d.name
        FROM sys.default_constraints d
        INNER JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
        WHERE d.parent_object_id = OBJECT_ID(N'[dbo].[HRK_ItemTemplates]') AND c.name = 'BaseStats';

        IF @DefBaseStats IS NOT NULL
        BEGIN
            EXEC('ALTER TABLE [dbo].[HRK_ItemTemplates] DROP CONSTRAINT [' + @DefBaseStats + ']');
        END

        ALTER TABLE [dbo].[HRK_ItemTemplates] DROP COLUMN [BaseStats];
        PRINT N'Đã xóa cột BaseStats khỏi HRK_ItemTemplates (dữ liệu phi chỉ số được lưu trong MetadataJson).';
    END


    -- -------------------------------------------------------------------------------------
    -- 8. TÍNH TOÁN LẠI CurrentStats JSON TRONG HRK_PlayerInventory
    -- -------------------------------------------------------------------------------------
    PRINT '8. Đang cập nhật lại CurrentStats JSON snapshot trong HRK_PlayerInventory...';

    -- Cập nhật snapshot CurrentStats cho tất cả các trang bị trong hành trang
    ;WITH CalcStats AS (
        SELECT 
            inv.[Id] AS [InvId],
            inv.[Enhancement],
            inv.[Stars],
            -- Tạo chuỗi JSON snapshot từ các thuộc tính HRK_ItemTemplateAttributes
            '{' + STRING_AGG('"' + a.[Code] + '":' + 
                CAST(
                    CASE 
                        WHEN a.[IsPercentage] = 1 THEN 
                            ROUND(ita.[Value] * (1.0 + (inv.[Enhancement] * 0.02)), 4)
                        ELSE 
                            ROUND(ita.[Value] * (1.0 + (inv.[Enhancement] * 0.08)) * (1.0 + (inv.[Stars] * 0.10)), 2)
                    END 
                AS NVARCHAR(30)), ',') + '}' AS [CalculatedJson]
        FROM [dbo].[HRK_PlayerInventory] inv
        INNER JOIN [dbo].[HRK_ItemTemplates] t ON inv.[ItemTemplateId] = t.[Id]
        INNER JOIN [dbo].[HRK_ItemCategories] c ON t.[CategoryId] = c.[Id]
        INNER JOIN [dbo].[HRK_ItemTemplateAttributes] ita ON ita.[ItemTemplateId] = t.[Id]
        INNER JOIN [dbo].[HRK_AttributeTypes] a ON ita.[AttributeTypeId] = a.[Id]
        WHERE c.[IsEquipment] = 1
        GROUP BY inv.[Id], inv.[Enhancement], inv.[Stars]
    )
    UPDATE inv
    SET inv.[CurrentStats] = cs.[CalculatedJson],
        inv.[UpdatedOn] = GETDATE()
    FROM [dbo].[HRK_PlayerInventory] inv
    INNER JOIN CalcStats cs ON inv.[Id] = cs.[InvId];

    PRINT N'Đã cập nhật snapshot CurrentStats cho toàn bộ trang bị người chơi.';

    COMMIT TRANSACTION;

    PRINT '=========================================================================================';
    PRINT N'MIGRATION THÀNH CÔNG! ĐÃ HOÀN TẤT TOÀN BỘ BƯỚC THAY ĐỔI SCHEMA VÀ DI TRÚ DỮ LIỆU.';
    PRINT '=========================================================================================';

    -- -------------------------------------------------------------------------------------
    -- 9. TRUY VẤN KIỂM TRA BÁO CÁO SAU KHI MIGRATION
    -- -------------------------------------------------------------------------------------
    EXEC('
        SELECT ''HRK_AttributeTypes'' AS [Table], COUNT(*) AS [TotalRows] FROM [dbo].[HRK_AttributeTypes]
        UNION ALL
        SELECT ''HRK_CategoryAllowedAttributes'', COUNT(*) FROM [dbo].[HRK_CategoryAllowedAttributes]
        UNION ALL
        SELECT ''HRK_ItemTemplateAttributes'', COUNT(*) FROM [dbo].[HRK_ItemTemplateAttributes]
        UNION ALL
        SELECT ''HRK_ItemTemplates (with Code)'', COUNT(*) FROM [dbo].[HRK_ItemTemplates] WHERE [Code] IS NOT NULL
        UNION ALL
        SELECT ''HRK_PlayerInventory (with CurrentStats)'', COUNT(*) FROM [dbo].[HRK_PlayerInventory] WHERE [CurrentStats] IS NOT NULL;
    ');

    -- Hiển thị mẫu một số trang bị và các thuộc tính quan hệ đã di trú
    EXEC('
        SELECT TOP 10 
            t.[Id] AS [TemplateId],
            t.[Code] AS [ItemCode],
            t.[Name] AS [ItemName],
            c.[Code] AS [Category],
            r.[Name] AS [Rarity],
            a.[Code] AS [AttributeCode],
            a.[Name] AS [AttributeName],
            ita.[Value] AS [BaseValue],
            a.[IsPercentage]
        FROM [dbo].[HRK_ItemTemplates] t
        INNER JOIN [dbo].[HRK_ItemCategories] c ON t.[CategoryId] = c.[Id]
        INNER JOIN [dbo].[HRK_Rarities] r ON t.[RarityId] = r.[Id]
        LEFT JOIN [dbo].[HRK_ItemTemplateAttributes] ita ON ita.[ItemTemplateId] = t.[Id]
        LEFT JOIN [dbo].[HRK_AttributeTypes] a ON ita.[AttributeTypeId] = a.[Id]
        ORDER BY t.[CategoryId], t.[Id], ita.[AttributeTypeId];
    ');

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();

    RAISERROR(N'Lỗi thực thi Migration: %s', @ErrSeverity, @ErrState, @ErrMsg);
END CATCH;
GO

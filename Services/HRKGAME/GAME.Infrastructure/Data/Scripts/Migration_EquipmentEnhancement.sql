-- =========================================================================================
-- HRK GAME DATABASE MIGRATION SCRIPT: EQUIPMENT ENHANCEMENT & FORGE SYSTEM (HỆ THỐNG CƯỜNG HÓA & LÒ RÈN)
-- =========================================================================================
-- Nội dung migration:
-- 1. Bảng [HRK_EnhancementLevelConfigs]: Cấu hình 15 cấp cường hóa (+0 -> +15), tỷ lệ cơ bản, chi phí vàng, quy tắc tụt cấp khi thất bại.
-- 2. Bảng [HRK_EnhancementMaterials]: Cấu hình đá cường hóa (5 cấp) và các loại bùa (Bùa May Mắn, Đại Bùa, Bùa Hộ Mệnh).
-- 3. Bảng [HRK_EquipmentEnhancementHistory]: Nhật ký kiểm toán nghiệp vụ cường hóa (Business Audit Log), chống trùng lặp (Idempotency theo RequestId).
-- 4. Seed dữ liệu Item Templates: 5 loại đá cường hóa và 3 loại bùa trong [HRK_ItemTemplates].
-- 5. Seed dữ liệu cấu hình nguyên liệu vào [HRK_EnhancementMaterials].
-- 6. Tặng đá cường hóa, bùa hộ mệnh và vàng mẫu cho người chơi để kiểm thử tính năng ngay lập tức.
-- =========================================================================================

SET NOCOUNT ON;

PRINT '=========================================================================================';
PRINT N'BẮT ĐẦU MIGRATION: HỆ THỐNG CƯỜNG HÓA TRANG BỊ & LÒ RÈN (EQUIPMENT ENHANCEMENT)';
PRINT '=========================================================================================';

BEGIN TRANSACTION;

BEGIN TRY

    -- -------------------------------------------------------------------------------------
    -- 1. TẠO BẢNG HRK_EnhancementLevelConfigs
    -- -------------------------------------------------------------------------------------
    PRINT N'1. Kiểm tra và tạo bảng [HRK_EnhancementLevelConfigs]...';

    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HRK_EnhancementLevelConfigs')
    BEGIN
        CREATE TABLE [dbo].[HRK_EnhancementLevelConfigs]
        (
            [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            [CurrentLevel] INT NOT NULL,
            [NextLevel] INT NOT NULL,
            [BaseSuccessRate] DECIMAL(8,4) NOT NULL,
            [GoldCost] INT NOT NULL,
            [FailureDropLevels] INT NOT NULL DEFAULT 0,
            [MaxStoneSlots] INT NOT NULL DEFAULT 3,
            [CreatedOn] DATETIME NOT NULL DEFAULT GETDATE(),
            [UpdatedOn] DATETIME NOT NULL DEFAULT GETDATE(),
            CONSTRAINT [UQ_HRK_EnhancementLevelConfigs_CurrentLevel] UNIQUE ([CurrentLevel])
        );
        PRINT N'Đã tạo bảng [HRK_EnhancementLevelConfigs].';
    END

    -- Seed 15 cấp độ cường hóa chuẩn xác (+0 -> +15)
    -- Quy tắc tụt cấp:
    --   Cấp hiện tại <= 6: FailureDropLevels = 0 (Thất bại không giảm cấp)
    --   Cấp hiện tại 7..10: FailureDropLevels = 1 (Thất bại giảm 1 cấp)
    --   Cấp hiện tại > 10:  FailureDropLevels = 2 (Thất bại giảm 2 cấp)
    MERGE [dbo].[HRK_EnhancementLevelConfigs] AS Target
    USING (VALUES
        (0,  1,  1.0000, 500,   0, 3),
        (1,  2,  0.9500, 750,   0, 3),
        (2,  3,  0.9000, 1000,  0, 3),
        (3,  4,  0.8500, 1500,  0, 3),
        (4,  5,  0.8000, 2500,  0, 3),
        (5,  6,  0.7500, 4000,  0, 3),
        (6,  7,  0.6500, 6000,  0, 3),
        (7,  8,  0.6000, 8500,  1, 3),
        (8,  9,  0.5500, 11500, 1, 3),
        (9,  10, 0.5000, 15000, 1, 3),
        (10, 11, 0.4000, 20000, 1, 3),
        (11, 12, 0.3200, 26000, 2, 3),
        (12, 13, 0.2500, 33000, 2, 3),
        (13, 14, 0.1800, 41000, 2, 3),
        (14, 15, 0.1200, 50000, 2, 3)
    ) AS Source ([CurrentLevel], [NextLevel], [BaseSuccessRate], [GoldCost], [FailureDropLevels], [MaxStoneSlots])
    ON Target.[CurrentLevel] = Source.[CurrentLevel]
    WHEN MATCHED THEN
        UPDATE SET 
            Target.[NextLevel] = Source.[NextLevel],
            Target.[BaseSuccessRate] = Source.[BaseSuccessRate],
            Target.[GoldCost] = Source.[GoldCost],
            Target.[FailureDropLevels] = Source.[FailureDropLevels],
            Target.[MaxStoneSlots] = Source.[MaxStoneSlots],
            Target.[UpdatedOn] = GETDATE()
    WHEN NOT MATCHED THEN
        INSERT ([CurrentLevel], [NextLevel], [BaseSuccessRate], [GoldCost], [FailureDropLevels], [MaxStoneSlots])
        VALUES (Source.[CurrentLevel], Source.[NextLevel], Source.[BaseSuccessRate], Source.[GoldCost], Source.[FailureDropLevels], Source.[MaxStoneSlots]);

    PRINT N'Đã đồng bộ dữ liệu cấu hình 15 cấp độ vào [HRK_EnhancementLevelConfigs].';

    -- -------------------------------------------------------------------------------------
    -- 2. TẠO BẢNG HRK_EnhancementMaterials
    -- -------------------------------------------------------------------------------------
    PRINT N'2. Kiểm tra và tạo bảng [HRK_EnhancementMaterials]...';

    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HRK_EnhancementMaterials')
    BEGIN
        CREATE TABLE [dbo].[HRK_EnhancementMaterials]
        (
            [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            [ItemTemplateId] INT NOT NULL,
            [MaterialType] NVARCHAR(30) NOT NULL, -- 'STONE' hoặc 'CHARM'
            [SuccessRateBonus] DECIMAL(8,4) NOT NULL DEFAULT 0,
            [PreventLevelDrop] BIT NOT NULL DEFAULT 0,
            [CreatedOn] DATETIME NOT NULL DEFAULT GETDATE(),
            [UpdatedOn] DATETIME NOT NULL DEFAULT GETDATE(),
            CONSTRAINT [FK_HRK_EnhancementMaterials_ItemTemplates] FOREIGN KEY ([ItemTemplateId]) REFERENCES [dbo].[HRK_ItemTemplates]([Id]) ON DELETE CASCADE,
            CONSTRAINT [UQ_HRK_EnhancementMaterials_ItemTemplateId] UNIQUE ([ItemTemplateId])
        );
        PRINT N'Đã tạo bảng [HRK_EnhancementMaterials].';
    END

    -- -------------------------------------------------------------------------------------
    -- 3. TẠO BẢNG HRK_EquipmentEnhancementHistory (Audit Log & Idempotency)
    -- -------------------------------------------------------------------------------------
    PRINT N'3. Kiểm tra và tạo bảng [HRK_EquipmentEnhancementHistory]...';

    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HRK_EquipmentEnhancementHistory')
    BEGIN
        CREATE TABLE [dbo].[HRK_EquipmentEnhancementHistory]
        (
            [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            [RequestId] UNIQUEIDENTIFIER NOT NULL,
            [PlayerId] BIGINT NOT NULL,
            [PlayerInventoryId] BIGINT NOT NULL,
            [ItemTemplateId] INT NOT NULL,

            [OldEnhancement] INT NOT NULL,
            [TargetEnhancement] INT NOT NULL,
            [NewEnhancement] INT NOT NULL,

            [BaseSuccessRate] DECIMAL(8,4) NOT NULL,
            [StoneBonusRate] DECIMAL(8,4) NOT NULL,
            [CharmBonusRate] DECIMAL(8,4) NOT NULL,
            [FinalSuccessRate] DECIMAL(8,4) NOT NULL,

            [IsSuccess] BIT NOT NULL,

            [FailureDropLevels] INT NOT NULL,
            [WasLevelProtected] BIT NOT NULL,

            [GoldCost] INT NOT NULL,

            [UsedMaterialsJson] NVARCHAR(MAX) NULL,

            [CreatedOn] DATETIME NOT NULL DEFAULT GETDATE(),
            CONSTRAINT [UQ_HRK_EquipmentEnhancementHistory_RequestId] UNIQUE ([RequestId])
        );

        CREATE NONCLUSTERED INDEX [IX_HRK_EquipmentEnhancementHistory_PlayerId] ON [dbo].[HRK_EquipmentEnhancementHistory]([PlayerId]);
        CREATE NONCLUSTERED INDEX [IX_HRK_EquipmentEnhancementHistory_PlayerInventoryId] ON [dbo].[HRK_EquipmentEnhancementHistory]([PlayerInventoryId]);
        PRINT N'Đã tạo bảng [HRK_EquipmentEnhancementHistory].';
    END

    -- -------------------------------------------------------------------------------------
    -- 4. SEED NGUYÊN LIỆU ĐÁ CƯỜNG HÓA VÀ BÙA VÀO HRK_ItemTemplates
    -- -------------------------------------------------------------------------------------
    PRINT N'4. Seed 5 loại đá cường hóa và 3 loại bùa vào [HRK_ItemTemplates]...';

    DECLARE @CatMaterialId INT = (SELECT TOP 1 [Id] FROM [dbo].[HRK_ItemCategories] WHERE [Code] = 'MATERIAL');
    IF @CatMaterialId IS NULL
    BEGIN
        INSERT INTO [dbo].[HRK_ItemCategories] ([Code], [Name], [Icon], [DisplayOrder], [IsEquipment])
        VALUES ('MATERIAL', N'Nguyên Liệu Rèn', 'bi-lightning-charge-fill', 9, 0);
        SET @CatMaterialId = SCOPE_IDENTITY();
    END

    DECLARE @RarityCommon INT = ISNULL((SELECT TOP 1 [Id] FROM [dbo].[HRK_Rarities] WHERE [Code] = 'Common'), 1);
    DECLARE @RarityRare INT = ISNULL((SELECT TOP 1 [Id] FROM [dbo].[HRK_Rarities] WHERE [Code] = 'Rare'), 2);
    DECLARE @RarityEpic INT = ISNULL((SELECT TOP 1 [Id] FROM [dbo].[HRK_Rarities] WHERE [Code] = 'Epic'), 3);
    DECLARE @RarityLegendary INT = ISNULL((SELECT TOP 1 [Id] FROM [dbo].[HRK_Rarities] WHERE [Code] = 'Legendary'), 4);
    DECLARE @RarityMythic INT = ISNULL((SELECT TOP 1 [Id] FROM [dbo].[HRK_Rarities] WHERE [Code] = 'Mythic'), 5);

    -- Tạo bảng tạm chứa 8 mẫu vật phẩm đá và bùa
    CREATE TABLE #NewTemplates
    (
        [Code] NVARCHAR(100) NOT NULL,
        [CategoryId] INT NOT NULL,
        [RarityId] INT NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [Icon] NVARCHAR(100) NOT NULL,
        [LevelReq] INT NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [IsStackable] BIT NOT NULL,
        [MaxStackSize] INT NOT NULL,
        [SellPrice] INT NOT NULL
    );

    INSERT INTO #NewTemplates ([Code], [CategoryId], [RarityId], [Name], [Icon], [LevelReq], [Description], [IsStackable], [MaxStackSize], [SellPrice])
    VALUES
        ('ENHANCEMENT_STONE_I',   @CatMaterialId, @RarityCommon,    N'Đá Cường Hóa I',   'bi-gem', 1, N'Đá cường hóa sơ cấp, gia tăng 3% tỷ lệ thành công khi rèn đúc trang bị.', 1, 999, 100),
        ('ENHANCEMENT_STONE_II',  @CatMaterialId, @RarityRare,      N'Đá Cường Hóa II',  'bi-gem', 1, N'Đá cường hóa trung cấp, gia tăng 6% tỷ lệ thành công khi rèn đúc trang bị.', 1, 999, 250),
        ('ENHANCEMENT_STONE_III', @CatMaterialId, @RarityEpic,      N'Đá Cường Hóa III', 'bi-gem', 1, N'Đá cường hóa cao cấp, gia tăng 10% tỷ lệ thành công khi rèn đúc trang bị.', 1, 999, 500),
        ('ENHANCEMENT_STONE_IV',  @CatMaterialId, @RarityLegendary, N'Đá Cường Hóa IV',  'bi-gem', 1, N'Đá cường hóa siêu cấp mang linh khí cổ xưa, gia tăng 15% tỷ lệ thành công.', 1, 999, 1200),
        ('ENHANCEMENT_STONE_V',   @CatMaterialId, @RarityMythic,    N'Đá Cường Hóa V',   'bi-gem', 1, N'Đá cường hóa thần phẩm rực cháy năng lượng thiên địa, gia tăng 22% tỷ lệ thành công.', 1, 999, 3000),

        -- 3 loại bùa cường hóa
        ('ENHANCEMENT_LUCKY_CHARM',         @CatMaterialId, @RarityEpic,      N'Bùa May Mắn',      'bi-stars', 1, N'Lá bùa ban phước linh nghiệm, gia tăng 10% tỷ lệ thành công cho một lần cường hóa.', 1, 999, 1000),
        ('ENHANCEMENT_GREATER_LUCKY_CHARM', @CatMaterialId, @RarityLegendary, N'Đại Bùa May Mắn',  'bi-stars', 1, N'Đại phù chú may mắn thượng thừa, gia tăng 20% tỷ lệ thành công cho một lần cường hóa.', 1, 999, 2500),
        ('ENHANCEMENT_PROTECTION_CHARM',    @CatMaterialId, @RarityMythic,    N'Bùa Hộ Mệnh',      'bi-shield-fill-check', 1, N'Bùa hộ mệnh thần kỳ giúp bảo toàn cấp độ trang bị, nếu cường hóa thất bại sẽ không bị tụt cấp.', 1, 999, 5000);

    -- Kiểm tra nếu bảng HRK_ItemTemplates vẫn còn tồn tại cột Rarity (legacy), insert chuỗi rỗng '' để vượt qua ràng buộc NOT NULL
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HRK_ItemTemplates]') AND name = 'Rarity')
    BEGIN
        EXEC('
            MERGE [dbo].[HRK_ItemTemplates] AS Target
            USING #NewTemplates AS Source
            ON Target.[Code] = Source.[Code]
            WHEN MATCHED THEN
                UPDATE SET
                    Target.[CategoryId] = Source.[CategoryId],
                    Target.[RarityId] = Source.[RarityId],
                    Target.[Name] = Source.[Name],
                    Target.[Icon] = Source.[Icon],
                    Target.[LevelReq] = Source.[LevelReq],
                    Target.[Description] = Source.[Description],
                    Target.[IsStackable] = Source.[IsStackable],
                    Target.[MaxStackSize] = Source.[MaxStackSize],
                    Target.[SellPrice] = Source.[SellPrice],
                    Target.[Rarity] = '''',
                    Target.[UpdatedOn] = GETDATE()
            WHEN NOT MATCHED THEN
                INSERT ([Code], [CategoryId], [RarityId], [Name], [Icon], [LevelReq], [Description], [IsStackable], [MaxStackSize], [SellPrice], [Rarity])
                VALUES (Source.[Code], Source.[CategoryId], Source.[RarityId], Source.[Name], Source.[Icon], Source.[LevelReq], Source.[Description], Source.[IsStackable], Source.[MaxStackSize], Source.[SellPrice], '''');
        ');
    END
    ELSE
    BEGIN
        EXEC('
            MERGE [dbo].[HRK_ItemTemplates] AS Target
            USING #NewTemplates AS Source
            ON Target.[Code] = Source.[Code]
            WHEN MATCHED THEN
                UPDATE SET
                    Target.[CategoryId] = Source.[CategoryId],
                    Target.[RarityId] = Source.[RarityId],
                    Target.[Name] = Source.[Name],
                    Target.[Icon] = Source.[Icon],
                    Target.[LevelReq] = Source.[LevelReq],
                    Target.[Description] = Source.[Description],
                    Target.[IsStackable] = Source.[IsStackable],
                    Target.[MaxStackSize] = Source.[MaxStackSize],
                    Target.[SellPrice] = Source.[SellPrice],
                    Target.[UpdatedOn] = GETDATE()
            WHEN NOT MATCHED THEN
                INSERT ([Code], [CategoryId], [RarityId], [Name], [Icon], [LevelReq], [Description], [IsStackable], [MaxStackSize], [SellPrice])
                VALUES (Source.[Code], Source.[CategoryId], Source.[RarityId], Source.[Name], Source.[Icon], Source.[LevelReq], Source.[Description], Source.[IsStackable], Source.[MaxStackSize], Source.[SellPrice]);
        ');
    END

    DROP TABLE #NewTemplates;

    PRINT N'Đã seed 8 mẫu vật phẩm đá và bùa vào [HRK_ItemTemplates].';

    -- -------------------------------------------------------------------------------------
    -- 5. SEED VÀO HRK_EnhancementMaterials
    -- -------------------------------------------------------------------------------------
    PRINT N'5. Cấu hình bảng [HRK_EnhancementMaterials]...';

    MERGE [dbo].[HRK_EnhancementMaterials] AS Target
    USING (
        SELECT t.[Id] AS [ItemTemplateId], m.[MaterialType], m.[SuccessRateBonus], m.[PreventLevelDrop]
        FROM (VALUES
            ('ENHANCEMENT_STONE_I',               'STONE', 0.0300, 0),
            ('ENHANCEMENT_STONE_II',              'STONE', 0.0600, 0),
            ('ENHANCEMENT_STONE_III',             'STONE', 0.1000, 0),
            ('ENHANCEMENT_STONE_IV',              'STONE', 0.1500, 0),
            ('ENHANCEMENT_STONE_V',               'STONE', 0.2200, 0),
            ('ENHANCEMENT_LUCKY_CHARM',           'CHARM', 0.1000, 0),
            ('ENHANCEMENT_GREATER_LUCKY_CHARM',   'CHARM', 0.2000, 0),
            ('ENHANCEMENT_PROTECTION_CHARM',      'CHARM', 0.0000, 1)
        ) AS m([Code], [MaterialType], [SuccessRateBonus], [PreventLevelDrop])
        INNER JOIN [dbo].[HRK_ItemTemplates] t ON t.[Code] = m.[Code]
    ) AS Source
    ON Target.[ItemTemplateId] = Source.[ItemTemplateId]
    WHEN MATCHED THEN
        UPDATE SET
            Target.[MaterialType] = Source.[MaterialType],
            Target.[SuccessRateBonus] = Source.[SuccessRateBonus],
            Target.[PreventLevelDrop] = Source.[PreventLevelDrop],
            Target.[UpdatedOn] = GETDATE()
    WHEN NOT MATCHED THEN
        INSERT ([ItemTemplateId], [MaterialType], [SuccessRateBonus], [PreventLevelDrop])
        VALUES (Source.[ItemTemplateId], Source.[MaterialType], Source.[SuccessRateBonus], Source.[PreventLevelDrop]);

    PRINT N'Đã cấu hình thuộc tính thành công vào [HRK_EnhancementMaterials].';

    -- -------------------------------------------------------------------------------------
    -- 6. TẶNG ĐÁ CƯỜNG HÓA VÀ VÀNG MẪU CHO TEST PLAYER (PlayerId = 1)
    -- -------------------------------------------------------------------------------------
    PRINT N'6. Cung cấp nguyên liệu cường hóa và vàng cho người chơi kiểm thử (PlayerId = 1)...';

    DECLARE @TestPlayerId BIGINT = 1;
    IF EXISTS (SELECT 1 FROM [dbo].[HRK_Players] WHERE [Id] = @TestPlayerId)
    BEGIN
        -- Đảm bảo ví có ít nhất 1,000,000 Vàng để thoải mái cường hóa
        UPDATE [dbo].[HRK_PlayerWallets]
        SET [Gold] = CASE WHEN [Gold] < 1000000 THEN 1000000 ELSE [Gold] END,
            [UpdatedOn] = GETDATE()
        WHERE [PlayerId] = @TestPlayerId;

        -- Tặng mỗi loại đá 50 viên, mỗi loại bùa 10 lá
        DECLARE @ItemTplId INT, @ItemCode NVARCHAR(100), @GiftQty INT;
        DECLARE cur_materials CURSOR FOR
            SELECT [Id], [Code], CASE WHEN [Code] LIKE '%STONE%' THEN 50 ELSE 10 END
            FROM [dbo].[HRK_ItemTemplates]
            WHERE [Code] IN (
                'ENHANCEMENT_STONE_I', 'ENHANCEMENT_STONE_II', 'ENHANCEMENT_STONE_III', 
                'ENHANCEMENT_STONE_IV', 'ENHANCEMENT_STONE_V',
                'ENHANCEMENT_LUCKY_CHARM', 'ENHANCEMENT_GREATER_LUCKY_CHARM', 'ENHANCEMENT_PROTECTION_CHARM'
            );

        OPEN cur_materials;
        FETCH NEXT FROM cur_materials INTO @ItemTplId, @ItemCode, @GiftQty;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            IF EXISTS (SELECT 1 FROM [dbo].[HRK_PlayerInventory] WHERE [PlayerId] = @TestPlayerId AND [ItemTemplateId] = @ItemTplId AND [IsActive] = 1)
            BEGIN
                UPDATE [dbo].[HRK_PlayerInventory]
                SET [Count] = [Count] + @GiftQty,
                    [UpdatedOn] = GETDATE()
                WHERE [PlayerId] = @TestPlayerId AND [ItemTemplateId] = @ItemTplId AND [IsActive] = 1;
            END
            ELSE
            BEGIN
                INSERT INTO [dbo].[HRK_PlayerInventory]
                    ([PlayerId], [ItemTemplateId], [Count], [Enhancement], [Stars], [IsEquipped], [IsLocked], [IsActive], [AcquiredOn], [UpdatedOn])
                VALUES
                    (@TestPlayerId, @ItemTplId, @GiftQty, 0, 0, 0, 0, 1, GETDATE(), GETDATE());
            END

            FETCH NEXT FROM cur_materials INTO @ItemTplId, @ItemCode, @GiftQty;
        END

        CLOSE cur_materials;
        DEALLOCATE cur_materials;

        PRINT N'Đã cấp phát thành công 50 viên mỗi loại đá và 10 lá mỗi loại bùa cho PlayerId = 1.';
    END

    COMMIT TRANSACTION;

    PRINT '=========================================================================================';
    PRINT N'MIGRATION THÀNH CÔNG! HỆ THỐNG CƯỜNG HÓA ĐÃ SẴN SÀNG HOẠT ĐỘNG.';
    PRINT '=========================================================================================';

    -- Truy vấn kiểm tra
    SELECT 'HRK_EnhancementLevelConfigs' AS [Table], COUNT(*) AS [TotalRows] FROM [dbo].[HRK_EnhancementLevelConfigs]
    UNION ALL
    SELECT 'HRK_EnhancementMaterials', COUNT(*) FROM [dbo].[HRK_EnhancementMaterials]
    UNION ALL
    SELECT 'HRK_ItemTemplates (Stones/Charms)', COUNT(*) FROM [dbo].[HRK_ItemTemplates] WHERE [Code] LIKE 'ENHANCEMENT_%';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();

    RAISERROR(N'Lỗi thực thi Migration Cường Hóa: %s', @ErrSeverity, @ErrState, @ErrMsg);
END CATCH;
GO

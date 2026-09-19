-- =========================================================================================
-- HRK GAME DEVELOPMENT/TEST DATA SEED SCRIPT: FORGE EQUIPMENT TEST SUITE
-- =========================================================================================
-- CẢNH BÁO: ĐÂY LÀ SCRIPT DÀNH CHO MÔI TRƯỜNG PHÁT TRIỂN / KIỂM THỬ (DEV / TEST ONLY).
-- KHÔNG CHẠY SCRIPT NÀY TRÊN MÔI TRƯỜNG PRODUCTION THẬT.
-- =========================================================================================
-- Mục đích:
-- 1. Tạo tập mẫu trang bị phong phú cho người chơi chính (Player Id = 1) để kiểm thử toàn diện Lò Rèn:
--    - Cấp +0  (Tier 1 animation: +0 -> +1)
--    - Cấp +4  (Tier 1 animation: +4 -> +5)
--    - Cấp +8  (Tier 2 animation: +8 -> +9)
--    - Cấp +12 (Tier 3 animation: +12 -> +13)
--    - Cấp +14 (Tier 3 max attempt: +14 -> +15, hiệu ứng thần thoại đặc biệt)
--    - Cấp +15 (Đạt cấp tối đa, vô hiệu hóa nút cường hóa, kiểm tra CanEnhance = false, reason = MAX_ENHANCEMENT)
--    - Trang bị bị KHÓA (IsLocked = 1, kiểm tra CanEnhance = false, reason = LOCKED)
--    - Đầy đủ 6 danh mục: WEAPON, ARMOR, HELMET, BOOTS, RING, ARTIFACT
-- =========================================================================================

SET NOCOUNT ON;

PRINT '=========================================================================================';
PRINT N'BẮT ĐẦU SEED DỮ LIỆU KIỂM THỬ: FORGE EQUIPMENT TEST DATA';
PRINT '=========================================================================================';

DECLARE @TargetPlayerId BIGINT = 1;

-- Kiểm tra xem người chơi có tồn tại không
IF NOT EXISTS (SELECT 1 FROM [dbo].[HRK_Players] WHERE [Id] = @TargetPlayerId)
BEGIN
    PRINT N'LƯU Ý: Không tìm thấy người chơi có Id = ' + CAST(@TargetPlayerId AS NVARCHAR(20)) + N'. Đang lấy người chơi đầu tiên...';
    SELECT TOP 1 @TargetPlayerId = [Id] FROM [dbo].[HRK_Players] ORDER BY [Id] ASC;
END

IF @TargetPlayerId IS NULL
BEGIN
    PRINT N'LỖI: Chưa có người chơi nào trong bảng HRK_Players. Vui lòng tạo tài khoản trước.';
    RETURN;
END

PRINT N'Người chơi mục tiêu được gán dữ liệu kiểm thử: PlayerId = ' + CAST(@TargetPlayerId AS NVARCHAR(20));

BEGIN TRANSACTION;

BEGIN TRY

    -- Tìm các ItemTemplateId đại diện cho từng danh mục trang bị
    DECLARE @WeaponTmpl INT, @ArmorTmpl INT, @HelmetTmpl INT, @BootsTmpl INT, @RingTmpl INT, @ArtifactTmpl INT;

    SELECT TOP 1 @WeaponTmpl = t.[Id] 
    FROM [dbo].[HRK_ItemTemplates] t 
    JOIN [dbo].[HRK_ItemCategories] c ON t.[CategoryId] = c.[Id] 
    WHERE c.[Code] = 'WEAPON' ORDER BY t.[Id] ASC;

    SELECT TOP 1 @ArmorTmpl = t.[Id] 
    FROM [dbo].[HRK_ItemTemplates] t 
    JOIN [dbo].[HRK_ItemCategories] c ON t.[CategoryId] = c.[Id] 
    WHERE c.[Code] = 'ARMOR' ORDER BY t.[Id] ASC;

    SELECT TOP 1 @HelmetTmpl = t.[Id] 
    FROM [dbo].[HRK_ItemTemplates] t 
    JOIN [dbo].[HRK_ItemCategories] c ON t.[CategoryId] = c.[Id] 
    WHERE c.[Code] = 'HELMET' ORDER BY t.[Id] ASC;

    SELECT TOP 1 @BootsTmpl = t.[Id] 
    FROM [dbo].[HRK_ItemTemplates] t 
    JOIN [dbo].[HRK_ItemCategories] c ON t.[CategoryId] = c.[Id] 
    WHERE c.[Code] = 'BOOTS' ORDER BY t.[Id] ASC;

    SELECT TOP 1 @RingTmpl = t.[Id] 
    FROM [dbo].[HRK_ItemTemplates] t 
    JOIN [dbo].[HRK_ItemCategories] c ON t.[CategoryId] = c.[Id] 
    WHERE c.[Code] = 'RING' ORDER BY t.[Id] ASC;

    SELECT TOP 1 @ArtifactTmpl = t.[Id] 
    FROM [dbo].[HRK_ItemTemplates] t 
    JOIN [dbo].[HRK_ItemCategories] c ON t.[CategoryId] = c.[Id] 
    WHERE c.[Code] = 'ARTIFACT' ORDER BY t.[Id] ASC;

    -- Nếu thiếu, fallback về bất kỳ template trang bị nào
    IF @WeaponTmpl IS NULL SELECT TOP 1 @WeaponTmpl = [Id] FROM [dbo].[HRK_ItemTemplates];
    IF @ArmorTmpl IS NULL SET @ArmorTmpl = @WeaponTmpl;
    IF @HelmetTmpl IS NULL SET @HelmetTmpl = @WeaponTmpl;
    IF @BootsTmpl IS NULL SET @BootsTmpl = @WeaponTmpl;
    IF @RingTmpl IS NULL SET @RingTmpl = @WeaponTmpl;
    IF @ArtifactTmpl IS NULL SET @ArtifactTmpl = @WeaponTmpl;

    -- Bổ sung mẫu trang bị kiểm thử đa dạng cấp độ
    -- 1. Vũ khí +0 (Tier 1)
    INSERT INTO [dbo].[HRK_PlayerInventory] ([PlayerId], [ItemTemplateId], [Count], [Enhancement], [Stars], [IsEquipped], [IsLocked], [IsActive], [AcquiredOn], [UpdatedOn])
    VALUES (@TargetPlayerId, @WeaponTmpl, 1, 0, 0, 0, 0, 1, GETDATE(), GETDATE());

    -- 2. Giáp +4 (Tier 1 cận kề Tier 2)
    INSERT INTO [dbo].[HRK_PlayerInventory] ([PlayerId], [ItemTemplateId], [Count], [Enhancement], [Stars], [IsEquipped], [IsLocked], [IsActive], [AcquiredOn], [UpdatedOn])
    VALUES (@TargetPlayerId, @ArmorTmpl, 1, 4, 1, 0, 0, 1, GETDATE(), GETDATE());

    -- 3. Mũ +8 (Tier 2)
    INSERT INTO [dbo].[HRK_PlayerInventory] ([PlayerId], [ItemTemplateId], [Count], [Enhancement], [Stars], [IsEquipped], [IsLocked], [IsActive], [AcquiredOn], [UpdatedOn])
    VALUES (@TargetPlayerId, @HelmetTmpl, 1, 8, 2, 0, 0, 1, GETDATE(), GETDATE());

    -- 4. Giày +12 (Tier 3 Thần Thoại)
    INSERT INTO [dbo].[HRK_PlayerInventory] ([PlayerId], [ItemTemplateId], [Count], [Enhancement], [Stars], [IsEquipped], [IsLocked], [IsActive], [AcquiredOn], [UpdatedOn])
    VALUES (@TargetPlayerId, @BootsTmpl, 1, 12, 3, 0, 0, 1, GETDATE(), GETDATE());

    -- 5. Nhẫn +14 (Tier 3 Cực Hạn +14 -> +15)
    INSERT INTO [dbo].[HRK_PlayerInventory] ([PlayerId], [ItemTemplateId], [Count], [Enhancement], [Stars], [IsEquipped], [IsLocked], [IsActive], [AcquiredOn], [UpdatedOn])
    VALUES (@TargetPlayerId, @RingTmpl, 1, 14, 4, 0, 0, 1, GETDATE(), GETDATE());

    -- 6. Thần Binh +15 (Đã đạt tối đa, kiểm thử Disable & Tooltip)
    INSERT INTO [dbo].[HRK_PlayerInventory] ([PlayerId], [ItemTemplateId], [Count], [Enhancement], [Stars], [IsEquipped], [IsLocked], [IsActive], [AcquiredOn], [UpdatedOn])
    VALUES (@TargetPlayerId, @ArtifactTmpl, 1, 15, 5, 0, 0, 1, GETDATE(), GETDATE());

    -- 7. Vũ khí bị KHÓA (Kiểm thử IsLocked = 1 & Tooltip)
    INSERT INTO [dbo].[HRK_PlayerInventory] ([PlayerId], [ItemTemplateId], [Count], [Enhancement], [Stars], [IsEquipped], [IsLocked], [IsActive], [AcquiredOn], [UpdatedOn])
    VALUES (@TargetPlayerId, @WeaponTmpl, 1, 5, 1, 0, 1, 1, GETDATE(), GETDATE());

    COMMIT TRANSACTION;
    PRINT N'SEED THÀNH CÔNG: Đã thêm 7 trang bị kiểm thử vào hành trang người chơi.';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT N'LỖI SEED: Đã rollback giao dịch.';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH;

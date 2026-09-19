-- =========================================================================================
-- HRK GAME DATABASE MIGRATION SCRIPT: FORGE EQUIPMENT PERFORMANCE SUPPORT
-- =========================================================================================
-- Script Mục Đích:
-- 1. Thêm chỉ mục tổng hợp (composite non-clustered index) trên bảng [HRK_PlayerInventory]
--    cho (PlayerId, IsActive, ItemTemplateId) để tối ưu hóa truy vấn danh sách trang bị cho Lò Rèn (Forge Browser).
-- 2. Tối ưu join giữa HRK_PlayerInventory và HRK_ItemTemplates.
-- =========================================================================================

SET NOCOUNT ON;

PRINT '=========================================================================================';
PRINT N'BẮT ĐẦU MIGRATION: FORGE EQUIPMENT PERFORMANCE SUPPORT (TỐI ƯU TRUY VẤN LÒ RÈN)';
PRINT '=========================================================================================';

BEGIN TRANSACTION;

BEGIN TRY

    -- -------------------------------------------------------------------------------------
    -- 1. KIỂM TRA VÀ TẠO CHỈ MỤC TRÊN [HRK_PlayerInventory]
    -- -------------------------------------------------------------------------------------
    PRINT N'1. Kiểm tra và tạo composite index IX_HRK_PlayerInventory_Player_Active_Template...';

    IF NOT EXISTS (
        SELECT 1 
        FROM sys.indexes 
        WHERE name = 'IX_HRK_PlayerInventory_Player_Active_Template' 
          AND object_id = OBJECT_ID('[dbo].[HRK_PlayerInventory]')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_HRK_PlayerInventory_Player_Active_Template]
        ON [dbo].[HRK_PlayerInventory] ([PlayerId], [IsActive], [ItemTemplateId])
        INCLUDE ([Enhancement], [Stars], [IsEquipped], [IsLocked], [EquippedHeroId], [CurrentStats]);

        PRINT N'Đã tạo thành công chỉ mục [IX_HRK_PlayerInventory_Player_Active_Template].';
    END
    ELSE
    BEGIN
        PRINT N'Chỉ mục [IX_HRK_PlayerInventory_Player_Active_Template] đã tồn tại.';
    END

    COMMIT TRANSACTION;
    PRINT N'MIGRATION THÀNH CÔNG: Hoàn tất cấu trúc chỉ mục cho Lò Rèn Thần Binh.';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT N'LỖI MIGRATION: Đã rollback giao dịch.';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH;

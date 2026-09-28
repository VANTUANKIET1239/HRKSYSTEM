-- ============================================================================
-- Script: Migration_UpdateSixRareHeroesAvatars.sql
-- Mục đích: Cập nhật đường dẫn Avatar của 6 Hero Rare sang bộ ảnh toàn thân mới (*-rare.png)
-- Ngày tạo: 2026-09-28
-- Database: HRK
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    PRINT N'=== BẮT ĐẦU CẬP NHẬT ĐƯỜNG DẪN ẢNH CHO 6 HERO RARE ===';

    -- 1. Trọng Chưa Nổ
    UPDATE dbo.HRK_HeroTemplates
    SET Avatar = '/assets/images/dcs-game/trong-chua-no-rare.png'
    WHERE UPPER(Name) = N'TRỌNG CHƯA NỔ' 
       OR Avatar LIKE '%trong-chua-no%';
    PRINT N'-> Đã cập nhật Avatar cho [Trọng Chưa Nổ] thành /assets/images/dcs-game/trong-chua-no-rare.png';

    -- 2. Quang Vinh THCS
    UPDATE dbo.HRK_HeroTemplates
    SET Avatar = '/assets/images/dcs-game/quang-vinh-thcs-rare.png'
    WHERE UPPER(Name) = N'QUANG VINH THCS' 
       OR Avatar LIKE '%quang-vinh-thcs%';
    PRINT N'-> Đã cập nhật Avatar cho [Quang Vinh THCS] thành /assets/images/dcs-game/quang-vinh-thcs-rare.png';

    -- 3. Nguyên Xàm Lớn
    UPDATE dbo.HRK_HeroTemplates
    SET Avatar = '/assets/images/dcs-game/nguyen-xam-lon-rare.png'
    WHERE UPPER(Name) = N'NGUYÊN XÀM LỚN' 
       OR Avatar LIKE '%nguyen-xam-lon%';
    PRINT N'-> Đã cập nhật Avatar cho [Nguyên Xàm Lớn] thành /assets/images/dcs-game/nguyen-xam-lon-rare.png';

    -- 4. Tiến Dũng Xuân
    UPDATE dbo.HRK_HeroTemplates
    SET Avatar = '/assets/images/dcs-game/tien-dung-xuan-rare.png'
    WHERE UPPER(Name) = N'TIẾN DŨNG XUÂN' 
       OR Avatar LIKE '%tien-dung-xuan%';
    PRINT N'-> Đã cập nhật Avatar cho [Tiến Dũng Xuân] thành /assets/images/dcs-game/tien-dung-xuan-rare.png';

    -- 5. Văn Trọng Điện Vàng
    UPDATE dbo.HRK_HeroTemplates
    SET Avatar = '/assets/images/dcs-game/van-trong-dien-vang-rare.png'
    WHERE UPPER(Name) = N'VĂN TRỌNG ĐIỆN VÀNG' 
       OR Avatar LIKE '%van-trong-dien-vang%'
       OR Avatar LIKE '%van-trong-pikachu%';
    PRINT N'-> Đã cập nhật Avatar cho [Văn Trọng Điện Vàng] thành /assets/images/dcs-game/van-trong-dien-vang-rare.png';

    -- 6. Tường Long Cấp 3
    UPDATE dbo.HRK_HeroTemplates
    SET Avatar = '/assets/images/dcs-game/tuong-long-cap-3-rare.png'
    WHERE UPPER(Name) = N'TƯỜNG LONG CẤP 3' 
       OR Avatar LIKE '%tuong-long-cap-3%';
    PRINT N'-> Đã cập nhật Avatar cho [Tường Long Cấp 3] thành /assets/images/dcs-game/tuong-long-cap-3-rare.png';

    -- 7. Đồng bộ sang bảng HRK_AvatarTemplates (nếu có sử dụng làm Player Avatar Template)
    IF OBJECT_ID(N'dbo.HRK_AvatarTemplates', N'U') IS NOT NULL
    BEGIN
        UPDATE at
        SET at.ImagePath = ht.Avatar,
            at.UpdatedOn = SYSUTCDATETIME()
        FROM dbo.HRK_AvatarTemplates at
        INNER JOIN dbo.HRK_HeroTemplates ht ON at.Code = CONCAT(N'HERO_', ht.Id)
        WHERE ht.Avatar LIKE '%-rare.png';

        PRINT N'-> Đã đồng bộ đường dẫn ảnh sang dbo.HRK_AvatarTemplates (Player Avatars).';
    END;

    COMMIT TRANSACTION;
    PRINT N'=== [HOÀN TẤT THÀNH CÔNG] Cập nhật đường dẫn ảnh 6 hero Rare ===';

    -- Truy vấn kiểm tra lại kết quả
    SELECT 
        Id,
        Name,
        Avatar,
        RarityId,
        CreatedOn
    FROM dbo.HRK_HeroTemplates
    WHERE Avatar LIKE '%-rare.png'
    ORDER BY Id;

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

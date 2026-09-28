-- ============================================================================
-- Script: Migration_UpdateFiveEpicHeroesAvatars.sql
-- Mục đích: Cập nhật đường dẫn Avatar của 5 Hero Epic sang bộ ảnh toàn thân mới (*-epic.png)
-- Ngày tạo: 2026-09-29
-- Database: HRK
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    PRINT N'=== BẮT ĐẦU CẬP NHẬT ĐƯỜNG DẪN ẢNH CHO 5 HERO EPIC ===';

    -- Kiểm tra prerequisite: Các hero phải tồn tại trước khi cập nhật
    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_HeroTemplates WHERE UPPER(Name) = N'KIỆT BÁC SĨ' OR Avatar LIKE '%kiet-bac-si%')
    BEGIN
        THROW 52001, N'Không tìm thấy Hero Kiệt Bác Sĩ. Vui lòng chạy Migration_AddFiveEpicHeroes.sql trước.', 1;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_HeroTemplates WHERE UPPER(Name) = N'TRƯỜNG KIỆT CHU MỎ' OR Avatar LIKE '%truong-kiet-chu-mo%')
    BEGIN
        THROW 52002, N'Không tìm thấy Hero Trường Kiệt Chu Mỏ. Vui lòng chạy Migration_AddFiveEpicHeroes.sql trước.', 1;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_HeroTemplates WHERE UPPER(Name) = N'TIẾN DŨNG TỔNG ĐÀI' OR Avatar LIKE '%tien-dung-call-video%' OR Avatar LIKE '%tien-dung-tong-dai%')
    BEGIN
        THROW 52003, N'Không tìm thấy Hero Tiến Dũng Tổng Đài. Vui lòng chạy Migration_AddFiveEpicHeroes.sql trước.', 1;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_HeroTemplates WHERE UPPER(Name) = N'QUỐC NHÂN GIẢ DIỆN' OR Avatar LIKE '%quoc-nhan%')
    BEGIN
        THROW 52004, N'Không tìm thấy Hero Quốc Nhân Giả Diện. Vui lòng chạy Migration_AddFiveEpicHeroes.sql trước.', 1;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_HeroTemplates WHERE UPPER(Name) = N'CẬU VÀNG MẶT LẠNH' OR Avatar LIKE '%cau-vang%' OR Avatar LIKE '%meme-cho%')
    BEGIN
        THROW 52005, N'Không tìm thấy Hero Cậu Vàng Mặt Lạnh. Vui lòng chạy Migration_AddFiveEpicHeroes.sql trước.', 1;
    END

    -- 1. Kiệt Bác Sĩ
    UPDATE dbo.HRK_HeroTemplates
    SET Avatar = '/assets/images/dcs-game/kiet-bac-si-epic.png'
    WHERE UPPER(Name) = N'KIỆT BÁC SĨ' 
       OR Avatar LIKE '%kiet-bac-si%';
    IF @@ROWCOUNT < 1
        THROW 52011, N'Cập nhật thất bại cho Kiệt Bác Sĩ.', 1;
    PRINT N'-> Đã cập nhật Avatar cho [Kiệt Bác Sĩ] thành /assets/images/dcs-game/kiet-bac-si-epic.png';

    -- 2. Trường Kiệt Chu Mỏ
    UPDATE dbo.HRK_HeroTemplates
    SET Avatar = '/assets/images/dcs-game/truong-kiet-chu-mo-epic.png'
    WHERE UPPER(Name) = N'TRƯỜNG KIỆT CHU MỎ' 
       OR Avatar LIKE '%truong-kiet-chu-mo%';
    IF @@ROWCOUNT < 1
        THROW 52012, N'Cập nhật thất bại cho Trường Kiệt Chu Mỏ.', 1;
    PRINT N'-> Đã cập nhật Avatar cho [Trường Kiệt Chu Mỏ] thành /assets/images/dcs-game/truong-kiet-chu-mo-epic.png';

    -- 3. Tiến Dũng Tổng Đài
    UPDATE dbo.HRK_HeroTemplates
    SET Avatar = '/assets/images/dcs-game/tien-dung-tong-dai-epic.png'
    WHERE UPPER(Name) = N'TIẾN DŨNG TỔNG ĐÀI' 
       OR Avatar LIKE '%tien-dung-call-video%'
       OR Avatar LIKE '%tien-dung-tong-dai%';
    IF @@ROWCOUNT < 1
        THROW 52013, N'Cập nhật thất bại cho Tiến Dũng Tổng Đài.', 1;
    PRINT N'-> Đã cập nhật Avatar cho [Tiến Dũng Tổng Đài] thành /assets/images/dcs-game/tien-dung-tong-dai-epic.png';

    -- 4. Quốc Nhân Giả Diện
    UPDATE dbo.HRK_HeroTemplates
    SET Avatar = '/assets/images/dcs-game/quoc-nhan-gia-dien-epic.png'
    WHERE UPPER(Name) = N'QUỐC NHÂN GIẢ DIỆN' 
       OR Avatar LIKE '%quoc-nhan-fake%'
       OR Avatar LIKE '%quoc-nhan-gia-dien%';
    IF @@ROWCOUNT < 1
        THROW 52014, N'Cập nhật thất bại cho Quốc Nhân Giả Diện.', 1;
    PRINT N'-> Đã cập nhật Avatar cho [Quốc Nhân Giả Diện] thành /assets/images/dcs-game/quoc-nhan-gia-dien-epic.png';

    -- 5. Cậu Vàng Mặt Lạnh
    UPDATE dbo.HRK_HeroTemplates
    SET Avatar = '/assets/images/dcs-game/cau-vang-mat-lanh-epic.png'
    WHERE UPPER(Name) = N'CẬU VÀNG MẶT LẠNH' 
       OR Avatar LIKE '%meme-cho-hai-huoc%'
       OR Avatar LIKE '%cau-vang-mat-lanh%';
    IF @@ROWCOUNT < 1
        THROW 52015, N'Cập nhật thất bại cho Cậu Vàng Mặt Lạnh.', 1;
    PRINT N'-> Đã cập nhật Avatar cho [Cậu Vàng Mặt Lạnh] thành /assets/images/dcs-game/cau-vang-mat-lanh-epic.png';

    -- 6. Đồng bộ sang bảng HRK_AvatarTemplates (nếu có sử dụng làm Player Avatar Template)
    IF OBJECT_ID(N'dbo.HRK_AvatarTemplates', N'U') IS NOT NULL
    BEGIN
        UPDATE at
        SET at.ImagePath = ht.Avatar,
            at.UpdatedOn = SYSUTCDATETIME()
        FROM dbo.HRK_AvatarTemplates at
        INNER JOIN dbo.HRK_HeroTemplates ht ON at.Code = CONCAT(N'HERO_', ht.Id)
        WHERE ht.Avatar LIKE '%-epic.png';

        PRINT N'-> Đã đồng bộ đường dẫn ảnh sang dbo.HRK_AvatarTemplates (Player Avatars).';
    END;

    -- Kiểm tra hậu điều kiện: Tất cả 5 hero phải có avatar chính xác
    DECLARE @UpdatedEpicCount INT;
    SELECT @UpdatedEpicCount = COUNT(*) 
    FROM dbo.HRK_HeroTemplates 
    WHERE Avatar IN (
        '/assets/images/dcs-game/kiet-bac-si-epic.png',
        '/assets/images/dcs-game/truong-kiet-chu-mo-epic.png',
        '/assets/images/dcs-game/tien-dung-tong-dai-epic.png',
        '/assets/images/dcs-game/quoc-nhan-gia-dien-epic.png',
        '/assets/images/dcs-game/cau-vang-mat-lanh-epic.png'
    );

    IF @UpdatedEpicCount < 5
    BEGIN
        THROW 52020, N'Hậu kiểm thất bại: Số lượng hero có avatar Epic không đạt đủ 5.', 1;
    END

    COMMIT TRANSACTION;
    PRINT N'=== [HOÀN TẤT THÀNH CÔNG] Cập nhật đường dẫn ảnh 5 hero Epic ===';

    -- Truy vấn kiểm tra lại kết quả
    SELECT 
        Id,
        Name,
        Avatar,
        RarityId,
        CreatedOn
    FROM dbo.HRK_HeroTemplates
    WHERE Avatar LIKE '%-epic.png'
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

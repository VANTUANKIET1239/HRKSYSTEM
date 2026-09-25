/* ============================================================================
   SCRIPT: Migration_RepairHaiLastSmileVietnameseText.sql
   MỤC ĐÍCH: Sửa triệt để các chuỗi tiếng Việt bị mojibake / lỗi font encoding
             trong dữ liệu của nhân vật "Tao là nhất" (HeroTemplateId = 11).
   YÊU CẦU: Lưu UTF-8, sử dụng N'...' cho mọi chuỗi Unicode, chạy trong TRANSACTION.
   ============================================================================ */

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    PRINT N'>>> Bắt đầu chuẩn hóa dữ liệu tiếng Việt cho nhân vật Tao là nhất...';

    -- 1. Sửa thông tin Hero Template
    UPDATE dbo.HRK_HeroTemplates
    SET Name = N'Tao là nhất',
        Avatar = '/assets/images/dcs-game/heroes/tao-la-nhat-transparent.png'
    WHERE Id = 11 OR Name LIKE N'%Tao là nhất%' OR Avatar LIKE '%nhan-cuoi-xam-lon%';

    PRINT N'  + Đã chuẩn hóa HRK_HeroTemplates';

    -- 2. Sửa thông tin Skill Effect Types
    -- 2.1 BLEED (Chảy Máu)
    UPDATE dbo.HRK_SkillEffectTypes
    SET Name = N'Chảy Máu',
        Description = N'Mỗi đầu lượt nhận sát thương bằng 18% ATK nguồn, bỏ qua 30% phòng thủ, tối thiểu còn 1 HP.',
        ImagePath = '/assets/images/dcs-game/effects/bleed.png',
        UpdatedOn = SYSUTCDATETIME()
    WHERE Code = 'BLEED';

    -- 2.2 PANIC (Hoảng Loạn)
    UPDATE dbo.HRK_SkillEffectTypes
    SET Name = N'Hoảng Loạn',
        Description = N'Giảm 20% phòng thủ và không thể nhận Khiên mới trong thời gian hiệu lực.',
        ImagePath = '/assets/images/dcs-game/effects/panic.png',
        UpdatedOn = SYSUTCDATETIME()
    WHERE Code = 'PANIC';

    -- 2.3 SHIELD_BLOCK (Cấm Nhận Khiên)
    UPDATE dbo.HRK_SkillEffectTypes
    SET Name = N'Cấm Nhận Khiên',
        Description = N'Không thể nhận Khiên mới trong thời gian hiệu lực.',
        ImagePath = '/assets/images/dcs-game/effects/shield-block.png',
        UpdatedOn = SYSUTCDATETIME()
    WHERE Code = 'SHIELD_BLOCK';

    -- 2.4 BLEED_DETONATE (Kích Nổ Chảy Máu)
    UPDATE dbo.HRK_SkillEffectTypes
    SET Name = N'Kích Nổ Chảy Máu',
        Description = N'Kích nổ tổng sát thương Chảy Máu còn lại trên mục tiêu.',
        ImagePath = '/assets/images/dcs-game/effects/bleed-detonate.png',
        UpdatedOn = SYSUTCDATETIME()
    WHERE Code = 'BLEED_DETONATE';

    -- 2.5 ENERGY_CHANGE (Thay Đổi Năng Lượng)
    UPDATE dbo.HRK_SkillEffectTypes
    SET Name = N'Thay Đổi Năng Lượng',
        Description = N'Hồi phục hoặc tiêu hao năng lượng của mục tiêu.',
        ImagePath = '/assets/images/dcs-game/effects/energy.png',
        UpdatedOn = SYSUTCDATETIME()
    WHERE Code = 'ENERGY_CHANGE';

    PRINT N'  + Đã chuẩn hóa HRK_SkillEffectTypes';

    -- 3. Sửa Skill Target Types
    UPDATE dbo.HRK_SkillTargetTypes
    SET Name = N'Kẻ địch có % HP thấp nhất',
        UpdatedOn = SYSUTCDATETIME()
    WHERE Code = 'LOWEST_HP_PERCENT';

    PRINT N'  + Đã chuẩn hóa HRK_SkillTargetTypes';

    -- 4. Sửa Skill Templates
    -- 4.1 HAI_BUG_SLASH (Dao Rạch Bug)
    UPDATE dbo.HRK_SkillTemplates
    SET Name = N'Dao Rạch Bug',
        Icon = '/assets/images/dcs-game/skills/hai-bug-slash.png',
        ImagePath = '/assets/images/dcs-game/skills/hai-bug-slash.png',
        Description = N'Lướt tới chém chéo mục tiêu có % HP thấp nhất, gây sát thương vật lý theo ATK. Nếu gây sát thương thành công, có xác suất gây Chảy Máu (BLEED). Mọi thông số được quản lý qua cấu hình dữ liệu.',
        UpdatedOn = SYSUTCDATETIME()
    WHERE Id = 'HAI_BUG_SLASH';

    -- 4.2 HAI_LAST_LAUGH (Cười Đi, Sắp Hết Lượt Rồi)
    UPDATE dbo.HRK_SkillTemplates
    SET Name = N'Cười Đi, Sắp Hết Lượt Rồi',
        Icon = '/assets/images/dcs-game/skills/hai-last-laugh.png',
        ImagePath = '/assets/images/dcs-game/skills/hai-last-laugh.png',
        Description = N'Khóa mục tiêu có % HP thấp nhất, gây Hoảng Loạn (giảm DEF, cấm nhận khiên). Nếu mục tiêu có Chảy Máu trước đó, kích nổ ngay lập tức gây sát thương Chảy Máu còn lại (bỏ qua một phần DEF). Sau đó tung 3 nhát đâm chớp nhoáng (HIT_1, HIT_2, HIT_3), mỗi nhát có xác suất gây Chảy Máu độc lập. Hạ gục mục tiêu sẽ hồi 25 năng lượng và nhận buff giảm sát thương; nếu không hạ gục được bị giảm DEF.',
        UpdatedOn = SYSUTCDATETIME()
    WHERE Id = 'HAI_LAST_LAUGH';

    PRINT N'  + Đã chuẩn hóa HRK_SkillTemplates';

    -- 5. Sửa Hero Star Aura Configs
    UPDATE dbo.HRK_HeroStarAuraConfigs
    SET Name = N'Hắc Ảnh Khởi Động',
        Description = N'Vòng sáng xanh đen mỏng dưới chân, ám khí sơ khởi.',
        UpdatedOn = SYSUTCDATETIME()
    WHERE HeroTemplateId = 11 AND StarLevel = 2;

    UPDATE dbo.HRK_HeroStarAuraConfigs
    SET Name = N'Vi Mạch Ám Sát',
        Description = N'Đường mạch vi tính công nghệ phát sáng xanh và các hạt phân tử nano bay lên.',
        UpdatedOn = SYSUTCDATETIME()
    WHERE HeroTemplateId = 11 AND StarLevel = 3;

    UPDATE dbo.HRK_HeroStarAuraConfigs
    SET Name = N'Hư Vô Đoạt Mệnh',
        Description = N'Bóng dao găm xoay chậm xung quanh người kết hợp xung lực năng lượng dâng trào.',
        UpdatedOn = SYSUTCDATETIME()
    WHERE HeroTemplateId = 11 AND StarLevel = 4;

    UPDATE dbo.HRK_HeroStarAuraConfigs
    SET Name = N'Nụ Cười Tối Thượng',
        Description = N'Vòng rune công nghệ ma trận hoàn chỉnh, bóng dao xoay tít, tia sáng xanh lam rực rỡ và hạt huyết sắc Chảy Máu.',
        UpdatedOn = SYSUTCDATETIME()
    WHERE HeroTemplateId = 11 AND StarLevel = 5;

    PRINT N'  + Đã chuẩn hóa HRK_HeroStarAuraConfigs';

    COMMIT TRANSACTION;
    PRINT N'>>> HOÀN THÀNH: Tất cả dữ liệu tiếng Việt đã được cập nhật thành công!';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;
GO

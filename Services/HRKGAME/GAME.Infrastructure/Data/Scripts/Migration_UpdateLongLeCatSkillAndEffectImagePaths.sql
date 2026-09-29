/*
================================================================================
Migration: Update Long Lê Con Mèo Skill & Status Effect Image Paths
Description:
    Idempotent update ensuring that CAT_SCRATCH, DEEP_CAT_SCRATCH,
    LONG_LE_CAT_SCRATCH_BASIC, and LONG_LE_CAT_COMPANIONS correctly reference
    the newly generated transparent high-definition game assets.
================================================================================
*/

SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    PRINT N'====================================================================';
    PRINT N'Bắt đầu cập nhật ImagePath cho Long Lê Con Mèo...';
    PRINT N'====================================================================';

    -- 1. Cập nhật Status Effect Type: CAT_SCRATCH (Vết Cào)
    IF EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'CAT_SCRATCH')
    BEGIN
        UPDATE dbo.HRK_SkillEffectTypes
        SET ImagePath = '/assets/images/dcs-game/effects/cat-scratch.png',
            ColorHex = '#eab308',
            UpdatedOn = SYSUTCDATETIME()
        WHERE UPPER(Code) = 'CAT_SCRATCH';
        PRINT N'-> Đã cập nhật ImagePath cho CAT_SCRATCH.';
    END;

    -- 2. Cập nhật Status Effect Type: DEEP_CAT_SCRATCH (Vết Cào Sâu)
    IF EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'DEEP_CAT_SCRATCH')
    BEGIN
        UPDATE dbo.HRK_SkillEffectTypes
        SET ImagePath = '/assets/images/dcs-game/effects/deep-cat-scratch.png',
            ColorHex = '#dc2626',
            UpdatedOn = SYSUTCDATETIME()
        WHERE UPPER(Code) = 'DEEP_CAT_SCRATCH';
        PRINT N'-> Đã cập nhật ImagePath cho DEEP_CAT_SCRATCH.';
    END;

    -- 3. Cập nhật Status Effect Type: CAT_COMPANION (Mèo Đồng Hành)
    IF EXISTS (SELECT 1 FROM dbo.HRK_SkillEffectTypes WHERE UPPER(Code) = 'CAT_COMPANION')
    BEGIN
        UPDATE dbo.HRK_SkillEffectTypes
        SET ImagePath = '/assets/images/dcs-game/skill-vfx/long-le-cat/long-le-cat-companion.png',
            ColorHex = '#f59e0b',
            UpdatedOn = SYSUTCDATETIME()
        WHERE UPPER(Code) = 'CAT_COMPANION';
        PRINT N'-> Đã cập nhật ImagePath cho CAT_COMPANION.';
    END;

    -- 4. Cập nhật Skill: LONG_LE_CAT_SCRATCH_BASIC (Mèo Cào Đánh Dấu)
    IF EXISTS (SELECT 1 FROM dbo.HRK_Skills WHERE UPPER(Id) = 'LONG_LE_CAT_SCRATCH_BASIC')
    BEGIN
        UPDATE dbo.HRK_Skills
        SET ImagePath = '/assets/images/dcs-game/skills/long-le-cat/cat-scratch.png'
        WHERE UPPER(Id) = 'LONG_LE_CAT_SCRATCH_BASIC';
        PRINT N'-> Đã cập nhật ImagePath cho LONG_LE_CAT_SCRATCH_BASIC.';
    END;

    -- 5. Cập nhật Skill: LONG_LE_CAT_COMPANIONS (Tam Miêu Hộ Trận)
    IF EXISTS (SELECT 1 FROM dbo.HRK_Skills WHERE UPPER(Id) = 'LONG_LE_CAT_COMPANIONS')
    BEGIN
        UPDATE dbo.HRK_Skills
        SET ImagePath = '/assets/images/dcs-game/skills/long-le-cat/cat-companions.png'
        WHERE UPPER(Id) = 'LONG_LE_CAT_COMPANIONS';
        PRINT N'-> Đã cập nhật ImagePath cho LONG_LE_CAT_COMPANIONS.';
    END;

    COMMIT TRANSACTION;
    PRINT N'====================================================================';
    PRINT N'Hoàn tất cập nhật ImagePath cho Long Lê Con Mèo.';
    PRINT N'====================================================================';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

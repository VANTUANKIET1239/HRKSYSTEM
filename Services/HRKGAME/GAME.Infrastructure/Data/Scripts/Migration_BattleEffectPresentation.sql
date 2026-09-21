/* Effect icons and tooltip metadata used by the battle replay UI. */
USE [HRK];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.HRK_SkillEffectTypes', 'ImagePath') IS NULL
    ALTER TABLE dbo.HRK_SkillEffectTypes ADD ImagePath NVARCHAR(255) NULL;

IF COL_LENGTH('dbo.HRK_SkillEffectTypes', 'ColorHex') IS NULL
    ALTER TABLE dbo.HRK_SkillEffectTypes ADD ColorHex NVARCHAR(20) NULL;

UPDATE effectType SET
    ImagePath = source.ImagePath,
    ColorHex = source.ColorHex,
    Description = COALESCE(NULLIF(effectType.Description, N''), source.Description),
    UpdatedOn = SYSUTCDATETIME()
FROM dbo.HRK_SkillEffectTypes effectType
JOIN (VALUES
    (N'STUN', N'/assets/images/dcs-game/effects/stun.png', N'#facc15', N'Không thể hành động trong lượt.'),
    (N'SHIELD', N'/assets/images/dcs-game/effects/shield.png', N'#38bdf8', N'Hấp thụ sát thương trước khi trừ HP.'),
    (N'MARK', N'/assets/images/dcs-game/effects/mark.png', N'#ef4444', N'Tăng sát thương mục tiêu phải nhận.'),
    (N'SILENCE', N'/assets/images/dcs-game/effects/silence.png', N'#a855f7', N'Không thể sử dụng kỹ năng năng lượng.'),
    (N'DAMAGE_REDUCTION', N'/assets/images/dcs-game/effects/damage-reduction.png', N'#60a5fa', N'Giảm sát thương phải nhận.'),
    (N'TAUNT', N'/assets/images/dcs-game/effects/taunt.png', N'#f97316', N'Buộc đối thủ ưu tiên tấn công người khiêu khích.'),
    (N'DAMAGE_REFLECTION', N'/assets/images/dcs-game/effects/damage-reflection.png', N'#fb7185', N'Phản lại một phần sát thương đã nhận.'),
    (N'STAT_BUFF', N'/assets/images/dcs-game/effects/stat-buff.png', N'#22c55e', N'Tăng thuộc tính chiến đấu.'),
    (N'STAT_DEBUFF', N'/assets/images/dcs-game/effects/stat-debuff.png', N'#ec4899', N'Giảm thuộc tính chiến đấu.')
) source(Code, ImagePath, ColorHex, Description)
    ON UPPER(effectType.Code) = source.Code;

COMMIT TRANSACTION;
GO

SET NOCOUNT ON;
GO

UPDATE dbo.HRK_SkillTemplates
SET Description = N'Tấn công 4 địch ngẫu nhiên. Nếu hạ gục trên 2 mục tiêu, hồi 20% Máu tối đa và tăng 30% Công trong 2 lượt. Nếu thất bại, giảm 50% Thủ và bị câm lặng trong 2 lượt.',
    UpdatedOn = SYSUTCDATETIME()
WHERE Id = N'FATAL_ALL_IN_DIRECTIVE';
GO

-- STAT_DEBUFF and SILENCE remain as outcome definitions. The battle engine
-- applies them only when the cast defeats at most two targets; they must not
-- be applied during the normal per-effect pass.

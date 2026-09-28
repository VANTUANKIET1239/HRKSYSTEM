/* Nam Deadline: Vax-A-Million targets the straight/front enemy row. */
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @TargetCode NVARCHAR(100) = N'ENEMY_STRAIGHT_FRONT_ROW';
    DECLARE @SkillId NVARCHAR(100) = N'VAX_A_MILLION_SANITZATION';
    DECLARE @TargetTypeId INT;

    SELECT @TargetTypeId = Id
    FROM dbo.HRK_SkillTargetTypes
    WHERE Code = @TargetCode;

    IF @TargetTypeId IS NULL
    BEGIN
        DECLARE @DisplayOrder INT =
            ISNULL((SELECT MAX(DisplayOrder) + 1 FROM dbo.HRK_SkillTargetTypes), 1);

        INSERT INTO dbo.HRK_SkillTargetTypes
            (Code, Name, TargetSide, SelectionRule, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
        VALUES
            (@TargetCode, N'Hàng dọc phía trước', N'ENEMY', N'STRAIGHT_FRONT_ROW',
             @DisplayOrder, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

        SET @TargetTypeId = CONVERT(INT, SCOPE_IDENTITY());
    END
    ELSE
    BEGIN
        UPDATE dbo.HRK_SkillTargetTypes
        SET Name = N'Hàng dọc phía trước',
            TargetSide = N'ENEMY',
            SelectionRule = N'STRAIGHT_FRONT_ROW',
            IsActive = 1,
            UpdatedOn = SYSUTCDATETIME()
        WHERE Id = @TargetTypeId;
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = @SkillId)
        THROW 51000, 'Nam Deadline skill VAX_A_MILLION_SANITZATION was not found.', 1;

    UPDATE dbo.HRK_SkillEffects
    SET TargetTypeId = @TargetTypeId,
        UpdatedOn = SYSUTCDATETIME()
    WHERE SkillId = @SkillId
      AND IsActive = 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

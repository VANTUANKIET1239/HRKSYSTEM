SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @HeroManagementId INT =
    (
        SELECT Id
        FROM dbo.HRK_GameFeatureConfigs
        WHERE Code = N'HERO_MANAGEMENT'
    );

    IF @HeroManagementId IS NULL
    BEGIN
        THROW 51000, 'Thiếu feature config HERO_MANAGEMENT.', 1;
    END;

    ;WITH HeroTabs AS
    (
        SELECT *
        FROM
        (
            VALUES
                (N'HERO_LEVEL_UP', N'Nâng cấp', N'bi-arrow-up-circle-fill', N'HERO_LEVEL_UP', 10),
                (N'HERO_STAR_UPGRADE', N'Tăng sao', N'bi-stars', N'HERO_STAR_UPGRADE', 20),
                (N'HERO_AURA', N'Hào quang', N'bi-brightness-high-fill', N'HERO_AURA', 30),
                (N'HERO_BOND', N'Kích duyên', N'bi-diagram-3-fill', N'HERO_BOND', 40)
        ) AS source(Code, Name, Icon, ActionCode, DisplayOrder)
    )
    MERGE dbo.HRK_GameFeatureConfigs AS target
    USING HeroTabs AS source
        ON target.Code = source.Code
    WHEN MATCHED THEN
        UPDATE SET
            Name = source.Name,
            Icon = source.Icon,
            ParentFeatureId = @HeroManagementId,
            Placement = N'HERO_DETAIL_TAB',
            ActionCode = source.ActionCode,
            DisplayOrder = source.DisplayOrder,
            IsEnabled = 1,
            IsLocked = 0,
            UpdatedOn = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT
        (
            Code,
            Name,
            Icon,
            ParentFeatureId,
            Placement,
            ActionCode,
            DisplayOrder,
            IsEnabled,
            IsLocked,
            HasNotification
        )
        VALUES
        (
            source.Code,
            source.Name,
            source.Icon,
            @HeroManagementId,
            N'HERO_DETAIL_TAB',
            source.ActionCode,
            source.DisplayOrder,
            1,
            0,
            0
        );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;

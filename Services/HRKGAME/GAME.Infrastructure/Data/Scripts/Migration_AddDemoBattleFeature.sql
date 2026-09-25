SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.HRK_GameFeatureConfigs', N'U') IS NOT NULL
BEGIN
    MERGE dbo.HRK_GameFeatureConfigs AS target
    USING (VALUES
        (N'DEMO_BATTLE', N'Demo Battle', N'bi-controller', N'BOTTOM_RIGHT', N'DEMO_BATTLE', 35, 1, 0, 0)
    ) AS source(Code, Name, Icon, Placement, ActionCode, DisplayOrder, IsEnabled, IsLocked, HasNotification)
    ON target.Code = source.Code
    WHEN MATCHED THEN UPDATE SET
        Name = source.Name,
        Icon = source.Icon,
        ParentFeatureId = NULL,
        Placement = source.Placement,
        ActionCode = source.ActionCode,
        DisplayOrder = source.DisplayOrder,
        IsEnabled = source.IsEnabled,
        IsLocked = source.IsLocked,
        HasNotification = source.HasNotification,
        UpdatedOn = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (Code, Name, Icon, ParentFeatureId, Placement, ActionCode, DisplayOrder, IsEnabled, IsLocked, HasNotification)
        VALUES (source.Code, source.Name, source.Icon, NULL, source.Placement, source.ActionCode, source.DisplayOrder, source.IsEnabled, source.IsLocked, source.HasNotification);
END

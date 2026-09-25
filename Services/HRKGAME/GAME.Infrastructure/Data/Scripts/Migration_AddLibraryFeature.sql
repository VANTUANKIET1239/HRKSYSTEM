SET NOCOUNT ON;
GO
MERGE dbo.HRK_GameFeatureConfigs AS target
USING (SELECT N'LIBRARY' AS Code, N'Thư viện' AS Name, N'bi-book-half' AS Icon, N'BOTTOM_LEFT' AS Placement, N'LIBRARY' AS ActionCode, 50 AS DisplayOrder) AS source
ON target.Code = source.Code
WHEN MATCHED THEN UPDATE SET Name = source.Name, Icon = source.Icon, Placement = source.Placement, ActionCode = source.ActionCode, DisplayOrder = source.DisplayOrder, IsEnabled = 1, IsLocked = 0, UpdatedOn = SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT (Code, Name, Icon, ParentFeatureId, Placement, ActionCode, DisplayOrder, IsEnabled, IsLocked, HasNotification)
VALUES (source.Code, source.Name, source.Icon, NULL, source.Placement, source.ActionCode, source.DisplayOrder, 1, 0, 0);
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.HRK_ApplicationRouteConfigs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_ApplicationRouteConfigs
    (
        Id           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HRK_ApplicationRouteConfigs PRIMARY KEY,
        AppCode      NVARCHAR(50)  NOT NULL,
        RoutePrefix  NVARCHAR(200) NOT NULL,
        ApiPrefix    NVARCHAR(200) NOT NULL,
        Audience     NVARCHAR(100) NOT NULL,
        DefaultRoute NVARCHAR(300) NOT NULL,
        LoginTitle   NVARCHAR(200) NOT NULL,
        ThemeClass   NVARCHAR(100) NULL,
        SortOrder    INT NOT NULL CONSTRAINT DF_HRK_ApplicationRouteConfigs_SortOrder DEFAULT (0),
        IsActive     BIT NOT NULL CONSTRAINT DF_HRK_ApplicationRouteConfigs_IsActive DEFAULT (1),
        CONSTRAINT UQ_HRK_ApplicationRouteConfigs_AppCode UNIQUE (AppCode),
        CONSTRAINT UQ_HRK_ApplicationRouteConfigs_Audience UNIQUE (Audience)
    );
END;

MERGE dbo.HRK_ApplicationRouteConfigs AS target
USING (VALUES
    (N'dcs-game', N'/dcs-game', N'/gateway/dcs-game', N'game-api', N'/dcs-game/home', N'Đăng nhập DCS Game', N'game-theme', 10, 1),
    (N'hrm',      N'/hrm',      N'/gateway/hrm',      N'hrm-api',  N'/hrm/home',      N'Đăng nhập HRM',      N'hrm-theme',  20, 1),
    (N'crm',      N'/crm',      N'/gateway/crm',      N'crm-api',  N'/crm/home',      N'Đăng nhập CRM',      N'crm-theme',  30, 1),
    (N'shell',    N'/',         N'/gateway/auth',     N'auth-api', N'/dcs-game/home', N'Đăng nhập HRK',      N'hrk-theme',  100, 1)
) AS source (AppCode, RoutePrefix, ApiPrefix, Audience, DefaultRoute, LoginTitle, ThemeClass, SortOrder, IsActive)
ON target.AppCode = source.AppCode
WHEN MATCHED THEN UPDATE SET
    RoutePrefix = source.RoutePrefix,
    ApiPrefix = source.ApiPrefix,
    Audience = source.Audience,
    DefaultRoute = source.DefaultRoute,
    LoginTitle = source.LoginTitle,
    ThemeClass = source.ThemeClass,
    SortOrder = source.SortOrder,
    IsActive = source.IsActive
WHEN NOT MATCHED THEN INSERT
    (AppCode, RoutePrefix, ApiPrefix, Audience, DefaultRoute, LoginTitle, ThemeClass, SortOrder, IsActive)
VALUES
    (source.AppCode, source.RoutePrefix, source.ApiPrefix, source.Audience, source.DefaultRoute, source.LoginTitle, source.ThemeClass, source.SortOrder, source.IsActive);

COMMIT TRANSACTION;

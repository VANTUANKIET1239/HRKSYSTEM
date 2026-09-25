/*
    Global game navigation and combat-power configuration.
    This script is idempotent: it creates missing tables and updates the default rows.
*/
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.HRK_GameFeatureConfigs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_GameFeatureConfigs
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HRK_GameFeatureConfigs PRIMARY KEY,
        Code NVARCHAR(80) NOT NULL,
        Name NVARCHAR(120) NOT NULL,
        Icon NVARCHAR(100) NULL,
        ParentFeatureId INT NULL,
        Placement NVARCHAR(40) NOT NULL CONSTRAINT DF_HRK_GameFeatureConfigs_Placement DEFAULT ('NONE'),
        ActionCode NVARCHAR(80) NULL,
        DisplayOrder INT NOT NULL CONSTRAINT DF_HRK_GameFeatureConfigs_DisplayOrder DEFAULT (0),
        IsEnabled BIT NOT NULL CONSTRAINT DF_HRK_GameFeatureConfigs_IsEnabled DEFAULT (1),
        IsLocked BIT NOT NULL CONSTRAINT DF_HRK_GameFeatureConfigs_IsLocked DEFAULT (0),
        HasNotification BIT NOT NULL CONSTRAINT DF_HRK_GameFeatureConfigs_HasNotification DEFAULT (0),
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_GameFeatureConfigs_CreatedOn DEFAULT (SYSUTCDATETIME()),
        UpdatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_GameFeatureConfigs_UpdatedOn DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_HRK_GameFeatureConfigs_Code UNIQUE (Code),
        CONSTRAINT FK_HRK_GameFeatureConfigs_Parent FOREIGN KEY (ParentFeatureId)
            REFERENCES dbo.HRK_GameFeatureConfigs(Id)
    );
END
GO

IF OBJECT_ID(N'dbo.HRK_CombatPowerConfigs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_CombatPowerConfigs
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HRK_CombatPowerConfigs PRIMARY KEY,
        StatCode NVARCHAR(40) NOT NULL,
        PowerPerUnit DECIMAL(18,4) NOT NULL,
        IsEnabled BIT NOT NULL CONSTRAINT DF_HRK_CombatPowerConfigs_IsEnabled DEFAULT (1),
        DisplayOrder INT NOT NULL CONSTRAINT DF_HRK_CombatPowerConfigs_DisplayOrder DEFAULT (0),
        UpdatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_CombatPowerConfigs_UpdatedOn DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_HRK_CombatPowerConfigs_StatCode UNIQUE (StatCode)
    );
END
GO

MERGE dbo.HRK_GameFeatureConfigs AS target
USING (VALUES
    ('FIRST_TOP_UP',       N'Nạp đầu',      'bi-gift-fill',            NULL, 'TOP_EVENT',    'FIRST_TOP_UP',              10, 1, 0, 1),
    ('SUMMON',             N'Chiêu mộ',     'bi-people-fill',          NULL, 'TOP_EVENT',    'SUMMON',                    20, 1, 0, 1),
    ('EVENTS',             N'Sự kiện',      'bi-calendar-event-fill',  NULL, 'TOP_EVENT',    'EVENTS',                    30, 1, 0, 1),
    ('BENEFITS',           N'Phúc lợi',     'bi-award-fill',           NULL, 'TOP_EVENT',    'BENEFITS',                  40, 1, 0, 1),
    ('TALENT',             N'Chiêu tài',    'bi-coin',                 NULL, 'TOP_EVENT',    'TALENT',                    50, 1, 0, 1),
    ('CHECK_IN',           N'Báo danh',     'bi-check-circle-fill',    NULL, 'TOP_EVENT',    'CHECK_IN',                  60, 1, 0, 1),
    ('HERO_MANAGEMENT',    N'Võ tướng',     'bi-shield-shaded',        NULL, 'BOTTOM_LEFT',  'HERO_MANAGEMENT',           10, 1, 0, 0),
    ('FORMATION',          N'Đội ngũ',      'bi-grid-3x3-gap-fill',    NULL, 'BOTTOM_LEFT',  'FORMATION_MANAGEMENT',       20, 1, 0, 0),
    ('INVENTORY',          N'Hành trang',   'bi-briefcase-fill',       NULL, 'BOTTOM_LEFT',  'INVENTORY',                 30, 1, 0, 0),
    ('FORGE',              N'Rèn',          'bi-hammer',               NULL, 'BOTTOM_LEFT',  'FORGE',                     40, 1, 0, 0),
    ('LIBRARY',            N'Thư viện',      'bi-book-half',            NULL, 'BOTTOM_LEFT',  'LIBRARY',                   50, 1, 0, 0),
    ('ESCORT',             N'Hộ tống',      'bi-truck',                NULL, 'BOTTOM_RIGHT', 'ESCORT',                    10, 1, 0, 0),
    ('EXPEDITION',         N'Xuất chinh',   'bi-compass',              NULL, 'BOTTOM_RIGHT', 'EXPEDITION',                20, 1, 0, 1),
    ('CAMPAIGN',           N'Chiến dịch',   'bi-map-fill',             NULL, 'BOTTOM_RIGHT', 'CAMPAIGN',                  30, 1, 0, 0),
    ('DEMO_BATTLE',        N'Demo Battle',  'bi-controller',           NULL, 'BOTTOM_RIGHT', 'DEMO_BATTLE',               35, 1, 0, 0),
    ('BATTLE',             N'Chính tuyến',  'bi-swords',               NULL, 'BOTTOM_FOCUS', 'BATTLE',                    40, 1, 0, 0),
    ('HERO_LEVEL_UP',      N'Nâng cấp',     'bi-arrow-up-circle-fill', 'HERO_MANAGEMENT', 'HERO_ACTION', 'HERO_LEVEL_UP', 10, 1, 0, 0),
    ('HERO_QUICK_BREAK',   N'Đột phá nhanh','bi-lightning-fill',        'HERO_MANAGEMENT', 'HERO_ACTION', 'HERO_QUICK_BREAK', 20, 1, 0, 0),
    ('HERO_EQUIP_BEST',    N'Mặc nhanh',    'bi-shield-check',          'HERO_MANAGEMENT', 'HERO_ACTION', 'HERO_EQUIP_BEST', 30, 1, 0, 0)
) AS source(Code, Name, Icon, ParentCode, Placement, ActionCode, DisplayOrder, IsEnabled, IsLocked, HasNotification)
ON target.Code = source.Code
WHEN MATCHED THEN UPDATE SET
    Name = source.Name, Icon = source.Icon, Placement = source.Placement, ActionCode = source.ActionCode,
    DisplayOrder = source.DisplayOrder, IsEnabled = source.IsEnabled, IsLocked = source.IsLocked,
    HasNotification = source.HasNotification, UpdatedOn = SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT (Code, Name, Icon, Placement, ActionCode, DisplayOrder, IsEnabled, IsLocked, HasNotification)
    VALUES (source.Code, source.Name, source.Icon, source.Placement, source.ActionCode, source.DisplayOrder, source.IsEnabled, source.IsLocked, source.HasNotification);
GO

UPDATE child SET ParentFeatureId = parent.Id, UpdatedOn = SYSUTCDATETIME()
FROM dbo.HRK_GameFeatureConfigs child
JOIN (VALUES ('HERO_LEVEL_UP', 'HERO_MANAGEMENT'), ('HERO_QUICK_BREAK', 'HERO_MANAGEMENT'), ('HERO_EQUIP_BEST', 'HERO_MANAGEMENT')) x(ChildCode, ParentCode)
    ON x.ChildCode = child.Code
JOIN dbo.HRK_GameFeatureConfigs parent ON parent.Code = x.ParentCode;
GO

MERGE dbo.HRK_CombatPowerConfigs AS target
USING (VALUES
    ('HP',          CAST(0.0500 AS DECIMAL(18,4)),  10, 1),
    ('ATK',         CAST(1.5000 AS DECIMAL(18,4)),  20, 1),
    ('DEF',         CAST(1.0000 AS DECIMAL(18,4)),  30, 1),
    ('SPD',         CAST(1.0000 AS DECIMAL(18,4)),  40, 1),
    ('CRIT_RATE',   CAST(10.0000 AS DECIMAL(18,4)), 50, 1),
    ('CRIT_DAMAGE', CAST(0.2000 AS DECIMAL(18,4)),  60, 1),
    ('LIFESTEAL',   CAST(8.0000 AS DECIMAL(18,4)),  70, 1),
    ('ACCURACY',    CAST(0.5000 AS DECIMAL(18,4)),  80, 1),
    ('RESISTANCE',  CAST(0.5000 AS DECIMAL(18,4)),  90, 1)
) AS source(StatCode, PowerPerUnit, DisplayOrder, IsEnabled)
ON target.StatCode = source.StatCode
WHEN MATCHED THEN UPDATE SET PowerPerUnit = source.PowerPerUnit, DisplayOrder = source.DisplayOrder,
    IsEnabled = source.IsEnabled, UpdatedOn = SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT (StatCode, PowerPerUnit, DisplayOrder, IsEnabled)
    VALUES (source.StatCode, source.PowerPerUnit, source.DisplayOrder, source.IsEnabled);
GO

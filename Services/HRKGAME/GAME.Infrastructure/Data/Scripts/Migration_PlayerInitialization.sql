USE HRK;
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.HRK_PlayerInitializationConfigs',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_PlayerInitializationConfigs
    (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_HRK_PlayerInitializationConfigs PRIMARY KEY,
        Code nvarchar(50) NOT NULL, Version int NOT NULL, IsActive bit NOT NULL DEFAULT(0),
        InitialLevel int NOT NULL, InitialStamina int NOT NULL, MaxStamina int NOT NULL,
        InitialGold bigint NOT NULL, InitialDiamonds int NOT NULL, InitialUpgradeMaterials int NOT NULL,
        InventoryCapacity int NOT NULL, DefaultFormationTemplateId int NOT NULL, DefaultAvatarTemplateId int NULL,
        CreatedAtUtc datetime2 NOT NULL DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT UQ_HRK_PlayerInitializationConfigs_Version UNIQUE(Code,Version),
        CONSTRAINT CK_HRK_PlayerInitializationConfigs_Values CHECK(Version>0 AND InitialLevel>0 AND
            MaxStamina>0 AND InitialStamina BETWEEN 0 AND MaxStamina AND InitialGold>=0 AND InitialDiamonds>=0 AND
            InitialUpgradeMaterials>=0 AND InventoryCapacity>0),
        CONSTRAINT FK_HRK_PlayerInitializationConfigs_Formation FOREIGN KEY(DefaultFormationTemplateId) REFERENCES dbo.HRK_FormationTemplates(Id),
        CONSTRAINT FK_HRK_PlayerInitializationConfigs_Avatar FOREIGN KEY(DefaultAvatarTemplateId) REFERENCES dbo.HRK_AvatarTemplates(Id)
    );
    CREATE UNIQUE INDEX UX_HRK_PlayerInitializationConfigs_Active ON dbo.HRK_PlayerInitializationConfigs(Code) WHERE IsActive=1;
END;
IF OBJECT_ID(N'dbo.HRK_PlayerInitializationHeroes',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_PlayerInitializationHeroes
    (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_HRK_PlayerInitializationHeroes PRIMARY KEY,
        ConfigId bigint NOT NULL, HeroTemplateId int NOT NULL, InitialLevel int NOT NULL DEFAULT(1),
        InitialStars int NOT NULL DEFAULT(1), FormationPosition int NULL, DisplayOrder int NOT NULL DEFAULT(0),
        CONSTRAINT UQ_HRK_PlayerInitializationHeroes_Hero UNIQUE(ConfigId,HeroTemplateId),
        CONSTRAINT CK_HRK_PlayerInitializationHeroes_Values CHECK(InitialLevel=1 AND InitialStars=1 AND (FormationPosition IS NULL OR FormationPosition BETWEEN 1 AND 5)),
        CONSTRAINT FK_HRK_PlayerInitializationHeroes_Config FOREIGN KEY(ConfigId) REFERENCES dbo.HRK_PlayerInitializationConfigs(Id),
        CONSTRAINT FK_HRK_PlayerInitializationHeroes_Hero FOREIGN KEY(HeroTemplateId) REFERENCES dbo.HRK_HeroTemplates(Id)
    );
    CREATE UNIQUE INDEX UX_HRK_PlayerInitializationHeroes_Position ON dbo.HRK_PlayerInitializationHeroes(ConfigId,FormationPosition) WHERE FormationPosition IS NOT NULL;
END;
IF OBJECT_ID(N'dbo.HRK_PlayerInitializationItems',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_PlayerInitializationItems
    (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_HRK_PlayerInitializationItems PRIMARY KEY,
        ConfigId bigint NOT NULL, ItemTemplateId int NOT NULL, Quantity int NOT NULL, DisplayOrder int NOT NULL DEFAULT(0),
        CONSTRAINT CK_HRK_PlayerInitializationItems_Quantity CHECK(Quantity>0),
        CONSTRAINT UQ_HRK_PlayerInitializationItems_Item UNIQUE(ConfigId,ItemTemplateId),
        CONSTRAINT FK_HRK_PlayerInitializationItems_Config FOREIGN KEY(ConfigId) REFERENCES dbo.HRK_PlayerInitializationConfigs(Id),
        CONSTRAINT FK_HRK_PlayerInitializationItems_Item FOREIGN KEY(ItemTemplateId) REFERENCES dbo.HRK_ItemTemplates(Id)
    );
END;
IF OBJECT_ID(N'dbo.HRK_PlayerInitializations',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_PlayerInitializations
    (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_HRK_PlayerInitializations PRIMARY KEY,
        UserId nvarchar(450) NOT NULL, PlayerId bigint NULL, ConfigId bigint NULL,
        RegistrationEventId uniqueidentifier NOT NULL, Status nvarchar(20) NOT NULL,
        CreatedAtUtc datetime2 NOT NULL DEFAULT(SYSUTCDATETIME()), CompletedAtUtc datetime2 NULL,
        CONSTRAINT UQ_HRK_PlayerInitializations_User UNIQUE(UserId),
        CONSTRAINT UQ_HRK_PlayerInitializations_Event UNIQUE(RegistrationEventId),
        CONSTRAINT CK_HRK_PlayerInitializations_Status CHECK
            ((Status=N'PENDING' AND ConfigId IS NOT NULL AND PlayerId IS NULL AND CompletedAtUtc IS NULL)
            OR (Status=N'COMPLETED' AND PlayerId IS NOT NULL AND CompletedAtUtc IS NOT NULL)),
        CONSTRAINT FK_HRK_PlayerInitializations_Config FOREIGN KEY(ConfigId) REFERENCES dbo.HRK_PlayerInitializationConfigs(Id),
        CONSTRAINT FK_HRK_PlayerInitializations_Player FOREIGN KEY(PlayerId) REFERENCES dbo.HRK_Players(Id)
    );
END;
COMMIT;
GO
-- SQL triggers enforce immutability after the first receipt pins a config, including pending players.
CREATE OR ALTER TRIGGER dbo.TR_HRK_PlayerInitializationConfigs_Immutable
ON dbo.HRK_PlayerInitializationConfigs AFTER UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted d JOIN dbo.HRK_PlayerInitializations p ON p.ConfigId=d.Id
        LEFT JOIN inserted i ON i.Id=d.Id WHERE i.Id IS NULL OR EXISTS
        (SELECT d.Code,d.Version,d.InitialLevel,d.InitialStamina,d.MaxStamina,d.InitialGold,d.InitialDiamonds,
                d.InitialUpgradeMaterials,d.InventoryCapacity,d.DefaultFormationTemplateId,d.DefaultAvatarTemplateId,d.CreatedAtUtc
         EXCEPT
         SELECT i.Code,i.Version,i.InitialLevel,i.InitialStamina,i.MaxStamina,i.InitialGold,i.InitialDiamonds,
                i.InitialUpgradeMaterials,i.InventoryCapacity,i.DefaultFormationTemplateId,i.DefaultAvatarTemplateId,i.CreatedAtUtc))
        THROW 51000, 'Used initialization configs are immutable. Create a new version.', 1;
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_HRK_PlayerInitializationHeroes_Immutable
ON dbo.HRK_PlayerInitializationHeroes AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.HRK_PlayerInitializations p JOIN
        (SELECT ConfigId FROM inserted UNION SELECT ConfigId FROM deleted) c ON c.ConfigId=p.ConfigId)
        THROW 51000, 'Used initialization hero configs are immutable. Create a new version.', 1;
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_HRK_PlayerInitializationItems_Immutable
ON dbo.HRK_PlayerInitializationItems AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.HRK_PlayerInitializations p JOIN
        (SELECT ConfigId FROM inserted UNION SELECT ConfigId FROM deleted) c ON c.ConfigId=p.ConfigId)
        THROW 51000, 'Used initialization item configs are immutable. Create a new version.', 1;
END;
GO

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.HRK_AvatarTemplates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_AvatarTemplates
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HRK_AvatarTemplates PRIMARY KEY,
        Code NVARCHAR(80) NOT NULL CONSTRAINT UQ_HRK_AvatarTemplates_Code UNIQUE,
        Name NVARCHAR(120) NOT NULL,
        ImagePath NVARCHAR(500) NOT NULL,
        DisplayOrder INT NOT NULL CONSTRAINT DF_HRK_AvatarTemplates_DisplayOrder DEFAULT (0),
        IsDefault BIT NOT NULL CONSTRAINT DF_HRK_AvatarTemplates_IsDefault DEFAULT (0),
        IsEnabled BIT NOT NULL CONSTRAINT DF_HRK_AvatarTemplates_IsEnabled DEFAULT (1),
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_AvatarTemplates_CreatedOn DEFAULT (SYSUTCDATETIME()),
        UpdatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_AvatarTemplates_UpdatedOn DEFAULT (SYSUTCDATETIME())
    );
END
GO

IF COL_LENGTH(N'dbo.HRK_Players', N'AvatarType') IS NULL
    ALTER TABLE dbo.HRK_Players ADD AvatarType VARCHAR(20) NOT NULL CONSTRAINT DF_HRK_Players_AvatarType DEFAULT ('TEMPLATE');
GO
IF COL_LENGTH(N'dbo.HRK_Players', N'AvatarTemplateId') IS NULL
    ALTER TABLE dbo.HRK_Players ADD AvatarTemplateId INT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_HRK_Players_AvatarType')
    ALTER TABLE dbo.HRK_Players ADD CONSTRAINT CK_HRK_Players_AvatarType CHECK (AvatarType IN ('TEMPLATE', 'CUSTOM'));
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_HRK_Players_AvatarTemplates')
    ALTER TABLE dbo.HRK_Players ADD CONSTRAINT FK_HRK_Players_AvatarTemplates FOREIGN KEY (AvatarTemplateId) REFERENCES dbo.HRK_AvatarTemplates(Id) ON DELETE SET NULL;
GO

IF OBJECT_ID(N'dbo.HRK_PlayerCustomAvatars', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_PlayerCustomAvatars
    (
        PlayerId BIGINT NOT NULL CONSTRAINT PK_HRK_PlayerCustomAvatars PRIMARY KEY,
        ImageData VARBINARY(MAX) NOT NULL,
        ContentType VARCHAR(50) NOT NULL,
        FileName NVARCHAR(255) NULL,
        FileSize INT NOT NULL,
        Width INT NOT NULL,
        Height INT NOT NULL,
        ContentHash CHAR(64) NOT NULL,
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_PlayerCustomAvatars_CreatedOn DEFAULT (SYSUTCDATETIME()),
        UpdatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_PlayerCustomAvatars_UpdatedOn DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_HRK_PlayerCustomAvatars_Players FOREIGN KEY (PlayerId) REFERENCES dbo.HRK_Players(Id) ON DELETE CASCADE,
        CONSTRAINT CK_HRK_PlayerCustomAvatars_FileSize CHECK (FileSize > 0 AND FileSize <= 1048576),
        CONSTRAINT CK_HRK_PlayerCustomAvatars_Dimensions CHECK (Width > 0 AND Height > 0 AND Width <= 1024 AND Height <= 1024)
    );
END
GO

-- Reuse hero portraits as the initial built-in avatar collection.
MERGE dbo.HRK_AvatarTemplates AS target
USING
(
    SELECT CONCAT(N'HERO_', Id) AS Code, Name, Avatar AS ImagePath, Id AS DisplayOrder
    FROM dbo.HRK_HeroTemplates
    WHERE NULLIF(LTRIM(RTRIM(Avatar)), N'') IS NOT NULL
) AS source
ON target.Code = source.Code
WHEN MATCHED THEN UPDATE SET Name = source.Name, ImagePath = source.ImagePath, DisplayOrder = source.DisplayOrder, IsEnabled = 1, UpdatedOn = SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT (Code, Name, ImagePath, DisplayOrder, IsDefault, IsEnabled)
VALUES (source.Code, source.Name, source.ImagePath, source.DisplayOrder, 0, 1);
GO

UPDATE dbo.HRK_AvatarTemplates
SET IsDefault = CASE WHEN Id = (SELECT MIN(Id) FROM dbo.HRK_AvatarTemplates WHERE IsEnabled = 1) THEN 1 ELSE 0 END;
GO

UPDATE p SET AvatarTemplateId = a.Id
FROM dbo.HRK_Players p
CROSS APPLY (SELECT TOP 1 Id FROM dbo.HRK_AvatarTemplates WHERE IsDefault = 1 AND IsEnabled = 1 ORDER BY Id) a
WHERE p.AvatarTemplateId IS NULL AND p.AvatarType = 'TEMPLATE';
GO

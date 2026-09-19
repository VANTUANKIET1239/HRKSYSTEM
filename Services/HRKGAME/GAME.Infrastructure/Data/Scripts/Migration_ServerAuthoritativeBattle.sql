USE [HRK];
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('dbo.HRK_Battles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_Battles
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HRK_Battles PRIMARY KEY,
        BattleId UNIQUEIDENTIFIER NOT NULL,
        PlayerId BIGINT NOT NULL,
        BattleType NVARCHAR(30) NOT NULL,
        StageId INT NULL,
        FormationId BIGINT NULL,
        RandomSeed INT NOT NULL,
        Status NVARCHAR(20) NOT NULL,
        WinnerSide NVARCHAR(10) NULL,
        LastTurn INT NOT NULL CONSTRAINT DF_HRK_Battles_LastTurn DEFAULT 0,
        RewardClaimedOn DATETIME2 NULL,
        StartedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_Battles_StartedOn DEFAULT SYSUTCDATETIME(),
        CompletedOn DATETIME2 NULL,
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_Battles_CreatedOn DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_HRK_Battles_BattleId UNIQUE (BattleId),
        CONSTRAINT FK_HRK_Battles_Player FOREIGN KEY (PlayerId) REFERENCES dbo.HRK_Players(Id),
        CONSTRAINT FK_HRK_Battles_Formation FOREIGN KEY (FormationId) REFERENCES dbo.HRK_PlayerFormations(Id),
        CONSTRAINT CHK_HRK_Battles_Status CHECK (Status IN ('CREATED','COMPLETED','CLAIMED','FAILED')),
        CONSTRAINT CHK_HRK_Battles_Winner CHECK (WinnerSide IS NULL OR WinnerSide IN ('LEFT','RIGHT','DRAW'))
    );
    CREATE INDEX IX_HRK_Battles_Player_CreatedOn ON dbo.HRK_Battles(PlayerId, CreatedOn DESC);
END;

IF OBJECT_ID('dbo.HRK_BattleParticipants', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_BattleParticipants
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HRK_BattleParticipants PRIMARY KEY,
        BattleId UNIQUEIDENTIFIER NOT NULL,
        BattleHeroId BIGINT NOT NULL,
        SourceHeroId BIGINT NULL,
        HeroTemplateId INT NOT NULL,
        TeamSide TINYINT NOT NULL,
        Position INT NOT NULL,
        Name NVARCHAR(150) NOT NULL,
        Avatar NVARCHAR(500) NULL,
        StatsSnapshotJson NVARCHAR(MAX) NOT NULL,
        SkillsSnapshotJson NVARCHAR(MAX) NOT NULL,
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_BattleParticipants_CreatedOn DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_HRK_BattleParticipants_Battle FOREIGN KEY (BattleId)
            REFERENCES dbo.HRK_Battles(BattleId) ON DELETE CASCADE,
        CONSTRAINT UQ_HRK_BattleParticipants_BattleHero UNIQUE (BattleId, BattleHeroId),
        CONSTRAINT CHK_HRK_BattleParticipants_Team CHECK (TeamSide IN (0,1)),
        CONSTRAINT CHK_HRK_BattleParticipants_Position CHECK (Position BETWEEN 1 AND 5),
        CONSTRAINT CHK_HRK_BattleParticipants_StatsJson CHECK (ISJSON(StatsSnapshotJson) = 1),
        CONSTRAINT CHK_HRK_BattleParticipants_SkillsJson CHECK (ISJSON(SkillsSnapshotJson) = 1)
    );
END;

IF OBJECT_ID('dbo.HRK_BattleEvents', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_BattleEvents
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HRK_BattleEvents PRIMARY KEY,
        BattleId UNIQUEIDENTIFIER NOT NULL,
        Sequence INT NOT NULL,
        Round INT NOT NULL,
        Turn INT NOT NULL,
        EventType NVARCHAR(40) NOT NULL,
        ActorBattleHeroId BIGINT NULL,
        TargetBattleHeroId BIGINT NULL,
        SkillId NVARCHAR(100) NULL,
        EffectTypeCode NVARCHAR(50) NULL,
        DamageSchoolCode NVARCHAR(20) NULL,
        Value INT NOT NULL CONSTRAINT DF_HRK_BattleEvents_Value DEFAULT 0,
        IsCrit BIT NOT NULL CONSTRAINT DF_HRK_BattleEvents_IsCrit DEFAULT 0,
        PayloadJson NVARCHAR(MAX) NULL,
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_BattleEvents_CreatedOn DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_HRK_BattleEvents_Battle FOREIGN KEY (BattleId)
            REFERENCES dbo.HRK_Battles(BattleId) ON DELETE CASCADE,
        CONSTRAINT UQ_HRK_BattleEvents_Sequence UNIQUE (BattleId, Sequence),
        CONSTRAINT CHK_HRK_BattleEvents_PayloadJson CHECK (PayloadJson IS NULL OR ISJSON(PayloadJson) = 1),
        CONSTRAINT CHK_HRK_BattleEvents_DamageSchool CHECK
            (DamageSchoolCode IS NULL OR DamageSchoolCode IN ('PHYSICAL','MAGIC','TRUE'))
    );
    CREATE INDEX IX_HRK_BattleEvents_Replay ON dbo.HRK_BattleEvents(BattleId, Sequence);
END;

COMMIT TRANSACTION;
GO

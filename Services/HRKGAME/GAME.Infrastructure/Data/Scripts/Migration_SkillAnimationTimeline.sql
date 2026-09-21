/*
    Presentation-only skill timeline. Battle calculation remains synchronous;
    the client replays authoritative events at these offsets.
*/
USE [HRK];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.HRK_SkillAnimationConfigs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_SkillAnimationConfigs
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HRK_SkillAnimationConfigs PRIMARY KEY,
        SkillId NVARCHAR(100) NOT NULL,
        AnimationKey NVARCHAR(100) NOT NULL,
        TotalDurationMs INT NOT NULL,
        DefaultPlaybackSpeed DECIMAL(5,2) NOT NULL CONSTRAINT DF_HRK_SkillAnimationConfigs_Speed DEFAULT (1),
        IsActive BIT NOT NULL CONSTRAINT DF_HRK_SkillAnimationConfigs_Active DEFAULT (1),
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_SkillAnimationConfigs_Created DEFAULT SYSUTCDATETIME(),
        UpdatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_SkillAnimationConfigs_Updated DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_HRK_SkillAnimationConfigs_Skill UNIQUE (SkillId),
        CONSTRAINT FK_HRK_SkillAnimationConfigs_Skill FOREIGN KEY (SkillId)
            REFERENCES dbo.HRK_SkillTemplates(Id) ON DELETE CASCADE,
        CONSTRAINT CHK_HRK_SkillAnimationConfigs_Duration CHECK (TotalDurationMs > 0),
        CONSTRAINT CHK_HRK_SkillAnimationConfigs_Speed CHECK (DefaultPlaybackSpeed > 0)
    );
END;

IF OBJECT_ID(N'dbo.HRK_SkillTimelinePhases', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_SkillTimelinePhases
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HRK_SkillTimelinePhases PRIMARY KEY,
        SkillAnimationConfigId INT NOT NULL,
        PhaseCode NVARCHAR(30) NOT NULL,
        StartAtMs INT NOT NULL,
        DurationMs INT NOT NULL,
        TriggerEventType NVARCHAR(40) NULL,
        DisplayOrder INT NOT NULL CONSTRAINT DF_HRK_SkillTimelinePhases_Order DEFAULT (0),
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_SkillTimelinePhases_Created DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_HRK_SkillTimelinePhases_Config FOREIGN KEY (SkillAnimationConfigId)
            REFERENCES dbo.HRK_SkillAnimationConfigs(Id) ON DELETE CASCADE,
        CONSTRAINT UQ_HRK_SkillTimelinePhases_Phase UNIQUE (SkillAnimationConfigId, PhaseCode),
        CONSTRAINT CHK_HRK_SkillTimelinePhases_Start CHECK (StartAtMs >= 0),
        CONSTRAINT CHK_HRK_SkillTimelinePhases_Duration CHECK (DurationMs >= 0)
    );
END;

/* Defaults preserve the old UI fallback: NORMAL = 800+800+800, ENERGY = 800+800+800. */
MERGE dbo.HRK_SkillAnimationConfigs AS target
USING
(
    SELECT Id AS SkillId,
           LOWER(REPLACE(Id, '_', '-')) AS AnimationKey,
           CAST(2400 AS INT) AS TotalDurationMs
    FROM dbo.HRK_SkillTemplates
    WHERE IsActive = 1
) AS source
ON target.SkillId = source.SkillId
WHEN NOT MATCHED THEN INSERT (SkillId, AnimationKey, TotalDurationMs)
    VALUES (source.SkillId, source.AnimationKey, source.TotalDurationMs);

/* Durations migrated from mock-battle.data.ts. Missing phases used the previous 800ms fallback. */
UPDATE c SET
    TotalDurationMs = x.Phase1Ms + x.Phase2Ms + x.Phase3Ms,
    UpdatedOn = SYSUTCDATETIME()
FROM dbo.HRK_SkillAnimationConfigs c
JOIN (VALUES
    (N'RANDOM_KNOWLEDGE_DROP',   800, 1500,  800),
    (N'TACTICAL_AIR_STRIKE',     800, 1500,  800),
    (N'DOI_NGOI_DAU_DOC',        800, 1400,  800),
    (N'FATAL_ALL_IN_DIRECTIVE', 1500, 2200, 1200),
    (N'WINTER_NIGHT_BLESSINGS', 1600, 2000, 1200),
    (N'DEADLIFT_DIA_CHAN',       1800, 1500, 1200)
) x(SkillId, Phase1Ms, Phase2Ms, Phase3Ms) ON x.SkillId = c.SkillId;

;WITH Durations AS
(
    SELECT c.Id ConfigId, c.SkillId,
           COALESCE(x.Phase1Ms, 800) Phase1Ms,
           COALESCE(x.Phase2Ms, 800) Phase2Ms,
           COALESCE(x.Phase3Ms, 800) Phase3Ms
    FROM dbo.HRK_SkillAnimationConfigs c
    LEFT JOIN (VALUES
        (N'RANDOM_KNOWLEDGE_DROP',   800, 1500,  800),
        (N'TACTICAL_AIR_STRIKE',     800, 1500,  800),
        (N'DOI_NGOI_DAU_DOC',        800, 1400,  800),
        (N'FATAL_ALL_IN_DIRECTIVE', 1500, 2200, 1200),
        (N'WINTER_NIGHT_BLESSINGS', 1600, 2000, 1200),
        (N'DEADLIFT_DIA_CHAN',       1800, 1500, 1200)
    ) x(SkillId, Phase1Ms, Phase2Ms, Phase3Ms) ON x.SkillId = c.SkillId
), SourcePhases AS
(
    SELECT ConfigId, N'CAST' PhaseCode, 0 StartAtMs, Phase1Ms DurationMs,
           N'SKILL_CAST' TriggerEventType, 1 DisplayOrder FROM Durations
    UNION ALL
    /* DAMAGE is emitted once at StartAtMs + DurationMs, after all cosmetic hits. */
    SELECT ConfigId, N'IMPACT', Phase1Ms, Phase2Ms, N'DAMAGE', 2 FROM Durations
    UNION ALL
    SELECT ConfigId, N'STATUS', Phase1Ms + Phase2Ms, 0, N'STATUS_APPLIED', 3 FROM Durations
    UNION ALL
    SELECT ConfigId, N'RECOVERY', Phase1Ms + Phase2Ms, Phase3Ms, N'SKILL_COMPLETED', 4 FROM Durations
)
MERGE dbo.HRK_SkillTimelinePhases AS target
USING SourcePhases AS source
ON target.SkillAnimationConfigId = source.ConfigId AND target.PhaseCode = source.PhaseCode
WHEN MATCHED THEN UPDATE SET StartAtMs = source.StartAtMs, DurationMs = source.DurationMs,
    TriggerEventType = source.TriggerEventType, DisplayOrder = source.DisplayOrder
WHEN NOT MATCHED THEN INSERT
    (SkillAnimationConfigId, PhaseCode, StartAtMs, DurationMs, TriggerEventType, DisplayOrder)
    VALUES (source.ConfigId, source.PhaseCode, source.StartAtMs, source.DurationMs,
            source.TriggerEventType, source.DisplayOrder);

COMMIT TRANSACTION;
GO

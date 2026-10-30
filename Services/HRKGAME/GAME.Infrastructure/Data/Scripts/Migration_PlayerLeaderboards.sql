-- Run against GAME (HRK). Safe to rerun; never reconstruct spending from wallet balances.
USE HRK;
GO
SET XACT_ABORT ON;
SET NOCOUNT ON;
GO
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.HRK_PlayerRankingStats', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_PlayerRankingStats
    (
        PlayerId bigint NOT NULL CONSTRAINT PK_HRK_PlayerRankingStats PRIMARY KEY,
        CurrentFormationPower bigint NOT NULL CONSTRAINT DF_Ranking_Current DEFAULT 0,
        PeakFormationPower bigint NOT NULL CONSTRAINT DF_Ranking_Peak DEFAULT 0,
        PeakPowerAchievedAtUtc datetime2 NOT NULL CONSTRAINT DF_Ranking_PeakAt DEFAULT SYSUTCDATETIME(),
        TotalDiamondsSpent bigint NOT NULL CONSTRAINT DF_Ranking_Spent DEFAULT 0,
        DiamondsSpentAchievedAtUtc datetime2 NOT NULL CONSTRAINT DF_Ranking_SpentAt DEFAULT SYSUTCDATETIME(),
        TrackingStartedAtUtc datetime2 NOT NULL CONSTRAINT DF_Ranking_Started DEFAULT SYSUTCDATETIME(),
        UpdatedAtUtc datetime2 NOT NULL CONSTRAINT DF_Ranking_Updated DEFAULT SYSUTCDATETIME(),
        PowerInitialized bit NOT NULL CONSTRAINT DF_Ranking_Initialized DEFAULT 0,
        RowVersion rowversion NOT NULL,
        CONSTRAINT FK_Ranking_Player FOREIGN KEY (PlayerId) REFERENCES dbo.HRK_Players(Id),
        CONSTRAINT CK_Ranking_Scores CHECK (CurrentFormationPower >= 0 AND PeakFormationPower >= CurrentFormationPower AND TotalDiamondsSpent >= 0)
    );
END;
IF OBJECT_ID(N'dbo.HRK_PlayerDiamondSpendTransactions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_PlayerDiamondSpendTransactions
    (
        Id bigint IDENTITY NOT NULL CONSTRAINT PK_HRK_PlayerDiamondSpendTransactions PRIMARY KEY,
        PlayerId bigint NOT NULL,
        RequestId uniqueidentifier NOT NULL,
        Reason nvarchar(40) NOT NULL,
        Reference nvarchar(120) NOT NULL,
        Amount bigint NOT NULL,
        CreatedAtUtc datetime2 NOT NULL,
        CONSTRAINT FK_DiamondSpend_Player FOREIGN KEY (PlayerId) REFERENCES dbo.HRK_Players(Id),
        CONSTRAINT UQ_DiamondSpend_Request UNIQUE(PlayerId, RequestId),
        CONSTRAINT CK_DiamondSpend_Amount CHECK (Amount >= 0)
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.HRK_PlayerRankingStats') AND name=N'IX_PlayerRanking_Power')
    CREATE INDEX IX_PlayerRanking_Power ON dbo.HRK_PlayerRankingStats(PeakFormationPower DESC, PeakPowerAchievedAtUtc, PlayerId) WHERE PowerInitialized=1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.HRK_PlayerRankingStats') AND name=N'IX_PlayerRanking_Diamonds')
    CREATE INDEX IX_PlayerRanking_Diamonds ON dbo.HRK_PlayerRankingStats(TotalDiamondsSpent DESC, DiamondsSpentAchievedAtUtc, PlayerId);
-- Leave current power uninitialized: the worker uses the shared C# calculation, never stale formation snapshots.
INSERT dbo.HRK_PlayerRankingStats (PlayerId)
SELECT p.Id FROM dbo.HRK_Players p
WHERE p.IsActive=1 AND NOT EXISTS (SELECT 1 FROM dbo.HRK_PlayerRankingStats r WHERE r.PlayerId=p.Id);
IF OBJECT_ID(N'dbo.HRK_GameFeatureConfigs', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.HRK_GameFeatureConfigs WHERE Code=N'LEADERBOARD')
        UPDATE dbo.HRK_GameFeatureConfigs SET Name=N'Bảng xếp hạng', Icon=N'bi-trophy-fill', Placement=N'BOTTOM_RIGHT',
            ActionCode=N'LEADERBOARD', IsEnabled=1, IsLocked=0, UpdatedOn=SYSUTCDATETIME() WHERE Code=N'LEADERBOARD';
    ELSE
        INSERT dbo.HRK_GameFeatureConfigs(Code,Name,Icon,Placement,ActionCode,DisplayOrder,IsEnabled,IsLocked,HasNotification,CreatedOn,UpdatedOn)
        VALUES(N'LEADERBOARD',N'Bảng xếp hạng',N'bi-trophy-fill',N'BOTTOM_RIGHT',N'LEADERBOARD',40,1,0,0,SYSUTCDATETIME(),SYSUTCDATETIME());
END;
COMMIT;
GO

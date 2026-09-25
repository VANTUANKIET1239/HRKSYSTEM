SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.HRK_Players', 'Exp') IS NULL ALTER TABLE dbo.HRK_Players ADD Exp INT NOT NULL CONSTRAINT DF_HRK_Players_Exp DEFAULT 0;
IF COL_LENGTH('dbo.HRK_Players', 'MaxExp') IS NULL ALTER TABLE dbo.HRK_Players ADD MaxExp INT NOT NULL CONSTRAINT DF_HRK_Players_MaxExp DEFAULT 1000;
IF COL_LENGTH('dbo.HRK_Players', 'Stamina') IS NULL ALTER TABLE dbo.HRK_Players ADD Stamina INT NOT NULL CONSTRAINT DF_HRK_Players_Stamina DEFAULT 200;
IF COL_LENGTH('dbo.HRK_Players', 'MaxStamina') IS NULL ALTER TABLE dbo.HRK_Players ADD MaxStamina INT NOT NULL CONSTRAINT DF_HRK_Players_MaxStamina DEFAULT 200;
IF COL_LENGTH('dbo.HRK_Players', 'LastStaminaRegeneratedOn') IS NULL ALTER TABLE dbo.HRK_Players ADD LastStaminaRegeneratedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_Players_LastStamina DEFAULT SYSUTCDATETIME();
IF COL_LENGTH('dbo.HRK_Players', 'DailyStaminaPurchaseCount') IS NULL ALTER TABLE dbo.HRK_Players ADD DailyStaminaPurchaseCount INT NOT NULL CONSTRAINT DF_HRK_Players_StaminaPurchases DEFAULT 0;
IF COL_LENGTH('dbo.HRK_Players', 'StaminaPurchaseDate') IS NULL ALTER TABLE dbo.HRK_Players ADD StaminaPurchaseDate DATETIME2 NULL;

IF OBJECT_ID('dbo.HRK_DungeonMaps', 'U') IS NULL
CREATE TABLE dbo.HRK_DungeonMaps(
    Id INT IDENTITY PRIMARY KEY, Code NVARCHAR(50) NOT NULL UNIQUE, Name NVARCHAR(150) NOT NULL,
    Description NVARCHAR(500) NULL, ImagePath NVARCHAR(500) NOT NULL, BackgroundPath NVARCHAR(500) NOT NULL,
    DisplayOrder INT NOT NULL UNIQUE, RequiredPlayerLevel INT NOT NULL DEFAULT 1, PreviousMapId INT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_DungeonMaps_Previous FOREIGN KEY(PreviousMapId) REFERENCES dbo.HRK_DungeonMaps(Id)
);

IF OBJECT_ID('dbo.HRK_DungeonStages', 'U') IS NULL
CREATE TABLE dbo.HRK_DungeonStages(
    Id INT IDENTITY PRIMARY KEY, DungeonMapId INT NOT NULL, StageNumber INT NOT NULL, Name NVARCHAR(150) NOT NULL,
    StageType NVARCHAR(20) NOT NULL, StaminaCost INT NOT NULL, RecommendedPower INT NOT NULL,
    GoldReward BIGINT NOT NULL, PlayerExpReward INT NOT NULL, HeroExpReward INT NOT NULL,
    FirstClearGoldReward BIGINT NOT NULL DEFAULT 0, BackgroundPath NVARCHAR(500) NULL, IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT UQ_DungeonStages_MapNumber UNIQUE(DungeonMapId, StageNumber),
    CONSTRAINT FK_DungeonStages_Map FOREIGN KEY(DungeonMapId) REFERENCES dbo.HRK_DungeonMaps(Id)
);

IF OBJECT_ID('dbo.HRK_DungeonStageEnemies', 'U') IS NULL
CREATE TABLE dbo.HRK_DungeonStageEnemies(
    Id INT IDENTITY PRIMARY KEY, StageId INT NOT NULL, Position INT NOT NULL, HeroTemplateId INT NOT NULL,
    DisplayName NVARCHAR(150) NULL, ImagePath NVARCHAR(500) NULL, Level INT NOT NULL, Stars TINYINT NOT NULL,
    StatMultiplier DECIMAL(8,4) NOT NULL, IsBoss BIT NOT NULL DEFAULT 0,
    CONSTRAINT UQ_DungeonStageEnemies_Position UNIQUE(StageId, Position),
    CONSTRAINT FK_DungeonEnemies_Stage FOREIGN KEY(StageId) REFERENCES dbo.HRK_DungeonStages(Id),
    CONSTRAINT FK_DungeonEnemies_Hero FOREIGN KEY(HeroTemplateId) REFERENCES dbo.HRK_HeroTemplates(Id)
);

IF OBJECT_ID('dbo.HRK_PlayerDungeonStageProgress', 'U') IS NULL
CREATE TABLE dbo.HRK_PlayerDungeonStageProgress(
    Id BIGINT IDENTITY PRIMARY KEY, PlayerId BIGINT NOT NULL, StageId INT NOT NULL, ClearCount INT NOT NULL,
    BestTurns INT NOT NULL, FirstClearedOn DATETIME2 NOT NULL, LastClearedOn DATETIME2 NOT NULL,
    CONSTRAINT UQ_PlayerDungeonProgress UNIQUE(PlayerId, StageId),
    CONSTRAINT FK_PlayerDungeonProgress_Player FOREIGN KEY(PlayerId) REFERENCES dbo.HRK_Players(Id),
    CONSTRAINT FK_PlayerDungeonProgress_Stage FOREIGN KEY(StageId) REFERENCES dbo.HRK_DungeonStages(Id)
);

IF OBJECT_ID('dbo.HRK_DungeonRuns', 'U') IS NULL
CREATE TABLE dbo.HRK_DungeonRuns(
    Id BIGINT IDENTITY PRIMARY KEY, BattleId NVARCHAR(64) NOT NULL, ClientRequestId NVARCHAR(64) NOT NULL,
    PlayerId BIGINT NOT NULL, StageId INT NOT NULL, Result NVARCHAR(20) NOT NULL, StaminaSpent INT NOT NULL,
    PlayerPower INT NOT NULL, EnemyPower INT NOT NULL, RandomSeed INT NOT NULL, GoldReward BIGINT NOT NULL,
    PlayerExpReward INT NOT NULL, HeroExpReward INT NOT NULL, StartedOn DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CompletedOn DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_DungeonRuns_Request UNIQUE(PlayerId, ClientRequestId),
    CONSTRAINT FK_DungeonRuns_Player FOREIGN KEY(PlayerId) REFERENCES dbo.HRK_Players(Id),
    CONSTRAINT FK_DungeonRuns_Stage FOREIGN KEY(StageId) REFERENCES dbo.HRK_DungeonStages(Id)
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_DungeonRuns_BattleId') CREATE INDEX IX_DungeonRuns_BattleId ON dbo.HRK_DungeonRuns(BattleId);

IF NOT EXISTS(SELECT 1 FROM dbo.HRK_DungeonMaps WHERE Code='BUG_FOREST')
BEGIN
    INSERT dbo.HRK_DungeonMaps(Code,Name,Description,ImagePath,BackgroundPath,DisplayOrder,RequiredPlayerLevel)
    VALUES('BUG_FOREST',N'Rừng Bug Khởi Nguyên',N'Khu rừng nơi những lỗi đầu tiên thức tỉnh.',
      '/assets/images/dcs-game/dungeon/maps/bug-forest.jpg','/assets/images/dcs-game/dungeon/maps/bug-forest.jpg',1,1);
    DECLARE @Map1 INT=SCOPE_IDENTITY();
    INSERT dbo.HRK_DungeonMaps(Code,Name,Description,ImagePath,BackgroundPath,DisplayOrder,RequiredPlayerLevel,PreviousMapId)
    VALUES('LEGACY_DUNGEON',N'Hầm Ngục Legacy',N'Tàn tích của những hệ thống không ai dám bảo trì.',
      '/assets/images/dcs-game/dungeon/maps/legacy-dungeon.jpg','/assets/images/dcs-game/dungeon/maps/legacy-dungeon.jpg',2,10,@Map1);
    DECLARE @Map2 INT=SCOPE_IDENTITY();
    INSERT dbo.HRK_DungeonMaps(Code,Name,Description,ImagePath,BackgroundPath,DisplayOrder,RequiredPlayerLevel,PreviousMapId)
    VALUES('PRODUCTION_CITADEL',N'Thành Trì Production',N'Pháo đài cuối cùng nơi production không bao giờ ngủ.',
      '/assets/images/dcs-game/dungeon/maps/production-citadel.jpg','/assets/images/dcs-game/dungeon/maps/production-citadel.jpg',3,20,@Map2);

    DECLARE @MapOrder INT=1, @StageNo INT, @MapId INT, @StageId INT, @GlobalStage INT, @Pos INT;
    WHILE @MapOrder<=3
    BEGIN
      SET @MapId=(SELECT Id FROM dbo.HRK_DungeonMaps WHERE DisplayOrder=@MapOrder);
      SET @StageNo=1;
      WHILE @StageNo<=15
      BEGIN
        SET @GlobalStage=(@MapOrder-1)*15+@StageNo;
        INSERT dbo.HRK_DungeonStages(DungeonMapId,StageNumber,Name,StageType,StaminaCost,RecommendedPower,GoldReward,PlayerExpReward,HeroExpReward,FirstClearGoldReward,BackgroundPath)
        VALUES(@MapId,@StageNo,
          CASE WHEN @StageNo IN(5,10,15) THEN N'Thủ Lĩnh '+CAST(@StageNo AS NVARCHAR(2)) ELSE N'Ải '+CAST(@StageNo AS NVARCHAR(2)) END,
          CASE WHEN @StageNo=15 THEN 'BOSS' WHEN @StageNo IN(5,10) THEN 'MINI_BOSS' ELSE 'NORMAL' END,
          CASE WHEN @StageNo IN(5,10,15) THEN 10 ELSE 6 END,
          2500+@GlobalStage*950, 800+@GlobalStage*180, 50+@GlobalStage*12, 35+@GlobalStage*8, 2500+@GlobalStage*400,
          (SELECT BackgroundPath FROM dbo.HRK_DungeonMaps WHERE Id=@MapId));
        SET @StageId=SCOPE_IDENTITY(); SET @Pos=1;
        WHILE @Pos<=5
        BEGIN
          DECLARE @HeroId INT=(SELECT Id FROM (SELECT Id,ROW_NUMBER() OVER(ORDER BY Id) rn FROM dbo.HRK_HeroTemplates) h
            WHERE rn=((@GlobalStage*3+@Pos-1)%(SELECT COUNT(*) FROM dbo.HRK_HeroTemplates))+1);
          DECLARE @Boss BIT=CASE WHEN @StageNo IN(5,10,15) AND @Pos=3 THEN 1 ELSE 0 END;
          DECLARE @MonsterIndex INT=((@MapOrder-1)*3+(@Pos-1)%3)+1;
          INSERT dbo.HRK_DungeonStageEnemies(StageId,Position,HeroTemplateId,DisplayName,ImagePath,Level,Stars,StatMultiplier,IsBoss)
          VALUES(@StageId,@Pos,@HeroId,
            CASE WHEN @Boss=1 THEN NULL ELSE CHOOSE(@MonsterIndex,N'Bug Slime',N'Dơi Null Pointer',N'Nhện Compiler',N'Zombie Legacy',N'Golem Dữ Liệu',N'Ma Rò Rỉ Bộ Nhớ',N'Thú Firewall',N'Robot Deployment',N'Quỷ Production') END,
            CASE WHEN @Boss=1 THEN NULL ELSE '/assets/images/dcs-game/dungeon/monsters/monster-'+CAST(@MonsterIndex AS VARCHAR(2))+'.png' END,
            1+@GlobalStage/2, CASE WHEN @Boss=1 THEN 3+@MapOrder/2 ELSE 1+@MapOrder/2 END,
            CAST((1.0+@GlobalStage*0.055)*CASE WHEN @Boss=1 THEN CASE WHEN @StageNo=5 THEN 1.25 WHEN @StageNo=10 THEN 1.45 ELSE 1.75 END ELSE 1 END AS DECIMAL(8,4)),@Boss);
          SET @Pos+=1;
        END
        SET @StageNo+=1;
      END
      SET @MapOrder+=1;
    END
END

COMMIT TRANSACTION;

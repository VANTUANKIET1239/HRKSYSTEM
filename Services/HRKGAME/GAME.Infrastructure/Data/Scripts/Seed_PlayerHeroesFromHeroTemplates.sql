/*
  Seed 10 hero templates hien co vao tai khoan nguoi choi.
  1) Lay PlayerId: SELECT Id, UserId, PlayerName FROM dbo.HRK_Players;
  2) Gan ID do vao @PlayerId truoc khi chay script.
  Script co the chay lai an toan: khong chen trung HeroTemplateId cho cung PlayerId.
*/

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @PlayerId BIGINT = NULL; -- TODO: thay bang ID trong dbo.HRK_Players, vi du: 1

IF @PlayerId IS NULL
    THROW 50001, 'Hay gan @PlayerId truoc khi chay Seed_PlayerHeroesFromHeroTemplates.sql.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.HRK_Players WHERE Id = @PlayerId AND IsActive = 1)
    THROW 50002, 'PlayerId khong ton tai hoac tai khoan dang khong hoat dong.', 1;

/*
  Mac dinh cap 1. Power khoi tao duoc tinh tu chi so co ban de phan biet tuong,
  CurrentStats de NULL de HeroStatsHelper su dung stats tu HRK_HeroTemplates.
*/
INSERT INTO dbo.HRK_PlayerHeroes
(
    PlayerId, HeroTemplateId, Level, Exp, MaxExp, Stars, Power, AuraTier,
    IsLocked, IsFavorite, IsActive, CurrentStats, CreatedOn, UpdatedOn
)
SELECT
    @PlayerId,
    ht.Id,
    1 AS Level,
    0 AS Exp,
    500 AS MaxExp,
    1 AS Stars,
    (ht.BaseHp / 10) + (ht.BaseAtk * 3) + (ht.BaseDef * 2) + ht.BaseSpd AS Power,
    1 AS AuraTier,
    0 AS IsLocked,
    0 AS IsFavorite,
    1 AS IsActive,
    NULL AS CurrentStats,
    SYSUTCDATETIME(),
    SYSUTCDATETIME()
FROM dbo.HRK_HeroTemplates ht
WHERE ht.Id IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10)
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.HRK_PlayerHeroes ph
      WHERE ph.PlayerId = @PlayerId
        AND ph.HeroTemplateId = ht.Id
        AND ph.IsActive = 1
  );

COMMIT TRANSACTION;

SELECT
    ph.Id,
    ph.PlayerId,
    ph.HeroTemplateId,
    ht.Name AS HeroName,
    ph.Level,
    ph.Stars,
    ph.Power,
    ph.IsLocked,
    ph.IsFavorite
FROM dbo.HRK_PlayerHeroes ph
INNER JOIN dbo.HRK_HeroTemplates ht ON ht.Id = ph.HeroTemplateId
WHERE ph.PlayerId = @PlayerId
ORDER BY ph.HeroTemplateId;

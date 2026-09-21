/*
  Prepares INPUT data for POST /api/battle/start.
  It does not seed battle logs or battle results: StartBattleAsync must generate them.

  Player team : selected formation of PlayerId = 1, hero templates 1..5.
  Enemy team  : five random hero templates selected temporarily by BattleService.

  Prerequisites:
    - full_data_dcs.sql
    - Migration_SkillSystemRefactor.sql
    - Migration_BasicAttackAndHealSkills.sql
    - Seed_HeroSkills.sql
*/
USE [HRK];
GO

SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @PlayerId BIGINT = 1;
    DECLARE @FormationTemplateId INT = 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_Players WHERE Id = @PlayerId AND IsActive = 1)
        THROW 51300, 'Active PlayerId 1 is missing. Load full_data_dcs.sql first.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_FormationTemplates WHERE Id = @FormationTemplateId)
        THROW 51301, 'FormationTemplateId 1 is missing.', 1;

    IF (SELECT COUNT(*) FROM dbo.HRK_HeroTemplates WHERE Id BETWEEN 1 AND 10) <> 10
        THROW 51302, 'Hero templates 1..10 are required for the test battle.', 1;

    /* Give the player the five templates used by the left team if they are missing. */
    INSERT dbo.HRK_PlayerHeroes
        (PlayerId, HeroTemplateId, Level, Exp, MaxExp, Stars, Power, AuraTier,
         IsLocked, IsFavorite, CurrentStats, CreatedOn, UpdatedOn, IsActive)
    SELECT @PlayerId, ht.Id, 10, 0, 1400, 2,
           CAST(ht.BaseHp * 0.25 + ht.BaseAtk * 2 + ht.BaseDef * 1.5 AS INT),
           1, 0, 0, NULL, SYSUTCDATETIME(), SYSUTCDATETIME(), 1
    FROM dbo.HRK_HeroTemplates ht
    WHERE ht.Id BETWEEN 1 AND 5
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.HRK_PlayerHeroes ph
          WHERE ph.PlayerId = @PlayerId AND ph.HeroTemplateId = ht.Id
      );

    UPDATE dbo.HRK_PlayerHeroes
    SET IsActive = 1, UpdatedOn = SYSUTCDATETIME()
    WHERE PlayerId = @PlayerId AND HeroTemplateId BETWEEN 1 AND 5;

    DECLARE @Hero1 BIGINT = (SELECT TOP (1) Id FROM dbo.HRK_PlayerHeroes WHERE PlayerId = @PlayerId AND HeroTemplateId = 1 AND IsActive = 1 ORDER BY Id);
    DECLARE @Hero2 BIGINT = (SELECT TOP (1) Id FROM dbo.HRK_PlayerHeroes WHERE PlayerId = @PlayerId AND HeroTemplateId = 2 AND IsActive = 1 ORDER BY Id);
    DECLARE @Hero3 BIGINT = (SELECT TOP (1) Id FROM dbo.HRK_PlayerHeroes WHERE PlayerId = @PlayerId AND HeroTemplateId = 3 AND IsActive = 1 ORDER BY Id);
    DECLARE @Hero4 BIGINT = (SELECT TOP (1) Id FROM dbo.HRK_PlayerHeroes WHERE PlayerId = @PlayerId AND HeroTemplateId = 4 AND IsActive = 1 ORDER BY Id);
    DECLARE @Hero5 BIGINT = (SELECT TOP (1) Id FROM dbo.HRK_PlayerHeroes WHERE PlayerId = @PlayerId AND HeroTemplateId = 5 AND IsActive = 1 ORDER BY Id);

    UPDATE dbo.HRK_PlayerFormations
    SET IsSelected = 0, UpdatedOn = SYSUTCDATETIME()
    WHERE PlayerId = @PlayerId;

    IF EXISTS
    (
        SELECT 1 FROM dbo.HRK_PlayerFormations
        WHERE PlayerId = @PlayerId AND FormationTemplateId = @FormationTemplateId
    )
    BEGIN
        UPDATE dbo.HRK_PlayerFormations
        SET FormationName = N'StartBattle Test Formation',
            Position1 = @Hero1, Position2 = @Hero2, Position3 = @Hero3,
            Position4 = @Hero4, Position5 = @Hero5,
            Level = 1, TotalPower = 0, IsActive = 1, IsSelected = 1,
            UpdatedOn = SYSUTCDATETIME()
        WHERE PlayerId = @PlayerId AND FormationTemplateId = @FormationTemplateId;
    END
    ELSE
    BEGIN
        INSERT dbo.HRK_PlayerFormations
            (PlayerId, FormationTemplateId, Level, FormationName,
             Position1, Position2, Position3, Position4, Position5,
             TotalPower, IsSelected, IsActive, UpdatedOn)
        VALUES
            (@PlayerId, @FormationTemplateId, 1, N'StartBattle Test Formation',
             @Hero1, @Hero2, @Hero3, @Hero4, @Hero5,
             0, 1, 1, SYSUTCDATETIME());
    END;

    /* StartBattleAsync currently supports exactly one NORMAL and at most one ENERGY skill. */
    IF EXISTS
    (
        SELECT ht.Id
        FROM dbo.HRK_HeroTemplates ht
        LEFT JOIN dbo.HRK_HeroSkills hs ON hs.HeroTemplateId = ht.Id
        LEFT JOIN dbo.HRK_SkillTemplates st ON st.Id = hs.SkillId AND st.IsActive = 1
        WHERE ht.Id BETWEEN 1 AND 10
        GROUP BY ht.Id
        HAVING SUM(CASE WHEN st.SkillTypeCode = 'NORMAL' THEN 1 ELSE 0 END) <> 1
            OR SUM(CASE WHEN st.SkillTypeCode = 'ENERGY' THEN 1 ELSE 0 END) > 1
    )
        THROW 51303, 'Invalid skill mapping. Run Migration_BasicAttackAndHealSkills.sql and Seed_HeroSkills.sql.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

SELECT pf.Id, pf.PlayerId, pf.FormationName,
       pf.Position1, pf.Position2, pf.Position3, pf.Position4, pf.Position5,
       pf.IsSelected
FROM dbo.HRK_PlayerFormations pf
WHERE pf.PlayerId = 1
ORDER BY pf.IsSelected DESC, pf.Id;

SELECT hs.HeroTemplateId, hs.SkillId, st.SkillTypeCode, st.EnergyCost, hs.SkillOrder
FROM dbo.HRK_HeroSkills hs
JOIN dbo.HRK_SkillTemplates st ON st.Id = hs.SkillId
WHERE hs.HeroTemplateId BETWEEN 1 AND 10 AND st.IsActive = 1
ORDER BY hs.HeroTemplateId, hs.SkillOrder;
GO

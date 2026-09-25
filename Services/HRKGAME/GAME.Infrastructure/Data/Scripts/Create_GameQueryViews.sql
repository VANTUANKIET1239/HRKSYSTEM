/*
    Game query views for administration, debugging and reporting.

    SQL Server 2017+ is required because vw_HRK_SkillEffectConfiguration uses STRING_AGG.
    The script is idempotent: every view is defined with CREATE OR ALTER VIEW.

    Notes:
    - These are read-only query projections; application writes must still target base tables.
    - Ordinary views mainly simplify queries. Performance depends on indexes on base tables.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* --------------------------------------------------------------------------
   1. Hero catalog with assigned skills
   One row per hero-skill assignment.
   -------------------------------------------------------------------------- */
CREATE OR ALTER VIEW dbo.vw_HRK_HeroSkillCatalog
AS
SELECT
    ht.Id                                      AS HeroTemplateId,
    ht.Name                                    AS HeroName,
    ht.Avatar,
    hf.Code                                    AS FactionCode,
    hf.Name                                    AS FactionName,
    hc.Code                                    AS ClassCode,
    hc.Name                                    AS ClassName,
    r.Code                                     AS RarityCode,
    r.Name                                     AS RarityName,
    r.DisplayOrder                             AS RarityDisplayOrder,
    r.ColorHex                                 AS RarityColorHex,
    ht.BaseHp,
    ht.BaseAtk,
    ht.BaseDef,
    ht.BaseSpd,
    ht.BaseCrit,
    ht.BaseCritDmg,
    ht.BaseLifesteal,
    ht.BaseAccuracy,
    ht.BaseResistance,
    ht.BaseMagicDamage,
    ht.BaseMagicResistance,
    hs.SkillOrder,
    st.Id                                      AS SkillId,
    st.Name                                    AS SkillName,
    st.SkillTypeCode,
    st.TriggerCode,
    st.EnergyCost,
    st.Icon                                    AS SkillIcon,
    st.ImagePath                               AS SkillImagePath,
    st.Description                             AS SkillDescription,
    st.IsActive                                AS IsSkillActive,
    animation.AnimationKey,
    animation.TotalDurationMs,
    animation.DefaultPlaybackSpeed
FROM dbo.HRK_HeroTemplates AS ht
INNER JOIN dbo.HRK_HeroFactions AS hf ON hf.Id = ht.FactionId
INNER JOIN dbo.HRK_HeroClasses AS hc ON hc.Id = ht.ClassId
INNER JOIN dbo.HRK_Rarities AS r ON r.Id = ht.RarityId
LEFT JOIN dbo.HRK_HeroSkills AS hs ON hs.HeroTemplateId = ht.Id
LEFT JOIN dbo.HRK_SkillTemplates AS st ON st.Id = hs.SkillId
LEFT JOIN dbo.HRK_SkillAnimationConfigs AS animation ON animation.SkillId = st.Id;
GO

/* --------------------------------------------------------------------------
   2. Full skill-effect configuration
   One row per effect, with readable scaling/modifier/parameter summaries.
   -------------------------------------------------------------------------- */
CREATE OR ALTER VIEW dbo.vw_HRK_SkillEffectConfiguration
AS
SELECT
    st.Id                                      AS SkillId,
    st.Name                                    AS SkillName,
    st.SkillTypeCode,
    st.EnergyCost,
    st.IsActive                                AS IsSkillActive,
    se.Id                                      AS SkillEffectId,
    se.DisplayOrder,
    se.ExecutionGroup,
    se.ConditionCode,
    effectType.Code                            AS EffectTypeCode,
    effectType.Name                            AS EffectTypeName,
    effectType.EffectGroup,
    effectType.IsBeneficial,
    effectType.IsStackable,
    targetType.Code                            AS TargetTypeCode,
    targetType.Name                            AS TargetTypeName,
    targetType.TargetSide,
    targetType.SelectionRule,
    se.DamageSchoolCode,
    se.BaseValue,
    se.DurationTurns,
    se.ChancePercent,
    se.MaxStacks,
    se.IsActive                                AS IsEffectActive,
    scalingSummary.Scalings,
    modifierSummary.StatModifiers,
    parameterSummary.Parameters
FROM dbo.HRK_SkillEffects AS se
INNER JOIN dbo.HRK_SkillTemplates AS st ON st.Id = se.SkillId
INNER JOIN dbo.HRK_SkillEffectTypes AS effectType ON effectType.Id = se.EffectTypeId
INNER JOIN dbo.HRK_SkillTargetTypes AS targetType ON targetType.Id = se.TargetTypeId
OUTER APPLY
(
    SELECT STRING_AGG(
        CONVERT(nvarchar(max),
            CONCAT(attributeType.Code, N' × ', scaling.Coefficient,
                   CASE WHEN scaling.FlatValue = 0 THEN N'' ELSE CONCAT(N' + ', scaling.FlatValue) END)),
        N'; ') AS Scalings
    FROM dbo.HRK_SkillEffectScalings AS scaling
    INNER JOIN dbo.HRK_AttributeTypes AS attributeType ON attributeType.Id = scaling.AttributeTypeId
    WHERE scaling.SkillEffectId = se.Id
) AS scalingSummary
OUTER APPLY
(
    SELECT STRING_AGG(
        CONVERT(nvarchar(max),
            CONCAT(attributeType.Code, N' ', modifier.ValueType, N' ', modifier.Value)),
        N'; ') AS StatModifiers
    FROM dbo.HRK_SkillEffectStatModifiers AS modifier
    INNER JOIN dbo.HRK_AttributeTypes AS attributeType ON attributeType.Id = modifier.AttributeTypeId
    WHERE modifier.SkillEffectId = se.Id
) AS modifierSummary
OUTER APPLY
(
    SELECT STRING_AGG(
        CONVERT(nvarchar(max),
            CONCAT(parameter.ParameterCode, N'=',
                COALESCE(
                    parameter.StringValue,
                    CONVERT(nvarchar(100), parameter.DecimalValue),
                    CONVERT(nvarchar(100), parameter.IntValue),
                    CASE WHEN parameter.BoolValue = 1 THEN N'true'
                         WHEN parameter.BoolValue = 0 THEN N'false' END,
                    N'NULL'))),
        N'; ') AS Parameters
    FROM dbo.HRK_SkillEffectParameters AS parameter
    WHERE parameter.SkillEffectId = se.Id
) AS parameterSummary;
GO

/* --------------------------------------------------------------------------
   3. Player account overview
   One row per player with wallet and resource counts.
   -------------------------------------------------------------------------- */
CREATE OR ALTER VIEW dbo.vw_HRK_PlayerOverview
AS
SELECT
    p.Id                                       AS PlayerId,
    p.UserId,
    p.PlayerName,
    p.Level,
    p.Exp,
    p.MaxExp,
    CAST(CASE WHEN p.MaxExp > 0
              THEN p.Exp * 100.0 / p.MaxExp ELSE 0 END AS decimal(6, 2)) AS ExpPercent,
    p.Stamina,
    p.MaxStamina,
    p.AvatarType,
    p.AvatarTemplateId,
    p.IsActive,
    wallet.Gold,
    wallet.Diamonds,
    wallet.UpgradeMaterials,
    wallet.MaxCapacity                         AS InventoryCapacity,
    ISNULL(heroCount.OwnedHeroes, 0)           AS OwnedHeroes,
    ISNULL(inventoryCount.ActiveInventoryRows, 0) AS ActiveInventoryRows,
    ISNULL(inventoryCount.UnequippedRows, 0)   AS UnequippedInventoryRows,
    ISNULL(formationCount.ActiveFormations, 0) AS ActiveFormations,
    p.CreatedOn,
    p.UpdatedOn
FROM dbo.HRK_Players AS p
LEFT JOIN dbo.HRK_PlayerWallets AS wallet ON wallet.PlayerId = p.Id
OUTER APPLY
(
    SELECT COUNT_BIG(*) AS OwnedHeroes
    FROM dbo.HRK_PlayerHeroes AS ph
    WHERE ph.PlayerId = p.Id AND ph.IsActive = 1
) AS heroCount
OUTER APPLY
(
    SELECT
        COUNT_BIG(*) AS ActiveInventoryRows,
        SUM(CASE WHEN inventory.IsEquipped = 0 THEN CONVERT(bigint, 1) ELSE CONVERT(bigint, 0) END) AS UnequippedRows
    FROM dbo.HRK_PlayerInventory AS inventory
    WHERE inventory.PlayerId = p.Id AND inventory.IsActive = 1
) AS inventoryCount
OUTER APPLY
(
    SELECT COUNT_BIG(*) AS ActiveFormations
    FROM dbo.HRK_PlayerFormations AS formation
    WHERE formation.PlayerId = p.Id AND formation.IsActive = 1
) AS formationCount;
GO

/* --------------------------------------------------------------------------
   4. Player hero roster
   One row per owned hero, including catalog information and equipment count.
   -------------------------------------------------------------------------- */
CREATE OR ALTER VIEW dbo.vw_HRK_PlayerHeroRoster
AS
SELECT
    ph.Id                                      AS PlayerHeroId,
    ph.PlayerId,
    p.PlayerName,
    ph.HeroTemplateId,
    ht.Name                                    AS HeroName,
    ht.Avatar,
    rarity.Code                                AS RarityCode,
    rarity.Name                                AS RarityName,
    rarity.DisplayOrder                        AS RarityDisplayOrder,
    class.Code                                 AS ClassCode,
    class.Name                                 AS ClassName,
    faction.Code                               AS FactionCode,
    faction.Name                               AS FactionName,
    ph.Level,
    ph.Exp,
    ph.MaxExp,
    ph.Stars,
    ph.Power,
    ph.AuraTier,
    ph.IsLocked,
    ph.IsFavorite,
    ph.IsActive,
    ph.CurrentStats,
    ISNULL(equipmentCount.EquippedItemCount, 0) AS EquippedItemCount,
    ph.CreatedOn,
    ph.UpdatedOn
FROM dbo.HRK_PlayerHeroes AS ph
INNER JOIN dbo.HRK_Players AS p ON p.Id = ph.PlayerId
INNER JOIN dbo.HRK_HeroTemplates AS ht ON ht.Id = ph.HeroTemplateId
INNER JOIN dbo.HRK_Rarities AS rarity ON rarity.Id = ht.RarityId
INNER JOIN dbo.HRK_HeroClasses AS class ON class.Id = ht.ClassId
INNER JOIN dbo.HRK_HeroFactions AS faction ON faction.Id = ht.FactionId
OUTER APPLY
(
    SELECT COUNT_BIG(*) AS EquippedItemCount
    FROM dbo.HRK_PlayerInventory AS inventory
    WHERE inventory.EquippedHeroId = ph.Id
      AND inventory.PlayerId = ph.PlayerId
      AND inventory.IsActive = 1
      AND inventory.IsEquipped = 1
) AS equipmentCount;
GO

/* --------------------------------------------------------------------------
   5. Inventory details, including the hero currently wearing each item
   One row per inventory record.
   -------------------------------------------------------------------------- */
CREATE OR ALTER VIEW dbo.vw_HRK_PlayerInventoryDetail
AS
SELECT
    inventory.Id                               AS InventoryItemId,
    inventory.PlayerId,
    player.PlayerName,
    inventory.ItemTemplateId,
    item.Code                                  AS ItemCode,
    item.Name                                  AS ItemName,
    item.ImagePath,
    category.Code                              AS CategoryCode,
    category.Name                              AS CategoryName,
    category.IsEquipment,
    rarity.Code                                AS RarityCode,
    rarity.Name                                AS RarityName,
    rarity.DisplayOrder                        AS RarityDisplayOrder,
    rarity.ColorHex                            AS RarityColorHex,
    inventory.Count,
    inventory.Enhancement,
    inventory.Stars,
    inventory.IsEquipped,
    inventory.EquippedHeroId,
    heroTemplate.Name                          AS EquippedHeroName,
    heroTemplate.Avatar                        AS EquippedHeroAvatar,
    inventory.IsLocked,
    inventory.IsActive,
    inventory.SlotIndex,
    inventory.CurrentStats,
    item.IsStackable,
    item.MaxStackSize,
    item.LevelReq,
    item.SellPrice,
    inventory.AcquiredOn,
    inventory.UpdatedOn
FROM dbo.HRK_PlayerInventory AS inventory
INNER JOIN dbo.HRK_Players AS player ON player.Id = inventory.PlayerId
INNER JOIN dbo.HRK_ItemTemplates AS item ON item.Id = inventory.ItemTemplateId
INNER JOIN dbo.HRK_ItemCategories AS category ON category.Id = item.CategoryId
INNER JOIN dbo.HRK_Rarities AS rarity ON rarity.Id = item.RarityId
LEFT JOIN dbo.HRK_PlayerHeroes AS playerHero ON playerHero.Id = inventory.EquippedHeroId
LEFT JOIN dbo.HRK_HeroTemplates AS heroTemplate ON heroTemplate.Id = playerHero.HeroTemplateId;
GO

/* --------------------------------------------------------------------------
   6. Formation slots in query-friendly row form
   One row per formation slot instead of Position1..Position5 columns.
   -------------------------------------------------------------------------- */
CREATE OR ALTER VIEW dbo.vw_HRK_PlayerFormationSlots
AS
SELECT
    formation.Id                               AS FormationId,
    formation.PlayerId,
    player.PlayerName,
    formation.FormationTemplateId,
    template.Code                              AS FormationCode,
    template.Name                              AS FormationTemplateName,
    formation.FormationName,
    formation.Level                            AS FormationLevel,
    formation.TotalPower,
    formation.IsSelected,
    formation.IsActive                         AS IsFormationActive,
    slot.SlotNumber,
    slot.PlayerHeroId,
    playerHero.HeroTemplateId,
    heroTemplate.Name                          AS HeroName,
    heroTemplate.Avatar,
    playerHero.Level                           AS HeroLevel,
    playerHero.Stars                           AS HeroStars,
    playerHero.Power                           AS HeroPower,
    playerHero.IsActive                        AS IsHeroActive,
    formation.UpdatedOn
FROM dbo.HRK_PlayerFormations AS formation
INNER JOIN dbo.HRK_Players AS player ON player.Id = formation.PlayerId
INNER JOIN dbo.HRK_FormationTemplates AS template ON template.Id = formation.FormationTemplateId
CROSS APPLY
(
    VALUES
        (1, formation.Position1),
        (2, formation.Position2),
        (3, formation.Position3),
        (4, formation.Position4),
        (5, formation.Position5)
) AS slot(SlotNumber, PlayerHeroId)
LEFT JOIN dbo.HRK_PlayerHeroes AS playerHero ON playerHero.Id = slot.PlayerHeroId
LEFT JOIN dbo.HRK_HeroTemplates AS heroTemplate ON heroTemplate.Id = playerHero.HeroTemplateId;
GO

/* --------------------------------------------------------------------------
   7. Dungeon stage overview
   One row per stage, with enemy and drop-pool summaries.
   -------------------------------------------------------------------------- */
CREATE OR ALTER VIEW dbo.vw_HRK_DungeonStageOverview
AS
SELECT
    map.Id                                     AS DungeonMapId,
    map.Code                                   AS MapCode,
    map.Name                                   AS MapName,
    map.DisplayOrder                           AS MapDisplayOrder,
    map.RequiredPlayerLevel,
    map.IsActive                               AS IsMapActive,
    maxRarity.Code                             AS MaxEquipmentRarityCode,
    stage.Id                                   AS StageId,
    stage.StageNumber,
    stage.Name                                 AS StageName,
    stage.StageType,
    stage.StaminaCost,
    stage.RecommendedPower,
    stage.GoldReward,
    stage.PlayerExpReward,
    stage.HeroExpReward,
    stage.FirstClearGoldReward,
    stage.BackgroundPath,
    stage.IsActive                             AS IsStageActive,
    ISNULL(enemySummary.EnemyCount, 0)         AS EnemyCount,
    ISNULL(enemySummary.BossCount, 0)          AS BossCount,
    ISNULL(dropSummary.DropItemCount, 0)       AS DropItemCount,
    dropSummary.HighestDropRate,
    CAST(ISNULL(dropSummary.HighestDropRate, 0) * 100.0 AS decimal(7, 2)) AS HighestDropRatePercent
FROM dbo.HRK_DungeonStages AS stage
INNER JOIN dbo.HRK_DungeonMaps AS map ON map.Id = stage.DungeonMapId
LEFT JOIN dbo.HRK_Rarities AS maxRarity ON maxRarity.Id = map.MaxEquipmentRarityId
OUTER APPLY
(
    SELECT
        COUNT_BIG(*) AS EnemyCount,
        SUM(CASE WHEN enemy.IsBoss = 1 THEN CONVERT(bigint, 1) ELSE CONVERT(bigint, 0) END) AS BossCount
    FROM dbo.HRK_DungeonStageEnemies AS enemy
    WHERE enemy.StageId = stage.Id
) AS enemySummary
OUTER APPLY
(
    SELECT
        COUNT_BIG(*) AS DropItemCount,
        MAX(pool.DropRate) AS HighestDropRate
    FROM dbo.HRK_DungeonStageDropPools AS pool
    WHERE pool.StageId = stage.Id AND pool.IsActive = 1
) AS dropSummary;
GO

/* --------------------------------------------------------------------------
   8. Dungeon equipment drop pool
   One row per possible item drop.
   -------------------------------------------------------------------------- */
CREATE OR ALTER VIEW dbo.vw_HRK_DungeonDropPoolDetail
AS
SELECT
    map.Id                                     AS DungeonMapId,
    map.Code                                   AS MapCode,
    map.Name                                   AS MapName,
    stage.Id                                   AS StageId,
    stage.StageNumber,
    stage.Name                                 AS StageName,
    stage.StageType,
    CASE WHEN EXISTS
    (
        SELECT 1
        FROM dbo.HRK_DungeonStageEnemies AS enemy
        WHERE enemy.StageId = stage.Id AND enemy.IsBoss = 1
    ) THEN CONVERT(bit, 1) ELSE CONVERT(bit, 0) END AS IsBossStage,
    pool.Id                                    AS DropPoolId,
    pool.ItemTemplateId,
    item.Code                                  AS ItemCode,
    item.Name                                  AS ItemName,
    item.ImagePath,
    category.Code                              AS CategoryCode,
    category.Name                              AS CategoryName,
    rarity.Code                                AS RarityCode,
    rarity.Name                                AS RarityName,
    rarity.DisplayOrder                        AS RarityDisplayOrder,
    pool.DropRate,
    CAST(pool.DropRate * 100.0 AS decimal(7, 2)) AS DropRatePercent,
    pool.Weight,
    pool.MinQuantity,
    pool.MaxQuantity,
    pool.IsFirstClearOnly,
    pool.IsActive,
    pool.CreatedOn
FROM dbo.HRK_DungeonStageDropPools AS pool
INNER JOIN dbo.HRK_DungeonStages AS stage ON stage.Id = pool.StageId
INNER JOIN dbo.HRK_DungeonMaps AS map ON map.Id = stage.DungeonMapId
INNER JOIN dbo.HRK_ItemTemplates AS item ON item.Id = pool.ItemTemplateId
INNER JOIN dbo.HRK_ItemCategories AS category ON category.Id = item.CategoryId
INNER JOIN dbo.HRK_Rarities AS rarity ON rarity.Id = item.RarityId;
GO

/* --------------------------------------------------------------------------
   9. Player dungeon progress
   One row per cleared stage, including best result and replay count.
   -------------------------------------------------------------------------- */
CREATE OR ALTER VIEW dbo.vw_HRK_PlayerDungeonProgress
AS
SELECT
    progress.PlayerId,
    player.PlayerName,
    map.Id                                     AS DungeonMapId,
    map.Code                                   AS MapCode,
    map.Name                                   AS MapName,
    map.DisplayOrder                           AS MapDisplayOrder,
    stage.Id                                   AS StageId,
    stage.StageNumber,
    stage.Name                                 AS StageName,
    stage.StageType,
    progress.ClearCount,
    progress.BestTurns,
    progress.BestStars,
    progress.BestRemainingHpRate,
    CAST(progress.BestRemainingHpRate * 100.0 AS decimal(7, 2)) AS BestRemainingHpPercent,
    progress.FirstClearedOn,
    progress.LastClearedOn,
    ISNULL(runSummary.TotalRuns, 0)             AS TotalRuns,
    ISNULL(runSummary.VictoryRuns, 0)           AS VictoryRuns,
    ISNULL(runSummary.DefeatRuns, 0)            AS DefeatRuns,
    runSummary.LastRunOn
FROM dbo.HRK_PlayerDungeonStageProgress AS progress
INNER JOIN dbo.HRK_Players AS player ON player.Id = progress.PlayerId
INNER JOIN dbo.HRK_DungeonStages AS stage ON stage.Id = progress.StageId
INNER JOIN dbo.HRK_DungeonMaps AS map ON map.Id = stage.DungeonMapId
OUTER APPLY
(
    SELECT
        COUNT_BIG(*) AS TotalRuns,
        SUM(CASE WHEN run.Result = 'VICTORY' THEN CONVERT(bigint, 1) ELSE CONVERT(bigint, 0) END) AS VictoryRuns,
        SUM(CASE WHEN run.Result = 'DEFEAT' THEN CONVERT(bigint, 1) ELSE CONVERT(bigint, 0) END) AS DefeatRuns,
        MAX(run.CompletedOn) AS LastRunOn
    FROM dbo.HRK_DungeonRuns AS run
    WHERE run.PlayerId = progress.PlayerId AND run.StageId = progress.StageId
) AS runSummary;
GO

/* Example queries

SELECT *
FROM dbo.vw_HRK_SkillEffectConfiguration
WHERE SkillId = 'HAI_LAST_LAUGH'
ORDER BY DisplayOrder;

SELECT *
FROM dbo.vw_HRK_PlayerInventoryDetail
WHERE PlayerId = 1
ORDER BY IsEquipped DESC, RarityDisplayOrder DESC, InventoryItemId;

SELECT *
FROM dbo.vw_HRK_PlayerFormationSlots
WHERE PlayerId = 1 AND IsSelected = 1
ORDER BY SlotNumber;

SELECT *
FROM dbo.vw_HRK_DungeonDropPoolDetail
WHERE MapCode = 'BUG_FOREST'
ORDER BY StageNumber, DropPoolId;

SELECT *
FROM dbo.vw_HRK_PlayerDungeonProgress
WHERE PlayerId = 1
ORDER BY MapDisplayOrder, StageNumber;
*/


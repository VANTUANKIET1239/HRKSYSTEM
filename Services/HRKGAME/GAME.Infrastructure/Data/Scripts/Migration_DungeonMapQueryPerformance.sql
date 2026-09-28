/*
    Idempotent indexes for the campaign map and player-summary read paths.
    These indexes complement the query-shape fixes in DungeonService and
    GamePlayerService; increasing CommandTimeout is intentionally avoided.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.HRK_PlayerHeroes')
          AND name = N'IX_HRK_PlayerHeroes_PlayerId_IsActive')
    BEGIN
        CREATE NONCLUSTERED INDEX IX_HRK_PlayerHeroes_PlayerId_IsActive
            ON dbo.HRK_PlayerHeroes(PlayerId, IsActive)
            INCLUDE (HeroTemplateId, Level, Exp, MaxExp, Stars, Power, AuraTier, IsLocked, IsFavorite);
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.HRK_PlayerHeroes')
          AND name = N'IX_HRK_PlayerHeroes_PlayerId_HeroTemplateId')
    BEGIN
        CREATE NONCLUSTERED INDEX IX_HRK_PlayerHeroes_PlayerId_HeroTemplateId
            ON dbo.HRK_PlayerHeroes(PlayerId, HeroTemplateId)
            INCLUDE (IsActive, Level, Stars);
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.HRK_PlayerInventory')
          AND name = N'IX_HRK_PlayerInventory_PlayerId_IsActive_IsEquipped')
    BEGIN
        CREATE NONCLUSTERED INDEX IX_HRK_PlayerInventory_PlayerId_IsActive_IsEquipped
            ON dbo.HRK_PlayerInventory(PlayerId, IsActive, IsEquipped)
            INCLUDE (ItemTemplateId, EquippedHeroId, Enhancement, Stars, SlotIndex);
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.HRK_PlayerInventory')
          AND name = N'IX_HRK_PlayerInventory_EquippedHeroId')
    BEGIN
        CREATE NONCLUSTERED INDEX IX_HRK_PlayerInventory_EquippedHeroId
            ON dbo.HRK_PlayerInventory(EquippedHeroId)
            INCLUDE (PlayerId, ItemTemplateId, IsActive, IsEquipped);
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.HRK_PlayerFormations')
          AND name = N'IX_HRK_PlayerFormations_PlayerId_IsActive_IsSelected')
    BEGIN
        CREATE NONCLUSTERED INDEX IX_HRK_PlayerFormations_PlayerId_IsActive_IsSelected
            ON dbo.HRK_PlayerFormations(PlayerId, IsActive, IsSelected)
            INCLUDE (FormationTemplateId, Level, Position1, Position2, Position3, Position4, Position5, TotalPower);
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.HRK_DungeonStages')
          AND name = N'IX_HRK_DungeonStages_Map_Active_StageNumber')
    BEGIN
        CREATE NONCLUSTERED INDEX IX_HRK_DungeonStages_Map_Active_StageNumber
            ON dbo.HRK_DungeonStages(DungeonMapId, IsActive, StageNumber)
            INCLUDE (StageType, RecommendedPower, StaminaCost, GoldReward, PlayerExpReward, HeroExpReward, FirstClearGoldReward);
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.HRK_DungeonMapStarChests')
          AND name = N'IX_HRK_DungeonMapStarChests_Map_Active_DisplayOrder')
    BEGIN
        CREATE NONCLUSTERED INDEX IX_HRK_DungeonMapStarChests_Map_Active_DisplayOrder
            ON dbo.HRK_DungeonMapStarChests(DungeonMapId, IsActive, DisplayOrder)
            INCLUDE (RequiredStars, GuaranteedItemTemplateId, GoldReward, DiamondReward, UpgradeMaterialsReward);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

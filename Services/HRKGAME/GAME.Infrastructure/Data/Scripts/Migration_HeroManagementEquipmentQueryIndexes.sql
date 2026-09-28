/* Indexes used by hero-management equipment batch queries. */
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- HRK_PlayerEquipment already has (PlayerId, HeroId) as its primary key.
    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.HRK_PlayerInventory')
          AND name = N'IX_HRK_PlayerInventory_PlayerId_IsActive_Id'
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_HRK_PlayerInventory_PlayerId_IsActive_Id
            ON dbo.HRK_PlayerInventory(PlayerId, IsActive, Id)
            INCLUDE (ItemTemplateId, Enhancement, Stars, IsEquipped, EquippedHeroId,
                     SlotIndex, EnhancementGrowthPercent,
                     EnhancementGrowthMinPercent, EnhancementGrowthMaxPercent);
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.HRK_PlayerInventoryAttributes')
          AND name = N'IX_HRK_PlayerInventoryAttributes_PlayerInventoryId'
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_HRK_PlayerInventoryAttributes_PlayerInventoryId
            ON dbo.HRK_PlayerInventoryAttributes(PlayerInventoryId)
            INCLUDE (AttributeTypeId, BaseRolledValue, CurrentValue,
                     RollMinValue, RollMaxValue, RollQualityPercent);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes indexInfo
        INNER JOIN sys.index_columns indexColumn
            ON indexColumn.object_id = indexInfo.object_id
           AND indexColumn.index_id = indexInfo.index_id
        INNER JOIN sys.columns columnInfo
            ON columnInfo.object_id = indexColumn.object_id
           AND columnInfo.column_id = indexColumn.column_id
        WHERE indexInfo.object_id = OBJECT_ID(N'dbo.HRK_ItemTemplateAttributes')
          AND columnInfo.name = N'ItemTemplateId'
          AND indexColumn.key_ordinal = 1
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_HRK_ItemTemplateAttributes_ItemTemplateId
            ON dbo.HRK_ItemTemplateAttributes(ItemTemplateId)
            INCLUDE (AttributeTypeId, Value, MinValue, MaxValue);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

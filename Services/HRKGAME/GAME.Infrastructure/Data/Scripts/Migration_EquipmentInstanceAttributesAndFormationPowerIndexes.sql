-- ==============================================================================
-- Script: Migration_EquipmentInstanceAttributesAndFormationPowerIndexes.sql
-- Description:
--   1. Đảm bảo Index hiệu năng cao cho việc tính lực chiến và load thuộc tính instance:
--      - IX_HRK_PlayerInventoryAttributes_PlayerInventoryId
--      - IX_HRK_PlayerInventory_PlayerId_IsActive
--      - IX_HRK_PlayerInventory_ItemTemplateId
--      - IX_HRK_PlayerFormations_PlayerId_IsSelected
--   2. Backfill idempotent cho các trang bị cũ chưa có bản ghi trong HRK_PlayerInventoryAttributes:
--      - BaseRolledValue lấy từ ItemTemplateAttributes
--      - RollMinValue, RollMaxValue lấy từ ItemTemplateAttributes (hoặc tính theo 85%-115%)
--      - RollQualityPercent tính theo công thức chuẩn: (BaseRolledValue - Min) / (Max - Min) * 100 (Min = Max -> 100%)
--   3. Cập nhật lại RollQualityPercent cho các bản ghi cũ nếu bị gán cứng 50%
-- ==============================================================================

SET NOCOUNT ON;
BEGIN TRANSACTION;

BEGIN TRY
    PRINT N'==> 1. Đảm bảo các Indexes hiệu năng cao cho Combat Power và Inventory Attributes...';

    -- 1.1 Index trên HRK_PlayerInventoryAttributes (PlayerInventoryId)
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes 
        WHERE name = 'IX_HRK_PlayerInventoryAttributes_PlayerInventoryId' 
        AND object_id = OBJECT_ID('dbo.HRK_PlayerInventoryAttributes')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_HRK_PlayerInventoryAttributes_PlayerInventoryId
        ON dbo.HRK_PlayerInventoryAttributes (PlayerInventoryId ASC)
        INCLUDE (AttributeTypeId, BaseRolledValue, CurrentValue, RollMinValue, RollMaxValue, RollQualityPercent);
        PRINT N'   + Đã tạo Index IX_HRK_PlayerInventoryAttributes_PlayerInventoryId.';
    END

    -- 1.2 Index trên HRK_PlayerInventory (PlayerId, IsActive)
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes 
        WHERE name = 'IX_HRK_PlayerInventory_PlayerId_IsActive' 
        AND object_id = OBJECT_ID('dbo.HRK_PlayerInventory')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_HRK_PlayerInventory_PlayerId_IsActive
        ON dbo.HRK_PlayerInventory (PlayerId ASC, IsActive ASC)
        INCLUDE (ItemTemplateId, Enhancement, Stars, IsEquipped, EquippedHeroId, SlotIndex);
        PRINT N'   + Đã tạo Index IX_HRK_PlayerInventory_PlayerId_IsActive.';
    END

    -- 1.3 Index trên HRK_PlayerInventory (ItemTemplateId)
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes 
        WHERE name = 'IX_HRK_PlayerInventory_ItemTemplateId' 
        AND object_id = OBJECT_ID('dbo.HRK_PlayerInventory')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_HRK_PlayerInventory_ItemTemplateId
        ON dbo.HRK_PlayerInventory (ItemTemplateId ASC)
        INCLUDE (PlayerId, IsEquipped);
        PRINT N'   + Đã tạo Index IX_HRK_PlayerInventory_ItemTemplateId.';
    END

    -- 1.4 Index trên HRK_PlayerFormations (PlayerId, IsSelected)
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes 
        WHERE name = 'IX_HRK_PlayerFormations_PlayerId_IsSelected' 
        AND object_id = OBJECT_ID('dbo.HRK_PlayerFormations')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_HRK_PlayerFormations_PlayerId_IsSelected
        ON dbo.HRK_PlayerFormations (PlayerId ASC, IsSelected ASC)
        INCLUDE (FormationTemplateId, Level, FormationName, Position1, Position2, Position3, Position4, Position5, TotalPower, IsActive);
        PRINT N'   + Đã tạo Index IX_HRK_PlayerFormations_PlayerId_IsSelected.';
    END

    -- ==============================================================================
    PRINT N'==> 2. Backfill thuộc tính instance cho các trang bị cũ chưa có HRK_PlayerInventoryAttributes...';

    EXEC sp_executesql N'
        INSERT INTO dbo.HRK_PlayerInventoryAttributes (
            PlayerInventoryId,
            AttributeTypeId,
            BaseRolledValue,
            CurrentValue,
            RollMinValue,
            RollMaxValue,
            RollQualityPercent,
            CreatedOn,
            UpdatedOn
        )
        SELECT 
            pi.Id AS PlayerInventoryId,
            ita.AttributeTypeId,
            ita.Value AS BaseRolledValue,
            CASE 
                WHEN at.IsPercentage = 1 THEN 
                    ROUND(ita.Value * (1.0 + (pi.Enhancement * ISNULL(pi.EnhancementGrowthPercent, 10.0) / 100.0)), 4)
                ELSE 
                    ROUND(ita.Value * (1.0 + (pi.Enhancement * ISNULL(pi.EnhancementGrowthPercent, 10.0) / 100.0)) * (1.0 + (pi.Stars * 0.10)), 2)
            END AS CurrentValue,
            ISNULL(ita.MinValue, ita.Value) AS RollMinValue,
            ISNULL(ita.MaxValue, ita.Value) AS RollMaxValue,
            CASE 
                WHEN ISNULL(ita.MaxValue, ita.Value) <= ISNULL(ita.MinValue, ita.Value) THEN 100.00
                ELSE ROUND(
                    CAST(
                        (ita.Value - ISNULL(ita.MinValue, ita.Value)) * 100.0 / 
                        NULLIF(ISNULL(ita.MaxValue, ita.Value) - ISNULL(ita.MinValue, ita.Value), 0)
                    AS DECIMAL(8, 4)), 
                    2
                )
            END AS RollQualityPercent,
            GETDATE(),
            GETDATE()
        FROM dbo.HRK_PlayerInventory pi
        JOIN dbo.HRK_ItemTemplates it ON pi.ItemTemplateId = it.Id
        JOIN dbo.HRK_ItemCategories ic ON it.CategoryId = ic.Id
        JOIN dbo.HRK_ItemTemplateAttributes ita ON it.Id = ita.ItemTemplateId
        JOIN dbo.HRK_AttributeTypes at ON ita.AttributeTypeId = at.Id
        LEFT JOIN dbo.HRK_PlayerInventoryAttributes existing 
            ON existing.PlayerInventoryId = pi.Id AND existing.AttributeTypeId = ita.AttributeTypeId
        WHERE ic.IsEquipment = 1 AND existing.Id IS NULL;
    ';
    PRINT N'   + Đã hoàn tất backfill HRK_PlayerInventoryAttributes còn thiếu.';

    -- ==============================================================================
    PRINT N'==> 3. Hiệu chỉnh lại RollQualityPercent theo công thức chuẩn cho bản ghi hiện có...';

    EXEC sp_executesql N'
        UPDATE pia
        SET 
            pia.RollQualityPercent = CASE 
                WHEN pia.RollMaxValue <= pia.RollMinValue THEN 100.00
                WHEN pia.BaseRolledValue >= pia.RollMaxValue THEN 100.00
                WHEN pia.BaseRolledValue <= pia.RollMinValue THEN 0.00
                ELSE ROUND(
                    CAST((pia.BaseRolledValue - pia.RollMinValue) * 100.0 / (pia.RollMaxValue - pia.RollMinValue) AS DECIMAL(8, 4)), 
                    2
                )
            END,
            pia.UpdatedOn = GETDATE()
        FROM dbo.HRK_PlayerInventoryAttributes pia
        WHERE pia.RollMaxValue IS NOT NULL 
          AND pia.RollMinValue IS NOT NULL
          AND (pia.RollQualityPercent = 50.00 OR pia.RollQualityPercent IS NULL);
    ';
    PRINT N'   + Đã chuẩn hóa RollQualityPercent cho tất cả thuộc tính instance.';

    COMMIT TRANSACTION;
    PRINT N'==> Migration hoàn tất thành công!';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT N'LỖI MIGRATION: ' + ERROR_MESSAGE();
    THROW;
END CATCH;

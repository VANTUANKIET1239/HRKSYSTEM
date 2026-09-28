/*
=========================================================================================
MIGRATION SCRIPT: CANONICAL MAGIC_DAMAGE FOR ARTIFACTS (THẦN BINH)
=========================================================================================
Project: HRK Game Service
Mục đích:
1. Đảm bảo thuộc tính canonical MAGIC_DAMAGE tồn tại trong HRK_AttributeTypes.
2. Cho phép thuộc tính MAGIC_DAMAGE trên danh mục ARTIFACT (Thần Binh) trong HRK_CategoryAllowedAttributes.
3. Bổ sung MAGIC_DAMAGE vào tất cả các template Thần Binh (HRK_ItemTemplates) chưa có,
   gán khoảng roll [MinValue, MaxValue] theo phẩm chất:
   - COMMON:    20 - 40   (Base Value: 30)
   - RARE:      45 - 80   (Base Value: 60)
   - EPIC:      90 - 150  (Base Value: 120)
   - LEGENDARY: 170 - 260 (Base Value: 215)
   - MYTHIC:    280 - 420 (Base Value: 350)
4. Backfill idempotent cho các instance Thần Binh trong HRK_PlayerInventory:
   - Nếu instance đã có MAGIC_DAMAGE: giữ nguyên giá trị đã roll.
   - Nếu instance có MAGIC_ATK: migrate an toàn sang MAGIC_DAMAGE.
   - Nếu chưa có: roll đúng 1 lần trong khoảng [MinValue, MaxValue] của template,
     tính RollQualityPercent và CurrentValue theo Enhancement & Stars.
   - Không roll lại khi chạy migration lần thứ hai.
5. Cập nhật lại snapshot cache CurrentStats JSON trong HRK_PlayerInventory.
6. Đảm bảo cấu hình hệ số lực chiến trong HRK_CombatPowerConfigs cho MAGIC_DAMAGE.
=========================================================================================
Script này là IDEMPOTENT: chạy nhiều lần an toàn tuyệt đối.
=========================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

PRINT '=========================================================================================';
PRINT N'BẮT ĐẦU MIGRATION: THẦN BINH PHẢI CỘNG SÁT THƯƠNG PHÉP (MAGIC_DAMAGE)';
PRINT '=========================================================================================';

BEGIN TRANSACTION;

BEGIN TRY
    -- -------------------------------------------------------------------------------------
    -- 1. ĐẢM BẢO THUỘC TÍNH CANONICAL MAGIC_DAMAGE TRONG HRK_AttributeTypes
    -- -------------------------------------------------------------------------------------
    PRINT N'1. Kiểm tra và đảm bảo thuộc tính MAGIC_DAMAGE trong HRK_AttributeTypes...';

    IF OBJECT_ID(N'dbo.HRK_AttributeTypes', N'U') IS NOT NULL
    BEGIN
        MERGE dbo.HRK_AttributeTypes AS Target
        USING (VALUES
            ('MAGIC_DAMAGE', N'Sát Thương Phép', 0, 25, N'Chỉ số sát thương phép thuật cơ bản')
        ) AS Source ([Code], [Name], [IsPercentage], [DisplayOrder], [Description])
        ON Target.[Code] = Source.[Code]
        WHEN MATCHED THEN
            UPDATE SET Target.[Name] = Source.[Name],
                       Target.[IsPercentage] = Source.[IsPercentage],
                       Target.[DisplayOrder] = Source.[DisplayOrder],
                       Target.[UpdatedOn] = SYSUTCDATETIME()
        WHEN NOT MATCHED THEN
            INSERT ([Code], [Name], [IsPercentage], [DisplayOrder], [Description], [CreatedOn], [UpdatedOn])
            VALUES (Source.[Code], Source.[Name], Source.[IsPercentage], Source.[DisplayOrder], Source.[Description], SYSUTCDATETIME(), SYSUTCDATETIME());

        PRINT N'   -> Đã đảm bảo thuộc tính MAGIC_DAMAGE.';
    END

    -- -------------------------------------------------------------------------------------
    -- 2. CHO PHÉP MAGIC_DAMAGE TRÊN CATEGORY ARTIFACT
    -- -------------------------------------------------------------------------------------
    PRINT N'2. Cấu hình HRK_CategoryAllowedAttributes cho Category ARTIFACT...';

    DECLARE @CatArtifactId INT = (SELECT TOP 1 Id FROM dbo.HRK_ItemCategories WHERE Code = 'ARTIFACT');
    DECLARE @AttrMagicDamageId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE Code = 'MAGIC_DAMAGE');
    DECLARE @AttrMagicAtkId INT = (SELECT TOP 1 Id FROM dbo.HRK_AttributeTypes WHERE Code = 'MAGIC_ATK');

    IF @CatArtifactId IS NOT NULL AND @AttrMagicDamageId IS NOT NULL
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM dbo.HRK_CategoryAllowedAttributes 
            WHERE CategoryId = @CatArtifactId AND AttributeTypeId = @AttrMagicDamageId
        )
        BEGIN
            INSERT INTO dbo.HRK_CategoryAllowedAttributes 
                (CategoryId, AttributeTypeId, IsMainStat, IsSubStat, MinValue, MaxValue, DisplayOrder)
            VALUES 
                (@CatArtifactId, @AttrMagicDamageId, 1, 1, 20, 800, 25);
            PRINT N'   -> Đã cho phép MAGIC_DAMAGE trên Category ARTIFACT.';
        END
        ELSE
        BEGIN
            PRINT N'   -> Category ARTIFACT đã cho phép MAGIC_DAMAGE.';
        END
    END

    -- -------------------------------------------------------------------------------------
    -- 3. BỔ SUNG MAGIC_DAMAGE VÀO CÁC TEMPLATE THẦN BINH CHƯA CÓ
    -- -------------------------------------------------------------------------------------
    PRINT N'3. Cập nhật HRK_ItemTemplateAttributes cho các template Thần Binh...';

    IF @CatArtifactId IS NOT NULL AND @AttrMagicDamageId IS NOT NULL
    BEGIN
        -- Thêm thuộc tính MAGIC_DAMAGE cho các template Thần Binh chưa có
        INSERT INTO dbo.HRK_ItemTemplateAttributes (ItemTemplateId, AttributeTypeId, [Value], MinValue, MaxValue)
        SELECT 
            t.Id AS ItemTemplateId,
            @AttrMagicDamageId AS AttributeTypeId,
            CASE 
                WHEN UPPER(r.Code) = 'COMMON'    OR r.Id = 1 THEN 30.0000
                WHEN UPPER(r.Code) = 'RARE'      OR r.Id = 2 THEN 60.0000
                WHEN UPPER(r.Code) = 'EPIC'      OR r.Id = 3 THEN 120.0000
                WHEN UPPER(r.Code) = 'LEGENDARY' OR r.Id = 4 THEN 215.0000
                WHEN UPPER(r.Code) = 'MYTHIC'    OR r.Id = 5 THEN 350.0000
                ELSE 100.0000
            END AS [Value],
            CASE 
                WHEN UPPER(r.Code) = 'COMMON'    OR r.Id = 1 THEN 20.0000
                WHEN UPPER(r.Code) = 'RARE'      OR r.Id = 2 THEN 45.0000
                WHEN UPPER(r.Code) = 'EPIC'      OR r.Id = 3 THEN 90.0000
                WHEN UPPER(r.Code) = 'LEGENDARY' OR r.Id = 4 THEN 170.0000
                WHEN UPPER(r.Code) = 'MYTHIC'    OR r.Id = 5 THEN 280.0000
                ELSE 20.0000
            END AS MinValue,
            CASE 
                WHEN UPPER(r.Code) = 'COMMON'    OR r.Id = 1 THEN 40.0000
                WHEN UPPER(r.Code) = 'RARE'      OR r.Id = 2 THEN 80.0000
                WHEN UPPER(r.Code) = 'EPIC'      OR r.Id = 3 THEN 150.0000
                WHEN UPPER(r.Code) = 'LEGENDARY' OR r.Id = 4 THEN 260.0000
                WHEN UPPER(r.Code) = 'MYTHIC'    OR r.Id = 5 THEN 420.0000
                ELSE 420.0000
            END AS MaxValue
        FROM dbo.HRK_ItemTemplates t
        LEFT JOIN dbo.HRK_Rarities r ON t.RarityId = r.Id
        WHERE t.CategoryId = @CatArtifactId
          AND NOT EXISTS (
              SELECT 1 FROM dbo.HRK_ItemTemplateAttributes ita 
              WHERE ita.ItemTemplateId = t.Id AND ita.AttributeTypeId = @AttrMagicDamageId
          );

        -- Cập nhật MinValue, MaxValue cho các template Thần Binh đã có MAGIC_DAMAGE nhưng thiếu khoảng roll
        UPDATE ita
        SET ita.MinValue = CASE 
                WHEN UPPER(r.Code) = 'COMMON'    OR r.Id = 1 THEN 20.0000
                WHEN UPPER(r.Code) = 'RARE'      OR r.Id = 2 THEN 45.0000
                WHEN UPPER(r.Code) = 'EPIC'      OR r.Id = 3 THEN 90.0000
                WHEN UPPER(r.Code) = 'LEGENDARY' OR r.Id = 4 THEN 170.0000
                WHEN UPPER(r.Code) = 'MYTHIC'    OR r.Id = 5 THEN 280.0000
                ELSE 20.0000
            END,
            ita.MaxValue = CASE 
                WHEN UPPER(r.Code) = 'COMMON'    OR r.Id = 1 THEN 40.0000
                WHEN UPPER(r.Code) = 'RARE'      OR r.Id = 2 THEN 80.0000
                WHEN UPPER(r.Code) = 'EPIC'      OR r.Id = 3 THEN 150.0000
                WHEN UPPER(r.Code) = 'LEGENDARY' OR r.Id = 4 THEN 260.0000
                WHEN UPPER(r.Code) = 'MYTHIC'    OR r.Id = 5 THEN 420.0000
                ELSE 420.0000
            END
        FROM dbo.HRK_ItemTemplateAttributes ita
        JOIN dbo.HRK_ItemTemplates t ON ita.ItemTemplateId = t.Id
        LEFT JOIN dbo.HRK_Rarities r ON t.RarityId = r.Id
        WHERE t.CategoryId = @CatArtifactId
          AND ita.AttributeTypeId = @AttrMagicDamageId
          AND (ita.MinValue IS NULL OR ita.MaxValue IS NULL);

        PRINT N'   -> Đã đồng bộ MAGIC_DAMAGE và khoảng roll cho toàn bộ ItemTemplates Thần Binh.';
    END

    -- -------------------------------------------------------------------------------------
    -- 4. BACKFILL IDEMPOTENT CHO CÁC INSTANCE THẦN BINH (HRK_PlayerInventoryAttributes)
    -- -------------------------------------------------------------------------------------
    PRINT N'4. Backfill thuộc tính instance Thần Binh trong HRK_PlayerInventoryAttributes...';

    IF @CatArtifactId IS NOT NULL AND @AttrMagicDamageId IS NOT NULL
    BEGIN
        -- 4.1 Migrate an toàn: Nếu instance có MAGIC_ATK nhưng chưa có MAGIC_DAMAGE -> chuyển sang MAGIC_DAMAGE
        IF @AttrMagicAtkId IS NOT NULL
        BEGIN
            UPDATE pia
            SET pia.AttributeTypeId = @AttrMagicDamageId,
                pia.UpdatedOn = SYSUTCDATETIME()
            FROM dbo.HRK_PlayerInventoryAttributes pia
            JOIN dbo.HRK_PlayerInventory pi ON pia.PlayerInventoryId = pi.Id
            JOIN dbo.HRK_ItemTemplates t ON pi.ItemTemplateId = t.Id
            WHERE t.CategoryId = @CatArtifactId
              AND pia.AttributeTypeId = @AttrMagicAtkId
              AND NOT EXISTS (
                  SELECT 1 FROM dbo.HRK_PlayerInventoryAttributes existing
                  WHERE existing.PlayerInventoryId = pi.Id AND existing.AttributeTypeId = @AttrMagicDamageId
              );

            PRINT N'   -> Đã chuẩn hóa các instance Thần Binh có MAGIC_ATK sang MAGIC_DAMAGE.';
        END

        -- 4.2 Thêm mới cho các instance Thần Binh hoàn toàn chưa có MAGIC_DAMAGE: Roll đúng 1 lần trong khoảng template
        ;WITH MissingArtifactInstances AS (
            SELECT 
                pi.Id AS PlayerInventoryId,
                pi.Enhancement,
                pi.Stars,
                ISNULL(pi.EnhancementGrowthPercent, 10.00) AS GrowthPercent,
                pi.AcquiredOn,
                ita.MinValue AS RollMin,
                ita.MaxValue AS RollMax,
                ita.[Value] AS DefaultBase
            FROM dbo.HRK_PlayerInventory pi
            JOIN dbo.HRK_ItemTemplates t ON pi.ItemTemplateId = t.Id
            JOIN dbo.HRK_ItemTemplateAttributes ita ON ita.ItemTemplateId = t.Id AND ita.AttributeTypeId = @AttrMagicDamageId
            WHERE t.CategoryId = @CatArtifactId
              AND NOT EXISTS (
                  SELECT 1 FROM dbo.HRK_PlayerInventoryAttributes pia
                  WHERE pia.PlayerInventoryId = pi.Id AND pia.AttributeTypeId = @AttrMagicDamageId
              )
        ),
        RolledValues AS (
            SELECT 
                mai.PlayerInventoryId,
                mai.RollMin,
                mai.RollMax,
                -- Roll giá trị giả định ngẫu nhiên tất định dựa trên Id và AcquiredOn (không đổi nếu chạy lại)
                CASE 
                    WHEN mai.RollMax > mai.RollMin THEN
                        mai.RollMin + (ABS(CHECKSUM(CAST(mai.PlayerInventoryId AS VARCHAR(20)) + CAST(mai.AcquiredOn AS VARCHAR(50)))) % CAST((mai.RollMax - mai.RollMin + 1) AS INT))
                    ELSE mai.DefaultBase
                END AS RolledBase,
                mai.Enhancement,
                mai.Stars,
                mai.GrowthPercent
            FROM MissingArtifactInstances mai
        )
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
            rv.PlayerInventoryId,
            @AttrMagicDamageId,
            rv.RolledBase,
            ROUND(rv.RolledBase * (1.0 + (rv.Enhancement * rv.GrowthPercent / 100.0)) * (1.0 + (rv.Stars * 0.10)), 2) AS CurrentValue,
            rv.RollMin,
            rv.RollMax,
            CASE 
                WHEN rv.RollMax > rv.RollMin THEN
                    ROUND(((rv.RolledBase - rv.RollMin) / (rv.RollMax - rv.RollMin)) * 100.0, 2)
                ELSE 100.00
            END AS RollQualityPercent,
            SYSUTCDATETIME(),
            SYSUTCDATETIME()
        FROM RolledValues rv;

        PRINT N'   -> Đã backfill thuộc tính MAGIC_DAMAGE cho các instance Thần Binh chưa có.';
    END

    -- -------------------------------------------------------------------------------------
    -- 5. CẬP NHẬT LẠI CurrentStats JSON SNAPSHOT TRONG HRK_PlayerInventory
    -- -------------------------------------------------------------------------------------
    PRINT N'5. Cập nhật lại cache CurrentStats JSON cho các Thần Binh trong HRK_PlayerInventory...';

    IF @CatArtifactId IS NOT NULL
    BEGIN
        ;WITH CalcArtifactStats AS (
            SELECT 
                inv.Id AS InvId,
                '{' + STRING_AGG('"' + at.[Code] + '":' + 
                    CAST(
                        CASE 
                            WHEN at.IsPercentage = 1 THEN 
                                ROUND(pia.BaseRolledValue * (1.0 + (inv.Enhancement * ISNULL(inv.EnhancementGrowthPercent, 10.0) / 100.0)), 4)
                            ELSE 
                                ROUND(pia.BaseRolledValue * (1.0 + (inv.Enhancement * ISNULL(inv.EnhancementGrowthPercent, 10.0) / 100.0)) * (1.0 + (inv.Stars * 0.10)), 2)
                        END 
                    AS NVARCHAR(30)), ',') + '}' AS CalculatedJson
            FROM dbo.HRK_PlayerInventory inv
            JOIN dbo.HRK_ItemTemplates t ON inv.ItemTemplateId = t.Id
            JOIN dbo.HRK_PlayerInventoryAttributes pia ON pia.PlayerInventoryId = inv.Id
            JOIN dbo.HRK_AttributeTypes at ON pia.AttributeTypeId = at.Id
            WHERE t.CategoryId = @CatArtifactId
            GROUP BY inv.Id
        )
        UPDATE inv
        SET inv.CurrentStats = cs.CalculatedJson,
            inv.UpdatedOn = SYSUTCDATETIME()
        FROM dbo.HRK_PlayerInventory inv
        JOIN CalcArtifactStats cs ON inv.Id = cs.InvId;

        PRINT N'   -> Đã đồng bộ snapshot CurrentStats cho toàn bộ instance Thần Binh.';
    END

    -- -------------------------------------------------------------------------------------
    -- 6. ĐẢM BẢO CẤU HÌNH LỰC CHIẾN (HRK_CombatPowerConfigs) CHO MAGIC_DAMAGE
    -- -------------------------------------------------------------------------------------
    PRINT N'6. Kiểm tra cấu hình lực chiến cho MAGIC_DAMAGE...';

    IF OBJECT_ID(N'dbo.HRK_CombatPowerConfigs', N'U') IS NOT NULL
    BEGIN
        MERGE dbo.HRK_CombatPowerConfigs AS Target
        USING (VALUES
            ('MAGIC_DAMAGE', CAST(1.5000 AS DECIMAL(18,4)), 25, 1)
        ) AS Source (StatCode, PowerPerUnit, DisplayOrder, IsEnabled)
        ON Target.StatCode = Source.StatCode
        WHEN MATCHED THEN
            UPDATE SET Target.PowerPerUnit = Source.PowerPerUnit,
                       Target.IsEnabled = 1,
                       Target.UpdatedOn = SYSUTCDATETIME()
        WHEN NOT MATCHED THEN
            INSERT (StatCode, PowerPerUnit, DisplayOrder, IsEnabled, UpdatedOn)
            VALUES (Source.StatCode, Source.PowerPerUnit, Source.DisplayOrder, Source.IsEnabled, SYSUTCDATETIME());

        PRINT N'   -> Đã đảm bảo cấu hình lực chiến cho MAGIC_DAMAGE.';
    END

    COMMIT TRANSACTION;

    PRINT '=========================================================================================';
    PRINT N'HOÀN TẤT MIGRATION: THẦN BINH CỘNG SÁT THƯƠNG PHÉP THÀNH CÔNG RỰC RỠ!';
    PRINT '=========================================================================================';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();

    RAISERROR(N'Lỗi thực thi Migration: %s', @ErrSeverity, @ErrState, @ErrMsg);
END CATCH;
GO

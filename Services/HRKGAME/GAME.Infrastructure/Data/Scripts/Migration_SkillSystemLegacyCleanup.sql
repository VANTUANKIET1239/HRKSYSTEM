/*
  Migration_SkillSystemLegacyCleanup.sql
  --------------------------------------------------------------------------------
  DCS Game - Skill System Legacy Cleanup Migration
  LƯU Ý QUAN TRỌNG:
  Chỉ chạy script này SAU KHI toàn bộ Backend và Frontend đã được chuyển giao,
  hoạt động ổn định trên hệ thống data-driven mới (HRK_SkillEffects, HRK_SkillEffectScalings, EnergyCost).
  
  Mục đích:
  - Xóa bỏ các Foreign Key constraints legacy trỏ tới các bảng cũ.
  - Drop các cột legacy trên HRK_SkillTemplates:
    Cost, CostTypeId, CategoryId, DamageTypeId, EffectTypeId, DamageMultiplier, TargetType, Cooldown.
  - Drop các bảng legacy:
    HRK_SkillCostTypes, HRK_SkillCategories, HRK_SkillDamageTypes.
*/

SET XACT_ABORT ON;
BEGIN TRANSACTION;

PRINT '=== [BẮT ĐẦU] Migration_SkillSystemLegacyCleanup ===';

-- ---------------------------------------------------------------------------
-- 1. DROP CÁC FOREIGN KEY CONSTRAINTS LEGACY
-- ---------------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_HRK_SkillTemplates_CostType' AND parent_object_id = OBJECT_ID('dbo.HRK_SkillTemplates'))
BEGIN
    ALTER TABLE dbo.HRK_SkillTemplates DROP CONSTRAINT FK_HRK_SkillTemplates_CostType;
    PRINT '-> Da drop FK_HRK_SkillTemplates_CostType.';
END

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_HRK_SkillTemplates_Category' AND parent_object_id = OBJECT_ID('dbo.HRK_SkillTemplates'))
BEGIN
    ALTER TABLE dbo.HRK_SkillTemplates DROP CONSTRAINT FK_HRK_SkillTemplates_Category;
    PRINT '-> Da drop FK_HRK_SkillTemplates_Category.';
END

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_HRK_SkillTemplates_DamageType' AND parent_object_id = OBJECT_ID('dbo.HRK_SkillTemplates'))
BEGIN
    ALTER TABLE dbo.HRK_SkillTemplates DROP CONSTRAINT FK_HRK_SkillTemplates_DamageType;
    PRINT '-> Da drop FK_HRK_SkillTemplates_DamageType.';
END

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_HRK_SkillTemplates_EffectType' AND parent_object_id = OBJECT_ID('dbo.HRK_SkillTemplates'))
BEGIN
    ALTER TABLE dbo.HRK_SkillTemplates DROP CONSTRAINT FK_HRK_SkillTemplates_EffectType;
    PRINT '-> Da drop FK_HRK_SkillTemplates_EffectType.';
END

-- ---------------------------------------------------------------------------
-- 2. DROP CÁC DEFAULT CONSTRAINTS LEGACY TRÊN HRK_SkillTemplates
-- ---------------------------------------------------------------------------
DECLARE @sql NVARCHAR(MAX) = N'';

SELECT @sql += N'ALTER TABLE dbo.HRK_SkillTemplates DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';' + CHAR(10)
FROM sys.default_constraints dc
JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE dc.parent_object_id = OBJECT_ID('dbo.HRK_SkillTemplates')
  AND c.name IN ('Cost', 'DamageMultiplier', 'TargetType', 'Cooldown');

IF LEN(@sql) > 0
BEGIN
    EXEC sp_executesql @sql;
    PRINT '-> Da drop cac default constraints tren cot legacy cua HRK_SkillTemplates.';
END

-- ---------------------------------------------------------------------------
-- 3. DROP CÁC CỘT LEGACY TRÊN HRK_SkillTemplates
-- ---------------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HRK_SkillTemplates') AND name = 'Cost')
BEGIN
    ALTER TABLE dbo.HRK_SkillTemplates DROP COLUMN Cost;
    PRINT '-> Da drop cot Cost.';
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HRK_SkillTemplates') AND name = 'CostTypeId')
BEGIN
    ALTER TABLE dbo.HRK_SkillTemplates DROP COLUMN CostTypeId;
    PRINT '-> Da drop cot CostTypeId.';
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HRK_SkillTemplates') AND name = 'CategoryId')
BEGIN
    ALTER TABLE dbo.HRK_SkillTemplates DROP COLUMN CategoryId;
    PRINT '-> Da drop cot CategoryId.';
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HRK_SkillTemplates') AND name = 'DamageTypeId')
BEGIN
    ALTER TABLE dbo.HRK_SkillTemplates DROP COLUMN DamageTypeId;
    PRINT '-> Da drop cot DamageTypeId.';
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HRK_SkillTemplates') AND name = 'EffectTypeId')
BEGIN
    ALTER TABLE dbo.HRK_SkillTemplates DROP COLUMN EffectTypeId;
    PRINT '-> Da drop cot EffectTypeId tren HRK_SkillTemplates.';
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HRK_SkillTemplates') AND name = 'DamageMultiplier')
BEGIN
    ALTER TABLE dbo.HRK_SkillTemplates DROP COLUMN DamageMultiplier;
    PRINT '-> Da drop cot DamageMultiplier.';
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HRK_SkillTemplates') AND name = 'TargetType')
BEGIN
    ALTER TABLE dbo.HRK_SkillTemplates DROP COLUMN TargetType;
    PRINT '-> Da drop cot TargetType.';
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HRK_SkillTemplates') AND name = 'Cooldown')
BEGIN
    ALTER TABLE dbo.HRK_SkillTemplates DROP COLUMN Cooldown;
    PRINT '-> Da drop cot Cooldown.';
END

-- ---------------------------------------------------------------------------
-- 4. DROP CÁC BẢNG LEGACY
-- ---------------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HRK_SkillCostTypes' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DROP TABLE dbo.HRK_SkillCostTypes;
    PRINT '-> Da drop bang HRK_SkillCostTypes.';
END

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HRK_SkillCategories' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DROP TABLE dbo.HRK_SkillCategories;
    PRINT '-> Da drop bang HRK_SkillCategories.';
END

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HRK_SkillDamageTypes' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DROP TABLE dbo.HRK_SkillDamageTypes;
    PRINT '-> Da drop bang HRK_SkillDamageTypes.';
END

COMMIT TRANSACTION;
PRINT '=== [HOÀN THÀNH] Migration_SkillSystemLegacyCleanup thanh cong! ===';

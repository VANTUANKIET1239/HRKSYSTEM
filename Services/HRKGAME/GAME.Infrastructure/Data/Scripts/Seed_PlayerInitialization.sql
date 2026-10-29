/* Default starter package. Edit these reviewed values BEFORE first use.
   Hero IDs 1,2,3 are explicit existing templates (not randomly chosen from all heroes).
   Re-running never overwrites an existing version or reactivates a retired version.
*/
USE HRK;
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @config bigint = (SELECT Id FROM dbo.HRK_PlayerInitializationConfigs WITH(UPDLOCK,HOLDLOCK) WHERE Code=N'DEFAULT' AND Version=1);
IF @config IS NULL
BEGIN
    DECLARE @formation int = (SELECT Id FROM dbo.HRK_FormationTemplates WHERE Code=N'LUC_DO' AND IsEnabled=1);
    DECLARE @avatar int = (SELECT TOP(1) Id FROM dbo.HRK_AvatarTemplates WHERE IsEnabled=1 ORDER BY IsDefault DESC,DisplayOrder,Id);
    DECLARE @stone int = (SELECT Id FROM dbo.HRK_ItemTemplates WHERE Code=N'ENHANCEMENT_STONE_I');
    IF @formation IS NULL OR @avatar IS NULL OR @stone IS NULL
        THROW 51000, 'Seed formation, avatar and enhancement stone templates before starter config.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.HRK_PlayerLevelConfigs WHERE Level=1 AND IsActive=1)
       OR NOT EXISTS(SELECT 1 FROM dbo.HRK_HeroLevelConfigs WHERE Level=1 AND IsActive=1)
        THROW 51000, 'Seed player and hero level-1 EXP configurations first.', 1;
    IF (SELECT COUNT(*) FROM dbo.HRK_HeroTemplates WHERE Id IN(1,2,3))<>3
        THROW 51000, 'Starter hero templates 1,2,3 are required; choose explicit valid IDs before running.', 1;
    IF EXISTS (SELECT 1 FROM dbo.HRK_HeroTemplates h WHERE h.Id IN(1,2,3) AND
        (NOT EXISTS(SELECT 1 FROM dbo.HRK_HeroRarityUpgradeConfigs c WHERE c.RarityId=h.RarityId)
        OR NOT EXISTS(SELECT 1 FROM dbo.HRK_HeroStarAttributePools a WHERE a.RarityId=h.RarityId AND a.IsEnabled=1)))
        THROW 51000, 'Seed hero progression and star attribute pools first.', 1;
    IF (SELECT COUNT(*) FROM dbo.HRK_FormationSlotTemplates WHERE FormationTemplateId=@formation AND IsEnabled=1 AND Slot IN(1,2,3))<>3
        THROW 51000, 'Starter formation slots 1,2,3 are required.', 1;
    INSERT dbo.HRK_PlayerInitializationConfigs
        (Code,Version,IsActive,InitialLevel,InitialStamina,MaxStamina,InitialGold,InitialDiamonds,
         InitialUpgradeMaterials,InventoryCapacity,DefaultFormationTemplateId,DefaultAvatarTemplateId)
    VALUES(N'DEFAULT',1,0,1,200,200,10000,300,100,200,@formation,@avatar);
    SET @config=SCOPE_IDENTITY();
    INSERT dbo.HRK_PlayerInitializationHeroes(ConfigId,HeroTemplateId,InitialLevel,InitialStars,FormationPosition,DisplayOrder)
    VALUES(@config,1,1,1,1,1),(@config,2,1,1,2,2),(@config,3,1,1,3,3);
    INSERT dbo.HRK_PlayerInitializationItems(ConfigId,ItemTemplateId,Quantity,DisplayOrder) VALUES(@config,@stone,10,1);
    IF NOT EXISTS(SELECT 1 FROM dbo.HRK_PlayerInitializationConfigs WHERE Code=N'DEFAULT' AND IsActive=1)
        UPDATE dbo.HRK_PlayerInitializationConfigs SET IsActive=1 WHERE Id=@config;
END;
COMMIT;
SELECT Id,Code,Version,IsActive FROM dbo.HRK_PlayerInitializationConfigs WHERE Id=@config;

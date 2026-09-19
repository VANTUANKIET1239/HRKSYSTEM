/*
  Equip existing inventory items on ONE owned hero (SQL Server).

  Important invariants:
  - HRK_PlayerEquipment.*Id always stores HRK_PlayerInventory.Id, NEVER ItemTemplateId.
  - An item must belong to @PlayerId and its category must match its destination slot.
  - Only unequipped items (or items already equipped by @HeroId) are selected.
  - Existing valid slots are preserved. Invalid legacy slot values are cleared and repaired.
*/

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @PlayerId BIGINT = 1; -- TODO: set the target player's Id
DECLARE @HeroId BIGINT = 1;   -- TODO: set an HRK_PlayerHeroes.Id owned by @PlayerId

IF NOT EXISTS (SELECT 1 FROM dbo.HRK_Players WHERE Id = @PlayerId AND IsActive = 1)
    THROW 50001, 'PlayerId does not exist or is inactive.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.HRK_PlayerHeroes WHERE Id = @HeroId AND PlayerId = @PlayerId AND IsActive = 1)
    THROW 50002, 'HeroId does not belong to PlayerId or is inactive.', 1;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_HRK_PlayerInventory_Player_Equipment'
      AND object_id = OBJECT_ID(N'dbo.HRK_PlayerInventory')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_HRK_PlayerInventory_Player_Equipment
        ON dbo.HRK_PlayerInventory (PlayerId, IsActive, IsEquipped, EquippedHeroId)
        INCLUDE (ItemTemplateId, Enhancement, Stars, IsLocked);
END;

/* Create the six-slot record when the hero has never equipped an item. */
IF NOT EXISTS (SELECT 1 FROM dbo.HRK_PlayerEquipment WHERE PlayerId = @PlayerId AND HeroId = @HeroId)
BEGIN
    INSERT INTO dbo.HRK_PlayerEquipment (PlayerId, HeroId)
    VALUES (@PlayerId, @HeroId);
END;

/*
  Clear a legacy value only when it points to a wrong-player, inactive, or wrong-category item.
  This prevents values such as an ARMOR inventory row being stored in WeaponId.
*/
UPDATE pe
SET WeaponId = CASE WHEN validWeapon.Id IS NULL THEN NULL ELSE pe.WeaponId END,
    ArmorId = CASE WHEN validArmor.Id IS NULL THEN NULL ELSE pe.ArmorId END,
    HelmetId = CASE WHEN validHelmet.Id IS NULL THEN NULL ELSE pe.HelmetId END,
    BootsId = CASE WHEN validBoots.Id IS NULL THEN NULL ELSE pe.BootsId END,
    RingId = CASE WHEN validRing.Id IS NULL THEN NULL ELSE pe.RingId END,
    ArtifactId = CASE WHEN validArtifact.Id IS NULL THEN NULL ELSE pe.ArtifactId END
FROM dbo.HRK_PlayerEquipment pe
OUTER APPLY (SELECT pi.Id FROM dbo.HRK_PlayerInventory pi JOIN dbo.HRK_ItemTemplates it ON it.Id = pi.ItemTemplateId JOIN dbo.HRK_ItemCategories c ON c.Id = it.CategoryId WHERE pi.Id = pe.WeaponId AND pi.PlayerId = @PlayerId AND pi.IsActive = 1 AND c.Code = 'WEAPON') validWeapon
OUTER APPLY (SELECT pi.Id FROM dbo.HRK_PlayerInventory pi JOIN dbo.HRK_ItemTemplates it ON it.Id = pi.ItemTemplateId JOIN dbo.HRK_ItemCategories c ON c.Id = it.CategoryId WHERE pi.Id = pe.ArmorId AND pi.PlayerId = @PlayerId AND pi.IsActive = 1 AND c.Code = 'ARMOR') validArmor
OUTER APPLY (SELECT pi.Id FROM dbo.HRK_PlayerInventory pi JOIN dbo.HRK_ItemTemplates it ON it.Id = pi.ItemTemplateId JOIN dbo.HRK_ItemCategories c ON c.Id = it.CategoryId WHERE pi.Id = pe.HelmetId AND pi.PlayerId = @PlayerId AND pi.IsActive = 1 AND c.Code = 'HELMET') validHelmet
OUTER APPLY (SELECT pi.Id FROM dbo.HRK_PlayerInventory pi JOIN dbo.HRK_ItemTemplates it ON it.Id = pi.ItemTemplateId JOIN dbo.HRK_ItemCategories c ON c.Id = it.CategoryId WHERE pi.Id = pe.BootsId AND pi.PlayerId = @PlayerId AND pi.IsActive = 1 AND c.Code = 'BOOTS') validBoots
OUTER APPLY (SELECT pi.Id FROM dbo.HRK_PlayerInventory pi JOIN dbo.HRK_ItemTemplates it ON it.Id = pi.ItemTemplateId JOIN dbo.HRK_ItemCategories c ON c.Id = it.CategoryId WHERE pi.Id = pe.RingId AND pi.PlayerId = @PlayerId AND pi.IsActive = 1 AND c.Code = 'RING') validRing
OUTER APPLY (SELECT pi.Id FROM dbo.HRK_PlayerInventory pi JOIN dbo.HRK_ItemTemplates it ON it.Id = pi.ItemTemplateId JOIN dbo.HRK_ItemCategories c ON c.Id = it.CategoryId WHERE pi.Id = pe.ArtifactId AND pi.PlayerId = @PlayerId AND pi.IsActive = 1 AND c.Code = 'ARTIFACT') validArtifact
WHERE pe.PlayerId = @PlayerId AND pe.HeroId = @HeroId;

DECLARE @EmptySlots TABLE (SlotCode VARCHAR(20) NOT NULL PRIMARY KEY);
INSERT INTO @EmptySlots (SlotCode)
SELECT v.SlotCode
FROM dbo.HRK_PlayerEquipment pe
CROSS APPLY (VALUES
    ('WEAPON', pe.WeaponId), ('ARMOR', pe.ArmorId), ('HELMET', pe.HelmetId),
    ('BOOTS', pe.BootsId), ('RING', pe.RingId), ('ARTIFACT', pe.ArtifactId)
) v(SlotCode, InventoryId)
WHERE pe.PlayerId = @PlayerId AND pe.HeroId = @HeroId AND v.InventoryId IS NULL;

DECLARE @Selected TABLE
(
    SlotCode VARCHAR(20) NOT NULL PRIMARY KEY,
    InventoryId BIGINT NOT NULL UNIQUE
);

;WITH Candidates AS
(
    SELECT
        c.Code AS SlotCode,
        pi.Id AS InventoryId,
        ROW_NUMBER() OVER
        (
            PARTITION BY c.Code
            ORDER BY it.RarityId DESC, pi.Enhancement DESC, pi.Stars DESC, pi.Id ASC
        ) AS RowNumber
    FROM dbo.HRK_PlayerInventory pi
    INNER JOIN dbo.HRK_ItemTemplates it ON it.Id = pi.ItemTemplateId
    INNER JOIN dbo.HRK_ItemCategories c ON c.Id = it.CategoryId
    INNER JOIN @EmptySlots es ON es.SlotCode = c.Code
    WHERE pi.PlayerId = @PlayerId
      AND pi.IsActive = 1
      AND c.Code IN ('WEAPON', 'ARMOR', 'HELMET', 'BOOTS', 'RING', 'ARTIFACT')
      AND (pi.IsEquipped = 0 OR pi.EquippedHeroId = @HeroId)
      AND (pi.EquippedHeroId IS NULL OR pi.EquippedHeroId = @HeroId)
)
INSERT INTO @Selected (SlotCode, InventoryId)
SELECT SlotCode, InventoryId
FROM Candidates
WHERE RowNumber = 1;

UPDATE pe
SET WeaponId = COALESCE(pe.WeaponId, (SELECT InventoryId FROM @Selected WHERE SlotCode = 'WEAPON')),
    ArmorId = COALESCE(pe.ArmorId, (SELECT InventoryId FROM @Selected WHERE SlotCode = 'ARMOR')),
    HelmetId = COALESCE(pe.HelmetId, (SELECT InventoryId FROM @Selected WHERE SlotCode = 'HELMET')),
    BootsId = COALESCE(pe.BootsId, (SELECT InventoryId FROM @Selected WHERE SlotCode = 'BOOTS')),
    RingId = COALESCE(pe.RingId, (SELECT InventoryId FROM @Selected WHERE SlotCode = 'RING')),
    ArtifactId = COALESCE(pe.ArtifactId, (SELECT InventoryId FROM @Selected WHERE SlotCode = 'ARTIFACT'))
FROM dbo.HRK_PlayerEquipment pe
WHERE pe.PlayerId = @PlayerId AND pe.HeroId = @HeroId;

/* Synchronise all currently referenced items as equipped by this hero. */
UPDATE pi
SET IsEquipped = 1,
    EquippedHeroId = @HeroId,
    UpdatedOn = SYSUTCDATETIME()
FROM dbo.HRK_PlayerInventory pi
INNER JOIN dbo.HRK_PlayerEquipment pe ON pe.PlayerId = @PlayerId AND pe.HeroId = @HeroId
WHERE pi.Id IN (pe.WeaponId, pe.ArmorId, pe.HelmetId, pe.BootsId, pe.RingId, pe.ArtifactId)
  AND pi.PlayerId = @PlayerId;

/* Unflag stale inventory items formerly assigned to this hero but no longer present in its six slots. */
UPDATE pi
SET IsEquipped = 0,
    EquippedHeroId = NULL,
    UpdatedOn = SYSUTCDATETIME()
FROM dbo.HRK_PlayerInventory pi
INNER JOIN dbo.HRK_PlayerEquipment pe ON pe.PlayerId = @PlayerId AND pe.HeroId = @HeroId
WHERE pi.PlayerId = @PlayerId
  AND pi.EquippedHeroId = @HeroId
  AND pi.Id NOT IN
  (
      SELECT Id
      FROM (VALUES (pe.WeaponId), (pe.ArmorId), (pe.HelmetId), (pe.BootsId), (pe.RingId), (pe.ArtifactId)) slots(Id)
      WHERE Id IS NOT NULL
  );

COMMIT TRANSACTION;

/* Verification: every populated slot must reference a matching PlayerInventory category. */
SELECT
    pe.PlayerId,
    pe.HeroId,
    v.SlotCode,
    v.InventoryId,
    it.Name AS ItemName,
    c.Code AS CategoryCode,
    pi.IsEquipped,
    pi.EquippedHeroId,
    pi.Enhancement
FROM dbo.HRK_PlayerEquipment pe
CROSS APPLY (VALUES
    ('WEAPON', pe.WeaponId), ('ARMOR', pe.ArmorId), ('HELMET', pe.HelmetId),
    ('BOOTS', pe.BootsId), ('RING', pe.RingId), ('ARTIFACT', pe.ArtifactId)
) v(SlotCode, InventoryId)
LEFT JOIN dbo.HRK_PlayerInventory pi ON pi.Id = v.InventoryId
LEFT JOIN dbo.HRK_ItemTemplates it ON it.Id = pi.ItemTemplateId
LEFT JOIN dbo.HRK_ItemCategories c ON c.Id = it.CategoryId
WHERE pe.PlayerId = @PlayerId AND pe.HeroId = @HeroId
ORDER BY CASE v.SlotCode
    WHEN 'WEAPON' THEN 1 WHEN 'ARMOR' THEN 2 WHEN 'HELMET' THEN 3
    WHEN 'BOOTS' THEN 4 WHEN 'RING' THEN 5 WHEN 'ARTIFACT' THEN 6 END;

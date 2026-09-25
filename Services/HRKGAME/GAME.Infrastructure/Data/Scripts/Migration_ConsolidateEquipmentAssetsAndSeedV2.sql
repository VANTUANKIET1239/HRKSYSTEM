/*
    Consolidate equipment images under one public asset root and seed every
    Equipment V2 item whose image currently exists in that folder.

    Canonical frontend folder:
      HrkUi/src/assets/images/dcs-game/equipments/

    Canonical public URL:
      /assets/images/dcs-game/equipments/{file}

    Idempotent: safe to execute more than once.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    IF OBJECT_ID(N'dbo.HRK_ItemTemplates', N'U') IS NULL
        THROW 51000, 'Missing table dbo.HRK_ItemTemplates.', 1;
    IF OBJECT_ID(N'dbo.HRK_ItemCategories', N'U') IS NULL
        THROW 51000, 'Missing table dbo.HRK_ItemCategories.', 1;
    IF OBJECT_ID(N'dbo.HRK_Rarities', N'U') IS NULL
        THROW 51000, 'Missing table dbo.HRK_Rarities.', 1;
    IF OBJECT_ID(N'dbo.HRK_AttributeTypes', N'U') IS NULL
        THROW 51000, 'Missing table dbo.HRK_AttributeTypes. Run Migration_RelationalItemAttributes.sql first.', 1;
    IF OBJECT_ID(N'dbo.HRK_ItemTemplateAttributes', N'U') IS NULL
        THROW 51000, 'Missing table dbo.HRK_ItemTemplateAttributes. Run Migration_RelationalItemAttributes.sql first.', 1;
    IF OBJECT_ID(N'dbo.HRK_PlayerInventory', N'U') IS NULL
        THROW 51000, 'Missing table dbo.HRK_PlayerInventory.', 1;
    IF OBJECT_ID(N'dbo.HRK_Players', N'U') IS NULL
        THROW 51000, 'Missing table dbo.HRK_Players.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_Players WHERE Id = 1)
        THROW 51000, 'PlayerId = 1 does not exist.', 1;

    -- Normalize every legacy DB URL before the obsolete asset folder is removed.
    UPDATE dbo.HRK_ItemTemplates
    SET ImagePath = CASE
            WHEN ImagePath LIKE '/assets/equipments/%'
                THEN REPLACE(ImagePath, '/assets/equipments/', '/assets/images/dcs-game/equipments/')
            ELSE REPLACE(ImagePath, 'assets/equipments/', '/assets/images/dcs-game/equipments/')
        END,
        UpdatedOn = SYSUTCDATETIME()
    WHERE ImagePath LIKE 'assets/equipments/%'
       OR ImagePath LIKE '/assets/equipments/%';

    DECLARE @Items TABLE
    (
        Code NVARCHAR(100) NOT NULL PRIMARY KEY,
        Name NVARCHAR(150) NOT NULL,
        CategoryCode NVARCHAR(50) NOT NULL,
        RarityCode NVARCHAR(50) NOT NULL,
        FileName NVARCHAR(150) NOT NULL,
        LevelReq INT NOT NULL,
        SellPrice INT NOT NULL,
        Description NVARCHAR(500) NOT NULL,
        Attr1Code NVARCHAR(50) NOT NULL,
        Attr1Value DECIMAL(18,4) NOT NULL,
        Attr2Code NVARCHAR(50) NULL,
        Attr2Value DECIMAL(18,4) NULL,
        Attr3Code NVARCHAR(50) NULL,
        Attr3Value DECIMAL(18,4) NULL
    );

    INSERT INTO @Items
        (Code, Name, CategoryCode, RarityCode, FileName, LevelReq, SellPrice, Description,
         Attr1Code, Attr1Value, Attr2Code, Attr2Value, Attr3Code, Attr3Value)
    VALUES
        ('COMBAT_BROOM_MK2', N'Chổi Bay Công Nghiệp MK-II', 'WEAPON', 'RARE', 'combat-broom-mk2.png', 15, 1200,
         N'Cây chổi chiến đấu gắn động cơ phản lực, quét sạch cả bụi bẩn lẫn kẻ địch.', 'PHYSICAL_ATK', 180, 'SPEED', 18, NULL, NULL),
        ('SIX_STRING_DEMON_GUITAR', N'Ma Cầm Sáu Dây', 'WEAPON', 'EPIC', 'six-string-demon-guitar.png', 30, 3600,
         N'Cây đàn ma phát ra sóng âm tím đủ sức nghiền nát ý chí đối phương.', 'PHYSICAL_ATK', 280, 'CRIT_RATE', 0.10, NULL, NULL),
        ('APOCALYPSE_MICROWAVE_CANNON', N'Pháo Vi Sóng Tận Thế', 'WEAPON', 'LEGENDARY', 'apocalypse-microwave-cannon.png', 45, 8200,
         N'Lò vi sóng cải tiến thành pháo plasma, nung nóng chiến trường chỉ trong vài giây.', 'PHYSICAL_ATK', 430, 'ARMOR_PEN', 90, 'SPEED', 8),
        ('NEBULA_PHOTON_SWORD', N'Kiếm Quang Tử Tinh Vân', 'WEAPON', 'MYTHIC', 'nebula-photon-sword.png', 50, 15000,
         N'Lưỡi kiếm photon chứa một tinh vân thu nhỏ và có thể xé rách lớp giáp kiên cố nhất.', 'PHYSICAL_ATK', 720, 'CRIT_RATE', 0.18, 'ARMOR_PEN', 140),

        ('LIGHTNING_RAINCOAT', N'Áo Mưa Chống Sét', 'ARMOR', 'COMMON', 'lightning-raincoat.png', 1, 120,
         N'Áo mưa được gia cố để chống lại thời tiết khắc nghiệt và những tia điện lạc.', 'ARMOR', 25, 'HP', 200, NULL, NULL),
        ('VIRTUAL_GLASS_ARMOR', N'Giáp Kính Thực Tế Ảo', 'ARMOR', 'RARE', 'virtual-glass-armor.png', 18, 1500,
         N'Các phiến giáp kính hologram phân tán lực tác động trước khi chúng chạm vào cơ thể.', 'ARMOR', 70, 'HP', 650, 'MAGIC_RESIST', 40),
        ('VOID_CIRCUS_COAT', N'Chiến Bào Rạp Xiếc Hư Không', 'ARMOR', 'EPIC', 'void-circus-coat.png', 32, 4200,
         N'Chiến bào của một ảo thuật gia hư không, khiến đòn đánh trượt khỏi thực tại.', 'ARMOR', 130, 'HP', 1400, 'DODGE_RATE', 0.06),
        ('SOLAR_REACTOR_ARMOR', N'Giáp Lò Phản Ứng Thái Dương', 'ARMOR', 'LEGENDARY', 'solar-reactor-armor.png', 45, 9000,
         N'Bộ giáp vận hành bằng một mặt trời nhân tạo được phong ấn trong lồng ngực.', 'ARMOR', 230, 'HP', 2600, 'MAGIC_RESIST', 120),

        ('TACTICAL_HONEYCOMB_SANDALS', N'Dép Tổ Ong Chiến Thuật', 'BOOTS', 'COMMON', 'tactical-honeycomb-sandals.png', 1, 80,
         N'Đôi dép tổ ong huyền thoại đã được gia cố để hành quân trên mọi địa hình.', 'ARMOR', 12, 'SPEED', 6, NULL, NULL),
        ('NEON_JET_SKATES', N'Giày Trượt Phản Lực Neon', 'BOOTS', 'RARE', 'neon-jet-skates.png', 16, 1400,
         N'Giày trượt gắn động cơ nhỏ giúp người sử dụng lướt qua chiến trường như ánh sáng.', 'SPEED', 25, 'DODGE_RATE', 0.04, NULL, NULL),
        ('QUANTUM_CAT_SLIPPERS', N'Hài Mèo Lượng Tử', 'BOOTS', 'EPIC', 'quantum-cat-slippers.png', 33, 3900,
         N'Đôi hài mèo tồn tại ở nhiều vị trí cùng lúc cho tới khi bị đối thủ quan sát.', 'SPEED', 38, 'DODGE_RATE', 0.08, 'ARMOR', 65),
        ('ZERO_GRAVITY_CELESTIAL_BOOTS', N'Bộ Hành Tinh Không', 'BOOTS', 'MYTHIC', 'zero-gravity-celestial-boots.png', 50, 14500,
         N'Chiến hài điều khiển trọng lực, cho phép chủ nhân bước đi giữa các vì sao.', 'SPEED', 58, 'DODGE_RATE', 0.12, 'ARMOR', 130),

        ('OLD_REPAIR_NOTEBOOK', N'Sổ Tay Sửa Máy Cũ', 'ARTIFACT', 'COMMON', 'old-repair-notebook.png', 1, 100,
         N'Cuốn sổ cũ chứa những mẹo sửa chữa đơn giản nhưng đáng tin cậy trong mọi cuộc hành trình.', 'MAGIC_ATK', 25, 'HP', 120, NULL, NULL),
        ('SUMMONING_TABLET', N'Máy Tính Bảng Triệu Hồi', 'ARTIFACT', 'RARE', 'summoning-tablet.png', 18, 1500,
         N'Pháp khí công nghệ mở kết nối số hóa để triệu hồi linh thể trợ chiến.', 'MAGIC_ATK', 160, 'HP', 500, NULL, NULL),
        ('ALCHEMY_RICE_COOKER', N'Nồi Cơm Luyện Đan', 'ARTIFACT', 'EPIC', 'alchemy-rice-cooker.jpg', 32, 4100,
         N'Nồi cơm cải tiến thành lò luyện đan, tỏa ra hơi thuốc tím đầy ma lực.', 'MAGIC_ATK', 280, 'HP', 1100, 'MAGIC_PEN', 55),
        ('MULTIVERSE_ENCYCLOPEDIA', N'Đại Từ Điển Đa Vũ Trụ', 'ARTIFACT', 'LEGENDARY', 'multiverse-encyclopedia.jpg', 45, 8800,
         N'Kho tri thức liên kết vô số thế giới, ban sức mạnh từ những thực tại xa xôi.', 'MAGIC_ATK', 460, 'HP', 2000, 'MAGIC_PEN', 100),

        ('BOTTLE_OPENER_RING', N'Nhẫn Mở Nắp Chai', 'RING', 'COMMON', 'bottle-opener-ring.jpg', 1, 80,
         N'Chiếc nhẫn sắt thô sơ nhưng luôn hữu ích sau một ngày phiêu lưu dài.', 'HP', 150, 'CRIT_RATE', 0.02, NULL, NULL),
        ('MIND_SYNC_HEADSET', N'Tai Nghe Đồng Bộ Tâm Trí', 'RING', 'RARE', 'mind-sync-headset.jpg', 17, 1450,
         N'Tai nghe thần kinh giúp đồng bộ ý niệm và phản ứng với đồng đội.', 'ACCURACY', 0.07, 'CRIT_RATE', 0.05, NULL, NULL),
        ('POCKET_PLANET_RING', N'Nhẫn Hành Tinh Bỏ Túi', 'RING', 'EPIC', 'pocket-planet-ring.jpg', 34, 4300,
         N'Một hành tinh tím thu nhỏ quay trong lòng nhẫn và bẻ cong không gian xung quanh.', 'CRIT_RATE', 0.10, 'MAGIC_PEN', 60, 'HP', 750),
        ('TIME_REVERSAL_WATCH', N'Đồng Hồ Nghịch Chuyển Thời Gian', 'RING', 'MYTHIC', 'time-reversal-watch.jpg', 50, 14800,
         N'Cỗ máy thời gian đeo tay cho phép chủ nhân đi trước đối thủ một nhịp định mệnh.', 'ACCURACY', 0.16, 'DODGE_RATE', 0.12, 'CRIT_RATE', 0.15),

        ('GUARDIAN_ALUMINUM_POT', N'Nồi Nhôm Hộ Mệnh', 'HELMET', 'COMMON', 'guardian-aluminum-pot.jpg', 1, 90,
         N'Chiếc nồi nhôm móp méo được lót vải và tận dụng làm mũ bảo hộ.', 'ARMOR', 30, 'HP', 250, NULL, NULL),
        ('METEOR_PILOT_HELMET', N'Mũ Phi Công Sao Băng', 'HELMET', 'RARE', 'meteor-pilot-helmet.jpg', 19, 1600,
         N'Mũ phi công liên sao với kính chắn xanh và hệ thống dẫn đường thiên thạch.', 'MAGIC_RESIST', 60, 'HP', 700, 'ARMOR', 35),
        ('ROBOT_SHARK_HELMET', N'Đầu Cá Mập Máy', 'HELMET', 'EPIC', 'robot-shark-helmet.jpg', 35, 4500,
         N'Mũ chiến đấu hình cá mập máy, phát điện tím và gây khiếp đảm trên chiến trường.', 'MAGIC_RESIST', 115, 'HP', 1300, 'ARMOR', 80),
        ('GALACTIC_ANTENNA_CROWN', N'Vương Miện Ăng-Ten Thiên Hà', 'HELMET', 'LEGENDARY', 'galactic-antenna-crown.jpg', 46, 9200,
         N'Vương miện thu nhận tín hiệu từ các vì sao và chuyển hóa chúng thành lá chắn.', 'MAGIC_RESIST', 210, 'HP', 2400, 'CRIT_RESIST', 0.10);

    IF EXISTS
    (
        SELECT 1
        FROM @Items i
        LEFT JOIN dbo.HRK_ItemCategories c ON UPPER(c.Code) = i.CategoryCode
        LEFT JOIN dbo.HRK_Rarities r ON UPPER(r.Code) = i.RarityCode OR UPPER(r.Name) = i.RarityCode
        WHERE c.Id IS NULL OR r.Id IS NULL
    )
        THROW 51000, 'One or more required equipment categories or rarities are missing.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM @Items item
        CROSS APPLY
        (
            VALUES (item.Attr1Code), (item.Attr2Code), (item.Attr3Code)
        ) attributeValue(Code)
        LEFT JOIN dbo.HRK_AttributeTypes attributeType ON attributeType.Code = attributeValue.Code
        WHERE attributeValue.Code IS NOT NULL
          AND attributeType.Id IS NULL
    )
        THROW 51000, 'One or more required equipment attribute types are missing.', 1;

    MERGE dbo.HRK_ItemTemplates AS target
    USING
    (
        SELECT i.*, c.Id AS CategoryId, r.Id AS RarityId
        FROM @Items i
        JOIN dbo.HRK_ItemCategories c ON UPPER(c.Code) = i.CategoryCode
        JOIN dbo.HRK_Rarities r ON UPPER(r.Code) = i.RarityCode OR UPPER(r.Name) = i.RarityCode
    ) AS source
    ON target.Code = source.Code
    WHEN MATCHED THEN UPDATE SET
        Name = source.Name,
        CategoryId = source.CategoryId,
        RarityId = source.RarityId,
        ImagePath = '/assets/images/dcs-game/equipments/' + source.FileName,
        LevelReq = source.LevelReq,
        Description = source.Description,
        MetadataJson = N'{"collection":"equipment_v2"}',
        IsStackable = 0,
        MaxStackSize = 1,
        SellPrice = source.SellPrice,
        UpdatedOn = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN INSERT
        (Code, Name, CategoryId, RarityId, ImagePath, LevelReq, Description, MetadataJson,
         IsStackable, MaxStackSize, SellPrice, CreatedOn, UpdatedOn)
    VALUES
        (source.Code, source.Name, source.CategoryId, source.RarityId,
         '/assets/images/dcs-game/equipments/' + source.FileName,
         source.LevelReq, source.Description, N'{"collection":"equipment_v2"}',
         0, 1, source.SellPrice, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Keep template attributes synchronized with this seed definition.
    DELETE existing
    FROM dbo.HRK_ItemTemplateAttributes existing
    JOIN dbo.HRK_ItemTemplates template ON template.Id = existing.ItemTemplateId
    JOIN @Items item ON item.Code = template.Code;

    INSERT INTO dbo.HRK_ItemTemplateAttributes (ItemTemplateId, AttributeTypeId, Value)
    SELECT template.Id, attributeType.Id, attributeValue.Value
    FROM @Items item
    JOIN dbo.HRK_ItemTemplates template ON template.Code = item.Code
    CROSS APPLY
    (
        VALUES
            (item.Attr1Code, item.Attr1Value),
            (item.Attr2Code, item.Attr2Value),
            (item.Attr3Code, item.Attr3Value)
    ) attributeValue(Code, Value)
    JOIN dbo.HRK_AttributeTypes attributeType ON attributeType.Code = attributeValue.Code
    WHERE attributeValue.Code IS NOT NULL;

    -- Grant exactly one active, unequipped copy of every V2 item to PlayerId 1.
    INSERT INTO dbo.HRK_PlayerInventory
        (PlayerId, ItemTemplateId, Count, Enhancement, Stars, IsEquipped, EquippedHeroId,
         IsLocked, IsActive, SlotIndex, CurrentStats, AcquiredOn, UpdatedOn)
    SELECT 1, template.Id, 1, 0, 0, 0, NULL, 0, 1, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()
    FROM dbo.HRK_ItemTemplates template
    JOIN @Items item ON item.Code = template.Code
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.HRK_PlayerInventory inventory
        WHERE inventory.PlayerId = 1
          AND inventory.ItemTemplateId = template.Id
          AND inventory.IsActive = 1
    );

    -- Materialize the same stat snapshot used by normal inventory operations.
    ;WITH CalculatedStats AS
    (
        SELECT inventory.Id AS InventoryId,
               '{' + STRING_AGG(
                   '"' + attributeType.Code + '":' +
                   CAST(
                       CASE
                           WHEN attributeType.IsPercentage = 1
                               THEN ROUND(templateAttribute.Value * (1.0 + inventory.Enhancement * 0.02), 4)
                           ELSE ROUND(
                               templateAttribute.Value
                               * (1.0 + inventory.Enhancement * 0.08)
                               * (1.0 + inventory.Stars * 0.10),
                               2)
                       END AS NVARCHAR(30)),
                   ',') + '}' AS CurrentStats
        FROM dbo.HRK_PlayerInventory inventory
        JOIN dbo.HRK_ItemTemplates template ON template.Id = inventory.ItemTemplateId
        JOIN @Items item ON item.Code = template.Code
        JOIN dbo.HRK_ItemTemplateAttributes templateAttribute ON templateAttribute.ItemTemplateId = template.Id
        JOIN dbo.HRK_AttributeTypes attributeType ON attributeType.Id = templateAttribute.AttributeTypeId
        WHERE inventory.PlayerId = 1
          AND inventory.IsActive = 1
        GROUP BY inventory.Id, inventory.Enhancement, inventory.Stars
    )
    UPDATE inventory
    SET CurrentStats = calculated.CurrentStats,
        UpdatedOn = SYSUTCDATETIME()
    FROM dbo.HRK_PlayerInventory inventory
    JOIN CalculatedStats calculated ON calculated.InventoryId = inventory.Id;

    COMMIT TRANSACTION;

    SELECT template.Id, template.Code, template.Name, category.Code AS Category,
           rarity.Code AS Rarity, template.ImagePath
    FROM dbo.HRK_ItemTemplates template
    JOIN @Items item ON item.Code = template.Code
    JOIN dbo.HRK_ItemCategories category ON category.Id = template.CategoryId
    JOIN dbo.HRK_Rarities rarity ON rarity.Id = template.RarityId
    ORDER BY item.Code;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

/*
  Migration_PlayerFormationSystem.sql
  --------------------------------------------------------------------------------
  DCS Game - Binh Pháp Trận Hình (5 Ô) & Multi-Formation System Migration
  
  Mục tiêu:
  1. Tạo bảng HRK_FormationTemplates (Config trận pháp chung)
  2. Tạo bảng HRK_FormationSlotTemplates (Config 5 slot: Tiền tuyến 1,3,5 & Hậu tuyến 2,4)
  3. Tạo bảng HRK_FormationLevelConfigs (Config cấp độ 1-5, chi phí Vàng, Đá Trận Pháp, bonus chỉ số)
  4. Tạo vật phẩm FORMATION_STONE (Đá Trận Pháp) trong HRK_ItemTemplates
  5. Mở rộng HRK_PlayerFormations (hỗ trợ nhiều preset theo FormationTemplate, cấp độ, IsSelected)
  6. Di chuyển dữ liệu cũ (Main Team -> LUC_DO) an toàn
  7. Seed dữ liệu mẫu: 3 trận pháp, 15 slots, 15 level configs, và đá test cho người chơi
*/

SET XACT_ABORT ON;
BEGIN TRANSACTION;

PRINT '=== [BẮT ĐẦU] Migration_PlayerFormationSystem ===';

-- ---------------------------------------------------------------------------
-- 1. BẢNG HRK_FormationTemplates
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HRK_FormationTemplates' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.HRK_FormationTemplates (
        Id INT IDENTITY(1,1) NOT NULL,
        Code NVARCHAR(50) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        ImagePath NVARCHAR(255) NULL,
        MaxLevel INT NOT NULL CONSTRAINT DF_HRK_FormationTemplates_MaxLevel DEFAULT 5,
        UnlockConditionJson NVARCHAR(MAX) NULL,
        DisplayOrder INT NOT NULL CONSTRAINT DF_HRK_FormationTemplates_DisplayOrder DEFAULT 0,
        IsEnabled BIT NOT NULL CONSTRAINT DF_HRK_FormationTemplates_IsEnabled DEFAULT 1,
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_FormationTemplates_CreatedOn DEFAULT SYSUTCDATETIME(),
        UpdatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_FormationTemplates_UpdatedOn DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_HRK_FormationTemplates PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_HRK_FormationTemplates_Code UNIQUE NONCLUSTERED (Code)
    );
    PRINT '-> Da tao bang dbo.HRK_FormationTemplates.';
END
ELSE
BEGIN
    PRINT '-> Bang dbo.HRK_FormationTemplates da ton tai.';
END

-- ---------------------------------------------------------------------------
-- 2. BẢNG HRK_FormationSlotTemplates
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HRK_FormationSlotTemplates' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.HRK_FormationSlotTemplates (
        Id INT IDENTITY(1,1) NOT NULL,
        FormationTemplateId INT NOT NULL,
        Slot INT NOT NULL,
        RowType NVARCHAR(20) NOT NULL, -- 'FRONT', 'BACK'
        Lane INT NOT NULL,
        DisplayX INT NOT NULL CONSTRAINT DF_HRK_FormationSlotTemplates_DisplayX DEFAULT 0,
        DisplayY INT NOT NULL CONSTRAINT DF_HRK_FormationSlotTemplates_DisplayY DEFAULT 0,
        IsEnabled BIT NOT NULL CONSTRAINT DF_HRK_FormationSlotTemplates_IsEnabled DEFAULT 1,
        CONSTRAINT PK_HRK_FormationSlotTemplates PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_HRK_FormationSlotTemplates_Formation FOREIGN KEY (FormationTemplateId)
            REFERENCES dbo.HRK_FormationTemplates(Id) ON DELETE CASCADE,
        CONSTRAINT UQ_HRK_FormationSlotTemplates_Formation_Slot UNIQUE NONCLUSTERED (FormationTemplateId, Slot)
    );
    PRINT '-> Da tao bang dbo.HRK_FormationSlotTemplates.';
END
ELSE
BEGIN
    PRINT '-> Bang dbo.HRK_FormationSlotTemplates da ton tai.';
END

-- ---------------------------------------------------------------------------
-- 3. SEED VẬT PHẨM ĐÁ TRẬN PHÁP (FORMATION_STONE) VÀO HRK_ItemTemplates
-- ---------------------------------------------------------------------------
DECLARE @MaterialCatId INT = (SELECT TOP 1 Id FROM dbo.HRK_ItemCategories WHERE Code = 'MATERIAL');
IF @MaterialCatId IS NULL
BEGIN
    -- Fallback nếu chưa có category MATERIAL
    SELECT TOP 1 @MaterialCatId = Id FROM dbo.HRK_ItemCategories WHERE IsEquipment = 0 ORDER BY Id;
END

IF NOT EXISTS (SELECT 1 FROM dbo.HRK_ItemTemplates WHERE Code = 'FORMATION_STONE')
BEGIN
    INSERT INTO dbo.HRK_ItemTemplates (
        Code,
        Name,
        CategoryId,
        RarityId,
        ImagePath,
        Description,
        CreatedOn,
        UpdatedOn
    ) VALUES (
        'FORMATION_STONE',
        N'Đá Trận Pháp',
        @MaterialCatId,
        3, -- Rarity: Lam (Rare)
        '/assets/images/dcs-game/items/formation_stone.png',
        N'Đá chứa linh lực cổ xưa, dùng để bồi dưỡng và nâng cấp trận pháp.',
        GETDATE(),
        GETDATE()
    );
    PRINT '-> Da seed vat pham FORMATION_STONE vao HRK_ItemTemplates.';
END
ELSE
BEGIN
    UPDATE dbo.HRK_ItemTemplates
    SET ImagePath = '/assets/images/dcs-game/items/formation_stone.png',
        Name = N'Đá Trận Pháp',
        Description = N'Đá chứa linh lực cổ xưa, dùng để bồi dưỡng và nâng cấp trận pháp.',
        UpdatedOn = GETDATE()
    WHERE Code = 'FORMATION_STONE';
    PRINT '-> Da cap nhat FORMATION_STONE trong HRK_ItemTemplates.';
END

DECLARE @StoneItemTemplateId INT = (SELECT TOP 1 Id FROM dbo.HRK_ItemTemplates WHERE Code = 'FORMATION_STONE');

-- ---------------------------------------------------------------------------
-- 4. BẢNG HRK_FormationLevelConfigs
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HRK_FormationLevelConfigs' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.HRK_FormationLevelConfigs (
        Id INT IDENTITY(1,1) NOT NULL,
        FormationTemplateId INT NOT NULL,
        Level INT NOT NULL,
        GoldCost BIGINT NOT NULL CONSTRAINT DF_HRK_FormationLevelConfigs_GoldCost DEFAULT 0,
        StoneItemTemplateId INT NOT NULL,
        StoneCost INT NOT NULL CONSTRAINT DF_HRK_FormationLevelConfigs_StoneCost DEFAULT 0,
        StatBonusJson NVARCHAR(MAX) NOT NULL,
        CreatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_FormationLevelConfigs_CreatedOn DEFAULT SYSUTCDATETIME(),
        UpdatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_FormationLevelConfigs_UpdatedOn DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_HRK_FormationLevelConfigs PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_HRK_FormationLevelConfigs_Formation FOREIGN KEY (FormationTemplateId)
            REFERENCES dbo.HRK_FormationTemplates(Id) ON DELETE CASCADE,
        CONSTRAINT FK_HRK_FormationLevelConfigs_StoneItem FOREIGN KEY (StoneItemTemplateId)
            REFERENCES dbo.HRK_ItemTemplates(Id),
        CONSTRAINT UQ_HRK_FormationLevelConfigs_Formation_Level UNIQUE NONCLUSTERED (FormationTemplateId, Level)
    );
    PRINT '-> Da tao bang dbo.HRK_FormationLevelConfigs.';
END
ELSE
BEGIN
    PRINT '-> Bang dbo.HRK_FormationLevelConfigs da ton tai.';
END

-- ---------------------------------------------------------------------------
-- 5. SEED 3 FORMATION TEMPLATES
-- ---------------------------------------------------------------------------
MERGE dbo.HRK_FormationTemplates AS target
USING (
    VALUES
    ('LUC_DO', N'Lục Đồ Trận', N'Trận hình cơ bản cân bằng giữa công và thủ, gia tăng hài hòa toàn diện các thuộc tính cho toàn đội.', '/assets/images/dcs-game/formations/luc_do.png', 5, 1, 1),
    ('NGU_HANH', N'Ngũ Hành Trận', N'Trận hình vận dụng sinh khắc ngũ hành, tối ưu hóa sức mạnh tấn công và phòng ngự kiên cố.', '/assets/images/dcs-game/formations/ngu_hanh.png', 5, 2, 1),
    ('PHONG_DUONG', N'Phong Dương Trận', N'Trận hình tập trung tốc độ thần tốc và bộc phá sát thương cực đại, áp đảo địch quân chớp nhoáng.', '/assets/images/dcs-game/formations/phong_duong.png', 5, 3, 1)
) AS source (Code, Name, Description, ImagePath, MaxLevel, DisplayOrder, IsEnabled)
ON target.Code = source.Code
WHEN MATCHED THEN
    UPDATE SET
        target.Name = source.Name,
        target.Description = source.Description,
        target.ImagePath = source.ImagePath,
        target.MaxLevel = source.MaxLevel,
        target.DisplayOrder = source.DisplayOrder,
        target.IsEnabled = source.IsEnabled,
        target.UpdatedOn = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (Code, Name, Description, ImagePath, MaxLevel, DisplayOrder, IsEnabled, CreatedOn, UpdatedOn)
    VALUES (source.Code, source.Name, source.Description, source.ImagePath, source.MaxLevel, source.DisplayOrder, source.IsEnabled, SYSUTCDATETIME(), SYSUTCDATETIME());

PRINT '-> Da seed/merge 3 Formation Templates.';

-- ---------------------------------------------------------------------------
-- 6. SEED 5 SLOTS CHO TỪNG FORMATION TEMPLATE
-- Hậu tuyến: Ô 2, Ô 4 (RowType: BACK)
-- Tiền tuyến: Ô 1, Ô 3, Ô 5 (RowType: FRONT)
-- ---------------------------------------------------------------------------
DECLARE @TmplLucDo INT = (SELECT Id FROM dbo.HRK_FormationTemplates WHERE Code = 'LUC_DO');
DECLARE @TmplNguHanh INT = (SELECT Id FROM dbo.HRK_FormationTemplates WHERE Code = 'NGU_HANH');
DECLARE @TmplPhongDuong INT = (SELECT Id FROM dbo.HRK_FormationTemplates WHERE Code = 'PHONG_DUONG');

MERGE dbo.HRK_FormationSlotTemplates AS target
USING (
    VALUES
    -- LUC_DO
    (@TmplLucDo, 1, 'FRONT', 1, 1, 2, 1),
    (@TmplLucDo, 2, 'BACK',  2, 2, 1, 1),
    (@TmplLucDo, 3, 'FRONT', 3, 3, 2, 1),
    (@TmplLucDo, 4, 'BACK',  4, 4, 1, 1),
    (@TmplLucDo, 5, 'FRONT', 5, 5, 2, 1),

    -- NGU_HANH
    (@TmplNguHanh, 1, 'FRONT', 1, 1, 2, 1),
    (@TmplNguHanh, 2, 'BACK',  2, 2, 1, 1),
    (@TmplNguHanh, 3, 'FRONT', 3, 3, 2, 1),
    (@TmplNguHanh, 4, 'BACK',  4, 4, 1, 1),
    (@TmplNguHanh, 5, 'FRONT', 5, 5, 2, 1),

    -- PHONG_DUONG
    (@TmplPhongDuong, 1, 'FRONT', 1, 1, 2, 1),
    (@TmplPhongDuong, 2, 'BACK',  2, 2, 1, 1),
    (@TmplPhongDuong, 3, 'FRONT', 3, 3, 2, 1),
    (@TmplPhongDuong, 4, 'BACK',  4, 4, 1, 1),
    (@TmplPhongDuong, 5, 'FRONT', 5, 5, 2, 1)
) AS source (FormationTemplateId, Slot, RowType, Lane, DisplayX, DisplayY, IsEnabled)
ON target.FormationTemplateId = source.FormationTemplateId AND target.Slot = source.Slot
WHEN MATCHED THEN
    UPDATE SET
        target.RowType = source.RowType,
        target.Lane = source.Lane,
        target.DisplayX = source.DisplayX,
        target.DisplayY = source.DisplayY,
        target.IsEnabled = source.IsEnabled
WHEN NOT MATCHED THEN
    INSERT (FormationTemplateId, Slot, RowType, Lane, DisplayX, DisplayY, IsEnabled)
    VALUES (source.FormationTemplateId, source.Slot, source.RowType, source.Lane, source.DisplayX, source.DisplayY, source.IsEnabled);

PRINT '-> Da seed 15 slot configs cho 3 formation templates.';

-- ---------------------------------------------------------------------------
-- 7. SEED LEVEL CONFIGS CHO 3 TRẬN PHÁP (Lv.1 -> Lv.5)
-- ---------------------------------------------------------------------------
MERGE dbo.HRK_FormationLevelConfigs AS target
USING (
    VALUES
    -- LUC_DO (Cân bằng: Tất cả chỉ số +2% mỗi cấp)
    (@TmplLucDo, 1, 20000,  @StoneItemTemplateId, 10,  N'{"hpPercent":2.0,"atkPercent":2.0,"defPercent":2.0,"spdPercent":2.0,"magicDamagePercent":2.0,"magicResistancePercent":2.0}'),
    (@TmplLucDo, 2, 50000,  @StoneItemTemplateId, 25,  N'{"hpPercent":4.0,"atkPercent":4.0,"defPercent":4.0,"spdPercent":4.0,"magicDamagePercent":4.0,"magicResistancePercent":4.0}'),
    (@TmplLucDo, 3, 100000, @StoneItemTemplateId, 50,  N'{"hpPercent":6.0,"atkPercent":6.0,"defPercent":6.0,"spdPercent":6.0,"magicDamagePercent":6.0,"magicResistancePercent":6.0}'),
    (@TmplLucDo, 4, 200000, @StoneItemTemplateId, 100, N'{"hpPercent":8.0,"atkPercent":8.0,"defPercent":8.0,"spdPercent":8.0,"magicDamagePercent":8.0,"magicResistancePercent":8.0}'),
    (@TmplLucDo, 5, 0,      @StoneItemTemplateId, 0,   N'{"hpPercent":10.0,"atkPercent":10.0,"defPercent":10.0,"spdPercent":10.0,"magicDamagePercent":10.0,"magicResistancePercent":10.0}'),

    -- NGU_HANH (Thiên về Thủ & Kháng phép: DEF, MRES tăng mạnh)
    (@TmplNguHanh, 1, 20000,  @StoneItemTemplateId, 10,  N'{"hpPercent":2.0,"atkPercent":1.0,"defPercent":3.0,"spdPercent":1.0,"magicDamagePercent":1.0,"magicResistancePercent":3.0}'),
    (@TmplNguHanh, 2, 50000,  @StoneItemTemplateId, 25,  N'{"hpPercent":4.0,"atkPercent":2.0,"defPercent":6.0,"spdPercent":2.0,"magicDamagePercent":2.0,"magicResistancePercent":6.0}'),
    (@TmplNguHanh, 3, 100000, @StoneItemTemplateId, 50,  N'{"hpPercent":6.0,"atkPercent":3.0,"defPercent":9.0,"spdPercent":3.0,"magicDamagePercent":3.0,"magicResistancePercent":9.0}'),
    (@TmplNguHanh, 4, 200000, @StoneItemTemplateId, 100, N'{"hpPercent":8.0,"atkPercent":4.0,"defPercent":12.0,"spdPercent":4.0,"magicDamagePercent":4.0,"magicResistancePercent":12.0}'),
    (@TmplNguHanh, 5, 0,      @StoneItemTemplateId, 0,   N'{"hpPercent":10.0,"atkPercent":5.0,"defPercent":15.0,"spdPercent":5.0,"magicDamagePercent":5.0,"magicResistancePercent":15.0}'),

    -- PHONG_DUONG (Thiên về Công & Tốc độ: ATK, MDMG, SPD tăng mạnh)
    (@TmplPhongDuong, 1, 20000,  @StoneItemTemplateId, 10,  N'{"hpPercent":1.0,"atkPercent":3.0,"defPercent":1.0,"spdPercent":3.0,"magicDamagePercent":3.0,"magicResistancePercent":1.0}'),
    (@TmplPhongDuong, 2, 50000,  @StoneItemTemplateId, 25,  N'{"hpPercent":2.0,"atkPercent":6.0,"defPercent":2.0,"spdPercent":6.0,"magicDamagePercent":6.0,"magicResistancePercent":2.0}'),
    (@TmplPhongDuong, 3, 100000, @StoneItemTemplateId, 50,  N'{"hpPercent":3.0,"atkPercent":9.0,"defPercent":3.0,"spdPercent":9.0,"magicDamagePercent":9.0,"magicResistancePercent":3.0}'),
    (@TmplPhongDuong, 4, 200000, @StoneItemTemplateId, 100, N'{"hpPercent":4.0,"atkPercent":12.0,"defPercent":4.0,"spdPercent":12.0,"magicDamagePercent":12.0,"magicResistancePercent":4.0}'),
    (@TmplPhongDuong, 5, 0,      @StoneItemTemplateId, 0,   N'{"hpPercent":5.0,"atkPercent":15.0,"defPercent":5.0,"spdPercent":5.0,"magicDamagePercent":15.0,"magicResistancePercent":5.0}')
) AS source (FormationTemplateId, Level, GoldCost, StoneItemTemplateId, StoneCost, StatBonusJson)
ON target.FormationTemplateId = source.FormationTemplateId AND target.Level = source.Level
WHEN MATCHED THEN
    UPDATE SET
        target.GoldCost = source.GoldCost,
        target.StoneItemTemplateId = source.StoneItemTemplateId,
        target.StoneCost = source.StoneCost,
        target.StatBonusJson = source.StatBonusJson,
        target.UpdatedOn = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (FormationTemplateId, Level, GoldCost, StoneItemTemplateId, StoneCost, StatBonusJson, CreatedOn, UpdatedOn)
    VALUES (source.FormationTemplateId, source.Level, source.GoldCost, source.StoneItemTemplateId, source.StoneCost, source.StatBonusJson, SYSUTCDATETIME(), SYSUTCDATETIME());

PRINT '-> Da seed 15 level configs cho 3 formation templates.';

-- ---------------------------------------------------------------------------
-- 8. MỞ RỘNG VÀ MIGRATE BẢNG HRK_PlayerFormations
-- ---------------------------------------------------------------------------
-- Thêm cột Id nếu chưa có
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HRK_PlayerFormations') AND name = 'Id')
BEGIN
    ALTER TABLE dbo.HRK_PlayerFormations ADD Id BIGINT IDENTITY(1,1) NOT NULL;
    PRINT '-> Da them cot Id IDENTITY vao HRK_PlayerFormations.';
END

-- Thêm cột FormationTemplateId
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HRK_PlayerFormations') AND name = 'FormationTemplateId')
BEGIN
    ALTER TABLE dbo.HRK_PlayerFormations ADD FormationTemplateId INT NULL;
    PRINT '-> Da them cot FormationTemplateId vao HRK_PlayerFormations.';
END

-- Thêm cột Level
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HRK_PlayerFormations') AND name = 'Level')
BEGIN
    ALTER TABLE dbo.HRK_PlayerFormations ADD Level INT NOT NULL CONSTRAINT DF_HRK_PlayerFormations_Level DEFAULT 1;
    PRINT '-> Da them cot Level vao HRK_PlayerFormations.';
END

-- Thêm cột IsSelected
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HRK_PlayerFormations') AND name = 'IsSelected')
BEGIN
    ALTER TABLE dbo.HRK_PlayerFormations ADD IsSelected BIT NOT NULL CONSTRAINT DF_HRK_PlayerFormations_IsSelected DEFAULT 0;
    PRINT '-> Da them cot IsSelected vao HRK_PlayerFormations.';
END

-- Thêm cột Position5 nếu chưa có
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HRK_PlayerFormations') AND name = 'Position5')
BEGIN
    ALTER TABLE dbo.HRK_PlayerFormations ADD Position5 BIGINT NULL;
    PRINT '-> Da them cot Position5 vao HRK_PlayerFormations.';
END

-- Migrate data cũ: gán FormationTemplateId = LUC_DO và IsSelected = 1 cho các dòng chưa có (chạy qua sp_executesql để tránh lỗi Msg 207 tại thời điểm biên dịch batch)
EXEC sp_executesql N'
    UPDATE dbo.HRK_PlayerFormations
    SET FormationTemplateId = @TmplLucDo,
        IsSelected = 1,
        Level = 1
    WHERE FormationTemplateId IS NULL;',
    N'@TmplLucDo INT',
    @TmplLucDo = @TmplLucDo;

PRINT '-> Da cap nhat du lieu cu sang tran LUC_DO.';

-- Sửa cột FormationTemplateId thành NOT NULL
EXEC('ALTER TABLE dbo.HRK_PlayerFormations ALTER COLUMN FormationTemplateId INT NOT NULL;');
PRINT '-> Da dat cot FormationTemplateId NOT NULL.';

-- Xử lý Primary Key & Unique Constraints:
-- Nếu PK cũ là composite (PlayerId, FormationName), ta drop nó và chuyển sang PK (Id)
-- BẮT BUỘC thực hiện trước khi đổi FormationName sang NULLABLE vì cột đang nằm trong PK cũ
DECLARE @OldPkName NVARCHAR(128);
SELECT @OldPkName = name 
FROM sys.key_constraints 
WHERE parent_object_id = OBJECT_ID('dbo.HRK_PlayerFormations') AND type = 'PK';

IF @OldPkName IS NOT NULL
BEGIN
    -- Kiểm tra xem PK có phải trên cột Id hay không
    IF NOT EXISTS (
        SELECT 1 
        FROM sys.index_columns ic
        JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
        JOIN sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        WHERE i.name = @OldPkName AND c.name = 'Id'
    )
    BEGIN
        EXEC('ALTER TABLE dbo.HRK_PlayerFormations DROP CONSTRAINT ' + @OldPkName + ';');
        PRINT '-> Da drop PK cu (' + @OldPkName + ') tren cot FormationName.';
    END
END

IF NOT EXISTS (
    SELECT 1 
    FROM sys.key_constraints 
    WHERE parent_object_id = OBJECT_ID('dbo.HRK_PlayerFormations') AND type = 'PK'
)
BEGIN
    EXEC('ALTER TABLE dbo.HRK_PlayerFormations ADD CONSTRAINT PK_HRK_PlayerFormations PRIMARY KEY CLUSTERED (Id);');
    PRINT '-> Da tao PK_HRK_PlayerFormations tren cot Id.';
END

-- Sửa cột FormationName thành NULLABLE (sau khi đã gỡ khỏi PK)
EXEC('ALTER TABLE dbo.HRK_PlayerFormations ALTER COLUMN FormationName NVARCHAR(50) NULL;');
PRINT '-> Da sua cot FormationName thanh NULLABLE.';

-- Thêm FK tới HRK_FormationTemplates nếu chưa có
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_HRK_PlayerFormations_Template' AND parent_object_id = OBJECT_ID('dbo.HRK_PlayerFormations'))
BEGIN
    EXEC('ALTER TABLE dbo.HRK_PlayerFormations
        ADD CONSTRAINT FK_HRK_PlayerFormations_Template FOREIGN KEY (FormationTemplateId)
            REFERENCES dbo.HRK_FormationTemplates(Id);');
    PRINT '-> Da tao FK_HRK_PlayerFormations_Template.';
END

-- Thêm Unique constraint: Mỗi player chỉ có 1 record cho mỗi FormationTemplate
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_HRK_PlayerFormations_Player_Template' AND object_id = OBJECT_ID('dbo.HRK_PlayerFormations'))
BEGIN
    EXEC('CREATE UNIQUE NONCLUSTERED INDEX UQ_HRK_PlayerFormations_Player_Template
        ON dbo.HRK_PlayerFormations (PlayerId, FormationTemplateId);');
    PRINT '-> Da tao UNIQUE INDEX UQ_HRK_PlayerFormations_Player_Template.';
END

-- Đảm bảo mỗi player chỉ có đúng 1 formation có IsSelected = 1
EXEC('
;WITH RankedFormations AS (
    SELECT Id, PlayerId, IsSelected,
           ROW_NUMBER() OVER (PARTITION BY PlayerId ORDER BY CASE WHEN IsSelected = 1 THEN 0 ELSE 1 END, Id ASC) AS rn
    FROM dbo.HRK_PlayerFormations
)
UPDATE pf
SET pf.IsSelected = CASE WHEN rf.rn = 1 THEN 1 ELSE 0 END
FROM dbo.HRK_PlayerFormations pf
JOIN RankedFormations rf ON pf.Id = rf.Id;
');

PRINT '-> Da dong bo trang thai IsSelected duy nhat cho moi player.';

-- ---------------------------------------------------------------------------
-- 9. SEED ĐÁ TRẬN PHÁP (200 VIÊN) CHO MỌI NGƯỜI CHƠI ĐỂ TEST
-- ---------------------------------------------------------------------------
INSERT INTO dbo.HRK_PlayerInventories (
    PlayerId,
    ItemTemplateId,
    Count,
    Enhancement,
    Stars,
    IsEquipped,
    EquippedHeroId,
    IsLocked,
    IsActive,
    AcquiredOn,
    UpdatedOn
)
SELECT 
    p.Id,
    @StoneItemTemplateId,
    200,
    0,
    0,
    0,
    NULL,
    0,
    1,
    GETDATE(),
    GETDATE()
FROM dbo.HRK_Players p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.HRK_PlayerInventories inv 
    WHERE inv.PlayerId = p.Id AND inv.ItemTemplateId = @StoneItemTemplateId AND inv.IsActive = 1
);

PRINT '-> Da seed 200 Da Tran Phap cho cac player chua co.';

COMMIT TRANSACTION;
PRINT '=== [HOÀN THÀNH] Migration_PlayerFormationSystem thanh cong! ===';

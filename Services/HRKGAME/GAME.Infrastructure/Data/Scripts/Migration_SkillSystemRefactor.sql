/*
  Migration_SkillSystemRefactor.sql
  --------------------------------------------------------------------------------
  DCS Game - Skill System Refactor (Data-Driven Architecture)
  - Additive migration for backward compatibility
  - Fixes HRK_HeroSkills unique constraint (multiple skills per hero)
  - Normalizes HRK_SkillTemplates with SkillTypeCode, TriggerCode, EnergyCost, ImagePath
  - Creates HRK_SkillTargetTypes, HRK_SkillEffects, HRK_SkillEffectScalings, HRK_SkillEffectStatModifiers
  - Standardizes 18 Effect Types
  - Seeds data-driven effects and scalings for all 26 skills
*/

SET XACT_ABORT ON;
BEGIN TRANSACTION;

PRINT '=== [BẮT ĐẦU] Migration_SkillSystemRefactor ===';

-- ---------------------------------------------------------------------------
-- 1. SỬA INDEX HRK_HeroSkills (Cho phép 1 Hero có nhiều Skill)
-- ---------------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_HRK_HeroSkills_HeroTemplateId'
           AND object_id = OBJECT_ID('dbo.HRK_HeroSkills'))
BEGIN
    DROP INDEX UX_HRK_HeroSkills_HeroTemplateId ON dbo.HRK_HeroSkills;
    PRINT '-> Da xoa index cu sai UX_HRK_HeroSkills_HeroTemplateId.';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_HRK_HeroSkills_HeroTemplateId_SkillId'
               AND object_id = OBJECT_ID('dbo.HRK_HeroSkills'))
BEGIN
    CREATE UNIQUE INDEX UX_HRK_HeroSkills_HeroTemplateId_SkillId
        ON dbo.HRK_HeroSkills(HeroTemplateId, SkillId);
    PRINT '-> Da tao index dung UX_HRK_HeroSkills_HeroTemplateId_SkillId.';
END

-- ---------------------------------------------------------------------------
-- 2. CẬP NHẬT CỘT MỚI CHO HRK_SkillTemplates (Additive)
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HRK_SkillTemplates') AND name = 'ImagePath')
BEGIN
    ALTER TABLE dbo.HRK_SkillTemplates ADD ImagePath NVARCHAR(255) NULL;
    PRINT '-> Da them cot ImagePath vao HRK_SkillTemplates.';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HRK_SkillTemplates') AND name = 'SkillTypeCode')
BEGIN
    ALTER TABLE dbo.HRK_SkillTemplates ADD SkillTypeCode NVARCHAR(30) NOT NULL CONSTRAINT DF_HRK_SkillTemplates_SkillTypeCode DEFAULT 'ENERGY';
    PRINT '-> Da them cot SkillTypeCode vao HRK_SkillTemplates.';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HRK_SkillTemplates') AND name = 'TriggerCode')
BEGIN
    ALTER TABLE dbo.HRK_SkillTemplates ADD TriggerCode NVARCHAR(30) NOT NULL CONSTRAINT DF_HRK_SkillTemplates_TriggerCode DEFAULT 'MANUAL_ENERGY_FULL';
    PRINT '-> Da them cot TriggerCode vao HRK_SkillTemplates.';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HRK_SkillTemplates') AND name = 'EnergyCost')
BEGIN
    ALTER TABLE dbo.HRK_SkillTemplates ADD EnergyCost INT NOT NULL CONSTRAINT DF_HRK_SkillTemplates_EnergyCost DEFAULT 100;
    PRINT '-> Da them cot EnergyCost vao HRK_SkillTemplates.';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HRK_SkillTemplates') AND name = 'DisplayOrder')
BEGIN
    ALTER TABLE dbo.HRK_SkillTemplates ADD DisplayOrder INT NOT NULL CONSTRAINT DF_HRK_SkillTemplates_DisplayOrder DEFAULT 0;
    PRINT '-> Da them cot DisplayOrder vao HRK_SkillTemplates.';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HRK_SkillTemplates') AND name = 'IsActive')
BEGIN
    ALTER TABLE dbo.HRK_SkillTemplates ADD IsActive BIT NOT NULL CONSTRAINT DF_HRK_SkillTemplates_IsActive DEFAULT 1;
    PRINT '-> Da them cot IsActive vao HRK_SkillTemplates.';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HRK_SkillTemplates') AND name = 'UpdatedOn')
BEGIN
    ALTER TABLE dbo.HRK_SkillTemplates ADD UpdatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_SkillTemplates_UpdatedOn DEFAULT SYSUTCDATETIME();
    PRINT '-> Da them cot UpdatedOn vao HRK_SkillTemplates.';
END

-- ---------------------------------------------------------------------------
-- 3. TẠO BẢNG HRK_SkillTargetTypes
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HRK_SkillTargetTypes' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.HRK_SkillTargetTypes
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Code NVARCHAR(50) NOT NULL UNIQUE,
        Name NVARCHAR(100) NOT NULL,
        TargetSide NVARCHAR(20) NOT NULL,
        SelectionRule NVARCHAR(50) NOT NULL,
        DisplayOrder INT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedOn DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedOn DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    PRINT '-> Da tao bang HRK_SkillTargetTypes.';
END

-- Seed 12 target types
DECLARE @TargetTypes TABLE (
    Code NVARCHAR(50),
    Name NVARCHAR(100),
    TargetSide NVARCHAR(20),
    SelectionRule NVARCHAR(50),
    DisplayOrder INT
);

INSERT INTO @TargetTypes (Code, Name, TargetSide, SelectionRule, DisplayOrder) VALUES
('SELF',                     N'Bản thân',                 'SELF',  'SELF',               1),
('ENEMY_SINGLE',             N'Đơn mục tiêu địch',        'ENEMY', 'SINGLE',             2),
('ENEMY_ALL',                N'Toàn bộ kẻ địch',          'ENEMY', 'ALL',                3),
('ENEMY_FRONT_ROW',          N'Hàng trước địch',          'ENEMY', 'FRONT_ROW',          4),
('ENEMY_BACK_ROW',           N'Hàng sau địch',            'ENEMY', 'BACK_ROW',           5),
('ENEMY_RANDOM',             N'1 địch ngẫu nhiên',        'ENEMY', 'RANDOM',             6),
('ENEMY_RANDOM_4',           N'4 địch ngẫu nhiên',        'ENEMY', 'RANDOM_4',           7),
('ENEMY_SAME_LANE_BACK_ROW', N'Hàng sau cùng làn địch',   'ENEMY', 'SAME_LANE_BACK_ROW', 8),
('ALLY_SINGLE',              N'1 đồng minh',              'ALLY',  'SINGLE',             9),
('ALLY_ALL',                 N'Toàn bộ đồng đội',         'ALLY',  'ALL',                10),
('ALLY_LOWEST_HP',           N'Đồng minh thấp máu nhất',  'ALLY',  'LOWEST_HP',          11),
('ALLY_RANDOM_2',            N'2 đồng minh ngẫu nhiên',   'ALLY',  'RANDOM_2',           12);

MERGE dbo.HRK_SkillTargetTypes AS target
USING @TargetTypes AS source
ON target.Code = source.Code
WHEN MATCHED THEN
    UPDATE SET 
        target.Name = source.Name,
        target.TargetSide = source.TargetSide,
        target.SelectionRule = source.SelectionRule,
        target.DisplayOrder = source.DisplayOrder,
        target.UpdatedOn = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (Code, Name, TargetSide, SelectionRule, DisplayOrder, IsActive, CreatedOn, UpdatedOn)
    VALUES (source.Code, source.Name, source.TargetSide, source.SelectionRule, source.DisplayOrder, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

PRINT '-> Da seed/merge HRK_SkillTargetTypes.';

-- ---------------------------------------------------------------------------
-- 4. CẬP NHẬT VÀ CHUẨN HÓA HRK_SkillEffectTypes
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HRK_SkillEffectTypes') AND name = 'EffectGroup')
BEGIN
    ALTER TABLE dbo.HRK_SkillEffectTypes ADD EffectGroup NVARCHAR(30) NOT NULL CONSTRAINT DF_HRK_SkillEffectTypes_EffectGroup DEFAULT 'SPECIAL';
    PRINT '-> Da them cot EffectGroup vao HRK_SkillEffectTypes.';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HRK_SkillEffectTypes') AND name = 'IsBeneficial')
BEGIN
    ALTER TABLE dbo.HRK_SkillEffectTypes ADD IsBeneficial BIT NOT NULL CONSTRAINT DF_HRK_SkillEffectTypes_IsBeneficial DEFAULT 0;
    PRINT '-> Da them cot IsBeneficial vao HRK_SkillEffectTypes.';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HRK_SkillEffectTypes') AND name = 'IsStackable')
BEGIN
    ALTER TABLE dbo.HRK_SkillEffectTypes ADD IsStackable BIT NOT NULL CONSTRAINT DF_HRK_SkillEffectTypes_IsStackable DEFAULT 0;
    PRINT '-> Da them cot IsStackable vao HRK_SkillEffectTypes.';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HRK_SkillEffectTypes') AND name = 'DefaultStackLimit')
BEGIN
    ALTER TABLE dbo.HRK_SkillEffectTypes ADD DefaultStackLimit INT NULL;
    PRINT '-> Da them cot DefaultStackLimit vao HRK_SkillEffectTypes.';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HRK_SkillEffectTypes') AND name = 'Description')
BEGIN
    ALTER TABLE dbo.HRK_SkillEffectTypes ADD Description NVARCHAR(500) NULL;
    PRINT '-> Da them cot Description vao HRK_SkillEffectTypes.';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HRK_SkillEffectTypes') AND name = 'UpdatedOn')
BEGIN
    ALTER TABLE dbo.HRK_SkillEffectTypes ADD UpdatedOn DATETIME2 NOT NULL CONSTRAINT DF_HRK_SkillEffectTypes_UpdatedOn DEFAULT SYSUTCDATETIME();
    PRINT '-> Da them cot UpdatedOn vao HRK_SkillEffectTypes.';
END

-- Seed 18 standardized Effect Types via dynamic SQL to avoid compile-time column binding error
EXEC sp_executesql N'
    DECLARE @EffectTypes TABLE (
        Code NVARCHAR(50),
        Name NVARCHAR(100),
        EffectGroup NVARCHAR(30),
        IsBeneficial BIT,
        IsStackable BIT,
        DefaultStackLimit INT,
        Description NVARCHAR(500)
    );

    INSERT INTO @EffectTypes (Code, Name, EffectGroup, IsBeneficial, IsStackable, DefaultStackLimit, Description) VALUES
    (''DAMAGE'',             N''Gây sát thương'',              ''DAMAGE'',   0, 0, NULL, N''Gây sát thương trực tiếp lên mục tiêu''),
    (''HEAL'',               N''Hồi phục sinh lực'',           ''HEAL'',     1, 0, NULL, N''Hồi phục máu cho bản thân hoặc đồng đội''),
    (''SHIELD'',             N''Tạo lá chắn'',                 ''DEFENSE'',  1, 1, 3,    N''Tạo lớp khiên hấp thụ sát thương''),
    (''STAT_BUFF'',          N''Gia tăng thuộc tính'',         ''BUFF'',     1, 1, 5,    N''Tăng cường các thuộc tính cơ bản''),
    (''STAT_DEBUFF'',        N''Giảm trừ thuộc tính'',         ''DEBUFF'',   0, 1, 5,    N''Giảm trừ các thuộc tính của mục tiêu''),
    (''STUN'',               N''Làm choáng'',                  ''CONTROL'',  0, 0, 1,    N''Mục tiêu không thể hành động trong thời gian hiệu lực''),
    (''FREEZE'',             N''Đóng băng'',                   ''CONTROL'',  0, 0, 1,    N''Đóng băng mục tiêu, không thể hành động''),
    (''SILENCE'',            N''Câm lặng'',                    ''CONTROL'',  0, 0, 1,    N''Mục tiêu không thể tung kỹ năng nộ''),
    (''TAUNT'',              N''Khiêu khích'',                 ''CONTROL'',  0, 0, 1,    N''Buộc mục tiêu phải tấn công người khiêu khích''),
    (''BURN'',               N''Thiêu đốt'',                   ''DOT'',      0, 1, 5,    N''Gây sát thương theo thời gian mỗi lượt''),
    (''POISON'',             N''Trúng độc'',                   ''DOT'',      0, 1, 5,    N''Mục tiêu chịu sát thương độc theo thời gian''),
    (''MARK'',               N''Đánh dấu'',                    ''DEBUFF'',   0, 0, 1,    N''Đánh dấu mục tiêu nhận thêm sát thương''),
    (''DAMAGE_REDUCTION'',   N''Giảm sát thương nhận vào'',    ''DEFENSE'',  1, 0, 1,    N''Giảm tỷ lệ % sát thương gánh chịu''),
    (''DAMAGE_REFLECTION'',  N''Phản sát thương'',             ''DEFENSE'',  1, 0, 1,    N''Phản lại một phần sát thương nhận vào cho kẻ tấn công''),
    (''POSITION_SWAP'',      N''Hoán đổi vị trí'',             ''SPECIAL'',  0, 0, NULL, N''Hoán đổi vị trí của các mục tiêu trên chiến trường''),
    (''ENERGY_GAIN'',        N''Hồi năng lượng'',              ''RESOURCE'', 1, 0, NULL, N''Gia tăng năng lượng cho tướng''),
    (''ENERGY_DRAIN'',       N''Hút năng lượng'',              ''RESOURCE'', 0, 0, NULL, N''Làm giảm năng lượng của đối thủ''),
    (''REVIVE'',             N''Hồi sinh'',                    ''SPECIAL'',  1, 0, NULL, N''Hồi sinh đồng đội đã hy sinh'');

    MERGE dbo.HRK_SkillEffectTypes AS target
    USING @EffectTypes AS source
    ON target.Code = source.Code
    WHEN MATCHED THEN
        UPDATE SET
            target.Name = source.Name,
            target.EffectGroup = source.EffectGroup,
            target.IsBeneficial = source.IsBeneficial,
            target.IsStackable = source.IsStackable,
            target.DefaultStackLimit = source.DefaultStackLimit,
            target.Description = source.Description,
            target.UpdatedOn = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (Code, Name, EffectGroup, IsBeneficial, IsStackable, DefaultStackLimit, Description, IsDebuff, CreatedOn, UpdatedOn)
        VALUES (source.Code, source.Name, source.EffectGroup, source.IsBeneficial, source.IsStackable, source.DefaultStackLimit, source.Description,
                CASE WHEN source.IsBeneficial = 1 THEN 0 ELSE 1 END, SYSUTCDATETIME(), SYSUTCDATETIME());
';

PRINT '-> Da seed/merge HRK_SkillEffectTypes.';

-- ---------------------------------------------------------------------------
-- 5. TẠO BẢNG HRK_SkillEffects
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HRK_SkillEffects' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.HRK_SkillEffects
    (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        SkillId NVARCHAR(100) NOT NULL,
        EffectTypeId INT NOT NULL,
        TargetTypeId INT NOT NULL,
        DamageSchoolCode NVARCHAR(20) NULL, -- PHYSICAL, MAGIC, TRUE
        BaseValue DECIMAL(18,4) NOT NULL DEFAULT 0,
        DurationTurns INT NULL,
        ChancePercent DECIMAL(5,2) NOT NULL DEFAULT 100,
        MaxStacks INT NULL,
        DisplayOrder INT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedOn DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedOn DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_HRK_SkillEffects_Skill FOREIGN KEY (SkillId) REFERENCES dbo.HRK_SkillTemplates(Id) ON DELETE CASCADE,
        CONSTRAINT FK_HRK_SkillEffects_EffectType FOREIGN KEY (EffectTypeId) REFERENCES dbo.HRK_SkillEffectTypes(Id),
        CONSTRAINT FK_HRK_SkillEffects_TargetType FOREIGN KEY (TargetTypeId) REFERENCES dbo.HRK_SkillTargetTypes(Id)
    );
    PRINT '-> Da tao bang HRK_SkillEffects.';
END

-- ---------------------------------------------------------------------------
-- 6. TẠO BẢNG HRK_SkillEffectScalings
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HRK_SkillEffectScalings' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.HRK_SkillEffectScalings
    (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        SkillEffectId BIGINT NOT NULL,
        AttributeTypeId INT NOT NULL,
        Coefficient DECIMAL(18,6) NOT NULL DEFAULT 0,
        FlatValue DECIMAL(18,4) NOT NULL DEFAULT 0,
        CreatedOn DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedOn DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_HRK_SkillEffectScalings_Effect_Attribute UNIQUE (SkillEffectId, AttributeTypeId),
        CONSTRAINT FK_HRK_SkillEffectScalings_Effect FOREIGN KEY (SkillEffectId) REFERENCES dbo.HRK_SkillEffects(Id) ON DELETE CASCADE,
        CONSTRAINT FK_HRK_SkillEffectScalings_AttributeType FOREIGN KEY (AttributeTypeId) REFERENCES dbo.HRK_AttributeTypes(Id)
    );
    PRINT '-> Da tao bang HRK_SkillEffectScalings.';
END

-- ---------------------------------------------------------------------------
-- 7. TẠO BẢNG HRK_SkillEffectStatModifiers
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HRK_SkillEffectStatModifiers' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.HRK_SkillEffectStatModifiers
    (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        SkillEffectId BIGINT NOT NULL,
        AttributeTypeId INT NOT NULL,
        ValueType NVARCHAR(20) NOT NULL, -- FLAT, PERCENT
        Value DECIMAL(18,4) NOT NULL,
        CreatedOn DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedOn DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_HRK_SkillEffectStatModifiers_Effect FOREIGN KEY (SkillEffectId) REFERENCES dbo.HRK_SkillEffects(Id) ON DELETE CASCADE,
        CONSTRAINT FK_HRK_SkillEffectStatModifiers_AttributeType FOREIGN KEY (AttributeTypeId) REFERENCES dbo.HRK_AttributeTypes(Id)
    );
    PRINT '-> Da tao bang HRK_SkillEffectStatModifiers.';
END

-- ---------------------------------------------------------------------------
-- 8. CẬP NHẬT METADATA 26 KỸ NĂNG HIỆN CÓ TRÊN HRK_SkillTemplates
-- ---------------------------------------------------------------------------
EXEC sp_executesql N'
    -- Đòn đánh thường
    UPDATE dbo.HRK_SkillTemplates
    SET SkillTypeCode = ''NORMAL'',
        TriggerCode = ''ON_ATTACK'',
        EnergyCost = 0,
        DisplayOrder = 1,
        UpdatedOn = SYSUTCDATETIME()
    WHERE Id IN (''NORMAL_ATTACK'', ''SLASH'', ''AUTOMATION_TEST'');

    -- Các kỹ năng nộ / tiêu hao năng lượng (EnergyCost = 100)
    UPDATE dbo.HRK_SkillTemplates
    SET SkillTypeCode = ''ENERGY'',
        TriggerCode = ''MANUAL_ENERGY_FULL'',
        EnergyCost = 100,
        DisplayOrder = 2,
        UpdatedOn = SYSUTCDATETIME()
    WHERE Id NOT IN (''NORMAL_ATTACK'', ''SLASH'', ''AUTOMATION_TEST'');
';

PRINT '-> Da cap nhat SkillTypeCode & EnergyCost cho HRK_SkillTemplates.';

-- ---------------------------------------------------------------------------
-- 9. SEED CHI TIẾT HIỆU ỨNG VÀ SCALING CHO TOÀN BỘ 26 KỸ NĂNG
-- ---------------------------------------------------------------------------
-- Dọn dẹp dữ liệu effects cũ (nếu chạy lại migration) để idempotent
DELETE FROM dbo.HRK_SkillEffectStatModifiers;
DELETE FROM dbo.HRK_SkillEffectScalings;
DELETE FROM dbo.HRK_SkillEffects;

-- Lấy IDs của Target Types
DECLARE @T_SELF INT = (SELECT Id FROM dbo.HRK_SkillTargetTypes WHERE Code = 'SELF');
DECLARE @T_SINGLE_ENEMY INT = (SELECT Id FROM dbo.HRK_SkillTargetTypes WHERE Code = 'ENEMY_SINGLE');
DECLARE @T_ALL_ENEMY INT = (SELECT Id FROM dbo.HRK_SkillTargetTypes WHERE Code = 'ENEMY_ALL');
DECLARE @T_FRONT_ROW_ENEMY INT = (SELECT Id FROM dbo.HRK_SkillTargetTypes WHERE Code = 'ENEMY_FRONT_ROW');
DECLARE @T_BACK_ROW_ENEMY INT = (SELECT Id FROM dbo.HRK_SkillTargetTypes WHERE Code = 'ENEMY_BACK_ROW');
DECLARE @T_RANDOM_ENEMY INT = (SELECT Id FROM dbo.HRK_SkillTargetTypes WHERE Code = 'ENEMY_RANDOM');
DECLARE @T_RANDOM_4_ENEMY INT = (SELECT Id FROM dbo.HRK_SkillTargetTypes WHERE Code = 'ENEMY_RANDOM_4');
DECLARE @T_SAME_LANE_BACK INT = (SELECT Id FROM dbo.HRK_SkillTargetTypes WHERE Code = 'ENEMY_SAME_LANE_BACK_ROW');
DECLARE @T_ALL_ALLY INT = (SELECT Id FROM dbo.HRK_SkillTargetTypes WHERE Code = 'ALLY_ALL');
DECLARE @T_RANDOM_2_ALLY INT = (SELECT Id FROM dbo.HRK_SkillTargetTypes WHERE Code = 'ALLY_RANDOM_2');

-- Lấy IDs của Effect Types
DECLARE @E_DAMAGE INT = (SELECT Id FROM dbo.HRK_SkillEffectTypes WHERE Code = 'DAMAGE');
DECLARE @E_HEAL INT = (SELECT Id FROM dbo.HRK_SkillEffectTypes WHERE Code = 'HEAL');
DECLARE @E_SHIELD INT = (SELECT Id FROM dbo.HRK_SkillEffectTypes WHERE Code = 'SHIELD');
DECLARE @E_STAT_BUFF INT = (SELECT Id FROM dbo.HRK_SkillEffectTypes WHERE Code = 'STAT_BUFF');
DECLARE @E_STAT_DEBUFF INT = (SELECT Id FROM dbo.HRK_SkillEffectTypes WHERE Code = 'STAT_DEBUFF');
DECLARE @E_STUN INT = (SELECT Id FROM dbo.HRK_SkillEffectTypes WHERE Code = 'STUN');
DECLARE @E_SILENCE INT = (SELECT Id FROM dbo.HRK_SkillEffectTypes WHERE Code = 'SILENCE');
DECLARE @E_TAUNT INT = (SELECT Id FROM dbo.HRK_SkillEffectTypes WHERE Code = 'TAUNT');
DECLARE @E_BURN INT = (SELECT Id FROM dbo.HRK_SkillEffectTypes WHERE Code = 'BURN');
DECLARE @E_MARK INT = (SELECT Id FROM dbo.HRK_SkillEffectTypes WHERE Code = 'MARK');
DECLARE @E_DAMAGE_REDUCTION INT = (SELECT Id FROM dbo.HRK_SkillEffectTypes WHERE Code = 'DAMAGE_REDUCTION');
DECLARE @E_DAMAGE_REFLECTION INT = (SELECT Id FROM dbo.HRK_SkillEffectTypes WHERE Code = 'DAMAGE_REFLECTION');
DECLARE @E_POSITION_SWAP INT = (SELECT Id FROM dbo.HRK_SkillEffectTypes WHERE Code = 'POSITION_SWAP');

-- Lấy IDs của Attribute Types
DECLARE @A_HP INT = (SELECT Id FROM dbo.HRK_AttributeTypes WHERE Code = 'HP');
DECLARE @A_ATK INT = (SELECT Id FROM dbo.HRK_AttributeTypes WHERE Code = 'ATK');
DECLARE @A_DEF INT = (SELECT Id FROM dbo.HRK_AttributeTypes WHERE Code = 'DEF');
DECLARE @A_SPD INT = (SELECT Id FROM dbo.HRK_AttributeTypes WHERE Code = 'SPD');
DECLARE @A_MAGIC_DMG INT = (SELECT Id FROM dbo.HRK_AttributeTypes WHERE Code = 'MAGIC_DAMAGE');
DECLARE @A_MAGIC_RES INT = (SELECT Id FROM dbo.HRK_AttributeTypes WHERE Code = 'MAGIC_RESISTANCE');

DECLARE @NewEffectId BIGINT;

-- 1. NORMAL_ATTACK: Đánh thường vật lý đơn mục tiêu (ATK x 1.0)
IF EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'NORMAL_ATTACK')
BEGIN
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DisplayOrder)
    VALUES ('NORMAL_ATTACK', @E_DAMAGE, @T_SINGLE_ENEMY, 'PHYSICAL', 0, 1);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue)
    VALUES (@NewEffectId, @A_ATK, 1.000000, 0);
END

-- 2. SLASH: Chém thường (ATK x 1.0)
IF EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'SLASH')
BEGIN
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DisplayOrder)
    VALUES ('SLASH', @E_DAMAGE, @T_SINGLE_ENEMY, 'PHYSICAL', 0, 1);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue)
    VALUES (@NewEffectId, @A_ATK, 1.000000, 0);
END

-- 3. AUTOMATION_TEST: Kiểm thử tự động (ATK x 1.20)
IF EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'AUTOMATION_TEST')
BEGIN
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DisplayOrder)
    VALUES ('AUTOMATION_TEST', @E_DAMAGE, @T_SINGLE_ENEMY, 'PHYSICAL', 0, 1);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue)
    VALUES (@NewEffectId, @A_ATK, 1.200000, 0);
END

-- 4. HEAVY_SLASH: Chém Deadline (ATK x 1.40)
IF EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'HEAVY_SLASH')
BEGIN
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DisplayOrder)
    VALUES ('HEAVY_SLASH', @E_DAMAGE, @T_SINGLE_ENEMY, 'PHYSICAL', 0, 1);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue)
    VALUES (@NewEffectId, @A_ATK, 1.400000, 0);
END

-- 5. FIREBALL: Hỏa cầu sếp phạt (MAGIC_DAMAGE x 1.50 + Thiêu đốt)
IF EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'FIREBALL')
BEGIN
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DisplayOrder)
    VALUES ('FIREBALL', @E_DAMAGE, @T_SINGLE_ENEMY, 'MAGIC', 0, 1);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue)
    VALUES (@NewEffectId, @A_MAGIC_DMG, 1.500000, 0);

    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, BaseValue, DurationTurns, DisplayOrder)
    VALUES ('FIREBALL', @E_BURN, @T_SINGLE_ENEMY, 50, 2, 2);
END

-- 6. LIGHTNING_STRIKE: Sét đánh khẩn cấp (MAGIC_DAMAGE x 1.40 + Choáng 30%)
IF EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'LIGHTNING_STRIKE')
BEGIN
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DisplayOrder)
    VALUES ('LIGHTNING_STRIKE', @E_DAMAGE, @T_SINGLE_ENEMY, 'MAGIC', 0, 1);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue)
    VALUES (@NewEffectId, @A_MAGIC_DMG, 1.400000, 0);

    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DurationTurns, ChancePercent, DisplayOrder)
    VALUES ('LIGHTNING_STRIKE', @E_STUN, @T_SINGLE_ENEMY, 1, 30.00, 2);
END

-- 7. ULTIMATE_SIXPACK: Nộ Long Sáu Múi (ATK x 2.50 + Buff ATK +20%)
IF EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'ULTIMATE_SIXPACK')
BEGIN
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DisplayOrder)
    VALUES ('ULTIMATE_SIXPACK', @E_DAMAGE, @T_SINGLE_ENEMY, 'PHYSICAL', 0, 1);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue)
    VALUES (@NewEffectId, @A_ATK, 2.500000, 0);

    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DurationTurns, DisplayOrder)
    VALUES ('ULTIMATE_SIXPACK', @E_STAT_BUFF, @T_SELF, 2, 2);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectStatModifiers (SkillEffectId, AttributeTypeId, ValueType, Value)
    VALUES (@NewEffectId, @A_ATK, 'PERCENT', 20.0000);
END

-- 8. CRITICAL_BUG: Bug Nghiêm Trọng (MAGIC_DAMAGE x 1.30 + Giảm 20% DEF)
IF EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'CRITICAL_BUG')
BEGIN
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DisplayOrder)
    VALUES ('CRITICAL_BUG', @E_DAMAGE, @T_SINGLE_ENEMY, 'MAGIC', 0, 1);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue)
    VALUES (@NewEffectId, @A_MAGIC_DMG, 1.300000, 0);

    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DurationTurns, DisplayOrder)
    VALUES ('CRITICAL_BUG', @E_STAT_DEBUFF, @T_SINGLE_ENEMY, 2, 2);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectStatModifiers (SkillEffectId, AttributeTypeId, ValueType, Value)
    VALUES (@NewEffectId, @A_DEF, 'PERCENT', -20.0000);
END

-- 9. SWORD_DANCE: Vũ Điệu Chuẩn Men (ATK x 2.20)
IF EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'SWORD_DANCE')
BEGIN
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DisplayOrder)
    VALUES ('SWORD_DANCE', @E_DAMAGE, @T_SINGLE_ENEMY, 'PHYSICAL', 0, 1);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue)
    VALUES (@NewEffectId, @A_ATK, 2.200000, 0);
END

-- 10. CHANGE_REQUIREMENT: Đổi Yêu Cầu Gấp (MAGIC_DAMAGE x 1.50 + Giảm 20% SPD)
IF EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'CHANGE_REQUIREMENT')
BEGIN
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DisplayOrder)
    VALUES ('CHANGE_REQUIREMENT', @E_DAMAGE, @T_SINGLE_ENEMY, 'MAGIC', 0, 1);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue)
    VALUES (@NewEffectId, @A_MAGIC_DMG, 1.500000, 0);

    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DurationTurns, DisplayOrder)
    VALUES ('CHANGE_REQUIREMENT', @E_STAT_DEBUFF, @T_SINGLE_ENEMY, 2, 2);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectStatModifiers (SkillEffectId, AttributeTypeId, ValueType, Value)
    VALUES (@NewEffectId, @A_SPD, 'PERCENT', -20.0000);
END

-- 11. REFACTOR_CODE: Tái Cấu Trúc Đẹp (MAGIC_DAMAGE x 1.50 + Buff 25% MAGIC_DAMAGE)
IF EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'REFACTOR_CODE')
BEGIN
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DisplayOrder)
    VALUES ('REFACTOR_CODE', @E_DAMAGE, @T_SINGLE_ENEMY, 'MAGIC', 0, 1);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue)
    VALUES (@NewEffectId, @A_MAGIC_DMG, 1.500000, 0);

    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DurationTurns, DisplayOrder)
    VALUES ('REFACTOR_CODE', @E_STAT_BUFF, @T_SELF, 2, 2);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectStatModifiers (SkillEffectId, AttributeTypeId, ValueType, Value)
    VALUES (@NewEffectId, @A_MAGIC_DMG, 'PERCENT', 25.0000);
END

-- 12. NULL_POINTER: Lỗi Con Trỏ Null (MAGIC_DAMAGE x 1.30 + Câm Lặng 50%)
IF EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'NULL_POINTER')
BEGIN
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DisplayOrder)
    VALUES ('NULL_POINTER', @E_DAMAGE, @T_SINGLE_ENEMY, 'MAGIC', 0, 1);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue)
    VALUES (@NewEffectId, @A_MAGIC_DMG, 1.300000, 0);

    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DurationTurns, ChancePercent, DisplayOrder)
    VALUES ('NULL_POINTER', @E_SILENCE, @T_SINGLE_ENEMY, 2, 50.00, 2);
END

-- 13. COMPLAIN: Phàn Nàn Giờ Chót (MAGIC_DAMAGE x 1.10 + Giảm 15% ATK)
IF EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'COMPLAIN')
BEGIN
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DisplayOrder)
    VALUES ('COMPLAIN', @E_DAMAGE, @T_SINGLE_ENEMY, 'MAGIC', 0, 1);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue)
    VALUES (@NewEffectId, @A_MAGIC_DMG, 1.100000, 0);

    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DurationTurns, DisplayOrder)
    VALUES ('COMPLAIN', @E_STAT_DEBUFF, @T_SINGLE_ENEMY, 2, 2);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectStatModifiers (SkillEffectId, AttributeTypeId, ValueType, Value)
    VALUES (@NewEffectId, @A_ATK, 'PERCENT', -15.0000);
END

-- 14. DEPLOY_PROD: Lên Prod Bảnh Tỏn (ATK x 2.50)
IF EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'DEPLOY_PROD')
BEGIN
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DisplayOrder)
    VALUES ('DEPLOY_PROD', @E_DAMAGE, @T_SINGLE_ENEMY, 'PHYSICAL', 0, 1);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue)
    VALUES (@NewEffectId, @A_ATK, 2.500000, 0);
END

-- 15. STACK_OVERFLOW: Tràn Bộ Đệm (MAGIC_DAMAGE x 1.20 + Giảm 30% DEF)
IF EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'STACK_OVERFLOW')
BEGIN
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DisplayOrder)
    VALUES ('STACK_OVERFLOW', @E_DAMAGE, @T_SINGLE_ENEMY, 'MAGIC', 0, 1);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue)
    VALUES (@NewEffectId, @A_MAGIC_DMG, 1.200000, 0);

    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DurationTurns, DisplayOrder)
    VALUES ('STACK_OVERFLOW', @E_STAT_DEBUFF, @T_SINGLE_ENEMY, 2, 2);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectStatModifiers (SkillEffectId, AttributeTypeId, ValueType, Value)
    VALUES (@NewEffectId, @A_DEF, 'PERCENT', -30.0000);
END

-- 16. CLOSE_JIRA: Đóng Task Jira! (Sát thương chuẩn TRUE: ATK x 3.00)
IF EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'CLOSE_JIRA')
BEGIN
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DisplayOrder)
    VALUES ('CLOSE_JIRA', @E_DAMAGE, @T_SINGLE_ENEMY, 'TRUE', 0, 1);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue)
    VALUES (@NewEffectId, @A_ATK, 3.000000, 0);
END

-- 17. HEAVENLY_JUDGMENT: Phán Quyết Sấm Sét Cởi Trần (MAGIC_DAMAGE x 3.50 lên toàn bộ kẻ địch)
IF EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'HEAVENLY_JUDGMENT')
BEGIN
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DisplayOrder)
    VALUES ('HEAVENLY_JUDGMENT', @E_DAMAGE, @T_ALL_ENEMY, 'MAGIC', 0, 1);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue)
    VALUES (@NewEffectId, @A_MAGIC_DMG, 3.500000, 0);
END

-- 18. VAX_A_MILLION_SANITZATION: Pháo Quang Phổ Tiệt Trùng (MAGIC_DAMAGE x 2.80 lên toàn bộ kẻ địch)
IF EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'VAX_A_MILLION_SANITZATION')
BEGIN
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DisplayOrder)
    VALUES ('VAX_A_MILLION_SANITZATION', @E_DAMAGE, @T_ALL_ENEMY, 'MAGIC', 0, 1);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue)
    VALUES (@NewEffectId, @A_MAGIC_DMG, 2.800000, 0);
END

-- 19. RICARDO_MILOS: Ricardo Milos! (ATK x 2.50 lên hàng sau cùng làn + Choáng 1 lượt)
IF EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'RICARDO_MILOS')
BEGIN
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DisplayOrder)
    VALUES ('RICARDO_MILOS', @E_DAMAGE, @T_SAME_LANE_BACK, 'PHYSICAL', 0, 1);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue)
    VALUES (@NewEffectId, @A_ATK, 2.500000, 0);

    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DurationTurns, ChancePercent, DisplayOrder)
    VALUES ('RICARDO_MILOS', @E_STUN, @T_SAME_LANE_BACK, 1, 100.00, 2);
END

-- 20. RANDOM_KNOWLEDGE_DROP: Kiến Thức Sang Chấn (MAGIC_DAMAGE x 2.60 lên 1 địch ngẫu nhiên + Choáng 80%)
IF EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'RANDOM_KNOWLEDGE_DROP')
BEGIN
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DisplayOrder)
    VALUES ('RANDOM_KNOWLEDGE_DROP', @E_DAMAGE, @T_RANDOM_ENEMY, 'MAGIC', 0, 1);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue)
    VALUES (@NewEffectId, @A_MAGIC_DMG, 2.600000, 0);

    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DurationTurns, ChancePercent, DisplayOrder)
    VALUES ('RANDOM_KNOWLEDGE_DROP', @E_STUN, @T_RANDOM_ENEMY, 1, 80.00, 2);
END

-- 21. DARK_KNOWLEDGE_SHIELD_CONVERSION: Giáp Hư Không (MAGIC_DAMAGE x 1.40 toàn địch + Shield bản thân MAGIC_DAMAGE x 0.80)
IF EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'DARK_KNOWLEDGE_SHIELD_CONVERSION')
BEGIN
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DisplayOrder)
    VALUES ('DARK_KNOWLEDGE_SHIELD_CONVERSION', @E_DAMAGE, @T_ALL_ENEMY, 'MAGIC', 0, 1);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue)
    VALUES (@NewEffectId, @A_MAGIC_DMG, 1.400000, 0);

    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DurationTurns, DisplayOrder)
    VALUES ('DARK_KNOWLEDGE_SHIELD_CONVERSION', @E_SHIELD, @T_SELF, 2, 2);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue)
    VALUES (@NewEffectId, @A_MAGIC_DMG, 0.800000, 0);
END

-- 22. TACTICAL_AIR_STRIKE: Oanh Tạc Hàng Sau (ATK x 2.00 hàng sau + Đánh Dấu 2 lượt)
IF EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'TACTICAL_AIR_STRIKE')
BEGIN
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DisplayOrder)
    VALUES ('TACTICAL_AIR_STRIKE', @E_DAMAGE, @T_BACK_ROW_ENEMY, 'PHYSICAL', 0, 1);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue)
    VALUES (@NewEffectId, @A_ATK, 2.000000, 0);

    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DurationTurns, DisplayOrder)
    VALUES ('TACTICAL_AIR_STRIKE', @E_MARK, @T_BACK_ROW_ENEMY, 2, 2);
END

-- 23. DOI_NGOI_DAU_DOC: Đổi Ngôi Đầu Độc (Hoán đổi vị trí + MAGIC_DAMAGE x 1.00 + Giảm 15% SPD)
IF EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'DOI_NGOI_DAU_DOC')
BEGIN
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DisplayOrder)
    VALUES ('DOI_NGOI_DAU_DOC', @E_POSITION_SWAP, @T_FRONT_ROW_ENEMY, 1);

    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DisplayOrder)
    VALUES ('DOI_NGOI_DAU_DOC', @E_DAMAGE, @T_FRONT_ROW_ENEMY, 'MAGIC', 0, 2);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue)
    VALUES (@NewEffectId, @A_MAGIC_DMG, 1.000000, 0);

    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DurationTurns, DisplayOrder)
    VALUES ('DOI_NGOI_DAU_DOC', @E_STAT_DEBUFF, @T_FRONT_ROW_ENEMY, 2, 3);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectStatModifiers (SkillEffectId, AttributeTypeId, ValueType, Value)
    VALUES (@NewEffectId, @A_SPD, 'PERCENT', -15.0000);
END

-- 24. FATAL_ALL_IN_DIRECTIVE: Lệnh All-In Hủy Diệt (ATK x 2.80 lên 4 địch ngẫu nhiên + Nguy cơ Phá Sản)
IF EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'FATAL_ALL_IN_DIRECTIVE')
BEGIN
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DisplayOrder)
    VALUES ('FATAL_ALL_IN_DIRECTIVE', @E_DAMAGE, @T_RANDOM_4_ENEMY, 'PHYSICAL', 0, 1);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue)
    VALUES (@NewEffectId, @A_ATK, 2.800000, 0);

    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DurationTurns, DisplayOrder)
    VALUES ('FATAL_ALL_IN_DIRECTIVE', @E_STAT_DEBUFF, @T_SELF, 2, 2);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectStatModifiers (SkillEffectId, AttributeTypeId, ValueType, Value)
    VALUES (@NewEffectId, @A_DEF, 'PERCENT', -50.0000);

    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DurationTurns, DisplayOrder)
    VALUES ('FATAL_ALL_IN_DIRECTIVE', @E_SILENCE, @T_SELF, 2, 3);
END

-- 25. WINTER_NIGHT_BLESSINGS: Quà Tặng Đêm Đông (Hồi 30% Max HP toàn đội + Giảm 50% dmg 2 đồng minh + Giảm 40% dmg bản thân)
IF EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'WINTER_NIGHT_BLESSINGS')
BEGIN
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DisplayOrder)
    VALUES ('WINTER_NIGHT_BLESSINGS', @E_HEAL, @T_ALL_ALLY, 1);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue)
    VALUES (@NewEffectId, @A_HP, 0.300000, 0);

    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, BaseValue, DurationTurns, DisplayOrder)
    VALUES ('WINTER_NIGHT_BLESSINGS', @E_DAMAGE_REDUCTION, @T_RANDOM_2_ALLY, 50.0000, 2, 2);

    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, BaseValue, DurationTurns, DisplayOrder)
    VALUES ('WINTER_NIGHT_BLESSINGS', @E_DAMAGE_REDUCTION, @T_SELF, 40.0000, 2, 3);
END

-- 26. DEADLIFT_DIA_CHAN: Deadlift Địa Chấn (ATK x 2.20 hàng trước + Khiêu khích 2 lượt + Giáp DEF x 1.50 + Phản 30% dmg)
IF EXISTS (SELECT 1 FROM dbo.HRK_SkillTemplates WHERE Id = 'DEADLIFT_DIA_CHAN')
BEGIN
    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DamageSchoolCode, BaseValue, DisplayOrder)
    VALUES ('DEADLIFT_DIA_CHAN', @E_DAMAGE, @T_FRONT_ROW_ENEMY, 'PHYSICAL', 0, 1);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue)
    VALUES (@NewEffectId, @A_ATK, 2.200000, 0);

    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DurationTurns, DisplayOrder)
    VALUES ('DEADLIFT_DIA_CHAN', @E_TAUNT, @T_FRONT_ROW_ENEMY, 2, 2);

    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, DurationTurns, DisplayOrder)
    VALUES ('DEADLIFT_DIA_CHAN', @E_SHIELD, @T_SELF, 2, 3);
    SET @NewEffectId = SCOPE_IDENTITY();
    INSERT INTO dbo.HRK_SkillEffectScalings (SkillEffectId, AttributeTypeId, Coefficient, FlatValue)
    VALUES (@NewEffectId, @A_DEF, 1.500000, 0);

    INSERT INTO dbo.HRK_SkillEffects (SkillId, EffectTypeId, TargetTypeId, BaseValue, DurationTurns, DisplayOrder)
    VALUES ('DEADLIFT_DIA_CHAN', @E_DAMAGE_REFLECTION, @T_SELF, 30.0000, 2, 4);
END

PRINT '-> Da seed day du Effects, Scalings va StatModifiers cho tat ca 26 ky nang.';

-- ---------------------------------------------------------------------------
-- 10. ĐẢM BẢO ÁNH XẠ HRK_HeroSkills CHO CÁC TƯỚNG HIỆN CÓ
-- ---------------------------------------------------------------------------
DECLARE @Mappings TABLE (HeroName NVARCHAR(200), SkillId NVARCHAR(100), SkillOrder TINYINT);
INSERT INTO @Mappings (HeroName, SkillId, SkillOrder) VALUES
  (N'K Cởi Trần',          'HEAVENLY_JUDGMENT', 1),
  (N'K Cởi Trần',          'ULTIMATE_SIXPACK', 2),
  (N'Nam Deadline',        'NORMAL_ATTACK', 1),
  (N'Nam Deadline',        'HEAVY_SLASH', 2),
  (N'Nam Deadline',        'VAX_A_MILLION_SANITZATION', 3),
  (N'Chuẩn Men',           'NORMAL_ATTACK', 1),
  (N'Chuẩn Men',           'SLASH', 2),
  (N'Chuẩn Men',           'SWORD_DANCE', 3),
  (N'Chuẩn Men',           'RICARDO_MILOS', 4),
  (N'Coder Bảnh',          'NORMAL_ATTACK', 1),
  (N'Coder Bảnh',          'RANDOM_KNOWLEDGE_DROP', 2),
  (N'Coder Bảnh',          'REFACTOR_CODE', 3),
  (N'Coder Bảnh',          'DEPLOY_PROD', 4),
  (N'Tester Đẹp',          'NORMAL_ATTACK', 1),
  (N'Tester Đẹp',          'AUTOMATION_TEST', 2),
  (N'Tester Đẹp',          'DARK_KNOWLEDGE_SHIELD_CONVERSION', 3),
  (N'Tướng Long Quân Đội', 'NORMAL_ATTACK', 1),
  (N'Tướng Long Quân Đội', 'TACTICAL_AIR_STRIKE', 2),
  (N'PM Hối Hả',           'NORMAL_ATTACK', 1),
  (N'PM Hối Hả',           'DOI_NGOI_DAU_DOC', 2),
  (N'QA Kỹ Tính',          'NORMAL_ATTACK', 1),
  (N'QA Kỹ Tính',          'FATAL_ALL_IN_DIRECTIVE', 2),
  (N'Kiet Noel',           'NORMAL_ATTACK', 1),
  (N'Kiet Noel',           'WINTER_NIGHT_BLESSINGS', 2),
  (N'Hoàng Nguyên',        'NORMAL_ATTACK', 1),
  (N'Hoàng Nguyên',        'DEADLIFT_DIA_CHAN', 2);

MERGE dbo.HRK_HeroSkills AS target
USING (
    SELECT h.Id AS HeroTemplateId, s.Id AS SkillId, m.SkillOrder
    FROM @Mappings m
    JOIN dbo.HRK_HeroTemplates h ON h.Name = m.HeroName
    JOIN dbo.HRK_SkillTemplates s ON s.Id = m.SkillId
) AS source
ON target.HeroTemplateId = source.HeroTemplateId AND target.SkillId = source.SkillId
WHEN MATCHED THEN
    UPDATE SET target.SkillOrder = source.SkillOrder
WHEN NOT MATCHED THEN
    INSERT (HeroTemplateId, SkillId, SkillOrder)
    VALUES (source.HeroTemplateId, source.SkillId, source.SkillOrder);

PRINT '-> Da seed/merge HRK_HeroSkills cho cac tuong mau.';

COMMIT TRANSACTION;
PRINT '=== [HOÀN THÀNH] Migration_SkillSystemRefactor thanh cong! ===';

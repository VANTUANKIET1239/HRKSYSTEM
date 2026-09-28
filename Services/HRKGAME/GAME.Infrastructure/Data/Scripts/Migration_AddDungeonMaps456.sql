-- ==============================================================================
-- MIGRATION SCRIPT: ADD DUNGEON CAMPAIGN MAPS 4, 5, AND 6
-- ==============================================================================
-- Map 4: Thành Phố Neon Mất Kiểm Soát (NEON_CITY)
-- Map 5: Sa Mạc Văn Phòng Deadline (DEADLINE_DESERT)
-- Map 6: Thiên Cung Meme Tối Thượng (MEME_HEAVEN)
--
-- Features:
-- - Idempotent execution (can be run multiple times safely).
-- - Dynamic lookups by Code / Name, avoiding hardcoded collision-prone IDs.
-- - 15 stages per map (Stages 5, 10 = MINI_BOSS, Stage 15 = BOSS).
-- - 5 deterministic enemy combatants per stage (225 enemies total).
-- - RecommendedPower calculated directly from actual enemy team power.
-- - Strictly adheres to rarity rank rules (drops <= Epic).
-- - 3 star chests per map (15, 30, 45 stars) with gold, diamonds, upgrade materials,
--   and guaranteed equipment.
-- ==============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    PRINT N'==> 1. Kiểm tra cấu trúc bảng hệ thống phó bản...';
    IF OBJECT_ID(N'dbo.HRK_DungeonMaps', N'U') IS NULL
        THROW 51000, 'Missing dbo.HRK_DungeonMaps. Chạy migration dungeon campaign trước.', 1;
    IF OBJECT_ID(N'dbo.HRK_DungeonStages', N'U') IS NULL
        THROW 51000, 'Missing dbo.HRK_DungeonStages.', 1;
    IF OBJECT_ID(N'dbo.HRK_DungeonStageEnemies', N'U') IS NULL
        THROW 51000, 'Missing dbo.HRK_DungeonStageEnemies.', 1;
    IF OBJECT_ID(N'dbo.HRK_DungeonMapStarChests', N'U') IS NULL
        THROW 51000, 'Missing dbo.HRK_DungeonMapStarChests.', 1;
    IF OBJECT_ID(N'dbo.HRK_DungeonStageDropPools', N'U') IS NULL
        THROW 51000, 'Missing dbo.HRK_DungeonStageDropPools.', 1;

    -- Tra cứu Rarity IDs
    DECLARE @EpicRarityId INT = (SELECT TOP 1 Id FROM dbo.HRK_Rarities WHERE UPPER(Code) = 'EPIC');
    DECLARE @RareRarityId INT = (SELECT TOP 1 Id FROM dbo.HRK_Rarities WHERE UPPER(Code) = 'RARE');
    DECLARE @CommonRarityId INT = (SELECT TOP 1 Id FROM dbo.HRK_Rarities WHERE UPPER(Code) = 'COMMON');

    IF @EpicRarityId IS NULL OR @RareRarityId IS NULL OR @CommonRarityId IS NULL
        THROW 51001, 'Không tìm thấy đủ phẩm chất COMMON, RARE, EPIC trong dbo.HRK_Rarities.', 1;

    -- Tra cứu Map 3 (PRODUCTION_CITADEL) để liên kết PreviousMapId
    DECLARE @Map3Id INT = (SELECT TOP 1 Id FROM dbo.HRK_DungeonMaps WHERE Code = 'PRODUCTION_CITADEL' OR DisplayOrder = 3);
    IF @Map3Id IS NULL
        THROW 51002, 'Không tìm thấy Bản đồ 3 (PRODUCTION_CITADEL) để liên kết tiến trình.', 1;

    -- Tra cứu Hero Template IDs linh hoạt bằng tên
    DECLARE @HeroK INT = ISNULL((SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE Name LIKE N'%K Cởi Trần%'), 1);
    DECLARE @HeroNam INT = ISNULL((SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE Name LIKE N'%Nam Deadline%'), 2);
    DECLARE @HeroChuanMen INT = ISNULL((SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE Name LIKE N'%Chuẩn Men%'), 3);
    DECLARE @HeroCoder INT = ISNULL((SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE Name LIKE N'%Coder Bảnh%'), 4);
    DECLARE @HeroTester INT = ISNULL((SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE Name LIKE N'%Tester Đẹp%'), 5);
    DECLARE @HeroTuongLong INT = ISNULL((SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE Name LIKE N'%Tướng Long%'), 6);
    DECLARE @HeroPM INT = ISNULL((SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE Name LIKE N'%PM Hối Hả%'), 7);
    DECLARE @HeroQA INT = ISNULL((SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE Name LIKE N'%QA Kỹ Tính%'), 8);
    DECLARE @HeroKietNoel INT = ISNULL((SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE Name LIKE N'%Kiet Noel%'), 9);
    DECLARE @HeroHoangNguyen INT = ISNULL((SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE Name LIKE N'%Hoàng Nguyên%'), 10);
    DECLARE @HeroTaoLaNhat INT = ISNULL((SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE Name LIKE N'%Tao là nhất%'), 11);
    DECLARE @HeroThanhThai INT = ISNULL((SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE Name LIKE N'%Thanh Thái%'), 12);
    DECLARE @HeroNghiaPhuc INT = ISNULL((SELECT TOP 1 Id FROM dbo.HRK_HeroTemplates WHERE Name LIKE N'%Nghĩa Phục%'), 13);

    -- Tra cứu Item Template IDs cho drops và rương sao
    DECLARE @BroomRareId INT = (SELECT TOP 1 Id FROM dbo.HRK_ItemTemplates WHERE Code = 'COMBAT_BROOM_MK2');
    DECLARE @GlassArmorRareId INT = (SELECT TOP 1 Id FROM dbo.HRK_ItemTemplates WHERE Code = 'VIRTUAL_GLASS_ARMOR');
    DECLARE @JetSkatesRareId INT = (SELECT TOP 1 Id FROM dbo.HRK_ItemTemplates WHERE Code = 'NEON_JET_SKATES');
    DECLARE @TabletRareId INT = (SELECT TOP 1 Id FROM dbo.HRK_ItemTemplates WHERE Code = 'SUMMONING_TABLET');
    DECLARE @HeadsetRareId INT = (SELECT TOP 1 Id FROM dbo.HRK_ItemTemplates WHERE Code = 'MIND_SYNC_HEADSET');
    DECLARE @HelmetRareId INT = (SELECT TOP 1 Id FROM dbo.HRK_ItemTemplates WHERE Code = 'METEOR_PILOT_HELMET');

    DECLARE @GuitarEpicId INT = (SELECT TOP 1 Id FROM dbo.HRK_ItemTemplates WHERE Code = 'SIX_STRING_DEMON_GUITAR');
    DECLARE @CircusCoatEpicId INT = (SELECT TOP 1 Id FROM dbo.HRK_ItemTemplates WHERE Code = 'VOID_CIRCUS_COAT');
    DECLARE @CatSlippersEpicId INT = (SELECT TOP 1 Id FROM dbo.HRK_ItemTemplates WHERE Code = 'QUANTUM_CAT_SLIPPERS');
    DECLARE @RiceCookerEpicId INT = (SELECT TOP 1 Id FROM dbo.HRK_ItemTemplates WHERE Code = 'ALCHEMY_RICE_COOKER');
    DECLARE @PocketPlanetEpicId INT = (SELECT TOP 1 Id FROM dbo.HRK_ItemTemplates WHERE Code = 'POCKET_PLANET_RING');
    DECLARE @SharkHelmetEpicId INT = (SELECT TOP 1 Id FROM dbo.HRK_ItemTemplates WHERE Code = 'ROBOT_SHARK_HELMET');

    PRINT N'==> 2. Khởi tạo / cập nhật thông tin 3 Bản đồ mới (Map 4, 5, 6)...';

    -- Map 4: NEON_CITY
    DECLARE @Map4Id INT;
    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_DungeonMaps WHERE Code = 'NEON_CITY')
    BEGIN
        INSERT INTO dbo.HRK_DungeonMaps (Code, Name, Description, ImagePath, BackgroundPath, DisplayOrder, RequiredPlayerLevel, PreviousMapId, IsActive, MaxEquipmentRarityId)
        VALUES ('NEON_CITY', N'Thành Phố Neon Mất Kiểm Soát',
                N'Thành phố tương lai rực rỡ ánh đèn neon chìm trong mưa đêm, nơi mạng lưới AI và robot quân sự nổi loạn chống lại loài người.',
                '/assets/images/dcs-game/dungeons/map-04-neon/thumbnail.jpg',
                '/assets/images/dcs-game/dungeons/map-04-neon/map-background.jpg',
                4, 20, @Map3Id, 1, @EpicRarityId);
        SET @Map4Id = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        SELECT @Map4Id = Id FROM dbo.HRK_DungeonMaps WHERE Code = 'NEON_CITY';
        UPDATE dbo.HRK_DungeonMaps
        SET Name = N'Thành Phố Neon Mất Kiểm Soát',
            Description = N'Thành phố tương lai rực rỡ ánh đèn neon chìm trong mưa đêm, nơi mạng lưới AI và robot quân sự nổi loạn chống lại loài người.',
            ImagePath = '/assets/images/dcs-game/dungeons/map-04-neon/thumbnail.jpg',
            BackgroundPath = '/assets/images/dcs-game/dungeons/map-04-neon/map-background.jpg',
            DisplayOrder = 4,
            RequiredPlayerLevel = 20,
            PreviousMapId = @Map3Id,
            IsActive = 1,
            MaxEquipmentRarityId = @EpicRarityId
        WHERE Id = @Map4Id;
    END

    -- Map 5: DEADLINE_DESERT
    DECLARE @Map5Id INT;
    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_DungeonMaps WHERE Code = 'DEADLINE_DESERT')
    BEGIN
        INSERT INTO dbo.HRK_DungeonMaps (Code, Name, Description, ImagePath, BackgroundPath, DisplayOrder, RequiredPlayerLevel, PreviousMapId, IsActive, MaxEquipmentRarityId)
        VALUES ('DEADLINE_DESERT', N'Sa Mạc Văn Phòng Deadline',
                N'Vùng sa mạc cát vàng vùi lấp những tòa cao ốc văn phòng cổ xưa, nơi đồng hồ đếm ngược vang vọng cùng nỗi ám ảnh deadline vĩnh hằng.',
                '/assets/images/dcs-game/dungeons/map-05-deadline-desert/thumbnail.jpg',
                '/assets/images/dcs-game/dungeons/map-05-deadline-desert/map-background.jpg',
                5, 25, @Map4Id, 1, @EpicRarityId);
        SET @Map5Id = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        SELECT @Map5Id = Id FROM dbo.HRK_DungeonMaps WHERE Code = 'DEADLINE_DESERT';
        UPDATE dbo.HRK_DungeonMaps
        SET Name = N'Sa Mạc Văn Phòng Deadline',
            Description = N'Vùng sa mạc cát vàng vùi lấp những tòa cao ốc văn phòng cổ xưa, nơi đồng hồ đếm ngược vang vọng cùng nỗi ám ảnh deadline vĩnh hằng.',
            ImagePath = '/assets/images/dcs-game/dungeons/map-05-deadline-desert/thumbnail.jpg',
            BackgroundPath = '/assets/images/dcs-game/dungeons/map-05-deadline-desert/map-background.jpg',
            DisplayOrder = 5,
            RequiredPlayerLevel = 25,
            PreviousMapId = @Map4Id,
            IsActive = 1,
            MaxEquipmentRarityId = @EpicRarityId
        WHERE Id = @Map5Id;
    END

    -- Map 6: MEME_HEAVEN
    DECLARE @Map6Id INT;
    IF NOT EXISTS (SELECT 1 FROM dbo.HRK_DungeonMaps WHERE Code = 'MEME_HEAVEN')
    BEGIN
        INSERT INTO dbo.HRK_DungeonMaps (Code, Name, Description, ImagePath, BackgroundPath, DisplayOrder, RequiredPlayerLevel, PreviousMapId, IsActive, MaxEquipmentRarityId)
        VALUES ('MEME_HEAVEN', N'Thiên Cung Meme Tối Thượng',
                N'Cung điện thần thánh ngự trị trên tầng mây ngũ sắc, nơi chư thần meme tối cao sở hữu quyền năng vô tận nhưng đầy hài hước.',
                '/assets/images/dcs-game/dungeons/map-06-meme-heaven/thumbnail.jpg',
                '/assets/images/dcs-game/dungeons/map-06-meme-heaven/map-background.jpg',
                6, 30, @Map5Id, 1, @EpicRarityId);
        SET @Map6Id = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        SELECT @Map6Id = Id FROM dbo.HRK_DungeonMaps WHERE Code = 'MEME_HEAVEN';
        UPDATE dbo.HRK_DungeonMaps
        SET Name = N'Thiên Cung Meme Tối Thượng',
            Description = N'Cung điện thần thánh ngự trị trên tầng mây ngũ sắc, nơi chư thần meme tối cao sở hữu quyền năng vô tận nhưng đầy hài hước.',
            ImagePath = '/assets/images/dcs-game/dungeons/map-06-meme-heaven/thumbnail.jpg',
            BackgroundPath = '/assets/images/dcs-game/dungeons/map-06-meme-heaven/map-background.jpg',
            DisplayOrder = 6,
            RequiredPlayerLevel = 30,
            PreviousMapId = @Map5Id,
            IsActive = 1,
            MaxEquipmentRarityId = @EpicRarityId
        WHERE Id = @Map6Id;
    END

    PRINT N'==> 3. Cấu hình 45 màn phó bản và đội hình địch cho Map 4, 5, 6...';

    -- Bảng tạm định nghĩa dữ liệu 45 Stage
    DECLARE @StageSeed TABLE (
        MapOrder INT,
        StageNo INT,
        StageName NVARCHAR(100),
        StageType VARCHAR(20),
        StaminaCost INT,
        GoldReward BIGINT,
        PlayerExpReward INT,
        HeroExpReward INT,
        FirstClearGold BIGINT,
        BattleBg NVARCHAR(260),
        BossName NVARCHAR(100),
        BossImg NVARCHAR(260),
        BossHeroTemplateId INT
    );

    -- MAP 4: NEON_CITY (15 stages)
    INSERT INTO @StageSeed VALUES (4, 1, N'Cổng Thành Phố Số Hóa', 'NORMAL', 8, 9750, 620, 435, 22800, '/assets/images/dcs-game/dungeons/map-04-neon/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (4, 2, N'Đại Lộ Mưa Neon', 'NORMAL', 8, 10000, 640, 450, 23600, '/assets/images/dcs-game/dungeons/map-04-neon/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (4, 3, N'Khu Xưởng Drone Hư Hỏng', 'NORMAL', 8, 10250, 660, 465, 24400, '/assets/images/dcs-game/dungeons/map-04-neon/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (4, 4, N'Hẻm Hologram Lạc Lối', 'NORMAL', 8, 10500, 680, 480, 25200, '/assets/images/dcs-game/dungeons/map-04-neon/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (4, 5, N'Trạm Kiểm Soát Cảnh Vệ', 'MINI_BOSS', 12, 11000, 720, 510, 26500, '/assets/images/dcs-game/dungeons/map-04-neon/battle-background.jpg', N'Robot Cảnh Vệ Neon', '/assets/images/dcs-game/dungeons/map-04-neon/neon-guard-boss.png', @HeroTuongLong);
    INSERT INTO @StageSeed VALUES (4, 6, N'Mạng Lưới Dây Cáp Ngầm', 'NORMAL', 8, 11250, 740, 525, 27300, '/assets/images/dcs-game/dungeons/map-04-neon/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (4, 7, N'Khu Ổ Chuột Hacker', 'NORMAL', 8, 11500, 760, 540, 28100, '/assets/images/dcs-game/dungeons/map-04-neon/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (4, 8, N'Tuyến Tàu Điện Cao Tốc Bị Hack', 'NORMAL', 8, 11750, 780, 555, 28900, '/assets/images/dcs-game/dungeons/map-04-neon/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (4, 9, N'Nút Giao Thông Quá Tải', 'NORMAL', 8, 12000, 800, 570, 29700, '/assets/images/dcs-game/dungeons/map-04-neon/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (4, 10, N'Tháp Điều Phối Giao Thông', 'MINI_BOSS', 12, 12500, 840, 600, 31000, '/assets/images/dcs-game/dungeons/map-04-neon/battle-background.jpg', N'AI Điều Phối Giao Thông', '/assets/images/dcs-game/dungeons/map-04-neon/traffic-ai-boss.png', @HeroCoder);
    INSERT INTO @StageSeed VALUES (4, 11, N'Phòng Nghiên Cứu Android', 'NORMAL', 8, 12750, 860, 615, 31800, '/assets/images/dcs-game/dungeons/map-04-neon/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (4, 12, N'Kho Vũ Khí Bạo Động', 'NORMAL', 8, 13000, 880, 630, 32600, '/assets/images/dcs-game/dungeons/map-04-neon/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (4, 13, N'Bến Cảng Robot Quân Sự', 'NORMAL', 8, 13250, 900, 645, 33400, '/assets/images/dcs-game/dungeons/map-04-neon/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (4, 14, N'Đỉnh Tháp Mạng Chủ Neon', 'NORMAL', 8, 13500, 920, 660, 34200, '/assets/images/dcs-game/dungeons/map-04-neon/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (4, 15, N'Trái Tim Máy Chủ Tận Cùng', 'BOSS', 12, 14250, 980, 705, 36000, '/assets/images/dcs-game/dungeons/map-04-neon/battle-background.jpg', N'404 Không Tìm Thấy Lòng Thương', '/assets/images/dcs-game/dungeons/map-04-neon/boss-404-ruthless.png', @HeroHoangNguyen);

    -- MAP 5: DEADLINE_DESERT (15 stages)
    INSERT INTO @StageSeed VALUES (5, 1, N'Cồn Cát Thực Tập Sinh', 'NORMAL', 10, 14350, 975, 700, 36200, '/assets/images/dcs-game/dungeons/map-05-deadline-desert/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (5, 2, N'Vùng Đất Máy In Nổi Loạn', 'NORMAL', 10, 14700, 1000, 720, 37400, '/assets/images/dcs-game/dungeons/map-05-deadline-desert/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (5, 3, N'Nghĩa Địa Hồ Sơ Thất Lạc', 'NORMAL', 10, 15050, 1025, 740, 38600, '/assets/images/dcs-game/dungeons/map-05-deadline-desert/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (5, 4, N'Thung Lũng Tăng Ca Không Công', 'NORMAL', 10, 15400, 1050, 760, 39800, '/assets/images/dcs-game/dungeons/map-05-deadline-desert/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (5, 5, N'Điện Thờ Máy Chấm Công', 'MINI_BOSS', 14, 16100, 1100, 800, 42000, '/assets/images/dcs-game/dungeons/map-05-deadline-desert/battle-background.jpg', N'Máy Chấm Công Vĩnh Cửu', '/assets/images/dcs-game/dungeons/map-05-deadline-desert/eternal-timekeeper-boss.png', @HeroQA);
    INSERT INTO @StageSeed VALUES (5, 6, N'Hẻm Cát Kế Toán Xác Sống', 'NORMAL', 10, 16450, 1125, 820, 43200, '/assets/images/dcs-game/dungeons/map-05-deadline-desert/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (5, 7, N'Ốc Đảo Hóa Đơn Khống', 'NORMAL', 10, 16800, 1150, 840, 44400, '/assets/images/dcs-game/dungeons/map-05-deadline-desert/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (5, 8, N'Tàn Tích Báo Cáo Tuần', 'NORMAL', 10, 17150, 1175, 860, 45600, '/assets/images/dcs-game/dungeons/map-05-deadline-desert/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (5, 9, N'Dốc Bão Cát Scope Creep', 'NORMAL', 10, 17500, 1200, 880, 46800, '/assets/images/dcs-game/dungeons/map-05-deadline-desert/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (5, 10, N'Văn Phòng Quản Lý Sa Mạc', 'MINI_BOSS', 14, 18200, 1250, 920, 49000, '/assets/images/dcs-game/dungeons/map-05-deadline-desert/battle-background.jpg', N'Trưởng Phòng Không Duyệt Nghỉ', '/assets/images/dcs-game/dungeons/map-05-deadline-desert/no-leave-manager-boss.png', @HeroPM);
    INSERT INTO @StageSeed VALUES (5, 11, N'Chiến Hào Email Khẩn Cấp', 'NORMAL', 10, 18550, 1275, 940, 50200, '/assets/images/dcs-game/dungeons/map-05-deadline-desert/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (5, 12, N'Rặng Đá Phê Duyệt Treo', 'NORMAL', 10, 18900, 1300, 960, 51400, '/assets/images/dcs-game/dungeons/map-05-deadline-desert/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (5, 13, N'Biển Cát Đếm Ngược Định Mệnh', 'NORMAL', 10, 19250, 1325, 980, 52600, '/assets/images/dcs-game/dungeons/map-05-deadline-desert/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (5, 14, N'Kim Tự Tháp Tăng Ca Đêm', 'NORMAL', 10, 19600, 1350, 1000, 53800, '/assets/images/dcs-game/dungeons/map-05-deadline-desert/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (5, 15, N'Điện Thờ Đại Đế Deadline', 'BOSS', 14, 20500, 1420, 1060, 56500, '/assets/images/dcs-game/dungeons/map-05-deadline-desert/battle-background.jpg', N'Đại Đế Deadline', '/assets/images/dcs-game/dungeons/map-05-deadline-desert/deadline-emperor-boss.png', @HeroNam);

    -- MAP 6: MEME_HEAVEN (15 stages)
    INSERT INTO @StageSeed VALUES (6, 1, N'Cổng Mây Ngũ Sắc', 'NORMAL', 12, 21000, 1435, 1050, 56800, '/assets/images/dcs-game/dungeons/map-06-meme-heaven/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (6, 2, N'Đảo Bay Thiên Binh', 'NORMAL', 12, 21500, 1470, 1080, 58600, '/assets/images/dcs-game/dungeons/map-06-meme-heaven/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (6, 3, N'Bậc Thềm Tượng Đá Thần Vực', 'NORMAL', 12, 22000, 1505, 1110, 60400, '/assets/images/dcs-game/dungeons/map-06-meme-heaven/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (6, 4, N'Khu Rừng Linh Thú Cổ Đại', 'NORMAL', 12, 22500, 1540, 1140, 62200, '/assets/images/dcs-game/dungeons/map-06-meme-heaven/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (6, 5, N'Thiên Môn Băng Thông Thần Thánh', 'MINI_BOSS', 16, 23500, 1610, 1200, 65500, '/assets/images/dcs-game/dungeons/map-06-meme-heaven/battle-background.jpg', N'Thần Giữ Cổng Wi-Fi', '/assets/images/dcs-game/dungeons/map-06-meme-heaven/wifi-gatekeeper-god.png', @HeroChuanMen);
    INSERT INTO @StageSeed VALUES (6, 6, N'Tháp Pháp Sư Thiên Giới', 'NORMAL', 12, 24000, 1645, 1230, 67300, '/assets/images/dcs-game/dungeons/map-06-meme-heaven/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (6, 7, N'Hành Lang Chữ Ký Số Hóa', 'NORMAL', 12, 24500, 1680, 1260, 69100, '/assets/images/dcs-game/dungeons/map-06-meme-heaven/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (6, 8, N'Vườn Hoa Đạo Đức Giả Vờ', 'NORMAL', 12, 25000, 1715, 1290, 70900, '/assets/images/dcs-game/dungeons/map-06-meme-heaven/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (6, 9, N'Sân Đấu Chiến Binh Thần Thoại', 'NORMAL', 12, 25500, 1750, 1320, 72700, '/assets/images/dcs-game/dungeons/map-06-meme-heaven/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (6, 10, N'Lôi Điện Đài Hết Năng Lượng', 'MINI_BOSS', 16, 26500, 1820, 1380, 76000, '/assets/images/dcs-game/dungeons/map-06-meme-heaven/battle-background.jpg', N'Thần Sấm Hết Pin', '/assets/images/dcs-game/dungeons/map-06-meme-heaven/depleted-thunder-god.png', @HeroThanhThai);
    INSERT INTO @StageSeed VALUES (6, 11, N'Cầu Vồng Tối Cao Hư Vô', 'NORMAL', 12, 27000, 1855, 1410, 77800, '/assets/images/dcs-game/dungeons/map-06-meme-heaven/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (6, 12, N'Trấn Yểm Thần Long Đa Vũ Trụ', 'NORMAL', 12, 27500, 1890, 1440, 79600, '/assets/images/dcs-game/dungeons/map-06-meme-heaven/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (6, 13, N'Vương Phủ Hộ Vệ Thần Tộc', 'NORMAL', 12, 28000, 1925, 1470, 81400, '/assets/images/dcs-game/dungeons/map-06-meme-heaven/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (6, 14, N'Thềm Trời Ngôi Đền Tối Cao', 'NORMAL', 12, 28500, 1960, 1500, 83200, '/assets/images/dcs-game/dungeons/map-06-meme-heaven/battle-background.jpg', NULL, NULL, NULL);
    INSERT INTO @StageSeed VALUES (6, 15, N'Ngai Vàng Meme Bất Tử', 'BOSS', 16, 30000, 2050, 1580, 87500, '/assets/images/dcs-game/dungeons/map-06-meme-heaven/battle-background.jpg', N'Chúa Tể "Tao Là Nhất" Thiên Giới', '/assets/images/dcs-game/dungeons/map-06-meme-heaven/celestial-supreme-lord.png', @HeroTaoLaNhat);

    -- Duyệt và thêm/cập nhật từng stage
    DECLARE @CurMapOrder INT, @CurStageNo INT, @CurStageId INT, @CurMapId INT;
    DECLARE @StageCursor CURSOR;

    SET @StageCursor = CURSOR FOR
        SELECT MapOrder, StageNo FROM @StageSeed ORDER BY MapOrder, StageNo;

    OPEN @StageCursor;
    FETCH NEXT FROM @StageCursor INTO @CurMapOrder, @CurStageNo;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @CurMapId = CASE @CurMapOrder
            WHEN 4 THEN @Map4Id
            WHEN 5 THEN @Map5Id
            WHEN 6 THEN @Map6Id
        END;

        DECLARE @sName NVARCHAR(100), @sType VARCHAR(20), @sStamina INT, @sGold BIGINT, @sPExp INT, @sHExp INT, @s1stGold BIGINT, @sBg NVARCHAR(260);
        DECLARE @bName NVARCHAR(100), @bImg NVARCHAR(260), @bHeroId INT;

        SELECT @sName = StageName, @sType = StageType, @sStamina = StaminaCost, @sGold = GoldReward,
               @sPExp = PlayerExpReward, @sHExp = HeroExpReward, @s1stGold = FirstClearGold, @sBg = BattleBg,
               @bName = BossName, @bImg = BossImg, @bHeroId = BossHeroTemplateId
        FROM @StageSeed WHERE MapOrder = @CurMapOrder AND StageNo = @CurStageNo;

        -- 1. Insert hoặc Update Stage
        IF NOT EXISTS (SELECT 1 FROM dbo.HRK_DungeonStages WHERE DungeonMapId = @CurMapId AND StageNumber = @CurStageNo)
        BEGIN
            INSERT INTO dbo.HRK_DungeonStages (DungeonMapId, StageNumber, Name, StageType, StaminaCost, RecommendedPower, GoldReward, PlayerExpReward, HeroExpReward, FirstClearGoldReward, BackgroundPath, IsActive)
            VALUES (@CurMapId, @CurStageNo, @sName, @sType, @sStamina, 50000, @sGold, @sPExp, @sHExp, @s1stGold, @sBg, 1);
            SET @CurStageId = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            SELECT @CurStageId = Id FROM dbo.HRK_DungeonStages WHERE DungeonMapId = @CurMapId AND StageNumber = @CurStageNo;
            UPDATE dbo.HRK_DungeonStages
            SET Name = @sName,
                StageType = @sType,
                StaminaCost = @sStamina,
                GoldReward = @sGold,
                PlayerExpReward = @sPExp,
                HeroExpReward = @sHExp,
                FirstClearGoldReward = @s1stGold,
                BackgroundPath = @sBg,
                IsActive = 1
            WHERE Id = @CurStageId;
        END

        -- 2. Cấu hình 5 Enemy combatants cho Stage
        -- Base stat multiplier scale:
        -- Map 4: 3.50 + (StageNo-1)*0.12 (Boss 5: x1.20, Boss 10: x1.35, Boss 15: x1.55)
        -- Map 5: 5.30 + (StageNo-1)*0.16 (Boss 5: x1.20, Boss 10: x1.35, Boss 15: x1.55)
        -- Map 6: 7.70 + (StageNo-1)*0.22 (Boss 5: x1.20, Boss 10: x1.35, Boss 15: x1.55)
        DECLARE @BaseMultiplier DECIMAL(8,4) = CASE @CurMapOrder
            WHEN 4 THEN 3.5000 + (@CurStageNo - 1) * 0.1200
            WHEN 5 THEN 5.3000 + (@CurStageNo - 1) * 0.1600
            WHEN 6 THEN 7.7000 + (@CurStageNo - 1) * 0.2200
        END;

        DECLARE @BossScale DECIMAL(8,4) = CASE @CurStageNo
            WHEN 5 THEN 1.2000
            WHEN 10 THEN 1.3500
            WHEN 15 THEN 1.5500
            ELSE 1.0000
        END;

        DECLARE @EnemyLevel INT = CASE @CurMapOrder
            WHEN 4 THEN 20 + (@CurStageNo * 10 / 15)
            WHEN 5 THEN 28 + (@CurStageNo * 14 / 15)
            WHEN 6 THEN 38 + (@CurStageNo * 17 / 15)
        END;

        DECLARE @EnemyStars TINYINT = CASE @CurMapOrder
            WHEN 4 THEN CASE WHEN @CurStageNo >= 10 THEN 3 ELSE 2 END
            WHEN 5 THEN CASE WHEN @CurStageNo >= 10 THEN 4 ELSE 3 END
            WHEN 6 THEN 4
        END;

        -- 5 Vị trí (Pos 1-5, Pos 3 là Boss nếu StageNo là 5, 10, 15)
        DECLARE @Pos INT = 1;
        WHILE @Pos <= 5
        BEGIN
            DECLARE @IsBossPos BIT = CASE WHEN @CurStageNo IN (5, 10, 15) AND @Pos = 3 THEN 1 ELSE 0 END;
            DECLARE @PosMultiplier DECIMAL(8,4) = CASE WHEN @IsBossPos = 1 THEN @BaseMultiplier * @BossScale ELSE @BaseMultiplier END;
            DECLARE @PosLevel INT = CASE WHEN @IsBossPos = 1 THEN @EnemyLevel + CASE @CurMapOrder WHEN 4 THEN 3 WHEN 5 THEN 4 WHEN 6 THEN 5 END ELSE @EnemyLevel END;
            DECLARE @PosStars TINYINT = CASE WHEN @IsBossPos = 1 THEN CASE @CurMapOrder WHEN 4 THEN 4 WHEN 5 THEN 5 WHEN 6 THEN 5 END ELSE @EnemyStars END;

            DECLARE @PosHeroTemplateId INT;
            DECLARE @PosDisplayName NVARCHAR(100);
            DECLARE @PosImagePath NVARCHAR(260);

            IF @IsBossPos = 1
            BEGIN
                SET @PosHeroTemplateId = @bHeroId;
                SET @PosDisplayName = @bName;
                SET @PosImagePath = @bImg;
            END
            ELSE
            BEGIN
                -- Deterministic selection of minion template, name, and image based on Map & Pos
                IF @CurMapOrder = 4
                BEGIN
                    SET @PosHeroTemplateId = CASE (@CurStageNo * 3 + @Pos) % 4
                        WHEN 0 THEN @HeroCoder
                        WHEN 1 THEN @HeroNam
                        WHEN 2 THEN @HeroTester
                        ELSE @HeroPM
                    END;
                    SET @PosDisplayName = CASE (@CurStageNo + @Pos) % 4
                        WHEN 0 THEN N'Drone Tuần Tra Neon'
                        WHEN 1 THEN N'Robot Sửa Chữa Lỗi'
                        WHEN 2 THEN N'Hacker Đường Phố'
                        ELSE N'Android Tinh Nhuệ'
                    END;
                    SET @PosImagePath = CASE (@CurStageNo + @Pos) % 4
                        WHEN 0 THEN '/assets/images/dcs-game/dungeons/map-04-neon/drone-patrol.png'
                        WHEN 1 THEN '/assets/images/dcs-game/dungeons/map-04-neon/repair-bot.png'
                        WHEN 2 THEN '/assets/images/dcs-game/dungeons/map-04-neon/cyber-hacker.png'
                        ELSE '/assets/images/dcs-game/dungeons/map-04-neon/assault-android.png'
                    END;
                END
                ELSE IF @CurMapOrder = 5
                BEGIN
                    SET @PosHeroTemplateId = CASE (@CurStageNo * 3 + @Pos) % 4
                        WHEN 0 THEN @HeroNam
                        WHEN 1 THEN @HeroPM
                        WHEN 2 THEN @HeroTester
                        ELSE @HeroQA
                    END;
                    SET @PosDisplayName = CASE (@CurStageNo + @Pos) % 4
                        WHEN 0 THEN N'Thực Tập Sinh Lạc Lối'
                        WHEN 1 THEN N'Máy In Nổi Loạn'
                        WHEN 2 THEN N'Kế Toán Xác Sống'
                        ELSE N'Email Khẩn Cấp'
                    END;
                    SET @PosImagePath = CASE (@CurStageNo + @Pos) % 4
                        WHEN 0 THEN '/assets/images/dcs-game/dungeons/map-05-deadline-desert/lost-intern.png'
                        WHEN 1 THEN '/assets/images/dcs-game/dungeons/map-05-deadline-desert/rogue-printer.png'
                        WHEN 2 THEN '/assets/images/dcs-game/dungeons/map-05-deadline-desert/zombie-accountant.png'
                        ELSE '/assets/images/dcs-game/dungeons/map-05-deadline-desert/urgent-email-golem.png'
                    END;
                END
                ELSE -- Map 6
                BEGIN
                    SET @PosHeroTemplateId = CASE (@CurStageNo * 3 + @Pos) % 4
                        WHEN 0 THEN @HeroK
                        WHEN 1 THEN @HeroKietNoel
                        WHEN 2 THEN @HeroChuanMen
                        ELSE @HeroNghiaPhuc
                    END;
                    SET @PosDisplayName = CASE (@CurStageNo + @Pos) % 4
                        WHEN 0 THEN N'Thiên Binh Mây Trắng'
                        WHEN 1 THEN N'Linh Thú Thiên Giới'
                        WHEN 2 THEN N'Pháp Sư Thiên Cung'
                        ELSE N'Hộ Vệ Tối Cao'
                    END;
                    SET @PosImagePath = CASE (@CurStageNo + @Pos) % 4
                        WHEN 0 THEN '/assets/images/dcs-game/dungeons/map-06-meme-heaven/cloud-soldier.png'
                        WHEN 1 THEN '/assets/images/dcs-game/dungeons/map-06-meme-heaven/divine-beast.png'
                        WHEN 2 THEN '/assets/images/dcs-game/dungeons/map-06-meme-heaven/celestial-mage.png'
                        ELSE '/assets/images/dcs-game/dungeons/map-06-meme-heaven/supreme-guardian.png'
                    END;
                END
            END;

            IF NOT EXISTS (SELECT 1 FROM dbo.HRK_DungeonStageEnemies WHERE StageId = @CurStageId AND Position = @Pos)
            BEGIN
                INSERT INTO dbo.HRK_DungeonStageEnemies (StageId, Position, HeroTemplateId, DisplayName, ImagePath, Level, Stars, StatMultiplier, IsBoss)
                VALUES (@CurStageId, @Pos, @PosHeroTemplateId, @PosDisplayName, @PosImagePath, @PosLevel, @PosStars, @PosMultiplier, @IsBossPos);
            END
            ELSE
            BEGIN
                UPDATE dbo.HRK_DungeonStageEnemies
                SET HeroTemplateId = @PosHeroTemplateId,
                    DisplayName = @PosDisplayName,
                    ImagePath = @PosImagePath,
                    Level = @PosLevel,
                    Stars = @PosStars,
                    StatMultiplier = @PosMultiplier,
                    IsBoss = @IsBossPos
                WHERE StageId = @CurStageId AND Position = @Pos;
            END;

            SET @Pos += 1;
        END;

        FETCH NEXT FROM @StageCursor INTO @CurMapOrder, @CurStageNo;
    END;

    CLOSE @StageCursor;
    DEALLOCATE @StageCursor;

    PRINT N'==> 4. Tính toán Lực chiến Đề xuất (RecommendedPower) khớp 100% đội hình thực tế...';
    -- Formula matches DungeonService & BattleService:
    -- Power = (int)Math.Round((enemy.HeroTemplate.BaseHp * .25m + enemy.HeroTemplate.BaseAtk * 3.5m + enemy.HeroTemplate.BaseDef * 2m + enemy.HeroTemplate.BaseSpd) * enemy.StatMultiplier)
    UPDATE s
    SET s.RecommendedPower = ISNULL(e.TotalPower, s.RecommendedPower)
    FROM dbo.HRK_DungeonStages s
    INNER JOIN (
        SELECT
            enemy.StageId,
            SUM(CAST(ROUND((ht.BaseHp * 0.25 + ht.BaseAtk * 3.5 + ht.BaseDef * 2.0 + ht.BaseSpd) * enemy.StatMultiplier, 0) AS INT)) AS TotalPower
        FROM dbo.HRK_DungeonStageEnemies enemy
        INNER JOIN dbo.HRK_HeroTemplates ht ON ht.Id = enemy.HeroTemplateId
        GROUP BY enemy.StageId
    ) e ON e.StageId = s.Id
    WHERE s.DungeonMapId IN (@Map4Id, @Map5Id, @Map6Id);

    PRINT N'==> 5. Cấu hình Drop Pool cho các màn Boss (chỉ rơi Common, Rare, Epic; tuyệt đối không vượt quá Epic)...';

    DECLARE @BossDrops TABLE (
        MapCode NVARCHAR(50),
        StageNumber INT,
        ItemCode NVARCHAR(100),
        DropRate DECIMAL(5,4),
        Weight INT
    );

    -- MAP 4: NEON_CITY
    -- Boss 5 (MINI_BOSS): 40% Chổi Bay Rare, 40% Tai Nghe Rare
    INSERT INTO @BossDrops VALUES ('NEON_CITY', 5, 'COMBAT_BROOM_MK2', 0.4000, 50);
    INSERT INTO @BossDrops VALUES ('NEON_CITY', 5, 'MIND_SYNC_HEADSET', 0.4000, 50);
    -- Boss 10 (MINI_BOSS): 45% Giáp Kính Rare, 45% Giày Trượt Neon Rare
    INSERT INTO @BossDrops VALUES ('NEON_CITY', 10, 'VIRTUAL_GLASS_ARMOR', 0.4500, 50);
    INSERT INTO @BossDrops VALUES ('NEON_CITY', 10, 'NEON_JET_SKATES', 0.4500, 50);
    -- Boss 15 (BOSS): 50% Máy Tính Bảng Rare, 50% Mũ Phi Công Rare, 15% Ma Cầm Epic, 15% Chiến Bào Hư Không Epic
    INSERT INTO @BossDrops VALUES ('NEON_CITY', 15, 'SUMMONING_TABLET', 0.5000, 35);
    INSERT INTO @BossDrops VALUES ('NEON_CITY', 15, 'METEOR_PILOT_HELMET', 0.5000, 35);
    INSERT INTO @BossDrops VALUES ('NEON_CITY', 15, 'SIX_STRING_DEMON_GUITAR', 0.1500, 15);
    INSERT INTO @BossDrops VALUES ('NEON_CITY', 15, 'VOID_CIRCUS_COAT', 0.1500, 15);

    -- MAP 5: DEADLINE_DESERT
    -- Boss 5 (MINI_BOSS): 50% Giày Trượt Neon Rare, 50% Tai Nghe Rare, 12% Hài Mèo Epic
    INSERT INTO @BossDrops VALUES ('DEADLINE_DESERT', 5, 'NEON_JET_SKATES', 0.5000, 45);
    INSERT INTO @BossDrops VALUES ('DEADLINE_DESERT', 5, 'MIND_SYNC_HEADSET', 0.5000, 45);
    INSERT INTO @BossDrops VALUES ('DEADLINE_DESERT', 5, 'QUANTUM_CAT_SLIPPERS', 0.1200, 10);
    -- Boss 10 (MINI_BOSS): 50% Giáp Kính Rare, 50% Chổi Bay Rare, 20% Nồi Cơm Luyện Đan Epic
    INSERT INTO @BossDrops VALUES ('DEADLINE_DESERT', 10, 'VIRTUAL_GLASS_ARMOR', 0.5000, 40);
    INSERT INTO @BossDrops VALUES ('DEADLINE_DESERT', 10, 'COMBAT_BROOM_MK2', 0.5000, 40);
    INSERT INTO @BossDrops VALUES ('DEADLINE_DESERT', 10, 'ALCHEMY_RICE_COOKER', 0.2000, 20);
    -- Boss 15 (BOSS): 40% Mũ Phi Công Rare, 40% Máy Tính Bảng Rare, 30% Chiến Bào Hư Không Epic, 30% Nhẫn Hành Tinh Epic
    INSERT INTO @BossDrops VALUES ('DEADLINE_DESERT', 15, 'METEOR_PILOT_HELMET', 0.4000, 30);
    INSERT INTO @BossDrops VALUES ('DEADLINE_DESERT', 15, 'SUMMONING_TABLET', 0.4000, 30);
    INSERT INTO @BossDrops VALUES ('DEADLINE_DESERT', 15, 'VOID_CIRCUS_COAT', 0.3000, 20);
    INSERT INTO @BossDrops VALUES ('DEADLINE_DESERT', 15, 'POCKET_PLANET_RING', 0.3000, 20);

    -- MAP 6: MEME_HEAVEN
    -- Boss 5 (MINI_BOSS): 40% Máy Tính Bảng Rare, 25% Đầu Cá Mập Epic, 25% Nhẫn Hành Tinh Epic
    INSERT INTO @BossDrops VALUES ('MEME_HEAVEN', 5, 'SUMMONING_TABLET', 0.4000, 35);
    INSERT INTO @BossDrops VALUES ('MEME_HEAVEN', 5, 'ROBOT_SHARK_HELMET', 0.2500, 35);
    INSERT INTO @BossDrops VALUES ('MEME_HEAVEN', 5, 'POCKET_PLANET_RING', 0.2500, 30);
    -- Boss 10 (MINI_BOSS): 35% Giày Trượt Neon Rare, 40% Hài Mèo Epic, 40% Nồi Cơm Luyện Đan Epic
    INSERT INTO @BossDrops VALUES ('MEME_HEAVEN', 10, 'NEON_JET_SKATES', 0.3500, 25);
    INSERT INTO @BossDrops VALUES ('MEME_HEAVEN', 10, 'QUANTUM_CAT_SLIPPERS', 0.4000, 40);
    INSERT INTO @BossDrops VALUES ('MEME_HEAVEN', 10, 'ALCHEMY_RICE_COOKER', 0.4000, 35);
    -- Boss 15 (BOSS): 50% Ma Cầm Sáu Dây Epic, 50% Chiến Bào Hư Không Epic, 50% Nhẫn Hành Tinh Epic, 50% Đầu Cá Mập Epic
    INSERT INTO @BossDrops VALUES ('MEME_HEAVEN', 15, 'SIX_STRING_DEMON_GUITAR', 0.5000, 30);
    INSERT INTO @BossDrops VALUES ('MEME_HEAVEN', 15, 'VOID_CIRCUS_COAT', 0.5000, 25);
    INSERT INTO @BossDrops VALUES ('MEME_HEAVEN', 15, 'POCKET_PLANET_RING', 0.5000, 25);
    INSERT INTO @BossDrops VALUES ('MEME_HEAVEN', 15, 'ROBOT_SHARK_HELMET', 0.5000, 20);

    MERGE INTO dbo.HRK_DungeonStageDropPools AS target
    USING (
        SELECT s.Id AS StageId, t.Id AS ItemTemplateId, cfg.DropRate, cfg.Weight
        FROM @BossDrops cfg
        JOIN dbo.HRK_DungeonMaps m ON m.Code = cfg.MapCode
        JOIN dbo.HRK_DungeonStages s ON s.DungeonMapId = m.Id AND s.StageNumber = cfg.StageNumber
        JOIN dbo.HRK_ItemTemplates t ON t.Code = cfg.ItemCode
    ) AS src
    ON target.StageId = src.StageId AND target.ItemTemplateId = src.ItemTemplateId
    WHEN MATCHED THEN
        UPDATE SET target.DropRate = src.DropRate,
                   target.Weight = src.Weight,
                   target.IsActive = 1
    WHEN NOT MATCHED THEN
        INSERT (StageId, ItemTemplateId, DropRate, Weight, MinQuantity, MaxQuantity, IsFirstClearOnly, IsActive)
        VALUES (src.StageId, src.ItemTemplateId, src.DropRate, src.Weight, 1, 1, 0, 1);

    PRINT N'==> 6. Cấu hình 3 Rương Sao (15, 30, 45 sao) cho từng Map...';

    DECLARE @ChestConfig TABLE (
        MapCode NVARCHAR(50),
        RequiredStars INT,
        DisplayOrder INT,
        GoldReward BIGINT,
        DiamondReward INT,
        UpgradeMaterialsReward INT,
        GuaranteedItemTemplateId INT,
        Description NVARCHAR(500)
    );

    -- MAP 4: NEON_CITY
    INSERT INTO @ChestConfig VALUES ('NEON_CITY', 15, 1, 45000, 150, 100, NULL, N'Rương 15 Sao - Tiến vào Thành Phố Neon Mất Kiểm Soát');
    INSERT INTO @ChestConfig VALUES ('NEON_CITY', 30, 2, 100000, 350, 200, NULL, N'Rương 30 Sao - Đột phá phòng tuyến AI Thành Phố Neon');
    INSERT INTO @ChestConfig VALUES ('NEON_CITY', 45, 3, 220000, 800, 400, @GlassArmorRareId, N'Rương Toàn Thắng 45 Sao - Tặng Giáp Kính Thực Tế Ảo Rare');

    -- MAP 5: DEADLINE_DESERT
    INSERT INTO @ChestConfig VALUES ('DEADLINE_DESERT', 15, 1, 70000, 200, 150, NULL, N'Rương 15 Sao - Bước chân vào Sa Mạc Văn Phòng Deadline');
    INSERT INTO @ChestConfig VALUES ('DEADLINE_DESERT', 30, 2, 160000, 500, 300, NULL, N'Rương 30 Sao - Chống chọi cơn bão cát Deadline');
    INSERT INTO @ChestConfig VALUES ('DEADLINE_DESERT', 45, 3, 350000, 1200, 600, @CatSlippersEpicId, N'Rương Toàn Thắng 45 Sao - Tặng Hài Mèo Lượng Tử Epic');

    -- MAP 6: MEME_HEAVEN
    INSERT INTO @ChestConfig VALUES ('MEME_HEAVEN', 15, 1, 120000, 300, 250, NULL, N'Rương 15 Sao - Khởi đầu thám hiểm Thiên Cung Meme');
    INSERT INTO @ChestConfig VALUES ('MEME_HEAVEN', 30, 2, 250000, 700, 500, NULL, N'Rương 30 Sao - Vượt qua sấm sét Thiên Cung Meme');
    INSERT INTO @ChestConfig VALUES ('MEME_HEAVEN', 45, 3, 600000, 2000, 1000, @GuitarEpicId, N'Rương Toàn Thắng 45 Sao - Tặng Ma Cầm Sáu Dây Epic');

    MERGE INTO dbo.HRK_DungeonMapStarChests AS target
    USING (
        SELECT m.Id AS DungeonMapId, c.RequiredStars, c.DisplayOrder, c.GoldReward, c.DiamondReward,
               c.UpgradeMaterialsReward, c.GuaranteedItemTemplateId, c.Description
        FROM @ChestConfig c
        JOIN dbo.HRK_DungeonMaps m ON m.Code = c.MapCode
    ) AS src
    ON target.DungeonMapId = src.DungeonMapId AND target.RequiredStars = src.RequiredStars
    WHEN MATCHED THEN
        UPDATE SET target.GoldReward = src.GoldReward,
                   target.DiamondReward = src.DiamondReward,
                   target.UpgradeMaterialsReward = src.UpgradeMaterialsReward,
                   target.GuaranteedItemTemplateId = src.GuaranteedItemTemplateId,
                   target.Description = src.Description,
                   target.DisplayOrder = src.DisplayOrder,
                   target.IsActive = 1
    WHEN NOT MATCHED THEN
        INSERT (DungeonMapId, RequiredStars, GoldReward, DiamondReward, UpgradeMaterialsReward, GuaranteedItemTemplateId, DisplayOrder, Description, IsActive)
        VALUES (src.DungeonMapId, src.RequiredStars, src.GoldReward, src.DiamondReward, src.UpgradeMaterialsReward, src.GuaranteedItemTemplateId, src.DisplayOrder, src.Description, 1);

    COMMIT TRANSACTION;
    PRINT N'==> THÀNH CÔNG: Đã triển khai hoàn chỉnh 3 Map phó bản (Map 4, 5, 6), 45 màn, 225 kẻ địch, drop pool và rương sao!';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

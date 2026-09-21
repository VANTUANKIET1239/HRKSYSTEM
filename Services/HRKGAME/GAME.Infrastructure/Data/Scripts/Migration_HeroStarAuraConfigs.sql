/*
    Hero Star Aura System Migration.
    Creates dbo.HRK_HeroStarAuraConfigs and seeds 40 records (10 heroes x 4 star levels: 2, 3, 4, 5).
    Idempotent and safe to run multiple times.
*/
USE [HRK];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.HRK_HeroStarAuraConfigs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HRK_HeroStarAuraConfigs
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        HeroTemplateId INT NOT NULL,
        StarLevel TINYINT NOT NULL,
        AuraCode NVARCHAR(100) NOT NULL,
        VisualKey NVARCHAR(100) NOT NULL,
        Name NVARCHAR(150) NOT NULL,
        Description NVARCHAR(500) NULL,
        PrimaryColorHex VARCHAR(9) NULL,
        SecondaryColorHex VARCHAR(9) NULL,
        Intensity DECIMAL(5,2) NOT NULL
            CONSTRAINT DF_HRK_HeroStarAuraConfigs_Intensity DEFAULT (1),
        ParticleLevel TINYINT NOT NULL
            CONSTRAINT DF_HRK_HeroStarAuraConfigs_ParticleLevel DEFAULT (1),
        IsActive BIT NOT NULL
            CONSTRAINT DF_HRK_HeroStarAuraConfigs_IsActive DEFAULT (1),
        CreatedOn DATETIME2 NOT NULL
            CONSTRAINT DF_HRK_HeroStarAuraConfigs_CreatedOn DEFAULT (SYSUTCDATETIME()),
        UpdatedOn DATETIME2 NOT NULL
            CONSTRAINT DF_HRK_HeroStarAuraConfigs_UpdatedOn DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_HRK_HeroStarAuraConfigs
            PRIMARY KEY CLUSTERED (Id),

        CONSTRAINT FK_HRK_HeroStarAuraConfigs_HeroTemplate
            FOREIGN KEY (HeroTemplateId)
            REFERENCES dbo.HRK_HeroTemplates(Id),

        CONSTRAINT UQ_HRK_HeroStarAuraConfigs_HeroStar
            UNIQUE (HeroTemplateId, StarLevel),

        CONSTRAINT UQ_HRK_HeroStarAuraConfigs_AuraCode
            UNIQUE (AuraCode),

        CONSTRAINT CK_HRK_HeroStarAuraConfigs_StarLevel
            CHECK (StarLevel BETWEEN 2 AND 5),

        CONSTRAINT CK_HRK_HeroStarAuraConfigs_Intensity
            CHECK (Intensity > 0),

        CONSTRAINT CK_HRK_HeroStarAuraConfigs_ParticleLevel
            CHECK (ParticleLevel BETWEEN 1 AND 4)
    );
END;

MERGE dbo.HRK_HeroStarAuraConfigs AS target
USING (VALUES
    -- Hero 1: K Cởi Trần (kiet-red-lightning)
    (1, CAST(2 AS TINYINT), N'KIET_STAR_2', N'kiet-red-lightning', N'Xích Lôi Khởi Phát', N'Aura nhiệt hỏa sơ khởi, tia điện đỏ phát ra dưới chân.', '#e11d48', '#f59e0b', CAST(0.85 AS DECIMAL(5,2)), CAST(1 AS TINYINT), 1),
    (1, CAST(3 AS TINYINT), N'KIET_STAR_3', N'kiet-red-lightning', N'Hồng Lôi Cuồng Nộ', N'Tầng sấm sét đỏ trung cấp bao bọc cơ bắp và phóng các tia điện ngắn.', '#e11d48', '#f59e0b', CAST(1.30 AS DECIMAL(5,2)), CAST(2 AS TINYINT), 1),
    (1, CAST(4 AS TINYINT), N'KIET_STAR_4', N'kiet-red-lightning', N'Xích Lôi Thần Khí', N'Vòng sấm điện đỏ vàng xoay quanh thân thể kèm xung lực năng lượng dâng trào.', '#e11d48', '#fbbf24', CAST(1.75 AS DECIMAL(5,2)), CAST(3 AS TINYINT), 1),
    (1, CAST(5 AS TINYINT), N'KIET_STAR_5', N'kiet-red-lightning', N'Chí Tôn Lôi Thần', N'Song vòng lôi quang cực đại, cổ văn sức mạnh hoàng kim và các chuỗi tia sét chấn động.', '#e11d48', '#fef08a', CAST(2.20 AS DECIMAL(5,2)), CAST(4 AS TINYINT), 1),

    -- Hero 2: Nam Deadline (nam-deadline-sterile-pulse)
    (2, CAST(2 AS TINYINT), N'NAM_DEADLINE_STAR_2', N'nam-deadline-sterile-pulse', N'Khử Trùng Khởi Nguyên', N'Vòng hào quang lam ngọc dịu nhẹ tỏa sáng dưới chân.', '#06b6d4', '#e0f2fe', CAST(0.85 AS DECIMAL(5,2)), CAST(1 AS TINYINT), 1),
    (2, CAST(3 AS TINYINT), N'NAM_DEADLINE_STAR_3', N'nam-deadline-sterile-pulse', N'Vùng Vô Trùng Tinh Khiết', N'Sóng xung kích quét khử khuẩn liên tục cùng hạt phân tử nano bay lượn.', '#06b6d4', '#bae6fd', CAST(1.30 AS DECIMAL(5,2)), CAST(2 AS TINYINT), 1),
    (2, CAST(4 AS TINYINT), N'NAM_DEADLINE_STAR_4', N'nam-deadline-sterile-pulse', N'Dược Thể Cao Tốc', N'Quỹ đạo ống tiêm mini xoay quanh người cùng xung lực khử khuẩn cyan-trắng chớp sáng.', '#0891b2', '#ffffff', CAST(1.75 AS DECIMAL(5,2)), CAST(3 AS TINYINT), 1),
    (2, CAST(5 AS TINYINT), N'NAM_DEADLINE_STAR_5', N'nam-deadline-sterile-pulse', N'Thần Y Tuyệt Đối', N'Hai vòng tròn y tế xoay nghịch hướng, phù hiệu chữ thập vô trùng và màn sương hồi phục rực rỡ.', '#06b6d4', '#ffffff', CAST(2.20 AS DECIMAL(5,2)), CAST(4 AS TINYINT), 1),

    -- Hero 3: Chuẩn Men (chuan-men-crimson-rhythm)
    (3, CAST(2 AS TINYINT), N'CHUAN_MEN_STAR_2', N'chuan-men-crimson-rhythm', N'Nhịp Điệu Sơ Khởi', N'Quầng sáng đỏ crimson rung cảm nhẹ nhàng quanh bước chân.', '#dc2626', '#9333ea', CAST(0.85 AS DECIMAL(5,2)), CAST(1 AS TINYINT), 1),
    (3, CAST(3 AS TINYINT), N'CHUAN_MEN_STAR_3', N'chuan-men-crimson-rhythm', N'Xích Huyết Cuồng Vũ', N'Tia sét đỏ phóng thích theo từng nhịp đập nhịp nhàng quyến rũ.', '#dc2626', '#7e22ce', CAST(1.30 AS DECIMAL(5,2)), CAST(2 AS TINYINT), 1),
    (3, CAST(4 AS TINYINT), N'CHUAN_MEN_STAR_4', N'chuan-men-crimson-rhythm', N'Vũ Điệu Hồng Ngọc', N'Vòng nhịp điệu đỏ rực xoay quanh kết hợp các tinh thể kim cương lấp lánh.', '#ef4444', '#a855f7', CAST(1.75 AS DECIMAL(5,2)), CAST(3 AS TINYINT), 1),
    (3, CAST(5 AS TINYINT), N'CHUAN_MEN_STAR_5', N'chuan-men-crimson-rhythm', N'Tuyệt Đỉnh Nam Nhi', N'Song quang hoàn huyết sắc, sóng xung kích nhịp điệu bùng nổ cuốn hút mọi ánh nhìn.', '#b91c1c', '#c084fc', CAST(2.20 AS DECIMAL(5,2)), CAST(4 AS TINYINT), 1),

    -- Hero 4: Coder Bảnh (coder-banh-digital-knowledge)
    (4, CAST(2 AS TINYINT), N'CODER_BANH_STAR_2', N'coder-banh-digital-knowledge', N'Ký Tự Nhị Phân', N'Các ký tự code và hạt pixel nhỏ màu xanh bay lên.', '#10b981', '#06b6d4', CAST(0.85 AS DECIMAL(5,2)), CAST(1 AS TINYINT), 1),
    (4, CAST(3 AS TINYINT), N'CODER_BANH_STAR_3', N'coder-banh-digital-knowledge', N'Vòng Lặp Kỹ Thuật Số', N'Vòng tròn ma trận số xoay quanh cùng các trang code lơ lửng.', '#10b981', '#3b82f6', CAST(1.30 AS DECIMAL(5,2)), CAST(2 AS TINYINT), 1),
    (4, CAST(4 AS TINYINT), N'CODER_BANH_STAR_4', N'coder-banh-digital-knowledge', N'Quỹ Đạo Thuật Toán', N'Quỹ đạo ma trận số hóa, luồng dữ liệu orbit xoay tít và các mảnh pixel sáng chói.', '#059669', '#60a5fa', CAST(1.75 AS DECIMAL(5,2)), CAST(3 AS TINYINT), 1),
    (4, CAST(5 AS TINYINT), N'CODER_BANH_STAR_5', N'coder-banh-digital-knowledge', N'Cổ Máy Kiến Thức Vô Tận', N'Hai vòng ma trận đối xứng, hologram tri thức ảo và trụ cột luồng dữ liệu trồi lên uy nghi.', '#10b981', '#93c5fd', CAST(2.20 AS DECIMAL(5,2)), CAST(4 AS TINYINT), 1),

    -- Hero 5: Tester Đẹp (tester-dep-obsidian-nebula)
    (5, CAST(2 AS TINYINT), N'TESTER_DEP_STAR_2', N'tester-dep-obsidian-nebula', N'Sương Mù Hắc Diệu', N'Màn sương tím đen hư không nhẹ nhàng lan tỏa quanh thân.', '#a855f7', '#1e1b4b', CAST(0.85 AS DECIMAL(5,2)), CAST(1 AS TINYINT), 1),
    (5, CAST(3 AS TINYINT), N'TESTER_DEP_STAR_3', N'tester-dep-obsidian-nebula', N'Màn Che Tinh Vân', N'Màn che tinh vân hắc diệu thạch với các xúc tu tử quang và hạt bóng tối huyền bí.', '#9333ea', '#0f172a', CAST(1.30 AS DECIMAL(5,2)), CAST(2 AS TINYINT), 1),
    (5, CAST(4 AS TINYINT), N'TESTER_DEP_STAR_4', N'tester-dep-obsidian-nebula', N'Quỹ Đạo Hư Không', N'Vòng tinh vân tím đen xoay chuyển với quỹ đạo các mảnh vỡ pha lê bóng tối.', '#a855f7', '#3b0764', CAST(1.75 AS DECIMAL(5,2)), CAST(3 AS TINYINT), 1),
    (5, CAST(5 AS TINYINT), N'TESTER_DEP_STAR_5', N'tester-dep-obsidian-nebula', N'Chúa Tể Vực Sâu', N'Hai tầng tinh vân đối lưu, bão mảnh hắc diệu thạch và cổ tự vực sâu phát quang ma mị.', '#c084fc', '#581c87', CAST(2.20 AS DECIMAL(5,2)), CAST(4 AS TINYINT), 1),

    -- Hero 6: Tướng Long Quân Đội (tuong-long-scorched-command)
    (6, CAST(2 AS TINYINT), N'TUONG_LONG_STAR_2', N'tuong-long-scorched-command', N'Tàn Lửa Chiến Hào', N'Than hồng âm ỉ và bụi đất chiến trường bốc lên dưới chân.', '#f97316', '#eab308', CAST(0.85 AS DECIMAL(5,2)), CAST(1 AS TINYINT), 1),
    (6, CAST(3 AS TINYINT), N'TUONG_LONG_STAR_3', N'tuong-long-scorched-command', N'Thiêu Đốt Chiến Địa', N'Địa chấn nhiệt hỏa nung đỏ mặt đất, khe nứt dung nham và tàn tro rực cháy.', '#ea580c', '#ca8a04', CAST(1.30 AS DECIMAL(5,2)), CAST(2 AS TINYINT), 1),
    (6, CAST(4 AS TINYINT), N'TUONG_LONG_STAR_4', N'tuong-long-scorched-command', N'Quân Lệnh Thiết Huyết', N'Vòng quân lệnh rực lửa, hạt vỏ đạn vàng xoay quanh và các đợt sóng xung kích nhiệt.', '#f97316', '#dc2626', CAST(1.75 AS DECIMAL(5,2)), CAST(3 AS TINYINT), 1),
    (6, CAST(5 AS TINYINT), N'TUONG_LONG_STAR_5', N'tuong-long-scorched-command', N'Bất Diệt Thần Tướng', N'Phù hiệu quân đoàn rực lửa trên không, song hoàn hỏa diệm và bão than lửa ngút trời.', '#fb923c', '#facc15', CAST(2.20 AS DECIMAL(5,2)), CAST(4 AS TINYINT), 1),

    -- Hero 7: PM Hối Hả (pm-hoi-ha-spectral-grooming)
    (7, CAST(2 AS TINYINT), N'PM_HOI_HA_STAR_2', N'pm-hoi-ha-spectral-grooming', N'Lưỡi Gió Cắt Tỉa', N'Những tia cắt khí lam ngọc nhẹ nhàng và các sợi tóc năng lượng bay lượn.', '#14b8a6', '#8b5cf6', CAST(0.85 AS DECIMAL(5,2)), CAST(1 AS TINYINT), 1),
    (7, CAST(3 AS TINYINT), N'PM_HOI_HA_STAR_3', N'pm-hoi-ha-spectral-grooming', N'Bầy Kéo Ma Thuật', N'Vortex cắt tỉa ma thuật với các đường kéo chém lướt sắc lẹm.', '#0d9488', '#7c3aed', CAST(1.30 AS DECIMAL(5,2)), CAST(2 AS TINYINT), 1),
    (7, CAST(4 AS TINYINT), N'PM_HOI_HA_STAR_4', N'pm-hoi-ha-spectral-grooming', N'Quỹ Đạo Hoàng Gia', N'Kéo ma thuật bay theo quỹ đạo xoay quanh người, tạo nên lốc xoáy tóc lấp lánh.', '#14b8a6', '#a78bfa', CAST(1.75 AS DECIMAL(5,2)), CAST(3 AS TINYINT), 1),
    (7, CAST(5 AS TINYINT), N'PM_HOI_HA_STAR_5', N'pm-hoi-ha-spectral-grooming', N'Đại Sư Tạo Mẫu', N'Vòng nguyệt quế tiệm tóc phát sáng, hai tầng lưỡi kéo xoay cực tốc và lốc xoáy lam tử tráng lệ.', '#2dd4bf', '#c4b5fd', CAST(2.20 AS DECIMAL(5,2)), CAST(4 AS TINYINT), 1),

    -- Hero 8: QA Kỹ Tính (qa-ky-tinh-vicious-debt)
    (8, CAST(2 AS TINYINT), N'QA_KY_TINH_STAR_2', N'qa-ky-tinh-vicious-debt', N'Tiền Tệ Lăn Lốc', N'Một vài đồng xu hoàng kim và lá bài ma thuật xoay nhẹ quanh chân.', '#ef4444', '#eab308', CAST(0.85 AS DECIMAL(5,2)), CAST(1 AS TINYINT), 1),
    (8, CAST(3 AS TINYINT), N'QA_KY_TINH_STAR_3', N'qa-ky-tinh-vicious-debt', N'Vòng Xoáy Nợ Nần', N'Vòng xoáy bài bạc và nợ nần rực rỡ, lá bài ma thuật và tiền vàng cuộn xoay dữ dội.', '#dc2626', '#ca8a04', CAST(1.30 AS DECIMAL(5,2)), CAST(2 AS TINYINT), 1),
    (8, CAST(4 AS TINYINT), N'QA_KY_TINH_STAR_4', N'qa-ky-tinh-vicious-debt', N'Thị Trường Biến Động', N'Chip casino xoay quanh quỹ đạo, nến thị trường xanh đỏ nhấp nháy cùng đai kim sắc.', '#ef4444', '#fde047', CAST(1.75 AS DECIMAL(5,2)), CAST(3 AS TINYINT), 1),
    (8, CAST(5 AS TINYINT), N'QA_KY_TINH_STAR_5', N'qa-ky-tinh-vicious-debt', N'Chúa Tể Sòng Bạc', N'Bão lốc casino vĩ đại, phù hiệu Jackpot hoàng kim rực sáng và mưa tiền tài cục bộ đỉnh cao.', '#b91c1c', '#fef08a', CAST(2.20 AS DECIMAL(5,2)), CAST(4 AS TINYINT), 1),

    -- Hero 9: Kiet Noel (kiet-noel-dark-blizzard)
    (9, CAST(2 AS TINYINT), N'KIET_NOEL_STAR_2', N'kiet-noel-dark-blizzard', N'Tuyết Rơi Giá Lạnh', N'Bụi tuyết xanh băng giá nhẹ nhàng rơi quanh gót chân.', '#38bdf8', '#7c3aed', CAST(0.85 AS DECIMAL(5,2)), CAST(1 AS TINYINT), 1),
    (9, CAST(3 AS TINYINT), N'KIET_NOEL_STAR_3', N'kiet-noel-dark-blizzard', N'Bão Tuyết Bóng Tối', N'Dòng lốc tuyết bóng tối buốt giá kết hợp hộp quà kỳ bí lướt quanh.', '#0284c7', '#6d28d9', CAST(1.30 AS DECIMAL(5,2)), CAST(2 AS TINYINT), 1),
    (9, CAST(4 AS TINYINT), N'KIET_NOEL_STAR_4', N'kiet-noel-dark-blizzard', N'Cực Quang Bắc Cực', N'Vòng cực quang tím xanh ảo diệu, ánh sao quà lấp lánh và tuyết băng nhiều lớp.', '#38bdf8', '#a855f7', CAST(1.75 AS DECIMAL(5,2)), CAST(3 AS TINYINT), 1),
    (9, CAST(5 AS TINYINT), N'KIET_NOEL_STAR_5', N'kiet-noel-dark-blizzard', N'Giáng Sinh Hắc Ám', N'Song vòng tuyết băng huyền ảo, cực quang vực thẳm và cỗ xe trượt tuyết năng lượng tuần hoàn.', '#7dd3fc', '#c084fc', CAST(2.20 AS DECIMAL(5,2)), CAST(4 AS TINYINT), 1),

    -- Hero 10: Hoàng Nguyên (hoang-nguyen-heavy-iron)
    (10, CAST(2 AS TINYINT), N'HOANG_NGUYEN_STAR_2', N'hoang-nguyen-heavy-iron', N'Trọng Lực Thô Sơ', N'Vòng từ trường hoàng kim nhẹ và vài mảnh đá vỡ lơ lửng.', '#eab308', '#64748b', CAST(0.85 AS DECIMAL(5,2)), CAST(1 AS TINYINT), 1),
    (10, CAST(3 AS TINYINT), N'HOANG_NGUYEN_STAR_3', N'hoang-nguyen-heavy-iron', N'Hào Quang Thiết Giáp', N'Vòng năng lượng trọng lực vàng óng nén chặt không gian, cổ tự chấn động xuất hiện.', '#ca8a04', '#475569', CAST(1.30 AS DECIMAL(5,2)), CAST(2 AS TINYINT), 1),
    (10, CAST(4 AS TINYINT), N'HOANG_NGUYEN_STAR_4', N'hoang-nguyen-heavy-iron', N'Thiết Phiến Vòng Xoay', N'Đĩa tạ năng lượng xoay quanh quỹ đạo và từng nhịp áp suất nén mặt đất rền vang.', '#eab308', '#94a3b8', CAST(1.75 AS DECIMAL(5,2)), CAST(3 AS TINYINT), 1),
    (10, CAST(5 AS TINYINT), N'HOANG_NGUYEN_STAR_5', N'hoang-nguyen-heavy-iron', N'Chí Tôn Trọng Lực', N'Song thiết hoàn hoàng kim rực sáng, rune đòn tạ uy nghiêm và không gian trọng lực bẻ cong vĩ đại.', '#facc15', '#f1f5f9', CAST(2.20 AS DECIMAL(5,2)), CAST(4 AS TINYINT), 1)
) AS source(HeroTemplateId, StarLevel, AuraCode, VisualKey, Name, Description, PrimaryColorHex, SecondaryColorHex, Intensity, ParticleLevel, IsActive)
ON target.HeroTemplateId = source.HeroTemplateId AND target.StarLevel = source.StarLevel
WHEN MATCHED THEN UPDATE SET
    AuraCode = source.AuraCode,
    VisualKey = source.VisualKey,
    Name = source.Name,
    Description = source.Description,
    PrimaryColorHex = source.PrimaryColorHex,
    SecondaryColorHex = source.SecondaryColorHex,
    Intensity = source.Intensity,
    ParticleLevel = source.ParticleLevel,
    IsActive = source.IsActive,
    UpdatedOn = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (HeroTemplateId, StarLevel, AuraCode, VisualKey, Name, Description, PrimaryColorHex, SecondaryColorHex, Intensity, ParticleLevel, IsActive)
    VALUES (source.HeroTemplateId, source.StarLevel, source.AuraCode, source.VisualKey, source.Name, source.Description, source.PrimaryColorHex, source.SecondaryColorHex, source.Intensity, source.ParticleLevel, source.IsActive);

COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

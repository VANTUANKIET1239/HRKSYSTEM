-- Apply to the GAME database before deploying the modal-config API.
-- Rerunnable: existing operator edits and inactive presets are preserved.
SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID('dbo.HRK_ModalConfigs', 'U') IS NULL
        CREATE TABLE dbo.HRK_ModalConfigs (
            Id INT IDENTITY PRIMARY KEY,
            Code NVARCHAR(100) NOT NULL,
            Kind NVARCHAR(20) NOT NULL DEFAULT 'CONTENT',
            Title NVARCHAR(200) NOT NULL,
            MessageTemplate NVARCHAR(2000) NULL,
            ConfirmText NVARCHAR(60) NOT NULL DEFAULT N'Xác nhận',
            CancelText NVARCHAR(60) NOT NULL DEFAULT N'Hủy',
            IconKey NVARCHAR(60) NULL,
            Size NVARCHAR(20) NOT NULL DEFAULT 'MD',
            Tone NVARCHAR(20) NOT NULL DEFAULT 'DEFAULT',
            CloseOnBackdrop BIT NOT NULL DEFAULT 1,
            CloseOnEscape BIT NOT NULL DEFAULT 1,
            IsActive BIT NOT NULL DEFAULT 1,
            Revision INT NOT NULL DEFAULT 1,
            UpdatedOn DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            CONSTRAINT UQ_HRK_ModalConfigs_Code UNIQUE(Code),
            CONSTRAINT CK_HRK_ModalConfigs_Kind CHECK(Kind IN ('CONFIRMATION','CONTENT','NOTICE')),
            CONSTRAINT CK_HRK_ModalConfigs_Size CHECK(Size IN ('SM','MD','LG','FULLSCREEN')),
            CONSTRAINT CK_HRK_ModalConfigs_Tone CHECK(Tone IN ('DEFAULT','WARNING','DANGER','SUCCESS')),
            CONSTRAINT CK_HRK_ModalConfigs_Revision CHECK(Revision > 0)
        );

    INSERT dbo.HRK_ModalConfigs(Code,Kind,Title,MessageTemplate,ConfirmText,CancelText,IconKey,Size,Tone,CloseOnBackdrop,CloseOnEscape)
    SELECT v.Code,v.Kind,v.Title,v.MessageTemplate,v.ConfirmText,v.CancelText,v.IconKey,v.Size,v.Tone,v.CloseOnBackdrop,v.CloseOnEscape
    FROM (VALUES
        ('LOGOUT_CONFIRM','CONFIRMATION',N'Đăng xuất khỏi trò chơi?',N'Bạn có chắc muốn kết thúc phiên đăng nhập hiện tại?',N'Đăng xuất',N'Ở lại','logout','SM','WARNING',1,1),
        ('BATTLE_SKIP_CONFIRM','CONFIRMATION',N'Bỏ qua trận đấu',N'Bạn có chắc muốn bỏ qua phần trình diễn và xem ngay kết quả trận đấu?',N'Bỏ qua',N'Tiếp tục xem','skip','SM','DEFAULT',1,1),
        ('TOWER_START_NEW_RUN','CONFIRMATION',N'Bắt đầu lượt leo mới',N'Bạn có chắc muốn bắt đầu lượt leo mới với số mạng ban đầu?',N'Bắt đầu',N'Hủy','tower','SM','WARNING',1,1),
        ('TOWER_START_QUICK_CLIMB','CONFIRMATION',N'Bắt đầu leo nhanh',N'Bắt đầu lượt leo nhanh {used}/{limit} từ Tầng 1? Phần thưởng các tầng thắng được cộng dồn; lượt leo dừng tại trận thua đầu tiên.',N'Bắt đầu',N'Hủy','tower','SM','DEFAULT',1,1),
        ('SUMMON_INFO','CONTENT',N'Thông tin chiêu mộ',NULL,N'Xác nhận',N'Hủy','info','LG','DEFAULT',1,1),
        ('SUMMON_RESULT','CONTENT',N'Chiêu mộ thành công',NULL,N'Xác nhận',N'Hủy','reward','LG','SUCCESS',1,1),
        ('AVATAR_SELECTOR','CONTENT',N'Ảnh đại diện',NULL,N'Xác nhận',N'Hủy','info','LG','DEFAULT',1,1),
        ('CAMPAIGN_STAGE_DETAIL','CONTENT',N'Thông tin khiêu chiến',NULL,N'Xác nhận',N'Hủy','info','LG','DEFAULT',1,1),
        ('BATTLE_FORMATION_SETUP','CONTENT',N'Chuẩn bị xuất chiến',NULL,N'Xác nhận',N'Hủy','hero','LG','DEFAULT',1,1),
        ('TOWER_QUICK_CLIMB','CONTENT',N'Leo tháp nhanh',NULL,N'Xác nhận',N'Hủy','tower','LG','DEFAULT',0,1),
        ('BATTLE_RESULT','CONTENT',N'Kết quả trận đấu',NULL,N'Xác nhận',N'Hủy','reward','LG','DEFAULT',0,0),
        ('CAMPAIGN_CHEST_PREVIEW','CONTENT',N'Rương báu',NULL,N'Xác nhận',N'Hủy','reward','MD','DEFAULT',1,1),
        ('CAMPAIGN_CHEST_REWARD','CONTENT',N'Nhận thưởng rương báu thành công',NULL,N'Xác nhận',N'Hủy','reward','MD','SUCCESS',1,1),
        ('HERO_SLOT_ACTION','CONTENT',N'Trang bị võ tướng',NULL,N'Xác nhận',N'Hủy','hero','MD','DEFAULT',1,1),
        ('HERO_UNEQUIP_ALL','CONTENT',N'Tháo toàn bộ trang bị',NULL,N'Xác nhận',N'Hủy','hero','MD','WARNING',1,1),
        ('HERO_SWAP_EQUIPMENT','CONTENT',N'Hoán đổi trang bị',NULL,N'Xác nhận',N'Hủy','hero','LG','DEFAULT',1,1),
        ('HERO_MANAGEMENT','CONTENT',N'Võ tướng',NULL,N'Xác nhận',N'Hủy','hero','FULLSCREEN','DEFAULT',0,1),
        ('INVENTORY','CONTENT',N'Hành trang',NULL,N'Xác nhận',N'Hủy','info','FULLSCREEN','DEFAULT',0,1),
        ('FORMATION_MANAGEMENT','CONTENT',N'Trận pháp',NULL,N'Xác nhận',N'Hủy','hero','FULLSCREEN','DEFAULT',0,1),
        ('FORGE','CONTENT',N'Lò rèn',NULL,N'Xác nhận',N'Hủy','info','FULLSCREEN','DEFAULT',0,1),
        ('LIBRARY','CONTENT',N'Thư viện',NULL,N'Xác nhận',N'Hủy','info','FULLSCREEN','DEFAULT',0,1),
        ('SUMMON_REEL','CONTENT',N'Chiêu mộ võ tướng',NULL,N'Xác nhận',N'Hủy','hero','FULLSCREEN','DEFAULT',0,1),
        ('FORGE_STONE_PICKER','CONTENT',N'Chọn đá cường hóa',NULL,N'Xác nhận',N'Hủy','info','LG','DEFAULT',1,1),
        ('FORGE_CHARM_PICKER','CONTENT',N'Chọn bùa hộ mệnh / may mắn',NULL,N'Xác nhận',N'Hủy','info','LG','DEFAULT',1,1),
        ('FORGE_DOWNGRADE','CONTENT',N'Hạ cấp trang bị',NULL,N'Xác nhận',N'Hủy','info','MD','WARNING',1,1),
        ('FORGE_RESULT','CONTENT',N'Kết quả cường hóa',NULL,N'Xác nhận',N'Hủy','info','MD','DEFAULT',1,1)
    ) v(Code,Kind,Title,MessageTemplate,ConfirmText,CancelText,IconKey,Size,Tone,CloseOnBackdrop,CloseOnEscape)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.HRK_ModalConfigs c WITH (UPDLOCK,HOLDLOCK) WHERE c.Code=v.Code);
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

-- Canonical game asset keys. Apply after legacy seed scripts and after deploying the compatible UI.
-- Mapping source: HrkUi/src/assets/catalog/asset-paths.json
-- Rerunnable; updates image columns only. External URLs, custom-avatar bytes, and historical JSON are preserved.
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    CREATE TABLE #HrkAssetAliases (OldKey NVARCHAR(500) COLLATE Latin1_General_100_BIN2 PRIMARY KEY, NewKey NVARCHAR(500) NOT NULL);
    INSERT INTO #HrkAssetAliases (OldKey, NewKey) VALUES
    (N'images/dcs-game/cau-vang-mat-lanh-epic.png', N'images/dcs-game/heroes/cau-vang-mat-lanh-epic.png'),
    (N'images/dcs-game/HoangNguyen-Gymer.png', N'images/dcs-game/heroes/hoang-nguyen-gymer.png'),
    (N'images/dcs-game/hongtam.png', N'images/dcs-game/effects/crosshair.png'),
    (N'images/dcs-game/hopqua-bth.png', N'images/dcs-game/effects/normal-gift.png'),
    (N'images/dcs-game/hopqua-special.png', N'images/dcs-game/effects/special-gift.png'),
    (N'images/dcs-game/kiet-bac-si-epic.png', N'images/dcs-game/heroes/kiet-bac-si-epic.png'),
    (N'images/dcs-game/kiet-bac-si.png', N'images/dcs-game/heroes/kiet-bac-si.png'),
    (N'images/dcs-game/kiet-mai-xeo.jpg', N'images/dcs-game/heroes/kiet-mai-xeo.jpg'),
    (N'images/dcs-game/kiet.png', N'images/dcs-game/heroes/kiet.png'),
    (N'images/dcs-game/lobby_bg.png', N'images/dcs-game/backgrounds/lobby-bg.png'),
    (N'images/dcs-game/long-le-con-meo.jpg', N'images/dcs-game/heroes/long-le-con-meo.jpg'),
    (N'images/dcs-game/meme-cho-hai-huoc.jpg', N'images/dcs-game/heroes/meme-cho-hai-huoc.jpg'),
    (N'images/dcs-game/nghia-phuc-prime.png', N'images/dcs-game/heroes/nghia-phuc-prime.png'),
    (N'images/dcs-game/nghiaphuc-bongtoi.png', N'images/dcs-game/heroes/nghiaphuc-bongtoi.png'),
    (N'images/dcs-game/nguyen-xam-lon-rare.png', N'images/dcs-game/heroes/nguyen-xam-lon-rare.png'),
    (N'images/dcs-game/nguyen-xam-lon.png', N'images/dcs-game/heroes/nguyen-xam-lon.png'),
    (N'images/dcs-game/nhan-cuoi-xam-lon.jpg', N'images/dcs-game/heroes/nhan-cuoi-xam-lon.jpg'),
    (N'images/dcs-game/quang-vinh-thcs-rare.png', N'images/dcs-game/heroes/quang-vinh-thcs-rare.png'),
    (N'images/dcs-game/quang-vinh-thcs.png', N'images/dcs-game/heroes/quang-vinh-thcs.png'),
    (N'images/dcs-game/quangvinh-barber.png', N'images/dcs-game/heroes/quangvinh-barber.png'),
    (N'images/dcs-game/quoc-nhan-chay-ngay-di.png', N'images/dcs-game/heroes/quoc-nhan-chay-ngay-di.png'),
    (N'images/dcs-game/quoc-nhan-fake.png', N'images/dcs-game/heroes/quoc-nhan-fake.png'),
    (N'images/dcs-game/quoc-nhan-gia-dien-epic.png', N'images/dcs-game/heroes/quoc-nhan-gia-dien-epic.png'),
    (N'images/dcs-game/quoc-nhan-tot-nghiep-cap-3.png', N'images/dcs-game/heroes/quoc-nhan-tot-nghiep-cap-3.png'),
    (N'images/dcs-game/ricardo-milos.gif', N'images/dcs-game/skill-vfx/ricardo/ricardo-dance.gif'),
    (N'images/dcs-game/ricardo-milos.png', N'images/dcs-game/heroes/ricardo-milos.png'),
    (N'images/dcs-game/sach-bongtoi.png', N'images/dcs-game/skill-vfx/shared/dark-book.png'),
    (N'images/dcs-game/sach.png', N'images/dcs-game/skill-vfx/shared/book.png'),
    (N'images/dcs-game/siba-thien-than-mythic.png', N'images/dcs-game/heroes/siba-thien-than-mythic.png'),
    (N'images/dcs-game/siba-thien-than.png', N'images/dcs-game/heroes/siba-thien-than.png'),
    (N'images/dcs-game/thanh-thai-aura.png', N'images/dcs-game/heroes/thanh-thai-aura.png'),
    (N'images/dcs-game/tien-dung-call-video.png', N'images/dcs-game/heroes/tien-dung-call-video.png'),
    (N'images/dcs-game/tien-dung-tong-dai-epic.png', N'images/dcs-game/heroes/tien-dung-tong-dai-epic.png'),
    (N'images/dcs-game/tien-dung-xuan-rare.png', N'images/dcs-game/heroes/tien-dung-xuan-rare.png'),
    (N'images/dcs-game/tien-dung-xuan.png', N'images/dcs-game/heroes/tien-dung-xuan.png'),
    (N'images/dcs-game/trg-kiet-covid.png', N'images/dcs-game/heroes/trg-kiet-covid.png'),
    (N'images/dcs-game/trong-chua-no-rare.png', N'images/dcs-game/heroes/trong-chua-no-rare.png'),
    (N'images/dcs-game/trong-chua-no.png', N'images/dcs-game/heroes/trong-chua-no.png'),
    (N'images/dcs-game/truong-kiet-chu-mo-epic.png', N'images/dcs-game/heroes/truong-kiet-chu-mo-epic.png'),
    (N'images/dcs-game/truong-kiet-chu-mo.png', N'images/dcs-game/heroes/truong-kiet-chu-mo.png'),
    (N'images/dcs-game/truong-kiet-tot-nghiep-cap-3.png', N'images/dcs-game/heroes/truong-kiet-tot-nghiep-cap-3.png'),
    (N'images/dcs-game/TruongKiet-Noel.png', N'images/dcs-game/heroes/truong-kiet-noel.png'),
    (N'images/dcs-game/tuong-long-cap-3-rare.png', N'images/dcs-game/heroes/tuong-long-cap-3-rare.png'),
    (N'images/dcs-game/tuong-long-cap-3.png', N'images/dcs-game/heroes/tuong-long-cap-3.png'),
    (N'images/dcs-game/tuonglong-quandoi.png', N'images/dcs-game/heroes/tuonglong-quandoi.png'),
    (N'images/dcs-game/van-trong-dien-vang-rare.png', N'images/dcs-game/heroes/van-trong-dien-vang-rare.png'),
    (N'images/dcs-game/van-trong-dien-vang.png', N'images/dcs-game/heroes/van-trong-dien-vang.png'),
    (N'images/dcs-game/van-trong-pikachu.png', N'images/dcs-game/heroes/van-trong-pikachu.png'),
    (N'images/dcs-game/vantrong-cobac.png', N'images/dcs-game/heroes/vantrong-cobac.png'),
    (N'images/dcs-game/vantrong-hs.png', N'images/dcs-game/heroes/vantrong-hs.png'),
    (N'images/dcs-game/dungeon/monsters/monster-1.png', N'images/dcs-game/dungeons/shared/monsters/monster-1.png'),
    (N'images/dcs-game/dungeon/monsters/monster-2.png', N'images/dcs-game/dungeons/shared/monsters/monster-2.png'),
    (N'images/dcs-game/dungeon/monsters/monster-3.png', N'images/dcs-game/dungeons/shared/monsters/monster-3.png'),
    (N'images/dcs-game/dungeon/monsters/monster-4.png', N'images/dcs-game/dungeons/shared/monsters/monster-4.png'),
    (N'images/dcs-game/dungeon/monsters/monster-5.png', N'images/dcs-game/dungeons/shared/monsters/monster-5.png'),
    (N'images/dcs-game/dungeon/monsters/monster-6.png', N'images/dcs-game/dungeons/shared/monsters/monster-6.png'),
    (N'images/dcs-game/dungeon/monsters/monster-7.png', N'images/dcs-game/dungeons/shared/monsters/monster-7.png'),
    (N'images/dcs-game/dungeon/monsters/monster-8.png', N'images/dcs-game/dungeons/shared/monsters/monster-8.png'),
    (N'images/dcs-game/dungeon/monsters/monster-9.png', N'images/dcs-game/dungeons/shared/monsters/monster-9.png'),
    (N'images/dcs-game/dungeon/maps/bug-forest.jpg', N'images/dcs-game/dungeons/bug-forest/map-background.jpg'),
    (N'images/dcs-game/dungeon/maps/legacy-dungeon.jpg', N'images/dcs-game/dungeons/legacy-dungeon/map-background.jpg'),
    (N'images/dcs-game/dungeon/maps/production-citadel.jpg', N'images/dcs-game/dungeons/production-citadel/map-background.jpg'),
    (N'images/dcs-game/formations/phong_duong.png', N'images/dcs-game/formations/phong-duong.png'),
    (N'images/dcs-game/formations/ngu_hanh.png', N'images/dcs-game/formations/ngu-hanh.png'),
    (N'images/dcs-game/formations/luc_do.png', N'images/dcs-game/formations/luc-do.png'),
    (N'images/dcs-game/items/formation_stone.png', N'images/dcs-game/items/formation-stone.png'),
    (N'images/dcs-game/skills/ground-shatter.jpg', N'images/dcs-game/skill-vfx/shared/ground-shatter.jpg'),
    (N'images/dcs-game/skills/ground-shatter-transparent.png', N'images/dcs-game/skill-vfx/shared/ground-shatter-transparent.png'),
    (N'images/dcs-game/skills/dust-impact.jpg', N'images/dcs-game/skill-vfx/shared/dust-impact.jpg'),
    (N'images/dcs-game/skills/dust-impact-transparent.png', N'images/dcs-game/skill-vfx/shared/dust-impact-transparent.png'),
    (N'images/dcs-game/skills/ricardo-fist.jpg', N'images/dcs-game/skill-vfx/ricardo/ricardo-fist.jpg'),
    (N'images/dcs-game/skills/nghia-phuc-prime-fortress.png', N'images/dcs-game/skill-vfx/nghia-phuc-prime/fortress.png'),
    (N'images/dcs-game/skills/van-trong-dien-vang/electric-mascot-orb.png', N'images/dcs-game/skill-vfx/van-trong-dien-vang/electric-mascot-orb.png'),
    (N'images/dcs-game/skills/tuong-long-cap-3/school-bell.png', N'images/dcs-game/skill-vfx/tuong-long-cap-3/school-bell.png'),
    (N'images/dcs-game/skills/trong-chua-no/timed-charge.png', N'images/dcs-game/skill-vfx/trong-chua-no/timed-charge.png'),
    (N'images/dcs-game/skills/tien-dung-xuan/spring-calligraphy-seal.png', N'images/dcs-game/skill-vfx/tien-dung-xuan/spring-calligraphy-seal.png'),
    (N'images/dcs-game/skills/quang-vinh-thcs/school-barrier.png', N'images/dcs-game/skill-vfx/quang-vinh-thcs/school-barrier.png'),
    (N'images/dcs-game/skills/nguyen-xam-lon/boss-stamp.png', N'images/dcs-game/skill-vfx/nguyen-xam-lon/boss-stamp.png'),
    (N'images/dcs-game/heroes/default.png', N'images/dcs-game/placeholders/hero.svg'),
    (N'images/dcs-game/events/tower-banner.png', N'images/dcs-game/backgrounds/tower-banner.svg'),
    (N'images/dcs-game/items/gold.png', N'images/dcs-game/items/currencies/gold.svg'),
    (N'images/dcs-game/items/diamond.png', N'images/dcs-game/items/currencies/diamond.svg');

    DECLARE @table SYSNAME, @column SYSNAME, @statement NVARCHAR(MAX);
    DECLARE image_columns CURSOR LOCAL FAST_FORWARD FOR
        SELECT t.name, c.name
        FROM sys.tables t
        JOIN sys.schemas s ON s.schema_id = t.schema_id
        JOIN sys.columns c ON c.object_id = t.object_id
        JOIN sys.types ty ON ty.user_type_id = c.user_type_id
        WHERE s.name = N'dbo' AND LEFT(t.name, 4) = N'HRK_' AND c.is_computed = 0
          AND c.name IN (N'Avatar', N'ImagePath', N'BackgroundPath', N'BannerImagePath', N'EffectImagePath', N'IconPath', N'AvatarUrl', N'Icon', N'ChestIcon')
          AND ty.name IN (N'varchar', N'nvarchar');
    OPEN image_columns;
    FETCH NEXT FROM image_columns INTO @table, @column;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @statement = N'
            UPDATE target SET ' + QUOTENAME(@column) + N' = COALESCE(alias.NewKey, normalized.AssetKey) + parts.Suffix
            FROM dbo.' + QUOTENAME(@table) + N' target
            CROSS APPLY (VALUES(CONVERT(NVARCHAR(MAX), target.' + QUOTENAME(@column) + N'))) raw(Value)
            CROSS APPLY (VALUES(PATINDEX(N''%[?#]%'', raw.Value))) boundary(Position)
            CROSS APPLY (VALUES(
                CASE WHEN boundary.Position > 0 THEN LEFT(raw.Value, boundary.Position - 1) ELSE raw.Value END,
                CASE WHEN boundary.Position > 0 THEN SUBSTRING(raw.Value, boundary.Position, LEN(raw.Value)) ELSE N'''' END
            )) parts(Path, Suffix)
            CROSS APPLY (VALUES(
                CASE WHEN LEFT(parts.Path, 8) = N''/assets/'' THEN SUBSTRING(parts.Path, 9, LEN(parts.Path))
                     WHEN LEFT(parts.Path, 7) = N''assets/'' THEN SUBSTRING(parts.Path, 8, LEN(parts.Path))
                     ELSE parts.Path END
            )) stripped(AssetKey)
            CROSS APPLY (VALUES(
                CASE WHEN LEFT(stripped.AssetKey, 11) = N''equipments/'' THEN N''images/dcs-game/'' + stripped.AssetKey
                     ELSE stripped.AssetKey END
            )) normalized(AssetKey)
            LEFT JOIN #HrkAssetAliases alias ON alias.OldKey = normalized.AssetKey COLLATE Latin1_General_100_BIN2
            WHERE (LEFT(normalized.AssetKey, 16) = N''images/dcs-game/'')
              AND raw.Value COLLATE Latin1_General_100_BIN2 <>
                  (COALESCE(alias.NewKey, normalized.AssetKey) + parts.Suffix) COLLATE Latin1_General_100_BIN2;';
        EXEC sys.sp_executesql @statement;
        FETCH NEXT FROM image_columns INTO @table, @column;
    END
    CLOSE image_columns;
    DEALLOCATE image_columns;
    DROP TABLE #HrkAssetAliases;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;


# Game image assets

Static artwork belongs to `HrkUi/src/assets`. GAME returns asset keys such as `images/dcs-game/heroes/kiet.png`; Angular resolves them with `environment.assetBaseUrl` (default `/assets/`). Custom avatars remain binary data in `HRK_PlayerCustomAvatars`, served through the existing gateway API and version query. No image table or backend static-file server was added.

## Layout

```text
src/assets/images/dcs-game/
  backgrounds/             lobby and tower banners
  heroes/                  portraits
  equipments/
  items/currencies/        gold.svg, diamond.svg
  materials/hero-stones/
  formations/
  dungeons/{map-code}/     backgrounds and map-specific enemies
  dungeons/shared/monsters/
  effects/                 status icons and shared props
  skills/                  skill icons
  skill-vfx/{hero-code}/   animation artwork
  skill-vfx/shared/
  summon/
  placeholders/            hero.svg, item.svg, background.svg
```

Use lowercase names and hyphens for new files. Seventy-eight existing files were relocated without modifying their contents. Five Epic hero scenes now render their existing CSS artwork directly instead of requesting nonexistent bitmaps.

## Resolve and render

`AssetUrlService` is in `src/libs/core/assets`. `HrkImageDirective` is in `src/libs/shared/ui/controls/hrk-image`.

```ts
@Component({ imports: [HrkImageDirective], /* ... */ })
```

```html
<img [src]="hero.avatar" imageKind="hero" [alt]="hero.name" loading="eager">
<img [src]="item.imagePath" imageKind="item" [alt]="item.name" loading="lazy">
```

The directive intentionally selects native `img` elements **only in components that import it**. Existing `[src]` bindings and static `src` attributes use the same resolver; no changes to layout, `alt`, dimensions or loading priority are required. Existing game image renderers, `HrkImageComponent`, `ItemImageComponent`, SVG icons and the loading image import it.

`imageKind` can be `hero`, `item`, `background` or `effect`. When omitted, the directive derives a default from the resolved asset folder and image class. Use an explicit kind for ambiguous/external images. `imageFallback` overrides the placeholder; an empty value disables fallback. The directive attempts one fallback, hides an image if both fail, and emits `hrkImageFailed` once. Source changes reset this state. Effects keep their own CSS fallback rather than showing an item placeholder.

Use `AssetUrlService.background(value)` for dynamic CSS backgrounds. The result is a quoted `url(...)` value. Map backgrounds and the shared `--hrk-lobby-background` property use this mechanism, including CDN configuration.

## Configuration

Both environment files define:

```ts
assetBaseUrl: '/assets/',
allowedImageOrigins: [] as string[],
```

For a deployment below `/hrk`, use `/hrk/assets/` as the asset base together with the application's deployment configuration. For a CDN, configure its complete asset base, for example `https://cdn.example.com/assets/`, and publish the built `assets` directory there. The directive does not change API URLs.

Full HTTP(S) image URLs are permitted for the frontend origin, configured API gateway origin, asset-base origin and explicitly listed `allowedImageOrigins`. Blob preview URLs and base64 PNG/JPEG/WebP/GIF previews are preserved. Unsupported schemes, traversal paths and other external origins resolve to the appropriate fallback. Configure additional portrait providers explicitly before using their URLs in data.

## Compatibility and database migration

The shared mapping is `HrkUi/src/assets/catalog/asset-paths.json`. It accepts `/assets/...`, `assets/...` and canonical asset keys, preserving query strings/fragments. The older `assets/equipments/...` prefix is also supported.

`npm start`, `npm run build`, `npm run watch` and `npm test` run `scripts/prepare-assets.mjs` first. This writes ignored compatibility copies to `.generated/legacy-assets`; Angular publishes them alongside canonical files. Existing cached clients and old replay/static URLs can continue using the original 78 moved-file paths. These copies temporarily increase build output size but do not duplicate source artwork. The four originally missing PNG paths resolve to SVG placeholders/currency artwork through the new resolver; SVG bytes are never copied under PNG extensions.

If invoking `ng` directly, first run `npm run assets:prepare`. Keep the legacy aliases while old clients, DB values or replay snapshots can still reference them.

Deploy the compatible frontend, then apply:

`Services/HRKGAME/GAME.Infrastructure/Data/Scripts/Migration_NormalizeGameAssetPaths.sql`

Run it **after legacy seed scripts**, which may contain the original paths. It is transactional and rerunnable. It normalizes game paths in recognized image columns of `dbo.HRK_*` tables, uses the catalog's exact alias mapping, and preserves query/fragment suffixes. External URLs, custom-avatar API paths, non-image icon names, uploaded image bytes and historical JSON snapshots are preserved. JSON replay data remains readable through the frontend alias resolver.

The script has not been applied to the application database by this refactor. The SQL integration test applies it twice only in a uniquely named disposable LocalDB database.

## Checks

```powershell
npm run assets:check
npm run build -- --configuration development
```

`assets:check` verifies complete literal image references, case-sensitive filenames, catalog targets and placeholders. It also checks GAME Application references when the backend checkout is present, and works in a standalone HrkUi checkout. Dynamic filename expressions are covered by feature tests. The HrkUi GitHub workflow runs this check without needing Angular dependencies.

The backend `GameAssetPathTests` verifies catalog/SQL mapping parity and, with `HRK_SUMMON_SQL_TESTS=1`, actual SQL normalization, rerun behavior and preservation of non-image data. Angular tests cover legacy/CDN/key resolution, custom avatars, previews, finite fallback attempts, source changes and affected battle/summon presentation.

When adding or moving artwork, update the catalog and migration together if a legacy alias is needed. Prefer canonical asset keys for new data. No automatic per-file fingerprint/version is introduced here; retire compatibility copies only after their consumers have migrated.

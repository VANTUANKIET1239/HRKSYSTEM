# Shared modals and Redis presets

Apply `Services/HRKGAME/GAME.Infrastructure/Data/Scripts/Migration_ModalConfigs.sql` to the
GAME database before deploying the updated API. It creates `HRK_ModalConfigs` and seeds
26 presets. Reruns insert missing presets and preserve operator edits, inactive entries,
titles and revisions. No application database is migrated automatically.

## Backend and cache

`GET /api/game-config/modals` requires authentication and returns the standard response
envelope. `GetModalConfigsQueryHandler` reads active rows through UnitOfWork and caches
the DTO list using the existing Redis-backed `ICacheService`.

- Logical Redis key: `game:config:modals:v1` (plus the configured Redis instance prefix).
- Absolute Redis TTL: five minutes, including empty lists.
- Redis outages use the existing fail-open adapter and read from the database.
- Browser memory refresh interval: one minute, checked when a modal opens; retry after
  failed loads: 30 seconds. A forced `GameModalConfigService.load(true)` refresh is available.
- SQL edits become visible after cache expiry and a subsequent browser refresh/open.
  To refresh sooner, remove the prefixed Redis key, then force the browser refresh/reload.

Example administrative edit (management is currently via SQL, not a new admin screen):

```sql
UPDATE dbo.HRK_ModalConfigs
SET Title = N'Xác nhận rời trò chơi', ConfirmText = N'Đăng xuất',
    Revision = Revision + 1, UpdatedOn = SYSUTCDATETIME()
WHERE Code = 'LOGOUT_CONFIRM';
```

`IsActive = 0` disables only the remote override. The client uses a local preset and still
asks required confirmation questions. Configuration never chooses API routes, component
classes, actions, prices or permission checks. SQL constraints restrict kind, size and tone.
Frontend validation rejects unsupported enums/icons and renders titles/messages as text.
Named placeholders such as `{used}` and `{limit}` receive values from the calling feature;
unknown placeholders fall back to local message text.

## Shared frontend API

`src/libs/shared/ui/modal` contains:

| Component/service | Responsibility |
| --- | --- |
| `HrkModalComponent` | Native dialog layer, body/footer projection, focus, Escape/backdrop policy, scroll and busy state |
| `HrkConfirmationModalComponent` | Message, icon, confirm/cancel actions, actual caller-owned busy state |
| `HrkConfirmationService` / host | Asynchronous questions, queue, pending-question deduplication and cancellation on navigation |
| `ModalStackService` | Top dialog, nested-dialog order, scroll reference count and focus restoration |
| `HrkModalFloatingDirective` | Moves an existing floating host into the current native dialog and restores it afterward, preserving its Angular state |

The shared library has no dependency on GAME or its configuration API. GAME supplies
`HRK_MODAL_CONFIG_SOURCE` through `GameModalConfigService` and keeps local presets in
`core/configs/game-modal-presets.ts`. Session-expired UI uses the common modal with local
configuration and does not depend on the authenticated GAME endpoint.

```html
@if (isOpen) {
  <hrk-modal modalCode="MY_FEATURE" title="Thông tin" [busy]="saving" (closed)="isOpen = false">
    <app-feature-content />
    <div hrkModalFooter><button [disabled]="saving" (click)="save()">Lưu</button></div>
  </hrk-modal>
}
```

Place the condition outside the wrapper so expensive projected components and subscriptions
are destroyed on close. `chrome=false` lets existing feature cards retain their own headers,
artwork and layouts while sharing the modal layer. Static feature headings can bind to the
wrapper's `resolved.title`; outcome titles, selected hero/stage names and reward data stay
feature-owned. Explicit size/dismissibility/close flags and busy state are feature constraints
that remote configuration cannot relax. Fullscreen management screens retain fullscreen size.

```ts
const confirmed = await confirmation.confirm('TOWER_START_QUICK_CLIMB', {}, {
  used: progress.quickClimbRunsUsed + 1, limit: progress.quickClimbDailyLimit
});
if (confirmed) { /* feature calls its own API */ }
```

Keep a pending/busy guard in the feature to prevent duplicate business requests. Reusing a
question's promise does not replace server-side validation/idempotency. Long-running actions
can use the confirmation component directly and pass their real busy state, as logout does.

GAME has one toast and one equipment-tooltip host at its root, each with `hrkModalFloating`.
The native top layer would otherwise cover ordinary page overlays. Hosts are moved, never
recreated; toast timers/subscriptions survive changes between parent and child dialogs.

Migrated callers include logout, battle skip/result, Tower questions/quick climb, campaign
stage/formation/chests, hero management/equipment actions, inventory, formation management,
forge pickers/downgrade/results, library, avatar selection and summon dialogs/reel.
The small `SummonDialogComponent` remains a compatibility wrapper around `HrkModalComponent`.

## Verification

```powershell
$env:HRK_SUMMON_SQL_TESTS = '1'
dotnet test Tests/dcs-game/GAME.Domain.Tests/GAME.Domain.Tests.csproj --no-restore
```

LocalDB tests apply the production migration to an isolated database and verify seeding,
reruns and edit preservation. Unit tests cover Redis hit/miss/TTL, active-only reads and
cancellation. Browser tests cover nested dialogs, focus restoration, busy policy, persistent
floating hosts, fallback/validation, confirmation queue/navigation, Tower request guards and
the existing summon/toast flows. Run the frontend build with
`npm run build -- --configuration development` from `HrkUi`.

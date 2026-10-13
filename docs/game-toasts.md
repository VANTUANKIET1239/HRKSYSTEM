# Game notifications

`GameToastService` is the single notification entry point for the game. Inject it in a component or service:

```ts
private readonly toast = inject(GameToastService);

this.toast.success('Đã lưu đội hình.');
this.toast.warning('Không đủ vàng.');
this.toast.info('Tính năng sắp ra mắt.');
this.toast.fromError(error, 'Không thể lưu đội hình.');
```

`fromError` uses the shared `AppError` normalization. Business, validation and permission errors show their message as a warning. Network failures show a connection message; unexpected system failures use the supplied safe fallback. A 401 remains handled by the existing session dialog. The toast service does not log errors; API diagnostics remain in the existing HTTP layer.

`GameToastComponent` subscribes to the service's read-only Subject stream. It owns the visible cards and timers, displays at most three cards, replaces duplicates within two seconds, and pauses expiry on hover or keyboard focus. Dismissal and destruction cancel timers. The stream does not replay old notifications after navigation.

The host is mounted before the router outlet in the game app and in the separate login/register layout. These layouts are mutually exclusive. Do not mount another host in individual pages or modals.

Optional settings: `title`, `durationMs` (zero keeps the card until dismissed) and `dedupeKey` (update a notification even when its text changes). `show` and the convenience methods return an ID usable with `dismiss(id)`; `clear()` closes all notifications. Defaults: success 3 seconds, info 3.5 seconds, warning 4.5 seconds, error 6 seconds.

Use a toast for action feedback. Keep field validation, page loading errors, confirmation dialogs, battle skill banners and detailed battle/forge reward results in their relevant UI. Do not automatically toast every failed HTTP request: the action handling the request decides what the player should see.

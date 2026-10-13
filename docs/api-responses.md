# API responses and errors

AUTH and GAME use `BaseResponse<T>` as the shared CQRS/controller envelope. This
keeps existing handler signatures and clients compatible. HTTP metadata is finalized
in Core.Web, so handlers/services do not generate trace IDs or write HTTP responses.

```json
{
  "traceId": "89393cafc6334d62871f4979db8317df",
  "success": false,
  "statusCode": 409,
  "data": null,
  "error": {
    "code": "INSUFFICIENT_GOLD",
    "message": "Không đủ Vàng.",
    "type": "business",
    "validationErrors": null
  },
  "message": "Không đủ Vàng.",
  "errors": ["Không đủ Vàng."],
  "hasErrors": true
}
```

`message`, `errors` and `hasErrors` are retained for existing clients. New code reads
`error.code`, `error.message`, `error.type` and `error.validationErrors`. Successful
responses have `error: null`. HTTP status and `statusCode` agree; failure data is null.
The trace ID comes from the active diagnostic Activity, with the request identifier
as fallback. A lost battle or unsuccessful enhancement roll is still a successful
request: outer `success: true`, HTTP 200, and its gameplay outcome inside `data`.

## Backend usage

Register MVC filters explicitly, followed by the response services:

```csharp
using Core.Web.Filters;
using Core.Web.Responses;

services.AddControllers(options => options.AddHrkApiFilters());
services.AddHrkApiResponses();
```

The GAME host uses `options.AddGameApiFilters()` instead: it includes the shared filters
and the game-specific resource concurrency filter. Shared filters live in
`Core.Web/Filters`; game filters live in `HRK.GAME/Filters`. `AddHrkApiResponses()`
registers exception middleware and model-validation responses without adding MVC filters.

Call
`app.UseHrkApiResponses()` after correlation middleware and before authentication.
The MVC response filter wraps raw JSON action results and normalizes existing envelopes.
File responses, health checks, SignalR and messaging contracts retain their protocols.
Empty HTTP 401/403/404 responses receive the same error envelope.

Use a typed exception when rejecting an operation inside a transaction:

```csharp
if (!wallet.HasEnoughGold(cost))
    throw new BusinessException("INSUFFICIENT_GOLD", "Không đủ Vàng.");
```

`BusinessException` and its GAME `DomainException` subtype propagate through the
transaction owner's catch/rollback. They are converted to responses only at the HTTP
boundary. Do not catch and return a failure inside a transaction callback: returning
normally allows the wrapper to save and commit. Keep cleanup catches that rethrow.
`ExecuteInTransactionAsync` rolls back and clears tracked state on failure; callers
using explicit Begin/Commit own their rollback. Transaction commit does not save.

| Kind | HTTP status | Error type |
| --- | --- | --- |
| Validation | 400 | validation |
| NotFound | 404 | business |
| Conflict (default) | 409 | business |
| Unauthorized | 401 | auth |
| Forbidden | 403 | auth |
| Unexpected exception | 500 | system |

Before any mutation starts, handlers may return a coded `FailResponse` directly.
The MediatR validation behavior rejects invalid requests before calling the handler;
model binding failures also produce `VALIDATION_ERROR` with field errors.

Known business conditions have stable codes such as `ITEM_LOCKED`, `INVENTORY_FULL`,
`INSUFFICIENT_GOLD`, `INVALID_REQUEST_ID`, `HERO_NOT_FOUND` and
`REWARD_ALREADY_CLAIMED`. Define a meaningful code at the throw/return site for new
conditions. Do not infer codes by parsing exception messages at runtime.
Configuration errors, corrupt persisted state and database exceptions remain system
failures. Their details are logged server-side and replaced with a safe 500 message;
never return `ex.Message` for an unexpected exception.

## Frontend usage

`HrkApiService` emits successful responses on `next` and throws `AppError` on `error`.
It also rejects legacy `success: false` envelopes returned with HTTP 200 during
migration. It preserves a successful response whose `data.success` is false.

```typescript
api.CallApi<BaseResponse<UpgradeResult>>('POST', url, request).subscribe({
  next: response => updateView(response.data),
  error: (error: AppError) => showMessage(error.message)
});
```

`AppError` exposes status, code, message, type, traceId and validationErrors. Its
`error.message` accessor supports older component handlers. The central API reporter
skips business/validation/auth errors and reports system/network errors with trace ID;
reporters in components avoid logging the same AppError twice. DevTools may still show
failed HTTP requests in its Network panel or browser-generated diagnostics.

Refresh token errors propagate to every waiting request. Only HTTP 401 invalidates the
session; a network failure, HTTP 500 or a retried business 409 does not expire it.
Login/register screens display rejection messages, and failed registration stays on
the registration form.

## Verification

`Tests/Core.Modules.Tests/ApiResponseTests.cs` exercises actual MVC requests for
responses, HTTP status, model/MediatR validation, expected-error logging and safe 500s.
GAME's transaction tests verify that a business exception rolls back without saving,
committing or retrying. Frontend specs cover AppError, HTTP error delivery, gameplay
outcomes and concurrent refresh failures.

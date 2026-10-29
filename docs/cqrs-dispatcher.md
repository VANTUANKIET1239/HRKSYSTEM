# Using the CQRS dispatcher in controllers

Controllers in AUTH, GAME, and ADMIN use
[`ICqrsDispatcher`](../CoreEngine/Core.Application/CQRS/ICqrsDispatcher.cs)
to dispatch existing CQRS requests. The dispatcher uses MediatR internally, so
registered handlers and validation behaviors still run.

## Register the application assembly

Call `AddCoreApplication` once for each application assembly containing handlers:

```csharp
using Core.Application.DependencyInjection;
using GAME.Application.Interfaces;

services.AddCoreApplication(typeof(IGameRequestContext).Assembly);
```

AUTH, GAME, and ADMIN already register their application assemblies. Test hosts
that activate these controllers must also call `AddCoreApplication` with the
assembly containing their real or test handlers.

## Inject and dispatch

Use `QueryAsync` for requests implementing `IQuery<TResponse>` and `CommandAsync`
for requests implementing `ICommand<TResponse>`. Choose the method by the request
type: an HTTP GET may need a lifecycle command before its query, and a POST that
only previews data can dispatch a query.

```csharp
using Core.Application.CQRS;
using GAME.Application.Features.Commands.CheckIn;
using GAME.Application.Features.Queries.CheckIn;

// Constructor dependency:
// public CheckInController(ICqrsDispatcher dispatcher) : HRKControllerBase

// Inside controller actions, use the authenticated user's identity:
var calendar = await dispatcher.QueryAsync(
    new GetCheckInCalendarQuery(RequireUserId()), cancellationToken);

var receipt = await dispatcher.CommandAsync(
    new ClaimCheckInCommand(RequireUserId(), request.RequestId), cancellationToken);
```

Forward the action's `CancellationToken` or `HttpContext.RequestAborted`. For an
operation with its own timeout, forward the linked timeout token. Return the
result using the controller's existing response method, such as `HrkOk(result)`.

See [CheckInController](../Services/HRKGAME/HRK.GAME/Controllers/CheckInController.cs)
for query and command actions,
[EventsController](../Services/HRKGAME/HRK.GAME/Controllers/EventsController.cs)
for lifecycle commands followed by queries, and
[AuthController](../Services/AUTH/HRK.AUTH/Controllers/AuthController.cs)
for commands bound directly from request bodies.

Services that do not currently implement CQRS can continue using their existing
dependencies. Introducing new requests and handlers is a separate change.

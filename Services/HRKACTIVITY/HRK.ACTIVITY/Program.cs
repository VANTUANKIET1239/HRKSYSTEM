using Activity.Application;
using Activity.Infrastructure;
using HRK.ACTIVITY.Configuration;
using Core.HealthChecks;
using Oservability;

var builder = WebApplication.CreateBuilder(args);
builder.AddCustomDependency();

var app = builder.Build();
app.UseHrkCorrelation();
app.UseAuthentication();
app.UseHrkIdentityLogging();
app.UseAuthorization();
app.MapHrkHealthChecks();
var api = app.MapGroup("/api/activities").RequireAuthorization("ActivityAdmin");
api.MapGet("", async ([AsParameters] GetActivitiesQuery query,
    IActivityQueries queries, CancellationToken ct) =>
{
    if (!query.IsValid) return Results.BadRequest("Invalid activity filters or pagination.");
    return Results.Ok(await queries.GetAsync(query, ct));
});
api.MapGet("/{activityId:guid}", async (Guid activityId, IActivityQueries queries, CancellationToken ct) =>
    await queries.GetByIdAsync(activityId, ct) is { } activity ? Results.Ok(activity) : Results.NotFound());
app.Run();

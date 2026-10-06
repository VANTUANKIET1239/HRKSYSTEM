using Activity.Application;
using Activity.Infrastructure;
using HRK.ACTIVITY.Configuration;
using Microsoft.EntityFrameworkCore;
using Oservability;

var builder = WebApplication.CreateBuilder(args);
builder.AddCustomDependency();

var app = builder.Build();
app.UseHrkCorrelation();
app.UseAuthentication();
app.UseHrkIdentityLogging();
app.UseAuthorization();
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", async (ActivityDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct) ? Results.Ok(new { status = "ready" }) : Results.StatusCode(503));
var api = app.MapGroup("/api/activities").RequireAuthorization("ActivityAdmin");
api.MapGet("", async ([AsParameters] GetActivitiesQuery query,
    IActivityQueries queries, CancellationToken ct) =>
{
    if (query.Page is < 1 or > 100000 || query.PageSize is < 1 or > 100) return Results.BadRequest("Invalid pagination.");
    return Results.Ok(await queries.GetAsync(query, ct));
});
api.MapGet("/{activityId:guid}", async (Guid activityId, IActivityQueries queries, CancellationToken ct) =>
    await queries.GetByIdAsync(activityId, ct) is { } activity ? Results.Ok(activity) : Results.NotFound());
app.Run();

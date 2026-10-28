using HRK.REALTIME.Configuration;
using HRK.REALTIME.Hubs;
using Oservability;
using Core.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.AddCustomDependency();

var app = builder.Build();
app.UseHrkCorrelation();
app.UseRouting();
app.UseCors("Spa");
app.UseAuthentication();
app.UseHrkIdentityLogging();
app.UseAuthorization();
app.MapHrkHealthChecks();
app.MapHub<RealtimeHub>("/hubs/realtime").RequireAuthorization();
app.Run();

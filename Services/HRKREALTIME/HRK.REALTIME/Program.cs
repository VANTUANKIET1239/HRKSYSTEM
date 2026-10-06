using HRK.REALTIME.Configuration;
using HRK.REALTIME.Hubs;
using Oservability;

var builder = WebApplication.CreateBuilder(args);
builder.AddCustomDependency();

var app = builder.Build();
app.UseHrkCorrelation();
app.UseRouting();
app.UseCors("Spa");
app.UseAuthentication();
app.UseHrkIdentityLogging();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapHub<RealtimeHub>("/hubs/realtime").RequireAuthorization();
app.Run();

using ApiGateway.Configuration;
using Ocelot.Middleware;
using Oservability;
using Core.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.AddCustomDependency();

var app = builder.Build();
app.UseHrkCorrelation();

app.UseRouting();
app.UseCors("AllowSpa");
app.UseAuthentication();
app.UseHrkIdentityLogging();
app.UseAuthorization();

// Serve local health routes before Ocelot's terminal downstream pipeline.
app.UseEndpoints(endpoints => endpoints.MapHrkHealthChecks());

await app.UseOcelot();

app.Run();

using ApiGateway.Configuration;
using Ocelot.Middleware;
using Oservability;

var builder = WebApplication.CreateBuilder(args);
builder.AddCustomDependency();

var app = builder.Build();
app.UseHrkCorrelation();

app.UseRouting();
app.UseCors("AllowSpa");
app.UseAuthentication();
app.UseHrkIdentityLogging();
app.UseAuthorization();

await app.UseOcelot();

app.Run();

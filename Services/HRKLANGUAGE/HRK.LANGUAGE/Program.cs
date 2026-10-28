using Core.Web.Compression;
using HRK.LANGUAGE.Configuration;
using Oservability;
using Core.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.AddCustomDependency();
var app = builder.Build();
app.UseHrkCorrelation();
app.UseExceptionHandler();
app.UseHrkResponseCompression();
app.MapHrkHealthChecks();
app.MapControllers();
app.Run();

public partial class Program;

using Core.HealthChecks;
using HRK.ADMIN.Configuration;
using Core.Web.Responses;
using Oservability;

var builder = WebApplication.CreateBuilder(args);
builder.AddCustomDependency();
var app = builder.Build();
app.UseHrkCorrelation();
app.UseHrkApiResponses();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseRouting();
app.UseAuthentication();
app.UseHrkIdentityLogging();
app.UseAuthorization();
app.MapHrkHealthChecks();
app.MapControllers();
app.Run();

public partial class Program;

using Oservability;
using HRK.GAME.Configuration;
using Core.Web.Responses;
using Core.Web.Compression;

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
app.UseHrkResponseCompression();
app.UseAuthentication();
app.UseHrkIdentityLogging();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

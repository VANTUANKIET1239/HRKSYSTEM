using Oservability;

using HRK.GAME.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.AddCustomDependency();

// Add services to container
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

builder.Services
    .AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", opts =>
    {
        opts.Audience = builder.Configuration["JwtSettings:Audience"];
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"] ?? "Kite@1239kK10Kite@1239kK10Kite@1239kK10")
            ),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(60)
        };

        if (builder.Environment.IsDevelopment())
            opts.RequireHttpsMetadata = false;
    });

builder.Services.AddAuthorization();

var app = builder.Build();
app.UseHrkCorrelation();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseHrkIdentityLogging();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

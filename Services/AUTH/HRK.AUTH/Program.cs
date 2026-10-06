using Oservability;
using AUTH.Infrastructure.Configuration;
using HRK.AUTH.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.AddCustomDependency();

// Add services to the container.
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
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],     // often same as Authority for many IdPs

            ValidateAudience = true,
            // If you use a single shared audience for all backend APIs:
            ValidAudience = builder.Configuration["JwtSettings:Audience"], // e.g. "your-backend-apis"
                                                                           // If you accept multiple audiences at the gateway, use:
                                                                           // ValidAudiences = builder.Configuration.GetSection("Jwt:Audiences").Get<string[]>(),

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"])
            ),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(60)
        };

        // Dev over HTTP (only during local dev!)
        if (builder.Environment.IsDevelopment())
            opts.RequireHttpsMetadata = false;
    });

builder.Services.AddAuthorization();

var app = builder.Build();
app.UseHrkCorrelation();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // default path: /swagger
}

// Configure the HTTP request pipeline.

app.UseAuthentication();
app.UseHrkIdentityLogging(); // must come before UseAuthorization
app.UseAuthorization();

app.MapControllers();

app.Run();

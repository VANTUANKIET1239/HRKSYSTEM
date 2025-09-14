using AUTH.Infrastructure.Configuration;
using HRK.AUTH.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

DependencyInjection.AddCustomDependency(builder.Services, builder.Configuration);

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

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(60)
        };

        // Dev over HTTP (only during local dev!)
        if (builder.Environment.IsDevelopment())
            opts.RequireHttpsMetadata = false;
    });

builder.Services.AddAuthorization();

var app = builder.Build();



if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // default path: /swagger
}


// Configure the HTTP request pipeline.

app.UseAuthentication(); // must come before UseAuthorization
app.UseAuthorization();

app.MapControllers();

app.Run();

using Oservability;
using ApiGateway.DelegatingHandlers;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ApiGateway.Configuration;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddCustomDependency(this WebApplicationBuilder builder)
    {
        AddObservability(builder);
        AddServices(builder);
        return builder;
    }

    public static void AddObservability(WebApplicationBuilder builder) =>
        builder.AddHrkObservability("ApiGateway");

    public static void AddServices(WebApplicationBuilder builder)
    {
        // builder.Configuration.AddJsonFile("Ocelot.json", optional: false, reloadOnChange: true);
        var ocelotConfig = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"
            ? "Ocelot.docker.json"
            : "Ocelot.json";
        builder.Configuration.AddJsonFile(ocelotConfig, optional: false, reloadOnChange: true);
        builder.Services.AddOcelot(builder.Configuration);

        // Add JWT authentication and authorization
        builder.Services
            .AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", opts =>
            {
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["JwtSettings:Issuer"],     // often same as Authority for many IdPs

                    ValidateAudience = true,
                    // If you use a single shared audience for all backend APIs:
                    //   ValidAudience = builder.Configuration["JwtSettings:Audience"], // e.g. "your-backend-apis"
                    // If you accept multiple audiences at the gateway, use:
                    ValidAudiences = builder.Configuration.GetSection("JwtSettings:Audiences").Get<string[]>(),

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

        builder.Services.AddCors(opts =>
         {
             var origins = builder.Configuration.GetSection("CORS:Origins").Get<string[]>() ?? [];
             opts.AddPolicy("AllowSpa", p => p
                 .WithOrigins(
                    origins
                 )
                 .AllowAnyHeader()
                 .AllowAnyMethod().WithExposedHeaders("X-Correlation-Id")
                 .AllowCredentials()
                );
         });
    }
}

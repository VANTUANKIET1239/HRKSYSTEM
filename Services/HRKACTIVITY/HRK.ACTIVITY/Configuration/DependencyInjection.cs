using System.Text;
using Activity.Application;
using Activity.Infrastructure;
using Core.Messaging.Contracts;
using Core.RabbitMQ.DependencyInjection;
using Core.TransactionalMessaging.Inbox;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Oservability;

namespace HRK.ACTIVITY.Configuration;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddCustomDependency(this WebApplicationBuilder builder)
    {
        AddObservability(builder);
        AddServices(builder);
        return builder;
    }

    public static void AddObservability(WebApplicationBuilder builder) =>
        builder.AddHrkObservability("HRK.ACTIVITY");

    public static void AddServices(WebApplicationBuilder builder)
    {
        var connection = builder.Configuration["Database:ConnectionString"]
            ?? builder.Configuration.GetConnectionString("Activity")
            ?? throw new InvalidOperationException("Database:ConnectionString is required.");
        builder.Services.AddDbContext<ActivityDbContext>(options => options.UseSqlServer(connection,
            sql => sql.EnableRetryOnFailure()));
        builder.Services.AddScoped<IInboxStore, SqlServerInboxStore<ActivityDbContext>>();
        builder.Services.AddScoped<IInboxExecutor, InboxExecutor<ActivityDbContext>>();
        builder.Services.AddScoped<IActivityRecorder, ActivityStore>();
        builder.Services.AddScoped<IActivityQueries, ActivityStore>();
        builder.Services.AddRabbitMqMessaging(builder.Configuration).EnsureRabbitTopology()
            .AddNamedRabbitConsumer<PlayerActivityRecordedV1, ActivityMessageHandler>(ActivityStore.ConsumerName);
        var secret = builder.Configuration["JwtSettings:SecretKey"];
        if (string.IsNullOrWhiteSpace(secret)) throw new InvalidOperationException("JwtSettings:SecretKey is required.");
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
                ValidateAudience = true, ValidAudience = builder.Configuration["JwtSettings:Audience"],
                ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(60)
            };
        });
        builder.Services.AddAuthorization(options => options.AddPolicy("ActivityAdmin", policy =>
            policy.RequireAuthenticatedUser().RequireRole("Admin")));
    }
}

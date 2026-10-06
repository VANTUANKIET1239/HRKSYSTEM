using Oservability;
using Oservability.Tracing;
using System.Security.Claims;
using System.Text;
using Core.Messaging.Contracts;
using Core.RabbitMQ.DependencyInjection;
using HRK.REALTIME.Authentication;
using HRK.REALTIME.Health;
using HRK.REALTIME.Hubs;
using HRK.REALTIME.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace HRK.REALTIME.Configuration;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddCustomDependency(this WebApplicationBuilder builder)
    {
        AddObservability(builder);
        AddServices(builder);
        return builder;
    }

    public static void AddObservability(WebApplicationBuilder builder) =>
        builder.AddHrkObservability("HRK.REALTIME");

    public static void AddServices(WebApplicationBuilder builder)
    {
        var signalR = builder.Services.AddSignalR();
        AddRedisTelemetry(builder, signalR);

        builder.Services.AddSingleton<IUserIdProvider, SignalRUserIdProvider>();
        var jwtSecret = builder.Configuration["JwtSettings:SecretKey"];
        if (string.IsNullOrWhiteSpace(jwtSecret))
        {
            throw new InvalidOperationException("JwtSettings:SecretKey is required and cannot be empty.");
        }

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["JwtSettings:Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(60),
                    NameClaimType = ClaimTypes.NameIdentifier
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Query["access_token"];
                        if (!string.IsNullOrWhiteSpace(token) &&
                            context.HttpContext.Request.Path.StartsWithSegments("/hubs/realtime"))
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    }
                };
                if (builder.Environment.IsDevelopment())
                {
                    options.RequireHttpsMetadata = false;
                }
            });
        builder.Services.AddAuthorization();

        var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
        builder.Services.AddCors(options => options.AddPolicy("Spa", policy =>
            policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("X-Correlation-Id").AllowCredentials()));

        builder.Services
            .AddRabbitMqMessaging(builder.Configuration)
            .EnsureRabbitTopology()
            .AddNamedRabbitConsumer<ProcessStatusUpdatedV1, ProcessStatusUpdatedHandler>(
                "RealtimeProcessStatus");
        AddServiceHealthChecks(builder.Services);
    }

    public static void AddRedisTelemetry(WebApplicationBuilder builder, ISignalRServerBuilder signalR)
    {
        if (builder.Configuration.GetValue<bool>("Redis:Enabled"))
        {
            builder.Services.AddHrkRedisTelemetry();
            var redisConnection = builder.Configuration["Redis:ConnectionString"]
                ?? throw new InvalidOperationException("Redis:ConnectionString is required when Redis is enabled.");
            signalR.AddStackExchangeRedis(redisConnection, options =>
            {
                options.Configuration.ChannelPrefix = RedisChannel.Literal(
                    builder.Configuration["Redis:ChannelPrefix"] ?? "hrk:realtime");
            });
            builder.Services.AddOptions<Microsoft.AspNetCore.SignalR.StackExchangeRedis.RedisOptions>()
                .Configure<RedisTelemetry>((options, telemetry) =>
                    options.ConnectionFactory = _ => telemetry.ConnectAsync(redisConnection));
        }
    }

    public static void AddServiceHealthChecks(IServiceCollection services) =>
        services.AddHealthChecks().AddCheck<RabbitMqHealthCheck>("rabbitmq");
}

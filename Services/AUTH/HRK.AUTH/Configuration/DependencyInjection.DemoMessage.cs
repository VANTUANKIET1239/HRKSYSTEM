using AUTH.Application.Interfaces;
using AUTH.Infrastructure.Configuration;
using AUTH.Infrastructure.Data;
using AUTH.Infrastructure.Identity;
using AUTH.Infrastructure.Services;
using Core.Persistence.Database.Extensions;
using Core.Persistence.Database.Options;
using Core.AUTH.DependencyInjection;
using Core.Web.DependencyInjection;
using Core.Persistence.DependencyInjection;
using Core.Application.DependencyInjection;
using Core.RabbitMQ.DependencyInjection;
using Core.RabbitMQ.Interfaces;
using Oservability;
using Core.Localization;
using Core.HealthChecks;
using HRK.AUTH.HealthChecks.Dependencies;
using Core.TransactionalMessaging.DependencyInjection;
using Core.Persistence.Repositories;

namespace HRK.AUTH.Configuration
{
    public static partial class DependencyInjection
    {
        //public static void IdentityConfiguration(IServiceCollection services)
        //{
        //    services.AddIdentity<ApplicationUser, IdentityRole>()
        //        .AddEntityFrameworkStores<AuthDbContext>()
        //        .AddDefaultTokenProviders();
        //}
        public sealed record DemoMessage(Guid Id, string Name, DateTime CreatedAtUtc);
    }
}

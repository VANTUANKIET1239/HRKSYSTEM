using AUTH.Infrastructure.Configuration;
using AUTH.Infrastructure.Data;
using AUTH.Infrastructure.Identity;
using Core.Common.Common;
using Core.Common.Extensions;
using Core.Common.SqlExecutor;
using FluentValidation;
using HRK.AUTH.Interfaces;
using HRK.AUTH.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Reflection;
namespace HRK.AUTH.Configuration
{
    public static class DependencyInjection
    {

        public static IServiceCollection AddCustomDependency(IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthInfrastructure(configuration);

            services.AddHttpContextAccessor();
            // add DbContexts
            AddDbContexts(services, configuration);

            AddServices(services, configuration);

            services.AddValidatorsFromAssembly(typeof(Program).Assembly);
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly());
            });



            // add services 



            return services;
        }

        public static void AddServices(IServiceCollection services, IConfiguration configuration)
        {

            services.AddCoreService(configuration);


            services.AddRepositoryUOW<ApplicationDbContext>();

            services.AddScoped<IRefreshTokenService, RefreshTokenService>();

          

        }

        public static void AddDbContexts(IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AuthDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString(Constants.DefaultConnection)));

            services.AddScoped<IDbConnection>(sp =>
            new SqlConnection(configuration.GetConnectionString(Constants.DefaultConnection)));


            services.AddDbContext<ApplicationDbContext>(options =>
              options.UseSqlServer(configuration.GetConnectionString(Constants.DefaultConnection)));
        }


        //public static void IdentityConfiguration(IServiceCollection services)
        //{
        //    services.AddIdentity<ApplicationUser, IdentityRole>()
        //        .AddEntityFrameworkStores<AuthDbContext>()
        //        .AddDefaultTokenProviders();



        //}
    }
}

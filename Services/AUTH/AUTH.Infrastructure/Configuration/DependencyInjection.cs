using AUTH.Infrastructure.Data;
using AUTH.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AUTH.Infrastructure.Configuration
{
    public static class AuthDependencyInjection
    {
        public static void AddAuthInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            //// Register the DbContext with the connection string from configuration
            //services.AddDbContext<ApplicationDbContext>(options =>
            //     options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));


            // Register Identity services
            // services.AddIdentityCore<ApplicationUser>()
            //.AddRoles<IdentityRole>()
            //.AddEntityFrameworkStores<AuthDbContext>()
            //.AddDefaultTokenProviders();




            services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();



            services.Configure<IdentityOptions>(options =>
            {
                options.Lockout.MaxFailedAccessAttempts = configuration.GetSection("Identity:Lockout:MaxFailedAccessAttempts").Get<int>();
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
                options.Lockout.AllowedForNewUsers = true;
            });
        }
    }
}

using AUTH.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AUTH.Infrastructure.Data
{

        public class AuthDbContext : IdentityDbContext<ApplicationUser>
        {
            public AuthDbContext(DbContextOptions<AuthDbContext> options)
                : base(options) { }


            protected override void OnModelCreating(ModelBuilder builder)
            {
                base.OnModelCreating(builder);

                // Rename Identity tables
                builder.Entity<ApplicationUser>().ToTable("HRK_Users");
                builder.Entity<IdentityRole>().ToTable("HRK_Roles");
                builder.Entity<IdentityUserRole<string>>().ToTable("HRK_UserRoles");
                builder.Entity<IdentityUserClaim<string>>().ToTable("HRK_UserClaims");
                builder.Entity<IdentityUserLogin<string>>().ToTable("HRK_UserLogins");
                builder.Entity<IdentityRoleClaim<string>>().ToTable("HRK_RoleClaims");
                builder.Entity<IdentityUserToken<string>>().ToTable("HRK_UserTokens");
            }

        }



}

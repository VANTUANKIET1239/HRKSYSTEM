using AUTH.Domain.Entities;
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

    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }


        public DbSet<HRK_LoginSession> HrkLoginSessions { get; set; }

        public DbSet<HRK_RefreshToken> HrkRefreshTokens  { get; set; }

        public DbSet<HRK_ApplicationRouteConfig> ApplicationRouteConfigs { get; set; }

       // public DbSet<AccessTokenRecord>  HrkAccessTokenRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<HRK_ApplicationRouteConfig>(entity =>
            {
                entity.HasIndex(x => x.AppCode).IsUnique();
                entity.HasIndex(x => x.Audience).IsUnique();
            });
        }

    }



}

using System.Data;
using Core.Persistence.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Core.Modules.Tests;
public sealed partial class SqlExecutorDatabase
{
    public sealed class ProbeContext(DbContextOptions<ProbeContext> options) : DbContext(options)
    {
        public DbSet<Probe> Records => Set<Probe>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Probe>().ToTable("Records", "dbo");
            builder.Entity<Probe>().Property(row => row.Id).ValueGeneratedNever();
            builder.Entity<Probe>().Property(row => row.Label).HasMaxLength(200);
            builder.Entity<Probe>().Property(row => row.Amount).HasPrecision(18, 2);
        }
    }
}

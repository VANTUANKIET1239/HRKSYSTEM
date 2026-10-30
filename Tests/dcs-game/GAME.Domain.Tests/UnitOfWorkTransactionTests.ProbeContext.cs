using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Core.Persistence.Repositories;
using GAME.Infrastructure.Data;
using GAME.Application.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAME.Domain.Tests;
public partial class UnitOfWorkTransactionTests
{
    private sealed class ProbeContext(DbContextOptions<ProbeContext> options) : DbContext(options)
    {
        public int SaveCount { get; private set; }

        protected override void OnModelCreating(ModelBuilder builder) => builder.Entity<Probe>().HasKey(x => x.Id);
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SaveCount++;
            ChangeTracker.AcceptAllChanges();
            return Task.FromResult(1);
        }
    }
}

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
    // Exercise EF's real relational transaction lifecycle without a database server.
    private sealed class ProbeConnection : DbConnection
    {
        private ConnectionState _state;
        public int Commits, Rollbacks;
        public bool FailCommit;
        public List<string> LockResources { get; } = new();

        [AllowNull]
        public override string ConnectionString { get; set; } = "Server=localhost;Database=Probe;Trusted_Connection=True";
        public override string Database => "Probe";
        public override string DataSource => "localhost";
        public override string ServerVersion => "16.0";
        public override ConnectionState State => _state;

        public override void Open() => _state = ConnectionState.Open;
        public override void Close() => _state = ConnectionState.Closed;
        public override void ChangeDatabase(string databaseName)
        {
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => new ProbeTransaction(this, isolationLevel);
        protected override DbCommand CreateDbCommand() => new ProbeCommand(this);
    }
}

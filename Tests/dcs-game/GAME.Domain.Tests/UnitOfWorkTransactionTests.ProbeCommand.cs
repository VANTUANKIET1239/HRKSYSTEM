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
    private sealed class ProbeCommand(ProbeConnection connection) : DbCommand
    {
        private readonly SqlCommand _parameters = new();
        [AllowNull]
        public override string CommandText { get; set; } = "";
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; } = connection;
        protected override DbTransaction? DbTransaction { get; set; }
        protected override DbParameterCollection DbParameterCollection => _parameters.Parameters;

        protected override DbParameter CreateDbParameter() => new SqlParameter();
        public override void Cancel()
        {
        }

        public override void Prepare()
        {
        }

        public override int ExecuteNonQuery()
        {
            Assert.Equal(CommandType.StoredProcedure, CommandType);
            Assert.Equal("[dbo].[usp_HRK_AcquireTransactionLock]", CommandText);
            Assert.NotNull(DbTransaction);
            connection.LockResources.Add(Assert.IsType<string>(Parameters["Resource"].Value));
            return 0;
        }

        public override object? ExecuteScalar() => throw new NotSupportedException();
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();
    }
}

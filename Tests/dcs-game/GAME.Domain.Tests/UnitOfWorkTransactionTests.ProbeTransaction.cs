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
    private sealed class ProbeTransaction(ProbeConnection connection, IsolationLevel isolationLevel) : DbTransaction
    {
        public override IsolationLevel IsolationLevel => isolationLevel;
        protected override DbConnection DbConnection => connection;

        public override void Commit()
        {
            connection.Commits++;
            if (connection.FailCommit)
                throw new TimeoutException("Commit acknowledgement lost.");
        }

        public override void Rollback() => connection.Rollbacks++;
    }
}

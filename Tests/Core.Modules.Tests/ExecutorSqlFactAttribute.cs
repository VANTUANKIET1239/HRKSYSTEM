using System.Data;
using Core.Persistence.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Core.Modules.Tests;

public sealed class ExecutorSqlFactAttribute : FactAttribute
{
    public ExecutorSqlFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("HRK_SQL_EXECUTOR_TESTS") != "1")
            Skip = "Set HRK_SQL_EXECUTOR_TESTS=1 to run isolated SQL Server LocalDB executor tests.";
    }
}

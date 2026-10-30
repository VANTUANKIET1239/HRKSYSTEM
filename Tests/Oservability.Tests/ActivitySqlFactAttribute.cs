using System.Text.Json;
using Activity.Application;
using Activity.Infrastructure;
using Core.Messaging.Contracts;
using Core.TransactionalMessaging.Inbox;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Oservability.Tests;

public sealed class ActivitySqlFactAttribute : FactAttribute
{
    public ActivitySqlFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("HRK_ACTIVITY_SQL_TESTS") != "1")
            Skip = "Set HRK_ACTIVITY_SQL_TESTS=1 to run isolated SQL Server LocalDB activity tests.";
    }
}

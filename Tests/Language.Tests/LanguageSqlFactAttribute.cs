using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using Core.Persistence.Repositories;
using Core.TransactionalMessaging.Configuration;
using Core.TransactionalMessaging.Outbox;
using Language.Application;
using Language.Domain;
using Language.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Language.Tests;

public sealed class LanguageSqlFactAttribute : FactAttribute
{
    public LanguageSqlFactAttribute()
    { if (Environment.GetEnvironmentVariable("HRK_LANGUAGE_SQL_TESTS") != "1") Skip = "Set HRK_LANGUAGE_SQL_TESTS=1 to use an isolated LocalDB test database."; }
}

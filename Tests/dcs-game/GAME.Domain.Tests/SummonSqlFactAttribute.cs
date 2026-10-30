using System.Text.Json;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using GAME.Application.Services;
using GAME.Application.Templates;
using GAME.Infrastructure.Templates;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace GAME.Domain.Tests;
// Opt in on machines with SQL Server LocalDB: HRK_SUMMON_SQL_TESTS=1 dotnet test ...
public sealed class SummonSqlFactAttribute : FactAttribute
{
    public SummonSqlFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("HRK_SUMMON_SQL_TESTS") != "1")
            Skip = "Set HRK_SUMMON_SQL_TESTS=1 to run isolated SQL Server LocalDB transaction tests.";
    }
}

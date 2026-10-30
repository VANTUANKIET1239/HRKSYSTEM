using AUTH.Application.Features.Commands.RegisterUser;
using AUTH.Application.Interfaces;
using AUTH.Infrastructure.Data;
using AUTH.Infrastructure.Identity;
using Core.Messaging.Contracts;
using Core.Persistence.Repositories;
using Core.TransactionalMessaging.Entities;
using Core.TransactionalMessaging.Outbox;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Xunit;

namespace AUTH.Tests;

public sealed class AuthSqlFactAttribute : FactAttribute
{
    public AuthSqlFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("HRK_AUTH_SQL_TESTS") != "1")
            Skip = "Set HRK_AUTH_SQL_TESTS=1 for isolated SQL Server LocalDB tests.";
    }
}

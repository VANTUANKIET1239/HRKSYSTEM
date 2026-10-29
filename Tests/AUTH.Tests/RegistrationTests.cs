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

public sealed class RegistrationTests
{
    [Fact]
    public void RequiresDisplayNameAndApplicationWithoutAcceptingRoles()
    {
        var validator = new RegisterUserCommandValidation();
        Assert.False(validator.Validate(new RegisterUserCommand { Email = "user@example.test", Password = "Secret123!", FullName = " " }).IsValid);
        Assert.False(validator.Validate(new RegisterUserCommand { Email = "user@example.test", Password = "Secret123!", FullName = "Player", AppCode = "" }).IsValid);
        Assert.True(validator.Validate(new RegisterUserCommand { Email = "user@example.test", Password = "Secret123!", FullName = "Player" }).IsValid);
        Assert.Null(typeof(RegisterUserCommand).GetProperty("Role"));
    }

    [AuthSqlFact]
    public async Task UserAndSanitizedEventCommitTogetherAndDuplicateEmailDoesNotPublish()
    {
        await using var database = await AuthTestDatabase.CreateAsync();
        await using var provider = Services(database);
        await using var scope = provider.CreateAsyncScope();
        var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();
        const string password = "StrongPassword123!";
        Assert.True((await identity.CreateUserAsync("player@example.test", password, "New Player", "dcs-game")).Succeeded);
        await using var db = database.Auth();
        var user = await db.Users.SingleAsync();
        Assert.Equal("New Player", user.FullName);
        Assert.NotEqual(password, user.PasswordHash);
        var row = await db.Set<OutboxMessage>().SingleAsync();
        var account = JsonSerializer.Deserialize<AccountRegisteredV1>(row.Payload, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal(user.Id, account.UserId);
        Assert.Equal(row.Id, account.EventId);
        Assert.Equal("New Player", account.DisplayName);
        Assert.Equal(AccountContractNames.Publisher, row.PublisherName);
        Assert.DoesNotContain(password, row.Payload);
        Assert.DoesNotContain(user.Email!, row.Payload);
        Assert.False((await identity.CreateUserAsync("player@example.test", password, "Another Player", "dcs-game")).Succeeded);
        Assert.Single(await db.Users.AsNoTracking().ToListAsync());
        Assert.Single(await db.Set<OutboxMessage>().AsNoTracking().ToListAsync());
    }

    [AuthSqlFact]
    public async Task OutboxFailureRollsBackIdentityUser()
    {
        await using var database = await AuthTestDatabase.CreateAsync();
        await database.ExecuteAsync("ALTER TABLE dbo.HRK_AuthOutboxMessages ADD CONSTRAINT CK_Test_OutboxFailure CHECK(EventName <> N'auth.account.registered.v1');");
        await using var provider = Services(database);
        await using var scope = provider.CreateAsyncScope();
        var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();
        await Assert.ThrowsAnyAsync<Exception>(() => identity.CreateUserAsync("rollback@example.test", "StrongPassword123!", "Player", "dcs-game"));
        await using var db = database.Auth();
        Assert.Empty(await db.Users.ToListAsync());
        Assert.Empty(await db.Set<OutboxMessage>().ToListAsync());
    }

    [AuthSqlFact]
    public async Task UnknownApplicationDoesNotCreateUser()
    {
        await using var database = await AuthTestDatabase.CreateAsync();
        await using var provider = Services(database);
        await using var scope = provider.CreateAsyncScope();
        Assert.False((await scope.ServiceProvider.GetRequiredService<IIdentityService>()
            .CreateUserAsync("unknown@example.test", "StrongPassword123!", "Player", "unknown")).Succeeded);
        await using var db = database.Auth();
        Assert.Empty(await db.Users.ToListAsync());
        Assert.Empty(await db.Set<OutboxMessage>().ToListAsync());
    }

    private static ServiceProvider Services(AuthTestDatabase database)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddAuthentication();
        services.AddScoped(_ => database.Auth());
        services.AddScoped(_ => database.Application());
        services.AddIdentityCore<ApplicationUser>().AddSignInManager().AddEntityFrameworkStores<AuthDbContext>();
        services.AddScoped<IUnitOfWork<AuthDbContext>, UnitOfWork<AuthDbContext>>();
        services.AddScoped<IUnitOfWork<ApplicationDbContext>, UnitOfWork<ApplicationDbContext>>();
        services.AddScoped<IOutboxWriter, EfOutboxWriter<AuthDbContext>>();
        services.AddScoped<IIdentityService, IdentityService>();
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }
}

using AUTH.Infrastructure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AUTH.Tests;

public sealed class AuthDatabaseMigrationTests
{
    [AuthSqlFact]
    public async Task MovesRowsIndexesIdentityAndForeignKeysWithoutTouchingGameOutbox()
    {
        await using var database = await AuthTestDatabase.CreateAsync();
        await using (var db = database.Auth())
        {
            db.Users.Add(new ApplicationUser { Id = "test-user", UserName = "player", NormalizedUserName = "PLAYER", Email = "player@example.test", PasswordHash = "fake-test-hash" });
            await db.SaveChangesAsync();
        }
        await database.ExecuteAsync("""
            ALTER TABLE dbo.HRK_Users ADD LegacyNote nvarchar(40) NULL;
            ALTER TABLE dbo.HRK_LoginSessions ADD IsActive AS(CASE WHEN LogoutTime IS NULL THEN 1 ELSE 0 END);
            ALTER TABLE dbo.HRK_LoginSessions ADD UserTag AS(UserId+N'_session') PERSISTED;
            GO
            UPDATE dbo.HRK_Users SET FullName=N'Người chơi mới',LegacyNote=N'Unmapped legacy column';
            INSERT dbo.HRK_LoginSessions(Id,UserId,SessionId,LoginTime) VALUES(NEWID(),N'test-user',NEWID(),SYSUTCDATETIME());
            INSERT dbo.HRK_UserClaims(UserId,ClaimType,ClaimValue) VALUES(N'test-user',N'test',N'value');
            DBCC CHECKIDENT('dbo.HRK_UserClaims',RESEED,50) WITH NO_INFOMSGS;
            INSERT dbo.HRK_UserClaims(UserId,ClaimType,ClaimValue) VALUES(N'test-user',N'temporary',N'value');
            DELETE dbo.HRK_UserClaims;
            CREATE TABLE dbo.HRK_Players(Id bigint PRIMARY KEY,UserId nvarchar(450) NOT NULL,
                CONSTRAINT FK_Test_Players_Users FOREIGN KEY(UserId) REFERENCES dbo.HRK_Users(Id));
            INSERT dbo.HRK_Players VALUES(1,N'test-user');
            CREATE TABLE dbo.HRK_OutboxMessages(Id uniqueidentifier PRIMARY KEY);
            INSERT dbo.HRK_OutboxMessages VALUES(NEWID());
            """);
        await database.ApplyScriptAsync("Migrate_HRK_AUTH.sql");
        await database.ApplyScriptAsync("Migrate_HRK_AUTH.sql");
        await database.ApplyScriptAsync("Create_AuthMessaging.sql");
        await database.ApplyScriptAsync("Create_AuthMessaging.sql");
        await using var target = database.Auth(database.Target);
        var user = await target.Users.SingleAsync();
        Assert.Equal("fake-test-hash", user.PasswordHash);
        await database.ExecuteAsync("""
            IF OBJECT_ID(N'dbo.HRK_Users',N'U') IS NOT NULL THROW 51000,'Source users must be removed',1;
            IF (SELECT COUNT(*) FROM dbo.HRK_Players)<>1 THROW 51000,'Player changed',1;
            IF (SELECT COUNT(*) FROM dbo.HRK_OutboxMessages)<>1 THROW 51000,'Game outbox changed',1;
            IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'dbo.HRK_Players')) THROW 51000,'Player FK remains',1;
            """);
        await database.ExecuteAsync("""
            IF (SELECT FullName FROM dbo.HRK_Users WHERE Id=N'test-user')<>N'Người chơi mới' THROW 51000,'Text changed',1;
            IF (SELECT LegacyNote FROM dbo.HRK_Users WHERE Id=N'test-user')<>N'Unmapped legacy column' THROW 51000,'Legacy column lost',1;
            IF NOT EXISTS(SELECT 1 FROM sys.computed_columns WHERE object_id=OBJECT_ID(N'dbo.HRK_LoginSessions') AND name=N'IsActive' AND is_persisted=0) THROW 51000,'Computed session status lost',1;
            IF NOT EXISTS(SELECT 1 FROM sys.computed_columns WHERE object_id=OBJECT_ID(N'dbo.HRK_LoginSessions') AND name=N'UserTag' AND is_persisted=1) THROW 51000,'Persisted computed column lost',1;
            IF (SELECT IsActive FROM dbo.HRK_LoginSessions)<>1 THROW 51000,'Initial session status changed',1;
            UPDATE dbo.HRK_LoginSessions SET LogoutTime=SYSUTCDATETIME();
            IF (SELECT IsActive FROM dbo.HRK_LoginSessions)<>0 THROW 51000,'Computed session status no longer updates',1;
            IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.HRK_Users') AND name=N'UserNameIndex' AND is_unique=1) THROW 51000,'Unique identity index lost',1;
            IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'dbo.HRK_UserClaims')) THROW 51000,'Identity FK lost',1;
            INSERT dbo.HRK_UserClaims(UserId,ClaimType,ClaimValue) VALUES(N'test-user',N'test2',N'value2');
            IF NOT EXISTS(SELECT 1 FROM dbo.HRK_UserClaims WHERE Id=52) THROW 51000,'Identity sequence lost for empty table',1;
            """, database.Target);
    }

    [AuthSqlFact]
    public async Task UnexpectedExternalForeignKeyStopsBeforeDeletingSource()
    {
        await using var database = await AuthTestDatabase.CreateAsync();
        await database.ExecuteAsync("CREATE TABLE dbo.UnexpectedConsumer(Id int PRIMARY KEY,UserId nvarchar(450) REFERENCES dbo.HRK_Users(Id));");
        var error = await Assert.ThrowsAsync<SqlException>(() => database.ApplyScriptAsync("Migrate_HRK_AUTH.sql"));
        Assert.Contains("Unexpected external foreign key", error.Message);
        await database.ExecuteAsync("IF OBJECT_ID(N'dbo.HRK_Users',N'U') IS NULL THROW 51000,'Source disappeared',1;");
    }

    [AuthSqlFact]
    public async Task ExistingTargetIsNeverOverwritten()
    {
        await using var database = await AuthTestDatabase.CreateAsync();
        await using (var target = database.Auth(database.Target)) await target.Database.EnsureCreatedAsync();
        var error = await Assert.ThrowsAsync<SqlException>(() => database.ApplyScriptAsync("Migrate_HRK_AUTH.sql"));
        Assert.Contains("Refusing to overwrite", error.Message);
        await database.ExecuteAsync("IF OBJECT_ID(N'dbo.HRK_Users',N'U') IS NULL THROW 51000,'Source disappeared',1;");
    }
}

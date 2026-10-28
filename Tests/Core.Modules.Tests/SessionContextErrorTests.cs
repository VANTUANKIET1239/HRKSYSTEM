using System.Security.Claims;
using Core.Common.Constants.Common;
using Core.Common.Errors;
using Core.Web.CQRS;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Core.Modules.Tests;

public sealed class SessionContextErrorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("not-a-session-id")]
    public void MissingOrMalformedSessionIsAnExpectedUnauthorizedFailure(string? session)
    {
        var accessor = Accessor(session);
        var command = Assert.Throws<BusinessException>(() => new Command(accessor).Session());
        var query = Assert.Throws<BusinessException>(() => new Query(accessor).Session());
        foreach (var error in new[] { command, query })
        {
            Assert.Equal("INVALID_SESSION", error.Code);
            Assert.Equal(BusinessErrorKind.Unauthorized, error.Kind);
            Assert.Equal("Phiên đăng nhập không hợp lệ.", error.Message);
        }
    }

    [Fact]
    public void ValidSessionIsReturnedUnchanged()
    {
        var id = Guid.NewGuid();
        var accessor = Accessor(id.ToString());
        Assert.Equal(id, new Command(accessor).Session());
        Assert.Equal(id, new Query(accessor).Session());
    }

    private static HttpContextAccessor Accessor(string? session) => new()
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(session is null ? [] :
                new[] { new Claim(Constants.JSON_WEB_TOKEN.SESSIONID, session) }, "test"))
        }
    };

    private sealed class Command(IHttpContextAccessor accessor) : HRKBaseCommand(accessor)
    { public Guid Session() => GetCurrentLoginSession(); }
    private sealed class Query(IHttpContextAccessor accessor) : HRKBaseQuery(accessor)
    { public Guid Session() => GetCurrentLoginSession(); }
}

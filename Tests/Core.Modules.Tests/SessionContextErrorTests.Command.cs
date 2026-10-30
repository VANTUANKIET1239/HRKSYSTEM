using System.Security.Claims;
using Core.Common.Constants.Common;
using Core.Common.Errors;
using Core.Web.CQRS;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Core.Modules.Tests;
public sealed partial class SessionContextErrorTests
{
    private sealed class Command(IHttpContextAccessor accessor) : HRKBaseCommand(accessor)
    {
        public Guid Session() => GetCurrentLoginSession();
    }
}

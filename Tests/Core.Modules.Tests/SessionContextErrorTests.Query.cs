using System.Security.Claims;
using Core.Common.Constants.Common;
using Core.Common.Errors;
using Core.Web.CQRS;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Core.Modules.Tests;
public sealed partial class SessionContextErrorTests
{
    private sealed class Query(IHttpContextAccessor accessor) : HRKBaseQuery(accessor)
    {
        public Guid Session() => GetCurrentLoginSession();
    }
}

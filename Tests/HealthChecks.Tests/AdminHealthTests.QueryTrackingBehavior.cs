using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Core.HealthChecks;
using Core.InternalApi;
using Admin.Application.DTOs.HealthMonitoring;
using Admin.Infrastructure.DependencyInjection;
using Admin.Application.Features.Queries.HealthMonitoring;
using MediatR;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Web.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace HealthChecks.Tests;
public sealed partial class AdminHealthTests
{
    private sealed class QueryTrackingBehavior : IPipelineBehavior<GetServicesHealthQuery, BaseResponse<SystemHealthReport>>
    {
        public int Calls;
        public Task<BaseResponse<SystemHealthReport>> Handle(GetServicesHealthQuery request, RequestHandlerDelegate<BaseResponse<SystemHealthReport>> next, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Calls);
            return next();
        }
    }
}

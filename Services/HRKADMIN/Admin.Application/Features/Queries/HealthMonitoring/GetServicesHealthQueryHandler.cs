using Admin.Application.DTOs.HealthMonitoring;
using Admin.Application.Interfaces.HealthMonitoring;
using Core.Web.CQRS;
using Microsoft.AspNetCore.Http;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace Admin.Application.Features.Queries.HealthMonitoring;

public sealed class GetServicesHealthQueryHandler(IHttpContextAccessor httpContextAccessor, IServiceHealthAggregator aggregator)
    : HRKBaseQuery<GetServicesHealthQuery, BaseResponse<SystemHealthReport>>(httpContextAccessor)
{
    public override async Task<BaseResponse<SystemHealthReport>> Handle(GetServicesHealthQuery request, CancellationToken cancellationToken) =>
        BaseResponse<SystemHealthReport>.SuccessResponse(await aggregator.CheckAsync(cancellationToken));
}

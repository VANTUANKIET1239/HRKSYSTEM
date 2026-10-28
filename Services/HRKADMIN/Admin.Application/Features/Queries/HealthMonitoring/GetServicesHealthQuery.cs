using Admin.Application.DTOs.HealthMonitoring;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace Admin.Application.Features.Queries.HealthMonitoring;

public sealed record GetServicesHealthQuery : IQuery<BaseResponse<SystemHealthReport>>;

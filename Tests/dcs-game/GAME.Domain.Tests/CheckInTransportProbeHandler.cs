using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.DTOs;
using GAME.Application.Features.Commands.CheckIn;
using GAME.Application.Features.Queries.CheckIn;
using MediatR;

namespace GAME.Domain.Tests;

public sealed class CheckInTransportProbeHandler(CqrsTransportProbe probe)
    : IRequestHandler<GetCheckInCalendarQuery, BaseResponse<CheckInCalendarDto>>,
      IRequestHandler<ClaimCheckInCommand, BaseResponse<CheckInClaimResultDto>>
{
    public Task<BaseResponse<CheckInCalendarDto>> Handle(GetCheckInCalendarQuery request, CancellationToken ct)
    {
        probe.Requests.Add(request);
        return Task.FromResult(BaseResponse<CheckInCalendarDto>.SuccessResponse(new(null, "Điểm danh", null, null,
            DateTime.UtcNow, null, false, false, null, 0, [])));
    }
    public Task<BaseResponse<CheckInClaimResultDto>> Handle(ClaimCheckInCommand request, CancellationToken ct)
    {
        probe.Requests.Add(request);
        return Task.FromResult(BaseResponse<CheckInClaimResultDto>.SuccessResponse(new(request.RequestId, 1,
            new DateOnly(2026, 10, 21), DateTime.UtcNow, 1, false, [])));
    }
}

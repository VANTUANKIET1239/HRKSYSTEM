using Core.Common.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace GAME.Application.Features.Queries.Formation
{
    public record GetPlayerFormationQuery(string FormationName = "Main Team") : IQuery<BaseResponse<FormationDto>>;

    public class GetPlayerFormationQueryHandler : HRKBaseQuery, IQueryHandler<GetPlayerFormationQuery, BaseResponse<FormationDto>>
    {
        private readonly IFormationService _formationService;

        public GetPlayerFormationQueryHandler(IFormationService formationService, IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
        {
            _formationService = formationService;
        }

        public async Task<BaseResponse<FormationDto>> Handle(GetPlayerFormationQuery request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var formation = await _formationService.GetPlayerFormationAsync(userId, request.FormationName, cancellationToken);
            return BaseResponse<FormationDto>.SuccessResponse(formation, "Retrieved player formation successfully.");
        }
    }
}

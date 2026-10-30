using Core.Application.CQRS;
using GAME.Application.Templates;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Domain.Entities;

namespace GAME.Application.Features.Queries.GameConfig;

public sealed class GetModalConfigsQueryHandler(IUnitOfWork unitOfWork, IGameTemplateCatalog? templates = null)
    : IQueryHandler<GetModalConfigsQuery, BaseResponse<List<ModalConfigDto>>>
{
    public async Task<BaseResponse<List<ModalConfigDto>>> Handle(GetModalConfigsQuery request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var catalog = templates ?? new DatabaseTemplateCatalog(unitOfWork);
        var result = (await catalog.GetAllAsync<HrkModalConfig>(ct))
            .Where(config => config.IsActive).OrderBy(config => config.Code)
            .Select(config => new ModalConfigDto(config.Code, config.Kind, config.Title, config.MessageTemplate,
                config.ConfirmText, config.CancelText, config.IconKey, config.Size, config.Tone,
                config.CloseOnBackdrop, config.CloseOnEscape, config.Revision))
            .ToList();
        return BaseResponse<List<ModalConfigDto>>.SuccessResponse(result);
    }
}

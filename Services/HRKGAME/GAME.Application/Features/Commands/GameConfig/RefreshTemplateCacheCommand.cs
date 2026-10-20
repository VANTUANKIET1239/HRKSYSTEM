using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Common.Errors;
using GAME.Application.Templates;

namespace GAME.Application.Features.Commands.GameConfig;

public sealed record RefreshTemplateCacheCommand : ICommand<BaseResponse<TemplateRefreshResult>>;

public sealed class RefreshTemplateCacheCommandHandler(IGameTemplateRefresh refresh, IGameTemplateRefreshAccess access)
    : ICommandHandler<RefreshTemplateCacheCommand, BaseResponse<TemplateRefreshResult>>
{
    public async Task<BaseResponse<TemplateRefreshResult>> Handle(RefreshTemplateCacheCommand request, CancellationToken ct)
    {
        if (!access.CanRefresh)
            throw new BusinessException("TEMPLATE_REFRESH_FORBIDDEN", "Bạn không có quyền đồng bộ template.", BusinessErrorKind.Forbidden);
        return BaseResponse<TemplateRefreshResult>.SuccessResponse(await refresh.RefreshAsync(ct), "Đã đồng bộ template.");
    }
}

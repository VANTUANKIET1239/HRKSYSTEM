using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Common.Errors;
using GAME.Application.Templates;

namespace GAME.Application.Features.Commands.GameConfig;

public sealed record RefreshTemplateCacheCommand : ICommand<BaseResponse<TemplateRefreshResult>>;

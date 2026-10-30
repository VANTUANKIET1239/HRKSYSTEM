using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Templates;

namespace GAME.Application.Features.Queries.GameConfig;

public sealed record TemplateCacheCapabilitiesDto(bool CanRefresh);

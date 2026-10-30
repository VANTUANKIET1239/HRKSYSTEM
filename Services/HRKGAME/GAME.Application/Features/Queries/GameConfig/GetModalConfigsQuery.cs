using Core.Application.CQRS;
using GAME.Application.Templates;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Domain.Entities;

namespace GAME.Application.Features.Queries.GameConfig;

public sealed record GetModalConfigsQuery : IQuery<BaseResponse<List<ModalConfigDto>>>;

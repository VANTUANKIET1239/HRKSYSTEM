using GAME.Application.Templates;
using Core.Persistence.Repositories;
using GAME.Application.Common.Helpers;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Queries.Catalog;

public record GetItemTemplatesQuery(int? CategoryId = null, int? RarityId = null) : IQuery<BaseResponse<List<ItemTemplateDto>>>;

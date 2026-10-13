using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Queries.Metadata;

public record GetSkillEnumsQuery : IQuery<BaseResponse<SkillEnumsDto>>;

public sealed class GetSkillEnumsQueryHandler(IUnitOfWork unitOfWork) : IQueryHandler<GetSkillEnumsQuery, BaseResponse<SkillEnumsDto>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    public async Task<BaseResponse<SkillEnumsDto>> Handle(GetSkillEnumsQuery request, CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(cancellationToken);
        return BaseResponse<SkillEnumsDto>.SuccessResponse(result, "Retrieved skill enums successfully.");
    }

    private async Task<SkillEnumsDto> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var effectTypes = await _unitOfWork.ReadOnlyRepository<HrkSkillEffectType>().Query()
            .Select(x => new LookupItemDto { Id = x.Id, Code = x.Code, Name = x.Name, IsDebuff = !x.IsBeneficial })
            .ToListAsync(cancellationToken);
        return new SkillEnumsDto
        {
            CostTypes = new List<LookupItemDto>
            {
                new()
                {
                    Id = 1,
                    Code = "NONE",
                    Name = "Không tốn"
                },
                new()
                {
                    Id = 2,
                    Code = "MP",
                    Name = "Năng Lượng"
                }
            },
            Categories = new List<LookupItemDto>
            {
                new()
                {
                    Id = 1,
                    Code = "passive",
                    Name = "Nội tại",
                    DisplayOrder = 1
                },
                new()
                {
                    Id = 2,
                    Code = "active",
                    Name = "Chủ động",
                    DisplayOrder = 2
                },
                new()
                {
                    Id = 3,
                    Code = "ultimate",
                    Name = "Tuyệt kỹ",
                    DisplayOrder = 3
                }
            },
            DamageTypes = new List<LookupItemDto>
            {
                new()
                {
                    Id = 1,
                    Code = "PHYSICAL",
                    Name = "Vật lý"
                },
                new()
                {
                    Id = 2,
                    Code = "MAGICAL",
                    Name = "Phép thuật"
                },
                new()
                {
                    Id = 3,
                    Code = "TRUE",
                    Name = "Chuẩn"
                }
            },
            EffectTypes = effectTypes
        };
    }
}

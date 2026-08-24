using GAME.Application.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Application.Interfaces
{
    public interface ICatalogService
    {
        Task<List<HeroTemplateDto>> GetHeroTemplatesAsync(CancellationToken cancellationToken = default);
        Task<HeroTemplateDto?> GetHeroTemplateByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<List<SkillTemplateDto>> GetSkillTemplatesAsync(CancellationToken cancellationToken = default);
        Task<List<ItemTemplateDto>> GetItemTemplatesAsync(int? categoryId, int? rarityId, CancellationToken cancellationToken = default);
    }
}

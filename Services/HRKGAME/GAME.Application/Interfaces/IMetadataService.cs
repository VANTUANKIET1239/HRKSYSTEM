using GAME.Application.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Application.Interfaces
{
    public interface IMetadataService
    {
        Task<List<RarityDto>> GetRaritiesAsync(CancellationToken cancellationToken = default);
        Task<List<HeroFactionDto>> GetFactionsAsync(CancellationToken cancellationToken = default);
        Task<List<HeroClassDto>> GetClassesAsync(CancellationToken cancellationToken = default);
        Task<List<ItemCategoryDto>> GetItemCategoriesAsync(CancellationToken cancellationToken = default);
        Task<SkillEnumsDto> GetSkillEnumsAsync(CancellationToken cancellationToken = default);
    }
}

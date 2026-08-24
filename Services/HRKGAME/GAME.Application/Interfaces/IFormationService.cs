using GAME.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Application.Interfaces
{
    public interface IFormationService
    {
        Task<FormationDto> GetPlayerFormationAsync(string userId, string formationName = "Main Team", CancellationToken cancellationToken = default);
    }
}

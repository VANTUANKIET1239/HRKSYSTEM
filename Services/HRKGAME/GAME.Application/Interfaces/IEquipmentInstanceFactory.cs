using GAME.Application.DTOs;
using GAME.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Application.Interfaces
{
    public interface IEquipmentInstanceFactory
    {
        Task<EquipmentInstanceCreationResult> CreateAsync(
            long playerId,
            HrkItemTemplate template,
            EquipmentAcquisitionContext context,
            CancellationToken cancellationToken = default);
    }
}

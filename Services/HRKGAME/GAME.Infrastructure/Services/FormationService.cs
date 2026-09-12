using Core.Common.Repositories;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAME.Infrastructure.Services
{
    public class FormationService : IFormationService
    {
        private readonly IUnitOfWork<GameDbContext> _unitOfWork;
        private readonly IGamePlayerService _gamePlayerService;

        public FormationService(IUnitOfWork<GameDbContext> unitOfWork, IGamePlayerService gamePlayerService)
        {
            _unitOfWork = unitOfWork;
            _gamePlayerService = gamePlayerService;
        }

        public async Task<FormationDto> GetPlayerFormationAsync(string userId, string formationName = "Main Team", CancellationToken cancellationToken = default)
        {
            var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken);

            var positions = new List<FormationPositionDto>();
            int computedPower = 0;

            if (player != null)
            {
                var formation = await _unitOfWork.ReadOnlyRepository<HrkPlayerFormation>().Query()
                    .Include(f => f.Hero1).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Faction)
                    .Include(f => f.Hero1).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Class)
                    .Include(f => f.Hero1).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Rarity)
                    .Include(f => f.Hero1).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.CostType)

                    .Include(f => f.Hero2).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Faction)
                    .Include(f => f.Hero2).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Class)
                    .Include(f => f.Hero2).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Rarity)
                    .Include(f => f.Hero2).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.CostType)

                    .Include(f => f.Hero3).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Faction)
                    .Include(f => f.Hero3).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Class)
                    .Include(f => f.Hero3).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Rarity)
                    .Include(f => f.Hero3).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.CostType)

                    .Include(f => f.Hero4).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Faction)
                    .Include(f => f.Hero4).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Class)
                    .Include(f => f.Hero4).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Rarity)
                    .Include(f => f.Hero4).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.CostType)

                    .Include(f => f.Hero5).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Faction)
                    .Include(f => f.Hero5).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Class)
                    .Include(f => f.Hero5).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.Rarity)
                    .Include(f => f.Hero5).ThenInclude(h => h!.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.CostType)
                    .FirstOrDefaultAsync(f => f.PlayerId == player.Id && f.FormationName == formationName && f.IsActive, cancellationToken);

                if (formation != null)
                {
                    var h1 = (formation.Hero1 != null && formation.Hero1.IsActive) ? GameDtoMapper.MapPlayerHero(formation.Hero1) : null;
                    var h2 = (formation.Hero2 != null && formation.Hero2.IsActive) ? GameDtoMapper.MapPlayerHero(formation.Hero2) : null;
                    var h3 = (formation.Hero3 != null && formation.Hero3.IsActive) ? GameDtoMapper.MapPlayerHero(formation.Hero3) : null;
                    var h4 = (formation.Hero4 != null && formation.Hero4.IsActive) ? GameDtoMapper.MapPlayerHero(formation.Hero4) : null;
                    var h5 = (formation.Hero5 != null && formation.Hero5.IsActive) ? GameDtoMapper.MapPlayerHero(formation.Hero5) : null;

                    positions.Add(new FormationPositionDto { Slot = 1, Hero = h1 });
                    positions.Add(new FormationPositionDto { Slot = 2, Hero = h2 });
                    positions.Add(new FormationPositionDto { Slot = 3, Hero = h3 });
                    positions.Add(new FormationPositionDto { Slot = 4, Hero = h4 });
                    positions.Add(new FormationPositionDto { Slot = 5, Hero = h5 });

                    computedPower = (h1?.Power ?? 0) + (h2?.Power ?? 0) + (h3?.Power ?? 0) + (h4?.Power ?? 0) + (h5?.Power ?? 0);

                    return new FormationDto
                    {
                        PlayerId = player.Id,
                        FormationName = formationName,
                        TotalPower = formation.TotalPower > 0 ? formation.TotalPower : computedPower,
                        Positions = positions
                    };
                }
            }

            for (int i = 1; i <= 5; i++)
            {
                positions.Add(new FormationPositionDto { Slot = i, Hero = null });
            }

            return new FormationDto
            {
                PlayerId = player?.Id ?? 0,
                FormationName = formationName,
                TotalPower = 0,
                Positions = positions
            };
        }
    }
}

using GAME.Application.DTOs;
using GAME.Domain.Entities;
using System.Collections.Generic;

namespace GAME.Application.Interfaces
{
    public class HeroStatCalculationResult
    {
        public CalculatedStatsDto FinalStats { get; set; } = new();
        public List<HeroStatBreakdownDto> Breakdowns { get; set; } = new();
    }
}

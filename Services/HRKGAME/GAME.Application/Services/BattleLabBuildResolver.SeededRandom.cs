using GAME.Application.Templates;
using Core.Common.Errors;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Core.Persistence.Repositories;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;

namespace GAME.Application.Services;
public sealed partial class BattleLabBuildResolver
{
    private sealed class SeededRandom(int seed) : IRandomService
    {
        private readonly System.Random random = new(seed);
        public double NextDouble() => random.NextDouble();
        public int Next(int minValue, int maxValue) => random.Next(minValue, maxValue);
        public decimal NextDecimal(decimal minValue, decimal maxValue, int decimals = 4) => Math.Round(minValue + (maxValue - minValue) * (decimal)random.NextDouble(), decimals);
    }
}

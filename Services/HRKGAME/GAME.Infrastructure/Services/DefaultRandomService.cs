using System;
using GAME.Application.Interfaces;

namespace GAME.Infrastructure.Services
{
    public class DefaultRandomService : IRandomService
    {
        private readonly System.Random _random;

        public DefaultRandomService()
        {
            _random = new System.Random();
        }

        public DefaultRandomService(int seed)
        {
            _random = new System.Random(seed);
        }

        public double NextDouble()
        {
            return _random.NextDouble();
        }

        public int Next(int minValue, int maxValue)
        {
            if (minValue >= maxValue) return minValue;
            return _random.Next(minValue, maxValue);
        }

        public decimal NextDecimal(decimal minValue, decimal maxValue, int decimals = 4)
        {
            if (minValue >= maxValue) return minValue;
            double range = (double)(maxValue - minValue);
            double sample = _random.NextDouble() * range + (double)minValue;
            return Math.Round((decimal)sample, decimals, MidpointRounding.AwayFromZero);
        }
    }
}

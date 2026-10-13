using System.Security.Cryptography;
using GAME.Application.Interfaces;

namespace GAME.Infrastructure.Random;

public sealed class CryptoSummonRandom : ISummonRandom
{
    public int Next(int exclusiveMaximum) => RandomNumberGenerator.GetInt32(exclusiveMaximum);
}

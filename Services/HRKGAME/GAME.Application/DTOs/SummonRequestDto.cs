namespace GAME.Application.DTOs;

public sealed record SummonRequestDto(Guid RequestId, string PoolCode, int Count);

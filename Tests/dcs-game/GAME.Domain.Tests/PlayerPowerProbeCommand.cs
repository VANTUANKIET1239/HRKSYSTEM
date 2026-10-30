using Core.Application.CQRS;
using GAME.Application.Interfaces;

namespace GAME.Domain.Tests;

public sealed record PlayerPowerProbeCommand(string UserId, int GoldDelta = 1) : ICommand<int>, IPlayerPowerChangeCommand;

namespace GAME.Application.Interfaces;

/// <summary>
/// Marks a command whose business changes and power record must commit together.
/// Commands with an explicit identity expose UserId; authenticated commands return null
/// and the pipeline resolves the current caller through IGameRequestContext.
/// </summary>
public interface IPlayerPowerChangeCommand
{
    string? UserId { get; }
}

namespace GAME.Application.Interfaces;
/// <summary>Identity of the authenticated caller. Worker commands carry their own identity.</summary>
public interface IGameRequestContext
{
    string UserId { get; }
}

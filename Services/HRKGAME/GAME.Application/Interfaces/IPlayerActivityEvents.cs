using GAME.Application.Events;

namespace GAME.Application.Interfaces;

public interface IPlayerActivityEvents
{
    // Stage an event in the current Game transaction. No remote call or commit.
    void Raise(PlayerActivityEvent activity);
}

using GAME.Domain.Battle;
using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Targets;
using Xunit;

namespace GAME.Domain.Tests.Battle;
public sealed partial class BattleSimulationEngineTests
{
    private sealed class TestTargetSelector : IBattleTargetSelector
    {
        public string TargetTypeCode => "CUSTOM_TARGET";
        public bool WasCalled { get; private set; }

        public IReadOnlyList<BattleCombatant> Select(BattleTargetContext context)
        {
            WasCalled = true;
            return context.Enemies.Take(1).ToList();
        }
    }
}

using GAME.Domain.Battle;
using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Targets;
using Xunit;

namespace GAME.Domain.Tests.Battle;
public sealed partial class BattleSimulationEngineTests
{
    private sealed class TestEffectHandler : IBattleEffectHandler
    {
        public string EffectTypeCode => "CUSTOM";
        public bool WasCalled { get; private set; }

        public IReadOnlyList<PendingBattleEvent> Apply(BattleEffectContext context)
        {
            WasCalled = true;
            return[];
        }
    }
}

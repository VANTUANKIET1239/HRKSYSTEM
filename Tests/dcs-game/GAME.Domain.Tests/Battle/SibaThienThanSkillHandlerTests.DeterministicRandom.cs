using GAME.Domain.Battle;
using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Skills;
using GAME.Domain.Battle.Skills.SibaThienThan;
using GAME.Domain.Battle.Targets;
using Xunit;

namespace GAME.Domain.Tests.Battle;
public sealed partial class SibaThienThanSkillHandlerTests
{
    private sealed class DeterministicRandom : Random
    {
        private readonly int _fixedValue;
        public DeterministicRandom(int fixedValue) => _fixedValue = fixedValue;
        public override int Next(int minValue, int maxValue) => _fixedValue;
    }
}

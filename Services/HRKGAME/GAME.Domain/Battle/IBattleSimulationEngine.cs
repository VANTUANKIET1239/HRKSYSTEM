using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Targets;
using GAME.Domain.Battle.Skills;
using GAME.Domain.Battle.Skills.QaKyTinh;
using GAME.Domain.Battle.Skills.HaiLastSmile;
using GAME.Domain.Battle.Skills.ChuanMen;
using GAME.Domain.Battle.Skills.ThanhThaiAura;
using GAME.Domain.Battle.Skills.NghiaPhucPrime;
using GAME.Domain.Battle.Skills.SibaThienThan;
using GAME.Domain.Battle.Reactions;

namespace GAME.Domain.Battle;

public interface IBattleSimulationEngine
{
    bool CanHandleEffect(string effectTypeCode);
    BattleSimulationResult Simulate(BattleSimulationRequest request);
}

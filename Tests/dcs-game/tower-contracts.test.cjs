const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const root = path.resolve(__dirname, '../../HrkUi');
const ts = require(path.join(root, 'node_modules/typescript'));
const source = fs.readFileSync(path.join(root, 'src/app/dcs-game/src/app/core/services/tower-response.mapper.ts'), 'utf8');
const output = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS } }).outputText;
const context = { exports: {}, Date };
vm.runInNewContext(output, context);
const m = context.exports;

test('progress maps actual server fields and retains floors/lead hero', () => {
  const p = m.mapProgress({
    currentFloor: 3, maxFloor: 90, periodEndUtc: '2026-10-02T05:00:00Z', hasActiveQuickClimb: true,
    leadHero: { id: 17, name: 'Test' }, floors: [
      { floorNumber: 3, state: 'CURRENT', hasMilestoneChest: true, representativeEnemy: { name: 'Boss' } }
    ]
  });
  assert.equal(p.hasActiveQuickClimbJob, true);
  assert.equal(p.leadHero.heroId, 17);
  assert.equal(p.maxFloor, 90);
  assert.equal(p.floors[0].isCurrent, true);
  assert.equal(p.floors[0].representativeEnemyName, 'Boss');
});
test('floor eligibility comes from canStart, not an invented isUnlocked field', () => {
  assert.equal(m.mapFloorDetail({ canStart: false, state: 'CURRENT', rewards: [] }).isUnlocked, false);
  assert.equal(m.mapFloorDetail({ canStart: true, state: 'CURRENT', rewards: [] }).isUnlocked, true);
});
test('result uses persisted battle/heroes and never fabricates life counts', () => {
  const r = m.mapResult({ battle: { battleId: 'b' }, heroes: [{ playerHeroId: 2 }],
    livesBefore: 3, livesAfter: 2, floorNumber: 29, isMilestoneChestUnlocked: true });
  assert.equal(r.battleSimulation.battleId, 'b');
  assert.equal(r.heroExpResults[0].playerHeroId, 2);
  assert.equal(r.livesBefore, 3);
  assert.equal(r.livesAfter, 2);
  assert.equal(r.floorNumber, 29);
  assert.equal(r.milestoneUnlocked, true);
});
test('expired jobs are terminal and stop polling', () => {
  assert.equal(m.isQuickClimbFinished('EXPIRED'), true);
  assert.equal(m.isQuickClimbFinished('PROCESSING'), false);
});
test('pending rewards map rewardItems', () => {
  assert.equal(m.mapPending({ rewardItems: [{ quantity: 9 }] }).items[0].quantity, 9);
});
test('tower URLs match controller routes', () => {
  const endpoints = fs.readFileSync(path.join(root, 'src/libs/shared/common/constants/api-endpoints.ts'), 'utf8');
  assert.ok(endpoints.includes('tower/floors/${floorNumber}/start'));
  assert.ok(endpoints.includes('tower/quick-climb/${jobId}/stop'));
  assert.ok(endpoints.includes('tower/quick-climb/active'));
  assert.ok(!endpoints.includes('tower/battle/start'));
});

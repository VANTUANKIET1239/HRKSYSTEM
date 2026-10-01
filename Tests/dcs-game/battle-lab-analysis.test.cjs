const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const ui = path.resolve(__dirname, '../../HrkUi');
const ts = require(path.join(ui, 'node_modules/typescript'));
const source = fs.readFileSync(path.join(ui, 'src/app/dcs-game/src/app/core/models/battle-lab-analysis.ts'), 'utf8');
const context = { exports: {} };
vm.runInNewContext(ts.transpileModule(source, {
  compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 }
}).outputText, context);
const { createBalanceAnalysis } = context.exports;

test('analysis contains distributions, survival, metadata and bounded uncertainty', () => {
  const stat = { combatantId: 1, team: 0, heroName: 'Hero', physicalDamageDealt: 100,
    magicDamageDealt: 0, healingDone: 0, physicalDamageTaken: 10, magicDamageTaken: 0 };
  const report = { createdAtUtc: 'date', engineVersion: 'v', snapshotHash: 'hash', settings: {},
    battleConfigs: {}, resolvedSnapshot: {}, resolvedBuilds: [], warnings: [], skills: [], totals: [stat],
    runs: [
      { seed: 1, winner: 'LEFT', rounds: 2, heroes: [{ statistics: stat, remainingHp: 10, skillCasts: 2, energyCasts: 1 }] },
      { seed: 2, winner: 'RIGHT', rounds: 4, heroes: [{ statistics: { ...stat, physicalDamageDealt: 300 }, remainingHp: 0, skillCasts: 4, energyCasts: 2 }] }
    ] };
  const analysis = createBalanceAnalysis(report);
  assert.equal(analysis.snapshotHash, 'hash');
  assert.equal(analysis.summary.leftWinRate, .5);
  assert.equal(analysis.heroes[0].physicalDamage.mean, 200);
  assert.equal(analysis.heroes[0].physicalDamage.standardDeviation, 100);
  assert.equal(analysis.heroes[0].survivalRate, .5);
  assert.equal(analysis.summary.rounds.mean, 3);
  assert.ok(analysis.summary.leftWinRateWilson95[0] >= 0);
  assert.ok(analysis.summary.leftWinRateWilson95[1] <= 1);
  assert.ok(analysis.warnings.some(w => w.includes('Small sample')));
  assert.equal(analysis.replays, undefined);
});

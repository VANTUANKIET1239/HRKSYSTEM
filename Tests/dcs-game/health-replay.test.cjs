const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const ui = path.resolve(__dirname, '../../HrkUi');
const ts = require(path.join(ui, 'node_modules/typescript'));
const replay = 'src/app/dcs-game/src/app/features/battle/replay';

function load(relativePath) {
  const source = fs.readFileSync(path.join(ui, replay, relativePath), 'utf8');
  const output = ts.transpileModule(source, {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 }
  }).outputText;
  const context = { exports: {} };
  vm.runInNewContext(output, context);
  return context.exports;
}

const { HealthEventHandler } = load('handlers/health-event.handler.ts');
const { BattleEventDispatcher } = load('battle-event-dispatcher.ts');

function setup() {
  let heroes = [{ id: -9505, hp: 16261 }, { id: 10003, hp: 20000 }, { id: 10004, hp: 4122 }];
  const texts = [];
  const dispatcher = new BattleEventDispatcher([new HealthEventHandler()]);
  const context = {
    updateHeroes: updater => { heroes = updater(heroes); },
    showCombatText: event => texts.push(event)
  };
  return {
    dispatch: event => dispatcher.dispatch(event, context),
    hp: id => heroes.find(hero => hero.id === id).hp,
    texts
  };
}

const redirect = {
  sequence: 271, eventType: 'PRIME_GUARD_REDIRECTED', actorId: -9505,
  targetId: 10003, sourceHeroId: 10004, value: 55, hpBefore: 4122, hpAfter: 4122
};

test('logged redirect never replaces Long HP with guardian HP', () => {
  const replay = setup();
  replay.dispatch(redirect);
  assert.equal(replay.hp(-9505), 16261);
  assert.equal(replay.hp(10003), 20000);
  assert.equal(replay.hp(10004), 4122);
  assert.equal(replay.texts.length, 0);
  replay.dispatch({ sequence: 272, eventType: 'DAMAGE', actorId: -9505,
    targetId: 10004, value: 0, hpBefore: 4122, hpAfter: 4122 });
  replay.dispatch({ sequence: 322, eventType: 'DAMAGE', actorId: 10003,
    targetId: -9505, value: 500, hpBefore: 16261, hpAfter: 15761 });
  assert.equal(replay.hp(-9505), 15761);
  assert.equal(replay.hp(10004), 4122);
});

test('guardian DAMAGE is the only HP update and combat text for redirected damage', () => {
  const replay = setup();
  replay.dispatch({ ...redirect, hpAfter: 4067, guardianHpAfter: 4067 });
  assert.equal(replay.hp(10004), 4122);
  replay.dispatch({ eventType: 'DAMAGE', actorId: -9505, targetId: 10004,
    value: 55, hpBefore: 4122, hpAfter: 4067 });
  assert.equal(replay.hp(-9505), 16261);
  assert.equal(replay.hp(10004), 4067);
  assert.equal(replay.texts.length, 1);
  assert.equal(replay.texts[0].targetId, 10004);
});

test('normal healing still updates its target', () => {
  const replay = setup();
  replay.dispatch({ eventType: 'HEAL', actorId: 10003, targetId: 10004,
    value: 100, hpBefore: 4122, hpAfter: 4222 });
  assert.equal(replay.hp(10004), 4222);
  assert.equal(replay.hp(10003), 20000);
  assert.equal(replay.texts.length, 1);
});

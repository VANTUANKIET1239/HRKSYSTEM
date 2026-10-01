const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const ui = path.resolve(__dirname, '../../HrkUi');
const ts = require(path.join(ui, 'node_modules/typescript'));
const source = fs.readFileSync(path.join(ui, 'src/app/dcs-game/src/app/core/models/battle-lab-config.ts'), 'utf8');
const context = { exports: {} };
vm.runInNewContext(ts.transpileModule(source, {
  compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 }
}).outputText, context);
const { parseLabConfig: parse, serializeLabConfig: serialize } = context.exports;
const heroes = [{ id: 7, heroTemplateId: 7, stats: {
  hp: 1000, atk: 100, def: 80, spd: 100, crit: 5, critDmg: 150,
  accuracy: 80, resistance: 10, lifesteal: 0, magicDamage: 50, magicResistance: 40
} }];
const config = () => ({ seed: 1, count: 100, maxRounds: 50, defenseConstant: 1000,
  left: [{ heroTemplateId: 7, position: 1, stats: { atk: 5509 } }],
  right: [{ heroTemplateId: 7, position: 1, stats: { def: 1100 } }] });

test('partial stats use catalog defaults without mutating inputs', () => {
  const value = parse(JSON.stringify(config()), heroes);
  assert.equal(value.left[0].stats.atk, 5509);
  assert.equal(value.left[0].stats.hp, 1000);
  assert.equal(heroes[0].stats.atk, 100);
  assert.equal(value.right[0].stats.def, 1100);
});
test('export round-trips with schemaVersion and accepts UTF-8 BOM', () => {
  const value = parse(JSON.stringify(config()), heroes);
  assert.equal(JSON.stringify(parse('\uFEFF' + serialize(value), heroes)), JSON.stringify(value));
});
test('rejects malformed JSON, unsupported version and typo keys', () => {
  assert.throws(() => parse('{', heroes), /JSON/);
  assert.throws(() => parse(JSON.stringify({ schemaVersion: 99, settings: config() }), heroes), /schemaVersion/);
  const value = config(); value.left[0].stats.attack = 100;
  assert.throws(() => parse(JSON.stringify(value), heroes), /attack/);
});
test('rejects unknown hero and duplicate positions', () => {
  const value = config(); value.left[0].heroTemplateId = 99;
  assert.throws(() => parse(JSON.stringify(value), heroes), /99/);
  const duplicate = config(); duplicate.left.push(duplicate.left[0]);
  assert.throws(() => parse(JSON.stringify(duplicate), heroes), /trùng/);
});
test('rejects invalid numeric values, null and overflow seeds', () => {
  for (const [key, invalid] of [['count', 301], ['seed', 2147483647], ['maxRounds', 0], ['defenseConstant', 0], ['count', '10']]) {
    const value = config(); value[key] = invalid;
    assert.throws(() => parse(JSON.stringify(value), heroes));
  }
  const value = config(); value.left[0].stats.hp = null;
  assert.throws(() => parse(JSON.stringify(value), heroes), /hp/);
});
test('rejects empty or oversized teams and fractional integer stats', () => {
  const value = config(); value.left = [];
  assert.throws(() => parse(JSON.stringify(value), heroes));
  value.left = Array(6).fill(config().left[0]);
  assert.throws(() => parse(JSON.stringify(value), heroes));
  const fractional = config(); fractional.left[0].stats.atk = 1.5;
  assert.throws(() => parse(JSON.stringify(fractional), heroes), /atk/);
});

test('v2 BUILD round-trips and excludes final stats', () => {
  const value = config();
  value.left[0] = { heroTemplateId: 7, position: 1, mode: 'BUILD',
    build: { level: 40, stars: 5, auraTier: 4, rollSeed: 42,
      equipment: [{ itemTemplateId: 10, enhancement: 5, stars: 2 }] } };
  const parsed = parse(JSON.stringify({ schemaVersion: 2, settings: value }), heroes);
  assert.equal(parsed.left[0].stats, undefined);
  assert.equal(parsed.left[0].build.level, 40);
  assert.equal(JSON.stringify(parse(serialize(parsed), heroes)), JSON.stringify(parsed));
  value.left[0].stats = { atk: 500 };
  assert.throws(() => parse(JSON.stringify(value), heroes), /BUILD/);
});

test('CUSTOM cannot include equipment build, invalid enhancement is rejected', () => {
  const value = config(); value.left[0].build = {};
  assert.throws(() => parse(JSON.stringify(value), heroes), /CUSTOM/);
  delete value.left[0].stats;
  value.left[0].mode = 'BUILD';
  value.left[0].build = { level: 40, stars: 5, auraTier: 4, rollSeed: 1,
    equipment: [{ itemTemplateId: 1, enhancement: 99, stars: 0 }] };
  assert.throws(() => parse(JSON.stringify(value), heroes), /enhancement/);
});

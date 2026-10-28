import fs from 'node:fs';
import path from 'node:path';
import { createRequire } from 'node:module';
import { pathToFileURL } from 'node:url';
const root = path.resolve(import.meta.dirname, '../..');
const require = createRequire(path.join(root, 'HrkUi/package.json'));
const ts = require('typescript');
const compiler = await import(pathToFileURL(path.join(root, 'HrkUi/node_modules/@angular/compiler/fesm2022/compiler.mjs')).href);
export function files(dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap(entry => entry.name === 'node_modules' || entry.name === 'obj' || entry.name === 'bin' || entry.name === '.git'
    ? [] : entry.isDirectory() ? files(path.join(dir, entry.name)) : [path.join(dir, entry.name)]);
}
const entries = new Map();
function add(text, file) {
  text = text?.trim();
  if (!text || !/[A-Za-zÀ-ỹ]/.test(text) || text.length < 2) return;
  if (!entries.has(text)) entries.set(text, new Set());
  entries.get(text).add(path.relative(root, file).replaceAll('\\', '/'));
}
for (const file of files(path.join(root, 'Services')).concat(files(path.join(root, 'CoreEngine/Core.Web')), files(path.join(root, 'CoreEngine/Core.Common'))).filter(file => file.endsWith('.cs'))) {
  const source = fs.readFileSync(file, 'utf8');
  for (const match of source.matchAll(/"((?:[^"\\\r\n]|\\.)*)"/g)) {
    const value = match[1].replaceAll('\\"', '"').replaceAll('\\n', '\n');
    if (/[À-ỹ]/.test(value) || /BusinessException|FailResponse|UnauthorizedResponse|NotFound\(|BadRequest\(/.test(source.slice(Math.max(0, match.index - 90), match.index))) add(value, file);
  }
}
for (const file of files(path.join(root, 'HrkUi/src')).filter(file => file.endsWith('.ts') && !file.endsWith('.spec.ts'))) {
  const source = fs.readFileSync(file, 'utf8');
  const ast = ts.createSourceFile(file, source, ts.ScriptTarget.Latest, true);
  function visit(node) {
    if (ts.isStringLiteralLike(node) && (/[À-ỹ]/.test(node.text) ||
      (node.parent && ts.isPropertyAssignment(node.parent) && /label|title|message|Text|placeholder/.test(node.parent.name.getText(ast))))) add(node.text, file);
    ts.forEachChild(node, visit);
  }
  visit(ast);
}
for (const file of files(path.join(root, 'HrkUi/src')).filter(file => file.endsWith('.html'))) {
  const source = fs.readFileSync(file, 'utf8');
  const parsed = compiler.parseTemplate(source, file, { preserveWhitespaces: true });
  const seen = new Set();
  function visit(node) {
    if (!node || typeof node !== 'object' || seen.has(node)) return;
    seen.add(node);
    if (node.constructor.name === 'Text') add(node.value, file);
    if (node.constructor.name === 'TextAttribute' && /^(title|placeholder|aria-label|alt|label|text|confirmText|cancelText)$/.test(node.name)) add(node.value, file);
    if (node.constructor.name === 'LiteralPrimitive' && typeof node.value === 'string') add(node.value, file);
    for (const [key, value] of Object.entries(node)) {
      if (['sourceSpan','keySpan','valueSpan','nameSpan','i18n','parent','span','absoluteSourceSpan'].includes(key)) continue;
      if (Array.isArray(value)) value.forEach(visit);
      else if (value && typeof value === 'object') visit(value);
    }
  }
  parsed.nodes.forEach(visit);
}
const output = [...entries].map(([text, locations]) => ({ text, locations: [...locations] })).sort((a,b) => a.text.localeCompare(b.text));
fs.writeFileSync(path.join(import.meta.dirname, 'source-messages.json'), JSON.stringify(output, null, 2) + '\n');
console.log(JSON.stringify({ unique: output.length, backend: output.filter(x=>x.locations.some(p=>!p.startsWith('HrkUi/'))).length,
  frontend: output.filter(x=>x.locations.some(p=>p.startsWith('HrkUi/'))).length }));

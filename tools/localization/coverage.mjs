// Read-only audit of literal backend failures and client notifications.
import fs from 'node:fs';
import path from 'node:path';
import { createRequire } from 'node:module';
const root=path.resolve(import.meta.dirname,'../..');
const require=createRequire(path.join(root,'HrkUi/package.json'));
const ts=require('typescript');
const catalog=JSON.parse(fs.readFileSync(path.join(import.meta.dirname,'catalog.json'),'utf8'));
const known=new Set(Object.entries(catalog).filter(([key])=>key!=='keys').flatMap(([,rows])=>rows.map(row=>row[0])));
function files(dir) { return fs.readdirSync(dir,{withFileTypes:true}).flatMap(x=>['node_modules','bin','obj','.git'].includes(x.name)?[]:x.isDirectory()?files(path.join(dir,x.name)):[path.join(dir,x.name)]); }
const missing=new Map();
function add(text,file) { if(!known.has(text)) { if(!missing.has(text)) missing.set(text,new Set()); missing.get(text).add(path.relative(root,file).replaceAll('\\','/')); } }
for(const file of files(path.join(root,'Services')).filter(x=>x.endsWith('.cs'))) {
  const source=fs.readFileSync(file,'utf8');
  for(const pattern of [/new\s+BusinessException\(\s*"[^"\r\n]+"\s*,\s*"((?:[^"\\\r\n]|\\.)*)"/g,/\.WithMessage\(\s*"((?:[^"\\\r\n]|\\.)*)"/g])
    for(const match of source.matchAll(pattern)) add(match[1].replaceAll('\\"','"').replaceAll('\\n','\n'),file);
}
for(const file of files(path.join(root,'HrkUi/src')).filter(x=>x.endsWith('.ts')&&!x.endsWith('.spec.ts'))) {
  const ast=ts.createSourceFile(file,fs.readFileSync(file,'utf8'),ts.ScriptTarget.Latest,true);
  function visit(node) {
    if(ts.isTaggedTemplateExpression(node) && node.tag.getText(ast).endsWith('.text')) {
      const value=node.template;
      const text=ts.isTemplateExpression(value)?value.head.text+value.templateSpans.map((span,index)=>`{{p${index}}}`+span.literal.text).join(''):value.text;
      add(text,file);
    }
    if(ts.isStringLiteralLike(node) && /[À-ỹ]/.test(node.text) && node.text.length<600 && !node.text.includes('<')) {
      const parent=node.parent?.getText(ast)??'';
      if(/(?:toast|error|Error|notice|Notice|warning|Warning|success|Success|message|Message|confirm|Confirm)/.test(parent)) add(node.text,file);
    }
    ts.forEachChild(node,visit);
  }
  visit(ast);
}
console.log(JSON.stringify([...missing].map(([text,locations])=>({text,locations:[...locations]})),null,2));

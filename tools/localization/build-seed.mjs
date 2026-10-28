import fs from 'node:fs';
import path from 'node:path';
const root = path.resolve(import.meta.dirname, '../..');
const catalog = JSON.parse(fs.readFileSync(path.join(import.meta.dirname, 'catalog.json'), 'utf8'));
function textKey(text) {
  let hash=14695981039346656037n;
  for (const value of new TextEncoder().encode(text)) hash=BigInt.asUintN(64,(hash^BigInt(value))*1099511628211n);
  return 'text_'+hash.toString(16).padStart(16,'0');
}
function placeholders(text) { return [...text.matchAll(/\{\{\s*(\w+)\s*\}\}/g)].map(x=>x[1]).sort().join(','); }
const seen=new Map();
const bundles=[];
let count=0,keyCount=0;
for (const [group,rows] of Object.entries(catalog).filter(([key])=>key!=='keys')) {
  if (!/^[a-z][a-z0-9-]{0,39}$/.test(group)) throw new Error('Invalid group: '+group);
  const vi={},en={};
  for (const [source,english,vietnamese] of rows) {
    const key=textKey(source);
    if (seen.has(key)&&seen.get(key)!==source) throw new Error('Translation key collision');
    if (vi[key]) throw new Error(`Duplicate ${group}: ${source}`);
    if (placeholders(vietnamese??source)!==placeholders(english)) throw new Error('Placeholder mismatch: '+source);
    seen.set(key,source); vi[key]=vietnamese??source; en[key]=english; count++;
  }
  for (const [key,[vietnamese,english]] of Object.entries(catalog.keys[group]??{})) {
    if (Object.hasOwn(vi,key)) throw new Error(`Duplicate ${group} key: ${key}`);
    if (placeholders(vietnamese)!==placeholders(english)) throw new Error('Placeholder mismatch: '+key);
    vi[key]=vietnamese; en[key]=english;
  }
  keyCount+=Object.keys(vi).length;
  for (const [culture,translations] of [['vi-VN',vi],['en-US',en]]) {
    const entries=Object.entries(translations).sort(([a],[b])=>a<b?-1:a>b?1:0);
    for (const [key,value] of entries) {
      if (!key.length||key.length>256||key.trim()!==key) throw new Error('Invalid key: '+key);
      if (typeof value!=='string'||!value.trim().length) throw new Error('Invalid translation: '+key);
    }
    bundles.push({group,culture,entries});
  }
}
function literal(value) { return `N'${value.replaceAll("'","''")}'`; }
for (const seed of [true,false]) {
  let sql='-- Generated from tools/localization/catalog.json. Run Migration_Language.sql first.\n';
  sql+=seed?'-- Existing publications are preserved; use Publish_Language.sql for reviewed updates.\n'
    :'-- Explicitly publish reviewed catalog changes. Identical bundles are left unchanged.\n';
  sql+='-- Each VALUES tuple is one translation; no translation JSON is stored in the database.\nSET NOCOUNT ON;\nSET XACT_ABORT ON;\nGO\n';
  for (const {group,culture,entries} of bundles) {
    sql+=`-- ${group} / ${culture}: ${entries.length} translations\nDECLARE @Texts dbo.HRK_LanguageTextTable;\n`;
    // Keep INSERT statements below SQL Server's 1,000-row VALUES limit.
    for (let offset=0;offset<entries.length;offset+=500) {
      sql+='INSERT INTO @Texts ([Key], [Value]) VALUES\n';
      sql+=entries.slice(offset,offset+500).map(([key,value])=>`    (${literal(key)}, ${literal(value)})`).join(',\n')+';\n';
    }
    sql+=`EXEC dbo.HRK_PublishLanguageBundle @Group=${literal(group)}, @Culture=${literal(culture)}, @Texts=@Texts, @PublishedBy=N'${seed?'initial-seed':'catalog-publish'}', @OnlyIfMissing=${seed?1:0};\nGO\n`;
  }
  const filename=path.join(root,'Services/HRKLANGUAGE/Language.Infrastructure/Data/Scripts',seed?'Seed_Language.sql':'Publish_Language.sql');
  if (process.argv.includes('--check')) {
    if (fs.readFileSync(filename,'utf8').replaceAll('\r\n','\n')!==sql) throw new Error('Generated SQL is stale: '+filename);
  } else fs.writeFileSync(filename,sql);
}
console.log(`Validated ${count} reviewed messages, ${keyCount} keys and ${keyCount*2} translation rows across ${bundles.length} bundles.`);

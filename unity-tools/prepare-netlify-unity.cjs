// Latest Unity at the existing Netlify URL; retain the complete original H5.
const fs=require('node:fs');
const path=require('node:path');
const crypto=require('node:crypto');
const {spawnSync}=require('node:child_process');
const task=path.resolve(__dirname,'..');
const site=process.argv[2]?path.resolve(process.argv[2]):path.join(task,'output/netlify-unity');
const dist=path.join(site,'dist');
if(fs.existsSync(site))throw Error('Netlify package already exists; review before overwriting');
const h5=path.join(task,'h5/dist');
fs.mkdirSync(dist,{recursive:true});
fs.cpSync(h5,dist,{recursive:true});
fs.cpSync(h5,path.join(dist,'h5'),{recursive:true});
const result=spawnSync(process.execPath,[path.join(__dirname,'prepare-public-unity.cjs'),site],{encoding:'utf8'});
if(result.status!==0)throw Error(result.stderr||result.stdout);
// Headers revalidate entry points; only content-addressed payloads are immutable.
fs.appendFileSync(path.join(dist,'_headers'),'\n/*\n  Cache-Control: public, max-age=0, must-revalidate\n/Build/*.bin\n  Content-Type: application/octet-stream\n  Cache-Control: public, max-age=31536000, immutable\n');
fs.writeFileSync(path.join(dist,'_redirects'),'/guanshan-youyan / 302\n/guanshan-youyan/* /:splat 302\n');
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const files=[];
function walk(dir){for(const entry of fs.readdirSync(dir,{withFileTypes:true})){
  const p=path.join(dir,entry.name);
  if(entry.isDirectory())walk(p);else{const data=fs.readFileSync(p);files.push({file:path.relative(dist,p).replaceAll('\\','/'),bytes:data.length,sha256:hash(data)});}
}}
walk(dist);
const report={createdAt:new Date().toISOString(),site:'https://playful-queijadas-03c105.netlify.app/',sourceSha256:JSON.parse(fs.readFileSync(path.join(dist,'release.json'))).sourceSha256,legacyUrl:'/h5/',files,totalBytes:files.reduce((sum,f)=>sum+f.bytes,0)};
fs.writeFileSync(path.join(site,'package-validation.json'),JSON.stringify(report,null,2));
console.log(JSON.stringify({success:true,dist,sourceSha256:report.sourceSha256,files:files.length,totalBytes:report.totalBytes,largestFile:Math.max(...files.map(f=>f.bytes))}));

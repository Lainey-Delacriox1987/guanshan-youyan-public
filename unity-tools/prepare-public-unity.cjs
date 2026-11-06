// Keep the verified Unity binaries unchanged; split compressed payloads for static hosting.
const fs=require('node:fs');
const path=require('node:path');
const zlib=require('node:zlib');
const crypto=require('node:crypto');
const task=path.resolve(__dirname,'..');
const source=path.join(task,'unity/GuanshanYouyan2D/Builds/FullStory-web-release');
const site=process.argv[2]?path.resolve(process.argv[2]):path.join(task,'output/unity-public-site');
const dist=path.join(site,'dist');
const build=path.join(dist,'Build');
const base='FullStory-web-release';
const release=JSON.parse(fs.readFileSync(path.join(task,'unity/GuanshanYouyan2D/ImportReports/full-build-web-release.json'),'utf8'));
const version=release.sourceSha256.slice(0,12);
const previous=fs.existsSync(path.join(dist,'release.json'))?JSON.parse(fs.readFileSync(path.join(dist,'release.json'),'utf8')):null;
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
fs.mkdirSync(build,{recursive:true});
fs.cpSync(path.join(source,'TemplateData'),path.join(dist,'TemplateData'),{recursive:true});
for(const name of ['startup-cover.js','web-performance.js','loading-manifest.json'])if(fs.existsSync(path.join(source,name)))fs.copyFileSync(path.join(source,name),path.join(dist,name));
for(const name of ['Noto-OFL.txt','LongCang-OFL.txt','背景音乐来源.md'])fs.copyFileSync(path.join(source,name),path.join(dist,name));
fs.copyFileSync(path.join(source,'Build',base+'.loader.js'),path.join(build,base+'.loader.js'));
fs.writeFileSync(path.join(build,base+'.framework.js'),zlib.gunzipSync(fs.readFileSync(path.join(source,'Build',base+'.framework.js.gz'))));
const manifest={};
for(const type of ['data','wasm']){
  const compressed=fs.readFileSync(path.join(source,'Build',base+'.'+type+'.gz'));
  const digest=hash(compressed);const parts=[];
  for(let offset=0,index=0;offset<compressed.length;offset+=16*1024*1024,index++){
    const name=base+'.'+type+'.'+digest.slice(0,12)+'.'+String(index+1).padStart(3,'0')+'.bin';
    fs.writeFileSync(path.join(build,name),compressed.subarray(offset,offset+16*1024*1024));parts.push('Build/'+name);
  }
  const joined=Buffer.concat(parts.map(p=>fs.readFileSync(path.join(dist,p))));
  if(hash(joined)!==digest)throw Error('Compressed chunk mismatch: '+type);
  manifest['Build/'+base+'.'+type]={parts,compressedSha256:digest,bytes:compressed.readUInt32LE(compressed.length-4),type:type==='wasm'?'application/wasm':'application/octet-stream'};
}
const transport=`// Streaming transport for exact Unity export bytes, without oversized static files.
(()=>{
  const manifest=${JSON.stringify(manifest)};
  const nativeFetch=window.fetch.bind(window);
  const entries=new Map(Object.entries(manifest).map(([url,value])=>[new URL(url,document.baseURI).href,value]));
  window.fetch=function(input,init){
    const url=new URL(typeof input==='string'||input instanceof URL?input:input.url,document.baseURI).href;
    const lookup=new URL(url);lookup.search='';
    const entry=entries.get(lookup.href);if(!entry)return nativeFetch(input,init);
    if(typeof DecompressionStream==='undefined')return Promise.reject(Error('请使用新版 Chrome、Edge 或 Safari 打开游戏。'));
    const headers={'Content-Type':entry.type,'Content-Length':String(entry.bytes),'Cache-Control':'no-store'};
    const method=init?.method||(input instanceof Request?input.method:'GET');
    const signal=init?.signal||(input instanceof Request?input.signal:undefined);
    const stream=method==='HEAD'?null:new ReadableStream({async start(controller){
      try{for(const part of entry.parts){
        const response=await nativeFetch(new URL(part,document.baseURI).href,{cache:'force-cache',signal});
        if(!response.ok)throw Error('游戏资源加载失败：'+response.status);
        const reader=response.body.getReader();while(true){const next=await reader.read();if(next.done)break;controller.enqueue(next.value);}
      }controller.close();}catch(error){controller.error(error);}
    }}).pipeThrough(new DecompressionStream('gzip'));
    const response=new Response(stream,{status:200,headers});Object.defineProperty(response,'url',{value:url});return Promise.resolve(response);
  };
  window.__unityPublicTransport={edition:'offline-fixed-topics',manifest};
})();
`;
fs.writeFileSync(path.join(dist,'unity-transport.js'),transport);
let html=fs.readFileSync(path.join(source,'index.html'),'utf8');
html=html.replace('</head>','<script src="unity-transport.js?v='+version+'"></script></head>');
function rewrite(suffix,target,token){
  const escaped=(base+'.'+suffix).replace(/[.*+?^${}()|[\]\\]/g,'\\$&');
  html=html.replace(new RegExp(escaped+'(?:\\.(?:gz|br))?(?:\\?v=[a-f0-9]+)?','g'),target+'?v='+token);
}
rewrite('data',base+'.data',manifest['Build/'+base+'.data'].compressedSha256.slice(0,16));
rewrite('wasm',base+'.wasm',manifest['Build/'+base+'.wasm'].compressedSha256.slice(0,16));
rewrite('framework.js',base+'.framework.js',version);
rewrite('loader.js',base+'.loader.js',version);
if(!html.includes('cacheControl:'))html=html.replace('arguments: [],','arguments: [],\n        cacheControl: function(){ return "immutable"; },');
fs.writeFileSync(path.join(dist,'index.html'),html);
require('./install-landscape-guard.cjs')(dist);
const coverPath=html.match(/TemplateData\/cover\.[a-f0-9]+\.webp/)?.[0]||'TemplateData/cover.jpg';
fs.writeFileSync(path.join(dist,'release.json'),JSON.stringify({edition:release.edition,sourceSha256:release.sourceSha256,shuttleSha256:release.shuttleSha256,startup:{coverPath,coverSha256:hash(fs.readFileSync(path.join(dist,coverPath))),holdSeconds:3,automaticEntry:true},orientation:'landscape-only',webShellSha256:hash(fs.readFileSync(path.join(dist,'landscape-only.js'))),transport:'streaming gzip chunks',manifest},null,2));
// Retain superseded generated chunks outside the published tree instead of growing each upload.
const currentParts=new Set(Object.values(manifest).flatMap(item=>item.parts));
if(previous?.manifest&&/^[a-f0-9]{64}$/.test(previous.sourceSha256))for(const oldPart of Object.values(previous.manifest).flatMap(item=>item.parts)){
  if(currentParts.has(oldPart))continue;
  const oldFile=path.resolve(dist,oldPart);const archiveRoot=path.resolve(task,'output/unity-public-retired');
  const target=path.resolve(archiveRoot,previous.sourceSha256,path.basename(oldFile));
  if(path.dirname(oldFile)!==path.resolve(build)||!path.basename(oldFile).startsWith(base+'.')||!oldFile.endsWith('.bin')||!target.startsWith(archiveRoot+path.sep))throw Error('Unsafe retired chunk path');
  if(fs.existsSync(oldFile)&&!fs.existsSync(target)){fs.mkdirSync(path.dirname(target),{recursive:true});fs.renameSync(oldFile,target);}
}
fs.writeFileSync(path.join(site,'.gitignore'),'*.log\n*.tar.gz\n');
fs.writeFileSync(path.join(site,'README.md'),'# 关山有眼·雾港来信 Unity 公网版\n\nSource export: '+release.sourceSha256+'\n\nThe Unity data and wasm binaries are byte-identical to the verified export after gzip streaming. Fixed offline mentor topics; no backend, keys, or model service.\n');
console.log(JSON.stringify({success:true,site,manifest,totalCompressedBytes:Object.values(manifest).reduce((sum,item)=>sum+item.parts.reduce((a,p)=>a+fs.statSync(path.join(dist,p)).size,0),0)},null,2));

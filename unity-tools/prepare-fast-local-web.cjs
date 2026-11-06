// Lossless Unity payload compression plus a small, high-priority cover preview.
const fs=require('node:fs'),path=require('node:path'),zlib=require('node:zlib'),crypto=require('node:crypto');
const {promisify}=require('node:util');
const sharp=require('sharp');
const compress=promisify(zlib.brotliCompress);
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
async function prepare(folder){
  folder=path.resolve(folder);const build=path.join(folder,'Build'),index=path.join(folder,'index.html');
  if(!fs.existsSync(index)||!fs.existsSync(build))throw Error('Expected exported Unity Web folder');
  const previous=fs.existsSync(path.join(folder,'loading-manifest.json'))?JSON.parse(fs.readFileSync(path.join(folder,'loading-manifest.json'))):null;
  const assets=[];
  for(const type of ['data','wasm','framework.js']){
    const names=fs.readdirSync(build).filter(n=>n.endsWith('.'+type+'.gz'));
    if(names.length!==1)throw Error('Expected one gzip '+type+' asset');
    const name=names[0],gzip=fs.readFileSync(path.join(build,name)),gzipSha256=hash(gzip),raw=zlib.gunzipSync(gzip);
    const output=name.slice(0,-3)+'.br',target=path.join(build,output);
    const cached=previous?.assets?.find(a=>a.gzipSha256===gzipSha256&&a.file==='Build/'+output);
    let brotli;
    if(cached&&fs.existsSync(target))brotli=fs.readFileSync(target);
    else {brotli=await compress(raw,{params:{[zlib.constants.BROTLI_PARAM_QUALITY]:9,[zlib.constants.BROTLI_PARAM_SIZE_HINT]:raw.length}});fs.writeFileSync(target,brotli);}
    if(!zlib.brotliDecompressSync(brotli).equals(raw))throw Error('Lossless reconstruction failed: '+type);
    assets.push({type,file:'Build/'+output,original:'Build/'+name,gzipBytes:gzip.length,bytes:brotli.length,sha256:hash(brotli),gzipSha256,uncompressedSha256:hash(raw),lossless:true});
  }
  const coverSource=[path.join(folder,'TemplateData/cover.png'),path.resolve(__dirname,'../unity/GuanshanYouyan2D/Assets/GuanshanYouyan/Art/FullStoryV2/Cover.png'),path.join(folder,'TemplateData/cover.jpg')].find(p=>fs.existsSync(p));
  if(!coverSource)throw Error('Missing cover source');
  const coverBytes=fs.readFileSync(coverSource);
  const preview=await sharp(coverBytes).webp({quality:90,effort:6,smartSubsample:true}).toBuffer();
  const coverName='cover.'+hash(preview).slice(0,12)+'.webp';fs.writeFileSync(path.join(folder,'TemplateData',coverName),preview);
  let html=fs.readFileSync(index,'utf8');
  for(const asset of assets){
    const escaped=path.basename(asset.original).replace(/[.*+?^${}()|[\]\\]/g,'\\$&');
    const br=path.basename(asset.file).replace(/[.*+?^${}()|[\]\\]/g,'\\$&');
    html=html.replace(new RegExp('(?:'+escaped+'|'+br+')(?:\\?v=[a-f0-9]+)?','g'),path.basename(asset.file)+'?v='+asset.sha256.slice(0,16));
  }
  const loader=fs.readdirSync(build).find(n=>n.endsWith('.loader.js'));
  const loaderHash=hash(fs.readFileSync(path.join(build,loader))).slice(0,16);
  html=html.replace(new RegExp(loader.replaceAll('.','\\.')+'(?:\\?v=[a-f0-9]+)?','g'),loader+'?v='+loaderHash);
  html=html.replace(/TemplateData\/cover(?:\.[a-f0-9]+)?\.(?:png|jpe?g|webp)/g,'TemplateData/'+coverName);
  if(!html.includes('id="fog-cover-preload"'))html=html.replace('</head>','<link id="fog-cover-preload" rel="preload" as="image" fetchpriority="high" href="TemplateData/'+coverName+'"></head>');
  // Unity's own IndexedDB cache is authoritative for versioned game data.
  html=html.replace(/cacheControl:\s*function\([^)]*\)\s*\{[^}]*\},?/g,'');
  html=html.replace('arguments: [],','arguments: [],\n        cacheControl: function(url){return /\\.data(?:\\.(?:br|gz))?(?:\\?|$)/.test(url)?"immutable":"must-revalidate";},');
  if(!html.includes('startup-cover.js')&&!html.includes('/* fog-cover-ready */'))html=html.replace(/document\.querySelector\(["']#unity-loading-bar["']\)\.style\.display\s*=\s*["']none["'];/g,
    '/* fog-cover-ready */ window.__fogLoadTimings={runtimeReadyMs:performance.now()};requestAnimationFrame(function waitForCover(){if(document.documentElement.dataset.fogIntro==="1"){requestAnimationFrame(()=>requestAnimationFrame(()=>{document.querySelector("#unity-loading-bar").style.display="none";window.__fogLoadTimings.coverReadyMs=performance.now();}));}else requestAnimationFrame(waitForCover);});');
  if(!html.includes('id="fog-cover-controls"'))html=html.replace('</head>','<style id="fog-cover-controls">html:not([data-fog-intro="0"]) #fog-fullscreen{display:none!important}</style></head>');
  if(html.includes('startup-cover.js'))fs.copyFileSync(path.join(__dirname,'startup-cover.js'),path.join(folder,'startup-cover.js'));
  fs.writeFileSync(index,html);
  const instructions=path.join(folder,'开始游玩.txt');
  if(fs.existsSync(instructions))fs.writeFileSync(instructions,fs.readFileSync(instructions,'utf8').replace('发布服务器需对.gz资源发送Content-Encoding:gzip并对wasm发送application/wasm。','本包使用Brotli预压缩资源，请使用随包Serve-Web.cjs；其它服务器需对.br资源发送Content-Encoding:br，并对wasm发送application/wasm。'));
  const baseline=assets.reduce((s,a)=>s+a.gzipBytes,coverBytes.length),optimized=assets.reduce((s,a)=>s+a.bytes,preview.length);
  const report={preparedAt:new Date().toISOString(),codec:'brotli quality 9',assets,cover:{originalBytes:coverBytes.length,bytes:preview.length,file:'TemplateData/'+coverName,originalPreserved:true,originalDimensionsPreserved:true},baselineBytes:baseline,optimizedBytes:optimized,reductionPercent:Math.round((1-optimized/baseline)*1000)/10,dataCache:'immutable content-versioned Unity IndexedDB',requiresServer:'serve-full-build.cjs with Brotli support'};
  fs.writeFileSync(path.join(folder,'loading-manifest.json'),JSON.stringify(report,null,2));
  console.log(JSON.stringify({folder,baselineBytes:baseline,optimizedBytes:optimized,reductionPercent:report.reductionPercent,coverBytes:preview.length,losslessPayloads:assets.length}));
  return report;
}
module.exports=prepare;
if(require.main===module)prepare(process.argv[2]||'unity/GuanshanYouyan2D/Builds/FullStory-web-release').catch(error=>{console.error(error);process.exitCode=1;});

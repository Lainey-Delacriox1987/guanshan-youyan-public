/* Keep native pixels. AVIF quality was chosen using native-resolution visual/SSIM comparisons. */
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const sharp=require('sharp');
const base=path.resolve(__dirname,'../public'),dir=path.join(base,'assets/mist'),assets={},report=[];
function save(name,kind,ext,bytes,extra={}){const hash=crypto.createHash('sha256').update(bytes).digest('hex').slice(0,12),file=`${name}-${kind}-${hash}.${ext}`;fs.writeFileSync(path.join(dir,file),bytes);return {src:'assets/mist/'+file,bytes:bytes.length,type:'image/'+ext,...extra};}
(async()=>{for(const name of ['harbor','warehouse','town','interview']){
 const input=path.join(dir,name+'.png'),m=await sharp(input).metadata();
 const avif=await sharp(input).avif({quality:75,effort:5,chromaSubsampling:'4:4:4'}).toBuffer();
 const webp=await sharp(input).webp({quality:95,effort:6}).toBuffer();
 const thumb=await sharp(input).resize({width:640}).webp({quality:86,effort:6}).toBuffer();
 assets[name]={width:m.width,height:m.height,avif:save(name,'native','avif',avif),webp:save(name,'native','webp',webp),thumb:save(name,'thumb-hq','webp',thumb,{width:640})};
 report.push({name,width:m.width,height:m.height,originalBytes:fs.statSync(input).size,avifBytes:avif.length,webpFallbackBytes:webp.length,thumbBytes:thumb.length});
}
 const probe=await sharp({create:{width:2,height:2,channels:3,background:'#b09b74'}}).avif({quality:20}).toBuffer();
 const js=`/* Native-resolution asset manifest. No full-scene Base64 payloads. */\n(function(root){const assets=${JSON.stringify(assets)};Object.defineProperty(assets,'avifProbe',{value:${JSON.stringify('data:image/avif;base64,'+probe.toString('base64'))}});if(typeof module!=='undefined'&&module.exports)module.exports=assets;else root.MistArt=assets;})(typeof window!=='undefined'?window:globalThis);\n`;
 fs.writeFileSync(path.join(base,'mist-art.js'),js);
 let html=fs.readFileSync(path.join(base,'index.html'),'utf8');html=html.replace(/<!-- responsive scene preload -->[\s\S]*?<!-- \/responsive scene preload -->/,'<!-- Native artwork is selected after codec detection; avoid duplicate-format preloads. -->');html=html.replace('首次载入会一并准备四个地点的标准画面。','正在检查画面格式，随后加载当前场景。');fs.writeFileSync(path.join(base,'index.html'),html);
 fs.writeFileSync(path.resolve(__dirname,'../../output/mist-native-art-report.json'),JSON.stringify({quality:{avif:75,chroma:'4:4:4',webp:95},assets:report},null,2));console.log(JSON.stringify(report));
})().catch(e=>{console.error(e);process.exitCode=1;});

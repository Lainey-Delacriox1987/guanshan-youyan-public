/* Native-resolution scene bundles: detect codec, stream, decode, then reveal. */
(function(root){
'use strict';
const assets=root.MistArt,loaded=new Map(),pending=new Map();
let format='webp',codecTask,foreground=null,epoch=0;
function prepare(){return codecTask??=(new Promise(resolve=>{const probe=new Image();const timer=setTimeout(()=>resolve('webp'),2500);probe.onload=()=>{clearTimeout(timer);resolve(probe.naturalWidth===2?'avif':'webp');};probe.onerror=()=>{clearTimeout(timer);resolve('webp');};probe.src=assets.avifProbe;}).then(value=>(format=value)));}
function entry(name){return assets[name]||assets.harbor;}
function source(name,kind='thumb'){const e=entry(name);return (e[kind]||e[format]).src;}
function status(name){const e=entry(name),src=e[format].src,v=loaded.get(src);return {format,src,width:e.width,height:e.height,bytes:e[format].bytes,ready:!!v};}
function decode(src,meta,priority){return new Promise((resolve,reject)=>{const img=new Image();img.decoding='async';img.fetchPriority=priority;img.onload=async()=>{try{await img.decode();if(img.naturalWidth!==meta.width||img.naturalHeight!==meta.height)throw new Error('场景尺寸校验失败');resolve(img);}catch(e){reject(e);}};img.onerror=()=>reject(new Error('场景解码失败'));img.src=src;});}
async function load(name,priority='high',observe){
 await prepare();const meta=entry(name),asset=meta[format],key=asset.src;
 if(loaded.has(key))return loaded.get(key);
 if(pending.has(key)){const task=pending.get(key);if(observe){task.observers.add(observe);observe(task.progress);}return task.promise;}
 const controller=new AbortController(),task={controller,priority,observers:new Set(observe?[observe]:[]),progress:{received:0,total:asset.bytes,phase:'download'}};
 const emit=(received,phase)=>{task.progress={received,total:asset.bytes,phase};for(const fn of task.observers)fn(task.progress);};
 task.promise=(async()=>{let objectURL,timer;
  try{
   let img;
   if(root.location.protocol==='file:'){emit(0,'file');img=await decode(key,meta,priority);objectURL=key;}
   else{
    const reset=()=>{clearTimeout(timer);timer=setTimeout(()=>controller.abort(),20000);};reset();
    const response=await fetch(key,{cache:'force-cache',priority,signal:controller.signal});
    if(!response.ok)throw new Error('场景请求失败：'+response.status);
    const chunks=[];let received=0;
    if(response.body){const reader=response.body.getReader();while(true){const {done,value}=await reader.read();if(done)break;chunks.push(value);received+=value.length;reset();emit(received,'download');}}
    else{const bytes=new Uint8Array(await response.arrayBuffer());chunks.push(bytes);received=bytes.length;}
    clearTimeout(timer);if(received!==asset.bytes)throw new Error('场景文件不完整，请重试');
    emit(received,'decode');objectURL=URL.createObjectURL(new Blob(chunks,{type:asset.type}));img=await decode(objectURL,meta,priority);
   }
   const result={url:objectURL,image:img,src:key,width:img.naturalWidth,height:img.naturalHeight};loaded.set(key,result);emit(asset.bytes,'ready');return result;
  }catch(e){if(objectURL?.startsWith('blob:'))URL.revokeObjectURL(objectURL);throw e;}
  finally{clearTimeout(timer);pending.delete(key);}
 })();pending.set(key,task);return task.promise;
}
function backdrop(name,extra=''){return `<div class="backdrop ${extra}" data-art="${name}" data-quality="original" aria-hidden="true"></div><div class="vignette" aria-hidden="true"></div>`;}
function overlay(){let el=document.getElementById('media-loading');if(!el){el=document.createElement('section');el.id='media-loading';el.setAttribute('role','status');el.setAttribute('aria-live','polite');el.innerHTML='<div class="loading-card"><span class="loading-kicker">关山有眼 · 雾港来信</span><h2>正在准备调查现场</h2><p class="loading-stage"></p><progress max="100" value="0" aria-label="场景下载进度"></progress><p class="loading-detail"></p><small>原尺寸画面 · 准备完成后进入</small><button type="button" class="paper-btn" hidden>重新加载场景</button></div>';document.body.append(el);}return el;}
function hydrate(scope=document,next,extras=[]){const el=scope.querySelector('.backdrop[data-art]');if(!el)return Promise.resolve();const id=++epoch,name=el.dataset.art,host=document.getElementById('app');foreground=name;
 for(const [key,task] of pending)if(task.priority==='low'&&key!==source(name,format)&&(!next||key!==source(next,format))&&!extras.some(n=>key===source(n,format))&&(!assets.walker||key!==source('walker',format))&&(!assets.cast||key!==source('cast',format)))task.controller.abort();
 const active=()=>id===epoch&&el.isConnected;
 const reveal=result=>{if(!active())return;el.style.setProperty('--art',`url("${result.url}")`);el.dataset.artSrc=result.src;el.dataset.width=result.width;el.dataset.height=result.height;el.classList.add('art-ready');document.getElementById('media-loading')?.remove();host.inert=false;for(const extra of extras){const node=scope.querySelector('.player-sprite');if(node)node.style.backgroundImage=`url("${imageURL(extra)}")`;}if(next&&next!==name)prefetch(next);};
 const ready=loaded.get(source(name,format));if(ready&&extras.every(n=>loaded.has(source(n,format)))){reveal(ready);return Promise.resolve(ready);}
 host.inert=true;const panel=overlay(),bar=panel.querySelector('progress'),detail=panel.querySelector('.loading-detail'),retry=panel.querySelector('button');panel.querySelector('.loading-stage').textContent=({harbor:'港口码头',warehouse:'物流仓库',town:'边境小镇',interview:'最终审讯室'})[name]||root.MistWalkData?.rooms.flat().find(r=>r.art===name)?.name||'调查场景';retry.hidden=true;
 const progress=p=>{if(!active())return;if(p.phase==='file'){bar.removeAttribute('value');detail.textContent='正在读取本地原尺寸画面…';}else{bar.value=Math.min(99,p.received/p.total*100);detail.textContent=p.phase==='decode'?'下载完成，正在解码画面…':`已下载 ${Math.round(p.received/1024)} / ${Math.round(p.total/1024)} KB`;}};
 const run=()=>{retry.hidden=true;detail.textContent='正在连接场景资源…';return prepare().then(()=>{const names=[name,...extras],states=new Map(names.map(n=>[n,{received:loaded.has(source(n,format))?entry(n)[format].bytes:0,total:entry(n)[format].bytes,phase:'download'}]));const update=(n,p)=>{states.set(n,p);const values=[...states.values()],received=values.reduce((sum,p)=>sum+p.received,0),total=values.reduce((sum,p)=>sum+p.total,0);progress({received,total,phase:values.some(p=>p.phase==='file')?'file':received===total?'decode':'download'});};return Promise.all(names.map(n=>load(n,'high',p=>update(n,p))));}).then(results=>reveal(results[0])).catch(()=>{if(active()){detail.textContent='场景尚未准备完成，请检查网络后重试。';retry.hidden=false;}});};retry.onclick=run;return run();
}
async function prefetch(name){if(!name||name===foreground)return;await prepare();if(pending.has(source(name,format)))return;try{return await load(name,'low');}catch{/* A failed background download is retried on entry. */}}
function imageURL(name){return loaded.get(source(name,format))?.url||source(name,format);}
root.MistMedia={source,status,load,backdrop,hydrate,prefetch,prepare,imageURL};
})(window);

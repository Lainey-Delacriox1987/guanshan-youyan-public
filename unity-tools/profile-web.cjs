// Isolated Chrome context: never touches the user's browser profile or saves.
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require('playwright');
const label=process.argv[2]||'baseline',root=path.resolve(__dirname,'..');
(async()=>{
 const browser=await chromium.launch({channel:'chrome',headless:true,args:['--autoplay-policy=no-user-gesture-required']});
 try{
  const context=await browser.newContext({viewport:{width:1600,height:900},deviceScaleFactor:2});
  const page=await context.newPage(),errors=[],messages=[];
  page.on('pageerror',e=>{errors.push(e.message);console.error('pageerror:',e.message);});page.on('console',m=>{if(m.type()==='error'||m.type()==='warning')messages.push(m.text());if(m.type()==='error')console.error('console:',m.text());});
  page.on('dialog',async d=>{errors.push(d.message());console.error('dialog:',d.message());await d.dismiss();});
  await page.addInitScript(()=>{
   const p=window.__perf={frames:[],draws:0,writes:0,longTasks:[],last:0};
   const set=Storage.prototype.setItem;Storage.prototype.setItem=function(k,v){if(k.startsWith('guanshan-mist-save'))p.writes++;return set.call(this,k,v);};
   for(const type of [window.WebGLRenderingContext,window.WebGL2RenderingContext])if(type)for(const method of ['drawElements','drawArrays','drawElementsInstanced','drawArraysInstanced'])if(type.prototype[method]){
    const old=type.prototype[method];type.prototype[method]=function(...args){p.draws++;return old.apply(this,args);};
   }
   try{new PerformanceObserver(list=>p.longTasks.push(...list.getEntries().map(e=>({start:e.startTime,ms:e.duration})))).observe({entryTypes:['longtask']});}catch{}
   const frame=t=>{if(p.draws){if(p.last)p.frames.push(t-p.last);p.last=t;p.draws=0;}requestAnimationFrame(frame);};requestAnimationFrame(frame);
  });
  const started=Date.now();await page.goto('http://127.0.0.1:18743/guanshan-youyan/');
  try{await page.waitForFunction(()=>!!window.fogGameInstance,{},{timeout:45000});}
  catch(error){console.error(JSON.stringify({url:page.url(),errors,messages,state:await page.evaluate(()=>({text:document.body.innerText,canvas:!!document.querySelector('canvas'),loading:document.querySelector('#unity-loading-bar')?.style.display,landscape:document.documentElement.dataset.fogOrientation,instance:typeof window.fogGameInstance,performanceScript:typeof window.FogWebPerformance,requests:performance.getEntriesByType('resource').map(x=>({name:x.name,bytes:x.transferSize,ms:x.duration}))}))},null,2));throw error;}
  const loadMs=Date.now()-started;
  await page.waitForTimeout(3700);
  await page.evaluate(()=>document.querySelector('canvas').focus());
  // Skip the black intro, letter and three-road journey through normal keyboard input.
  for(let i=0;i<8;i++){
   const finished=await page.evaluate(()=>{const k=Object.keys(localStorage).find(k=>k.startsWith('guanshan-mist-save-v1:')&&k.endsWith('.json'));if(!k)return false;const s=JSON.parse(localStorage[k]);return s.pending===''&&!s.openingDrivePending&&s.facts.includes('letterReceived');});
   if(finished)break;await page.keyboard.press('Space');await page.waitForTimeout(1100);
  }
  await page.waitForTimeout(2800);
  await page.waitForFunction(()=>{const k=Object.keys(localStorage).find(k=>k.startsWith('guanshan-mist-save-v1:')&&k.endsWith('.json'));if(!k)return false;const s=JSON.parse(localStorage[k]);return s.pending===''&&!s.openingDrivePending&&s.facts.includes('letterReceived');},{},{timeout:45000});
  await page.waitForTimeout(2000);
  const sample=async(name,action)=>{
   await page.evaluate(()=>{__perf.frames=[];__perf.longTasks=[];__perf.writes=0;__perf.last=0;});
   await action();
   return page.evaluate(name=>{const a=__perf.frames.slice().sort((x,y)=>x-y);return {name,renderedFrames:a.length,medianMs:a[Math.floor(a.length*.5)]||null,p95Ms:a[Math.floor(a.length*.95)]||null,over50ms:a.filter(x=>x>50).length,saveWrites:__perf.writes,longTasks:__perf.longTasks};},name);
  };
  const samples=[];
  samples.push(await sample('idle-8s',()=>page.waitForTimeout(8000)));
  samples.push(await sample('walk-8s',async()=>{await page.keyboard.down('ArrowRight');await page.waitForTimeout(4000);await page.keyboard.up('ArrowRight');await page.keyboard.down('ArrowLeft');await page.waitForTimeout(4000);await page.keyboard.up('ArrowLeft');}));
  samples.push(await sample('notebook-open-close-8',async()=>{for(let i=0;i<8;i++){await page.keyboard.press('Tab');await page.waitForTimeout(250);await page.keyboard.press('Escape');await page.waitForTimeout(250);}}));
  await page.screenshot({path:path.join(root,'output',`performance-${label}.png`)});
  const state=await page.evaluate(()=>({canvas:{width:document.querySelector('canvas').width,height:document.querySelector('canvas').height},heapBytes:window.fogGameInstance.Module.HEAPU8?.buffer.byteLength||null,saveKeys:Object.keys(localStorage).filter(k=>k.startsWith('guanshan-mist-save')),gpu:(()=>{const gl=document.querySelector('canvas').getContext('webgl2');const e=gl?.getExtension('WEBGL_debug_renderer_info');return e?gl.getParameter(e.UNMASKED_RENDERER_WEBGL):null;})(),resources:performance.getEntriesByType('resource').filter(x=>x.name.includes('/Build/')).map(x=>({name:x.name.split('/').pop(),transferBytes:x.transferSize,decodedBytes:x.decodedBodySize,ms:x.duration}))}));
  assert(state.saveKeys.length,'Game must have actually entered its story');
  const checks=[];
  const progress=await page.evaluate(()=>{fogGameInstance.SendMessage('雾港来信 · 完整故事','SaveForPageHide');const k=Object.keys(localStorage).find(k=>k.startsWith('guanshan-mist-save-v1:')&&k.endsWith('.json'));return JSON.parse(localStorage[k]);});
  if(label!=='baseline'){
   assert(state.canvas.width*state.canvas.height<=1920*1080+5000);checks.push('high-DPI rendering stays within the pixel budget');
   await page.setViewportSize({width:844,height:390});await page.waitForTimeout(700);
   const mobile=await page.evaluate(()=>({w:document.querySelector('canvas').width,h:document.querySelector('canvas').height}));
   assert(Math.abs(mobile.w/mobile.h-844/390)<.01);checks.push('mobile landscape resize preserves aspect');
   await page.setViewportSize({width:390,height:844});await page.waitForTimeout(300);
   assert.equal(await page.locator('#fog-rotate').isVisible(),true);checks.push('portrait still pauses behind the rotation prompt');
   await page.setViewportSize({width:1600,height:900});await page.waitForTimeout(400);
   await page.reload();await page.waitForFunction(()=>!!window.fogGameInstance,{},{timeout:120000});await page.waitForTimeout(4300);
   const loaded=await page.evaluate(()=>{const k=Object.keys(localStorage).find(k=>k.startsWith('guanshan-mist-save-v1:')&&k.endsWith('.json'));return JSON.parse(localStorage[k]);});
   assert.deepEqual(loaded.facts,progress.facts);assert.equal(loaded.room,progress.room);assert.deepEqual(loaded.positions,progress.positions);checks.push('saved position and evidence survive resize and browser reload');
  }
  const report={label,browser:await browser.version(),isolatedContext:true,deviceScaleFactor:2,loadMs,state,samples,checks,errors,messages};
  fs.writeFileSync(path.join(root,'unity/GuanshanYouyan2D/ImportReports',`performance-${label}-browser.json`),JSON.stringify(report,null,2));
  console.log(JSON.stringify(report,null,2));
  assert.equal(errors.length,0,errors.join('\n'));
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});

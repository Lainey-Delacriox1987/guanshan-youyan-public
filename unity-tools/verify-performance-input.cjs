const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require('playwright');
const root=path.resolve(__dirname,'..'),reports=path.join(root,'unity/GuanshanYouyan2D/ImportReports');
const fixture=fs.readFileSync(path.join(reports,'performance-browser-fixture.json'),'utf8');
(async()=>{
 const b=await chromium.launch({channel:'chrome',headless:true});
 try{
  const context=await b.newContext({viewport:{width:1600,height:900},deviceScaleFactor:2}),p=await context.newPage(),errors=[],checks=[];
  if(process.env.FOG_PERFORMANCE_CANDIDATE)await p.route('**/web-performance.js*',r=>r.fulfill({status:200,contentType:'text/javascript',body:fs.readFileSync(path.join(root,'unity-tools/web-performance.js'),'utf8')}));
  p.on('pageerror',e=>errors.push(e.message));
  await p.addInitScript(json=>{if(!sessionStorage.getItem('fixture-seeded')){localStorage.setItem('guanshan-mist-save-v1:guanshan-mist-story-v2.json',json);sessionStorage.setItem('fixture-seeded','1');}},fixture);
  await p.goto('http://127.0.0.1:18743/guanshan-youyan/');await p.waitForFunction(()=>!!window.fogGameInstance,{},{timeout:45000});await p.waitForTimeout(4200);
  await p.evaluate(()=>{document.querySelector('canvas').focus();fogGameInstance.SendMessage('雾港来信 · 完整故事','ContinueGame');});await p.waitForTimeout(800);
  const state=()=>p.evaluate(()=>JSON.parse(localStorage.getItem('guanshan-mist-save-v1:guanshan-mist-story-v2.json')));
  const save=()=>p.evaluate(()=>fogGameInstance.SendMessage('雾港来信 · 完整故事','SaveForPageHide'));
  assert.equal((await state()).room,'warehouse');checks.push('existing validated progress loads into the warehouse');
  // Actual native mouse input at the visible exit; projection from the 8-unit camera.
  await p.screenshot({path:path.join(root,'output/performance-input-before.png')});
  await p.mouse.click(1499,473,{delay:80});
  try{await p.waitForFunction(()=>JSON.parse(localStorage.getItem('guanshan-mist-save-v1:guanshan-mist-story-v2.json')).room==='town',{},{timeout:6000});}
  catch(e){await save();console.error(JSON.stringify({state:await state(),canvas:await p.evaluate(()=>({w:document.querySelector('canvas').width,h:document.querySelector('canvas').height,rect:document.querySelector('canvas').getBoundingClientRect().toJSON()}))},null,2));throw e;}
  checks.push('native scene click travels from warehouse to town');await p.waitForTimeout(900);
  await p.screenshot({path:path.join(root,'output/performance-input-town.png')});
  // This is the 44px music button at its unchanged upper-right center.
  await p.mouse.click(1550,50);await p.waitForTimeout(350);
  assert.equal(await p.evaluate(()=>localStorage.getItem('guanshan-mist-music-gain-v1')),'0');checks.push('44px music button hit test mutes');
  await p.screenshot({path:path.join(root,'output/performance-music-button-muted.png')});
  await p.mouse.click(1550,50);await p.waitForTimeout(350);
  assert.equal(await p.evaluate(()=>localStorage.getItem('guanshan-mist-music-gain-v1')),'1');checks.push('second click restores music');
  await p.setViewportSize({width:1200,height:700});await p.waitForTimeout(600);
  // At this aspect the CanvasScaler uses height, so the button's 1600x900
  // reference coordinate is scaled by 700/900 from the upper-right corner.
  await p.mouse.click(1200-50*700/900,50*700/900,{delay:80});await p.waitForTimeout(250);
  assert.equal(await p.evaluate(()=>localStorage.getItem('guanshan-mist-music-gain-v1')),'0');checks.push('resized player retains correct UI layout and click coordinates');
  await p.screenshot({path:path.join(root,'output/performance-resize-ui.png')});
  await p.setViewportSize({width:1600,height:900});await p.waitForTimeout(600);
  await p.mouse.click(1550,50,{delay:80});await p.waitForTimeout(200);
  assert.equal(await p.evaluate(()=>localStorage.getItem('guanshan-mist-music-gain-v1')),'1');
  await save();const before=await state();
  await p.mouse.click(1323,64);await p.waitForTimeout(650);
  await p.screenshot({path:path.join(root,'output/performance-notebook.png')});
  await p.mouse.click(700,180);await p.waitForTimeout(600);await save();
  assert.deepEqual((await state()).positions,before.positions);checks.push('notebook UI opens and shields scene movement');
  await p.keyboard.press('Escape');await p.waitForTimeout(150);
  await p.mouse.click(700,180);await p.waitForTimeout(1000);await save();
  assert.notDeepEqual((await state()).positions,before.positions);checks.push('native ground click walks after closing notebook');
  // Freeze movement before comparing a checkpoint; pagehide correctly saves the
  // next frame if a character is still walking when a reload starts.
  await p.keyboard.press('Tab');await p.waitForTimeout(250);await save();
  const progress=await state();await p.reload();await p.waitForFunction(()=>!!window.fogGameInstance,{},{timeout:45000});await p.waitForTimeout(4300);
  const loaded=await state();assert.equal(loaded.room,progress.room);assert.deepEqual(loaded.facts,progress.facts);assert.deepEqual(loaded.positions,progress.positions);checks.push('room, evidence and walking position survive browser reload');
  assert.deepEqual(errors,[]);
  const result={success:true,checks,errors,scope:'Chrome native mouse and keyboard in an isolated profile; user saves untouched'};
  fs.writeFileSync(path.join(reports,'performance-browser-input-validation.json'),JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));
 }finally{await b.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});

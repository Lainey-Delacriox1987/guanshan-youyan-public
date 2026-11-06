/* Browser acceptance test. Uses a locally installed Chrome and Playwright.
   Run: node tests/browser-mist.cjs. No production dependency or network call. */
const assert=require('node:assert/strict');
const path=require('node:path');
const fs=require('node:fs');
const {pathToFileURL}=require('node:url');
let playwright;try{playwright=require('playwright');}catch{playwright=require('playwright');}
const output=path.resolve(__dirname,'../../output/mist-acceptance');fs.mkdirSync(output,{recursive:true});
const entry=pathToFileURL(path.resolve(__dirname,process.argv.includes('--dist')?'../dist/index.html':'../public/index.html')).href;
const preview=process.argv.find(v=>v.startsWith('--preview='))?.slice(10);
const errors=[],requests=[];let checks=0;
function ok(value,message){assert.ok(value,message);checks++;}
async function shot(p,name){await p.locator('.backdrop.art-ready').waitFor({timeout:10000});await p.screenshot({path:path.join(output,name+'.png'),fullPage:true,animations:'disabled'});}
async function act(p,a,extra=''){await p.locator(`button[data-action="${a}"]${extra}`).filter({visible:true}).first().click();}
async function close(p){await p.locator('#case-dialog button[data-action="close"]').first().click();}
async function evidence(p,id,answer){await act(p,'inspect',`[data-id="${id}"]`);if(answer!==undefined){await p.locator(`button[data-action="compare"][data-answer="${answer}"]`).click();await close(p);}else await p.locator('button[data-action="collect"]').click();}
async function topic(p,id,i){await act(p,'npc',`[data-id="${id}"]`);await p.locator(`button[data-action="topic"][data-topic="${i}"]`).click();await close(p);}
async function solve(p,answer,n){await act(p,'board');await shot(p,'board-'+n);await act(p,'deduce');await p.locator(`button[data-action="solve"][data-answer="${answer}"]`).click();ok(await p.locator('.lesson-card').isVisible(),'Knowledge card '+n);await shot(p,'lesson-'+n);await act(p,'advance');await act(p,'enter');}
async function noOverflow(p,label){ok(await p.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),label+' horizontal overflow');}
(async()=>{const browser=await playwright.chromium.launch({headless:true,channel:'chrome'});try{
 const context=await browser.newContext({viewport:{width:1440,height:900}});const p=await context.newPage();p.on('pageerror',e=>errors.push(e.message));p.on('requestfailed',r=>requests.push(r.url()));
 await p.goto(entry);await shot(p,'01-home');await noOverflow(p,'desktop home');
 await act(p,'help');ok(await p.getByRole('heading',{name:'先观察，再判断。'}).isVisible(),'Guide opens');await close(p);
 await act(p,'settings');await act(p,'toggle-motion');await act(p,'toggle-sound');await close(p);await act(p,'new');await act(p,'enter');
 await act(p,'map');ok(await p.locator('[data-action="travel"][data-id="3"]').isDisabled(),'Future stages locked');await act(p,'travel','[data-id="0"]');
 await act(p,'inspect','[data-id="weight"]');await p.locator('[data-action="compare"][data-answer="0"]').click();ok((await p.locator('#compare-feedback').innerText()).length>0,'Wrong compare gives feedback');ok((await p.evaluate(()=>JSON.parse(localStorage.getItem('guanshan-mist-v1')).evidence)).length===0,'Wrong compare does not collect');await p.locator('[data-action="compare"][data-answer="1"]').click();await shot(p,'02-comparison');await close(p);
 await evidence(p,'seal');await evidence(p,'cargo',2);await topic(p,'guard',1);await shot(p,'03-harbor');
 await act(p,'tab','[data-tab="evidence"]');ok((await p.locator('.evidence-item').count())===4,'Evidence folder lists all first-stage records');await shot(p,'04-evidence');
 await act(p,'tab','[data-tab="people"]');await act(p,'select-person','[data-id="guard"]');ok((await p.locator('.talk-record').innerText()).includes('顺达仓库'),'NPC dialogue persisted');await shot(p,'05-people');
 await act(p,'tab','[data-tab="notes"]');await p.locator('#personal-note').fill('<img src=x onerror=alert(1)> 陈渡 → 顺达仓库');await shot(p,'06-notebook');
 await p.reload();await act(p,'continue');await act(p,'tab','[data-tab="notes"]');ok((await p.locator('#personal-note').inputValue()).includes('陈渡'),'Save restored notes');ok(await p.locator('#personal-note img').count()===0,'Notes escaped');await act(p,'board');await act(p,'deduce');await p.locator('[data-action="solve"][data-answer="0"]').click();ok((await p.locator('#deduce-feedback').innerText()).length>0,'Wrong deduction retry');await p.locator('[data-action="solve"][data-answer="1"]').click();await p.keyboard.press('Escape');await act(p,'deduce');ok(await p.locator('.lesson-card').isVisible(),'Solved lesson can be reopened after closing');await act(p,'advance');await act(p,'enter');
 await shot(p,'07-warehouse');await evidence(p,'parcels',1);await evidence(p,'camera');await evidence(p,'dispatch');await topic(p,'worker',1);await solve(p,0,2);
 await shot(p,'08-town');await evidence(p,'shelf',2);await evidence(p,'account');await evidence(p,'wildlife');await topic(p,'shop',0);await topic(p,'courier',1);await topic(p,'suspect',0);await topic(p,'suspect',2);await solve(p,1,3);
 await shot(p,'09-interview');await act(p,'tab','[data-tab="notes"]');await act(p,'lesson','[data-id="0"]');ok(await p.locator('.lesson-card').isVisible(),'Earlier knowledge cards remain available');await close(p);await act(p,'tab','[data-tab="scene"]');await topic(p,'suspect',2);await act(p,'final');
 for(const [name,v] of Object.entries({person:0,method:0,chain:2,action:1}))await p.locator(`input[name="${name}"][value="${v}"]`).check();await shot(p,'10-final-form');await act(p,'submit-final');ok(await p.getByRole('heading',{name:'真相，差了一步。'}).isVisible(),'Warning ending reached');await shot(p,'11-warning-ending');
 await act(p,'retry');ok(await p.locator('.chain-card').count()===3,'Retry retains evidence board');await act(p,'final');await p.locator('input[name="person"][value="1"]').check();await act(p,'submit-final');ok(await p.getByRole('heading',{name:'雾散，港明。'}).isVisible(),'Good ending reached');ok((await p.locator('.score-strip').innerText()).includes('12/12'),'All twelve evidence records retained');await shot(p,'12-good-ending');await noOverflow(p,'good ending');
 await p.reload();await act(p,'continue');ok(await p.getByRole('heading',{name:'雾散，港明。'}).isVisible(),'Ending restored after reload');
 await act(p,'home');await act(p,'new');await close(p);await act(p,'continue');ok(await p.getByRole('heading',{name:'雾散，港明。'}).isVisible(),'Cancel new story retains save');
 await act(p,'retry');await act(p,'map');await act(p,'travel','[data-id="0"]');ok(await p.locator('.location-plaque h1').innerText()==='港口码头','Earlier scene revisit works');await act(p,'current');ok(await p.locator('.location-plaque h1').innerText()==='最终审讯室','Return to current stage works');
 await context.close();
 const mobile=await browser.newContext({viewport:{width:390,height:844},isMobile:true,hasTouch:true,deviceScaleFactor:1});const m=await mobile.newPage();m.on('pageerror',e=>errors.push(e.message));await m.goto(entry);await shot(m,'13-mobile-home');await noOverflow(m,'mobile home');await act(m,'new');await act(m,'enter');await shot(m,'14-mobile-scene');await noOverflow(m,'mobile scene');
 await act(m,'inspect','[data-id="weight"]');await shot(m,'15-mobile-comparison');await pWait(m);await noOverflow(m,'mobile comparison');await m.locator('[data-action="compare"][data-answer="1"]').click();await close(m);await evidence(m,'seal');await evidence(m,'cargo',2);await topic(m,'guard',1);await act(m,'tab','[data-tab="evidence"]');await shot(m,'16-mobile-evidence');await noOverflow(m,'mobile folder');await act(m,'tab','[data-tab="people"]');await shot(m,'17-mobile-people');await noOverflow(m,'mobile people');await act(m,'tab','[data-tab="notes"]');await shot(m,'18-mobile-notes');await noOverflow(m,'mobile notes');await act(m,'board');await shot(m,'19-mobile-board');await noOverflow(m,'mobile board');await act(m,'map');await shot(m,'20-mobile-map');await noOverflow(m,'mobile map');await act(m,'travel','[data-id="0"]');
 await m.setViewportSize({width:844,height:390});await shot(m,'21-mobile-landscape');await noOverflow(m,'landscape scene');await mobile.close();
 const unavailable=await browser.newContext();await unavailable.addInitScript(()=>{Object.defineProperty(window,'localStorage',{get(){throw Error('Storage blocked');}});});const u=await unavailable.newPage();u.on('pageerror',e=>errors.push(e.message));await u.goto(entry);await act(u,'new');await act(u,'enter');await evidence(u,'seal');ok((await u.locator('#save-status').innerText()).includes('无法存档'),'Storage-blocked mode remains playable');await unavailable.close();
 if(preview){const v=await browser.newPage();const response=await v.goto(preview);ok(response.status()===200&&await v.getByRole('heading',{name:'关山有眼',exact:true}).isVisible(),'Local preview responds with playable title');await v.close();}
 ok(errors.length===0,'No browser runtime errors: '+errors.join(';'));ok(requests.length===0,'All offline assets loaded: '+requests.join(';'));fs.writeFileSync(path.join(output,'report.json'),JSON.stringify({date:'2026-10-26',checks,errors,requests,entry,preview,desktop:'1440x900',mobile:'390x844',landscape:'844x390',screenshots:fs.readdirSync(output).filter(f=>f.endsWith('.png')).length},null,2));console.log(JSON.stringify({passed:true,checks,errors,requests,output}));
 }finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1;});
async function pWait(p){await p.locator('.comparison').waitFor();}


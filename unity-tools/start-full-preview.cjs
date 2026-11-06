// Project-specific, detached local preview. Never stops or replaces another project.
const fs=require('node:fs');
const path=require('node:path');
const {spawn}=require('node:child_process');
const project=path.resolve(__dirname,'..');
const root=path.join(project,'unity/GuanshanYouyan2D/Builds/FullStory-web-release');
const url='http://127.0.0.1:18743/guanshan-youyan/';
const siteId='guanshan-youyan-unity-full';
async function identify(){
  try {const response=await fetch(url+'__site.json',{signal:AbortSignal.timeout(1200)});
    if(!response.ok)return 'occupied';
    const data=await response.json();
    return data.siteId===siteId && path.resolve(data.root)===root ? 'ours':'occupied';
  } catch(error){return error.cause?.code==='ECONNREFUSED' ? 'absent':'occupied';}
}
(async()=>{
  let state=await identify();
  if(state==='occupied')throw Error('Port 18743 belongs to another service or is unresponsive. No process was stopped.');
  if(state==='absent'){
    if(!fs.existsSync(path.join(root,'index.html')))throw Error('Unity Web export is missing.');
    const logDir=path.join(project,'unity/GuanshanYouyan2D/ImportReports');
    fs.mkdirSync(logDir,{recursive:true});
    const out=fs.openSync(path.join(logDir,'local-preview.stdout.log'),'a');
    const err=fs.openSync(path.join(logDir,'local-preview.stderr.log'),'a');
    const child=spawn(process.execPath,[path.join(__dirname,'serve-full-build.cjs'),root,'18743'],
      {cwd:project,detached:true,windowsHide:true,stdio:['ignore',out,err]});
    child.unref();fs.closeSync(out);fs.closeSync(err);
    for(let attempt=0;attempt<30;attempt++){
      await new Promise(resolve=>setTimeout(resolve,200));state=await identify();if(state==='ours')break;
    }
    if(state!=='ours')throw Error('Preview did not start. See ImportReports/local-preview.stderr.log.');
  }
  console.log('Guanshan Youyan Unity preview: '+url);
  if(!process.argv.includes('--no-open')){
    const opener=spawn('explorer.exe',[url],{detached:true,windowsHide:true,stdio:'ignore'});opener.unref();
  }
})().catch(error=>{console.error(error.message);process.exitCode=1;});

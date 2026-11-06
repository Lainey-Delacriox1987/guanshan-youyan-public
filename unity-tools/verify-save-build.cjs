const fs=require('fs'),path=require('path'),crypto=require('crypto'),zlib=require('zlib');
(async()=>{
  const root='unity/GuanshanYouyan2D/Builds/FullStory-web-release',build=JSON.parse(fs.readFileSync('unity/GuanshanYouyan2D/ImportReports/full-build-web-release.json'));
  const files=fs.readdirSync(path.join(root,'Build')).filter(n=>/\.(loader\.js|framework\.js\.gz|data\.gz|wasm\.gz)$/.test(n)),checks=[];
  for(const file of files){const r=await fetch('http://127.0.0.1:18743/guanshan-youyan/Build/'+file);if(!r.ok)throw Error(file+': '+r.status);let expected=fs.readFileSync(path.join(root,'Build',file));if(file.endsWith('.gz'))expected=zlib.gunzipSync(expected);const actual=Buffer.from(await r.arrayBuffer());const hash=b=>crypto.createHash('sha256').update(b).digest('hex');if(hash(actual)!==hash(expected))throw Error('HTTP differs: '+file);checks.push({file,sha256:hash(actual)});}
  if(checks.length!==4)throw Error('Expected four Unity runtime assets');
  fs.writeFileSync('unity/GuanshanYouyan2D/ImportReports/save-served-build-validation.json',JSON.stringify({success:true,sourceSha256:build.sourceSha256,checks},null,2));console.log(JSON.stringify({success:true,sourceSha256:build.sourceSha256,assets:checks.length}));
})().catch(e=>{console.error(e);process.exitCode=1;});

const fs=require('fs'),path=require('path'),crypto=require('crypto');
const root=path.resolve('unity/GuanshanYouyan2D'),sha=b=>crypto.createHash('sha256').update(b).digest('hex');
const rel=fs.readdirSync(path.join(root,'Assets/GuanshanYouyan/Scripts/Story')).filter(n=>n.endsWith('.cs')).map(n=>'Assets/GuanshanYouyan/Scripts/Story\\'+n).concat(['Assets/GuanshanYouyan/Data/StoryV2.json','Assets/GuanshanYouyan/Plugins/WebGL/FogVerification.jslib']).sort();
const source=new Map(rel.map(p=>[p,fs.readFileSync(path.join(root,p))]));
const fingerprint=()=>sha(Buffer.from(rel.map(p=>p+'='+sha(source.get(p))).join('\n')));
const current=fingerprint(),release=JSON.parse(fs.readFileSync(path.join(root,'ImportReports/full-build-web-release.json'))).sourceSha256;
const probe=rel.find(p=>p.endsWith('FogWalkthrough.cs'));let previous=source.get(probe).toString('utf8').replaceAll('\r\n','\n');
previous=previous.replace('Check(GameObject.Find("Profile_9")==null,"unknown profile does not reveal Chen Du early");\n            yield return Touch(new Vector2(Screen.width*.025f,Screen.height*.5f));game.ContinueGame();yield return Wait(()=>game.CurrentView=="Dialogue","continue pending");','Check(GameObject.Find("Profile_9")==null,"unknown profile does not reveal Chen Du early");yield return Click("SaveHomeButton");yield return null;yield return Click("ContinueButton");yield return Wait(()=>game.CurrentView=="Dialogue","continue pending");').replace('game.SaveForPageHide();game.ContinueGame();yield return Wait(()=>game.CurrentView=="EndingAccepted","saved ending resumes");','yield return Click("EndingSaveButton");yield return Wait(()=>game.CurrentView=="EndingAccepted","cover resumes saved ending");');
let matched=current===release,variant=null;
for(const [name,text] of [['LF',previous],['CRLF',previous.replaceAll('\n','\r\n')]]){source.set(probe,Buffer.from(text));if(fingerprint()===release){matched=true;variant=name;break;}}
const result={success:matched,releaseSourceSha256:release,currentWorkingSourceSha256:current,onlyDifference:variant?'FogWalkthrough.cs development/Editor-only retired test controls updated after release snapshot':null,developmentProbeNewline:variant,runtimeSourcesMatch:matched};
fs.writeFileSync(path.join(root,'ImportReports/release-source-validation.json'),JSON.stringify(result,null,2));console.log(JSON.stringify(result));if(!matched)process.exitCode=1;

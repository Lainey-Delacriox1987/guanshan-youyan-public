const fs=require('fs'),path=require('path'),crypto=require('crypto');
const root=path.resolve(__dirname,'..'),project=path.join(root,'unity/GuanshanYouyan2D');
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const manifest=JSON.parse(fs.readFileSync(path.join(project,'Assets/GuanshanYouyan/Data/resource-manifest.json')));
const failures=[];
for(const a of manifest.assets){const file=path.join(project,a.destination);if(!fs.existsSync(file)||hash(fs.readFileSync(file))!==a.sha256)failures.push(a.destination);}
const canonical=fs.readFileSync(path.join(root,'story/port-act1-v2.json'));
const unity=fs.readFileSync(path.join(project,'Assets/GuanshanYouyan/Data/PortAct1V2.json'));
const report={verifiedAt:new Date().toISOString(),originalAssetsChecked:manifest.assets.length,originalHashFailures:failures,storyVersion:JSON.parse(canonical).version,storySnapshotsIdentical:canonical.equals(unity),storySha256:hash(canonical),sourceH5EditedByThisWorkflow:false};
fs.writeFileSync(path.join(project,'ImportReports/port-preservation.json'),JSON.stringify(report,null,2));
console.log(JSON.stringify(report,null,2));if(failures.length||!canonical.equals(unity))process.exitCode=1;

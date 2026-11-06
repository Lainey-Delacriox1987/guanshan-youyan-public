const fs=require('fs'),path=require('path'),crypto=require('crypto');
const root=path.resolve(__dirname,'..'),project=path.join(root,'unity/GuanshanYouyan2D');
const hash=f=>crypto.createHash('sha256').update(fs.readFileSync(f)).digest('hex');
const original=JSON.parse(fs.readFileSync(path.join(project,'Assets/GuanshanYouyan/Data/resource-manifest.json')));
const failures=[];let checked=0,h5Checked=0;
for(const e of [...original.assets,...original.archive]){checked++;const target=path.join(project,e.destination);if(!fs.existsSync(target)||hash(target)!==e.sha256)failures.push({kind:'original',path:e.destination});}
for(const e of original.archive.filter(e=>e.source.startsWith('h5/'))){const current=path.join(root,e.source);if(fs.existsSync(current)){h5Checked++;if(hash(current)!==e.sha256)failures.push({kind:'h5-current-vs-original-snapshot',path:e.source});}}
const canonical=path.join(root,'story/story-v2.json'),mirror=path.join(project,'Assets/GuanshanYouyan/Data/StoryV2.json');
const story=JSON.parse(fs.readFileSync(canonical));const same=fs.readFileSync(canonical).equals(fs.readFileSync(mirror));
if(!same)failures.push({kind:'story-snapshot-mismatch'});
const actionIds=new Set(story.actions.map(a=>a.id));const known=new Set([...story.materials.map(m=>m.id),...story.actions.flatMap(a=>a.gives),...story.relations.map(r=>r.id)]);
for(const item of [...story.actions,...story.relations])for(const needed of item.required||[])if(!known.has(needed))failures.push({kind:'unknown-prerequisite',id:item.id,needed});
for(const room of story.rooms)for(const spot of room.spots){if(spot.target&&!story.rooms.some(r=>r.id===spot.target))failures.push({kind:'unknown-destination',room:room.id,target:spot.target});if(spot.kind==='action'&&!actionIds.has(spot.id))failures.push({kind:'unknown-action',id:spot.id});}
const art=[];for(const name of ['Warehouse','Dispatch','Town','Records','Interview','Archive','People','Objects']){const f=path.join(project,'Assets/GuanshanYouyan/Art/FullStoryV2',name+'.png');if(!fs.existsSync(f))failures.push({kind:'missing-art',name});else art.push({name,bytes:fs.statSync(f).size,sha256:hash(f)});}
const report={success:failures.length===0,checkedOriginalFiles:checked,checkedCurrentH5Files:h5Checked,storySnapshotsIdentical:same,storySha256:hash(canonical),rooms:story.rooms.length,chapters:story.chapters.length,actions:story.actions.length,relations:story.relations.length,reportDialogues:story.reports.length,knowledgeCards:story.knowledge.length,art,failures,verifiedAt:new Date().toISOString()};
fs.writeFileSync(path.join(project,'ImportReports/full-artifact-validation.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report,null,2));if(!report.success)process.exitCode=1;

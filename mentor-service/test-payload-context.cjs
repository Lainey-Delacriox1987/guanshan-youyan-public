// Consumes request snapshots produced by the actual Unity serializer; no model/network.
const assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
const {context}=require('./server.cjs');
const reportDir=path.resolve(__dirname,'../unity/GuanshanYouyan2D/ImportReports');
const unity=JSON.parse(fs.readFileSync(path.join(reportDir,'mentor-payload-regression.json'),'utf8'));
assert.equal(unity.success,true);
const stages=unity.snapshots.map(({investigationAct,room,payload})=>{
  const c=context(payload);
  assert.equal(c.chapter,investigationAct);
  assert.deepEqual([...c.owned].sort(),[...payload.materials].sort());
  assert.deepEqual([...c.events].sort(),[...payload.events].sort());
  assert(!c.owned.has('wildlife'));
  if(investigationAct===3)assert(c.owned.has('account'));
  return {investigationAct,room,knownMaterials:payload.materials.length,knownEvents:payload.events.length,allOwnedContextRetained:true};
});
const output={success:true,stages,scope:'Actual Unity serialization snapshots through the local gateway context filter, including old-room revisits. No model or generated response.',verifiedAt:new Date().toISOString()};
fs.writeFileSync(path.join(reportDir,'mentor-payload-context-regression.json'),JSON.stringify(output,null,2));console.log(JSON.stringify(output,null,2));

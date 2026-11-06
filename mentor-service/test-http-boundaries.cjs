// Tests only request rejection/lifetime. Uses an ephemeral loopback listener, never a model.
const assert=require('node:assert/strict'),http=require('node:http'),fs=require('node:fs'),path=require('node:path');
const {once}=require('node:events');
const {server,localModelUrl}=require('./server.cjs');
async function main(){
  const checked=[];
  server.listen(0,'127.0.0.1');await once(server,'listening');
  const base='http://127.0.0.1:'+server.address().port;
  const post=(body,headers={})=>fetch(base+'/mentor',{method:'POST',headers:{'Content-Type':'application/json',...headers},body,signal:AbortSignal.timeout(3000)});
  try{
    for(const value of [null,[],42,'not an object']){const r=await post(JSON.stringify(value));assert.equal(r.status,400);assert((await r.json()).error);}
    checked.push('null, array and primitive JSON rejected without model access');
    assert.equal((await post('{broken')).status,400);checked.push('malformed JSON rejected');
    assert.equal((await post(JSON.stringify({question:''}))).status,400);
    assert.equal((await post(JSON.stringify({question:'问'.repeat(241)}))).status,400);checked.push('empty and overlong questions rejected');
    for(const question of [null,[],123,{toString:'invalid'}])assert.equal((await post(JSON.stringify({question}))).status,400);
    checked.push('non-string question fields rejected without coercion or inference');
    assert.equal((await post(JSON.stringify({question:'问',ignored:'字'.repeat(5000)}))).status,413);checked.push('raw request byte limit enforced for UTF-8');
    assert.equal((await post('{}',{Origin:'https://example.com'})).status,403);checked.push('external browser origin rejected before inference');
    const cors=await fetch(base+'/mentor',{method:'OPTIONS',headers:{Origin:'http://localhost:8097'},signal:AbortSignal.timeout(3000)});
    assert.equal(cors.status,204);assert.equal(cors.headers.get('access-control-allow-origin'),'http://localhost:8097');checked.push('local browser CORS preflight accepted');
    // Destroy an upload after its first byte. The following request must still reach a live server.
    await new Promise(resolve=>{
      const req=http.request(base+'/mentor',{method:'POST',headers:{'Content-Length':'1000'}});
      req.on('error',()=>resolve());req.on('socket',socket=>socket.once('connect',()=>{req.write('{');setTimeout(()=>{req.destroy();resolve();},30);}));
    });
    const afterAbort=await fetch(base+'/missing',{signal:AbortSignal.timeout(3000)});assert.equal(afterAbort.status,404);checked.push('interrupted upload does not terminate gateway');
    assert.equal(localModelUrl('http://127.0.0.1:39572/'),'http://127.0.0.1:39572');
    for(const value of ['https://example.com','http://example.com','http://user:pass@localhost:39572','http://localhost:39572/?token=secret'])assert.throws(()=>localModelUrl(value));
    checked.push('model upstream cannot accidentally point to an external vendor');
    const report={success:true,checked,scope:'HTTP rejection/lifetime tests on an ephemeral loopback port only. No model server started and no real AI response is claimed.',verifiedAt:new Date().toISOString()};
    fs.writeFileSync(path.resolve(__dirname,'../unity/GuanshanYouyan2D/ImportReports/mentor-http-boundary-tests.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report,null,2));
  }finally{server.closeAllConnections();await new Promise(resolve=>server.close(resolve));}
}
main().catch(error=>{console.error(error);process.exitCode=1;});

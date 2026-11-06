// Temporary failure-only QA fixture. Never starts a model and never returns an answer.
const http=require('node:http'),fs=require('node:fs'),path=require('node:path');
const port=Number(process.argv[2]||39573);
const output=path.resolve(__dirname,'../unity/GuanshanYouyan2D/ImportReports/mentor-fault-fixture.json');
const state={fixture:true,realModel:false,port,healthRequests:0,heldQuestions:0,clientClosures:0};
const save=()=>fs.writeFileSync(output,JSON.stringify({...state,updatedAt:new Date().toISOString()},null,2));
const server=http.createServer((req,res)=>{
  if(req.method==='GET'&&req.url==='/health'){
    state.healthRequests++;save();res.writeHead(200,{'Content-Type':'application/json'});res.end(JSON.stringify({ready:true,fixture:true,local:true,realModel:false}));return;
  }
  if(req.method==='POST'&&req.url==='/mentor'){
    state.heldQuestions++;save();req.resume();res.on('close',()=>{state.clientClosures++;save();});
    // Intentionally leave the response unfinished so the actual Unity client must timeout/cancel.
    return;
  }
  res.writeHead(404).end();
});
server.listen(port,'127.0.0.1',()=>{save();console.log('Failure-only fixture on loopback '+port+'; no model and no generated replies.');});

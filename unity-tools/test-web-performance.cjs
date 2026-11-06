const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),vm=require('node:vm'),path=require('node:path');
const source=fs.readFileSync(path.join(__dirname,'web-performance.js'),'utf8');
function setup(width,height,dpr){
 const events={},canvas={width:1,height:1,getBoundingClientRect:()=>({width,height})},config={};
 const window={devicePixelRatio:dpr,addEventListener:(name,fn)=>events[name]=fn};
 const document={addEventListener:(name,fn)=>events[name]=fn};
 vm.runInNewContext(source,{window,document,Math});window.FogWebPerformance.configure(canvas,config);
 return {canvas,config,window,resize:(w,h)=>{width=w;height=h;events.resize();}};
}
for(const [width,height,dpr] of [[1600,900,2],[3840,2160,2],[844,390,3],[1920,1080,1],[390,844,3]])test(`render budget ${width}x${height} DPR ${dpr}`,()=>{
 const {canvas,config}=setup(width,height,dpr);
 assert(canvas.width*canvas.height<=1920*1080+5000);
 assert(Math.abs(canvas.width/canvas.height-width/height)<.005);
 assert.equal(config.matchWebGLToCanvasSize,false);
});
test('fullscreen and viewport resize retain the budget and aspect',()=>{
 const s=setup(1600,900,2);s.resize(2560,1440);
 assert.equal(s.canvas.width,1920);assert.equal(s.canvas.height,1080);
 s.resize(844,390);assert.equal(s.canvas.width,1055);assert.equal(s.canvas.height,488);
});
test('a running player receives engine resize notifications',()=>{
 const s=setup(1600,900,2);let calls=0;
 s.window.fogGameInstance={Module:{setCanvasSize:(w,h)=>{calls++;s.canvas.width=w;s.canvas.height=h;}}};
 s.resize(844,390);assert.equal(calls,1);assert.equal(s.canvas.width,1055);
 s.resize(844,390);assert.equal(calls,1);
});

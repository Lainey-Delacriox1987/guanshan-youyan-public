(function(root){
'use strict';
// Projected stride / native Walk period: the ground and feet travel together.
const WORLD={width:2400,height:800,minX:100,maxX:2300,speed:205.9263746576055,range:115};
const npc=(id,x,label)=>({key:'npc-'+id,type:'npc',id,x,label});
const clue=(id,x,label)=>({key:'clue-'+id,type:'inspect',id,x,label});
const door=(key,x,label,to,spawn)=>({key,type:'door',x,label,to,spawn});
const board=x=>({key:'board',type:'board',x,label:'整理线索'});
const exit=(x,label)=>({key:'exit',type:'advance',x,label});
const final=x=>({key:'final',type:'final',x,label:'提交最终推理'});
const rooms=[
 [{name:'港口堆场',art:'walk-harbor',floor:650,description:'沿着货物的来路，走近每一处细节。',objects:[{...exit(300,'接驳车 · 前往仓库'),transport:'shuttle'},npc('guard',670,'值班员'),clue('seal',1200,'货柜封条'),clue('cargo',1510,'查验货箱'),door('inspection-door',2160,'进入查验室',1,330)]},
  {name:'查验办公室',art:'walk-inspection',floor:650,description:'把货单与实物摆在一起核对。',objects:[door('yard-door',240,'返回堆场',0,2020),clue('weight',690,'货单与地磅'),npc('mentor',1120,'许岚'),board(1590)]}],
 [{name:'物流分拣区',art:'walk-warehouse',floor:640,stairs:{start:1530,end:2010,top:290},description:'分拣台在一楼，调度室在楼上。',objects:[exit(130,'装卸门 · 前往青禾镇'),clue('parcels',500,'分拣台运单'),npc('worker',1090,'仓管老赵'),door('dispatch-door',2260,'二层调度室',1,330)]},
  {name:'二层调度室',art:'walk-dispatch',floor:650,description:'调度记录与监控，需要互相印证。',objects:[door('warehouse-door',240,'返回楼梯平台',0,2130),clue('camera',660,'监控记录'),clue('dispatch',1160,'转运登记'),npc('mentor',1580,'许岚'),board(2030)]}],
 [{name:'青禾镇街巷',art:'walk-town',floor:620,description:'从店门进入，也能从后门返回街巷。',objects:[npc('courier',340,'快递员周叔'),door('shop-door',885,'进入禾记美妆',1,350),npc('shop',1240,'店主苏禾'),{key:'contact',type:'npc',id:'suspect',x:1680,label:'电话联系陈渡',prop:'phone'},exit(2240,'巷口 · 前往调查中心')]},
  {name:'禾记美妆 · 后室',art:'walk-records',floor:650,description:'批次、账目与来源，各有一份答案。',objects:[door('front-door',230,'由店门返回街巷',0,1000),clue('shelf',610,'店铺橱窗'),clue('account',1050,'店内账簿'),clue('wildlife',1450,'可疑纪念品'),board(1770),door('back-door',2190,'后门通往街巷',0,1950)]}],
 [{name:'调查中心 · 询问室',art:'walk-interview',floor:650,description:'走近陈渡，用已经核验的材料追问。',objects:[npc('mentor',430,'许岚'),npc('suspect',980,'询问陈渡'),final(1470),{...door('archive-door',2140,'乘电梯 · 档案层',1,330),type:'lift'}]},
  {name:'调查中心 · 案卷室',art:'walk-archive',floor:650,description:'最后一次，把判断放回证据里检验。',objects:[{...door('interview-door',240,'乘电梯 · 返回询问层',0,1980),type:'lift'},board(780),{key:'notes',type:'notes',x:1320,label:'调查手记'},final(1910)]}]
];
// Scene art has different camera distances: calibrate actors to its doors/desks.
[[260,390],[270,410],[300,390],[350,380]].forEach((pair,stage)=>pair.forEach((height,room)=>{rooms[stage][room].actorHeight=height;}));
function actorScale(stage,room){return rooms[stage][room].actorHeight/240;}
function portalX(stage,room,o){return rooms[stage][room].stairs&&o.to===1?rooms[stage][room].stairs.end:o.x;}
function crossedPortal(stage,room,from,to,armed){if(from===to)return null;return rooms[stage][room].objects.find(o=>{if(!['door','lift'].includes(o.type)||!armed.has(o.key))return false;const x=portalX(stage,room,o);if(rooms[stage][room].stairs&&o.to===1&&to<from)return false;return Math.max(from,to)>=x-24&&Math.min(from,to)<=x+24;})||null;}
function floorY(stage,room,x){const r=rooms[stage][room],s=r.stairs;if(!s||x<=s.start)return r.floor;if(x>=s.end)return s.top;return r.floor+(s.top-r.floor)*(x-s.start)/(s.end-s.start);}
function clamp(x){return Math.min(WORLD.maxX,Math.max(WORLD.minX,Number.isFinite(x)?x:280));}
function restore(raw,stage){const r=raw&&Number.isInteger(raw.room)&&raw.room===1?1:0;return {room:r,x:clamp(raw?.x),facing:raw?.facing===-1?-1:1};}
function step(x,target,dt,scale=1){const d=clamp(target)-x,travel=WORLD.speed*scale*Math.min(.05,Math.max(0,dt));return clamp(x+Math.sign(d)*Math.min(Math.abs(d),travel));}
function gait(distance,moving,atlas){const columns=atlas.columns||4,rows=atlas.rows||1,frames=atlas.walkFrames||[1,2,3,2],cycle=atlas.cycleDistance||300,phase=((distance%cycle)+cycle)%cycle/cycle,frame=moving?frames[Math.floor(phase*frames.length)]:atlas.idleFrame||0;return {frame,x:columns>1?frame%columns*100/(columns-1):0,y:rows>1?Math.floor(frame/columns)*100/(rows-1):0,bob:moving?Math.sin(phase*Math.PI*4)*(atlas.bobAmount??1.4):0};}
function nearest(stage,room,x){return rooms[stage][room].objects.filter(o=>Math.abs(o.x-x)<=WORLD.range).sort((a,b)=>Math.abs(a.x-x)-Math.abs(b.x-x))[0]||null;}
function locate(stage,type,id){for(let room=0;room<2;room++){const object=rooms[stage][room].objects.find(o=>o.type===type&&(id===undefined||o.id===id));if(object)return {room,object};}return null;}
const W={WORLD,rooms,floorY,clamp,restore,step,gait,nearest,locate,actorScale,portalX,crossedPortal};if(typeof module!=='undefined'&&module.exports)module.exports=W;else root.MistWalkData=W;
})(typeof window!=='undefined'?window:globalThis);

/* One illustrated texture and the approved joint curves. The GPU bends the art;
   walking does not require downloading a large sheet of independent poses. */
(function(root){
'use strict';
const R=root.MistPlayerRigData,clamp=(v,a,b)=>Math.min(b,Math.max(a,v)),lerp=(a,b,t)=>a.map((v,i)=>v+(b[i]-v)*t);
function mix(a,b,t){if(typeof a==='number')return a+(b-a)*t;if(Array.isArray(a))return a.map((v,i)=>mix(v,b[i],t));const out={};for(const key in a)out[key]=mix(a[key],b[key],t);return out;}
function pose(distance,moving,cycle){if(!moving)return R.poses[0];const phase=((distance%cycle)+cycle)%cycle/cycle*32,index=Math.floor(phase);return mix(R.poses[index+1],R.poses[(index+1)%32+1],phase-index);}
function bend(src,dst,width,x,y){let i=y<src[1][1]?0:1;if(src.length===4&&y>=src[2][1])i=2;const a=src[i],b=src[i+1],p=dst[i],q=dst[i+1],t=(y-a[1])/(b[1]-a[1]),center=lerp(p,q,t),cx=a[0]+(b[0]-a[0])*t;let angle=Math.atan2(-(q[0]-p[0]),q[1]-p[1]);for(let j=1;j<src.length-1;j++){const w=24,d=y-src[j][1];if(Math.abs(d)<w){const u=clamp((d+w)/(2*w),0,1),before=Math.atan2(-(dst[j][0]-dst[j-1][0]),dst[j][1]-dst[j-1][1]),after=Math.atan2(-(dst[j+1][0]-dst[j][0]),dst[j+1][1]-dst[j][1]);angle=before+(after-before)*u;}}return [center[0]+Math.cos(angle)*(x-cx)*width,center[1]+Math.sin(angle)*(x-cx)*width];}
function vertices(p,meta,idle=false){const data=[],parts=meta.regions;
 function strip(name,map,shade=1,alpha=()=>1,rows=48){const r=parts[name],point=(x,y)=>{const pos=map(x,y);return [pos[0],pos[1],(r.x+x)/meta.width,(r.y+y)/meta.height,shade,alpha(y)]};for(let i=0;i<rows;i++){const y=i/rows*r.height,z=(i+1)/rows*r.height,a=point(0,y),b=point(r.width,y),c=point(0,z),d=point(r.width,z);data.push(...a,...b,...c,...b,...d,...c);}}
 if(idle){const r=parts.portrait,s=R.frameHeight*.91/r.height;strip('portrait',(x,y)=>[R.frameWidth/2+(x-r.width/2)*s,R.frameHeight*.95+(y-r.height)*s]);return new Float32Array(data);}
 function leg(side,shade){const l=p.legs[side],s=R.sourceLeg;strip('leg',(x,y)=>{const dx=(x-R.sourceBall[0])*R.legWidth,dy=(y-R.sourceBall[1])*R.legWidth,foot=[l.ball[0]+Math.cos(l.angle)*dx-Math.sin(l.angle)*dy,l.ball[1]+Math.sin(l.angle)*dx+Math.cos(l.angle)*dy],limb=bend(s,l.points,R.legWidth,x,y);return lerp(limb,foot,clamp((y-s[2][1]+12)/24,0,1));},shade);}
 function arm(side,shade){strip('arm',(x,y)=>bend(R.sourceArm,p.arms[side],R.armWidth,x,y),shade);}
 // Far hand passes behind both the torso AND trousers, never over a thigh.
 arm('Left',.83);leg('Left',.83);leg('Right',1);
 const b=p.body,r=parts.body;strip('body',(x,y)=>{const dx=(x-r.width*.51)*R.bodyScale,dy=(y-r.height*.90)*R.bodyScale;return [b.hip[0]+Math.cos(b.angle)*dx-Math.sin(b.angle)*dy,b.hip[1]+Math.sin(b.angle)*dx+Math.cos(b.angle)*dy];},1,y=>clamp((r.height-y)/35,0,1));
 arm('Right',1);return new Float32Array(data);
}
function image(src){return new Promise((resolve,reject)=>{const img=new Image();img.onload=()=>resolve(img);img.onerror=reject;img.src=src;});}
async function create(canvas,url,meta){
 async function fallback(){const img=await image(meta.fallback.webp.src);canvas.style.backgroundImage=`url("${img.src}")`;canvas.dataset.renderer='atlas-fallback';return {draw(){},destroy(){}};}
 // Browsers forbid uploading opaque file:// origins to WebGL. Keep direct-file
 // offline delivery fully functional through the same pre-rendered motion.
 if(root.location.protocol==='file:')return fallback();
 const gl=canvas.getContext('webgl',{alpha:true,antialias:true,premultipliedAlpha:true,depth:false,preserveDrawingBuffer:true});
 if(!gl)return fallback();
 const img=await image(url);canvas.width=R.frameWidth;canvas.height=R.frameHeight;canvas.style.backgroundImage='none';canvas.dataset.renderer='illustrated-skeletal-webgl';
 const vs='attribute vec2 a_pos;attribute vec2 a_uv;attribute vec2 a_tone;varying vec2 v_uv;varying vec2 v_tone;void main(){gl_Position=vec4(a_pos.x/'+R.frameWidth+'.0*2.0-1.0,1.0-a_pos.y/'+R.frameHeight+'.0*2.0,0.0,1.0);v_uv=a_uv;v_tone=a_tone;}',fs='precision mediump float;uniform sampler2D u_tex;varying vec2 v_uv;varying vec2 v_tone;void main(){vec4 c=texture2D(u_tex,v_uv);gl_FragColor=vec4(c.rgb*v_tone.x*v_tone.y,c.a*v_tone.y);}';
 function shader(type,source){const s=gl.createShader(type);gl.shaderSource(s,source);gl.compileShader(s);if(!gl.getShaderParameter(s,gl.COMPILE_STATUS))throw Error('Character shader failed');return s;}
 const program=gl.createProgram(),shaders=[shader(gl.VERTEX_SHADER,vs),shader(gl.FRAGMENT_SHADER,fs)];for(const s of shaders)gl.attachShader(program,s);gl.linkProgram(program);if(!gl.getProgramParameter(program,gl.LINK_STATUS))throw Error('Character program failed');gl.useProgram(program);
 const buffer=gl.createBuffer();gl.bindBuffer(gl.ARRAY_BUFFER,buffer);for(const [name,offset]of [['a_pos',0],['a_uv',8],['a_tone',16]]){const loc=gl.getAttribLocation(program,name);gl.enableVertexAttribArray(loc);gl.vertexAttribPointer(loc,2,gl.FLOAT,false,24,offset);}
 const texture=gl.createTexture();gl.bindTexture(gl.TEXTURE_2D,texture);gl.pixelStorei(gl.UNPACK_PREMULTIPLY_ALPHA_WEBGL,true);gl.texImage2D(gl.TEXTURE_2D,0,gl.RGBA,gl.RGBA,gl.UNSIGNED_BYTE,img);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_S,gl.CLAMP_TO_EDGE);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_T,gl.CLAMP_TO_EDGE);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,gl.LINEAR);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,gl.LINEAR);gl.enable(gl.BLEND);gl.blendFunc(gl.ONE,gl.ONE_MINUS_SRC_ALPHA);gl.viewport(0,0,canvas.width,canvas.height);
 let previous=-1,stopped=null;function draw(distance,moving){if(stopped===moving&&(!moving||Math.abs(distance-previous)<.01))return;stopped=moving;previous=distance;const v=vertices(pose(distance,moving,meta.cycleDistance),meta,!moving);gl.clearColor(0,0,0,0);gl.clear(gl.COLOR_BUFFER_BIT);gl.bufferData(gl.ARRAY_BUFFER,v,gl.DYNAMIC_DRAW);gl.drawArrays(gl.TRIANGLES,0,v.length/6);}
 draw(0,false);return {draw,destroy(){gl.deleteBuffer(buffer);gl.deleteTexture(texture);gl.deleteProgram(program);for(const s of shaders)gl.deleteShader(s);queueMicrotask(()=>{if(!canvas.isConnected)gl.getExtension('WEBGL_lose_context')?.loseContext();});}};
}
root.MistPlayerRig={create,pose,vertices};
})(window);

// Retarget the approved humanoid joint motion onto the illustrated character.
// This is a textured 2D mesh, not a collection of generated walking poses.
const fs=require('node:fs'),path=require('node:path');
const sharp=require('sharp'),{createCanvas,loadImage,ImageData}=require('@napi-rs/canvas');
const root=path.resolve(__dirname,'../..'),out=path.join(root,'output/illustrated-rig');fs.mkdirSync(out,{recursive:true});
const motion=JSON.parse(fs.readFileSync(path.join(root,'output/skeletal-walk-model.json'),'utf8'));
const W=512,H=640,unit=H/6,baseline=H*.95,project=p=>[W/2+(.7*p[0]/Math.hypot(9,.7)-motion.forwardProjection*p[1])*unit,baseline-p[2]*unit];
const lerp=(a,b,t)=>a.map((v,i)=>v+(b[i]-v)*t),clamp=(v,a,b)=>Math.min(b,Math.max(a,v));
const point=(r,b,tail=false)=>project(r.bones[b][tail?'tail':'head']);
function affine(ctx,img,s,d){
 const [a,b,c]=s,[p,q,r]=d,det=(b[0]-a[0])*(c[1]-a[1])-(c[0]-a[0])*(b[1]-a[1]);
 if(Math.abs(det)<1e-6)return;
 const xx=((q[0]-p[0])*(c[1]-a[1])-(r[0]-p[0])*(b[1]-a[1]))/det,xy=((r[0]-p[0])*(b[0]-a[0])-(q[0]-p[0])*(c[0]-a[0]))/det;
 const yx=((q[1]-p[1])*(c[1]-a[1])-(r[1]-p[1])*(b[1]-a[1]))/det,yy=((r[1]-p[1])*(b[0]-a[0])-(q[1]-p[1])*(c[0]-a[0]))/det;
 const center=[(p[0]+q[0]+r[0])/3,(p[1]+q[1]+r[1])/3],expand=v=>{const dx=v[0]-center[0],dy=v[1]-center[1],l=Math.hypot(dx,dy);return [v[0]+dx/l*.55,v[1]+dy/l*.55]};
 const verts=d.map(expand);ctx.save();ctx.beginPath();ctx.moveTo(...verts[0]);ctx.lineTo(...verts[1]);ctx.lineTo(...verts[2]);ctx.closePath();ctx.clip();ctx.setTransform(xx,yx,xy,yy,p[0]-xx*a[0]-xy*a[1],p[1]-yx*a[0]-yy*a[1]);ctx.drawImage(img,0,0);ctx.restore();
}
function mesh(ctx,img,map,far=false){
 const target=new Uint8ClampedArray(W*H*4),texture=img.pixels;
 function triangle(s,d){const [a,b,c]=d,den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1]);if(Math.abs(den)<1e-7)return;
  const x0=Math.max(0,Math.floor(Math.min(...d.map(p=>p[0])))),x1=Math.min(W-1,Math.ceil(Math.max(...d.map(p=>p[0])))),y0=Math.max(0,Math.floor(Math.min(...d.map(p=>p[1])))),y1=Math.min(H-1,Math.ceil(Math.max(...d.map(p=>p[1]))));
  for(let y=y0;y<=y1;y++)for(let x=x0;x<=x1;x++){const p=x+.5,q=y+.5,u=((b[1]-c[1])*(p-c[0])+(c[0]-b[0])*(q-c[1]))/den,v=((c[1]-a[1])*(p-c[0])+(a[0]-c[0])*(q-c[1]))/den,w=1-u-v;if(Math.min(u,v,w)<-1e-6)continue;
   const sx=clamp(s[0][0]*u+s[1][0]*v+s[2][0]*w,0,img.width-1),sy=clamp(s[0][1]*u+s[1][1]*v+s[2][1]*w,0,img.height-1),ix=Math.floor(sx),iy=Math.floor(sy),fx=sx-ix,fy=sy-iy,dst=(y*W+x)*4;
   for(let channel=0;channel<4;channel++){let value=0;for(let yy=0;yy<2;yy++)for(let xx=0;xx<2;xx++)value+=texture[(Math.min(img.height-1,iy+yy)*img.width+Math.min(img.width-1,ix+xx))*4+channel]*(xx?fx:1-fx)*(yy?fy:1-fy);target[dst+channel]=channel===3?value:value*(far?.83:1);}
  }
 }
 const rows=96;for(let i=0;i<rows;i++){const y=i/rows*img.height,z=(i+1)/rows*img.height,s=[[0,y],[img.width,y],[0,z],[img.width,z]],d=s.map(map);triangle([s[0],s[1],s[2]],[d[0],d[1],d[2]]);triangle([s[1],s[3],s[2]],[d[1],d[3],d[2]]);}
 const layer=createCanvas(W,H);layer.getContext('2d').putImageData(new ImageData(target,W,H),0,0);ctx.drawImage(layer,0,0);
}
function bend(src,dst,width,x,y){
 let i=y<src[1][1]?0:1;if(src.length===4&&y>=src[2][1])i=2;
 const a=src[i],b=src[i+1],p=dst[i],q=dst[i+1],t=(y-a[1])/(b[1]-a[1]),center=lerp(p,q,t),cx=a[0]+(b[0]-a[0])*t;
 let angle=Math.atan2(-(q[0]-p[0]),q[1]-p[1]);
 // Average the directions around the elbow/knee to keep the cloth continuous.
 for(let j=1;j<src.length-1;j++){const w=24,dist=y-src[j][1];if(Math.abs(dist)<w){const u=clamp((dist+w)/(w*2),0,1),before=Math.atan2(-(dst[j][0]-dst[j-1][0]),dst[j][1]-dst[j-1][1]),after=Math.atan2(-(dst[j+1][0]-dst[j][0]),dst[j+1][1]-dst[j][1]);angle=before+(after-before)*u;}}
 return [center[0]+Math.cos(angle)*(x-cx)*width,center[1]+Math.sin(angle)*(x-cx)*width];
}
(async()=>{
 const input=path.join(root,'h5/design/motion-reference/LinJian-illustrated-parts-alpha.png'),{data,info}=await sharp(input).ensureAlpha().raw().toBuffer({resolveWithObject:true}),parts={};
 for(const [name,col,row,qw,qh]of [['body',0,0,512,768],['arm',512,0,512,710],['leg',0,768,512,768],['portrait',512,710,512,826]]){
  const partInput=name==='body'?path.join(root,'h5/design/motion-reference/LinJian-illustrated-torso-edit.png'):input,partData=name==='body'?await sharp(partInput).ensureAlpha().raw().toBuffer():data;
  let x0=qw,y0=qh,x1=0,y1=0;
  for(let y=0;y<qh;y++)for(let x=0;x<qw;x++)if(partData[((((row+y)*info.width+col+x)*4)+3)]>48){x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x);y1=Math.max(y1,y);}
  x0=Math.max(0,x0-3);y0=Math.max(0,y0-3);x1=Math.min(qw-1,x1+3);y1=Math.min(qh-1,y1+3);
  const file=path.join(out,name+'.png');await sharp(partInput).extract({left:col+x0,top:row+y0,width:x1-x0+1,height:y1-y0+1}).png().toFile(file);parts[name]=await loadImage(file);parts[name].pixels=await sharp(file).ensureAlpha().raw().toBuffer();
  console.log(name,{left:col+x0,top:row+y0,width:x1-x0+1,height:y1-y0+1});
 }
 const bodyPatch=path.join(root,'h5/design/motion-reference/LinJian-torso-single-epaulette.png');
 if(fs.existsSync(bodyPatch)){const file=path.join(out,'body.png');await sharp(bodyPatch).resize(parts.body.width,parts.body.height).png().toFile(file);parts.body=await loadImage(file);parts.body.pixels=await sharp(file).ensureAlpha().raw().toBuffer();}
 const leg=parts.leg,arm=parts.arm;
 // Landmarks are in the generated components, expressed as fractions of its bounds.
 const LS=[[.34,.12],[.29,.50],[.34,.90]],ball=[leg.width*.73,leg.height-2],legWidth=.43;
 const srcLeg=LS.map(([x,y])=>[x*leg.width,y*leg.height]);
 const srcArm=[[.45,.12],[.44,.50],[.65,.85],[.65,.99]].map(([x,y])=>[x*arm.width,y*arm.height]);
 const bodyLayer=createCanvas(parts.body.width,parts.body.height),bodyCtx=bodyLayer.getContext('2d');bodyCtx.drawImage(parts.body,0,0);bodyCtx.globalCompositeOperation='destination-in';const hem=bodyCtx.createLinearGradient(0,bodyLayer.height-35,0,bodyLayer.height);hem.addColorStop(0,'#ffffffff');hem.addColorStop(1,'#ffffff00');bodyCtx.fillStyle=hem;bodyCtx.fillRect(0,0,bodyLayer.width,bodyLayer.height);
 const records=[],poses=[];
 for(const record of motion.records){
  const c=createCanvas(W,H),ctx=c.getContext('2d'),framePose={legs:{},arms:{}};ctx.imageSmoothingEnabled=true;
  const drawLeg=(side,far)=>{
   const hip=point(record,side+'UpLeg'),knee=point(record,side+'Leg'),toe=point(record,side+'ToeBase'),ankleNative=point(record,side+'Foot');
   const flat=record.sourceFeet[side][2]<.025||record.frame===0;
   const angle=flat?0:clamp(Math.atan2(toe[1]-ankleNative[1],toe[0]-ankleNative[0])-.38,-.48,.43);
   const targetBall=[toe[0],baseline-record.soleHeights[side]*unit];
   const foot=p=>{const dx=(p[0]-ball[0])*legWidth,dy=(p[1]-ball[1])*legWidth;return [targetBall[0]+Math.cos(angle)*dx-Math.sin(angle)*dy,targetBall[1]+Math.sin(angle)*dx+Math.cos(angle)*dy]};
   const ankle=foot(srcLeg[2]),dst=[hip,knee,ankle];
   framePose.legs[side]={points:dst,ball:targetBall,angle};
   mesh(ctx,leg,([x,y])=>{const a=bend(srcLeg,dst,legWidth,x,y),b=foot([x,y]),t=clamp((y-srcLeg[2][1]+12)/24,0,1);return lerp(a,b,t)},far);
   records.push({frame:record.frame,side,contact:flat,ball:targetBall,angle});
  };
  const drawArm=(side,far)=>{const dst=[point(record,side+'Arm'),point(record,side+'ForeArm'),point(record,side+'Hand'),point(record,side+'HandIndex3',true)].map((p,i)=>[p[0]+[-26,-14,0,0][i],p[1]+8]);const rawHip=point(record,'Hips'),hip=[rawHip[0]+5,rawHip[1]],spine=point(record,'Spine2'),angle=Math.atan2(spine[0]-rawHip[0],rawHip[1]-spine[1])+.065,dx=(parts.body.width*.27-parts.body.width*.51)*.49,dy=(parts.body.height*.46-parts.body.height*.90)*.49,anchor=[hip[0]+Math.cos(angle)*dx-Math.sin(angle)*dy+(far?8:0),hip[1]+Math.sin(angle)*dx+Math.cos(angle)*dy+(far?2:0)],offset=[anchor[0]-dst[0][0],anchor[1]-dst[0][1]];for(const p of dst){p[0]+=offset[0];p[1]+=offset[1];}framePose.arms[side]=dst;mesh(ctx,arm,([x,y])=>bend(srcArm,dst,.43,x,y),far);};
  drawArm('Left',true);drawLeg('Left',true);drawLeg('Right',false);
  const rawHip=point(record,'Hips'),hip=[rawHip[0]+5,rawHip[1]],spine=point(record,'Spine2'),body=parts.body,angle=Math.atan2(spine[0]-rawHip[0],rawHip[1]-spine[1])+.065;
  ctx.save();ctx.translate(...hip);ctx.rotate(angle);const scale=.49;ctx.drawImage(bodyLayer,-body.width*.51*scale,-body.height*.90*scale,body.width*scale,body.height*scale);ctx.restore();
  drawArm('Right',false);
  framePose.body={hip,angle};poses.push(framePose);
  if(record.frame===0){ctx.clearRect(0,0,W,H);const portrait=parts.portrait,h=H*.91,w=portrait.width/portrait.height*h;ctx.drawImage(portrait,(W-w)/2,baseline-h,w,h);}
  fs.writeFileSync(path.join(out,String(record.frame).padStart(2,'0')+'.png'),c.toBuffer('image/png'));
 }
 fs.writeFileSync(path.join(out,'retarget.json'),JSON.stringify({renderer:'illustrated 2D mesh driven by native Walk joints',frameWidth:W,frameHeight:H,groundBaseline:.95,cycleDistance:motion.cycleDistance,cycleSeconds:motion.cycleSeconds,sourceLeg:srcLeg,sourceArm:srcArm,sourceBall:ball,legWidth,armWidth:.43,bodyScale:.49,poses,records},(_,v)=>typeof v==='number'?Math.round(v*10000)/10000:v,2));
})();

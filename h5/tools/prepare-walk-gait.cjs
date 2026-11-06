const fs=require('node:fs'),path=require('node:path'),vm=require('node:vm'),crypto=require('node:crypto');
const sharp=require('sharp');
const root=path.resolve(__dirname,'../..'),dir=path.join(root,'h5/public/assets/walk'),input=JSON.parse(fs.readFileSync(path.join(root,'output/walk-gait-generation.json'),'utf8'));
const cellWidth=448,cellHeight=640,baseline=616,bodyHeight=584;
async function frame(file,grid,index){
 const m=await sharp(file).metadata(),col=index%grid,row=Math.floor(index/grid),left=Math.max(0,Math.floor(col*m.width/grid)-12),top=Math.max(0,Math.floor(row*m.height/grid)-40),width=Math.min(m.width,Math.floor((col+1)*m.width/grid)+12)-left,height=Math.min(m.height,Math.floor((row+1)*m.height/grid)+40)-top;
 const {data,info}=await sharp(file).extract({left,top,width,height}).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 let x0=width,y0=height,x1=0,y1=0,largest=0;const visited=new Uint8Array(width*height);for(let seed=0;seed<visited.length;seed++){if(visited[seed]||data[seed*4+3]<=16)continue;const queue=[seed];visited[seed]=1;let minX=width,minY=height,maxX=0,maxY=0;for(let n=0;n<queue.length;n++){const v=queue[n],x=v%width,y=Math.floor(v/width);minX=Math.min(minX,x);maxX=Math.max(maxX,x);minY=Math.min(minY,y);maxY=Math.max(maxY,y);for(const next of [x>0?v-1:-1,x<width-1?v+1:-1,y>0?v-width:-1,y<height-1?v+width:-1])if(next>=0&&!visited[next]&&data[next*4+3]>16){visited[next]=1;queue.push(next);}}if(queue.length>largest){largest=queue.length;x0=minX;x1=maxX;y0=minY;y1=maxY;}}
 if(x1<=x0||y1<=y0)throw Error('Empty gait frame');
 const h=y1-y0+1,w=x1-x0+1,head=[];for(let y=y0+Math.floor(h*.06);y<y0+Math.floor(h*.18);y++)for(let x=x0;x<=x1;x++)if(data[(y*width+x)*4+3]>100)head.push(x-x0);head.sort((a,b)=>a-b);const anchor=head[Math.floor(head.length/2)];
 const scale=Math.min(bodyHeight/h,(cellWidth-24)/w),outW=Math.round(w*scale),outH=Math.round(h*scale),offsetX=Math.max(8,Math.min(cellWidth-outW-8,Math.round(cellWidth/2-anchor*scale)));
 const buffer=await sharp(data,{raw:info}).extract({left:x0,top:y0,width:w,height:h}).resize(outW,outH).png().toBuffer();
 return sharp({create:{width:cellWidth,height:cellHeight,channels:4,background:'#00000000'}}).composite([{input:buffer,left:offsetX,top:baseline-outH}]).png().toBuffer();
}
(async()=>{
 for(const [key,file]of Object.entries(input.paths))fs.copyFileSync(file,path.join(dir,'gait-'+key+'-source.png'));
 const frames=[];if(input.paths.low){for(let index=0;index<9;index++)frames.push(await frame(input.paths.low,3,index));}else{for(const index of [0,1,2,3,4])frames.push(await frame(input.paths.base,3,index));for(const index of [0,1,2,3])frames.push(await frame(input.paths.opposite,2,index));}
 const png=await sharp({create:{width:cellWidth*3,height:cellHeight*3,channels:4,background:'#00000000'}}).composite(frames.map((input,i)=>({input,left:i%3*cellWidth,top:Math.floor(i/3)*cellHeight}))).png().toBuffer();
 const encoded=await sharp(png).webp({quality:93,alphaQuality:100,effort:6}).toBuffer(),hash=crypto.createHash('sha256').update(encoded).digest('hex').slice(0,12),name='player-gait-'+hash+'.webp',src='assets/walk/'+name;
 fs.writeFileSync(path.join(dir,input.paths.low?'player-low-step-atlas.png':'player-gait-atlas.png'),png);fs.writeFileSync(path.join(dir,name),encoded);
 const file={src,bytes:encoded.length,type:'image/webp'},walker={width:cellWidth*3,height:cellHeight*3,columns:3,rows:3,frameWidth:cellWidth,frameHeight:cellHeight,idleFrame:0,walkFrames:[1,2,3,4,5,6,7,8],cycleDistance:300,bobAmount:input.paths.low?0:1.4,avif:file,webp:file,thumb:file};
 const window={MistArt:{}};vm.runInNewContext(fs.readFileSync(path.join(root,'h5/public/mist-walk-art.js'),'utf8'),{window});window.MistArt.walker=walker;fs.writeFileSync(path.join(root,'h5/public/mist-walk-art.js'),'(function(root){Object.assign(root.MistArt,'+JSON.stringify(window.MistArt)+');})(window);\n');
 fs.writeFileSync(path.join(root,'output/walk-gait-manifest.json'),JSON.stringify(walker,null,2));
 const gifFrames=[];for(const i of [1,2,3,4,5,6,7,8])gifFrames.push(await sharp(frames[i]).resize(224,320).raw().toBuffer());
 await sharp(Buffer.concat(gifFrames),{raw:{width:224,height:320*8,channels:4,pageHeight:320}}).gif({loop:0,delay:80}).toFile(path.join(root,'output/V1.0.2-左右脚步态.gif'));
 console.log(JSON.stringify(walker));
})().catch(e=>{console.error(e);process.exitCode=1});

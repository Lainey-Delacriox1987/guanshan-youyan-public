const fs=require('node:fs'),path=require('node:path'),vm=require('node:vm'),crypto=require('node:crypto');
const sharp=require('sharp');
const root=path.resolve(__dirname,'../..'),dir=path.join(root,'output/illustrated-rig'),pub=path.join(root,'h5/public');
(async()=>{
 const motion=JSON.parse(fs.readFileSync(path.join(root,'output/skeletal-walk-model.json'),'utf8')),rig=JSON.parse(fs.readFileSync(path.join(dir,'retarget.json'),'utf8'));
 const {frameWidth,frameHeight}=rig,columns=6,rows=6,frames=await Promise.all(Array.from({length:33},(_,i)=>fs.promises.readFile(path.join(dir,String(i).padStart(2,'0')+'.png'))));
 const portrait=await sharp(path.join(dir,'portrait.png')).resize({height:Math.round(frameHeight*.91)}).png().toBuffer(),pm=await sharp(portrait).metadata();
 frames.push(await sharp({create:{width:frameWidth,height:frameHeight,channels:4,background:'#00000000'}}).composite([{input:portrait,left:Math.round((frameWidth-pm.width)/2),top:Math.round(frameHeight*.95)-pm.height}]).png().toBuffer());
 const atlas=await sharp({create:{width:frameWidth*columns,height:frameHeight*rows,channels:4,background:'#00000000'}}).composite(frames.map((input,i)=>({input,left:i%columns*frameWidth,top:Math.floor(i/columns)*frameHeight}))).png().toBuffer();
 fs.writeFileSync(path.join(root,'design/LinJian-illustrated-skeletal-atlas.png'),atlas);
 const encoded=await sharp(atlas).webp({quality:93,alphaQuality:100,effort:6}).toBuffer(),hash=crypto.createHash('sha256').update(encoded).digest('hex').slice(0,12),src='assets/walk/player-illustrated-'+hash+'.webp';fs.writeFileSync(path.join(pub,src),encoded);
 const window={MistArt:{}};vm.runInNewContext(fs.readFileSync(path.join(pub,'mist-walk-art.js'),'utf8'),{window});
 const file={src,bytes:encoded.length,type:'image/webp'};window.MistArt.walker={width:frameWidth*columns,height:frameHeight*rows,frameWidth,frameHeight,columns,rows,idleFrame:0,portraitFrame:33,walkFrames:Array.from({length:32},(_,i)=>i+1),cycleDistance:motion.cycleDistance,cycleSeconds:motion.cycleSeconds,groundBaseline:rig.groundBaseline,bobAmount:0,renderer:rig.renderer,source:motion.source,action:'Walk',avif:file,webp:file,thumb:file};
 fs.writeFileSync(path.join(pub,'mist-walk-art.js'),'(function(root){Object.assign(root.MistArt,'+JSON.stringify(window.MistArt)+');})(window);\n');
 // Use the approved motion report; only the drawing renderer changes.
 fs.copyFileSync(path.join(root,'output/skeletal-walk-model.json'),path.join(pub,'docs/mist/skeletal-walk-model.json'));
 fs.copyFileSync(path.join(dir,'retarget.json'),path.join(pub,'docs/mist/illustrated-rig.json'));
 fs.writeFileSync(path.join(root,'output/illustrated-rig-manifest.json'),JSON.stringify(window.MistArt.walker,null,2));
 const gifFrames=await Promise.all(frames.slice(1,33).map(input=>sharp(input).resize(256,320).raw().toBuffer()));
 await sharp(Buffer.concat(gifFrames),{raw:{width:256,height:320*32,channels:4,pageHeight:320}}).gif({loop:0,delay:Math.round(motion.cycleSeconds*1000/32)}).toFile(path.join(root,'output/V1.0.2-插画骨骼步态.gif'));
 console.log(JSON.stringify({src,bytes:encoded.length,frames:32,portraitFrame:33,unchangedCycleDistance:motion.cycleDistance}));
})().catch(e=>{console.error(e);process.exitCode=1});

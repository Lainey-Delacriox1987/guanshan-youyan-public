// Native rig frames keep a single camera, scale and ground plane. Do not trim or
// normalize frames independently: that would destroy the measured foot plants.
const fs=require('node:fs'),path=require('node:path'),vm=require('node:vm'),crypto=require('node:crypto');
const sharp=require('sharp');
const root=path.resolve(__dirname,'../..'),pub=path.join(root,'h5/public');
(async()=>{
 const report=JSON.parse(fs.readFileSync(path.join(root,'output/skeletal-walk-model.json'),'utf8'));
 const {frameWidth,frameHeight}=report,columns=6,rows=6;
 const frames=await Promise.all(report.records.map(r=>fs.promises.readFile(path.join(root,'output/skeletal-walk',String(r.frame).padStart(2,'0')+'.png'))));
 const png=await sharp({create:{width:frameWidth*columns,height:frameHeight*rows,channels:4,background:'#00000000'}}).composite(frames.map((input,i)=>({input,left:i%columns*frameWidth,top:Math.floor(i/columns)*frameHeight}))).png().toBuffer();
 const encoded=await sharp(png).webp({quality:93,alphaQuality:100,effort:6}).toBuffer(),hash=crypto.createHash('sha256').update(encoded).digest('hex').slice(0,12),src='assets/walk/player-skeletal-'+hash+'.webp';
 fs.writeFileSync(path.join(pub,src),encoded);
 fs.writeFileSync(path.join(root,'design/LinJian-skeletal-atlas.png'),png);
 const file={src,bytes:encoded.length,type:'image/webp'},walker={width:frameWidth*columns,height:frameHeight*rows,columns,rows,frameWidth,frameHeight,idleFrame:0,walkFrames:Array.from({length:32},(_,i)=>i+1),cycleDistance:report.cycleDistance,cycleSeconds:report.cycleSeconds,groundBaseline:report.groundBaseline,bobAmount:0,source:report.source,action:report.action,avif:file,webp:file,thumb:file};
 const window={MistArt:{}};vm.runInNewContext(fs.readFileSync(path.join(pub,'mist-walk-art.js'),'utf8'),{window});window.MistArt.walker=walker;
 fs.writeFileSync(path.join(pub,'mist-walk-art.js'),'(function(root){Object.assign(root.MistArt,'+JSON.stringify(window.MistArt)+');})(window);\n');
 fs.copyFileSync(path.join(root,'output/skeletal-walk-model.json'),path.join(pub,'docs/mist/skeletal-walk-model.json'));
 fs.writeFileSync(path.join(root,'output/skeletal-walk-manifest.json'),JSON.stringify(walker,null,2));
 const gifFrames=await Promise.all(frames.slice(1).map(input=>sharp(input).resize(192,288).raw().toBuffer()));
 await sharp(Buffer.concat(gifFrames),{raw:{width:192,height:288*32,channels:4,pageHeight:288}}).gif({loop:0,delay:Math.round(report.cycleSeconds*1000/32)}).toFile(path.join(root,'output/V1.0.2-骨骼步态预览.gif'));
 console.log(JSON.stringify({src,bytes:encoded.length,frames:32,cycleDistance:report.cycleDistance,speed:report.recommendedSpeed}));
})().catch(e=>{console.error(e);process.exitCode=1});

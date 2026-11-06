const fs=require('node:fs'),path=require('node:path');
const sharp=require('sharp');
(async()=>{
 const root=path.resolve(__dirname,'..'),fontfile=path.join(root,'unity/GuanshanYouyan2D/Assets/GuanshanYouyan/Fonts/Ending/FogPosterSerif.otf');
 const text=fs.readFileSync(path.join(root,'output/结尾海报文案-用户原文.txt'),'utf8').trim().split(/\r?\n/);
 if(text.length!==19)throw Error('Expected exactly nineteen user-supplied lines');
 const lengths=[5,2,4,4,2,2],xs=[362,240,560,236,548,362],ys=[346,642,910,1202,1628,1808];
 const entries=[{text:'关山有眼',x:370,y:78,size:70,color:'#edc16e'}];let at=0;
 for(let g=0;g<lengths.length;g++)for(let i=0;i<lengths[g];i++)entries.push({text:text[at++],x:xs[g],y:ys[g]+i*35,size:18,color:g===5?'#142637':'#faedc2'});
 const layers=[];for(const e of entries){const {data,info}=await sharp({text:{text:`<span foreground="${e.color}">${e.text}</span>`,font:`Fog Poster Serif ${e.size*2}`,fontfile,rgba:true,dpi:72}}).png().toBuffer({resolveWithObject:true});layers.push({input:data,left:Math.round(e.x*2-info.width/2),top:Math.round(e.y*2-info.height/2)});}
 const target=path.join(root,'output/关山有眼-结尾海报-校对版.png');
 await sharp(path.join(root,'unity/GuanshanYouyan2D/Assets/GuanshanYouyan/Resources/Ending/SuccessPosterClean.png')).resize(1448,4344).composite(layers).png().toFile(target);
 console.log(JSON.stringify({target,width:1448,height:4344,bodyLines:19,bodyFontSize:36,titleFontSize:140,artwork:'original painting retained',font:'OFL Noto Serif CJK subset'}));
})().catch(e=>{console.error(e);process.exitCode=1;});

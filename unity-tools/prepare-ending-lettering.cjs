// The supplied illustration is immutable outside the explicitly edited title area.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const sharp=require('sharp');
(async()=>{
 const original=process.argv[2],removedTitle=process.argv[3];if(!original||!removedTitle)throw Error('Pass supplied clean poster and title-removal image');
 const a=await sharp(original).metadata(),b=await sharp(removedTitle).metadata();if(a.width!==724||a.height!==2172||b.width!==724||b.height!==2172)throw Error('Unexpected art dimensions');
 const region={left:200,top:10,width:350,height:140};const patch=await sharp(removedTitle).extract(region).png().toBuffer();
 const target=path.resolve(__dirname,'../unity/GuanshanYouyan2D/Assets/GuanshanYouyan/Resources/Ending/SuccessPosterClean.png');
 await sharp(original).composite([{input:patch,left:region.left,top:region.top}]).png().toFile(target);
 const rawA=await sharp(original).removeAlpha().raw().toBuffer(),rawB=await sharp(target).removeAlpha().raw().toBuffer();let protectedChanges=0;
 for(let y=0;y<a.height;y++)for(let x=0;x<a.width;x++)if(x<region.left||x>=region.left+region.width||y<region.top||y>=region.top+region.height){let i=(y*a.width+x)*3;if(rawA[i]!==rawB[i]||rawA[i+1]!==rawB[i+1]||rawA[i+2]!==rawB[i+2])protectedChanges++;}
 if(protectedChanges)throw Error('Protected illustration changed');
 const report={supplied:original,output:target,width:a.width,height:a.height,editRegion:region,protectedPixelChanges:protectedChanges,sha256:crypto.createHash('sha256').update(fs.readFileSync(target)).digest('hex')};
 fs.writeFileSync(path.resolve(__dirname,'../output/ending-illustration-preservation.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report));
})().catch(e=>{console.error(e);process.exitCode=1;});

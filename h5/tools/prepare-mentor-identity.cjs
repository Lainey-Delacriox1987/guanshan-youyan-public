/* Keep Xu Lan's dialogue, field and dossier art on one approved identity. */
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const sharp=require('sharp');
const root=path.resolve(__dirname,'../..'),dir=path.join(root,'h5/public/assets/walk');
async function main(){
 const input=process.argv[2];if(!input)throw Error('Pass the transparent original-identity cutout');
 const original=await sharp(input).metadata();if(!original.hasAlpha)throw Error('Transparent cutout required');
 const full=await sharp(input).trim().resize({height:1400,withoutEnlargement:true}).png().toBuffer();
 fs.writeFileSync(path.join(root,'h5/design/motion-reference/XuLan-original-identity.png'),full);
 const meta=await sharp(full).metadata();
 const save=async(name,buf,type)=>{const hash=crypto.createHash('sha256').update(buf).digest('hex').slice(0,12),src=`assets/walk/${name}-${hash}.${type}`;fs.writeFileSync(path.join(root,'h5/public',src),buf);return {src,bytes:buf.length,type:'image/'+type};};
 const avif=await save('mentor-original',await sharp(full).avif({quality:85,chromaSubsampling:'4:4:4',effort:6}).toBuffer(),'avif');
 const webp=await save('mentor-original',await sharp(full).webp({quality:95,alphaQuality:100,effort:6}).toBuffer(),'webp');
 const thumb=await save('mentor-portrait',await sharp(full).extract({left:Math.round(meta.width*.14),top:0,width:Math.round(meta.width*.72),height:Math.round(meta.height*.38)}).resize({width:420}).webp({quality:95,alphaQuality:100}).toBuffer(),'webp');
 const file=path.join(root,'h5/public/mist-walk-art.js'),window={MistArt:{}};require('node:vm').runInNewContext(fs.readFileSync(file,'utf8'),{window});
 window.MistArt.mentor={width:meta.width,height:meta.height,avif,webp,thumb};
 fs.writeFileSync(file,'(function(root){Object.assign(root.MistArt,'+JSON.stringify(window.MistArt)+');})(window);\n');
 console.log(JSON.stringify({mentor:window.MistArt.mentor}));
}
main().catch(e=>{console.error(e);process.exitCode=1;});

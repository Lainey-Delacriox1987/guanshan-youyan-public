import {cpSync,mkdirSync,rmSync} from 'node:fs';
import path from 'node:path';
const output=path.resolve('dist');if(path.dirname(output)!==process.cwd()||path.basename(output)!=='dist')throw Error('Unsafe build output path');
rmSync(output,{recursive:true,force:true});mkdirSync(output,{recursive:true});cpSync('public',output,{recursive:true});console.log('Built static H5 → '+output);

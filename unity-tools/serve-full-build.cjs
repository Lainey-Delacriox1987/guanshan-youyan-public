// Local static server for exported Unity Web builds. No file writes or directory traversal.
const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(process.argv[2] || 'unity/GuanshanYouyan2D/Builds/FullStory-web-release');
const port = Number(process.argv[3] || 18743);
const prefix = '/guanshan-youyan/';
const siteId = 'guanshan-youyan-unity-full';
if (!fs.existsSync(path.join(root,'index.html')) || !fs.existsSync(path.join(root,'Build'))) {
  console.error('Missing exported Unity Web build: '+root); process.exit(1);
}
const types = {'.html':'text/html; charset=utf-8','.js':'text/javascript; charset=utf-8','.wasm':'application/wasm','.data':'application/octet-stream','.json':'application/json','.png':'image/png','.jpg':'image/jpeg','.jpeg':'image/jpeg','.webp':'image/webp','.avif':'image/avif','.css':'text/css; charset=utf-8','.txt':'text/plain; charset=utf-8','.md':'text/plain; charset=utf-8'};
http.createServer((req,res)=>{
  let pathname;
  try {pathname=decodeURIComponent(new URL(req.url,'http://localhost').pathname);} catch {res.writeHead(400).end();return;}
  res.setHeader('X-Local-Site',siteId);
  if(pathname==='/'){res.writeHead(302,{Location:prefix}).end();return;}
  if(pathname===prefix+'__site.json'){
    res.writeHead(200,{'Content-Type':'application/json; charset=utf-8','Cache-Control':'no-store'});
    res.end(JSON.stringify({siteId,serverVersion:2,title:'关山有眼·雾港来信 Unity 整版',root,url:'http://127.0.0.1:'+port+prefix}));return;
  }
  if(!pathname.startsWith(prefix)){res.writeHead(404).end('This server hosts Guanshan Youyan at '+prefix);return;}
  pathname=pathname.slice(prefix.length-1);
  const file=path.resolve(root,'.'+pathname,pathname.endsWith('/')?'index.html':'');
  if(file!==root&&!file.startsWith(root+path.sep)){res.writeHead(403).end();return;}
  if(!fs.existsSync(file)||!fs.statSync(file).isFile()){res.writeHead(404).end('Not found');return;}
  const encoding=file.endsWith('.gz')?'gzip':file.endsWith('.br')?'br':null;
  res.setHeader('Content-Type',types[path.extname(encoding?file.slice(0,-3):file)]||'application/octet-stream');
  if(encoding)res.setHeader('Content-Encoding',encoding);
  const version=new URL(req.url,'http://localhost').searchParams.get('v');
  const immutable=(pathname.startsWith('/Build/')&&/^[a-f0-9]{12,64}$/.test(version||''))||/\/cover\.[a-f0-9]+\.webp$/.test(pathname);
  res.setHeader('Cache-Control',immutable?'public, max-age=31536000, immutable':'no-cache');
  res.setHeader('X-Content-Type-Options','nosniff');
  const stat=fs.statSync(file),etag='W/"'+stat.size.toString(16)+'-'+Math.floor(stat.mtimeMs).toString(16)+'"';
  res.setHeader('Content-Length',stat.size);res.setHeader('ETag',etag);
  if(req.headers['if-none-match']===etag){res.writeHead(304).end();return;}
  if(req.method==='HEAD'){res.writeHead(200).end();return;}
  fs.createReadStream(file).pipe(res);
}).on('error',error=>{console.error(error.code+': '+error.message);process.exitCode=1;})
  .listen(port,'127.0.0.1',()=>console.log(JSON.stringify({siteId,root,url:'http://127.0.0.1:'+port+prefix,localOnly:true})));

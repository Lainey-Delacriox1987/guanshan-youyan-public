const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const source=path.join(__dirname,'landscape-only.js');
function install(folder){
  const target=path.join(folder,'index.html');let html=fs.readFileSync(target,'utf8');
  const bytes=fs.readFileSync(source),version=crypto.createHash('sha256').update(bytes).digest('hex').slice(0,12);
  if(!/<meta[^>]+name=["']viewport["']/i.test(html))html=html.replace('</head>','<meta name="viewport" content="width=device-width,initial-scale=1,viewport-fit=cover"></head>');
  if(!html.includes('landscape-only.js')){
    if(!html.includes('createUnityInstance(canvas, config,')||!html.includes('window.fogGameInstance=unityInstance;'))throw Error('Unknown Unity template: '+target);
    html=html.replace('</head>','<script src="landscape-only.js?v='+version+'"></script></head>')
      .replace('createUnityInstance(canvas, config,','window.FogLandscape.start(createUnityInstance, canvas, config,')
      .replace('window.fogGameInstance=unityInstance;','window.fogGameInstance=unityInstance;window.FogLandscape.attach(unityInstance);');
  }else html=html.replace(/landscape-only\.js(?:\?v=[a-f0-9]+)?/,'landscape-only.js?v='+version);
  fs.copyFileSync(source,path.join(folder,'landscape-only.js'));fs.writeFileSync(target,html);
  return {folder,landscapeOnly:true,version};
}
module.exports=install;
if(require.main===module)for(const folder of process.argv.slice(2))console.log(JSON.stringify(install(path.resolve(folder))));

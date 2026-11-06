using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GuanshanYouyan.Editor
{
    public static class FogBuild
    {
        static string request;
        [InitializeOnLoadMethod]
        static void Resume()
        {
            const string file="ImportReports/build-full.request";if(!File.Exists(file))return;
            request=File.ReadAllLines(file).FirstOrDefault(s=>!string.IsNullOrWhiteSpace(s))?.Trim();
            if(!new[]{"windows-dev","windows-release","web-dev","web-release"}.Contains(request))throw new Exception("Invalid full-build request");
            EditorApplication.update+=Tick;
        }
        static void Tick()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            EditorApplication.update-=Tick;
            // A build-target switch reloads the domain. Keep the request until the target is ready.
            var target=request.StartsWith("web")?BuildTarget.WebGL:BuildTarget.StandaloneWindows64;
            if(EditorUserBuildSettings.activeBuildTarget!=target)
            {EditorUserBuildSettings.SwitchActiveBuildTarget(target==BuildTarget.WebGL?BuildTargetGroup.WebGL:BuildTargetGroup.Standalone,target);EditorApplication.update+=Tick;return;}
            var remaining=File.ReadAllLines("ImportReports/build-full.request").Where(s=>!string.IsNullOrWhiteSpace(s)).Skip(1).ToArray();
            if(remaining.Length==0)File.Delete("ImportReports/build-full.request");else File.WriteAllLines("ImportReports/build-full.request",remaining);
            Build(request);Resume();
        }
        public static void Build(string mode)
        {
            string resultFile="ImportReports/full-build-"+mode+".json";
            try
            {
                if(EditorApplication.isPlaying)throw new Exception("Stop Play before building");
                bool web=mode.StartsWith("web"),dev=mode.EndsWith("dev");var target=web?BuildTarget.WebGL:BuildTarget.StandaloneWindows64;
                string folder="Builds/FullStory-"+mode;Directory.CreateDirectory(folder);
                PlayerSettings.productName="关山有眼·雾港来信";PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;
                if(web)
                {
                    FogWebOptimizer.ApplySettings(dev);FogWebOptimizer.ApplyAssets();FogWebOptimizer.BuildFontAndBind();
                }
                FogPrologueFont.Build();AssetDatabase.SaveAssets();
                var sourcePaths=Directory.GetFiles("Assets/GuanshanYouyan/Scripts/Story","*.cs").Concat(new[]{"Assets/GuanshanYouyan/Data/StoryV2.json","Assets/GuanshanYouyan/Plugins/WebGL/FogVerification.jslib"}).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
                var sourceSha256=Hash(Encoding.UTF8.GetBytes(string.Join("\n",sourcePaths.Select(p=>p+"="+Hash(File.ReadAllBytes(p))))));
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{FogStoryBuilder.ScenePath},target=target,locationPathName=web?folder:folder+"/雾港来信.exe",options=BuildOptions.DetailedBuildReport|(dev?BuildOptions.Development:BuildOptions.None)});
                var result=new{success=report.summary.result==BuildResult.Succeeded,mode,edition="offline-fixed-topics",sourceSha256,shuttleSha256=Hash(File.ReadAllBytes("Assets/GuanshanYouyan/Art/FullStoryV2/Shuttle.png")),result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,size=report.summary.totalSize,seconds=report.summary.totalTime.TotalSeconds,output=Path.GetFullPath(folder),date=DateTime.UtcNow.ToString("o")};
                File.WriteAllText(resultFile,Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
                if(!result.success)throw new Exception("Build did not succeed");
                File.Copy("Assets/GuanshanYouyan/Fonts/OFL.txt",folder+"/Noto-OFL.txt",true);
                File.Copy("Assets/GuanshanYouyan/Fonts/LongCang/OFL.txt",folder+"/LongCang-OFL.txt",true);
                File.Copy("Assets/GuanshanYouyan/Documentation/背景音乐来源.md",folder+"/背景音乐来源.md",true);
                File.WriteAllText(folder+"/开始游玩.txt","关山有眼·雾港来信｜四幕整版\n"+
                    (web?"使用HTTP服务器打开index.html。发布服务器需对.gz资源发送Content-Encoding:gzip并对wasm发送application/wasm。\n":"运行雾港来信.exe；保留同目录Data文件夹、DLL和其他依赖。\n")+
                    "点击场景空白处行走，点击人物、物件或出口直接交互，无需走近。入场提示1.8秒自动消失且不挡操作。A/D或方向键移动，E调查，Tab手册，Esc收起。\n"+
                    "四幕八处场景；线索板和生态支线可选；手册可开关两首叠放音乐。第一首为120秒。\n"+
                    "本版为离线固定话题整版：许岚提供固定核查话题；没有自由输入入口，不探测或连接模型服务，无需模型和密钥。\n"+
                    "存档独立于原H5及第一幕试玩。浏览器存档与地址和浏览器资料绑定，清理网站数据会清除进度。\n"+
                    "本包是本地试制，未完成实体手机或陌生玩家体验验收。字体和音乐来源见随包许可文件。\n");
                if(web)
                {
                    File.Copy("../../unity-tools/serve-full-build.cjs",folder+"/Serve-Web.cjs",true);
                    File.AppendAllText(folder+"/开始游玩.txt","\n本机试玩：安装Node.js后，在本解压目录运行 node Serve-Web.cjs . 18743，然后打开 http://127.0.0.1:18743/guanshan-youyan/ 。服务器只监听本机，终端关闭即停止。完整工程可双击根目录的“启动雾港来信本地试玩.cmd”，独立后台服务不随终端关闭而退出。\n");
                    var index=folder+"/index.html";var html=File.ReadAllText(index);
                    var version=Hash(Encoding.UTF8.GetBytes(string.Join("\n",Directory.GetFiles(folder+"/Build").OrderBy(p=>p,StringComparer.Ordinal).Select(p=>Hash(File.ReadAllBytes(p)))))).Substring(0,16);
                    foreach(var resource in new[]{".data.gz",".framework.js.gz",".wasm.gz"})html=html.Replace(resource+"\"",resource+"?v="+version+"\"");
                    var cover=new Texture2D(2,2);ImageConversion.LoadImage(cover,File.ReadAllBytes("Assets/GuanshanYouyan/Art/FullStoryV2/Cover.png"));
                    File.WriteAllBytes(folder+"/TemplateData/cover.jpg",cover.EncodeToJPG(88));UnityEngine.Object.DestroyImmediate(cover);
                    if(File.Exists(folder+"/TemplateData/cover.png"))File.Delete(folder+"/TemplateData/cover.png");
                    html=html.Replace("</head>","<style>#unity-loading-bar{position:fixed!important;inset:0!important;transform:none!important;width:100%!important;height:100%!important;background:#0a1621 url('TemplateData/cover.jpg') center/cover no-repeat}#unity-logo,#unity-progress-bar-empty,#unity-progress-bar-full{display:none!important}</style></head>");
                    File.Copy("../../unity-tools/web-performance.js",folder+"/web-performance.js",true);
                    html=html.Replace("</head>","<script src=\"web-performance.js?v="+version+"\"></script></head>");
                    html=html.Replace("// config.matchWebGLToCanvasSize = false;","window.FogWebPerformance.configure(canvas,config);");
                    html=html.Replace("var canvas = document.querySelector(\"#unity-canvas\");","var canvas = document.querySelector(\"#unity-canvas\");\ncanvas.tabIndex=0;\nwindow.addEventListener('pointerdown', function(){canvas.focus();}, {once:true});");
                    html=html.Replace("}).then((unityInstance) => {","}).then((unityInstance) => {\nwindow.fogGameInstance=unityInstance;");
                    // Landscape is mandatory; defer startup in portrait and pause on rotation.
                    File.Copy("../../unity-tools/landscape-only.js",folder+"/landscape-only.js",true);
                    html=html.Replace("</head>","<script src=\"landscape-only.js\"></script></head>");
                    html=html.Replace("createUnityInstance(canvas, config,","window.FogLandscape.start(createUnityInstance, canvas, config,");
                    html=html.Replace("window.fogGameInstance=unityInstance;","window.fogGameInstance=unityInstance;window.FogLandscape.attach(unityInstance);");
                    html=html.Replace("</head>","<style>html,body{margin:0;width:100%;height:100%;overflow:hidden;background:#13212b}#unity-container{position:fixed!important;inset:0!important;transform:none!important;width:100%!important;height:100%!important}#unity-canvas{width:100%!important;height:100%!important;display:block}#unity-footer{display:none}#unity-loading-bar{z-index:5}html[data-fog-intro='1'] #fog-fullscreen{display:none!important}</style></head>");
                    html=html.Replace("</body>","<button id='fog-fullscreen' type='button' style='position:fixed;right:12px;bottom:3px;height:26px;z-index:20;border:1px solid #8c9b9d;border-radius:4px;padding:2px 12px;background:#182a33e8;color:#e8dfc7;font:14px sans-serif;cursor:pointer'>全屏游玩</button><script>const fsButton=document.getElementById('fog-fullscreen');['pointerdown','mousedown','touchstart'].forEach(name=>fsButton.addEventListener(name,event=>event.stopPropagation()));fsButton.addEventListener('click',()=>{if(document.fullscreenElement){document.exitFullscreen();}else{document.documentElement.requestFullscreen().catch(()=>{fsButton.textContent='请按 F11 全屏';});}});document.addEventListener('fullscreenchange',()=>{fsButton.textContent=document.fullscreenElement?'退出全屏':'全屏游玩';});</script></body>");
                    File.Copy("../../unity-tools/startup-cover.js",folder+"/startup-cover.js",true);
                    const string hideLoading=@"document.querySelector(""#unity-loading-bar"").style.display = ""none"";";
                    if(!html.Contains(hideLoading))throw new Exception("Unknown Unity loading-cover template");
                    html=html.Replace(hideLoading,"window.FogStartupCover.runtimeReady();");
                    html=html.Replace("</head>","<style id='fog-startup-cover'>html,body{background:#0a1621 url('TemplateData/cover.jpg') center/cover no-repeat}#unity-loading-bar{display:block!important}html:not([data-fog-cover-ready='1']) #unity-canvas{opacity:0}html:not([data-fog-cover-ready='1']) #fog-fullscreen{visibility:hidden}html[data-fog-cover-ready='1'] #unity-loading-bar{display:none!important}</style><script src='startup-cover.js?v="+version+"'></script></head>");
                    File.WriteAllText(index,html);
                }
            }
            catch(Exception e){File.WriteAllText(resultFile+".error.txt",e.ToString());Debug.LogException(e);}
        }
        static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
    }
}

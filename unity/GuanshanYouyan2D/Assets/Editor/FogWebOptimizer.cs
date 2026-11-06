using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using GuanshanYouyan.Story;

namespace GuanshanYouyan.Editor
{
    // Original image/audio files and save schema remain untouched. Import settings
    // and a separate, pre-baked font are reproducible for every release build.
    public static class FogWebOptimizer
    {
        const string Root="Assets/GuanshanYouyan";
        public const string FontPath=Root+"/Fonts/NotoSansCJKsc-WebStatic.asset";
        [MenuItem("关山有眼/应用网页性能优化")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before optimizing assets");
            ApplySettings(false);ApplyAssets();BuildFontAndBind();AssetDatabase.SaveAssets();
        }
        public static void ApplySettings(bool development)
        {
            var target=NamedBuildTarget.WebGL;
            PlayerSettings.WebGL.compressionFormat=development?WebGLCompressionFormat.Disabled:WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback=false;PlayerSettings.WebGL.dataCaching=true;
            PlayerSettings.WebGL.initialMemorySize=128;PlayerSettings.WebGL.maximumMemorySize=1024;
            PlayerSettings.WebGL.memoryGrowthMode=WebGLMemoryGrowthMode.Geometric;
            PlayerSettings.WebGL.exceptionSupport=WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.debugSymbolMode=WebGLDebugSymbolMode.Off;
            PlayerSettings.stripEngineCode=true;PlayerSettings.stripUnusedMeshComponents=true;
            PlayerSettings.SetManagedStrippingLevel(target,development?ManagedStrippingLevel.Low:ManagedStrippingLevel.High);
            PlayerSettings.SetIl2CppCodeGeneration(target,Il2CppCodeGeneration.OptimizeSize);
            UnityEditor.WebGL.UserBuildSettings.codeOptimization=development?UnityEditor.WebGL.WasmCodeOptimization.BuildTimes:UnityEditor.WebGL.WasmCodeOptimization.DiskSizeLTO;
            QualitySettings.vSyncCount=0;
            AssetDatabase.SaveAssets();
        }
        static string[] AssetPaths()=>AssetDatabase.GetDependencies(FogStoryBuilder.ScenePath,true)
            .Concat(AssetDatabase.FindAssets("",new[]{Root+"/Resources"}).Select(AssetDatabase.GUIDToAssetPath)).Distinct().ToArray();
        public static void ApplyAssets()
        {
            var changes=new System.Collections.Generic.List<object>();
            foreach(string path in AssetPaths())
            {
                if(!path.StartsWith(Root+"/",StringComparison.Ordinal))continue;
                var texture=AssetImporter.GetAtPath(path) as TextureImporter;
                if(texture!=null)
                {
                    var settings=texture.GetPlatformTextureSettings("WebGL");
                    bool alpha=texture.DoesSourceTextureHaveAlpha();
                    // BC3 preserves alpha on cast/vehicle atlases; BC1 suits opaque art.
                    // Keep original dimensions: runtime sprite rectangles use source pixels.
                    // This poster contains small baked lettering. Preserve it without
                    // block-compression artifacts; its source resolution is retained.
                    var format=path.EndsWith("/Ending/SuccessPoster.png",StringComparison.Ordinal)||path.EndsWith("/Ending/SuccessPosterClean.png",StringComparison.Ordinal)?TextureImporterFormat.RGB24:alpha?TextureImporterFormat.DXT5:TextureImporterFormat.DXT1;
                    bool changed=texture.isReadable||texture.mipmapEnabled||!settings.overridden||settings.format!=format||settings.maxTextureSize!=4096;
                    texture.isReadable=false;texture.mipmapEnabled=false;
                    settings.name="WebGL";settings.overridden=true;settings.maxTextureSize=4096;
                    settings.format=format;settings.compressionQuality=100;settings.crunchedCompression=false;
                    texture.SetPlatformTextureSettings(settings);
                    if(changed)texture.SaveAndReimport();
                    var imported=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    changes.Add(new{path,width=imported.width,height=imported.height,format=imported.format.ToString(),readable=texture.isReadable,bytes=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(imported)});
                }
                var audio=AssetImporter.GetAtPath(path) as AudioImporter;
                if(audio!=null)
                {
                    // Preserve stereo positioning; music is already compressed/streamed.
                    var settings=audio.defaultSampleSettings;
                    if(settings.quality>.5f){settings.quality=.5f;audio.defaultSampleSettings=settings;audio.SaveAndReimport();}
                }
            }
            // The superseded button was implicitly included by Resources although unused.
            const string old=Root+"/Resources/UI/NotebookButton.png";
            if(File.Exists(old))
            {
                const string archive=Root+"/Art/UnusedUI";Directory.CreateDirectory(archive);AssetDatabase.Refresh();
                string error=AssetDatabase.MoveAsset(old,archive+"/NotebookButton.png");if(error!="")throw new Exception(error);
            }
            File.WriteAllText("ImportReports/performance-optimized-textures.json",Newtonsoft.Json.JsonConvert.SerializeObject(changes,Newtonsoft.Json.Formatting.Indented));
            AssetDatabase.SaveAssets();
        }
        public static void BuildFontAndBind()
        {
            string corpus=File.ReadAllText(Root+"/Data/StoryV2.json")+string.Concat(Directory.GetFiles(Root+"/Scripts/Story","*.cs").Select(File.ReadAllText));
            corpus=new string(corpus.Concat(Enumerable.Range(32,95).Select(c=>(char)c)).Where(c=>!char.IsControl(c)&&!char.IsSurrogate(c)).Distinct().OrderBy(c=>c).ToArray());
            string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(corpus)));
            const string stamp="ImportReports/web-font-corpus.sha256";
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if(font==null||!File.Exists(stamp)||File.ReadAllText(stamp)!=hash)
            {
                var source=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/NotoSansCJKsc-Regular.otf");
                var generated=TMP_FontAsset.CreateFontAsset(source,56,7,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);
                generated.name="NotoSansCJKsc-WebStatic";generated.TryAddCharacters(corpus,out string missing);
                if(missing.Any(c=>c>=0x3400&&c<=0x9fff))throw new Exception("Missing Chinese glyphs: "+missing);
                generated.atlasPopulationMode=AtlasPopulationMode.Static;
                if(font==null){font=generated;AssetDatabase.CreateAsset(font,FontPath);}
                else
                {
                    // Preserve the font GUID referenced by the scene and rebuild subassets.
                    foreach(var child in AssetDatabase.LoadAllAssetsAtPath(FontPath))if(child!=font)UnityEngine.Object.DestroyImmediate(child,true);
                    EditorUtility.CopySerialized(generated,font);
                    // TMP_FontAsset.OnDestroy destroys its material and atlases. The
                    // rebuilt asset now owns them; detach them from the temporary font.
                    generated.material=null;generated.atlasTextures=Array.Empty<Texture2D>();
                    UnityEngine.Object.DestroyImmediate(generated);
                }
                AssetDatabase.AddObjectToAsset(font.material,font);
                foreach(var atlas in font.atlasTextures)if(atlas!=null&&!AssetDatabase.Contains(atlas))AssetDatabase.AddObjectToAsset(atlas,font);
                EditorUtility.SetDirty(font);AssetDatabase.SaveAssets();File.WriteAllText(stamp,hash);
                File.WriteAllText("ImportReports/web-font-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{characters=corpus.Length,baked=font.characterTable.Count,atlases=font.atlasTextures.Length,missing,mode=font.atlasPopulationMode.ToString()},Newtonsoft.Json.Formatting.Indented));
            }
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(FogStoryBuilder.ScenePath);bool opened=!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(FogStoryBuilder.ScenePath,OpenSceneMode.Additive);
            var game=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<FogGame>(true)).Single();
            if(game.chineseFont!=font||game.sceneCamera.allowHDR||game.sceneCamera.allowMSAA)
            {
                game.chineseFont=font;game.sceneCamera.allowHDR=false;game.sceneCamera.allowMSAA=false;
                EditorUtility.SetDirty(game);EditorUtility.SetDirty(game.sceneCamera);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            if(opened)EditorSceneManager.CloseScene(scene,true);
        }
    }
}

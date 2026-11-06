using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using GuanshanYouyan.Story;

namespace GuanshanYouyan.Editor
{
    public static class PortStoryBuilder
    {
        // A clean domain avoids temporary script-eval assemblies in InputSystem's linker scan.
        [InitializeOnLoadMethod]
        static void ResumeQueuedBuild()
        {
            const string request="ImportReports/build-port.request";
            if(!File.Exists(request))return;
            EditorApplication.update+=RunQueuedBuild;
        }
        static void RunQueuedBuild()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            EditorApplication.update-=RunQueuedBuild;
            File.Delete("ImportReports/build-port.request");BuildWindows();
        }
        const string Root="Assets/GuanshanYouyan";
        public const string ScenePath=Root+"/Scenes/PortStoryV2.unity";
        [MenuItem("关山有眼/打开第一幕新版")]
        public static void Open(){EditorSceneManager.OpenScene(ScenePath);}
        [MenuItem("关山有眼/导出第一幕 Windows 试玩包")]
        public static void BuildWindows()
        {
            try
            {
                if(EditorApplication.isPlaying)throw new Exception("Stop Play mode before building");
                Directory.CreateDirectory("Builds/PortStoryV2");
                PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},target=BuildTarget.StandaloneWindows64,locationPathName="Builds/PortStoryV2/雾港来信.exe",options=BuildOptions.None});
                var result=new {success=report.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded,result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,size=report.summary.totalSize,seconds=report.summary.totalTime.TotalSeconds};
                File.WriteAllText("ImportReports/port-windows-build.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
                if(!result.success)throw new Exception("Build failed");
                File.Copy(Root+"/Fonts/OFL.txt","Builds/PortStoryV2/Noto-OFL.txt",true);
                File.Copy(Root+"/Documentation/背景音乐来源.md","Builds/PortStoryV2/背景音乐来源.md",true);
            }
            catch(Exception e){File.WriteAllText("ImportReports/port-windows-build-error.txt",e.ToString());Debug.LogException(e);}
        }
        [MenuItem("关山有眼/制作第一幕新版")]
        public static string Build()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop Play mode before building the scene.");
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Save current scene first.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if(!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
            {
                var package=Directory.GetFiles("Library/PackageCache","TMP Essential Resources.unitypackage",SearchOption.AllDirectories).First();
                AssetDatabase.ImportPackage(package,false);
            }
            var fontPath=Root+"/Fonts/NotoSansCJKsc-Story.asset";
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if(font==null)
            {
                var source=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/NotoSansCJKsc-Regular.otf");
                font=TMP_FontAsset.CreateFontAsset(source,56,7,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);
                font.name="NotoSansCJKsc Story";AssetDatabase.CreateAsset(font,fontPath);
                font.TryAddCharacters(File.ReadAllText(Root+"/Data/PortAct1V2.json"));
                AssetDatabase.AddObjectToAsset(font.material,font);
                foreach(var atlas in font.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,font);
                EditorUtility.SetDirty(font);AssetDatabase.SaveAssets();
            }
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cameraGo=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));cameraGo.tag="MainCamera";
            var camera=cameraGo.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=4;camera.backgroundColor=new Color(.075f,.12f,.16f);camera.clearFlags=CameraClearFlags.SolidColor;camera.transform.position=new Vector3(8,0,-10);
            var root=new GameObject("雾港来信 · 第一幕");var game=root.AddComponent<PortGame>();
            game.storyData=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Data/PortAct1V2.json");game.chineseFont=font;game.sceneCamera=camera;
            game.harborArt=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/PortStoryV2/Harbor.png");
            game.officeArt=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/PortStoryV2/Inspection.png");
            game.castArt=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/PortStoryV2/Cast.png");
            game.spriteMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Settings/ResourceSpriteUnlit.mat");
            if(game.spriteMaterial==null)
            {
                var shader=Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                game.spriteMaterial=new Material(shader);AssetDatabase.CreateAsset(game.spriteMaterial,Root+"/Settings/PortSpriteUnlit.mat");
            }
            var music=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/BackgroundMusic.prefab");
            if(music==null)throw new Exception("Missing existing BGM prefab");PrefabUtility.InstantiatePrefab(music,scene);
            PlayerSettings.runInBackground=true;
            EditorSceneManager.SaveScene(scene,ScenePath);
            var other=EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath).ToArray();
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)}.Concat(other).ToArray();
            AssetDatabase.SaveAssets();
            return "Created "+ScenePath+"; original scenes and music prefab retained.";
        }
    }
}

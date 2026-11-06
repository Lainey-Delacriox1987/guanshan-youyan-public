using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GuanshanYouyan.Story;

namespace GuanshanYouyan.Editor
{
    public static class FogStoryBuilder
    {
        const string Root="Assets/GuanshanYouyan";
        public const string ScenePath=Root+"/Scenes/FogHarborStoryV2.unity";
        [MenuItem("关山有眼/打开完整故事")]
        public static void Open()=>EditorSceneManager.OpenScene(ScenePath);
        [MenuItem("关山有眼/制作完整故事场景")]
        public static string BuildScene()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop Play before building scene");
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Save current scene first");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FogWebOptimizer.FontPath)??AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"/Fonts/NotoSansCJKsc-Story.asset");
            if(font==null)throw new Exception("Missing existing Chinese font");
            if(font.atlasPopulationMode!=AtlasPopulationMode.Static)font.TryAddCharacters(File.ReadAllText(Root+"/Data/StoryV2.json"));
            foreach(var texture in font.atlasTextures)if(!AssetDatabase.Contains(texture))AssetDatabase.AddObjectToAsset(texture,font);
            EditorUtility.SetDirty(font);AssetDatabase.SaveAssets();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cameraGo=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));cameraGo.tag="MainCamera";
            var camera=cameraGo.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=4;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.075f,.12f,.16f);camera.transform.position=new Vector3(8,0,-10);
            var game=new GameObject("雾港来信 · 完整故事").AddComponent<FogGame>();game.storyData=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Data/StoryV2.json");game.chineseFont=font;game.sceneCamera=camera;
            game.harborArt=Sprite(Root+"/Art/PortStoryV2/Harbor.png");game.officeArt=Sprite(Root+"/Art/PortStoryV2/Inspection.png");game.castArt=Sprite(Root+"/Art/PortStoryV2/Cast.png");
            game.otherCastArt=Sprite(Root+"/Art/FullStoryV2/People.png");game.objectArt=Sprite(Root+"/Art/FullStoryV2/Objects.png");
            game.shuttleArt=Sprite(Root+"/Art/FullStoryV2/Shuttle.png");
            game.coverArt=Sprite(Root+"/Art/FullStoryV2/Cover.png");
            game.roomArts=new[]{game.harborArt,game.officeArt}.Concat(new[]{"Warehouse","Dispatch","Town","Records","Interview","Archive"}.Select(n=>Sprite(Root+"/Art/FullStoryV2/"+n+".png"))).ToArray();
            game.spriteMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Settings/ResourceSpriteUnlit.mat");if(game.spriteMaterial==null)game.spriteMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Settings/PortSpriteUnlit.mat");
            if(game.spriteMaterial==null)throw new Exception("Missing sprite material");
            var music=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/BackgroundMusic.prefab");PrefabUtility.InstantiatePrefab(music,scene);
            PlayerSettings.runInBackground=true;EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)}.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath)).ToArray();AssetDatabase.SaveAssets();
            return "Full story scene saved; 8 backgrounds, 10 cast sprites, 6 props and existing two-track BGM bound.";
        }
        static Sprite Sprite(string path){var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(sprite==null)throw new Exception("Missing art: "+path);return sprite;}
    }
}

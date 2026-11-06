using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace GuanshanYouyan.Editor
{
    [Serializable] public class ResourceEntry
    {
        public string source, destination, category, sha256;
        public int width, height, frameIndex;
    }
    [Serializable] public class ActiveEntry { public string key, destination; }
    [Serializable] public class ResourceManifest
    {
        public ResourceEntry[] assets;
        public ActiveEntry[] activeEntries;
    }

    public sealed class ResourceTextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/GuanshanYouyan/Art/", StringComparison.Ordinal)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 8192;
            if (assetPath.Contains("/Characters/LinJian/"))
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(0.5f, 0.05f);
                importer.SetTextureSettings(settings);
            }
        }
    }

    public static class ResourceSetup
    {
        public const string Root = "Assets/GuanshanYouyan";
        public const string ManifestPath = Root + "/Data/resource-manifest.json";
        public static ResourceManifest LoadManifest() => JsonUtility.FromJson<ResourceManifest>(File.ReadAllText(ManifestPath));

        [MenuItem("关山有眼/校验资源库")]
        public static void VerifyFromMenu() => Verify();

        public static string Verify()
        {
            var manifest = LoadManifest();
            int sprites = 0, models = 0, music = 0;
            foreach (var entry in manifest.assets)
            {
                if (!File.Exists(entry.destination)) throw new Exception("Missing resource: " + entry.destination);
                if (entry.width > 0)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(entry.destination);
                    if (sprite == null) throw new Exception("Sprite import failed: " + entry.destination);
                    if (sprite.texture.width != entry.width || sprite.texture.height != entry.height)
                        throw new Exception("Texture resolution changed: " + entry.destination);
                    sprites++;
                }
                else if (entry.destination.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                {
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(entry.destination) == null) throw new Exception("Model import failed");
                    models++;
                }
                else if (entry.destination.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) || entry.destination.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(entry.destination);
                    if (clip == null || clip.samples <= 0 || clip.length <= 0)
                        throw new Exception("Audio import failed: " + entry.destination);
                    music++;
                }
            }
            var result = $"Verified {manifest.assets.Length} resources: {sprites} full-resolution sprites, {models} model(s), {music} audio clip(s).";
            Directory.CreateDirectory("ImportReports");
            File.WriteAllText("ImportReports/unity-import-validation.json", JsonUtility.ToJson(new Validation { success = true, resources = manifest.assets.Length, sprites = sprites, models = models, music = music, editorVersion = Application.unityVersion, verifiedAt = DateTime.UtcNow.ToString("o") }, true));
            Debug.Log(result);
            return result;
        }

        [Serializable] class Validation { public bool success; public int resources, sprites, models, music; public string editorVersion, verifiedAt; }

        public static string CreatePreview()
        {
            var scenePath = Root + "/Scenes/ResourcePreview.unity";
            if (File.Exists(scenePath)) throw new Exception("Preview already exists; open the saved scene instead.");
            Directory.CreateDirectory(Root + "/Scenes");
            Directory.CreateDirectory(Root + "/Settings");
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6.4f;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.085f, 0.105f);
            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            cameraData.volumeLayerMask = 1;
            cameraObject.AddComponent<AudioListener>();
            var pipeline = UniversalRenderPipeline.asset;
            if (pipeline == null) throw new Exception("URP is not active.");
            pipeline.supportsHDR = true;
            EditorUtility.SetDirty(pipeline);
            var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, Root + "/Settings/ResourcePreviewVolume.asset");
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.value = TonemappingMode.ACES;
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.value = 0.9f;
            bloom.intensity.value = 0.5f;
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.value = 0.15f;
            var volume = new GameObject("Global Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) throw new Exception("URP sprite shader missing.");
            var material = new Material(shader);
            AssetDatabase.CreateAsset(material, Root + "/Settings/ResourceSpriteUnlit.mat");
            var manifest = LoadManifest();
            string Active(string key) => manifest.activeEntries.First(a => a.key == key).destination;
            SpriteRenderer Place(string name, string file, Vector3 position, float scale, int order)
            {
                var obj = new GameObject(name);
                obj.transform.position = position;
                obj.transform.localScale = Vector3.one * scale;
                var renderer = obj.AddComponent<SpriteRenderer>();
                renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(file);
                if (renderer.sprite == null) throw new Exception("Missing preview sprite: " + file);
                renderer.sharedMaterial = material;
                renderer.sortingOrder = order;
                return renderer;
            }
            Place("港口背景 · 资源预览", Active("walk-harbor"), Vector3.zero, 1f, -10);
            Place("林简 · 站姿", Root + "/Art/Characters/LinJian/Frame_00.png", new Vector3(-4, -2.8f, 0), 0.375f, 1);
            Place("许岚 · 原角色", Active("mentor"), new Vector3(-1, -1.6f, 0), 0.18f, 2);
            var qualityNames = QualitySettings.names;
            if (qualityNames.Length > 0) QualitySettings.SetQualityLevel(qualityNames.Length - 1, true);
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            PlayerSettings.productName = "关山有眼·雾港来信";
            PlayerSettings.companyName = "GuanshanYouyan";
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
            AssetDatabase.SaveAssets();
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.Frame(new Bounds(Vector3.zero, new Vector3(22, 9, 1)), false);
            return scenePath;
        }
    }

    public class ResourceLibraryWindow : EditorWindow
    {
        [MenuItem("关山有眼/资源库")]
        public static void Open() { var window = GetWindow<ResourceLibraryWindow>(); window.titleContent = new GUIContent("关山有眼资源库"); window.minSize = new Vector2(600, 400); }
        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.Add(new Label("关山有眼 · 已导入资源库"));
            var info = new Label("搜索素材后点击定位；图片可直接拖入 Scene。原始文件保存在工程的 SourceArchive~ 文件夹。");
            info.style.whiteSpace = WhiteSpace.Normal;
            root.Add(info);
            var search = new TextField("搜索");
            root.Add(search);
            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            root.Add(scroll);
            var entries = ResourceSetup.LoadManifest().assets;
            void Render(string query)
            {
                scroll.Clear();
                foreach (var entry in entries.Where(e => string.IsNullOrWhiteSpace(query) || (e.destination + e.category).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.marginBottom = 6;
                    var asset = AssetDatabase.LoadMainAssetAtPath(entry.destination);
                    var preview = new Image { image = AssetPreview.GetMiniThumbnail(asset), scaleMode = ScaleMode.ScaleToFit };
                    preview.style.width = 48;
                    preview.style.height = 48;
                    row.Add(preview);
                    var button = new Button(() => { Selection.activeObject = asset; EditorGUIUtility.PingObject(asset); }) { text = Path.GetFileName(entry.destination) + "  [" + entry.category + "]" };
                    button.style.flexGrow = 1;
                    row.Add(button);
                    scroll.Add(row);
                }
            }
            search.RegisterValueChangedCallback(evt => Render(evt.newValue));
            Render("");
        }
    }
}

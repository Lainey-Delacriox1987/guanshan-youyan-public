var paths = new[] {
    "Assets/GuanshanYouyan/Audio/Music/underwater-297537-first-120s.wav",
    "Assets/GuanshanYouyan/Audio/Music/mondamusic-mystery-mysterious-mystical-519335.mp3"
};
UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
foreach (var path in paths)
{
    var importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.AudioImporter;
    if (importer == null) throw new System.Exception("Missing AudioImporter: " + path);
    var settings = importer.defaultSampleSettings;
    settings.loadType = UnityEngine.AudioClipLoadType.Streaming;
    settings.compressionFormat = UnityEngine.AudioCompressionFormat.Vorbis;
    settings.quality = 0.7f;
    settings.sampleRateSetting = UnityEditor.AudioSampleRateSetting.PreserveSampleRate;
    settings.preloadAudioData = false;
    importer.defaultSampleSettings = settings;
    importer.forceToMono = false;
    importer.loadInBackground = true;
    importer.SaveAndReimport();
}
var clips = System.Array.ConvertAll(paths, path => UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AudioClip>(path));
foreach (var clip in clips)
    if (clip == null || clip.samples <= 0 || clip.length <= 0) throw new System.Exception("Audio did not import");
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (scene.path != "Assets/GuanshanYouyan/Scenes/ResourcePreview.unity")
    throw new System.Exception("Expected ResourcePreview scene; audio imported, scene not changed.");
var musicObject = UnityEngine.GameObject.Find("背景音乐 BGM");
if (musicObject == null)
{
    musicObject = new UnityEngine.GameObject("背景音乐 BGM");
    UnityEditor.Undo.RegisterCreatedObjectUndo(musicObject, "Add background music");
}
var sourceNames = new[] { "Underwater · 2分钟", "Mystery Mysterious Mystical" };
var volumes = new[] { 0.65f, 0.55f };
var sources = new UnityEngine.AudioSource[2];
for (int i = 0; i < clips.Length; i++)
{
    var child = musicObject.transform.Find(sourceNames[i]);
    var layer = child != null ? child.gameObject : new UnityEngine.GameObject(sourceNames[i]);
    layer.transform.SetParent(musicObject.transform, false);
    var source = layer.GetComponent<UnityEngine.AudioSource>();
    if (source == null) source = layer.AddComponent<UnityEngine.AudioSource>();
    UnityEditor.Undo.RecordObject(source, "Configure background music layer");
    source.clip = clips[i];
    source.playOnAwake = true;
    source.loop = true;
    source.volume = volumes[i];
    source.spatialBlend = 0f;
    source.dopplerLevel = 0f;
    source.mute = false;
    sources[i] = source;
}
if (System.Math.Abs(clips[0].length - 120f) > 0.001f) throw new System.Exception("Trim is not exactly 120 seconds");
if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/GuanshanYouyan/Prefabs"))
    UnityEditor.AssetDatabase.CreateFolder("Assets/GuanshanYouyan", "Prefabs");
var prefabPath = "Assets/GuanshanYouyan/Prefabs/BackgroundMusic.prefab";
UnityEditor.PrefabUtility.SaveAsPrefabAsset(musicObject, prefabPath);
var originalAsset = "Assets/GuanshanYouyan/Audio/Music/harumachimusic-underwater-mysterious-ambient-music-297537.mp3";
if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AudioClip>(originalAsset) != null)
    UnityEditor.AssetDatabase.DeleteAsset(originalAsset);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
UnityEditor.AssetDatabase.SaveAssets();
var report = new {
    success = true,
    verifiedAt = System.DateTime.UtcNow.ToString("o"),
    scene = scene.path,
    prefab = prefabPath,
    playbackMode = "simultaneous-layers",
    layers = System.Array.ConvertAll(sources, s => new { clip = s.clip.name, loop = s.loop, playOnAwake = s.playOnAwake, volume = s.volume, spatialBlend = s.spatialBlend }),
    clips = System.Array.ConvertAll(clips, clip => new { name = clip.name, seconds = clip.length, channels = clip.channels, sampleRate = clip.frequency, samples = clip.samples, loadType = clip.loadType.ToString() })
};
System.IO.File.WriteAllText("ImportReports/music-import-validation.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
return report;

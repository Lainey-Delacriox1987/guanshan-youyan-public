var sources = UnityEngine.GameObject.Find("背景音乐 BGM").GetComponentsInChildren<UnityEngine.AudioSource>();
var layers = System.Array.ConvertAll(sources, source => {
    var data = new float[2048];
    source.GetOutputData(data, 0);
    float peak = 0;
    foreach (var sample in data) peak = System.Math.Max(peak, System.Math.Abs(sample));
    if (!source.isPlaying || peak <= 0) throw new System.Exception("Layer has no audio output: " + source.clip.name);
    source.timeSamples = source.clip.samples - source.clip.frequency;
    return new { clip = source.clip.name, playing = source.isPlaying, outputPeak = peak, seekSample = source.timeSamples, samples = source.clip.samples, seconds = source.clip.length };
});
var mixed = new float[2048];
UnityEngine.AudioListener.GetOutputData(mixed, 0);
float mixedPeak = 0;
foreach (var sample in mixed) mixedPeak = System.Math.Max(mixedPeak, System.Math.Abs(sample));
var report = new { frame = UnityEngine.Time.frameCount, dspTime = UnityEngine.AudioSettings.dspTime, mixedPeak = mixedPeak, layers = layers };
System.IO.File.WriteAllText("ImportReports/music-playback-before-loop.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
return report;

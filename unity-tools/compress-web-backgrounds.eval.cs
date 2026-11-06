var names=new[]{"Archive","Dispatch","Interview","Records","Town"};
var output="ImportReports/web-background-compression";
System.IO.Directory.CreateDirectory(output);
var records=new System.Collections.Generic.List<object>();
System.Action<UnityEngine.Texture2D,string> capture=(texture,file)=>{
    var previous=UnityEngine.RenderTexture.active;var oldSrgb=UnityEngine.GL.sRGBWrite;
    var rt=UnityEngine.RenderTexture.GetTemporary(texture.width,texture.height,0,UnityEngine.RenderTextureFormat.ARGB32,UnityEngine.RenderTextureReadWrite.sRGB);
    UnityEngine.GL.sRGBWrite=UnityEngine.QualitySettings.activeColorSpace==UnityEngine.ColorSpace.Linear;
    UnityEngine.Graphics.Blit(texture,rt);UnityEngine.RenderTexture.active=rt;
    var image=new UnityEngine.Texture2D(texture.width,texture.height,UnityEngine.TextureFormat.RGBA32,false);
    image.ReadPixels(new UnityEngine.Rect(0,0,texture.width,texture.height),0,0);image.Apply();
    System.IO.File.WriteAllBytes(file,UnityEngine.ImageConversion.EncodeToPNG(image));
    UnityEngine.Object.DestroyImmediate(image);UnityEngine.RenderTexture.active=previous;UnityEngine.GL.sRGBWrite=oldSrgb;UnityEngine.RenderTexture.ReleaseTemporary(rt);
};
foreach(var name in names){
    var asset="Assets/GuanshanYouyan/Art/FullStoryV2/"+name+".png";
    var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(asset);
    var old=importer.GetPlatformTextureSettings("WebGL");
    System.IO.File.WriteAllText(output+"/"+name+".platform-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(old,Newtonsoft.Json.Formatting.Indented));
    var texture=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(asset);
    var width=texture.width;var height=texture.height;
    if(width%4!=0||height%4!=0)throw new System.Exception("Not block aligned: "+name);
    capture(texture,output+"/"+name+".before.png");
    var setting=new UnityEditor.TextureImporterPlatformSettings();
    setting.name="WebGL";setting.overridden=true;setting.maxTextureSize=importer.GetDefaultPlatformTextureSettings().maxTextureSize;
    setting.format=UnityEditor.TextureImporterFormat.DXT1Crunched;setting.textureCompression=UnityEditor.TextureImporterCompression.CompressedHQ;
    setting.crunchedCompression=true;setting.compressionQuality=100;setting.allowsAlphaSplitting=false;
    importer.SetPlatformTextureSettings(setting);importer.SaveAndReimport();
    texture=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(asset);
    if(texture.width!=width||texture.height!=height)throw new System.Exception("Resolution changed: "+name);
    capture(texture,output+"/"+name+".after.png");
    records.Add(new{name,asset,width,height,format=texture.format.ToString(),quality=100,sourcePngUnchanged=true});
    System.IO.File.WriteAllText(output+"/progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(records,Newtonsoft.Json.Formatting.Indented));
}
UnityEditor.AssetDatabase.SaveAssets();
return new {success=true,records};

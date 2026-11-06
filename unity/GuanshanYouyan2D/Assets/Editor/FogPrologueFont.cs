using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using GuanshanYouyan.Story;

namespace GuanshanYouyan.Editor
{
    public static class FogPrologueFont
    {
        public static string Build()
        {
            const string sourcePath="Assets/GuanshanYouyan/Fonts/LongCang/LongCang-Regular.ttf";
            const string target="Assets/GuanshanYouyan/Resources/Intro/LongCang-Intro.asset";
            if(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(target)!=null)return "Intro font already built";
            Directory.CreateDirectory(Path.GetDirectoryName(target));AssetDatabase.Refresh();
            var source=AssetDatabase.LoadAssetAtPath<Font>(sourcePath);if(source==null)throw new Exception("Missing Long Cang source font");
            var font=TMP_FontAsset.CreateFontAsset(source,100,10,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,false);
            font.name="LongCang-Intro";
            if(!font.TryAddCharacters(FogPrologueView.Caption,out string missing))throw new Exception("Missing intro glyphs: "+missing);
            font.atlasPopulationMode=AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(font,target);AssetDatabase.AddObjectToAsset(font.material,font);
            foreach(var texture in font.atlasTextures)AssetDatabase.AddObjectToAsset(texture,font);
            EditorUtility.SetDirty(font);AssetDatabase.SaveAssets();
            return "Static handwritten intro atlas saved with every caption glyph";
        }
    }
}

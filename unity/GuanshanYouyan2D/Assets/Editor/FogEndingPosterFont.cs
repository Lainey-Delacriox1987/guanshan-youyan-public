using System;
using System.IO;
using System.Linq;
using GuanshanYouyan.Story;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace GuanshanYouyan.Editor
{
    public static class FogEndingPosterFont
    {
        public static string Build()
        {
            const string path="Assets/GuanshanYouyan/Resources/Ending/PosterSerif.asset";
            var existing=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);if(existing!=null)return "Poster font already built";
            var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/GuanshanYouyan/Fonts/Ending/FogPosterSerif.otf");if(source==null)throw new Exception("Poster font not imported");
            string text="关山有眼"+string.Concat(FogEndingPosterText.Copy.SelectMany(group=>group));
            var font=TMP_FontAsset.CreateFontAsset(source,90,12,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,false);
            font.name="PosterSerif";if(!font.TryAddCharacters(text,out string missing))throw new Exception("Missing poster characters "+missing);
            font.atlasPopulationMode=AtlasPopulationMode.Static;AssetDatabase.CreateAsset(font,path);AssetDatabase.AddObjectToAsset(font.material,font);
            foreach(var atlas in font.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,font);EditorUtility.SetDirty(font);AssetDatabase.SaveAssets();
            return "Poster serif font baked, "+font.characterTable.Count+" characters";
        }
    }
}

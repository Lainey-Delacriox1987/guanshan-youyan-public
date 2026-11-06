using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using GuanshanYouyan.Story;

namespace GuanshanYouyan.Editor
{
    public static class FogLetterSetup
    {
        public static string Build()
        {
            const string paper="Assets/GuanshanYouyan/Resources/Letter/LetterPaper.png";
            AssetDatabase.ImportAsset(paper,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(paper);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.mipmapEnabled=false;importer.maxTextureSize=2048;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;
            importer.SaveAndReimport();
            const string target="Assets/GuanshanYouyan/Resources/Letter/LongCang-Letter.asset";
            var book=JsonUtility.FromJson<PortBook>(File.ReadAllText("Assets/GuanshanYouyan/Data/StoryV2.json"));
            var text=FogGame.LetterText(book);
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(target);
            if(font==null)
            {
                var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/GuanshanYouyan/Fonts/LongCang/LongCang-Regular.ttf");
                if(source==null)throw new Exception("Missing licensed Long Cang source font");
                font=TMP_FontAsset.CreateFontAsset(source,96,10,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,false);
                font.name="LongCang-Letter";
                if(!font.TryAddCharacters(text,out string missing))throw new Exception("Missing letter characters: "+missing);
                font.atlasPopulationMode=AtlasPopulationMode.Static;
                AssetDatabase.CreateAsset(font,target);AssetDatabase.AddObjectToAsset(font.material,font);
                foreach(var texture in font.atlasTextures)AssetDatabase.AddObjectToAsset(texture,font);
            }
            else
            {
                font.atlasPopulationMode=AtlasPopulationMode.Dynamic;
                if(!font.TryAddCharacters(text,out string missing))throw new Exception("Missing letter characters: "+missing);
                font.atlasPopulationMode=AtlasPopulationMode.Static;
                foreach(var texture in font.atlasTextures)if(!AssetDatabase.Contains(texture))AssetDatabase.AddObjectToAsset(texture,font);
            }
            foreach(char c in text)if(!char.IsWhiteSpace(c)&&!font.HasCharacter(c))throw new Exception("Unbaked letter character: "+c);
            EditorUtility.SetDirty(font);AssetDatabase.SaveAssets();
            return "Original supplied paper imported; handwritten letter atlas contains every letter character";
        }
    }
}

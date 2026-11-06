using UnityEditor;
using UnityEngine;

namespace GuanshanYouyan.Editor
{
    public static class FogOpeningDriveSetup
    {
        public static string Import()
        {
            AssetDatabase.Refresh();
            for(int i=1;i<=3;i++)
            {
                string path="Assets/GuanshanYouyan/Resources/OpeningDrive/Road"+i+".png";
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
                importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return "Three supplied road compositions imported as separate cool-color sprites";
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace SecretVirus.Editor
{
    public class TitleArtImporter:AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(assetPath!="Assets/Game/Resources/TitleIllustration.png"&&assetPath!="Assets/Game/Resources/CharacterFaceAtlas.png")return;
            var texture=(TextureImporter)assetImporter;
            texture.textureType=TextureImporterType.Default;texture.sRGBTexture=true;
            texture.mipmapEnabled=false;texture.npotScale=TextureImporterNPOTScale.None;
            texture.maxTextureSize=4096;texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;
            texture.textureCompression=TextureImporterCompression.CompressedHQ;
        }
    }
}

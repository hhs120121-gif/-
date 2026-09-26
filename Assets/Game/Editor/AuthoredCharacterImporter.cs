using UnityEditor;

namespace SecretVirus.Editor
{
    public class AuthoredCharacterImporter:AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if(!assetPath.StartsWith("Assets/Game/Resources/Characters/")&&!assetPath.StartsWith("Assets/Game/Resources/Environment/"))return;
            var model=(ModelImporter)assetImporter;
            model.globalScale=1;model.useFileScale=true;model.isReadable=true;
            model.importAnimation=false;model.animationType=ModelImporterAnimationType.Generic;
            model.importBlendShapes=true;model.importNormals=ModelImporterNormals.Import;
            model.importCameras=false;model.importLights=false;
            model.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
        }
    }
}

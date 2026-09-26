#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;

namespace SecretVirus
{
    public partial class VirusGame
    {
        public void ReviewCharacters(){StartCoroutine(CaptureCharacters());}
        IEnumerator CaptureCharacters()
        {
            var group=Model3D.Group("Character review only",null,new Vector3(300,0,0));var actors=new Transform[3];
            Model3D.Box(group,"Shadow floor",0,-.09f,0,12,.12f,12,"#828C8C");
            for(int i=0;i<3;i++)actors[i]=Model3D.Character(i,group,new Vector3((i-1)*1.4f,0,0));Model3D.Layer(group,30);
            var go=new GameObject("Review camera");var camera=go.AddComponent<Camera>();camera.cullingMask=1<<30;camera.fieldOfView=32;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=PixelArt.C("#747F82");camera.nearClipPlane=.1f;camera.farClipPlane=20;camera.transform.position=new Vector3(300,1.7f,-6.0f);camera.transform.LookAt(new Vector3(300,1.30f,0));camera.enabled=false;
            var target=new RenderTexture(1500,1000,24){antiAliasing=1};target.Create();camera.targetTexture=target;
            string[] names={"front","threequarter","back","walk"};float[] angles={0,55,180,15};
            yield return null;
            int skinnedParts=0;
            foreach(var actor in actors){
                foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>()){
                    if(skin.bones.Length!=2||skin.sharedMesh.bindposes.Length!=2)throw new System.Exception("Character rig has missing bones.");
                    foreach(var weight in skin.sharedMesh.boneWeights)if(Mathf.Abs(weight.weight0+weight.weight1-1)>.0001f)throw new System.Exception("Unnormalized character skin weight.");
                    if(!skin.sharedMaterial.shader.isSupported)throw new System.Exception("Character shader is unsupported.");
                    skinnedParts++;
                }
            }
            if(skinnedParts!=16||Resources.Load<Texture2D>("CharacterFaceAtlas")==null)throw new System.Exception("Character assets are incomplete.");
            for(int frame=0;frame<4;frame++){
                foreach(var actor in actors){actor.localRotation=Quaternion.Euler(0,angles[frame],0);actor.GetComponent<CharacterMotion3D>().Pose(1.0f,frame==3);}
                yield return new WaitForEndOfFrame();camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(1500,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1500,1000),0,0);image.Apply();RenderTexture.active=previous;File.WriteAllBytes("Tools/characters-"+names[frame]+".png",image.EncodeToPNG());Destroy(image);
            }
            File.WriteAllText("Tools/characters-review.txt","PASS: 16 skinned parts across three heroes; two bones per part; normalized weights; supported shader; face atlas present. Captured front, three-quarter, rear, and walking poses. Visual quality remains a separate review.");
            group.gameObject.SetActive(false);Destroy(group.gameObject);camera.targetTexture=null;Destroy(go);target.Release();Destroy(target);
        }
    }
}
#endif

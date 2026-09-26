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
            var target=new RenderTexture(1500,1000,24){antiAliasing=4};target.Create();camera.targetTexture=target;
            string[] names={"front","threequarter","back","walk","daeun-face","james-face","daniel-face","grey","blink"};float[] angles={0,55,180,15,0,0,0,0,0};
            yield return null;
            int skinnedParts=0;
            foreach(var actor in actors){
                if(actor.GetComponent<AuthoredCharacterPose>()==null)throw new System.Exception("Authored character missing.");
                foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>()){
                    if(skin.bones.Length<1||skin.sharedMesh.bindposes.Length!=skin.bones.Length)throw new System.Exception("Character rig has missing bones.");
                    foreach(var bone in skin.bones)if(bone==null)throw new System.Exception("Null skin bone.");
                    foreach(var weight in skin.sharedMesh.boneWeights)if(Mathf.Abs(weight.weight0+weight.weight1+weight.weight2+weight.weight3-1)>.001f)throw new System.Exception("Unnormalized character skin weight.");
                    if(!skin.sharedMaterial.shader.isSupported)throw new System.Exception("Character shader is unsupported.");
                    skinnedParts++;
                }
            }
            if(skinnedParts<24)throw new System.Exception("Character assets are incomplete.");
            Material clay=null;
            for(int frame=0;frame<names.Length;frame++){
                foreach(var actor in actors){actor.localRotation=Quaternion.Euler(0,angles[frame],0);actor.GetComponent<CharacterMotion3D>().Pose(1.0f,frame==3);}
                foreach(var actor in actors)foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>()){
                    var baked=new Mesh();skin.BakeMesh(baked);foreach(var vertex in baked.vertices)if(float.IsNaN(vertex.x)||float.IsInfinity(vertex.x)||vertex.magnitude>10)throw new System.Exception("Invalid deformed mesh: "+skin.name);Destroy(baked);
                }
                if(frame>=4&&frame<=6){float x=300+(frame-5)*1.4f,y=2.18f*(frame==6?1.08f:frame==5?1.025f:1);camera.transform.position=new Vector3(x,y,-1.05f);camera.transform.LookAt(new Vector3(x,y,0));}
                if(frame==7){camera.transform.position=new Vector3(300,1.7f,-6);camera.transform.LookAt(new Vector3(300,1.3f,0));clay=new Material(Resources.Load<Shader>("IllustratedCharacter"));clay.color=new Color(.55f,.55f,.55f);foreach(var actor in actors)foreach(var renderer in actor.GetComponentsInChildren<SkinnedMeshRenderer>()){var slots=renderer.sharedMaterials;for(int i=0;i<slots.Length;i++)slots[i]=clay;renderer.sharedMaterials=slots;}}
                if(frame==8){camera.transform.position=new Vector3(298.6f,2.18f,-1.05f);camera.transform.LookAt(new Vector3(298.6f,2.18f,0));foreach(var skin in actors[0].GetComponentsInChildren<SkinnedMeshRenderer>())for(int i=0;i<skin.sharedMesh.blendShapeCount;i++)if(skin.sharedMesh.GetBlendShapeName(i).EndsWith("Blink"))skin.SetBlendShapeWeight(i,100);}
                yield return new WaitForEndOfFrame();camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(1500,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1500,1000),0,0);image.Apply();RenderTexture.active=previous;File.WriteAllBytes("Tools/characters-"+names[frame]+".png",image.EncodeToPNG());Destroy(image);
            }
            File.WriteAllText("Tools/characters-review.txt","PASS: "+skinnedParts+" skinned parts across three heroes; matching bones/bindposes; normalized weights; supported shader. Captured front, three-quarter, rear, and walking poses. Visual quality remains a separate review.");
            group.gameObject.SetActive(false);Destroy(group.gameObject);camera.targetTexture=null;Destroy(go);target.Release();Destroy(target);if(clay!=null)Destroy(clay);
        }
    }
}
#endif

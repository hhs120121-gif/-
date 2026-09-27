using System.Collections.Generic;
using UnityEngine;

namespace SecretVirus
{
    public static class AuthoredCharacters
    {
        static readonly Dictionary<Material,Material> materials=new Dictionary<Material,Material>();
        public static Transform Create(int hero,Transform parent,Vector3 position)
        {
            var prefab=Resources.Load<GameObject>("Characters/"+new[]{"Daeun","James","Daniel"}[hero]);
            if(prefab==null)return IllustratedCharacters.Create(hero,parent,position);
            var root=Object.Instantiate(prefab,parent).transform;root.name="Authored "+prefab.name;
            root.localPosition=position;root.localRotation=Quaternion.identity;root.localScale=Vector3.one*(hero==2?1.08f:hero==1?1.025f:1);
            foreach(var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>()){
                var source=renderer.sharedMaterials;var converted=new Material[source.Length];
                for(int i=0;i<source.Length;i++){
                    if(!materials.TryGetValue(source[i],out var material)){
                        material=new Material(Resources.Load<Shader>("IllustratedCharacter"));material.name="Authored / "+source[i].name;
                        // Blender exports linear material factors; this project renders in Gamma space.
                        material.color=QualitySettings.activeColorSpace==ColorSpace.Gamma?source[i].color.gamma:source[i].color;
                        string role=source[i].name.ToLowerInvariant();
                        material.SetFloat("_Glossiness",role.Contains("metal")?.65f:role.Contains("leather")?.22f:role.Contains("iris")?.35f:.04f);
                        material.SetFloat("_Skin",role.StartsWith("skin")?1:0);
                        material.SetFloat("_Fabric",role.Contains("cloth")||role.Contains("coat")||role.Contains("cotton")?1:0);
                        int tile=role.StartsWith("hair")?hero:role.StartsWith("leather")?7:role.StartsWith("cotton")?4:role.StartsWith("cloth")||role.StartsWith("coat")||role.StartsWith("patch")?(hero==0?3:hero==1?5:6):-1;
                        if(tile>=0){material.SetTexture("_DetailAtlas",Resources.Load<Texture2D>("CharacterMaterialAtlas"));material.SetVector("_AtlasRect",new Vector4((tile%4)*.25f,tile<4?.5f:0,.25f,.5f));material.SetFloat("_UseAtlas",tile<3?.45f:.3f);material.SetFloat("_DetailMean",new[]{.19f,.51f,.27f,.30f,.74f,.19f,.31f,.19f}[tile]);material.SetFloat("_DetailTiling",tile<3?1:3);}
                        materials[source[i]]=material;
                    }
                    converted[i]=material;
                }
                renderer.sharedMaterials=converted;renderer.updateWhenOffscreen=false;
                // Face and eyes use the same soft light, without fringe casting a mask over the eyes.
                if(renderer.name.Contains("Face")||renderer.name.Contains("Eye"))renderer.receiveShadows=false;
            }
            var animator=root.GetComponentInChildren<Animator>();if(animator!=null)animator.enabled=false;
            root.gameObject.AddComponent<AuthoredCharacterPose>().Initialize(hero);
            root.gameObject.AddComponent<CharacterMotion3D>();return root;
        }
    }

    public class AuthoredCharacterPose:MonoBehaviour
    {
        class Joint{public Transform bone;public Quaternion rest;public Vector3 x,y,z;}
        readonly Dictionary<string,Joint> joints=new Dictionary<string,Joint>();
        SkinnedMeshRenderer[] faces;int hero;float phase;Vector3 hipRest;
        public void Initialize(int id)
        {
            hero=id;phase=id*1.3f;
            foreach(var bone in GetComponentsInChildren<Transform>()){
                if(joints.ContainsKey(bone.name))continue;
                joints[bone.name]=new Joint{bone=bone,rest=bone.localRotation,x=bone.InverseTransformDirection(transform.right),y=bone.InverseTransformDirection(transform.up),z=bone.InverseTransformDirection(transform.forward)};
            }
            if(joints.TryGetValue("Hips",out var hips))hipRest=hips.bone.localPosition;
            faces=GetComponentsInChildren<SkinnedMeshRenderer>();
        }
        void Rotate(string name,float x=0,float y=0,float z=0)
        {
            if(!joints.TryGetValue(name,out var j))return;
            j.bone.localRotation=j.rest*Quaternion.AngleAxis(x,j.x)*Quaternion.AngleAxis(y,j.y)*Quaternion.AngleAxis(z,j.z);
        }
        public void Pose(float time,bool walking,float ability)
        {
            float wave=Mathf.Sin(time),stride=walking?wave*(hero==1?22:27):0;
            Rotate("LThigh",stride);Rotate("RThigh",-stride);
            Rotate("LShin",walking?-Mathf.Max(0,-wave)*38:0);Rotate("RShin",walking?-Mathf.Max(0,wave)*38:0);
            Rotate("LFoot",walking?Mathf.Max(0,-wave)*14:0);Rotate("RFoot",walking?Mathf.Max(0,wave)*14:0);
            Rotate("LUpperArm",-stride*.65f,z:hero==1?2:0);Rotate("RUpperArm",ability>0?-58:stride*.65f);
            Rotate("LForearm",-8-Mathf.Abs(stride)*.22f);Rotate("RForearm",ability>0?-48:-8-Mathf.Abs(stride)*.22f);
            float idle=Mathf.Sin(Time.unscaledTime*1.45f+phase);
            Rotate("Hips",y:walking?wave*3:0,z:walking?wave*1.5f:hero==2?-1:0);
            Rotate("Spine",x:hero==1?1.5f:0,y:walking?-wave*2:0);
            Rotate("Chest",x:idle*.45f,y:walking?-wave*3:0);
            bool talking=VirusGame.Instance!=null&&VirusGame.Instance.Mode==GameMode.Dialogue;
            Rotate("Neck",x:walking?0:idle*(talking?1.7f:.45f));Rotate("Head",y:walking?0:Mathf.Sin(Time.unscaledTime*.48f+phase)*2);
            if(joints.TryGetValue("Hips",out var hips)){
                hips.bone.localPosition=hipRest+hips.bone.parent.InverseTransformVector(transform.up*(walking?Mathf.Abs(wave)*.025f:idle*.004f));
                if(walking&&joints.TryGetValue("LFoot",out var lf)&&joints.TryGetValue("RFoot",out var rf)){
                    float lowest=Mathf.Min(transform.InverseTransformPoint(lf.bone.position).y,transform.InverseTransformPoint(rf.bone.position).y);
                    hips.bone.localPosition+=hips.bone.parent.InverseTransformVector(transform.TransformVector(Vector3.up*Mathf.Clamp(.16f-lowest,-.18f,.08f)));
                }
            }
            float cycle=(Time.unscaledTime+phase)%4.8f;float blink=cycle<.16f?Mathf.Sin(cycle/.16f*Mathf.PI)*100:0;
            foreach(var face in faces){var mesh=face.sharedMesh;
                for(int i=0;i<mesh.blendShapeCount;i++){
                    string name=mesh.GetBlendShapeName(i);float value=name.EndsWith("Blink")?blink:name.EndsWith("Resolve")?ability*55:name.EndsWith("Concern")&&VirusGame.Instance!=null&&VirusGame.Instance.State.hp<35?40:name.EndsWith("Smile")&&VirusGame.Instance!=null&&VirusGame.Instance.Mode==GameMode.Ending?30:0;
                    face.SetBlendShapeWeight(i,value);
                }
            }
        }
    }
}

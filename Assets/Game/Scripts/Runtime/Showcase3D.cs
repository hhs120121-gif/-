using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace SecretVirus
{
    public class Showcase3D:MonoBehaviour
    {
        public RenderTexture target;Camera camera3D;Transform scene,cast,enemy,party;GameMode mode=(GameMode)(-1);string enemyId="";int hero=-1;
        readonly Dictionary<int,Texture2D> portraits=new Dictionary<int,Texture2D>();
        float actionStart=-10;BattleAction actionType;
        public void PlayAction(BattleAction action){actionStart=Time.unscaledTime;actionType=action;}
        public void Initialize()
        {
            scene=Model3D.Group("Cinematic 3D set",transform,new Vector3(120,0,0));
            Model3D.Box(scene,"Cinematic floor",0,-.2f,0,28,.4f,22,"#273F49");Model3D.Box(scene,"Back wall",0,2.5f,6,28,5,.3f,"#34525E");
            for(int x=-12;x<=12;x+=3){Model3D.Box(scene,"Structure",x,2.5f,5.7f,.28f,5,.35f,"#6C8588");Model3D.Box(scene,"Vertical light",x+.25f,2.5f,5.48f,.045f,3.5f,.035f,"#92D7C9",true);}
            for(int z=-5;z<6;z+=2)Model3D.Box(scene,"Floor seam",0,.012f,z,28,.014f,.018f,"#668482");
            Model3D.PointLight(scene,new Vector3(-3,3,-2),"#FFD7AE",2.8f,10);Model3D.PointLight(scene,new Vector3(4,3,2),"#85DCE2",3,10);
            Model3D.Layer(scene,28);
            var cameraObject=Model3D.Group("Cinematic camera",transform,Vector3.zero);camera3D=cameraObject.gameObject.AddComponent<Camera>();camera3D.cullingMask=1<<28;camera3D.fieldOfView=40;camera3D.clearFlags=CameraClearFlags.SolidColor;camera3D.backgroundColor=PixelArt.C("#101E29");camera3D.nearClipPlane=.1f;camera3D.farClipPlane=50;
            target=new RenderTexture(1280,720,24){antiAliasing=4,filterMode=FilterMode.Bilinear,name="Cinematic 3D view"};target.Create();camera3D.targetTexture=target;
            StartCoroutine(MakePortraits());
        }
        IEnumerator MakePortraits()
        {
          for(int id=0;id<6;id++){
            var actor=Model3D.Character(id,transform,new Vector3(220,0,0));Model3D.Layer(actor,29);
            var obj=new GameObject("Portrait capture");var cam=obj.AddComponent<Camera>();cam.cullingMask=1<<29;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.10f,.17f,.20f,1);cam.fieldOfView=31;cam.nearClipPlane=.1f;cam.farClipPlane=10;
            cam.transform.position=new Vector3(220,1.65f,-2.55f);cam.transform.LookAt(new Vector3(220,1.46f,0));
            var rt=new RenderTexture(256,320,24){antiAliasing=1,filterMode=FilterMode.Bilinear,name="3D portrait "+id};rt.Create();cam.targetTexture=rt;cam.enabled=false;
            yield return null;
            cam.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(256,320,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,256,320),0,0);image.Apply();RenderTexture.active=previous;portraits[id]=image;
            yield return new WaitForEndOfFrame();
            actor.gameObject.SetActive(false);Destroy(actor.gameObject);cam.targetTexture=null;Destroy(obj);rt.Release();Destroy(rt);
          }
        }
        public Texture Portrait(int id)=>portraits.TryGetValue(Mathf.Clamp(id,0,5),out var texture)?texture:Texture2D.blackTexture;
        public void Present(GameMode next,CombatSession battle,int leader)
        {
            bool visible=next==GameMode.Title||next==GameMode.Battle||next==GameMode.Ending;camera3D.enabled=visible;if(!visible)return;
            string id=next==GameMode.Battle&&battle!=null?battle.enemy.id:"";
            if(mode!=next||enemyId!=id||hero!=leader){
                mode=next;enemyId=id;hero=leader;if(cast!=null){cast.gameObject.SetActive(false);Destroy(cast.gameObject);}cast=Model3D.Group("Cast",scene,Vector3.zero);
                if(next==GameMode.Battle&&battle!=null){
                    party=Model3D.Character(leader,cast,new Vector3(-2,0,-.4f));party.localRotation=Quaternion.Euler(0,-100,0);
                    enemy=battle.enemy.machine?Model3D.Prop("machine",cast,new Vector3(.5f,0,1)):Model3D.Character(battle.enemy.boss?3:battle.enemy.id=="c"?5:4,cast,new Vector3(.5f,0,1));enemy.localRotation=Quaternion.Euler(0,18,0);
                    camera3D.transform.position=scene.position+new Vector3(0,3.6f,-8.3f);camera3D.transform.LookAt(scene.position+new Vector3(0,1.1f,0));
                }else{
                    Model3D.Character(1,cast,new Vector3(2.0f,0,1.1f));Model3D.Character(0,cast,new Vector3(3.4f,0,0));Model3D.Character(2,cast,new Vector3(4.9f,0,1.2f));
                    Model3D.Prop("machine",cast,new Vector3(6.7f,0,3));Model3D.Prop("terminal",cast,new Vector3(1,0,3));
                    camera3D.transform.position=scene.position+new Vector3(0,3.5f,-10.8f);camera3D.transform.LookAt(scene.position+new Vector3(.5f,1.15f,.5f));
                }
                Model3D.Layer(cast,28);
            }
            foreach(var actor in cast.GetComponentsInChildren<CharacterMotion3D>())actor.Pose(Time.unscaledTime*2,false);
            if(next==GameMode.Battle&&party!=null&&enemy!=null){
                float t=Time.unscaledTime-actionStart;
                float lunge=t>=0&&t<.5f?Mathf.Sin(t/.5f*Mathf.PI):0;
                bool physical=actionType==BattleAction.Attack||actionType==BattleAction.Subdue;
                party.localPosition=new Vector3(-2+(physical?lunge*.42f:0),0,-.4f);
                if(t>=0&&t<.5f)party.GetComponent<CharacterMotion3D>().Pose(t*12,false,1);
                float recoil=t>=.6f&&t<1.05f?Mathf.Sin((t-.6f)/.45f*Mathf.PI)*.12f:0;
                enemy.localPosition=new Vector3(.5f-recoil,0,1-recoil);party.localPosition+=Vector3.left*recoil;
            }
        }
        void OnDestroy(){if(target!=null){target.Release();Destroy(target);}foreach(var portrait in portraits.Values)Destroy(portrait);}
    }
}

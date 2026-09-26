using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SecretVirus
{
    // Original, volumetric models. Every visible part is a lit mesh, not a billboard.
    public static class Model3D
    {
        static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        public static Material Material(string hex,bool glow=false)
        {
            string key=hex+glow;if(materials.TryGetValue(key,out var found))return found;
            var template=Resources.Load<Material>("World3DMaterial");var m=template!=null?new Material(template):new Material(Shader.Find("Standard"));m.name="SV3D "+key;m.color=PixelArt.C(hex);m.SetFloat("_Glossiness",.28f);
            if(glow){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",PixelArt.C(hex)*1.2f);}materials[key]=m;return m;
        }
        public static Transform Group(string name,Transform parent,Vector3 position)
        {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
        public static Transform Part(Transform parent,string name,PrimitiveType shape,Vector3 p,Vector3 scale,string color,bool glow=false)
        {
            var go=GameObject.CreatePrimitive(shape);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;
            var collider=go.GetComponent<Collider>();if(collider!=null)Object.Destroy(collider);
            var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=Material(color,glow);r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;return go.transform;
        }
        public static Transform Box(Transform t,string n,float x,float y,float z,float w,float h,float d,string c,bool glow=false)
        {return Part(t,n,PrimitiveType.Cube,new Vector3(x,y,z),new Vector3(w,h,d),c,glow);}
        static Transform Ball(Transform t,string n,float x,float y,float z,float w,float h,float d,string c)
        {return Part(t,n,PrimitiveType.Sphere,new Vector3(x,y,z),new Vector3(w,h,d),c);}
        static Transform Cylinder(Transform t,string n,float x,float y,float z,float w,float h,string c)
        {return Part(t,n,PrimitiveType.Cylinder,new Vector3(x,y,z),new Vector3(w,h/2,w),c);}
        public static Transform Character(int hero,Transform parent,Vector3 position)
        {
            var root=Group("Character "+hero,parent,position);
            string skin=hero==0?"#BD9477":hero==1?"#E8C5AA":hero==2?"#CC987A":"#C8B19D";
            string hair=hero==0?"#3A2926":hero==1?"#DAB85D":hero==2?"#A74929":hero==3?"#98A5AA":"#333947";
            string cloth=hero==0?"#527C91":hero==1?"#303B4D":hero==2?"#586064":hero==3?"#293F50":hero==5?"#D3DFD9":"#355365";
            var body=Group("Body",root,Vector3.zero);
            // Separate limb pivots support a real 3D walk cycle and profession gestures.
            for(int side=-1;side<=1;side+=2){
                var leg=Group(side<0?"Left leg":"Right leg",body,new Vector3(side*.16f,.83f,0));
                Part(leg,"Trouser leg",PrimitiveType.Capsule,new Vector3(0,-.29f,0),new Vector3(.23f,.33f,.24f),hero==2?"#494A42":cloth);
                Box(leg,"Boot",0,-.70f,-.075f,.25f,.19f,.39f,"#252A2E");
                var arm=Group(side<0?"Left arm":"Right arm",body,new Vector3(side*.34f,1.35f,0));
                Part(arm,"Sleeve",PrimitiveType.Capsule,new Vector3(0,-.21f,0),new Vector3(.20f,.24f,.23f),hero==0?"#E4DED0":cloth);
                Part(arm,"Forearm",PrimitiveType.Capsule,new Vector3(0,-.47f,-.02f),new Vector3(.16f,.15f,.17f),skin);
                Ball(arm,"Hand",0,-.64f,-.04f,.17f,.20f,.16f,skin);
            }
            Part(body,"Torso",PrimitiveType.Capsule,new Vector3(0,1.18f,0),new Vector3(.61f,.37f,.38f),hero==0?"#E4DED0":cloth);
            Box(body,"Shirt front",0,1.15f,-.184f,.30f,.55f,.036f,hero==2?"#693A38":hero==0?cloth:"#E0D9C5");
            Box(body,"Belt",0,.87f,-.015f,.57f,.09f,.39f,"#443E37");Box(body,"Buckle",0,.87f,-.22f,.09f,.07f,.035f,"#AE9970");
            if(hero==0){for(int side=-1;side<=1;side+=2)Box(body,"Overall strap",side*.19f,1.30f,-.185f,.065f,.45f,.05f,cloth);Box(body,"Bib pocket",0,1.12f,-.226f,.22f,.13f,.04f,"#7295A7");Box(body,"Sewn patch",.15f,.48f,-.13f,.13f,.15f,.03f,"#A4B0A7");Box(body,"Tool pouch",.33f,.88f,0,.17f,.25f,.20f,"#806245");}
            if(hero==1||hero==5){Box(body,"Coat left",-.24f,.88f,0,.22f,.53f,.41f,cloth);Box(body,"Coat right",.24f,.88f,0,.22f,.53f,.41f,cloth);}
            if(hero==2){Box(body,"Shoulder patch L",-.24f,1.44f,-.03f,.20f,.075f,.31f,"#8A6951");Box(body,"Shoulder patch R",.24f,1.44f,-.03f,.20f,.075f,.31f,"#8A6951");}
            if(hero==3)Box(body,"Director tie",0,1.27f,-.216f,.065f,.32f,.026f,"#8E4E44");
            if(hero==4){Box(body,"Protective vest",0,1.20f,-.15f,.49f,.45f,.17f,"#263B47");Box(body,"Badge",.14f,1.35f,-.25f,.07f,.10f,.025f,"#D6BA75");}
            Cylinder(body,"Neck",0,1.54f,0,.18f,.20f,skin);
            var head=Group("Head",body,new Vector3(0,1.83f,0));
            Ball(head,"Face",0,0,0,.52f,.60f,.47f,skin);Ball(head,"Hair cap",0,.17f,.045f,.56f,.36f,.50f,hair);
            for(int j=0;j<5;j++){var hairLock=Ball(head,"Hair lock",-.22f+j*.105f,.19f,-.15f,.18f,.20f,.18f,hair);hairLock.localRotation=Quaternion.Euler(0,0,15+j*8);}
            if(hero==0){Ball(head,"Tied hair",0,.10f,.30f,.27f,.33f,.30f,hair);Ball(head,"Loose ponytail",0,-.13f,.34f,.19f,.38f,.20f,hair);}
            for(int side=-1;side<=1;side+=2){
                Ball(head,"Ear",side*.26f,-.015f,0,.11f,.16f,.10f,skin);
                Ball(head,"Eye white",side*.115f,.015f,-.211f,.105f,.072f,.043f,"#F2EDE1");
                Ball(head,"Iris",side*.115f,.012f,-.236f,.045f,.053f,.02f,hero==1?"#507CA0":hero==2?"#4E7661":"#4D3931");
                Box(head,"Eyebrow",side*.117f,.086f,-.222f,.115f,.023f,.025f,hair);
                if(hero==1){Box(head,"Glasses upper",side*.12f,.068f,-.253f,.19f,.022f,.025f,"#4B4639");Box(head,"Glasses lower",side*.12f,-.045f,-.25f,.19f,.016f,.025f,"#4B4639");for(int edge=-1;edge<=1;edge+=2)Box(head,"Glasses rim",side*.12f+edge*.086f,.011f,-.25f,.017f,.12f,.025f,"#4B4639");}
                if(hero==2)for(int n=0;n<3;n++)Ball(head,"Freckle",side*(.12f+n*.027f),-.075f+(n%2)*.018f,-.22f,.013f,.014f,.012f,"#95644F");
            }
            if(hero==1)Box(head,"Glasses bridge",0,.028f,-.26f,.07f,.02f,.02f,"#4B4639");
            Ball(head,"Nose",0,-.047f,-.241f,.08f,.13f,.09f,skin);Box(head,"Mouth",0,-.158f,-.207f,.10f,.018f,.018f,"#92685C");
            root.localScale=Vector3.one*(hero==2?1.10f:hero==1?1.04f:1);
            root.gameObject.AddComponent<CharacterMotion3D>();return root;
        }
        public static Transform Prop(string type,Transform parent,Vector3 position,int variant=0)
        {
            var t=Group(type,parent,position);string metal="#52656B",dark="#26383F",wood="#887252",light="#B4BAB0";
            switch(type){
                case "bed":
                    Box(t,"Bed frame",0,.35f,0,1.2f,.25f,1.8f,dark);Box(t,"Mattress",0,.56f,0,1.12f,.22f,1.72f,"#B7B3A0");Box(t,"Blanket",0,.70f,-.32f,1.13f,.12f,1.05f,"#637D7A");Box(t,"Pillow",0,.74f,.60f,.77f,.18f,.38f,"#D8D0B8");for(int s=-1;s<=1;s+=2)Box(t,"Headboard",0,.77f,s*.88f,1.24f,.83f,.10f,metal);break;
                case "desk":case "bench":
                    Box(t,"Worktop",0,.94f,0,type=="bench"?1.8f:1.35f,.15f,.72f,wood);
                    for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Box(t,"Steel leg",x*.54f,.46f,z*.25f,.10f,.87f,.10f,dark);
                    Box(t,"Document",-.16f,1.03f,-.08f,.38f,.018f,.32f,"#E4DABD");for(int i=0;i<4;i++)Box(t,"Written line",-.16f,1.042f,-.17f+i*.06f,.24f,.005f,.011f,"#6A7C79");
                    if(type=="bench"){Box(t,"Tool rack",0,1.42f,.3f,1.65f,.75f,.08f,metal);for(int i=0;i<5;i++)Box(t,"Hanging tool",-.55f+i*.28f,1.45f,.22f,.055f,.37f,.06f,"#C0BDA8");}break;
                case "shelf":
                    for(int side=-1;side<=1;side+=2)Box(t,"Shelf upright",side*.62f,1,0,.10f,2,.64f,dark);
                    for(int y=0;y<4;y++){Box(t,"Shelf",0,.15f+y*.53f,0,1.35f,.07f,.65f,metal);for(int i=0;i<3;i++)Box(t,"Stored carton",-.42f+i*.4f,.32f+y*.53f,.03f,.29f,.28f,.44f,i%2==0?wood:"#7C8980");}break;
                case "terminal":
                    Box(t,"Terminal pedestal",0,.55f,.06f,.52f,1.1f,.50f,dark);Box(t,"Monitor casing",0,1.36f,.03f,.85f,.62f,.22f,metal);Box(t,"Display glass",0,1.38f,-.09f,.70f,.45f,.026f,"#214B52");for(int i=0;i<4;i++)Box(t,"Display line",-.08f,1.52f-i*.075f,-.11f,.46f-i*.07f,.018f,.013f,"#70D4BC",true);Box(t,"Keyboard",0,1.03f,-.25f,.70f,.065f,.30f,"#839897");break;
                case "door":
                    Box(t,"Door panel",0,1.25f,.14f,1.35f,2.5f,.16f,"#344F59");for(int side=-1;side<=1;side+=2)Box(t,"Door frame",side*.74f,1.29f,.1f,.12f,2.6f,.34f,metal);Box(t,"Header",0,2.55f,.10f,1.60f,.16f,.35f,metal);Box(t,"Access strip",.47f,1.30f,.03f,.04f,.56f,.025f,"#8FD6BC",true);Box(t,"Handle",.39f,1.1f,-.04f,.08f,.22f,.10f,light);break;
                case "machine":
                    Box(t,"Machine body",0,.90f,0,1.12f,1.8f,.67f,metal);Box(t,"Upper casing",0,1.8f,0,1.25f,.18f,.77f,light);Box(t,"Status panel",0,1.35f,-.35f,.71f,.39f,.04f,dark);for(int i=0;i<3;i++)Ball(t,"Indicator",-.22f+i*.22f,1.4f,-.39f,.085f,.085f,.045f,i==2?"#DC9B59":"#77C4AC");for(int i=0;i<5;i++)Box(t,"Cooling grille",0,.43f+i*.10f,-.35f,.76f,.04f,.045f,dark);Cylinder(t,"Side reservoir",.60f,1,.08f,.19f,1.25f,"#859D99");break;
                case "vial":
                    Cylinder(t,"Vial plinth",0,.20f,0,.55f,.40f,dark);Cylinder(t,"Glass vessel",0,.65f,0,.22f,.50f,"#A4CCCA");Cylinder(t,"Sample",0,.58f,0,.225f,.25f,new[]{"#C58C69","#8BADD3","#AF6769","#A69F57","#9677A4","#7BB8AB"}[variant%6]);Cylinder(t,"Stopper",0,.93f,0,.26f,.08f,metal);break;
                case "rubble":
                    for(int i=0;i<8;i++){var rock=Box(t,"Broken masonry",Mathf.Sin(i*4)*.55f,.18f+(i%3)*.10f,Mathf.Cos(i*4)*.30f,.45f,.34f,.35f,i%2==0?"#77786D":"#989587");rock.localRotation=Quaternion.Euler(i*17,i*53,i*11);}break;
                case "plant":
                    Cylinder(t,"Planter",0,.24f,0,.48f,.48f,wood);for(int i=0;i<5;i++){var leaf=Ball(t,"Leaf",Mathf.Sin(i*2)*.20f,.7f+(i%2)*.22f,Mathf.Cos(i*2)*.20f,.18f,.72f,.16f,"#607C61");leaf.localRotation=Quaternion.Euler(i*8,0,i*19);}break;
                case "lamp":
                    Cylinder(t,"Lamp foot",0,.06f,0,.48f,.12f,dark);Cylinder(t,"Stem",0,.85f,0,.07f,1.65f,metal);Cylinder(t,"Shade",0,1.70f,0,.52f,.38f,"#E0C891");PointLight(t,new Vector3(0,1.6f,0),"#FFD7A0",1.4f,4);break;
                case "vent":
                    Box(t,"Vent casing",0,.32f,0,1.25f,.64f,.65f,metal);for(int i=0;i<7;i++)Box(t,"Vent slat",-.48f+i*.16f,.35f,-.34f,.055f,.43f,.06f,dark);break;
                case "notice":
                    Box(t,"Notice stand",0,.55f,.15f,.09f,1.1f,.09f,metal);Box(t,"Notice board",0,1.35f,.07f,.90f,.85f,.10f,wood);Box(t,"Posted document",0,1.35f,.005f,.64f,.65f,.02f,"#E3D8BA");for(int i=0;i<6;i++)Box(t,"Printed record",0,1.56f-i*.08f,-.012f,.43f,.018f,.012f,metal);break;
                default:
                    Box(t,"Supply chest",0,.34f,0,1.12f,.67f,.60f,wood);Box(t,"Lid",0,.70f,0,1.17f,.09f,.65f,"#AC9771");for(int side=-1;side<=1;side+=2)Box(t,"Metal band",side*.40f,.37f,-.31f,.075f,.69f,.025f,metal);Box(t,"Latch",0,.50f,-.33f,.13f,.19f,.05f,"#B7BDAF");break;
            }return t;
        }
        public static Light PointLight(Transform parent,Vector3 position,string color,float intensity,float range)
        {var t=Group("Practical light",parent,position);var l=t.gameObject.AddComponent<Light>();l.type=LightType.Point;l.color=PixelArt.C(color);l.intensity=intensity;l.range=range;l.shadows=LightShadows.None;return l;}
        public static void Layer(Transform t,int layer){t.gameObject.layer=layer;foreach(Transform child in t)Layer(child,layer);}
    }
    public class CharacterMotion3D:MonoBehaviour
    {
        public float stride,gesture;public bool walking;Transform leftLeg,rightLeg,leftArm,rightArm,body;
        void Awake(){body=transform.Find("Body");leftLeg=body.Find("Left leg");rightLeg=body.Find("Right leg");leftArm=body.Find("Left arm");rightArm=body.Find("Right arm");}
        public void Pose(float time,bool move,float ability=0)
        {
            float swing=move?Mathf.Sin(time)*28:0;leftLeg.localRotation=Quaternion.Euler(swing,0,0);rightLeg.localRotation=Quaternion.Euler(-swing,0,0);
            leftArm.localRotation=Quaternion.Euler(-swing*.7f,0,0);rightArm.localRotation=Quaternion.Euler(ability>0?-65+Mathf.Sin(time*2)*15:swing*.7f,0,0);
            body.localPosition=new Vector3(0,move?Mathf.Abs(Mathf.Sin(time))*.045f:Mathf.Sin(Time.unscaledTime*1.7f)*.012f,0);
        }
    }
}

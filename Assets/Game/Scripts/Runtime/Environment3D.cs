using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SecretVirus
{
    public static class Environment3D
    {
        static readonly Dictionary<string,Material> mats=new Dictionary<string,Material>();
        static readonly Dictionary<Material,Material> imported=new Dictionary<Material,Material>();
        static Mesh unit;
        public static Material Surface(string name,string hex,float smooth=.18f,float metal=0,float kind=0,bool glow=false)
        {
            string key=name+hex;if(mats.TryGetValue(key,out var value))return value;
            value=new Material(Resources.Load<Shader>("EnvironmentSurface")){name=name,color=PixelArt.C(hex)};value.SetFloat("_Glossiness",smooth);value.SetFloat("_Metallic",metal);value.SetFloat("_Kind",kind);if(glow)value.SetColor("_EmissionColor",value.color*.8f);mats[key]=value;return value;
        }
        public static Transform Box(Transform parent,string name,Vector3 position,Vector3 size,Material mat)
        {
            if(unit==null){var prefab=Resources.Load<GameObject>("Environment/BeveledUnit");if(prefab!=null){unit=Object.Instantiate(prefab.GetComponentInChildren<MeshFilter>().sharedMesh);unit.name="Normalized bevel module";var bounds=unit.bounds;var vertices=unit.vertices;for(int i=0;i<vertices.Length;i++){var p=vertices[i]-bounds.center;vertices[i]=new Vector3(p.x/bounds.size.x,p.y/bounds.size.y,p.z/bounds.size.z);}unit.vertices=vertices;unit.RecalculateBounds();}}
            var t=Model3D.Group(name,parent,position);t.localScale=size;t.gameObject.AddComponent<MeshFilter>().sharedMesh=unit;
            var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=mat;return t;
        }
        public static Transform Prop(string type,Transform parent,Vector3 position,int variant)
        {
            var prefab=Resources.Load<GameObject>("Environment/"+type);if(prefab==null)return null;
            var t=Model3D.Group("Authored prop / "+type,parent,position);var artwork=Object.Instantiate(prefab,t).transform;artwork.localPosition=Vector3.zero;artwork.localRotation=prefab.transform.localRotation;artwork.localScale=prefab.transform.localScale;
            foreach(var r in t.GetComponentsInChildren<MeshRenderer>()){
                var slots=r.sharedMaterials;
                for(int i=0;i<slots.Length;i++){
                    if(!imported.TryGetValue(slots[i],out var m)){
                        string n=slots[i].name;Color c=QualitySettings.activeColorSpace==ColorSpace.Gamma?slots[i].color.gamma:slots[i].color;
                        m=new Material(Resources.Load<Shader>(n.Contains("GlassClear")?"EnvironmentGlass":"EnvironmentSurface")){name="Environment / "+n,color=c};
                        bool steel=n.Contains("Steel"),paint=n.Contains("Paint"),rubber=n.Contains("Rubber"),glass=n.Contains("Glass");
                        m.SetFloat("_Glossiness",steel?.55f:glass?.65f:paint?.28f:rubber?.035f:.12f);m.SetFloat("_Metallic",steel?.75f:n.Contains("Metal")?.55f:paint?.25f:0);
                        m.SetFloat("_Kind",n.Contains("Wood")?2:n.Contains("Cotton")||n.Contains("Blanket")?3:n.Contains("Concrete")?1:0);
                        if(n.Contains("Glow"))m.SetColor("_EmissionColor",c*.65f);imported[slots[i]]=m;
                    }
                    slots[i]=type=="vial"&&m.name.Contains("GlowMint")?Surface("Sample fluid "+variant,new[]{"#659D88","#708DAA","#A46761","#A19B65","#967CA3","#7DADA3"}[variant%6],.3f,0,0,true):m;
                }
                r.sharedMaterials=slots;
            }
            if(type=="lamp"){var light=Model3D.PointLight(t,new Vector3(0,1.76f,0),"#FFD6A0",1.1f,4);light.shadows=LightShadows.None;}
            if(type=="bench")TaskLight(t,new Vector3(-.65f,2.25f,-.05f));
            return t;
        }
        static void TaskLight(Transform parent,Vector3 p)
        {
            var t=Model3D.Group("Task spotlight",parent,p);t.localRotation=Quaternion.Euler(60,0,0);var l=t.gameObject.AddComponent<Light>();l.type=LightType.Spot;l.color=PixelArt.C("#FFDFB0");l.intensity=2.2f;l.range=6;l.spotAngle=85;l.shadows=LightShadows.Soft;l.shadowStrength=.55f;l.shadowBias=.025f;
        }
        public static EnvironmentPresentation Build(WorldView world,int stage)
        {
            var root=world.root;var presentation=root.gameObject.AddComponent<EnvironmentPresentation>();
            bool home=stage<=3,lab=stage>=10;var concrete=Surface("Fine concrete",home?"#696A61":"#536369",.08f,0,1);var wall=Surface("Painted plaster",home?"#929488":"#70868A",.09f,0,1);var steel=Surface("Structural steel","#405760",.32f,.5f);var trim=Surface("Brushed trim","#8B9998",.4f,.6f);var dark=Surface("Rubber","#303E43",.05f);var wood=Surface("Warm wood","#80664F",.12f,0,2);var glow=Surface("Fixture diffuser",home?"#DCC899":"#A8C7C4",.2f,0,0,true);
            Box(root,"Foundation",new Vector3(15,-.23f,9),new Vector3(30,.42f,18),steel);
            Box(root,"Continuous concrete floor",new Vector3(15,-.015f,9),new Vector3(29.1f,.055f,17.1f),concrete);
            // Movement boundaries retain the campaign coordinate contract.
            for(int z=0;z<18;z++)for(int x=0;x<30;x++)if(z>=16||x==0||x==29||z==0)world.walls.Add(new Rect(x,z,1,1));
            for(int i=0;i<5;i++){
                float x=3+i*6;
                Box(root,"Window wall lower",new Vector3(x,.58f,16.35f),new Vector3(6,1.16f,.65f),wall);
                Box(root,"Window wall upper",new Vector3(x,3.9f,16.35f),new Vector3(6,.9f,.65f),wall);
                for(int s=-1;s<=1;s+=2){Box(root,"Window masonry return",new Vector3(x+s*2.32f,2.3f,16.35f),new Vector3(1.36f,2.3f,.65f),wall);Box(root,"Window frame jamb",new Vector3(x+s*1.59f,2.3f,16.06f),new Vector3(.09f,2.4f,.20f),trim);}
                Box(root,"Deep window sill",new Vector3(x,1.18f,15.98f),new Vector3(3.35f,.11f,.96f),trim);
                Box(root,"Window lintel",new Vector3(x,3.46f,16.06f),new Vector3(3.35f,.11f,.22f),trim);
                for(int m=-1;m<=1;m++)Box(root,"Window mullion",new Vector3(x+m*1.05f,2.3f,16.23f),new Vector3(.045f,2.3f,.07f),steel);
                Box(root,"Window transom",new Vector3(x,2.9f,16.23f),new Vector3(3.2f,.055f,.07f),steel);
                Box(root,"Back skirting",new Vector3(x,.13f,15.96f),new Vector3(6,.25f,.12f),steel);
                var column=Box(root,"Structural column",new Vector3(i*6+.5f,2.2f,15.84f),new Vector3(.30f,4.4f,.38f),steel);presentation.Track(column);
                Box(root,"Overhead beam",new Vector3(x,4.23f,15.85f),new Vector3(6,.27f,.38f),steel);
                Box(root,"Practical lamp housing",new Vector3(x,3.6f,15.75f),new Vector3(1.7f,.13f,.22f),dark);Box(root,"Practical lamp diffuser",new Vector3(x,3.53f,15.73f),new Vector3(1.5f,.025f,.17f),glow);
                var fill=Model3D.PointLight(root,new Vector3(x,3.05f,14.9f),home?"#E2C5A0":"#AEC8C5",.85f,5.5f);fill.shadows=LightShadows.None;
                // Window views contain real distant geometry for movement parallax.
                float h=5+i%3*2;Box(root,"Exterior industrial building",new Vector3(x+1,h/2,23+i%2*4),new Vector3(4.2f,h,3.2f),Surface("Exterior desaturated","#3C545F",.12f,0,1));
                for(int y=1;y<4;y++)for(int w=-1;w<=1;w++)Box(root,"Distant window",new Vector3(x+1+w*.95f,y*1.45f,21.35f+i%2*4),new Vector3(.48f,.75f,.025f),Surface("Distant glass","#718B91",.4f,.2f));
            }
            for(int side=-1;side<=1;side+=2)for(int z=3;z<17;z+=6){
                var group=Model3D.Group("Side wall bay",root,new Vector3(side<0?.5f:29.5f,0,z));
                Box(group,"Thick wall",new Vector3(0,2.1f,0),new Vector3(.7f,4.2f,5.9f),wall);Box(group,"Skirting",new Vector3(side<0?.39f:-.39f,.14f,0),new Vector3(.09f,.28f,5.9f),steel);presentation.Track(group);
            }
            for(int x=3;x<30;x+=6){var front=Box(root,"Foreground wall",new Vector3(x,1.9f,.45f),new Vector3(5.8f,3.8f,.6f),wall);presentation.Track(front);}
            foreach(float x in new[]{2f,27f}){
                var portal=Model3D.Group("Architectural doorway surround",root,new Vector3(x,0,3.2f));
                for(int side=-1;side<=1;side+=2)Box(portal,"Door masonry pier",new Vector3(side*.94f,1.55f,0),new Vector3(.42f,3.1f,.5f),wall);
                Box(portal,"Door masonry lintel",new Vector3(0,3.04f,0),new Vector3(2.3f,.25f,.5f),wall);presentation.Track(portal);
                world.walls.Add(new Rect(x-1.15f,2.95f,.42f,.5f));world.walls.Add(new Rect(x+.73f,2.95f,.42f,.5f));
            }
            // Ceiling services establish height without a solid roof hiding play.
            for(int z=5;z<=15;z+=5){var beam=Box(root,"Ceiling crossbeam",new Vector3(15,4.28f,z),new Vector3(28,.18f,.22f),steel);presentation.Track(beam);}
            for(int i=0;i<3;i++){
                var pipe=Model3D.Part(root,"Service pipe",PrimitiveType.Cylinder,new Vector3(15,3.93f,15.42f-i*.20f),new Vector3(.08f,14,.08f),"#8B9998");pipe.localRotation=Quaternion.Euler(0,0,90);pipe.GetComponent<Renderer>().sharedMaterial=trim;
            }
            for(int x=2;x<29;x+=4)Box(root,"Cable tray bracket",new Vector3(x,3.98f,15.20f),new Vector3(.04f,.25f,.7f),steel);
            if(home){
                Box(root,"Living zone timber platform",new Vector3(5.3f,.018f,12.1f),new Vector3(6.1f,.035f,5.2f),wood);
                for(int i=0;i<12;i++)Box(root,"Quiet timber joint",new Vector3(2.5f+i*.51f,.039f,12.1f),new Vector3(.009f,.002f,5.1f),Surface("Wood seams","#655342",.08f));
                var divider=Model3D.Group("Living workspace partition",root,new Vector3(10,0,13.7f));
                Box(divider,"Lower partition",new Vector3(0,.65f,0),new Vector3(.16f,1.3f,4.1f),wood);
                for(int z=-2;z<=2;z+=2)Box(divider,"Partition upright",new Vector3(0,1.4f,z),new Vector3(.10f,2.8f,.10f),steel);
                Box(divider,"Partition top rail",new Vector3(0,2.8f,0),new Vector3(.1f,.1f,4.2f),steel);world.walls.Add(new Rect(9.9f,11.6f,.2f,4.2f));presentation.Track(divider);
                Box(root,"Storage floor patch",new Vector3(23,.018f,10.5f),new Vector3(9,.025f,8.8f),Surface("Storage sealed concrete","#606A65",.12f,0,1));
                Box(root,"Workbench rubber mat",new Vector3(14,.031f,9.35f),new Vector3(2.9f,.045f,1.18f),dark);
            }else{
                Box(root,"Central circulation strip",new Vector3(15,.017f,8),new Vector3(27,.023f,2.8f),Surface("Sealed circulation",lab?"#657778":"#60706F",.22f,0,1));
                for(int side=-1;side<=1;side+=2)for(int x=3;x<28;x+=5)Box(root,"Restrained safety marking",new Vector3(x,.034f,8+side*1.45f),new Vector3(1.1f,.004f,.045f),Surface("Safety ochre","#A59768",.09f));
            }
            // Small recessed service covers break the broad floor without restoring a grid.
            for(int x=11;x<27;x+=7){Box(root,"Service cover rim",new Vector3(x,.018f,14.65f),new Vector3(1.1f,.025f,.8f),steel);Box(root,"Service cover",new Vector3(x,.036f,14.65f),new Vector3(1.02f,.018f,.72f),trim);}
            if(stage==4)for(int i=0;i<5;i++){var brace=Box(root,"Damaged structural brace",new Vector3(3+i*5,2.8f,15.6f),new Vector3(.15f,2.4f,.17f),steel);brace.localRotation=Quaternion.Euler(0,0,i%2==0?18:-12);}
            if(stage==6||stage==7)for(int x=3;x<29;x+=5){for(int b=0;b<4;b++)Box(root,"Containment bars",new Vector3(x+b*.25f,2.25f,15.7f),new Vector3(.035f,2.0f,.04f),steel);}
            if(lab)for(int x=4;x<28;x+=8){Box(root,"Laboratory service chase",new Vector3(x,1.0f,15.45f),new Vector3(2.2f,1.6f,.32f),Surface("Laboratory enamel","#899D9B",.35f,.2f));for(int i=0;i<3;i++)Box(root,"Supply socket",new Vector3(x-.6f+i*.6f,1.2f,15.27f),new Vector3(.13f,.18f,.04f),dark);}
            return presentation;
        }
    }
    public class EnvironmentPresentation:MonoBehaviour
    {
        class Occluder{public Renderer[] renderers;public Bounds bounds;public float visibility=1;}
        readonly List<Occluder> items=new List<Occluder>();MaterialPropertyBlock block;
        public void Track(Transform group){var r=group.GetComponentsInChildren<Renderer>();if(r.Length==0)return;Bounds b=r[0].bounds;foreach(var renderer in r)b.Encapsulate(renderer.bounds);items.Add(new Occluder{renderers=r,bounds=b});}
        public void Present(Camera camera,Vector3 player,float dt)
        {
            if(block==null)block=new MaterialPropertyBlock();
            foreach(var item in items){bool obstruction=false;var bounds=item.bounds;bounds.Expand(.22f);
                for(int sample=0;sample<3;sample++){Vector3 delta=player+Vector3.up*(.45f+sample*.98f)-camera.transform.position;var ray=new Ray(camera.transform.position,delta.normalized);if(bounds.IntersectRay(ray,out float hit)&&hit<delta.magnitude-.15f)obstruction=true;}
                float goal=obstruction?0:1;item.visibility=dt<=0?goal:Mathf.MoveTowards(item.visibility,goal,dt*5);
                foreach(var renderer in item.renderers){renderer.GetPropertyBlock(block);block.SetFloat("_Visibility",item.visibility);renderer.SetPropertyBlock(block);}
            }
        }
    }
}

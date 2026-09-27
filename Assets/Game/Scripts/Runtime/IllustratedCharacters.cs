using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SecretVirus
{
    // Sculpted cross-sections and swept surfaces, with articulated skinning.
    // Front-face artwork is UV mapped onto the curved head, never a billboard.
    public static class IllustratedCharacters
    {
        static readonly Dictionary<string,Material> mats=new Dictionary<string,Material>();
        static Material Mat(string color)
        {
            if(mats.TryGetValue(color,out var result))return result;
            var shader=Resources.Load<Shader>("IllustratedCharacter");result=shader!=null?new Material(shader):new Material(Resources.Load<Material>("World3DMaterial"));result.name="Character / "+color;result.color=PixelArt.C(color);result.SetFloat("_Glossiness",.12f);result.DisableKeyword("_EMISSION");mats[color]=result;return result;
        }
        static Material Face()
        {
            if(mats.TryGetValue("face",out var m))return m;
            m=new Material(Mat("#FFFFFF"));m.name="Illustrated face atlas";m.mainTexture=Resources.Load<Texture2D>("CharacterFaceAtlas");m.SetFloat("_Glossiness",.05f);mats["face"]=m;return m;
        }
        static Vector4 R(float y,float x,float z,float center=0)=>new Vector4(y,x,z,center);
        static Vector4 Curve(Vector4[] points,float t)
        {
            float f=t*(points.Length-1);int i=Mathf.Min(points.Length-2,(int)f);float a=f-i;
            Vector4 p0=points[Mathf.Max(0,i-1)],p1=points[i],p2=points[i+1],p3=points[Mathf.Min(points.Length-1,i+2)];
            return .5f*((2*p1)+(-p0+p2)*a+(2*p0-5*p1+4*p2-p3)*a*a+(-p0+3*p1-3*p2+p3)*a*a*a);
        }
        static Mesh Grid(string name,List<Vector3> v,List<Vector2> uv,int rows,int cols)
        {
            var indices=new List<int>();for(int y=0;y<rows-1;y++)for(int x=0;x<cols-1;x++){int a=y*cols+x;indices.Add(a);indices.Add(a+cols);indices.Add(a+1);indices.Add(a+1);indices.Add(a+cols);indices.Add(a+cols+1);}
            var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        static Transform Render(Transform parent,string name,Mesh mesh,Material material)
        {
            var t=Model3D.Group(name,parent,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<GeneratedCharacterMesh>().mesh=mesh;var renderer=t.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.On;return t;
        }
        static Transform Loft(Transform parent,string name,Vector4[] rings,string color,int rows=32,int sides=32)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();for(int y=0;y<rows;y++){float t=(float)y/(rows-1);Vector4 r=Curve(rings,t);for(int x=0;x<=sides;x++){float a=x*Mathf.PI*2/sides;v.Add(new Vector3(Mathf.Sin(a)*r.y,r.x,r.w-Mathf.Cos(a)*r.z));uv.Add(new Vector2((float)x/sides,t));}}
            var mesh=Grid(name,v,uv,rows,sides+1);var indices=new List<int>(mesh.triangles);
            for(int end=0;end<2;end++){Vector4 r=Curve(rings,end);int center=v.Count;v.Add(new Vector3(0,r.x,r.w));uv.Add(new Vector2(.5f,end));int ring=end==0?0:(rows-1)*(sides+1);for(int j=0;j<sides;j++){indices.Add(center);indices.Add(ring+j+(end==0?0:1));indices.Add(ring+j+(end==0?1:0));}}
            mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return Render(parent,name,mesh,Mat(color));
        }
        static void Skin(Transform surface,Transform upper,Transform lower,float jointY)
        {
            var mesh=surface.GetComponent<MeshFilter>().sharedMesh;var material=surface.GetComponent<MeshRenderer>().sharedMaterial;var weights=new BoneWeight[mesh.vertexCount];var vertices=mesh.vertices;
            for(int i=0;i<weights.Length;i++){float lowerWeight=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(jointY-.085f,jointY+.085f,vertices[i].y));weights[i]=new BoneWeight{boneIndex0=0,weight0=1-lowerWeight,boneIndex1=1,weight1=lowerWeight};}
            mesh.boneWeights=weights;mesh.bindposes=new[]{upper.worldToLocalMatrix*surface.localToWorldMatrix,lower.worldToLocalMatrix*surface.localToWorldMatrix};
            surface.GetComponent<MeshRenderer>().enabled=false;Object.Destroy(surface.GetComponent<MeshRenderer>());Object.Destroy(surface.GetComponent<MeshFilter>());
            var skin=surface.gameObject.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=mesh;skin.sharedMaterial=material;skin.bones=new[]{upper,lower};skin.rootBone=upper;skin.updateWhenOffscreen=true;skin.localBounds=new Bounds(new Vector3(0,-.35f,0),Vector3.one*2);
        }
        static Transform Ribbon(Transform parent,string name,Vector3[] path,float width,float thickness,string color,bool taper=false)
        {
            // Oval swept cross-section gives cloth straps and hair real thickness.
            var p=new Vector4[path.Length];for(int i=0;i<path.Length;i++)p[i]=new Vector4(path[i].x,path[i].y,path[i].z,0);
            const int rows=25,sides=10;var v=new List<Vector3>();var uv=new List<Vector2>();
            for(int i=0;i<rows;i++){float t=(float)i/(rows-1);Vector4 a=Curve(p,t),before=Curve(p,Mathf.Max(0,t-.005f)),after=Curve(p,Mathf.Min(1,t+.005f));Vector3 tangent=((Vector3)(after-before)).normalized;Vector3 axis=Mathf.Abs(Vector3.Dot(tangent,Vector3.right))>.95f?Vector3.up:Vector3.right;Vector3 widthAxis=Vector3.ProjectOnPlane(axis,tangent).normalized,depthAxis=Vector3.Cross(widthAxis,tangent).normalized;float w=width*(taper?Mathf.Max(.02f,Mathf.Pow(1-t,.55f))*Mathf.Min(1,.35f+t*5):1);
                for(int j=0;j<=sides;j++){float angle=j*Mathf.PI*2/sides;v.Add((Vector3)a+widthAxis*Mathf.Sin(angle)*w-depthAxis*Mathf.Cos(angle)*thickness*(taper?Mathf.Max(.03f,1-t):1));uv.Add(new Vector2((float)j/sides,t));}}
            return Render(parent,name,Grid(name,v,uv,rows,sides+1),Mat(color));
        }
        static Transform Patch(Transform parent,string name,Vector3 center,float width,float height,string color)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();for(int y=0;y<9;y++)for(int x=0;x<9;x++){float u=x/8f-.5f,w=y/8f-.5f;v.Add(center+new Vector3(u*width,w*height,.035f*u*u));uv.Add(new Vector2(u+.5f,w+.5f));}
            return Render(parent,name,Grid(name,v,uv,9,9),Mat(color));
        }
        static void Stitches(Transform parent,Vector3 center,float width,float height,string color)
        {for(int i=0;i<7;i++){float x=center.x-width*.45f+width*.9f*i/6;Ribbon(parent,"Hand sewn thread",new[]{new Vector3(x,center.y-height*.54f,center.z-.003f),new Vector3(x,center.y-height*.44f,center.z-.003f)},.0018f,.0018f,color);Ribbon(parent,"Hand sewn thread",new[]{new Vector3(x,center.y+height*.44f,center.z-.003f),new Vector3(x,center.y+height*.54f,center.z-.003f)},.0018f,.0018f,color);}}
        public static Transform Create(int hero,Transform parent,Vector3 position)
        {
            var root=Model3D.Group("Illustrated "+new[]{"Daeun","James","Daniel"}[hero],parent,position);var body=Model3D.Group("Body",root,Vector3.zero);
            string skin=new[]{"#BD8C70","#E7BFA8","#C58C6D"}[hero],hair=new[]{"#30231F","#BE9149","#813A23"}[hero];
            string cloth=hero==0?"#405F76":hero==1?"#303743":"#505958",pants=hero==2?"#353D3D":cloth;
            float shoulder=hero==2?.33f:hero==1?.28f:.255f;
            var torso=Loft(body,"Tailored shirt silhouette",new[]{R(hero==0?1.32f:1.18f,.185f,.125f),R(1.37f,hero==0?.17f:.205f,.12f),R(1.60f,hero==0?.22f:.26f,.14f),R(1.76f,shoulder,.12f),R(1.84f,.12f,.095f),R(1.88f,.067f,.066f)},hero==0?"#D8D3C6":hero==1?"#BDBAAE":"#512C31");
            Loft(body,"Trouser seat and waistband",new[]{R(1.015f,.035f,.048f),R(1.09f,.262f,.141f),R(1.19f,.25f,.145f),R(1.27f,.213f,.137f)},pants,28,36);
            Loft(body,"Neck",new[]{R(1.82f,.07f,.065f),R(1.96f,.061f,.064f),R(2.01f,.083f,.076f)},skin,16,24);
            for(int side=-1;side<=1;side+=2){
                var leg=Model3D.Group(side<0?"Left leg":"Right leg",body,new Vector3(side*.135f,1.13f,0));var knee=Model3D.Group("Knee",leg,new Vector3(0,-.52f,0));
                var trousers=Loft(leg,"Shaped trouser fabric",new[]{R(-1.02f,.102f,.102f,-.005f),R(-.93f,.118f,.111f),R(-.72f,.11f,.11f),R(-.53f,.105f,.105f,-.007f),R(-.31f,.125f,.126f),R(-.10f,.137f,.128f),R(.10f,.102f,.110f)},pants,40,28);Skin(trousers,leg,knee,-.52f);
                var boot=Model3D.Group("Boot",knee,new Vector3(0,-.46f,0));Loft(boot,"Sculpted footwear",new[]{R(-.15f,.096f,.16f,-.052f),R(-.12f,.105f,.172f,-.060f),R(-.06f,.095f,.16f,-.055f),R(.01f,.081f,.092f),R(.10f,.077f,.077f)},hero==0?"#44382F":"#242A2C",18,28);
                for(int j=0;j<4;j++)Patch(boot,"Boot lacing",new Vector3(0,.025f+j*.015f,-.093f),.09f,.005f,"#91877A");
                var arm=Model3D.Group(side<0?"Left arm":"Right arm",body,new Vector3(side*(shoulder-.022f),1.77f,0));var elbow=Model3D.Group("Elbow",arm,new Vector3(0,-.34f,0));
                var armSkin=Loft(arm,"Continuous arm skin",new[]{R(-.69f,.043f,.047f),R(-.58f,.047f,.048f),R(-.46f,.061f,.062f),R(-.33f,.061f,.061f),R(-.17f,.076f,.075f),R(.010f,.060f,.055f)},skin,32,24);Skin(armSkin,arm,elbow,-.34f);
                var sleeve=Loft(arm,"Cloth sleeve",hero==0?new[]{R(-.24f,.085f,.089f),R(-.19f,.091f,.090f),R(-.09f,.094f,.092f),R(.012f,.080f,.078f),R(.066f,.004f,.004f)}:new[]{R(-.68f,.057f,.063f),R(-.62f,.069f,.075f),R(-.40f,.079f,.079f),R(-.20f,.096f,.089f),R(.012f,.089f,.082f),R(.070f,.004f,.004f)},hero==0?"#D8D3C6":cloth,32,28);if(hero!=0)Skin(sleeve,arm,elbow,-.34f);
                if(hero==0)Loft(arm,"Rolled cotton cuff",new[]{R(-.245f,.085f,.09f),R(-.225f,.093f,.096f),R(-.193f,.09f,.093f)},"#EEE8D7",12,24);
                var hand=Model3D.Group("Hand",elbow,new Vector3(0,-.36f,0));Loft(hand,"Palm",new[]{R(-.10f,.046f,.025f),R(-.035f,.05f,.027f),R(.015f,.037f,.03f)},skin,16,20);
                for(int finger=0;finger<4;finger++){var f=Model3D.Group("Finger",hand,new Vector3(-.035f+finger*.023f,-.078f,0));Loft(f,"Tapered finger",new[]{R(-.087f+Mathf.Abs(finger-1.5f)*.011f,.004f,.006f,.018f),R(-.048f,.010f,.011f,.008f),R(0,.010f,.012f)},skin,12,12);}
                Ribbon(hand,"Thumb",new[]{new Vector3(side*.045f,-.02f,0),new Vector3(side*.069f,-.052f,.005f),new Vector3(side*.069f,-.09f,.019f)},.015f,.016f,skin,true);
                if(hero==0&&side==1){Patch(leg,"Repaired denim patch",new Vector3(0,-.31f,-.131f),.155f,.18f,"#66849A");Stitches(leg,new Vector3(0,-.31f,-.136f),.155f,.18f,"#B2B3A2");}
                if(hero==2)Patch(leg,"Cargo pocket",new Vector3(side*.025f,-.23f,-.136f),.145f,.17f,"#47504D");
            }
            if(hero==0){
                Loft(body,"Overall fitted waist",new[]{R(1.18f,.246f,.149f),R(1.26f,.215f,.141f),R(1.36f,.179f,.127f)},cloth,22,36);
                Patch(body,"Denim bib",new Vector3(0,1.50f,-.146f),.32f,.34f,cloth);Patch(body,"Bib stitched pocket",new Vector3(0,1.48f,-.157f),.185f,.135f,"#52748A");Stitches(body,new Vector3(0,1.48f,-.16f),.185f,.135f,"#B3A78D");
                for(int side=-1;side<=1;side+=2){Ribbon(body,"Overall shoulder strap",new[]{new Vector3(side*.145f,1.64f,-.15f),new Vector3(side*.18f,1.78f,-.13f),new Vector3(side*.19f,1.86f,-.035f),new Vector3(side*.17f,1.79f,.12f),new Vector3(side*.15f,1.42f,.14f)},.026f,.008f,"#58798D");Patch(body,"Brass buckle",new Vector3(side*.153f,1.68f,-.160f),.041f,.043f,"#B7AC83");}
            }else{
                for(int side=-1;side<=1;side+=2){
                    Ribbon(body,"Open tailored coat panel",new[]{new Vector3(side*.20f,hero==1?.62f:1.12f,-.10f),new Vector3(side*.21f,1.23f,-.135f),new Vector3(side*.19f,1.58f,-.151f),new Vector3(side*.24f,1.77f,-.078f)},hero==1?.105f:.094f,.025f,cloth);
                    Ribbon(body,"Turned lapel",new[]{new Vector3(side*.15f,1.46f,-.185f),new Vector3(side*.095f,1.72f,-.167f),new Vector3(side*.11f,1.87f,-.051f)},.033f,.014f,hero==1?"#454D58":"#6B7470");
                    if(hero==2)Ribbon(body,"Leather shoulder yoke",new[]{new Vector3(side*.12f,1.805f,-.115f),new Vector3(side*.22f,1.819f,-.092f),new Vector3(side*.34f,1.785f,-.075f)},.048f,.018f,"#75523D");
                }
                var back=Loft(body,"Coat back",new[]{R(hero==1?.63f:1.1f,hero==1?.29f:.24f,.15f,.035f),R(1.24f,.226f,.145f,.018f),R(1.60f,.265f,.155f,.016f),R(1.78f,shoulder+.015f,.115f,.025f),R(1.86f,.10f,.075f,.022f)},cloth,40,32);
                // Open front is a mesh cut, not a painted rectangle.
                var mesh=back.GetComponent<MeshFilter>().sharedMesh;var tris=new List<int>();var verts=mesh.vertices;var original=mesh.triangles;for(int i=0;i<original.Length;i+=3){Vector3 c=(verts[original[i]]+verts[original[i+1]]+verts[original[i+2]])/3;if(c.z<.03f&&Mathf.Abs(c.x)<.18f)continue;tris.Add(original[i]);tris.Add(original[i+1]);tris.Add(original[i+2]);}mesh.SetTriangles(tris,0);mesh.RecalculateNormals();
            }
            Head(body,hero,skin,hair);
            root.localScale=Vector3.one*(hero==2?1.08f:hero==1?1.025f:1);
            root.gameObject.AddComponent<CharacterMotion3D>();return root;
        }
        static void Head(Transform body,int hero,string skin,string hair)
        {
            var head=Model3D.Group("Head",body,Vector3.zero);
            Vector4[] profile={R(1.94f,.035f,.048f,-.018f),R(1.975f,.080f,.083f,-.009f),R(2.035f,.124f,.113f),R(2.10f,.153f,.125f),R(2.17f,.158f,.130f,.005f),R(2.24f,.143f,.117f,.009f),R(2.285f,.09f,.087f,.018f),R(2.305f,.005f,.010f,.025f)};
            for(int half=0;half<2;half++){
                const int rows=45,cols=49;var v=new List<Vector3>();var uv=new List<Vector2>();
                for(int y=0;y<rows;y++){float t=(float)y/(rows-1);Vector4 r=Curve(profile,t);for(int x=0;x<cols;x++){float angle=-Mathf.PI/2+x*Mathf.PI/(cols-1)+half*Mathf.PI;float xx=Mathf.Sin(angle)*r.y,z=r.w-Mathf.Cos(angle)*r.z;
                    if(half==0){float nose=Mathf.Exp(-Mathf.Pow(xx/.025f,2)-Mathf.Pow((r.x-2.07f)/.038f,2))*.022f;z-=nose;z-=Mathf.Exp(-Mathf.Pow(xx/.052f,2)-Mathf.Pow((r.x-2.014f)/.018f,2))*.005f;}
                    v.Add(new Vector3(xx,r.x,z));float u=Mathf.Clamp01(xx/.31f+.5f);float vv=Mathf.InverseLerp(1.945f,2.31f,r.x);uv.Add(new Vector2((hero+u)/3f,vv));}}
                var surface=Render(head,half==0?"Sculpted painted face":"Cranium",Grid("Head surface",v,uv,rows,cols),half==0?Face():Mat(skin));if(half==0)surface.GetComponent<MeshRenderer>().receiveShadows=false;
            }
            for(int side=-1;side<=1;side+=2){var ear=Model3D.Group("Ear",head,new Vector3(side*.154f,2.085f,.002f));Loft(ear,"Ear cartilage",new[]{R(-.045f,.012f,.015f),R(-.023f,.025f,.021f),R(.025f,.023f,.020f),R(.045f,.007f,.009f)},skin,20,16);}
            // A shaped cap, overlapping tapered locks, and individual flyaways replace bead-like hair.
            var cap=Loft(head,"Hair volume",new[]{R(2.13f,.158f,.133f,.033f),R(2.20f,.174f,.145f,.025f),R(2.29f,.142f,.134f,.016f),R(2.335f,.06f,.08f,.022f),R(2.345f,.002f,.005f,.025f)},hair,30,40);
            var capMesh=cap.GetComponent<MeshFilter>().sharedMesh;var cv=capMesh.vertices;var ct=capMesh.triangles;var keep=new List<int>();for(int i=0;i<ct.Length;i+=3){Vector3 c=(cv[ct[i]]+cv[ct[i+1]]+cv[ct[i+2]])/3;if(c.z<-.045f&&c.y<2.25f)continue;keep.Add(ct[i]);keep.Add(ct[i+1]);keep.Add(ct[i+2]);}capMesh.SetTriangles(keep,0);capMesh.RecalculateNormals();
            string highlight=hero==0?"#49352B":hero==1?"#D9B86E":"#A95631";
            for(int i=0;i<8;i++){
                float x=-.15f+i*.043f;float shift=hero==1?-.045f:.045f;float tipY=2.23f-Mathf.Pow(Mathf.Abs(x)/.17f,2)*.075f;
                var fringe=Ribbon(head,"Swept fringe",new[]{new Vector3(x*.55f-shift*.5f,2.335f,-.025f),new Vector3(x*.82f,2.31f,-.105f),new Vector3(x+shift*.4f,2.275f,-.137f),new Vector3(x+shift,tipY,-.143f)},.052f,.012f,i%3==0?highlight:hair,true);fringe.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            }
            for(int side=-1;side<=1;side+=2)for(int i=0;i<5;i++)Ribbon(head,"Side hair lock",new[]{new Vector3(side*.11f,2.30f,.01f+i*.015f),new Vector3(side*.176f,2.23f,.035f+i*.013f),new Vector3(side*(.163f+i*.003f),2.12f,.045f+i*.012f),new Vector3(side*.13f,hero==0?2.02f:2.075f,.07f+i*.012f)},.032f,.012f,i%3==0?highlight:hair,true);
            if(hero==0){for(int i=0;i<7;i++)Ribbon(head,"Loose tied ponytail",new[]{new Vector3((i-3)*.01f,2.27f,.13f),new Vector3((i-3)*.017f,2.32f,.21f),new Vector3((i-3)*.024f,2.21f,.27f),new Vector3((i-3)*.024f+.02f,2.06f,.24f)},.030f,.014f,i%3==0?highlight:hair,true);}
            if(hero==1){for(int side=-1;side<=1;side+=2){float cx=side*.077f;Ribbon(head,"Spectacle rim",new[]{new Vector3(cx-.058f,2.185f,-.135f),new Vector3(cx+.058f,2.185f,-.138f),new Vector3(cx+.059f,2.133f,-.143f),new Vector3(cx-.057f,2.133f,-.143f),new Vector3(cx-.058f,2.185f,-.135f)},.0026f,.003f,"#5D5147");Ribbon(head,"Glasses temple",new[]{new Vector3(side*.136f,2.176f,-.131f),new Vector3(side*.163f,2.171f,-.015f),new Vector3(side*.158f,2.15f,.027f)},.0025f,.003f,"#5D5147");}Ribbon(head,"Bridge",new[]{new Vector3(-.019f,2.164f,-.151f),new Vector3(0,2.170f,-.16f),new Vector3(.019f,2.164f,-.151f)},.0025f,.003f,"#5D5147");}
        }
    }
    public class GeneratedCharacterMesh:MonoBehaviour
    {
        public Mesh mesh;
        void OnDestroy(){if(mesh!=null)Destroy(mesh);}
    }
}

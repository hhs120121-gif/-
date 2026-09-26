using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SecretVirus
{
    public class WorldVisual
    {
        public Transform transform;public bool enabled {get=>transform.gameObject.activeSelf;set=>transform.gameObject.SetActive(value);}
        public WorldVisual(Transform value){transform=value;}
    }
    public class WorldThing
    {
        public string id,label,type;public Vector2 position;public WorldVisual renderer;public Rect obstacle;public bool solid=true;
        public WorldThing(string id,string label,string type,float x,float y){this.id=id;this.label=label;this.type=type;position=new Vector2(x,y);float depth=type=="bed"?.9f:type=="person"?.24f:.36f;obstacle=new Rect(x-.65f,y-depth,1.3f,depth*2);}
    }
    public class WorldView:MonoBehaviour
    {
        public const int Width=30,Height=18;
        public Camera sceneCamera;public RenderTexture target;public Transform root,player;
        public readonly List<WorldThing> things=new List<WorldThing>();public readonly List<Rect> walls=new List<Rect>();
        public Vector2 position=new Vector2(5,11),facing=Vector2.down,cameraPosition;public bool moving,running,guardTriggered;public int direction,frame;
        public float stepClock,alert,simulationTime;float footstepTimer,abilityTime;int displayedHero=-1;public GameState state;public Action Step;
        CharacterMotion3D motion;Transform vision;Showcase3D showcase;
        public static Vector3 Ground(Vector2 p)=>new Vector3(p.x,0,p.y);
        public void Initialize()
        {
            var go=new GameObject("Perspective exploration camera");go.transform.SetParent(transform);go.AddComponent<AudioListener>();sceneCamera=go.AddComponent<Camera>();sceneCamera.orthographic=false;sceneCamera.fieldOfView=46;sceneCamera.nearClipPlane=.1f;sceneCamera.farClipPlane=100;
            sceneCamera.clearFlags=CameraClearFlags.SolidColor;sceneCamera.backgroundColor=PixelArt.C("#15232D");sceneCamera.cullingMask=~((1<<28)|(1<<29));sceneCamera.allowHDR=true;
            target=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32){antiAliasing=4,filterMode=FilterMode.Bilinear,name="3D exploration 1280x720"};target.Create();sceneCamera.targetTexture=target;
            var displayObject=new GameObject("Display camera");displayObject.transform.SetParent(transform);var display=displayObject.AddComponent<Camera>();display.cullingMask=0;display.clearFlags=CameraClearFlags.SolidColor;display.backgroundColor=Color.black;display.depth=1;
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=PixelArt.C("#889EA9");RenderSettings.ambientEquatorColor=PixelArt.C("#52636C");RenderSettings.ambientGroundColor=PixelArt.C("#30383D");
            var sun=Model3D.Group("Soft key light",transform,Vector3.zero);sun.rotation=Quaternion.Euler(48,-32,0);var light=sun.gameObject.AddComponent<Light>();light.type=LightType.Directional;light.color=PixelArt.C("#FFE3BA");light.intensity=1.05f;light.shadows=LightShadows.Soft;light.shadowStrength=.72f;light.shadowBias=.04f;
            QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.High;QualitySettings.shadowDistance=45;QualitySettings.pixelLightCount=6;
            showcase=gameObject.AddComponent<Showcase3D>();showcase.Initialize();
        }
        public void Build(GameState s)
        {
            state=s;if(root!=null){root.gameObject.SetActive(false);Destroy(root.gameObject);}root=Model3D.Group("Stage "+s.stage+" 3D",transform,Vector3.zero);things.Clear();walls.Clear();guardTriggered=false;alert=0;displayedHero=-1;
            Architecture(s.stage);
            if(s.stage==1){Add("bed","간이침대","bed",4,13);Add("request","오늘의 의뢰서","desk",8,10);Add("shelf","부품 선반","shelf",24,13);Add("parts","공구 부품 ×2","crate",19,12);Add("wire","전선 ×2","crate",22,10);Add("scrap","고철 ×2","crate",26,9);Add("chip","전자 부품 ×1","crate",20,8);Add("cloth","천 ×1","crate",24,6);Add("craft","제작대","bench",14,3);Add("radio","오래된 라디오","terminal",4,7);Add("memento","덧댄 작업복","notice",5,4);Decor("lamp",3,12);Decor("rubble",9,3);}
            else if(s.stage==2){Add("sibling","동생","bed",7,12);Add("medicine","약 봉투","desk",12,12);Add("toy","고장 난 작은 조명","lamp",6,7);Decor("plant",3,13);Decor("shelf",23,12);Add("memory","오래된 사진","notice",18,7);}
            else if(s.stage==3){AddPerson("james","제임스",1,11,10);Add("supply","공급 기록","notice",22,13);Add("dispenser","고장 난 배급 설비","machine",23,7);Add("cache3","비상 구급함","crate",6,6);Decor("shelf",6,13);Decor("desk",15,4);}
            else if(s.stage==4){AddPerson("daniel","다니엘",2,8,9);Add("rescue","갇힌 주민과 잔해","rubble",15,10);Add("jump","무너진 연결 통로","vent",23,10);Add("cache4","구조 물품","crate",24,13);for(int i=0;i<4;i++)Decor("rubble",5+i*5,5);}
            else if(s.stage==5){Add("orders","검문 지침","notice",8,12);AddPerson("checkpoint","검문 담당자",4,22,10);Decor("door",24,13);Decor("crate",10,5);Decor("terminal",19,13);}
            else if(s.stage==6){Add("cell_rubble","통로의 잔해","rubble",10,10);Add("cell_gas","누출된 정화 배관","machine",18,12);Add("cell_lock","수용실 잠금 패널","terminal",24,7);Add("cell_note","벽면의 낙서","notice",5,12);Decor("bed",5,5);}
            else if(s.stage==7){AddPerson("guard","순찰 경비",4,17,10);Add("bypass","환기 제어 장치","terminal",7,12);Add("stealth_exit","우회 통로","vent",25,13);Add("cache7","보급 상자","crate",7,5);for(int i=0;i<3;i++)Decor("crate",13,6+i*2);}
            else if(s.stage==8){Add("hub","중앙 안내 단말","terminal",15,11);Add("craft","작업대","bench",7,7);Add("cache8","유지보수 보급함","crate",23,8);Add("machine","활성 보안 장치","machine",22,13);Add("rest","안전한 휴게 의자","bed",5,12);}
            else if(s.stage==9){Add("a_rubble","무너진 보관대","rubble",8,11);Add("a_record","보관 등급 기록","notice",15,12);Add("a_panel","전자 키패드","terminal",23,10);Add("a_bypass","비상 개방 장치","door",24,5);Add("shield","방패 경비","terminal",9,5);Decor("shelf",20,14);}
            else if(s.stage==10){Add("b_rules","시료 관리 일지","notice",6,12);Add("b_analyze","시료 분석기","terminal",12,12);Add("b_inject","최종 주입 장치","machine",24,10);for(int i=0;i<6;i++)Add("sample"+i,"시료 "+(char)('A'+i),"vial",6+i*3,6);Add("cache10","중화 도구 보관함","crate",19,13);}
            else if(s.stage==11){Add("patients","환자 추적 기록","notice",7,12);AddPerson("researcher","연구원 C",5,18,10);Add("c_terminal","정보 보관 단말","terminal",24,12);Add("c_backup","배급 기록 사본","desk",7,6);Decor("shelf",16,14);}
            else if(s.stage==12){Add("experiment","생체 실험 승인서","notice",8,12);Add("v06","V-06 정제 절차","terminal",21,12);Add("link","기록 연결 단말","terminal",15,7);Add("witness","남겨진 음성 기록","desk",6,5);Decor("machine",25,6);}
            else if(s.stage==13){Add("review","최종 기록 점검","terminal",15,12);Add("rest","휴식 공간","bed",6,11);Add("craft","정비 작업대","bench",8,5);Add("cache13","마지막 보급함","crate",22,7);Add("return_hub","중앙 홀로 돌아가기","vent",5,4);Decor("machine",24,13);}
            else {AddPerson("director","연구소장",3,16,11);Add("core","V-06 정제 설비","machine",24,12);Add("last_record","비공개 생산 명령","notice",7,11);for(int i=0;i<3;i++)Decor("machine",6+i*8,5);}
            Add("exit",s.stage==14?"마지막 문":"다음 구역","door",27,3);
            if(s.stage>2 && s.stage<14)Add("back","이전 구역","door",2,3);
            ChangeHero();position=new Vector2(s.x,s.y);if(Blocked(position))position=new Vector2(5,8);cameraPosition=position;UpdateView(0,false,Vector2.zero);
        }
        void Architecture(int stage)
        {
            string floor=stage<=3?"#625F50":stage==4?"#65716B":stage>=12?"#425D66":"#52656A";
            string wall=stage<=3?"#859084":"#70888C";
            Model3D.Box(root,"Foundation",15,-.23f,9,30,.4f,18,"#26383D");
            for(int x=0;x<30;x+=2)for(int z=0;z<18;z+=2)Model3D.Box(root,"Floor slab",x+1,-.04f,z+1,1.985f,.12f,1.985f,floor);
            for(int z=0;z<18;z++)for(int x=0;x<30;x++)if(z>=16||x==0||x==29||z==0)walls.Add(new Rect(x,z,1,1));
            Model3D.Box(root,"Back wall",15,1.7f,16.5f,30,3.4f,1,wall);Model3D.Box(root,"Back wall skirting",15,.28f,15.95f,30,.55f,.14f,"#324A52");
            // Cutaway side walls retain depth without obscuring the player.
            Model3D.Box(root,"Left cutaway",.5f,.35f,8.5f,1,.7f,17,wall);Model3D.Box(root,"Right cutaway",29.5f,.35f,8.5f,1,.7f,17,wall);Model3D.Box(root,"Front curb",15,.12f,.5f,30,.24f,1,"#344A51");
            for(int x=3;x<29;x+=5){
                Model3D.Box(root,"Wall pilaster",x,1.7f,15.85f,.22f,3.4f,.27f,"#4C656C");
                Model3D.Box(root,"Wall light",x+1.8f,2.8f,15.85f,1.45f,.11f,.17f,stage<4?"#FFE4AF":"#AAE4DC",true);
                Model3D.PointLight(root,new Vector3(x+1.8f,2.7f,14.5f),stage<4?"#FFD19A":"#97D8D8",2.1f,7);
                if(stage<4){Model3D.Box(root,"Window recess",x+1.8f,1.9f,15.94f,1.50f,1.12f,.09f,"#263E4D");for(int i=-1;i<=1;i++)Model3D.Box(root,"Window mullion",x+1.8f+i*.48f,1.9f,15.82f,.05f,1.12f,.06f,"#B5BCAE");}
            }
            if(stage<=3){Model3D.Box(root,"Woven living rug",5.5f,.035f,11.8f,4.5f,.015f,3.5f,"#897759");for(int i=-2;i<=2;i++)Model3D.Box(root,"Rug stripe",5.5f+i*.7f,.048f,11.8f,.06f,.006f,3.3f,"#AD9972");}
            else {for(int side=-1;side<=1;side+=2)Model3D.Box(root,"Walkway border",15,.035f,8+side*1.05f,27,.025f,.055f,"#B7A571");for(int x=3;x<28;x+=4)Model3D.Box(root,"Direction marker",x,.035f,8,.50f,.025f,.12f,"#839997");}
            for(int x=3;x<28;x+=6){var pipe=Model3D.Part(root,"Conduit",PrimitiveType.Cylinder,new Vector3(x,2.9f,15.65f),new Vector3(.09f,3,.09f),"#A09D87");pipe.localRotation=Quaternion.Euler(0,0,90);}
        }
        public WorldThing Add(string id,string label,string type,float x,float y)
        {
            var t=new WorldThing(id,label,type,x,y);t.renderer=new WorldVisual(Model3D.Prop(type,root,new Vector3(x,0,y),id.StartsWith("sample")?int.Parse(id.Substring(6)):0));
            var collider=t.renderer.transform.gameObject.AddComponent<BoxCollider>();collider.center=new Vector3(0,.65f,0);collider.size=new Vector3(t.obstacle.width,1.3f,t.obstacle.height);things.Add(t);return t;
        }
        void AddPerson(string id,string label,int hero,float x,float y)
        {
            var t=new WorldThing(id,label,"person",x,y);t.renderer=new WorldVisual(Model3D.Character(hero,root,new Vector3(x,0,y)));var c=t.renderer.transform.gameObject.AddComponent<CapsuleCollider>();c.center=new Vector3(0,1,0);c.height=2;c.radius=.3f;things.Add(t);
        }
        void Decor(string type,float x,float y){Add("decor_"+things.Count,"",type,x,y);}
        void ChangeHero()
        {
            if(displayedHero==state.leader&&player!=null)return;if(player!=null){player.gameObject.SetActive(false);Destroy(player.gameObject);}player=Model3D.Character(state.leader,root,Ground(position));motion=player.GetComponent<CharacterMotion3D>();displayedHero=state.leader;
        }
        public bool Blocked(Vector2 p)
        {
            var r=new Rect(p.x-.23f,p.y-.10f,.46f,.24f);foreach(var wall in walls)if(r.Overlaps(wall))return true;
            foreach(var t in things)if(t.solid && t.renderer.enabled && r.Overlaps(t.obstacle))return true;return false;
        }
        public void Move(Vector2 input,bool run,float delta)
        {
            simulationTime+=delta;
            // Preserves the supplied PlayerController's normalized 5 / 8 movement and cardinal facing.
            input=Vector2.ClampMagnitude(input,1);moving=input.sqrMagnitude>.001f;running=run;
            if(moving){facing=Mathf.Abs(input.x)>Mathf.Abs(input.y)?new Vector2(Mathf.Sign(input.x),0):new Vector2(0,Mathf.Sign(input.y));direction=facing.y<0?0:facing.y>0?1:facing.x<0?2:3;}
            Vector2 offset=input*(run?8:5)*Mathf.Min(delta,.05f);int steps=Mathf.Max(1,Mathf.CeilToInt(offset.magnitude/.12f));offset/=steps;
            for(int i=0;i<steps;i++){var next=position+new Vector2(offset.x,0);if(!Blocked(next))position=next;next=position+new Vector2(0,offset.y);if(!Blocked(next))position=next;}
            if(moving){stepClock+=delta*(run?11:7);footstepTimer+=delta;if(footstepTimer>(run?.21f:.34f)){footstepTimer=0;Step?.Invoke();}}
            frame=moving?(int)stepClock%4:0;state.x=position.x;state.y=position.y;
        }
        public void UpdateView(float dt,bool fixedCamera,Vector2 fixedPoint)
        {
            if(root==null)return;ChangeHero();player.position=Ground(position);Quaternion turn=Quaternion.LookRotation(new Vector3(-facing.x,0,-facing.y));player.rotation=Quaternion.Slerp(player.rotation,turn,dt<=0?1:1-Mathf.Exp(-dt*16));motion.Pose(stepClock*1.6f,moving,abilityTime);abilityTime=Mathf.Max(0,abilityTime-dt);
            Vector2 focus=fixedCamera?fixedPoint:position;Vector2 desired=new Vector2(Mathf.Clamp(focus.x,6,24),Mathf.Clamp(focus.y,4,13));cameraPosition=dt<=0?desired:Vector2.Lerp(cameraPosition,desired,1-Mathf.Exp(-dt*8));
            Vector3 look=Ground(cameraPosition)+Vector3.up*.85f;sceneCamera.transform.position=look+new Vector3(0,7.8f,-7.3f);sceneCamera.transform.LookAt(look);
            foreach(var t in things){
                if(t.id=="guard"&&!state.Has("guard_done")){float x=17+Mathf.Sin(simulationTime*.5f)*3;t.position=new Vector2(x,10);t.obstacle=new Rect(x-.5f,9.76f,1,.48f);t.renderer.transform.position=Ground(t.position);t.renderer.transform.rotation=Quaternion.Euler(0,Mathf.Cos(simulationTime*.5f)>0?-90:90,0);}
                bool hide=state.Has("collected_"+t.id)||(t.id=="a_rubble"&&state.Has("a_clear"))||(t.id=="cell_rubble"&&state.Has("cell_clear"))||(t.id=="rescue"&&state.Has("rescued"));t.renderer.enabled=!hide;
                var character=t.renderer.transform.GetComponent<CharacterMotion3D>();if(character!=null)character.Pose(simulationTime*7,t.id=="guard"&&!state.Has("guard_done"));
            }
            UpdateVision();
            var game=VirusGame.Instance;if(game!=null)showcase.Present(game.Mode,game.Battle,state.leader);
        }
        void UpdateVision()
        {
            if(state.stage!=7)return;var guard=things.Find(t=>t.id=="guard");if(guard==null)return;
            if(vision==null){vision=Model3D.Group("Patrol ground cone",root,Vector3.zero);var mf=vision.gameObject.AddComponent<MeshFilter>();var mesh=new Mesh();mesh.vertices=new[]{Vector3.zero,new Vector3(4,0,-1.4f),new Vector3(4,0,1.4f)};mesh.triangles=new[]{0,2,1};mesh.RecalculateNormals();mf.mesh=mesh;var mr=vision.gameObject.AddComponent<MeshRenderer>();mr.sharedMaterial=Model3D.Material("#9B8443");mr.shadowCastingMode=ShadowCastingMode.Off;}
            vision.gameObject.SetActive(!state.Has("guard_done"));vision.position=Ground(guard.position)+Vector3.up*.055f;vision.rotation=Quaternion.Euler(0,Mathf.Cos(simulationTime*.5f)>0?0:180,0);
        }
        public void WakePose(bool lying){player.position=lying?new Vector3(4,.86f,13):new Vector3(5,0,12);player.rotation=lying?Quaternion.Euler(-90,0,0):Quaternion.identity;}
        public RenderTexture Display(GameMode mode)=>mode==GameMode.Title||mode==GameMode.Battle||mode==GameMode.Ending?showcase.target:target;
        public Texture Portrait(int hero)=>showcase.Portrait(hero);
        public void CombatPose(BattleAction action){showcase.PlayAction(action);}
        public WorldThing Nearest()
        {
            WorldThing best=null;float score=float.MaxValue;
            foreach(var t in things){if(t.label==""||!t.renderer.enabled)continue;Vector2 delta=t.position-position;float d=delta.magnitude;
                if(d>1.05f || Vector2.Dot(delta.normalized,facing)<.45f)continue;
                bool blocked=false;for(float f=.15f;f<d-.6f;f+=.2f){Vector2 point=position+delta.normalized*f;foreach(var w in walls)if(w.Contains(point))blocked=true;foreach(var other in things)if(other!=t&&other.solid&&other.renderer.enabled&&other.obstacle.Contains(point))blocked=true;}
                if(!blocked&&d<score){best=t;score=d;}
            }return best;
        }
        public Vector2 ScreenPoint(Vector2 point){Vector3 p=sceneCamera.WorldToViewportPoint(new Vector3(point.x,1.25f,point.y));return new Vector2(p.x*1280,(1-p.y)*720);}
        public bool ClearSight(WorldThing observer,Vector2 targetPoint)
        {
            Vector2 delta=targetPoint-observer.position;float distance=delta.magnitude;
            for(float f=.3f;f<distance;f+=.15f){Vector2 p=observer.position+delta.normalized*f;foreach(var wall in walls)if(wall.Contains(p))return false;foreach(var t in things)if(t!=observer&&t.solid&&t.renderer.enabled&&t.obstacle.Contains(p))return false;}return true;
        }
        public void PlayAbility(int hero){abilityTime=.85f;}
        void OnDestroy(){if(target!=null){target.Release();Destroy(target);}}
    }
}

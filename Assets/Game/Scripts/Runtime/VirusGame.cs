using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SecretVirus
{
    public partial class VirusGame:MonoBehaviour
    {
        public static VirusGame Instance {get;private set;}
        public GameState State=new GameState(); public GameMode Mode=GameMode.Title; public Preferences Settings;
        public WorldView World; [NonSerialized] public CombatSession Battle;
        GameAudio sound;Font font;GameMode returnMode=GameMode.Field,settingsReturn=GameMode.Menu;
        GameState battleCheckpoint;CraftSession craft;Recipe chosenRecipe=Catalog.Repair;
        string craftHeld="",toast="",dialogSpeaker="",battleId="",endingCode="",binding="";float toastTime,dialogStart,openingStart,confirmHeld=-1,interactionBuffer=-1,idleTime,playClock,sceneFade;
        List<string> dialogLines=new List<string>();int dialogIndex,dialogHero=-1,openingIndex,menuFocus,inventoryTab,selectedItem,selectedClue,puzzleAttempts,debateStep,debateScore;
        bool textComplete;int[] aSymbols=new int[3];bool[] eliminated=new bool[6];int selectedSample;
        float wakeUntil,actionPulse; Vector2 pulsePosition;
        List<Choice> dialogChoices=new List<Choice>();Action dialogDone,pendingAction;
        readonly List<UiButton> buttons=new List<UiButton>();
        public string LastError="";public bool Ready {get;private set;}
        class Choice { public string text;public Action action;public Choice(string text,Action action){this.text=text;this.action=action;} }
        class UiButton {public string text;public Action action;public bool enabled;public Rect rect;}

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap(){if(FindAnyObjectByType<VirusGame>()==null)new GameObject("The Secret of Virus").AddComponent<VirusGame>();}
        void Awake()
        {
            if(Instance!=null){Destroy(gameObject);return;}Instance=this;DontDestroyOnLoad(gameObject);Application.targetFrameRate=60;Application.runInBackground=true;
            Settings=SaveStore.ReadPreferences();font=Resources.Load<Font>("NotoSansKR-Regular");if(font==null)font=Font.CreateDynamicFontFromOSFont(new[]{"Malgun Gothic","Arial"},24);
            World=new GameObject("World").AddComponent<WorldView>();World.transform.SetParent(transform);World.Initialize();World.Build(State);World.Step=()=>sound?.Play("step");
            sound=gameObject.AddComponent<GameAudio>();sound.Initialize(Settings);Ready=true;
            if(Environment.GetCommandLineArgs().Contains("--smoke"))StartCoroutine(RuntimeSmoke());
        }
        void OnDestroy(){if(Instance==this)Instance=null;}
        void Update()
        {
            if(!Ready)return;float dt=Time.unscaledDeltaTime;toastTime-=dt;sceneFade=Mathf.Max(0,sceneFade-dt*2);
            if(diagnosticRunning){World.UpdateView(dt,false,Vector2.zero);return;}
            if(BattlePresenting){if(!impactPlayed&&BattleProgress>=.57f){impactPlayed=true;if(State.hp<previousPartyHp)sound.Play("hit");}World.UpdateView(dt,false,Vector2.zero);return;}
            if(pendingAction!=null){Action action=pendingAction;pendingAction=null;action();return;}
            if(binding!=""){foreach(KeyCode code in Enum.GetValues(typeof(KeyCode)))if(Input.GetKeyDown(code)){if(code==KeyCode.Escape){binding="";break;}if(code>=KeyCode.Mouse0||code==KeyCode.None||code==KeyCode.UpArrow||code==KeyCode.DownArrow||code==KeyCode.LeftArrow||code==KeyCode.RightArrow||code==KeyCode.Return||code==KeyCode.Alpha1||code==KeyCode.Alpha2||code==KeyCode.Alpha3||code==KeyCode.Tab)break;Bind(code);break;}return;}
            if(Mode!=GameMode.Title&&Mode!=GameMode.Ending&&Mode!=GameMode.Gallery&&Mode!=GameMode.Menu&&Mode!=GameMode.Settings){State.playSeconds+=dt;}
            actionPulse=Mathf.Max(0,actionPulse-dt);
            if(wakeUntil>Time.unscaledTime){World.UpdateView(dt,true,new Vector2(5,12));bool lying=wakeUntil-Time.unscaledTime>.7f;World.WakePose(lying);return;}
            if(Mode==GameMode.Field){UpdateField(dt);}
            else if(Mode==GameMode.Opening){
                if(Input.GetKeyDown(KeyCode.Escape)){WakeUp();return;}
                if(ConfirmDown()){if(!textComplete){textComplete=true;}else AdvanceOpening();}
                if(ConfirmHeld()){if(confirmHeld<0)confirmHeld=Time.unscaledTime;if(Time.unscaledTime-confirmHeld>1.2f)WakeUp();}else confirmHeld=-1;
            }
            else{
                if(Input.GetKeyDown(KeyCode.Escape)){Back();return;}
                if(Mode==GameMode.Battle && Battle!=null&&Battle.CanSwitch)SwitchInput();
                if(CancelDown()){Back();return;}
                if(Input.GetKeyDown(KeyCode.UpArrow)||Input.GetKeyDown(KeyCode.LeftArrow))MoveFocus(-1);
                if(Input.GetKeyDown(KeyCode.DownArrow)||Input.GetKeyDown(KeyCode.RightArrow))MoveFocus(1);
                if(ConfirmDown()){
                    if(Mode==GameMode.Dialogue&&!textComplete){textComplete=true;return;}
                    if(buttons.Count>0){menuFocus=Mathf.Clamp(menuFocus,0,buttons.Count-1);var b=buttons[menuFocus];if(b.enabled)b.action?.Invoke();else sound.Play("error");}
                }
            }
            World.UpdateView(dt,Mode==GameMode.Opening, new Vector2(5,11));
        }
        bool ConfirmDown(){return Input.GetKeyDown((KeyCode)Settings.interact)||Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter);}
        bool ConfirmHeld(){return Input.GetKey((KeyCode)Settings.interact)||Input.GetKey(KeyCode.Return);}
        bool CancelDown(){return Input.GetKeyDown((KeyCode)Settings.cancel)||Input.GetKeyDown(KeyCode.LeftShift)||Input.GetKeyDown(KeyCode.RightShift);}
        void MoveFocus(int d){if(buttons.Count==0)return;for(int i=0;i<buttons.Count;i++){menuFocus=(menuFocus+d+buttons.Count)%buttons.Count;if(buttons[menuFocus].enabled)break;}sound.Play("tick");}
        void SetMode(GameMode mode){Mode=mode;menuFocus=0;buttons.Clear();World.moving=false;World.frame=0;}
        void UpdateField(float dt)
        {
            if(Input.GetKeyDown(KeyCode.Escape)){returnMode=Mode;SetMode(GameMode.Menu);return;}
            if(Input.GetKeyDown((KeyCode)Settings.inventory)||Input.GetKeyDown(KeyCode.Tab)){SetMode(GameMode.Inventory);return;}
            SwitchInput();Vector2 move=new Vector2((Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.LeftArrow)?1:0),(Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.DownArrow)?1:0));
            bool running=Input.GetKey((KeyCode)Settings.cancel)||Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift);World.Move(move,running,dt);
            if(move.sqrMagnitude>0)idleTime=0;else idleTime+=dt;
            if(ConfirmDown())interactionBuffer=Time.unscaledTime+.15f;
            if(!World.moving&&interactionBuffer>=Time.unscaledTime){interactionBuffer=-1;var target=World.Nearest();if(target!=null){idleTime=0;Interact(target.id);}else Toast("물건 가까이에서 바라본 뒤 조사하세요.");}
            if(State.stage==7&&!State.Has("guard_done")&&!World.guardTriggered){var guard=World.things.Find(t=>t.id=="guard");if(guard!=null){Vector2 d=World.position-guard.position;bool sight=Mathf.Abs(d.y)<.35f*Mathf.Abs(d.x)&&Mathf.Abs(d.x)<4&&((Mathf.Cos(World.simulationTime*.5f)>0&&d.x>0)||(Mathf.Cos(World.simulationTime*.5f)<=0&&d.x<0))&&World.ClearSight(guard,World.position);if(State.Has("bypass_open")&&World.position.y>12)sight=false;World.alert=Mathf.Clamp01(World.alert+dt*(sight?(running?.75f:.35f):-.55f));if(World.alert>=1){World.guardTriggered=true;StartBattle("guard");}}}
        }
        void SwitchInput(){if(Input.GetKeyDown((KeyCode)Settings.switchHero))SwitchHero((State.leader+1)%State.joined);for(int i=0;i<3;i++)if(Input.GetKeyDown(KeyCode.Alpha1+i))SwitchHero(i);}
        public bool SwitchHero(int hero)
        {
            if(hero<0||hero>=State.joined){Toast("아직 합류하지 않은 동료입니다.");return false;}
            if(Mode!=GameMode.Field && !(Mode==GameMode.Battle&&Battle!=null&&Battle.CanSwitch))return false;
            State.leader=hero;sound.Play("switch");Toast(Catalog.Heroes[hero]+" · "+Catalog.Jobs[hero]);return true;
        }
        void Bind(KeyCode code)
        {
            int c=(int)code;if(c==Settings.interact||c==Settings.cancel||c==Settings.switchHero||c==Settings.inventory){Toast("이미 사용 중인 키입니다.");return;}
            if(binding=="interact")Settings.interact=c;if(binding=="cancel")Settings.cancel=c;if(binding=="switch")Settings.switchHero=c;if(binding=="inventory")Settings.inventory=c;binding="";PersistSettings();
        }
        void PersistSettings(){sound.Apply();if(!SaveStore.WritePreferences(Settings))Toast("환경 설정 저장에 실패했습니다.");}
        public void NewGame()
        {
            State=new GameState();World.Build(State);openingIndex=0;openingStart=Time.unscaledTime;textComplete=false;confirmHeld=-1;SetMode(GameMode.Opening);sound.SetTheme(0);sceneFade=1;
        }
        void AdvanceOpening(){openingIndex++;if(openingIndex>=Catalog.Opening.Length){WakeUp();return;}openingStart=Time.unscaledTime;textComplete=false;}
        void WakeUp(){State.tutorial=Mathf.Max(1,State.tutorial);wakeUntil=Time.unscaledTime+1.4f;World.position=new Vector2(5,11);SetMode(GameMode.Field);sound.Play("bed");Say("다은",0,new[]{"오늘의 의뢰를 확인해볼까나.","방향키로 이동하고, 물건을 바라본 뒤 멈춰서 Z / Enter로 조사한다.\n책상 위에 오늘의 의뢰서가 있다."},()=>{State.tutorial=2;SetMode(GameMode.Field);});}
        void Say(string speaker,int hero,IEnumerable<string> lines,Action done=null,params Choice[] choices)
        {
            dialogSpeaker=speaker;dialogHero=hero;dialogLines=lines.ToList();dialogIndex=0;dialogStart=Time.unscaledTime;textComplete=false;dialogDone=done;dialogChoices=choices.ToList();SetMode(GameMode.Dialogue);
        }
        void NextDialogue()
        {
            if(!textComplete){textComplete=true;return;}
            if(dialogIndex<dialogLines.Count-1){dialogIndex++;dialogStart=Time.unscaledTime;textComplete=false;return;}
            var done=dialogDone;dialogDone=null;SetMode(GameMode.Field);done?.Invoke();
        }
        void Info(string message,int hero=-1){Say(hero>=0?Catalog.Heroes[hero]:"조사",hero,new[]{message});}
        void Toast(string message){toast=message;toastTime=4;}
        bool RequireHero(int hero){if(State.leader==hero){World.PlayAbility(hero);return true;}Info(Catalog.Heroes[hero]+"의 전문성이 필요하다.\n"+(hero+1)+"번 키 또는 C로 리더를 바꿔 보자.");return false;}
        void Learn(string id,int hero=-1)
        {
            State.Clue(id);var clue=Catalog.Clues[id];sound.Play("paper");Say(clue.title,hero,new[]{clue.text,"해석 · "+clue.interpretation+"\n\n기록창에서 다시 읽을 수 있다."});
        }
        bool Grant(string source,params ItemStack[] items)
        {
            if(State.Has("collected_"+source)){Info("이미 필요한 물품을 챙겼다.");return false;}
            var copy=State.Clone();foreach(var item in items)if(!Inventory.Add(copy,item.id,item.count)){Info("인벤토리에 공간이 부족하다. 물품은 이곳에 남겨 두었다.");return false;}
            State.items=copy.items;State.Flag("collected_"+source);sound.Play("pickup");Toast(string.Join("  ·  ",items.Select(i=>Catalog.ItemName(i.id)+" ×"+i.count)));return true;
        }
        public void EnterStage(int stage,bool fresh=true)
        {
            if(stage<1||stage>14)return;State.stage=stage;State.highestStage=Mathf.Max(stage,State.highestStage);if(fresh){State.x=5;State.y=8;}
            World.Build(State);SetMode(GameMode.Field);sceneFade=1;sound.SetTheme(stage<5?0:1);
            if(State.tutorial>=5)Save("auto",false);
            if(State.Flag("visited_"+stage))Say(Catalog.Stages[stage].title,-1,new[]{Catalog.Stages[stage].intro});
        }
        public void LoadGame(string slot)
        {
            string error;var loaded=SaveStore.Read(slot,out error);if(loaded==null){Toast(error);return;}State=loaded;EnterStage(State.stage,false);if(error!="")Toast(error);else Toast("저장된 여정을 불러왔습니다.");
        }
        void Save(string slot,bool announce=true)
        {
#if UNITY_EDITOR
            if(verificationRunning)return;
#endif
            if(State.tutorial<5){Toast("튜토리얼 완료 전에는 저장할 수 없습니다.");return;}State.x=World.position.x;State.y=World.position.y;
            if(!SaveStore.Write(State,slot,out var error)){LastError=error;Toast(error);}else if(announce)Toast("저장했습니다.");
        }
        bool CanExit()
        {
            switch(State.stage){case 1:return State.tutorial>=5;case 2:return State.Has("sibling_talk")&&State.clues.Contains("medicine");case 3:return State.Has("dispenser_fixed")&&State.clues.Contains("supply")&&State.joined>=2;case 4:return State.Has("rescued");case 5:return State.Has("captured");case 6:return State.Has("cell_open");case 7:return State.Has("guard_done");case 8:return State.Has("hub_read");case 9:return State.Has("a_open")||State.Has("a_bypass");case 10:return State.Has("b_done");case 11:return State.fragments.Contains("C");case 12:return State.Has("records_linked");case 13:return State.Has("reviewed");default:return false;}
        }
        void ExitStage()
        {
            if(!CanExit()){Info("아직 이곳에서 확인할 일이 남아 있다.\n"+Objective());return;}
            if(State.stage==13){Say("마지막 문",-1,new[]{"이 문을 넘으면 연구소장이 기다린다. 이전 구역으로 돌아갈 수 없다.\n\n열쇠 조각 "+State.fragments.Count+" / 3\n비살상 해결 "+State.Ratio+"\n\n지금의 여정을 자동 저장한다."},null,new Choice("준비됐다 · 소장실로",()=>{Save("before-final",false);EnterStage(14);Save("before-final",false);}),new Choice("조금 더 준비한다",()=>SetMode(GameMode.Field)));return;}
            EnterStage(State.stage+1);
        }
        string Objective()
        {
            if(State.stage==1)return State.tutorial<3?"책상의 의뢰서를 확인하자":State.tutorial==3?"공방에서 필요한 재료 5종을 모으자":State.tutorial==4?"제작대에서 수리 키트를 완성하자":"출입문으로 나가 동생을 만나자";
            return CurrentTask();
        }
        void Back()
        {
            switch(Mode){
                case GameMode.Title:return;
                case GameMode.Menu:SetMode(returnMode);break;
                case GameMode.Settings:PersistSettings();SetMode(settingsReturn);break;
                case GameMode.Inventory:SetMode(GameMode.Field);break;
                case GameMode.Crafting:if(craftHeld!=""){craftHeld="";Toast("재료 선택을 취소했다.");}else{int slot=menuFocus-(State.stage==1?0:3);if(slot>=0&&slot<5&&craft.slots[slot]!=null)CraftPlace(slot);else ConfirmLeaveCraft();}break;
                case GameMode.PuzzleA:case GameMode.PuzzleB:case GameMode.Debate:SetMode(GameMode.Field);break;
                case GameMode.Battle:returnMode=Mode;SetMode(GameMode.Menu);break;
                case GameMode.Dialogue:Toast("중요한 대화는 확인 키로 진행하세요.");break;
                case GameMode.Gallery:SetMode(GameMode.Title);break;
                case GameMode.Ending:SetMode(GameMode.Title);break;
                default:returnMode=Mode;SetMode(GameMode.Menu);break;
            }
        }
        void OpenSettings(){settingsReturn=Mode;SetMode(GameMode.Settings);}
        void QuitPrompt(){var previous=Mode;Say("게임 종료",-1,new[]{State.tutorial<5?"튜토리얼 완료 전 진행은 저장되지 않습니다. 종료할까요?":"마지막 저장 이후 진행은 사라집니다. 종료할까요?"},null,new Choice("종료한다",()=>{
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }),new Choice("계속한다",()=>SetMode(previous)));}
    }
}

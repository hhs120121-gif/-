#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SecretVirus
{
    public partial class VirusGame
    {
        public string VerificationState(){return JsonUtility.ToJson(new Snapshot{mode=Mode.ToString(),stage=State.stage,tutorial=State.tutorial,hp=State.hp,joined=State.joined,fragments=State.fragments.Count,incidents=State.Total,leader=State.leader,position=World.position.ToString(),error=LastError});}
        [Serializable]class Snapshot{public string mode,position,error;public int stage,tutorial,hp,joined,fragments,incidents,leader;}
        public void VerificationScenario(string name)
        {
            if(name=="title"){SetMode(GameMode.Title);return;}
            if(name=="new"){NewGame();return;}
            State=new GameState{tutorial=5,joined=3,stage=8,highestStage=14,hp=100};foreach(var key in Catalog.Clues.Keys)State.Clue(key);State.Fragment("A");State.Fragment("B");State.Record("guard",Resolution.Persuaded);
            foreach(string item in Catalog.Items.Keys)Inventory.Add(State,item,6);
            if(name=="field"||name=="tutorial"){State.stage=1;State.joined=1;State.tutorial=3;State.x=13;State.y=9;World.Build(State);SetMode(GameMode.Field);}
            else if(name=="craft"){State.stage=1;State.tutorial=4;World.Build(State);chosenRecipe=Catalog.Repair;OpenCraft();}
            else if(name=="battle"){State.stage=14;World.Build(State);StartBattle("boss");}
            else if(name=="inventory"){World.Build(State);inventoryTab=2;SetMode(GameMode.Inventory);}
            else if(name=="puzzle"){State.stage=10;World.Build(State);SetMode(GameMode.PuzzleB);}
            else if(name=="dialogue"){World.Build(State);Say("다은",0,new[]{"고장 난 건 고치면 돼. 하지만 이곳에서 망가진 건 기계만이 아니었어.\n동생을 구하러 왔지만, 이제 그냥 돌아갈 수는 없어."});textComplete=true;}
            else if(name=="ending"){State.Fragment("C");endingCode="A";SetMode(GameMode.Ending);}
            toastTime=0;sceneFade=0;World.UpdateView(0,false,Vector2.zero);
        }
        public string VerifyJourney()
        {
            var originalState=State;var originalMode=Mode;var originalSettings=Settings;var results=new List<string>();
            // Campaign interactions are exercised through the same runtime handlers as player input.
            // Saves are redirected by the guard below in editor verification, never the player's slots.
            verificationRunning=true;
            try{
                State=new GameState();World.Build(State);WakeUp();Drain();CheckJourney(State.tutorial==2,"opening / waking",results);
                Interact("request");Drain();foreach(string part in Catalog.Repair.parts){Interact(part);Drain();}CheckJourney(State.tutorial==4,"collect five types",results);
                Interact("craft");for(int i=0;i<5;i++){craftHeld=Catalog.Repair.parts[i];CraftPlace(i);}Drain();CheckJourney(State.tutorial==5&&Inventory.Count(State,"repair")==1,"craft tutorial completes",results);
                ExitStage();Drain();Interact("sibling");Drain();Interact("medicine");Drain();ExitStage();Drain();CheckJourney(State.stage==3,"family motivation and medicine clue",results);
                Interact("james");Drain();State.leader=0;Interact("dispenser");Drain();State.leader=1;Interact("supply");Drain();Interact("cache3");Drain();ExitStage();Drain();CheckJourney(State.stage==4&&State.joined==2,"pharmacist and repair reward",results);
                Interact("daniel");Drain();State.leader=2;Interact("rescue");Drain();Interact("cache4");Drain();ExitStage();Drain();Interact("orders");Drain();Interact("checkpoint");Drain();CheckJourney(State.stage==6&&State.joined==3,"athlete rescue and capture",results);
                State.leader=2;Interact("cell_rubble");Drain();State.leader=1;Interact("cell_gas");Drain();State.leader=0;Interact("cell_lock");Drain();ExitStage();Drain();CheckJourney(State.stage==7,"three hero cell escape",results);
                StartBattle("guard");WinNonlethal();FinishBattle();Drain();ExitStage();Drain();Interact("hub");Drain();Interact("cache8");Drain();ExitStage();Drain();CheckJourney(State.stage==9,"encounter and hub",results);
                State.leader=2;Interact("a_rubble");Drain();State.leader=1;Interact("a_record");Drain();State.leader=0;Interact("a_panel");SubmitA(new[]{0,1,2});Drain();ExitStage();Drain();CheckJourney(State.fragments.Contains("A")&&State.stage==10,"A cooperation and key",results);
                State.leader=1;Interact("b_analyze");Drain();Interact("cache10");Drain();Inject(5);Drain();WinNonlethal();FinishBattle();Drain();ExitStage();Drain();CheckJourney(State.fragments.Contains("B")&&State.stage==11,"B deduction and weakened battle",results);
                State.leader=1;Interact("patients");Drain();Interact("researcher");DebateAnswer(true);DebateAnswer(true);DebateAnswer(true);Drain();State.leader=0;Interact("c_terminal");Drain();ExitStage();Drain();CheckJourney(State.fragments.Contains("C")&&State.stage==12,"C peaceful evidence route",results);
                Interact("experiment");Drain();State.leader=1;Interact("v06");Drain();Interact("link");Drain();ExitStage();Drain();Interact("review");Drain();Interact("rest");Drain();Interact("cache13");Drain();CheckJourney(State.stage==13&&State.Has("records_linked"),"truth and preparation",results);
                EnterStage(14);Drain();StartBattle("boss");WinNonlethal();CheckJourney(State.DetermineEnding()=="A","full journey to ending A",results);
                State.offerAccepted=true;CheckJourney(State.DetermineEnding()=="B","ending B branch",results);State.offerAccepted=false;State.fragments.Remove("B");CheckJourney(State.DetermineEnding()=="C","ending C branch",results);State.bossWon=false;State.bossLost=true;CheckJourney(State.DetermineEnding()=="D","ending D branch",results);
                return "PASS "+results.Count+" campaign checkpoints\n"+string.Join("\n",results);
            }finally{verificationRunning=false;State=originalState;Settings=originalSettings;World.Build(State);SetMode(originalMode);Battle=null;}
        }
        void CheckJourney(bool condition,string name,List<string> results){if(!condition)throw new Exception("Campaign failed: "+name+" / "+VerificationState());results.Add(name);}
        void Drain(){int limit=50;while(Mode==GameMode.Dialogue&&dialogChoices.Count==0&&limit-->0){textComplete=true;NextDialogue();}}
        void WinNonlethal(){State.leader=2;int limit=10;while(Battle!=null&&!Battle.finished&&limit-->0){State.hp=100;BattleAct(BattleAction.Subdue);}if(Battle==null||!Battle.won)throw new Exception("Verification battle failed");}
        public string VerifyTraversal()
        {
            var originalState=State;var originalMode=Mode;int targets=0;try{
                for(int stage=1;stage<=14;stage++){
                    State=new GameState{stage=stage,highestStage=stage,tutorial=5,joined=3,x=5,y=8};World.Build(State);
                    var reached=new HashSet<Vector2Int>();var queue=new Queue<Vector2Int>();var start=new Vector2Int(10,16);queue.Enqueue(start);reached.Add(start);
                    Vector2Int[] directions={Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};
                    while(queue.Count>0){var point=queue.Dequeue();foreach(var d in directions){var next=point+d;if(next.x<2||next.x>57||next.y<2||next.y>31||reached.Contains(next)||World.Blocked((Vector2)next*.5f))continue;reached.Add(next);queue.Enqueue(next);}}
                    foreach(var thing in World.things.Where(t=>t.label!="")){
                        bool accessible=false;foreach(var point in reached){Vector2 p=(Vector2)point*.5f,delta=thing.position-p;if(delta.magnitude>1.05f||delta.magnitude<.1f)continue;World.position=p;World.facing=Mathf.Abs(delta.x)>Mathf.Abs(delta.y)?new Vector2(Mathf.Sign(delta.x),0):new Vector2(0,Mathf.Sign(delta.y));if(World.Nearest()==thing){accessible=true;break;}}
                        if(!accessible)throw new Exception("Unreachable interactable: stage "+stage+" / "+thing.id);targets++;
                    }
                }
                string result="PASS all 14 maps / "+targets+" interactables have collision-free routes and facing-based interaction positions.";System.IO.File.WriteAllText("Tools/traversal-verification.txt",result);return result;
            }finally{State=originalState;World.Build(State);SetMode(originalMode);}
        }
        public string VerifyBranches()
        {
            var original=State;var mode=Mode;verificationRunning=true;var checks=new List<string>();try{
                State=new GameState{tutorial=5,joined=3,stage=9,highestStage=9,leader=2};World.Build(State);SetMode(GameMode.Field);Interact("a_bypass");dialogChoices[0].action();CheckJourney(CanExit()&&!State.fragments.Contains("A"),"A bypass progresses without key",checks);
                State.leader=2;Interact("a_rubble");Drain();State.leader=1;Interact("a_record");Drain();State.leader=0;SubmitA(new[]{0,1,2});Drain();CheckJourney(State.fragments.Contains("A"),"A key recoverable after bypass",checks);
                EnterStage(10);Drain();Inject(0);Drain();CheckJourney(State.poison&&!State.fragments.Contains("B")&&Battle!=null,"B wrong sample poisons and starts fight",checks);WinNonlethal();FinishBattle();Drain();CheckJourney(CanExit()&&!State.fragments.Contains("B"),"B failed puzzle does not softlock progression",checks);
                State.stage=11;World.Build(State);SetMode(GameMode.Field);StartBattle("c");WinNonlethal();FinishBattle();Drain();CheckJourney(State.fragments.Contains("C")&&State.incidents.Count(i=>i.id=="c")==1,"C combat key and single record",checks);
                State.stage=7;State.hp=3;State.poison=false;State.flags.Remove("guard_done");World.Build(State);SetMode(GameMode.Field);Inventory.Add(State,"med",1);int medBefore=Inventory.Count(State,"med");StartBattle("guard");State.leader=1;BattleAct(BattleAction.Attack);CheckJourney(Battle.finished&&!Battle.won,"ordinary party defeat",checks);FinishBattle();CheckJourney(State.hp==3&&Inventory.Count(State,"med")==medBefore&&!State.incidents.Any(i=>i.id=="guard"),"retry restores checkpoint without counting defeat",checks);
                State=new GameState{tutorial=5,joined=3,stage=14,highestStage=14,hp=1,leader=1};World.Build(State);StartBattle("boss");BattleAct(BattleAction.Attack);CheckJourney(State.DetermineEnding()=="D","actual final defeat routes to D",checks);
                State=new GameState{tutorial=5,joined=3,stage=8,highestStage=8};World.Build(State);SetMode(GameMode.Inventory);Inventory.Add(State,"unlock",1);World.position=new Vector2(5,8);UseInventoryItem("unlock");CheckJourney(Inventory.Count(State,"unlock")==1&&!State.Has("bonus_open"),"remote unlock cannot consume or award items",checks);
                World.position=new Vector2(23,7);UseInventoryItem("unlock");CheckJourney(State.Has("bonus_open")&&Inventory.Count(State,"unlock")==0&&Inventory.Count(State,"trap")==3,"nearby maintenance stash opens atomically",checks);UseInventoryItem("unlock");CheckJourney(Inventory.Count(State,"trap")==3,"maintenance reward cannot repeat",checks);
                State.stage=6;State.flags.Clear();CheckJourney(CurrentTask().Contains("다니엘"),"escape objective starts with rubble",checks);State.Flag("cell_clear");CheckJourney(CurrentTask().Contains("제임스"),"escape objective advances to gas",checks);State.Flag("cell_safe");CheckJourney(CurrentTask().Contains("다은"),"escape objective advances to lock",checks);State.Flag("cell_open");CheckJourney(CurrentTask().Contains("다음 구역"),"completed objective directs to exit",checks);
                string result="PASS "+checks.Count+" alternate-path checks\n"+string.Join("\n",checks);System.IO.File.WriteAllText("Tools/branches-verification.txt",result);return result;
            }finally{verificationRunning=false;State=original;World.Build(State);Battle=null;SetMode(mode);}
        }
        bool verificationRunning;
    }
}
#endif

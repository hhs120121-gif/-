using System;
using System.Linq;
using UnityEngine;

namespace SecretVirus
{
    public partial class VirusGame
    {
        public void Interact(string id)
        {
            if(Mode!=GameMode.Field)return;sound.Play("tick");
            if(id=="exit"){ExitStage();return;}if(id=="back"){if(State.stage==6){Info("수용 구역의 문은 뒤에서 잠겼다.");return;}EnterStage(Mathf.Max(2,State.stage-1));return;}
            if(id=="craft"){OpenCraft();return;}
            if(id=="rest"){State.hp=100;State.poison=false;Save("auto",false);Info("잠시 숨을 고른다.\n파티 HP가 회복되고 중독이 해제됐다.\n\n살려 둔 사람들의 흔적이, 아직 갈 수 있는 길이 있음을 알려 준다.");return;}
            switch(id){
                case "bed":Info("낡은 간이침대. 기름 냄새가 이불에까지 배어 있다.\n그래도 하루가 끝나면 돌아올 곳이다.",0);break;
                case "request":
                    if(State.tutorial>=3){Info("의뢰품 · 수리 키트\n공구 부품 2 / 전선 2 / 고철 2 / 전자 부품 1 / 천 1\n\n완성한 키트는 배급소 설비 수리에 쓰인다.",0);break;}
                    State.tutorial=3;Say("다은",0,new[]{"오늘은… 이거구나.","수리 키트 하나. 공구 부품 두 개, 전선 두 개, 고철 두 개, 전자 부품 하나, 천 하나.","재료를… 어디에 정리해놨더라…\n선반과 부품 바구니를 확인하자."});break;
                case "parts":case "wire":case "scrap":case "chip":case "cloth":
                    if(State.stage!=1)break;if(State.tutorial<3){Info("먼저 오늘의 의뢰부터 확인하자.",0);break;}
                    if(Grant(id,new ItemStack(id,id=="parts"||id=="wire"||id=="scrap"?2:1))&&Catalog.Repair.Ready(State)){State.tutorial=4;Say("다은",0,new[]{"재료 5종을 모두 찾았다.\n이제 작업을 하러 가자."});}break;
                case "shelf":Info("철제 선반에는 부품 바구니가 늘어서 있다.\n바닥의 바구니에도 필요한 재료가 있다.",0);break;
                case "radio":Info("[ G.M.R.I. 공식 방송 ]\n치료제 공급은 안정적으로 유지되고 있습니다. 지정 배급일을 준수하십시오.");break;
                case "memento":Info("몇 번이고 덧댄 작업복.\n버리기 전에 한 번 더 고쳐 보는 것이 다은의 습관이다.",0);break;
                case "sibling":State.Flag("sibling_talk");Say("동생",-1,new[]{"오늘도 늦게까지 일해? 조명이 또 깜빡이더라.","약 먹었으니까 괜찮아. 돌아오면 같이 불을 고치자.","말이 끝나기 전에 기침이 이어진다.\n책상 위 약 봉투를 확인해야겠다."});break;
                case "toy":State.Flag("toy_fixed");sound.Play("success");Info("작은 조명의 접점을 고쳤다.\n동생이 돌아오면, 이번에는 같이 켜 보기로 한다.",0);break;
                case "memory":Info("둘이 함께 찍은 사진. 사진 뒤에는 짧은 메모가 있다.\n\"고장 나면 또 고치면 되지.\"");break;
                case "medicine":Learn("medicine",0);break;
                case "james":
                    if(State.joined>=2){Info("공급 기록과 배급 설비를 확인해 봅시다.\n제 전문 분야라면 제가 읽어 볼게요.",1);break;}
                    Say("제임스",1,new[]{"그 약은 증상을 잠시 억제할 뿐입니다. 완치라고 발표됐지만 재발하는 환자가 계속 있어요.","설비가 멈춰 배급 기록도 읽을 수 없군요. 수리 키트가 있나요?", "저도 동행하겠습니다. 기록 속 약과 실제로 배급되는 약이 왜 다른지 확인해야겠어요."},()=>{State.joined=2;State.Flag("james_joined");Toast("제임스 합류 · C 또는 2번 키");Save("auto",false);});break;
                case "dispenser":
                    if(State.Has("dispenser_fixed")){Info("배급 설비가 다시 움직인다. 일부 기록은 제임스가 해독할 수 있다.");break;}if(!RequireHero(0))break;
                    if(!Inventory.Take(State,"repair",1)){Info("수리 키트가 필요하다. 공방의 의뢰품을 확인하자.");break;}State.Flag("dispenser_fixed");sound.Play("success");Info("배급 설비의 전원을 복구했다.\n공급 기록을 읽을 수 있다.",0);break;
                case "supply":if(!State.Has("dispenser_fixed")){Info("설비가 멈춰 기록의 일부를 확인할 수 없다.");break;}if(RequireHero(1))Learn("supply",1);break;
                case "cache3":Grant(id,new ItemStack("med",3),new ItemStack("antidote",2));break;
                case "daniel":
                    if(State.joined==3){Info("저 잔해를 함께 치우자. 힘을 쓸 곳은 이런 데지.",2);break;}
                    Say("다니엘",2,new[]{"잠깐, 안에 사람이 있어! 이쪽부터 치우자.","연구소로 가는 거야? 나도 그쪽에서 사라진 사람을 찾고 있어.","힘쓰는 건 맡겨. 길이 막혔다고 돌아갈 수는 없잖아."},()=>{State.joined=3;State.Flag("daniel_joined");Toast("다니엘 합류 · C 또는 3번 키");});break;
                case "rescue":if(RequireHero(2)){State.Flag("rescued");sound.Play("success");Say("다니엘",2,new[]{"천천히, 이쪽으로 나와.\n잔해를 옮겨 주민이 빠져나올 공간을 만들었다.","주민은 연구소 유지보수 입구의 위치를 알려 주었다.\n힘을 써서 누군가를 살릴 수도 있다."});}break;
                case "jump":if(RequireHero(2)){World.position=new Vector2(24,11);State.Flag("jumped");sound.Play("step");Toast("안전한 착지 지점을 확보했다. 동료도 건너왔다.");}break;
                case "cache4":Grant(id,new ItemStack("trap",4),new ItemStack("med",2));break;
                case "orders":Learn("orders");break;
                case "checkpoint":Say("검문 담당자",-1,new[]{"그 기록은 어디서 얻었습니까? 치료 효과에 관한 질문은 연구소에서 받겠습니다.","세 사람이 저항하기 전에 검문 구역의 문이 잠긴다.\n배급 기록은 다은의 작업복 안쪽에 남아 있다."},()=>{State.Flag("captured");State.Clue("orders");EnterStage(6);});break;
                case "cell_rubble":if(RequireHero(2)){State.Flag("cell_clear");Info("막힌 배관 앞의 잔해를 치웠다.\n새어 나오는 가스는 제임스가 확인해야 한다.",2);}break;
                case "cell_gas":if(!State.Has("cell_clear")){Info("잔해 때문에 정화 배관에 접근할 수 없다.");break;}if(RequireHero(1)){State.Flag("cell_safe");Info("표시된 중화 절차에 맞춰 정화 장치를 가동했다.\n이제 잠금 패널에 접근할 수 있다.",1);}break;
                case "cell_lock":if(!State.Has("cell_safe")){Info("가스가 남아 있어 작업할 수 없다.");break;}if(RequireHero(0)){State.Flag("cell_open");sound.Play("success");Info("잠금 회로를 우회했다. 유지보수 통로로 나갈 수 있다.",0);}break;
                case "cell_note":Info("\"질문하면 이곳으로 온다. 하지만 정비 통로는 아직 연결되어 있다.\"");break;
                case "guard":if(State.Has("guard_done")){Info("이미 이 통로의 대치는 끝났다.");break;}Say("순찰 경비",-1,new[]{"거기 멈춰! …너희는 수용 구역에 있던 사람들이잖아.","전투에서는 HP를 줄이거나, 제압 또는 설득을 100까지 채워 상황을 끝낼 수 있다.\n행동을 고르기 전 C / 1·2·3으로 리더를 바꿀 수 있다."},()=>StartBattle("guard"));break;
                case "bypass":if(RequireHero(0)){State.Flag("bypass_open");Info("환기 제어 장치를 열었다.\n북동쪽 환기구로 경비를 피해 이동할 수 있다.",0);}break;
                case "stealth_exit":if(!State.Has("bypass_open")){Info("먼저 환기 제어 장치를 열어야 한다.");break;}State.Record("guard",Resolution.Avoided);State.Flag("guard_done");EnterStage(8);break;
                case "cache7":Grant(id,new ItemStack("trap",3),new ItemStack("med",2));break;
                case "hub":State.Flag("hub_read");Say("중앙 단말",-1,new[]{"V-06 접근 권한: 보안 A / 약물 B / 정보 C.\n세 조각을 모으면 정제 자료 전체를 복원할 수 있다.","방식은 기록된다. 공격으로 파괴할 수도, 제압하거나 설득할 수도 있다.\n해결 기록과 열쇠 조각은 I / Tab 상태창에서 확인할 수 있다."});break;
                case "cache8":Grant(id,new ItemStack("scrap",6),new ItemStack("wire",6),new ItemStack("parts",6),new ItemStack("cloth",4),new ItemStack("chip",5),new ItemStack("board",1),new ItemStack("battery",1),new ItemStack("trap",3),new ItemStack("med",3));break;
                case "machine":if(State.incidents.Any(i=>i.id=="machine")){Info("보안 장치의 위협은 사라졌다.");break;}StartBattle("machine");break;
                case "a_rubble":if(RequireHero(2)){State.Flag("a_clear");Info("무너진 보관대를 밀자 벽면 기록이 드러났다.\n제임스가 보관 표식을 해석할 수 있다.",2);}break;
                case "a_record":if(!State.Has("a_clear")){Info("보관대의 잔해를 먼저 옮겨야 기록 전체를 읽을 수 있다.");break;}if(RequireHero(1))Learn("a_order",1);break;
                case "a_panel":if(State.fragments.Contains("A")){Info("열쇠 조각 1을 이미 회수했다.");break;}if(!RequireHero(0))break;if(!State.clues.Contains("a_order")){Info("기호의 순서를 해석할 단서가 필요하다.",0);break;}SetMode(GameMode.PuzzleA);break;
                case "a_bypass":if(!RequireHero(2))break;Say("다니엘",2,new[]{"비상문은 열 수 있어. 하지만 보관함의 조각은 남겨 두게 돼.\n최심부에 들어가기 전까지 돌아와 퍼즐을 풀 수 있다."},null,new Choice("비상문을 연다",()=>{State.Flag("a_bypass");SetMode(GameMode.Field);Toast("출구가 열렸다. 조각 1은 아직 보관함에 있다.");}),new Choice("정상 해법을 찾는다",()=>SetMode(GameMode.Field)));break;
                case "shield":if(State.incidents.Any(i=>i.id=="shield")){Info("비상 통로의 경비는 더 이상 길을 막지 않는다.");break;}StartBattle("shield");break;
                case "b_rules":Learn("b_rules",State.leader==1?1:-1);break;
                case "b_analyze":if(RequireHero(1)){State.Flag("b_analyzed");if(!State.clues.Contains("b_rules"))Learn("b_rules",1);else Info("여섯 시료의 정보를 정리했다.\n특수 반응, 보관 이력, 폐기 표시, 첨가제, 봉인을 비교하자.\n주입 전까지 분석은 반복할 수 있다.",1);}break;
                case "b_inject":if(State.Has("b_done")){Info(State.Has("b_correct")?"안전한 용액으로 장치를 안정화했다.":"잘못된 주입으로 조각 보관함은 비상 폐쇄되었다.");break;}if(State.Has("b_injected")){StartBattle("b");break;}if(!RequireHero(1))break;if(!State.Has("b_analyzed")){Info("분석기에서 시료 정보를 먼저 확인해야 한다.",1);break;}SetMode(GameMode.PuzzleB);break;
                case "cache10":Grant(id,new ItemStack("antidote",3),new ItemStack("med",3),new ItemStack("trap",3));break;
                case "patients":if(RequireHero(1))Learn("patients",1);break;
                case "c_backup":Learn("supply");break;
                case "researcher":if(State.fragments.Contains("C")){Say("연구원 C",5,new[]{"당신들이 무엇을 남기는지 지켜보겠습니다."});break;}debateStep=0;debateScore=0;SetMode(GameMode.Debate);break;
                case "c_terminal":if(State.Has("c_cooperate")){if(RequireHero(0)){State.Fragment("C");State.Flag("c_done");State.Record("c",Resolution.Peaceful);Save("auto",false);Info("연구원 C가 풀어 준 접근 권한을 연결했다.\n열쇠 조각 3과 증언을 확보했다.",0);}}else Info("연구원 C의 접근 허가 또는 구역의 위협 해제가 필요하다.");break;
                case "experiment":Learn("experiment");break;
                case "v06":if(RequireHero(1))Learn("v06",1);break;
                case "link":if(!State.clues.Contains("experiment")||!State.clues.Contains("v06")){Info("실험 승인서와 V-06 정제 절차를 모두 확인해야 한다.");break;}State.Flag("records_linked");Say("제임스",1,new[]{"배급약은 완치제가 아니었습니다. 완치 치료제의 가능성을 알면서도 통제를 유지했어요.","우리가 가진 조각은 정제 자료를 복구할 권한입니다. 하지만 자료만으로는 부족해요. 시설을 유지하고 복구할 사람도 필요합니다.","이 기록은 바이러스의 최초 기원을 증명하지 않습니다. 분명한 것은 실험과 방치, 그리고 은폐입니다."});break;
                case "witness":Info(State.NonLethal>0?"남겨진 교신이 들린다.\n\"그들이 우릴 해치지 않았어. 비상 복구 회로를 연결할 수 있다면 도울게.\"":"끊긴 교신만 남아 있다.\n시설 복구를 도울 사람과 기록을 충분히 확보했는지 돌아본다.");break;
                case "review":State.Flag("reviewed");Say("진행 기록",-1,new[]{"열쇠 조각 "+State.fragments.Count+" / 3\n비살상 해결 "+State.Ratio+"\n\n완전한 정제 자료에는 조각 3개가 필요하다. 복구 기반은 비살상 해결 50% 이상에서 확보된다.","최종 전투도 하나의 해결 사건으로 집계된다.\n조각 1은 A 구역 재방문으로 회수할 수 있다. B 구역에서 비상 폐쇄된 조각은 복구할 수 없다.","소장의 거래를 받아들이면 임시 억제제를 받고 돌아간다.\n마지막 문 앞에서는 지금까지의 기록을 저장한다."});break;
                case "cache13":Grant(id,new ItemStack("trap",6),new ItemStack("med",6),new ItemStack("antidote",2),new ItemStack("wire",3),new ItemStack("parts",3),new ItemStack("chip",3),new ItemStack("scrap",3),new ItemStack("cloth",3));break;
                case "return_hub":EnterStage(8);break;
                case "director":FinalOffer();break;
                case "core":Info("V-06의 정제 설비가 낮게 울린다.\n모든 것을 파괴하지 않고 통제권을 되찾아야 한다.");break;
                case "last_record":Info("정제 생산량은 공개 배급량과 일치하지 않는다.\n숨겨진 완치 가능성은 오래전부터 존재했다.");break;
                default:if(id.StartsWith("sample")){int index=int.Parse(id.Substring(6));Info("시료 "+(char)('A'+index)+"\n"+SampleDescription(index),State.leader==1?1:-1);}break;
            }
        }
        string SampleDescription(int i){return new[]{"분석기에 특수 반응 표시. 관리 규칙의 위험 시료 조건과 일치한다.","냉동 보관 표시. 목표 용액의 상온 보관 이력과 맞지 않는다.","붉은 삼각 기호. 폐기 목록에 같은 기호가 있다.","첨가제 코드 X. 해당 첨가제가 있으면 제외해야 한다.","봉인이 개봉되어 있다. 오염 격리 대상이다.","미개봉. 기준 코드 일치. 특수 반응 없음. 모든 관리 조건을 충족한다."}[i];}
        void OpenCraft()
        {
            if(!RequireHero(0))return;if(State.stage==1){chosenRecipe=Catalog.Repair;if(State.tutorial<4){Info("재료가 부족한 것 같아.\n의뢰서와 5종 재료를 먼저 확인하자.",0);return;}if(State.tutorial>=5){Info("오늘의 의뢰품은 완성했다. 동생을 만나러 가자.",0);return;}}
            craft=new CraftSession(chosenRecipe);craftHeld="";SetMode(GameMode.Crafting);
        }
        void CraftPlace(int slot)
        {
            if(craftHeld==""){if(craft.Withdraw(slot)){sound.Play("tick");if(craft.withdrawals==10)Toast("다은: 하아… 그만 하고싶다…");}return;}
            if(!craft.Place(State,craftHeld,slot)){Toast("이 슬롯은 비어 있지 않거나 남은 재료가 부족합니다.");sound.Play("error");return;}
            craftHeld="";actionPulse=.65f;sound.Play(craft.Correct(slot)?"pickup":"error");if(!craft.Correct(slot))Toast("앗, 잘못 넣었네. 슬롯을 다시 선택하면 회수할 수 있다.");
            if(craft.Complete){if(!craft.Commit(State)){Toast("완성품을 넣을 공간이 부족합니다. 재료는 소모되지 않았습니다.");return;}sound.Play("success");if(State.stage==1){State.tutorial=5;Say("다은",0,new[]{"휴우… 됐다…", "수리 키트를 완성했다.\n이제 밖으로 나가 동생을 만나자."},()=>{SetMode(GameMode.Field);Save("auto");});}else{Toast(chosenRecipe.name+" 제작 완료");craft=new CraftSession(chosenRecipe);}}
        }
        void ConfirmLeaveCraft(){Say("제작 종료",0,new[]{"배치한 재료를 모두 회수하고 제작을 마칠까요?"},null,new Choice("회수하고 닫기",()=>{craft=null;craftHeld="";SetMode(GameMode.Field);}),new Choice("계속 제작",()=>SetMode(GameMode.Crafting)));}
        public bool SubmitA(int[] sequence)
        {
            if(sequence.Length==3&&sequence[0]==0&&sequence[1]==1&&sequence[2]==2){State.Flag("a_open");State.Fragment("A");sound.Play("success");Save("auto",false);Info("등급 순서가 일치한다. 보관함이 열렸다.\n열쇠 조각 1을 획득했다.",0);return true;}
            puzzleAttempts++;sound.Play("error");Toast(puzzleAttempts>=3?"제임스: 보관 등급 순서예요. 원형 → 삼각 → 사각.":"순서가 맞지 않습니다. 기록창의 보관 등급을 다시 확인하세요.");return false;
        }
        void ConfirmSample()
        {
            int chosen=selectedSample;Say("최종 주입",1,new[]{"시료 "+(char)('A'+chosen)+"를 주입한다.\n\n이 장치는 열쇠 조각 보관함과 연결되어 있다. 잘못된 주입은 중독을 일으키고 보관함을 영구 폐쇄한다. 분석 화면으로 돌아가 다시 확인할 수 있다."},null,new Choice("이 시료를 주입한다",()=>Inject(chosen)),new Choice("다시 분석한다",()=>SetMode(GameMode.PuzzleB)));
        }
        public void Inject(int index)
        {
            if(State.Has("b_injected")){Toast("이미 주입 결과가 확정되었습니다.");return;}State.Flag("b_injected");bool correct=index==5;
            if(correct){State.Flag("b_correct");State.Fragment("B");sound.Play("success");Say("제임스",1,new[]{"생리식염수다. 공급 장치가 안정화됐다.\n열쇠 조각 2를 확보했고 강화 요원의 약물 공급이 약해졌다."},()=>StartBattle("b"));}
            else {State.poison=true;sound.Play("error");Say("경보",-1,new[]{"오염 경보. 보관함 비상 폐쇄.\n파티가 중독되었다. 전투에서 중화 도구로 해독할 수 있다. 필드에서는 중독 피해가 진행되지 않는다."},()=>StartBattle("b"));}
        }
        void DebateAnswer(bool good)
        {
            if(!good){Say("연구원 C",5,new[]{"그 말로는 누구도 설득할 수 없습니다. 더 이상 접근하지 마십시오."},null,new Choice("물러나 단서를 더 찾는다",()=>SetMode(GameMode.Field)),new Choice("강행한다 · 전투",()=>StartBattle("c")));return;}
            debateScore++;debateStep++;menuFocus=0;if(debateStep>=3){State.Flag("c_cooperate");Say("연구원 C",5,new[]{"처음부터 모두를 구하러 왔다고 말하지 않아서 다행입니다.","당신들이 가져온 기록은 제 주장과 맞지 않군요. 단말의 접근 잠금을 풀겠습니다.\n다은이 연결하면 권한 조각과 제 증언을 가져갈 수 있습니다."});}
        }
        public void StartBattle(string id)
        {
            if(State.incidents.Any(i=>i.id==id && i.result!=Resolution.Avoided)){Toast("이미 해결한 사건입니다.");SetMode(GameMode.Field);return;}
            battleId=id;battleCheckpoint=State.Clone();Battle=new CombatSession(State,EnemyDefinition.For(id,State));SetMode(GameMode.Battle);sound.SetTheme(2);
        }
        void BattleAct(BattleAction action,bool correct=true)
        {
            if(Battle==null||Battle.finished||BattlePresenting)return;int oldParty=State.hp,oldEnemy=Battle.hp,oldBlue=Battle.subdue,oldGreen=Battle.persuade;bool result=Battle.Act(action,correct,UnityEngine.Random.value);if(!result){Toast(Battle.log);sound.Play("error");return;}
            BeginBattlePresentation(action,oldParty,oldEnemy,oldBlue,oldGreen);actionPulse=.6f;sound.Play(action==BattleAction.Attack?"hit":action==BattleAction.Subdue||action==BattleAction.Trap?"capture":action==BattleAction.Persuade?"persuade":"tick");menuFocus=0;
            if(Battle.finished){if(Battle.won){State.Record(battleId,Battle.result);if(battleId=="guard")State.Flag("guard_done");if(battleId=="b")State.Flag("b_done");if(battleId=="c"){State.Fragment("C");State.Flag("c_done");}if(battleId=="boss"){State.bossWon=true;State.bossLost=false;}}
                else if(battleId=="boss"){State.bossLost=true;State.bossWon=false;}}
        }
        void FinishBattle()
        {
            if(!Battle.finished)return;
            if(battleId=="boss"){ShowEnding(State.DetermineEnding());return;}
            if(!Battle.won){State=battleCheckpoint.Clone();World.state=State;World.position=new Vector2(State.x,State.y);World.guardTriggered=false;World.alert=0;Battle=null;SetMode(GameMode.Field);sound.SetTheme(1);Toast("전투 직전으로 돌아왔습니다. 소모품과 상태도 복원되었습니다.");return;}
            string result=Battle.result==Resolution.Lethal?"대치는 끝났다. 남겨진 기록에서 복구할 수 있는 부분을 찾는다.":Battle.result==Resolution.Persuaded?"상대가 전의를 거두었다. 살아남은 사람의 협력은 이후에도 남는다.":"상대를 해치지 않고 무력화했다. 시설과 기록을 보존할 가능성이 남았다.";
            Battle=null;SetMode(GameMode.Field);sound.SetTheme(1);Save("auto",false);Say("사건 기록",-1,new[]{result,"비살상 해결 · "+State.Ratio});
        }
        void FinalOffer()
        {
            Save("before-final",false);Say("연구소장",3,new[]{"동생의 증상을 억제할 약을 주지. 공급도 보장하겠다. 대신 이곳에서 본 것을 잊어라.","제임스: 완치가 아닙니다. 다음 약을 받을 권한까지 저 사람에게 맡기게 돼요.","다니엘: 돌아갈 수는 있어. 하지만 뒤에 남는 사람들은 어떻게 되지?", "다은: 동생을 구하러 여기까지 왔어. 그리고 이제, 무엇이 동생을 계속 아프게 했는지도 알아."},null,new Choice("거래 수락 · 임시 억제제를 받는다",()=>ConfirmOffer(true)),new Choice("거래 거절 · 최종 전투",()=>ConfirmOffer(false)));
        }
        void ConfirmOffer(bool accept){Say("마지막 선택",0,new[]{accept?"약을 받고 돌아간다. 동생은 완치되지 않으며 연구소의 공급에 의존한다.\n이 거래를 받아들일까?":"거래를 거절하고 정제 시설의 통제권을 되찾는다.\n최종 전투에서 패배하면 수감될 수 있다."},null,new Choice(accept?"수락한다":"거절한다",()=>{State.offerAccepted=accept;if(accept)ShowEnding("B");else StartBattle("boss");}),new Choice("다시 생각한다",FinalOffer));}
        void ShowEnding(string code)
        {
            endingCode=code;State.ending=code;if(!Settings.endings.Contains(code)){Settings.endings.Add(code);PersistSettings();}SetMode(GameMode.Ending);sound.SetTheme(code=="A"?3:1);
        }
        void UseInventoryItem(string id)
        {
            if(id=="med"&&State.hp<100){if(Inventory.Take(State,id,1)){int amount=State.leader==0?45:30;State.hp=Mathf.Min(100,State.hp+amount);Toast("파티 HP 회복");sound.Play("success");}}
            else if(id=="antidote"&&State.poison){if(Inventory.Take(State,id,1)){State.poison=false;Toast("중독 해제");}}
            else if(id=="unlock"){if(State.stage!=8||Vector2.Distance(World.position,new Vector2(23,8))>2){Toast("중앙 홀의 유지보수 보급함 가까이에서 사용하세요.");return;}if(State.leader!=0){Toast("다은이 사용할 수 있습니다.");return;}if(State.Has("bonus_open")){Toast("이미 보조 보관함을 열었습니다.");return;}var copy=State.Clone();if(Inventory.Take(copy,id,1)&&Inventory.Add(copy,"trap",3)&&Inventory.Add(copy,"med",2)){State.items=copy.items;State.Flag("bonus_open");Toast("보조 보관함 개방 · 구속 도구 3 / 회복 도구 2");}else Toast("보관함 물품을 넣을 공간이 부족합니다.");}
            else Toast("지금 사용할 수 없는 아이템입니다.");
        }
    }
}

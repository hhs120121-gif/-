using System;
using System.Collections.Generic;
using UnityEngine;

namespace SecretVirus
{
    public enum BattleAction { Attack, Subdue, Analyze, Trap, Heal, Antidote, Persuade, Wait }
    public class EnemyDefinition
    {
        public string id,name,hint; public int hp,damage; public bool machine,shield,boss;
        public EnemyDefinition(string id,string name,int hp,int damage,string hint,bool machine=false,bool shield=false,bool boss=false)
        { this.id=id;this.name=name;this.hp=hp;this.damage=damage;this.hint=hint;this.machine=machine;this.shield=shield;this.boss=boss; }
        public static EnemyDefinition For(string id,GameState s)
        {
            switch(id) {
                case "guard": return new EnemyDefinition(id,"불안한 경비",100,12,"가족에게 돌아갈 안전한 길을 걱정한다. 경보보다 사람을 먼저 생각한다.");
                case "shield": return new EnemyDefinition(id,"방패 경비",120,13,"방패가 올라간 동안 제압량이 절반. 강한 공격 직후 방패가 내려간다.",false,true);
                case "machine": return new EnemyDefinition(id,"자동 보안 장치",100,12,"대화할 수 없다. 구속 도구와 물리 제압으로 제어 회로를 정지시킬 수 있다.",true);
                case "b": return new EnemyDefinition(id,"약물 강화 요원",120,s.Has("b_correct")?11:14,"강화 다음에는 반동이 온다. 자신도 투여된 약의 정체를 모른다.");
                case "c": return new EnemyDefinition(id,"연구원 C",110,12,"질서를 잃는 것이 두렵다. 환자 기록과 배급 기록의 모순을 제시하자.");
                default: return new EnemyDefinition("boss","연구소장 · 최심부 통제",240,13,"통제는 생존을 위한 것이라 주장한다. 승인서와 V-06 자료가 그 주장을 반박한다.",false,false,true);
            }
        }
    }
    public class CombatSession
    {
        public GameState state; public EnemyDefinition enemy; public int hp,subdue,persuade,round=1,phase=1,toolsUsed,firstToolLeader=-1;
        public bool analyzed,finished,won,bonusPending; public int analysisRound=-10,insightPhase;
        public Resolution result; public string log=""; public List<string> history=new List<string>();
        public CombatSession(GameState s,EnemyDefinition enemy) { state=s;this.enemy=enemy;hp=enemy.hp; Say("적의 다음 행동을 확인하고 해결 방법을 선택하세요."); }
        public bool CanSwitch { get { return !finished && toolsUsed==0; } }
        public bool Strong { get { return round%3==0; } }
        public bool ShieldUp { get { return enemy.shield && round%3!=1; } }
        public int IntentDamage { get { return enemy.damage+(Strong?8:0)+(enemy.boss?(phase-1)*3:0); } }
        public string Intent { get { return (Strong?"강한 공격 준비":"접근 공격")+" · 예상 피해 "+IntentDamage+(state.poison?" + 중독 5":""); } }
        public int AttackDamage { get { return Mathf.FloorToInt(20*(state.leader==2?1.3f:1f)*(round==analysisRound+1?1.25f:1f)); } }
        public int ToolAmount(int n) { return Mathf.FloorToInt(n*(state.leader==0?1.5f:1f)); }
        public string Validate(BattleAction action)
        {
            if(finished)return "이미 종료된 전투입니다.";
            if(toolsUsed>0 && action!=BattleAction.Trap && action!=BattleAction.Heal && action!=BattleAction.Antidote && action!=BattleAction.Wait)return "두 번째 도구를 사용하거나 턴을 마치세요.";
            if(action==BattleAction.Subdue && state.leader!=2)return "다니엘만 물리 제압을 사용할 수 있습니다.";
            if(action==BattleAction.Persuade && enemy.machine)return "자동 장치는 대화할 수 없습니다.";
            if(action==BattleAction.Trap && Inventory.Count(state,"trap")==0)return "구속 도구가 없습니다. 다니엘의 제압은 소모품이 필요하지 않습니다.";
            if(action==BattleAction.Heal && (Inventory.Count(state,"med")==0 || state.hp==100))return "회복 도구가 없거나 HP가 가득 찼습니다.";
            if(action==BattleAction.Antidote && (!state.poison || Inventory.Count(state,"antidote")==0))return "중독 상태가 아니거나 중화 도구가 없습니다.";
            return "";
        }
        public bool Act(BattleAction action,bool correct,float roll)
        {
            string invalid=Validate(action); if(invalid!=""){Say(invalid);return false;}
            bool tool=action==BattleAction.Trap||action==BattleAction.Heal||action==BattleAction.Antidote;
            string text="";
            switch(action) {
                case BattleAction.Attack: int damage=AttackDamage;hp=Math.Max(0,hp-damage);text="공격 · HP -"+damage;break;
                case BattleAction.Subdue: int gain=(ShieldUp?15:30)+(roll<.3f?15:0);subdue=Math.Min(100,subdue+gain);text="비살상 제압 +"+gain+(roll<.3f?" · 정확한 제압!":"");break;
                case BattleAction.Analyze:
                    analyzed=true;if(state.leader==1)analysisRound=round;
                    if(insightPhase!=phase){if(!enemy.machine)persuade=Math.Min(100,persuade+10);insightPhase=phase;}
                    text=enemy.hint+(state.leader==1?" 다음 턴 첫 공격 +25%.":"");break;
                case BattleAction.Trap: Inventory.Take(state,"trap",1);int capture=ToolAmount(20);subdue=Math.Min(100,subdue+capture);text="구속 도구 · 제압 +"+capture;break;
                case BattleAction.Heal: Inventory.Take(state,"med",1);int heal=Math.Min(100-state.hp,ToolAmount(30));state.hp+=heal;text="파티 HP +"+heal;break;
                case BattleAction.Antidote: Inventory.Take(state,"antidote",1);state.poison=false;text="중독을 해제했다.";break;
                case BattleAction.Persuade:
                    if(correct){int p=25+(roll<(state.leader==1?.55f:.2f)?10:0);persuade=Math.Min(100,persuade+p);text="근거가 전해졌다. 설득 +"+p;}else text="상대의 걱정과 맞지 않는 말이다. 분석으로 단서를 확인하자.";break;
                case BattleAction.Wait:text="남은 도구 행동을 마쳤다.";break;
            }
            Say(text); Check(); if(finished)return true;
            UpdatePhase();
            if(tool && state.leader==0 && toolsUsed==0){toolsUsed=1;firstToolLeader=0;return true;}
            EnemyTurn();return true;
        }
        void EnemyTurn()
        {
            int damage=IntentDamage;state.hp=Math.Max(0,state.hp-damage);string text="상대의 공격 · 파티 HP -"+damage;
            if(state.poison && state.hp>0){state.hp=Math.Max(0,state.hp-5);text+=" / 중독 -5";}
            history.Add(text);log+="\n"+text;
            toolsUsed=0;firstToolLeader=-1;round++;
            if(state.hp==0){finished=true;won=false;log+="\n파티가 더 이상 전투를 지속할 수 없다.";}
        }
        void Check()
        {
            if(hp<=0){finished=true;won=true;result=Resolution.Lethal;}
            else if(subdue>=100){finished=true;won=true;result=Resolution.Subdued;}
            else if(persuade>=100){finished=true;won=true;result=Resolution.Persuaded;}
        }
        void UpdatePhase()
        {
            if(!enemy.boss)return;
            float progress=Math.Max(100f*(enemy.hp-hp)/enemy.hp,Math.Max(subdue,persuade));
            int next=progress>=66?3:progress>=33?2:1;
            if(next>phase){phase=next;log+="\nPHASE "+phase+" · "+(phase==2?"시설 부하가 올라간다.":"소장이 통제력을 잃기 시작했다.");}
        }
        void Say(string message) {log=message;history.Add(message);if(history.Count>20)history.RemoveAt(0);}
    }
}

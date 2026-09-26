using UnityEngine;

namespace SecretVirus
{
    public partial class VirusGame
    {
        Texture2D softLight, edgeShade;
        float battleBeatUntil;
        int previousPartyHp, previousEnemyHp, previousSubdue, previousPersuade;
        bool impactPlayed;
        string actionCaption="";
        bool BattlePresenting => Mode==GameMode.Battle && Time.unscaledTime<battleBeatUntil;
        float BattleProgress => Mathf.Clamp01(1-(battleBeatUntil-Time.unscaledTime)/1.15f);
        void BeginBattlePresentation(BattleAction action,int partyHp,int enemyHp,int blue,int green)
        {
#if UNITY_EDITOR
            if(verificationRunning)return;
#endif
            previousPartyHp=partyHp;previousEnemyHp=enemyHp;previousSubdue=blue;previousPersuade=green;
            World.CombatPose(action);
            battleBeatUntil=Time.unscaledTime+1.15f;impactPlayed=false;
            actionCaption=action==BattleAction.Attack?"공격":action==BattleAction.Subdue?"비살상 제압":action==BattleAction.Persuade?"설득":action==BattleAction.Analyze?"상태 분석":action==BattleAction.Heal?"응급 처치":action==BattleAction.Antidote?"중독 해제":action==BattleAction.Trap?"구속 도구":"행동 종료";
        }
        int Presented(int previous,int current,bool party=false)
        {
            if(!BattlePresenting)return current;
            float t=Mathf.Clamp01((BattleProgress-(party?.57f:.10f))/.25f);
            return Mathf.RoundToInt(Mathf.Lerp(previous,current,Mathf.SmoothStep(0,1,t)));
        }
        string PresentedLog()
        {
            if(!BattlePresenting)return Battle.log;
            int split=Battle.log.IndexOf("상대의 공격");
            return BattleProgress<.57f&&split>=0?Battle.log.Substring(0,split).Trim():Battle.log;
        }
        void BattleFeedback()
        {
            if(!BattlePresenting)return;
            bool response=BattleProgress>=.57f&&State.hp<previousPartyHp;
            string label=response?"반격  −"+(previousPartyHp-State.hp):actionCaption;
            Color color=response?red:mint;
            Fill(new Rect(483,369,280,37),new Color(.03f,.07f,.09f,.9f));
            Text(new Rect(483,371,280,32),label,21,color,TextAnchor.MiddleCenter);
            if(response)Border(new Rect(48,148,393,109),new Color(red.r,red.g,red.b,1-BattleProgress),3);
        }
        void AtmosphereUI()
        {
            if(softLight==null){
                softLight=new Texture2D(128,128,TextureFormat.RGBA32,false);softLight.wrapMode=TextureWrapMode.Clamp;
                edgeShade=new Texture2D(128,72,TextureFormat.RGBA32,false);edgeShade.wrapMode=TextureWrapMode.Clamp;
                for(int y=0;y<128;y++)for(int x=0;x<128;x++){float d=Vector2.Distance(new Vector2(x,y),new Vector2(63.5f,63.5f))/64;softLight.SetPixel(x,y,new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-d),2)));}
                for(int y=0;y<72;y++)for(int x=0;x<128;x++){float d=new Vector2((x-63.5f)/75,(y-35.5f)/46).magnitude;edgeShade.SetPixel(x,y,new Color(.015f,.03f,.045f,Mathf.SmoothStep(0,.67f,Mathf.Clamp01((d-.25f)/.95f))));}
                softLight.Apply();edgeShade.Apply();
            }
            GUI.color=Color.white;
            if(Mode==GameMode.Field){
                for(int i=0;i<22;i++){float x=(i*197.3f+uiTime*(i%2==0?3:-2)+2560)%1280,y=90+(i*83.7f+uiTime*(2+i%3))%540;Fill(new Rect(x,y,i%3==0?2:1,2),new Color(.84f,.86f,.69f,.10f+.08f*Mathf.Sin(uiTime+i)));}
                foreach(var t in World.things){if(t.label==""||!t.renderer.enabled||Vector2.Distance(t.position,World.position)>3.2f)continue;Vector2 p=World.ScreenPoint(t.position);float a=.35f+.15f*Mathf.Sin(uiTime*2);Fill(new Rect(p.x-2,p.y-49,4,4),new Color(gold.r,gold.g,gold.b,a));}
            }
        }
        string CurrentTask()
        {
            if(CanExit())return State.stage==13?"휴식과 보급을 마친 뒤 마지막 문으로 향하자":"조사를 마쳤다 · 남은 물품을 챙기고 다음 구역으로";
            switch(State.stage){
                case 2:return !State.Has("sibling_talk")?"동생의 상태를 확인하자":"책상 위 약 봉투를 조사하자";
                case 3:return State.joined<2?"배급소의 제임스와 이야기하자":!State.Has("dispenser_fixed")?"다은 · 수리 키트로 배급 설비를 복구하자":"제임스 · 공급 기록을 해독하자";
                case 4:return State.joined<3?"다니엘과 이야기하자":"다니엘 · 잔해에 갇힌 주민을 구하자";
                case 6:return !State.Has("cell_clear")?"다니엘 · 배관 앞 잔해를 치우자":!State.Has("cell_safe")?"제임스 · 누출된 가스를 중화하자":"다은 · 수용실 잠금 패널을 해제하자";
                case 7:return State.Has("bypass_open")?"북동쪽 환기구로 우회하거나 경비와 대치하자":"순찰 시야를 피해 환기 제어 장치를 찾자";
                case 9:return !State.Has("a_clear")?"다니엘 · 무너진 보관대를 옮기자":!State.clues.Contains("a_order")?"제임스 · 보관 등급 기록을 읽자":"다은 · 기록의 순서로 전자 키패드를 해제하자";
                case 10:return !State.clues.Contains("b_rules")?"시료 관리 일지에서 안전 조건을 확인하자":!State.Has("b_analyzed")?"제임스 · 분석기에서 시료를 비교하자":"제임스 · 최종 주입 장치에서 안전한 시료를 선택하자";
                case 11:return State.Has("c_cooperate")?"다은 · 허가받은 정보 보관 단말을 연결하자":!State.clues.Contains("patients")?"제임스 · 환자 추적 기록을 확인하자":"연구원 C에게 기록을 근거로 이야기하자";
                case 12:return !State.clues.Contains("experiment")?"생체 실험 승인서를 확인하자":!State.clues.Contains("v06")?"제임스 · V-06 정제 절차를 해독하자":"기록 연결 단말에서 진실을 정리하자";
                default:return Catalog.Stages[State.stage].objective;
            }
        }
    }
}

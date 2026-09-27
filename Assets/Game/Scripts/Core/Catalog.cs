using System;
using System.Collections.Generic;
using UnityEngine;

namespace SecretVirus
{
    public class ItemDefinition
    {
        public string name, description; public Color color;
        public ItemDefinition(string name, string description, string hex) { this.name = name; this.description = description; ColorUtility.TryParseHtmlString(hex, out color); }
    }
    public class StageDefinition
    {
        public int number; public string title, subtitle, objective, intro; public Color floor, wall;
        public StageDefinition(int n, string title, string subtitle, string objective, string intro, string floor, string wall)
        { number=n; this.title=title; this.subtitle=subtitle; this.objective=objective; this.intro=intro; ColorUtility.TryParseHtmlString(floor,out this.floor); ColorUtility.TryParseHtmlString(wall,out this.wall); }
    }
    public class ClueDefinition
    {
        public string title, text, interpretation, place;
        public ClueDefinition(string title,string text,string interpretation,string place) { this.title=title; this.text=text; this.interpretation=interpretation; this.place=place; }
    }
    public static class Catalog
    {
        public static readonly string[] Heroes = { "다은", "제임스", "다니엘" };
        public static readonly string[] Jobs = { "엔지니어", "임상 약사", "운동선수" };
        public static readonly string[] Talents = { "회로 수리 · 해킹 · 도구 효과 +50%", "약품 분석 · 기록 해독 · 설득", "장애물 제거 · 도약 · 비살상 제압" };
        public static readonly Dictionary<string,ItemDefinition> Items = new Dictionary<string,ItemDefinition> {
            {"parts",new ItemDefinition("공구 부품","닳았지만 다시 쓸 수 있는 기계 부품.","#C5A273")},
            {"wire",new ItemDefinition("전선","낡은 피복 안에 남은 연결의 가능성.","#CE735B")},
            {"scrap",new ItemDefinition("고철","구속 장치의 프레임을 만들 수 있다.","#94A0AA")},
            {"chip",new ItemDefinition("전자 부품","잠금 신호와 장치를 제어하는 부품.","#65BFA3")},
            {"cloth",new ItemDefinition("천","수리와 완충에 쓰이는 질긴 천.","#D3C4AB")},
            {"board",new ItemDefinition("회로기판","여러 장치를 하나의 신호로 연결한다.","#608C77")},
            {"battery",new ItemDefinition("배터리","휴대형 장치의 전원.","#D7B269")},
            {"repair",new ItemDefinition("수리 키트","배급소 설비를 복구할 수 있는 의뢰품.","#D8B675")},
            {"unlock",new ItemDefinition("자물쇠 해제 장치","중앙 홀의 유지보수 보급함 옆에서 다은이 사용한다. 보조 칸의 구속·회복 도구를 회수한다.","#73CBC4")},
            {"trap",new ItemDefinition("구속 도구","전투에서 제압 +20. 다은은 +30, 한 턴에 최대 2회.","#81B2D9")},
            {"med",new ItemDefinition("회복 도구","파티 HP +30. 다은은 +45. 최대 HP를 넘지 않는다.","#8EC1A1")},
            {"antidote",new ItemDefinition("중화 도구","전투 또는 필드에서 중독을 해제한다.","#BFA4D2")}
        };
        public static readonly Recipe Repair = new Recipe("repair","수리 키트","repair",new[]{"parts","wire","scrap","chip","cloth"},new[]{2,2,2,1,1});
        public static readonly Recipe Unlock = new Recipe("unlock","자물쇠 해제 장치","unlock",new[]{"chip","board","wire","battery","parts"},new[]{2,1,2,1,1});
        public static readonly Recipe Trap = new Recipe("trap","구속 도구","trap",new[]{"scrap","wire","parts","cloth","chip"},new[]{1,1,1,1,1});
        public static readonly Recipe[] Recipes = { Repair, Unlock, Trap };
        public static readonly string[] Opening = {
            "20XX년, 정체불명의 바이러스로 세상은 멸망의 위기에 몰렸다.",
            "[ G.M.R.I. 공식 방송 ]\n그러나 G.M.R.I.는 치료제 개발에 성공했고,",
            "거대한 연구소를 중심으로 사람들은 새로운 사회를 구축했다.",
            "이 이야기는…",
            "우연히 세상의 진실에 도달한,\n평범했던 사람의 이야기다."
        };
        public static readonly StageDefinition[] Stages = {
            null,
            new StageDefinition(1,"오래된 공방","A SMALL LIGHT","책상의 의뢰서를 확인하자","오늘의 의뢰를 확인해볼까나.","#48463B","#30382F"),
            new StageDefinition(2,"아직 남아 있는 일상","SOMEONE TO RETURN TO","동생과 이야기하고 약 봉투를 확인하자","어제보다 기침이 잦다. 그래도 동생은 먼저 웃어 보인다.","#514B40","#3E4038"),
            new StageDefinition(3,"배급의 끝","A BORROWED TOMORROW","제임스와 이야기하고 배급 설비를 복구하자","배급소는 문을 닫았다. 약사는 빈 진열대 앞에 남아 있다.","#51574F","#3A4743"),
            new StageDefinition(4,"무너진 통로","A WAY THROUGH","다니엘을 도와 잔해를 옮기자","길이 무너졌지만, 누군가는 아직 그 안에 있다.","#494649","#35363C"),
            new StageDefinition(5,"검문선","THE PRICE OF A QUESTION","공급 기록을 조사하고 검문소로 향하자","같은 약을 운반하는 차량인데 목적지는 모두 다르다.","#40494D","#303C45"),
            new StageDefinition(6,"닫힌 방","THREE DIFFERENT ANSWERS","잔해·가스·잠금 장치를 차례로 해결하자","질문을 했다는 이유만으로 이곳에 갇혔다.","#39494B","#283B41"),
            new StageDefinition(7,"유지보수 통로","HOW WE GET THROUGH","경비를 해결하거나 우회 통로를 열자","저 경비도 우리처럼 돌아갈 곳이 있을까.","#3C4A4C","#273B42"),
            new StageDefinition(8,"연구소 중앙 홀","THE THREE KEYS","중앙 단말을 조사하고 보안 구역으로 향하자","세 개의 권한이 있어야 V-06 기록 전체를 열 수 있다.","#4A5655","#30484D"),
            new StageDefinition(9,"A 구역 · 보안 연구실","THE FIRST FRAGMENT","잔해 뒤 기록을 해독하고 보관함을 열자","질서를 위한 잠금인지, 진실을 숨기기 위한 잠금인지.","#3D5055","#293D4C"),
            new StageDefinition(10,"B 구역 · 약물 연구실","A PROCESS OF ELIMINATION","여섯 시료를 분석하고 장치에 주입할 용액을 정하자","같은 용기 안에 서로 다른 목적이 담겨 있다.","#4B5146","#354539"),
            new StageDefinition(11,"C 구역 · 정보 연구실","WHAT WE CHOOSE TO SAY","기록을 모아 연구원 C와 이야기하자","진실을 알고 있는 사람은, 반드시 같은 편은 아니다.","#4E4958","#393344"),
            new StageDefinition(12,"제한 기록실","THE SECRET OF VIRUS","실험 기록과 V-06 자료를 연결하자","배급되는 것은 완치제가 아니었다. 그 사실을 아는 사람들이 있었다.","#344D50","#203C43"),
            new StageDefinition(13,"정제 시설 앞","BEFORE THE LAST DOOR","단말에서 기록을 점검하고 마지막 문으로 향하자","아직 돌아갈 수 있다. 이 문을 넘기 전까지는.","#465150","#2F4245"),
            new StageDefinition(14,"소장실 · 최심부","A CURE AND A COST","소장의 거래에 답하자","동생을 위한 약이 눈앞에 있다. 그 약의 대가도.","#394548","#27343F")
        };
        public static readonly Dictionary<string,ClueDefinition> Clues = new Dictionary<string,ClueDefinition> {
            {"medicine",new ClueDefinition("동생의 약 봉투","증상 억제 목적. 효과 지속 기간이 지나면 재투여 필요.","완치 치료가 아니라 반복적인 증상 조절이다.","주거 공간")},
            {"supply",new ClueDefinition("비공개 공급 기록","공개 배급분 외의 제제는 연구소 B 구역으로 이송. 일반 배급 금지.","공식 치료 성공 발표와 달리 다른 제제가 비밀리에 연구되고 있다.","배급소")},
            {"orders",new ClueDefinition("검문 지침","치료 효과에 관한 질문은 지정 연구소로 이관. 기록 사본 소지자는 보호 수용.","보호라는 명목으로 정보 유통을 차단한다.","검문선")},
            {"a_order",new ClueDefinition("A 구역 보관 등급","등급 순서: 원형의 저온 보관 → 삼각의 격리 보관 → 사각의 밀폐 보관.","색이 아닌 보관 등급의 순서다. ○ → △ → □.","보안 연구실")},
            {"b_rules",new ClueDefinition("시료 관리 규칙","특수 반응, 냉동 이력, 붉은 삼각 폐기 표시, 첨가제 X, 개봉 봉인은 각각 제외 대상.","안전한 생리식염수는 미개봉이며 기준 코드와 일치하고 특수 반응이 없다.","약물 연구실")},
            {"patients",new ClueDefinition("환자 추적 기록","배급 중단 이후 증상이 재발했다. 완치 사례는 공개 기록에서 제외되었다.","약에 대한 의존은 감춰졌고 완치 가능성은 통제되고 있다.","정보 연구실")},
            {"experiment",new ClueDefinition("생체 실험 승인서","피험자 동의 절차 생략. 외부 보고 보류. 배급 체계 유지 우선.","무차별 생체 실험과 바이러스 방치가 조직적으로 은폐되었다.","제한 기록실")},
            {"v06",new ClueDefinition("V-06 정제 절차","세 권한을 결합하면 완전한 정제 자료 복구 가능. 설비 안정화와 운용 인력 필요.","완치 치료제의 생산에는 접근 권한과 보존된 복구 기반이 모두 필요하다.","제한 기록실")}
        };
        public static string ItemName(string id) { return Items.TryGetValue(id,out var value) ? value.name : id; }
        public static string EndingName(string code) { return code=="A"?"다시, 내일":code=="B"?"빌려 온 평온":code=="C"?"남겨진 가능성":"닫힌 문 너머"; }
        public static string EndingText(string code,GameState s)
        {
            if(code=="A")return "세 조각이 정제 자료를 복원했다. 우리가 살려 둔 사람과 보존한 기록이 멈춰 가던 시설을 다시 움직였다.\n\nV-06이 생산되기 시작했고 동생은 더 이상 다음 배급을 기다리지 않아도 됐다.\n\n연구소의 실험과 방치의 기록이 세상에 공개됐다. 무너진 세계가 하루아침에 돌아오지는 않았다. 그래도 이번에는, 시작할 수 있었다.";
            if(code=="B")return "약을 받은 날, 동생의 기침은 잦아들었다. 다은은 그 옆에서 오랜만에 잠들었다.\n\n약 봉투에는 다음 배급일이 적혀 있었다. 완치라는 말은 어디에도 없었다.\n\n연구소의 문은 다시 닫혔다. 가족의 평온은 이제 그 문 안에 있는 사람들의 결정에 달려 있었다.";
            if(code=="D")return "마지막 저항이 멈추자 정제 시설의 불빛도 멀어졌다.\n\n세 사람은 다시 수용 구역으로 옮겨졌다. 동생에게 돌아갈 약도, 바깥에 전할 기록도 손에 남지 않았다.\n\n하지만 이 선택의 직전으로 돌아가, 다른 방법을 찾을 수 있다.";
            string cause = s.fragments.Count<3 ? "접근 권한이 부족해 정제 자료 일부를 복원하지 못했다." : "정제 자료는 남았지만 시설 복구에 필요한 사람과 보존된 지원 기록이 부족했다.";
            if(s.fragments.Count<3 && !s.Humane)cause += " 복구를 도울 사람과 자료도 충분하지 않았다.";
            return "소장의 통제는 끝났다. 그러나 모든 것을 되돌릴 수는 없었다.\n\n"+cause+"\n\n남은 백업 자료로 동생의 중증 증상을 겨우 완화했다. 다은은 다시 공구를 꺼냈다. 아직 고쳐야 할 것이 남아 있었다.";
        }
    }
}

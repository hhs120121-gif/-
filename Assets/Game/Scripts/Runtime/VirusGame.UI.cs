using System;
using System.Linq;
using UnityEngine;

namespace SecretVirus
{
    public partial class VirusGame
    {
        readonly Color ink=PixelArt.C("#101D26"),panel=PixelArt.C("#172A33"),line=PixelArt.C("#3B5158"),paper=PixelArt.C("#EAE6D5"),muted=PixelArt.C("#9CB2B1"),gold=PixelArt.C("#DFBD78"),mint=PixelArt.C("#88C9B1"),red=PixelArt.C("#D78075");
        int battleMenu;float uiTime;
        [NonSerialized] Texture2D titleIllustration;
        void OnGUI()
        {
            if(!Ready)return;uiTime=Time.unscaledTime;float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);float ox=(Screen.width-1280*scale)/2,oy=(Screen.height-720*scale)/2;
            GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.blackTexture);GUI.matrix=Matrix4x4.TRS(new Vector3(ox,oy,0),Quaternion.identity,new Vector3(scale,scale,1));GUI.color=Color.white;GUI.BeginGroup(new Rect(0,0,1280,720));
            buttons.Clear();GUI.DrawTexture(new Rect(0,0,1280,720),World.Display(Mode),ScaleMode.StretchToFill,false);
            AtmosphereUI();
            if(Mode==GameMode.Title)TitleUI();else if(Mode==GameMode.Opening)OpeningUI();else if(Mode==GameMode.Ending)EndingUI();else if(Mode==GameMode.Gallery)GalleryUI();else{
                if(Mode!=GameMode.Battle)FieldUI();switch(Mode){case GameMode.Dialogue:DialogueUI();break;case GameMode.Inventory:InventoryUI();break;case GameMode.Crafting:CraftUI();break;case GameMode.PuzzleA:PuzzleAUI();break;case GameMode.PuzzleB:PuzzleBUI();break;case GameMode.Debate:DebateUI();break;case GameMode.Battle:BattleUI();break;case GameMode.Menu:MenuUI();break;case GameMode.Settings:SettingsUI();break;}
            }
            if(toastTime>0){Rect r=new Rect(245,76,790,48);Fill(r,new Color(.06f,.12f,.15f,.96f));Fill(new Rect(r.x,r.y,3,r.height),gold);Text(new Rect(r.x+18,r.y+8,r.width-30,34),toast,17,paper);}
            if(actionPulse>0&&State.stage==1&&(Mode==GameMode.Crafting||Mode==GameMode.Dialogue)){for(int i=0;i<16;i++){float radius=(1-actionPulse)*130,angle=i*2.4f;Fill(new Rect(640+Mathf.Cos(angle)*radius,310+Mathf.Sin(angle)*radius*.5f,4,4),new Color(gold.r,gold.g,gold.b,actionPulse*(Settings.flashes?1:.35f)));}}
            if(sceneFade>0&&Mode==GameMode.Field)Fill(new Rect(0,0,1280,720),new Color(0,0,0,sceneFade));
            if(binding!=""){Shade(.85f);Text(new Rect(270,280,740,50),"새 조작키를 누르세요",32,paper,TextAnchor.MiddleCenter);Text(new Rect(220,350,840,90),"ESC 취소 · 방향키, Enter, Tab, 숫자 1~3은 고정 조작입니다.\n이미 배정된 키는 중복 사용할 수 없습니다.",19,muted,TextAnchor.MiddleCenter);}
            GUI.EndGroup();GUI.matrix=Matrix4x4.identity;
        }
        void Fill(Rect r,Color c){Color old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        void Border(Rect r,Color c,float thickness=1){Fill(new Rect(r.x,r.y,r.width,thickness),c);Fill(new Rect(r.x,r.yMax-thickness,r.width,thickness),c);Fill(new Rect(r.x,r.y,thickness,r.height),c);Fill(new Rect(r.xMax-thickness,r.y,thickness,r.height),c);}
        void Text(Rect r,string text,int size,Color color,TextAnchor anchor=TextAnchor.UpperLeft,bool bold=false)
        {
            var style=new GUIStyle(GUI.skin.label){font=font,fontSize=Mathf.RoundToInt(size*Settings.textScale),wordWrap=true,alignment=anchor,fontStyle=bold?FontStyle.Bold:FontStyle.Normal,richText=false};style.normal.textColor=color;
            int minimum=Mathf.Max(11,Mathf.RoundToInt(style.fontSize*.78f));while(style.fontSize>minimum&&style.CalcHeight(new GUIContent(text),r.width)>r.height)style.fontSize--;
            GUI.Label(r,text,style);
        }
        void Small(float x,float y,string text,Color? color=null){Text(new Rect(x,y,1000,25),text,12,color??muted);}
        void Shade(float a=.78f){Fill(new Rect(0,0,1280,720),new Color(.025f,.06f,.085f,a));}
        void Panel(Rect r){Fill(new Rect(r.x+5,r.y+6,r.width,r.height),new Color(0,0,0,.25f));Fill(r,panel);Border(r,line);}
        void Heading(string eyebrow,string title,string description="")
        {
            Shade();Panel(new Rect(70,64,1140,606));Small(102,84,eyebrow,gold);Text(new Rect(100,110,1060,49),title,30,paper);if(description!="")Text(new Rect(102,159,1058,50),description,17,muted);Fill(new Rect(100,210,1080,1),line);
        }
        void Button(Rect rect,string label,Action action,bool enabled=true,bool primary=false)
        {
            enabled=enabled&&!BattlePresenting;int index=buttons.Count;buttons.Add(new UiButton{text=label,action=action,enabled=enabled,rect=rect});bool selected=index==menuFocus,hover=rect.Contains(Event.current.mousePosition);
            Color bg=!enabled?new Color(.11f,.17f,.20f):primary?PixelArt.C("#345951"):selected||hover?PixelArt.C("#304850"):PixelArt.C("#20343D");Fill(rect,bg);Border(rect,!enabled?line:selected||hover?gold:primary?mint:line,selected?2:1);
            Text(new Rect(rect.x+14,rect.y+3,rect.width-28,rect.height-6),label,17,!enabled?PixelArt.C("#647878"):paper,TextAnchor.MiddleLeft);
            if(enabled&&GUI.Button(rect,GUIContent.none,GUIStyle.none)){menuFocus=index;sound.Play("tick");pendingAction=action;}
        }
        void Icon(Rect r,string id,float alpha=1){Color old=GUI.color;GUI.color=new Color(1,1,1,alpha);GUI.DrawTexture(r,PixelArt.Icon(id),ScaleMode.ScaleToFit);GUI.color=old;}
        void Portrait(Rect r,int hero,int mood=0)
        {
            if(titleIllustration==null)titleIllustration=Resources.Load<Texture2D>("TitleIllustration");
            if(hero>=0&&hero<3&&titleIllustration!=null){
                // Pixel bounds in the original 1672 x 941 illustration, measured from its top left.
                Rect crop=hero==0?new Rect(953,142,300,360):hero==1?new Rect(752,118,230,276):new Rect(1248,4,260,312);
                float width=Mathf.Min(r.width,r.height*crop.width/crop.height),height=width*crop.height/crop.width;
                Rect frame=new Rect(r.center.x-width*.5f,r.center.y-height*.5f,width,height);
                GUI.DrawTextureWithTexCoords(frame,titleIllustration,new Rect(crop.x/1672f,1-(crop.y+crop.height)/941f,crop.width/1672f,crop.height/941f));
                Border(frame,line);
                return;
            }
            GUI.DrawTexture(r,World.Portrait(hero),ScaleMode.ScaleToFit);
        }
        void Bar(Rect r,float fraction,Color color,string label)
        {
            Fill(r,PixelArt.C("#10212A"));Fill(new Rect(r.x,r.y,r.width*Mathf.Clamp01(fraction),r.height),color);Border(r,line);Text(new Rect(r.x+9,r.y-1,r.width-18,r.height+1),label,14,paper,TextAnchor.MiddleLeft);
        }
        void TitleUI()
        {
            if(titleIllustration==null)titleIllustration=Resources.Load<Texture2D>("TitleIllustration");
            if(titleIllustration!=null)GUI.DrawTexture(new Rect(0,0,1280,720),titleIllustration,ScaleMode.ScaleAndCrop);
            for(int x=0;x<720;x+=8)Fill(new Rect(x,0,8,720),new Color(.018f,.04f,.055f,.30f*(1-Mathf.SmoothStep(0,1,x/720f))));
            Small(78,80,"G.M.R.I.  /  ARCHIVE 20XX",gold);Fill(new Rect(80,122,50,3),gold);
            Text(new Rect(72,145,660,95),"바이러스의",62,paper,TextAnchor.UpperLeft,true);Text(new Rect(72,230,660,95),"비밀",62,paper,TextAnchor.UpperLeft,true);Small(80,337,"THE SECRET OF VIRUS",gold);
            Text(new Rect(80,377,520,80),"동생을 구할 치료제.\n그것을 감춘 사람들. 우리가 남길 선택.",21,muted);
            bool hasSave=System.IO.File.Exists(SaveStore.SlotPath("auto"));
            Button(new Rect(80,480,340,51),"새로운 여정",()=>{if(hasSave)Say("새로운 여정",-1,new[]{"새 여정을 시작하면 첫 자동 저장 이후 이전 자동 저장을 대체합니다.\n수동 저장과 엔딩 감상 기록은 유지됩니다."},null,new Choice("새로 시작",NewGame),new Choice("돌아가기",()=>SetMode(GameMode.Title)));else NewGame();},true,true);
            Button(new Rect(80,543,340,51),"이어 하기",()=>LoadGame("auto"),hasSave);
            Button(new Rect(445,480,175,51),"환경 설정",OpenSettings);Button(new Rect(445,543,175,51),"엔딩 기록",()=>SetMode(GameMode.Gallery));Button(new Rect(445,606,175,40),"종료",QuitPrompt);
            Small(80,659,"방향키 이동  ·  Z / Enter 확인  ·  X / Shift 달리기  ·  C 동료 교체");
        }
        void OpeningUI()
        {
            Fill(new Rect(0,0,1280,720),PixelArt.C("#090F16"));Small(100,86,"G.M.R.I. PUBLIC TRANSMISSION",gold);
            string lineText=Catalog.Opening[Mathf.Clamp(openingIndex,0,Catalog.Opening.Length-1)];int chars=textComplete?lineText.Length:Mathf.Min(lineText.Length,(int)((uiTime-openingStart)*Settings.textSpeed));if(chars>=lineText.Length)textComplete=true;
            Text(new Rect(145,245,990,210),lineText.Substring(0,chars),31,paper,TextAnchor.MiddleCenter);
            for(int i=0;i<5;i++)Fill(new Rect(592+i*21,555,8,8),i<=openingIndex?gold:line);
            Text(new Rect(0,653,1280,36),"Z / Enter 문장 진행 · 길게 누르면 건너뛰기 · ESC 전체 건너뛰기",15,muted,TextAnchor.MiddleCenter);
        }
        void FieldUI()
        {
            Fill(new Rect(0,0,1280,67),new Color(.045f,.09f,.12f,.96f));Fill(new Rect(0,66,1280,1),line);
            Portrait(new Rect(17,6,44,53),State.leader,State.hp<30?3:0);Text(new Rect(76,9,140,27),Catalog.Heroes[State.leader],20,paper);Small(76,37,Catalog.Jobs[State.leader]);
            Bar(new Rect(202,22,153,21),State.hp/100f,State.poison?red:mint,"파티 "+State.hp+" / 100");
            Text(new Rect(387,10,665,25),Catalog.Stages[State.stage].title,19,paper);Text(new Rect(387,36,755,26),Objective(),14,muted);
            Text(new Rect(1100,19,158,25),State.stage.ToString("00")+" / 14",17,gold,TextAnchor.MiddleRight);
            Fill(new Rect(0,677,1280,43),new Color(.045f,.09f,.12f,.97f));Small(23,688,"이동 ↑↓←→   ·   확인 "+((KeyCode)Settings.interact)+" / Enter   ·   달리기 X / Shift   ·   교체 C / 1·2·3   ·   기록 I / Tab   ·   메뉴 ESC");
            if(Mode==GameMode.Field){var target=World.Nearest();if(target!=null&&!World.moving){Vector2 p=World.ScreenPoint(target.position);float x=Mathf.Clamp(p.x-150,15,965),y=Mathf.Clamp(p.y-66,85,605);Fill(new Rect(x,y,300,37),new Color(.04f,.10f,.14f,.95f));Border(new Rect(x,y,300,37),gold);Text(new Rect(x+10,y+3,280,30),"[ "+(KeyCode)Settings.interact+" ]  "+target.label,16,paper,TextAnchor.MiddleCenter);}
                if(State.stage==7&&World.alert>.03f)Bar(new Rect(475,90,330,23),World.alert,red,"경계도 · 걷거나 시야 밖으로 이동");
                if(idleTime>20){Text(new Rect(390,610,650,42),"주변 물건을 바라보고 멈춘 뒤 조사해 보세요. 목표는 화면 위에서 확인할 수 있습니다.",15,paper);var hint=World.things.FirstOrDefault(t=>t.id==(State.stage==1?(State.tutorial<3?"request":State.tutorial<4?Catalog.Repair.parts.FirstOrDefault(p=>!State.Has("collected_"+p)):"craft"):"exit"));if(hint!=null){Vector2 p=World.ScreenPoint(hint.position);Text(new Rect(Mathf.Clamp(p.x-20,10,1230),Mathf.Clamp(p.y-90,90,610),40,40),"▼",27,gold,TextAnchor.MiddleCenter);}}
            }
        }
        void DialogueUI()
        {
            Fill(new Rect(0,0,1280,720),new Color(0,0,0,.18f));Panel(new Rect(55,365,1170,295));
            float x=dialogHero>=0?235:90,w=dialogHero>=0?930:1100;
            if(dialogHero>=0)Portrait(new Rect(80,395,125,150),dialogHero);
            Small(x,388,dialogSpeaker,gold);string message=dialogLines.Count>0?dialogLines[dialogIndex]:"";int chars=textComplete?message.Length:Mathf.Min(message.Length,(int)((uiTime-dialogStart)*Settings.textSpeed));if(chars>=message.Length)textComplete=true;
            Text(new Rect(x,425,w,140),message.Substring(0,chars),20,paper);
            if(dialogIndex==dialogLines.Count-1&&dialogChoices.Count>0){for(int i=0;i<dialogChoices.Count;i++){var choice=dialogChoices[i];float bw=(1090-(dialogChoices.Count-1)*14)/dialogChoices.Count;Button(new Rect(90+i*(bw+14),590,bw,48),choice.text,()=>{if(!textComplete){textComplete=true;return;}choice.action();},true,i==0);}}
            else Button(new Rect(965,590,220,46),textComplete?"계속  →":"문장 바로 표시",NextDialogue,true,true);
        }
        void InventoryUI()
        {
            Heading("PARTY / ARCHIVE","소지품과 기록");
            Button(new Rect(102,164,180,36),"소지품",()=>{inventoryTab=0;menuFocus=0;},true,inventoryTab==0);Button(new Rect(296,164,180,36),"단서 기록",()=>{inventoryTab=1;menuFocus=1;},true,inventoryTab==1);Button(new Rect(490,164,180,36),"파티와 여정",()=>{inventoryTab=2;menuFocus=2;},true,inventoryTab==2);
            if(inventoryTab==0){
                for(int i=0;i<20;i++){int index=i;float x=103+(i%5)*100,y=235+(i/5)*88;bool exists=i<State.items.Count;Fill(new Rect(x,y,88,77),i==selectedItem?PixelArt.C("#304B50"):ink);Border(new Rect(x,y,88,77),i==selectedItem?gold:line);if(exists){var item=State.items[i];Icon(new Rect(x+17,y+8,46,46),item.id);Text(new Rect(x+49,y+48,30,23),item.count.ToString(),15,paper,TextAnchor.MiddleRight);Button(new Rect(x,y,88,77),"",()=>selectedItem=index);Icon(new Rect(x+17,y+8,46,46),item.id);Text(new Rect(x+49,y+48,30,23),item.count.ToString(),15,paper,TextAnchor.MiddleRight);}}
                Text(new Rect(647,236,490,35),State.items.Count+" / 20칸",19,muted);
                if(State.items.Count>0){selectedItem=Mathf.Clamp(selectedItem,0,State.items.Count-1);var item=State.items[selectedItem];Text(new Rect(646,290,480,39),Catalog.ItemName(item.id),28,paper);Text(new Rect(646,345,490,108),Catalog.Items[item.id].description,20,muted);Button(new Rect(648,497,245,46),"사용한다",()=>UseInventoryItem(item.id),item.id=="med"||item.id=="antidote"||item.id=="unlock");if(State.tutorial>=5)Button(new Rect(910,497,225,46),"1개 버리기",()=>{string itemId=item.id;Say("소지품 정리",-1,new[]{Catalog.ItemName(itemId)+" 1개를 버릴까요?"},null,new Choice("버린다",()=>{Inventory.Take(State,itemId,1);SetMode(GameMode.Inventory);}),new Choice("돌아간다",()=>SetMode(GameMode.Inventory)));},item.id!="repair");}
            }else if(inventoryTab==1){
                if(State.clues.Count==0)Text(new Rect(115,247,920,90),"아직 기록한 단서가 없습니다. 주변 문서와 물건을 조사하세요.",22,muted);
                for(int i=0;i<State.clues.Count;i++){int index=i;Button(new Rect(103,232+i*43,290,37),Catalog.Clues[State.clues[i]].title,()=>selectedClue=index);}
                if(State.clues.Count>0){selectedClue=Mathf.Clamp(selectedClue,0,State.clues.Count-1);var clue=Catalog.Clues[State.clues[selectedClue]];Small(430,237,clue.place,gold);Text(new Rect(430,274,705,40),clue.title,27,paper);Text(new Rect(430,332,705,95),clue.text,21,paper);Fill(new Rect(431,445,690,1),line);Small(430,467,"해석",mint);Text(new Rect(430,503,705,100),clue.interpretation,20,muted);}
            }else{
                for(int i=0;i<3;i++){Portrait(new Rect(110+i*180,238,102,123),i);Text(new Rect(110+i*180,374,160,30),Catalog.Heroes[i],23,i<State.joined?paper:muted);Text(new Rect(110+i*180,409,164,72),i<State.joined?Catalog.Talents[i]:"아직 합류하지 않음",15,muted);}
                Text(new Rect(720,241,420,40),"열쇠 조각  "+State.fragments.Count+" / 3",26,gold);Text(new Rect(720,300,420,40),"비살상 해결  "+State.Ratio,21,paper);Text(new Rect(720,355,420,112),"완전한 정제 자료: 조각 3개\n복구 기반: 비살상 해결 50% 이상\n최종 전투도 집계에 포함됩니다.",18,muted);Text(new Rect(720,495,420,80),"파티 HP "+State.hp+" / 100"+(State.poison?" · 중독":"")+"\n여정 "+TimeSpan.FromSeconds(State.playSeconds).ToString(@"hh\:mm\:ss"),19,paper);
            }
            Button(new Rect(985,608,190,42),"닫기 · X / ESC",()=>SetMode(GameMode.Field));
        }
        void CraftUI()
        {
            Heading("WORKBENCH / ENGINEERING","작업대");
            if(State.stage!=1){for(int i=0;i<Catalog.Recipes.Length;i++){var recipe=Catalog.Recipes[i];Button(new Rect(102+i*263,164,250,36),recipe.name,()=>{chosenRecipe=recipe;craft=new CraftSession(recipe);craftHeld="";});}}
            else Small(102,176,"의뢰품 · 수리 키트     /     5종, 총 8개 재료",gold);
            Small(110,216,"재료 선택 → 슬롯에 투입  /  배치된 슬롯을 다시 선택하거나 X를 누르면 회수");
            for(int i=0;i<5;i++){int slot=i;float x=110+i*213;Rect r=new Rect(x,246,195,151);Fill(r,ink);Border(r,craft.slots[i]==null?line:craft.Correct(i)?mint:red,2);Icon(new Rect(x+66,262,62,62),craft.slots[i]??chosenRecipe.parts[i],craft.slots[i]==null?.22f:1);Text(new Rect(x+10,331,175,30),Catalog.ItemName(chosenRecipe.parts[i])+" ×"+chosenRecipe.counts[i],17,muted,TextAnchor.MiddleCenter);Button(new Rect(x,363,195,34),craft.slots[i]==null?"투입":"회수 · "+(craft.Correct(i)?"일치":"위치 불일치"),()=>CraftPlace(slot));}
            Small(110,421,craftHeld==""?"아래에서 재료 선택":"선택한 재료 · "+Catalog.ItemName(craftHeld),craftHeld==""?muted:gold);
            string[] parts={"parts","wire","scrap","chip","cloth","board","battery"};for(int i=0;i<parts.Length;i++){string id=parts[i];float x=110+i*151;Icon(new Rect(x+43,465,46,46),id);Text(new Rect(x,518,139,29),Catalog.ItemName(id)+" ×"+Inventory.Count(State,id),15,paper,TextAnchor.MiddleCenter);Button(new Rect(x,553,139,37),craftHeld==id?"선택됨":"선택",()=>craftHeld=id,Inventory.Count(State,id)>0);}
            Small(110,622,"재료는 완성할 때만 소모됩니다.  ·  회수 "+craft.withdrawals+"회");Button(new Rect(961,608,211,42),"제작 마치기",ConfirmLeaveCraft);
        }
        void PuzzleAUI()
        {
            Heading("SECURITY / A","보관함 접근 순서","제임스가 해독한 보관 등급과 기호를 맞추세요. 오답은 재시도할 수 있습니다.");
            Text(new Rect(112,235,1000,45),"저온 보관 → 격리 보관 → 밀폐 보관",24,gold,TextAnchor.MiddleCenter);
            string[] symbols={"○ 원형","△ 삼각","□ 사각"};for(int i=0;i<3;i++){int slot=i;Panel(new Rect(193+i*310,318,270,140));Small(211+i*310,334,"입력 "+(i+1));Button(new Rect(211+i*310,383,234,52),symbols[aSymbols[i]],()=>aSymbols[slot]=(aSymbols[slot]+1)%3);}
            Text(new Rect(180,490,920,45),"각 입력을 선택하면 기호가 바뀝니다. 기록의 색보다 등급의 순서가 중요합니다.",18,muted,TextAnchor.MiddleCenter);
            Button(new Rect(686,584,251,48),"잠금 해제",()=>SubmitA(aSymbols),true,true);Button(new Rect(950,584,223,48),"나중에 다시",()=>SetMode(GameMode.Field));
        }
        void PuzzleBUI()
        {
            Heading("PHARMACOLOGY / B","여섯 시료의 소거","분석은 반복할 수 있습니다. 주입은 한 번만 가능하며 결과가 확정됩니다.");
            for(int i=0;i<6;i++){int index=i;float x=104+i*180;Fill(new Rect(x,236,167,105),selectedSample==i?PixelArt.C("#385450"):ink);GUI.DrawTexture(new Rect(x+52,243,62,62),PixelArt.Prop("vial",i).texture,ScaleMode.ScaleToFit);Button(new Rect(x,345,167,39),"시료 "+(char)('A'+i)+(eliminated[i]?" · 제외":""),()=>selectedSample=index);Button(new Rect(x,394,167,34),eliminated[i]?"제외 취소":"후보 제외",()=>eliminated[index]=!eliminated[index]);}
            Text(new Rect(116,460,1035,74),SampleDescription(selectedSample),23,paper);Small(117,541,"안전 조건: 미개봉 / 기준 코드 일치 / 특수 반응 없음",gold);
            Button(new Rect(731,593,250,46),"선택 시료 주입",ConfirmSample,!eliminated[selectedSample],true);Button(new Rect(997,593,175,46),"나가기",()=>SetMode(GameMode.Field));
        }
        void DebateUI()
        {
            Heading("INFORMATION / C","질서를 위한 통제인가","단서에 근거해 답하세요. 대화를 멈추고 기록을 더 찾을 수 있습니다.");Portrait(new Rect(113,243,138,166),5);
            string[] questions={"당신들이 여기 들어온 이유는 무엇입니까?\n모두를 구하겠다는 말은 듣고 싶지 않군요.","약의 배급을 통제하지 않았다면 사회는 이미 무너졌을 겁니다.\n그 판단이 잘못됐다는 근거가 있습니까?","기록의 모순은 인정하겠습니다.\n그렇다고 공개가 해답이라는 보장은 없지 않습니까?"};
            Text(new Rect(290,252,840,135),questions[Mathf.Min(debateStep,2)],25,paper);
            if(debateStep==0){Button(new Rect(291,421,846,49),"처음에는 동생을 구하려고 왔습니다. 지금은 확인할 것이 더 생겼습니다.",()=>DebateAnswer(true));Button(new Rect(291,482,846,49),"처음부터 세상 모든 사람을 구하러 왔습니다.",()=>DebateAnswer(false));}
            else if(debateStep==1){Button(new Rect(291,421,846,49),"배급 기록과 환자 기록을 제시한다 · 반복 의존과 비공개 연구",()=>DebateAnswer(true),State.clues.Contains("supply")&&State.clues.Contains("patients"));Button(new Rect(291,482,846,49),"연구소가 하는 일은 전부 틀렸다고 단정한다",()=>DebateAnswer(false));}
            else {Button(new Rect(291,421,846,49),"통제를 유지하는 대신, 자료와 당신의 증언으로 다른 길을 만들고 싶습니다.",()=>DebateAnswer(true));Button(new Rect(291,482,846,49),"협조하지 않으면 당신도 같은 대가를 치를 겁니다.",()=>DebateAnswer(false));}
            Button(new Rect(900,584,236,43),"물러나 더 조사한다",()=>SetMode(GameMode.Field));Small(115,602,"문답 "+(debateStep+1)+" / 3",gold);
        }
        void BattleUI()
        {
            int shownHp=Presented(previousEnemyHp,Battle.hp),shownBlue=Presented(previousSubdue,Battle.subdue),shownGreen=Presented(previousPersuade,Battle.persuade),shownParty=Presented(previousPartyHp,State.hp,true);
            Fill(new Rect(0,0,1280,145),new Color(.025f,.06f,.085f,.92f));Fill(new Rect(43,149,403,245),new Color(.025f,.06f,.085f,.75f));Fill(new Rect(753,157,480,237),new Color(.025f,.06f,.085f,.75f));
            Small(55,22,"ENCOUNTER / "+(Battle.enemy.boss?"PHASE "+Battle.phase+" OF 3":"ROUND "+Battle.round),gold);Text(new Rect(52,53,700,45),Battle.enemy.name,30,paper);
            Bar(new Rect(749,33,475,26),(float)shownHp/Battle.enemy.hp,red,"HP  "+shownHp+" / "+Battle.enemy.hp+"  ·  0이면 살상 / 파괴");Bar(new Rect(749,72,475,26),shownBlue/100f,PixelArt.C("#719CBF"),"제압  "+shownBlue+" / 100  ·  비살상");Bar(new Rect(749,111,475,26),shownGreen/100f,mint,Battle.enemy.machine?"설득 불가 · 자동 장치":"설득  "+shownGreen+" / 100  ·  평화 해결");
            float shake=Settings.shake?Mathf.Sin(uiTime*75)*actionPulse*7:0;
            if(actionPulse>0){for(int i=0;i<12;i++){float angle=i*2.4f,radius=(1-actionPulse)*140;Fill(new Rect(606+Mathf.Cos(angle)*radius,272+Mathf.Sin(angle)*radius*.6f,4,4),new Color(mint.r,mint.g,mint.b,actionPulse));}if(Settings.flashes)Fill(new Rect(470,163,290,220),new Color(.8f,.9f,.8f,actionPulse*.08f));}
            Portrait(new Rect(54,155,93,112),State.leader,State.hp<30?3:0);Text(new Rect(164,165,310,31),Catalog.Heroes[State.leader]+" · "+Catalog.Jobs[State.leader],20,paper);Bar(new Rect(165,216,270,25),shownParty/100f,State.poison?red:mint,"파티 HP "+shownParty+" / 100");Text(new Rect(55,289,390,85),Battle.toolsUsed>0?"두 번째 도구 사용 가능\n리더는 이 턴 동안 다은으로 고정됩니다.":"C / 1·2·3으로 리더 교체\n행동을 실행할 때 리더가 확정됩니다.",17,muted);
            Text(new Rect(771,179,450,52),BattlePresenting?"행동 결과 확인 중":Battle.finished?"대치 종료":Battle.Intent,19,gold);Text(new Rect(771,247,450,127),Battle.analyzed?Battle.enemy.hint:"분석으로 적의 상태와 유효한 대응을 확인할 수 있습니다.",19,muted);
            Panel(new Rect(49,416,1179,91));Text(new Rect(68,429,1137,73),PresentedLog(),17,paper);BattleFeedback();
            if(Battle.finished&&!BattlePresenting){Button(new Rect(857,572,370,58),Battle.won?(Battle.enemy.boss?"결말 확인":"사건 결과 확인"):(Battle.enemy.boss?"파국 엔딩 확인":"전투 직전부터 다시"),FinishBattle,true,true);Text(new Rect(60,556,730,84),Battle.won?(Battle.result==Resolution.Lethal?"살상 / 파괴로 종료":"비살상 해결 성공"):"파티가 무력화되었습니다.",28,Battle.won?mint:red);return;}
            string[] commands={"A  공격 / 제압","T  분석","T  도구 / 포획","C  설득"};for(int i=0;i<4;i++){int tab=i;Button(new Rect(51+i*296,525,282,48),commands[i],()=>{battleMenu=tab;menuFocus=4;},Battle.toolsUsed==0||i==2,i==battleMenu);}
            if(battleMenu==0){Button(new Rect(51,589,366,51),"공격 · HP -"+Battle.AttackDamage,()=>BattleAct(BattleAction.Attack));Button(new Rect(435,589,366,51),"제압 · +"+(Battle.ShieldUp?15:30)+" / 30% 확률 +15",()=>BattleAct(BattleAction.Subdue),State.leader==2);Text(new Rect(830,590,390,66),"제압은 적 HP를 줄이지 않습니다.\n다니엘만 사용할 수 있습니다.",17,muted);}
            if(battleMenu==1){Button(new Rect(51,589,366,51),"분석 실행 · 상태와 단서 확인",()=>BattleAct(BattleAction.Analyze));Text(new Rect(443,587,770,75),"처음 분석한 페이즈는 설득 +10.\n제임스는 다음 턴 첫 공격 피해 +25%. 반복해서 중첩되지 않습니다.",18,muted);}
            if(battleMenu==2){Button(new Rect(51,589,285,51),"구속 +"+Battle.ToolAmount(20)+"  ["+Inventory.Count(State,"trap")+"]",()=>BattleAct(BattleAction.Trap),Inventory.Count(State,"trap")>0);Button(new Rect(347,589,285,51),"회복 +"+Battle.ToolAmount(30)+"  ["+Inventory.Count(State,"med")+"]",()=>BattleAct(BattleAction.Heal),Inventory.Count(State,"med")>0&&State.hp<100);Button(new Rect(643,589,285,51),"중화  ["+Inventory.Count(State,"antidote")+"]",()=>BattleAct(BattleAction.Antidote),State.poison&&Inventory.Count(State,"antidote")>0);Button(new Rect(939,589,285,51),"턴 마치기",()=>BattleAct(BattleAction.Wait),Battle.toolsUsed>0);}
            if(battleMenu==3){string evidence=Battle.enemy.id=="guard"?"안전하게 돌아갈 길이 있다":Battle.enemy.id=="b"?"당신에게 투여한 약을 먼저 확인하자":Battle.enemy.boss?"실험 승인서와 V-06 자료를 제시한다":"배급·환자 기록의 모순을 설명한다";Button(new Rect(51,589,570,51),evidence,()=>BattleAct(BattleAction.Persuade,true),!Battle.enemy.machine);Button(new Rect(638,589,586,51),"겁먹지 말고 무조건 내 말을 따라라",()=>BattleAct(BattleAction.Persuade,false),!Battle.enemy.machine);}
            Small(53,671,"예측 가능한 기본 효과 + 별도 추가 확률  ·  세 게이지는 합산하지 않습니다.");
        }
        void MenuUI()
        {
            Shade(.88f);Panel(new Rect(427,104,426,517));Small(462,131,"PAUSED",gold);Text(new Rect(460,166,335,45),"잠시 숨을 고르며",28,paper);
            Button(new Rect(461,234,359,47),"계속하기",()=>SetMode(returnMode),true,true);Button(new Rect(461,294,359,47),"수동 저장",()=>Save("manual"),returnMode==GameMode.Field&&State.tutorial>=5);Button(new Rect(461,354,359,47),"수동 저장 불러오기",()=>LoadGame("manual"),System.IO.File.Exists(SaveStore.SlotPath("manual")));Button(new Rect(461,414,359,47),"환경 설정",OpenSettings);Button(new Rect(461,474,359,47),"타이틀로",()=>Say("타이틀로 돌아가기",-1,new[]{"마지막 저장 이후 진행은 유지되지 않습니다. 타이틀로 돌아갈까요?"},null,new Choice("돌아간다",()=>SetMode(GameMode.Title)),new Choice("계속한다",()=>SetMode(returnMode))));Button(new Rect(461,534,359,47),"게임 종료",QuitPrompt);
        }
        void SettingsUI()
        {
            Heading("PREFERENCES","환경 설정","변경 사항은 자동으로 보관됩니다. 설정은 모든 회차에 공통으로 적용됩니다.");
            SettingRow(105,231,"전체 음량",Settings.master.ToString("P0"),()=>{Settings.master=Mathf.Max(0,Settings.master-.1f);PersistSettings();},()=>{Settings.master=Mathf.Min(1,Settings.master+.1f);PersistSettings();});
            SettingRow(105,295,"배경음",Settings.music.ToString("P0"),()=>{Settings.music=Mathf.Max(0,Settings.music-.1f);PersistSettings();},()=>{Settings.music=Mathf.Min(1,Settings.music+.1f);PersistSettings();});
            SettingRow(105,359,"효과음",Settings.effects.ToString("P0"),()=>{Settings.effects=Mathf.Max(0,Settings.effects-.1f);PersistSettings();},()=>{Settings.effects=Mathf.Min(1,Settings.effects+.1f);PersistSettings();});
            SettingRow(105,423,"글자 크기",Settings.textScale.ToString("P0"),()=>{Settings.textScale=Mathf.Max(.85f,Settings.textScale-.05f);PersistSettings();},()=>{Settings.textScale=Mathf.Min(1.2f,Settings.textScale+.05f);PersistSettings();});
            SettingRow(105,487,"글자 속도",Settings.textSpeed.ToString("0"),()=>{Settings.textSpeed=Mathf.Max(10,Settings.textSpeed-10);PersistSettings();},()=>{Settings.textSpeed=Mathf.Min(100,Settings.textSpeed+10);PersistSettings();});
            Button(new Rect(668,235,493,43),"화면 흔들림 · "+(Settings.shake?"켜짐":"꺼짐"),()=>{Settings.shake=!Settings.shake;PersistSettings();});Button(new Rect(668,289,493,43),"강한 깜빡임 · "+(Settings.flashes?"켜짐":"줄이기"),()=>{Settings.flashes=!Settings.flashes;PersistSettings();});
            Button(new Rect(668,359,237,42),"확인 · "+(KeyCode)Settings.interact,()=>binding="interact");Button(new Rect(920,359,240,42),"취소 · "+(KeyCode)Settings.cancel,()=>binding="cancel");Button(new Rect(668,414,237,42),"교체 · "+(KeyCode)Settings.switchHero,()=>binding="switch");Button(new Rect(920,414,240,42),"기록 · "+(KeyCode)Settings.inventory,()=>binding="inventory");
            Text(new Rect(670,482,495,68),"표시되는 정보에는 색과 문자를 함께 사용합니다.\n전투 선택에는 시간 제한이 없습니다.",17,muted);Button(new Rect(952,599,211,44),"돌아가기",()=>{PersistSettings();SetMode(settingsReturn);});
        }
        void SettingRow(float x,float y,string name,string value,Action minus,Action plus){Text(new Rect(x,y+7,200,37),name,21,paper);Button(new Rect(x+220,y,52,41),"−",minus);Text(new Rect(x+278,y+7,90,31),value,19,gold,TextAnchor.MiddleCenter);Button(new Rect(x+382,y,52,41),"+",plus);}
        void EndingUI()
        {
            Fill(new Rect(0,0,945,720),new Color(.025f,.06f,.085f,.94f));Small(85,61,"ENDING "+endingCode+" / "+(endingCode=="A"?"TRUE ENDING":endingCode=="D"?"BAD ENDING":"ALTERNATE ENDING"),gold);Text(new Rect(80,112,970,65),Catalog.EndingName(endingCode),44,paper);
            Fill(new Rect(85,200,55,3),gold);Text(new Rect(83,246,840,320),Catalog.EndingText(endingCode,State),23,paper);
            Portrait(new Rect(1000,240,120,144),0,endingCode=="A"?2:1);Text(new Rect(949,414,269,113),"열쇠 "+State.fragments.Count+" / 3\n비살상 "+State.Ratio+"\n\n선택의 기록은 남습니다.",17,muted,TextAnchor.UpperCenter);
            Button(new Rect(82,604,380,50),"최종 선택 직전부터 다시",()=>LoadGame("before-final"),System.IO.File.Exists(SaveStore.SlotPath("before-final")),true);Button(new Rect(480,604,250,50),"타이틀로",()=>SetMode(GameMode.Title));Small(83,679,"THE SECRET OF VIRUS   ·   당신이 남긴 선택으로 끝나는 이야기");
        }
        void GalleryUI()
        {
            Heading("ENDING ARCHIVE","남겨진 여정","감상한 엔딩은 새 게임을 시작해도 유지됩니다.");string[] codes={"A","B","C","D"};
            for(int i=0;i<4;i++){string code=codes[i];float x=105+i*270;bool seen=Settings.endings.Contains(code);Panel(new Rect(x,249,249,281));Text(new Rect(x+20,270,209,65),code,45,seen?gold:line);Text(new Rect(x+20,363,210,70),seen?Catalog.EndingName(code):"아직 닿지 않은 결말",24,seen?paper:muted);Text(new Rect(x+20,459,210,42),seen?"감상 완료":"미발견",16,seen?mint:muted);}
            Text(new Rect(107,552,805,45),"세 사람의 전문성, 남겨 둔 생명과 기록, 마지막 거래가 결말을 바꿉니다.",18,muted);Button(new Rect(952,604,218,43),"타이틀로",()=>SetMode(GameMode.Title));
        }
    }
}

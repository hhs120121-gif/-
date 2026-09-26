using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SecretVirus.Editor
{
    public static class RulesChecks
    {
        static List<string> passed;
        static void Check(bool result,string name){if(!result)throw new Exception("FAILED: "+name);passed.Add(name);}
        static GameState State(){return new GameState{tutorial=5,joined=3,stage=8,highestStage=8};}
        public static string Run()
        {
            passed=new List<string>();var s=State();
            Check(Inventory.Add(s,"wire",21)&&s.items.Count==2&&s.items[0].count==20,"stack limit 20 and acquisition order");
            Check(!Inventory.Take(s,"wire",22)&&Inventory.Count(s,"wire")==21,"failed removal is atomic");
            Check(Inventory.Take(s,"wire",20)&&s.items.Count==1&&s.items[0].count==1,"empty stacks removed");
            Check(!Inventory.Add(s,"unknown",1)&&!Inventory.Add(s,"wire",0),"reject unknown and nonpositive items");
            s=State();Check(Inventory.Add(s,"wire",400),"twenty slots capacity");Check(!Inventory.Add(s,"wire",1)&&Inventory.Count(s,"wire")==400,"full inventory keeps source transaction intact");
            s=State();for(int i=0;i<5;i++)Inventory.Add(s,Catalog.Repair.parts[i],Catalog.Repair.counts[i]);var craft=new CraftSession(Catalog.Repair);
            for(int i=0;i<5;i++)Check(craft.Place(s,Catalog.Repair.parts[i],i),"craft reserve slot "+i);
            Check(Inventory.Count(s,"wire")==2,"reservation does not consume inventory");Check(craft.Complete&&craft.Commit(s)&&Inventory.Count(s,"repair")==1&&Inventory.Count(s,"wire")==0,"five types eight units craft transaction");Check(!craft.Commit(s)&&Inventory.Count(s,"repair")==1,"craft cannot commit twice");
            s=State();Inventory.Add(s,"wire",20);craft=new CraftSession(Catalog.Repair);for(int i=0;i<10;i++){Check(craft.Place(s,"wire",0),"wrong placement "+i);Check(craft.Withdraw(0),"withdrawal "+i);}Check(craft.withdrawals==10&&!craft.Withdraw(0)&&Inventory.Count(s,"wire")==20,"withdraw Easter egg counts actual returns only");
            s=State();Check(s.Record("c",Resolution.Peaceful)&&!s.Record("c",Resolution.Lethal)&&s.NonLethal==1&&s.Total==1,"unique peaceful event counts once");s.Record("avoid",Resolution.Avoided);Check(s.Total==1,"avoidance excluded from ratio");s.Record("dead",Resolution.Lethal);Check(s.Humane,"exact 50 percent boundary");s.Record("dead2",Resolution.Lethal);Check(!s.Humane,"below 50 percent");Check(!State().Humane,"zero denominator is not humane completion");
            s=State();s.Fragment("A");s.Fragment("A");s.Fragment("B");s.Fragment("C");Check(s.fragments.Count==3,"unique key pieces");s.bossWon=true;s.Record("one",Resolution.Subdued);Check(s.DetermineEnding()=="A","ending A");s.fragments.Remove("B");Check(s.DetermineEnding()=="C","ending C missing piece");s.offerAccepted=true;Check(s.DetermineEnding()=="B","offer takes priority B");s.offerAccepted=false;s.bossWon=false;s.bossLost=true;Check(s.DetermineEnding()=="D","final defeat D");
            s=State();for(int i=0;i<5;i++)s.Record("pre"+i,i<2?Resolution.Persuaded:Resolution.Lethal);s.Record("boss",Resolution.Subdued);Check(s.Humane&&s.NonLethal==3&&s.Total==6,"final boss included 2/5 to 3/6");
            s=State();s.leader=2;var b=new CombatSession(s,EnemyDefinition.For("guard",s));Check(b.AttackDamage==26,"Daniel attack +30%");b.Act(BattleAction.Subdue,true,.8f);Check(b.subdue==30&&b.hp==100,"subdue never damages HP");b.Act(BattleAction.Subdue,true,.1f);Check(b.subdue==75,"30 percent additional effect");
            s=State();Inventory.Add(s,"trap",2);b=new CombatSession(s,EnemyDefinition.For("guard",s));b.Act(BattleAction.Trap,true,.9f);Check(b.subdue==30&&b.toolsUsed==1&&s.hp==100&&!b.CanSwitch,"Daeun first tool +50%, second tool pending");b.Act(BattleAction.Trap,true,.9f);Check(b.subdue==60&&s.hp==88&&b.round==2,"enemy acts after second tool");
            s=State();Inventory.Add(s,"trap",2);b=new CombatSession(s,EnemyDefinition.For("guard",s)){subdue=80};b.Act(BattleAction.Trap,true,.9f);Check(b.finished&&b.won&&s.hp==100&&Inventory.Count(s,"trap")==1,"victory after first tool stops second and enemy");
            s=State();s.leader=1;b=new CombatSession(s,EnemyDefinition.For("guard",s));b.Act(BattleAction.Analyze,true,.9f);Check(b.persuade==10&&b.AttackDamage==25,"James next turn attack bonus and first insight");b.Act(BattleAction.Analyze,true,.9f);Check(b.persuade==10,"repeat analysis cannot farm same phase");b.Act(BattleAction.Persuade,true,.4f);Check(b.persuade==45,"James persuasion extra chance 55 percent");
            s=State();b=new CombatSession(s,EnemyDefinition.For("machine",s));Check(b.Validate(BattleAction.Persuade)!="","machine cannot be persuaded");
            s=State();s.leader=2;b=new CombatSession(s,EnemyDefinition.For("boss",s));for(int i=0;i<3;i++)b.Act(BattleAction.Subdue,true,.9f);Check(b.phase==3&&b.subdue==90&&!b.finished,"boss continuous gauges and three phases");b.Act(BattleAction.Subdue,true,.9f);Check(b.finished&&b.won&&b.result==Resolution.Subdued,"boss nonlethal completion");
            s=State();s.poison=true;Inventory.Add(s,"antidote",1);b=new CombatSession(s,EnemyDefinition.For("guard",s));b.Act(BattleAction.Antidote,true,.9f);Check(!s.poison&&s.hp==100,"Daeun antidote before second action");
            s=State();s.Fragment("A");s.Record("c",Resolution.Peaceful);string slot="verification-"+Guid.NewGuid().ToString("N");Check(SaveStore.Write(s,slot,out var err),"atomic save writes");var restored=SaveStore.Read(slot,out err);Check(restored!=null&&restored.fragments.Contains("A")&&restored.NonLethal==1,"save roundtrip");s.hp=70;Check(SaveStore.Write(s,slot,out err),"second save rotates backup");File.WriteAllText(SaveStore.SlotPath(slot),"corrupted test fixture");restored=SaveStore.Read(slot,out err);Check(restored!=null&&restored.hp==100&&err!="","corrupt primary recovers valid backup");
            // Only unique files generated by this test are cleaned up.
            foreach(string suffix in new[]{"",".bak",".tmp"}){string path=SaveStore.SlotPath(slot)+suffix;if(File.Exists(path))File.Delete(path);}
            s=State();s.tutorial=4;Check(!SaveStore.Write(s,slot,out err),"tutorial cannot save");s=State();s.x=float.NaN;Check(!s.Valid(),"invalid position rejected");
            string summary="PASS "+passed.Count+" rules checks\n"+string.Join("\n",passed);Directory.CreateDirectory("Tools");File.WriteAllText("Tools/rules-verification.txt",summary);return summary;
        }
    }
}

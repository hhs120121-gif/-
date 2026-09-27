using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SecretVirus
{
    public partial class VirusGame
    {
        bool diagnosticRunning;
        [Serializable]class SmokeReport{public string status,unityVersion;public int captured;public string[] errors;public bool fontLoaded,audioListener;}
        IEnumerator RuntimeSmoke()
        {
            diagnosticRunning=true;var errors=new List<string>();Application.LogCallback collect=(message,stack,type)=>{if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)errors.Add(message);};Application.logMessageReceived+=collect;
            string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Verification"));Directory.CreateDirectory(directory);
            string[] names={"title","workshop","crafting","battle","records","ending"};
            for(int i=0;i<names.Length;i++){
                State=new GameState{tutorial=5,joined=3,stage=8,highestStage=14,hp=100};foreach(string id in Catalog.Items.Keys)Inventory.Add(State,id,8);foreach(string id in Catalog.Clues.Keys)State.Clue(id);State.Fragment("A");State.Fragment("B");State.Fragment("C");State.Record("guard",Resolution.Persuaded);
                if(i==0){SetMode(GameMode.Title);World.Build(State);}
                if(i==1){State.stage=1;State.tutorial=3;State.joined=1;State.x=10;State.y=11;World.Build(State);SetMode(GameMode.Field);}
                if(i==2){State.stage=1;State.tutorial=4;World.Build(State);chosenRecipe=Catalog.Repair;craft=new CraftSession(chosenRecipe);SetMode(GameMode.Crafting);}
                if(i==3){State.stage=14;World.Build(State);StartBattle("boss");}
                if(i==4){World.Build(State);inventoryTab=1;SetMode(GameMode.Inventory);}
                if(i==5){World.Build(State);endingCode="A";SetMode(GameMode.Ending);}
                sceneFade=0;toastTime=0;yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();
                // Managed file output supports Unicode project paths consistently on Windows.
                var screenshot=ScreenCapture.CaptureScreenshotAsTexture();
                if(screenshot==null)errors.Add("No framebuffer for "+names[i]);else{File.WriteAllBytes(Path.Combine(directory,names[i]+".png"),screenshot.EncodeToPNG());Destroy(screenshot);}
                yield return new WaitForSecondsRealtime(.35f);
            }
            // Exercise presentation using the real action entrypoint without modifying player saves.
            State=new GameState{tutorial=5,joined=3,stage=14,highestStage=14,hp=100,leader=2};World.Build(State);StartBattle("boss");
            BattleAct(BattleAction.Attack);int afterFirst=Battle.hp;BattleAct(BattleAction.Attack);
            if(!BattlePresenting||Battle.hp!=afterFirst||Presented(previousPartyHp,State.hp,true)!=100)errors.Add("Battle presentation did not lock duplicate input or preserve pre-response HP.");
            yield return new WaitForSecondsRealtime(.25f);yield return new WaitForEndOfFrame();
            var actionFrame=ScreenCapture.CaptureScreenshotAsTexture();if(actionFrame!=null){File.WriteAllBytes(Path.Combine(directory,"battle-action.png"),actionFrame.EncodeToPNG());Destroy(actionFrame);}else errors.Add("No action framebuffer");
            yield return new WaitForSecondsRealtime(.48f);yield return new WaitForEndOfFrame();
            var responseFrame=ScreenCapture.CaptureScreenshotAsTexture();if(responseFrame!=null){File.WriteAllBytes(Path.Combine(directory,"battle-response.png"),responseFrame.EncodeToPNG());Destroy(responseFrame);}else errors.Add("No response framebuffer");
            yield return new WaitForSecondsRealtime(.55f);
            if(BattlePresenting||Presented(previousPartyHp,State.hp,true)!=State.hp)errors.Add("Battle presentation failed to return control.");
            BattleAct(BattleAction.Attack);if(Battle.hp>=afterFirst)errors.Add("Next action did not resume after presentation.");
            int captured=0;foreach(string name in names)if(File.Exists(Path.Combine(directory,name+".png")))captured++;
            bool loaded=Resources.Load<Font>("NotoSansKR-Regular")!=null,listener=FindAnyObjectByType<AudioListener>()!=null;
            bool world3D=!World.sceneCamera.orthographic&&World.root.GetComponentsInChildren<MeshRenderer>().Length>100&&World.root.GetComponentsInChildren<SpriteRenderer>().Length==0;
            var worldMaterial=Resources.Load<Material>("World3DMaterial");if(worldMaterial==null||!worldMaterial.shader.isSupported)errors.Add("3D material shader missing or unsupported.");
            var characterShader=Resources.Load<Shader>("IllustratedCharacter");if(characterShader==null||!characterShader.isSupported||Resources.Load<Texture2D>("CharacterFaceAtlas")==null)errors.Add("Illustrated character shader or face atlas missing/unsupported.");
            foreach(string character in new[]{"Daeun","James","Daniel"}){var model=Resources.Load<GameObject>("Characters/"+character);if(model==null||model.GetComponentsInChildren<SkinnedMeshRenderer>().Length<8)errors.Add("Authored character missing: "+character);}
            if(Resources.Load<Texture2D>("CharacterMaterialAtlas")==null)errors.Add("Character material atlas missing.");
            foreach(string shaderName in new[]{"EnvironmentSurface","EnvironmentGlass"}){var shader=Resources.Load<Shader>(shaderName);if(shader==null||!shader.isSupported)errors.Add("Environment shader missing/unsupported: "+shaderName);}
            foreach(string prop in new[]{"desk","bench","bed","shelf","crate","terminal","machine","door","lamp","vent","notice","vial","plant","rubble","BeveledUnit"})if(Resources.Load<GameObject>("Environment/"+prop)==null)errors.Add("Environment model missing: "+prop);
            var report=new SmokeReport{status=errors.Count==0&&captured==6&&loaded&&listener&&world3D?"PASS":"FAIL",unityVersion=Application.unityVersion,captured=captured,errors=errors.ToArray(),fontLoaded=loaded,audioListener=listener};
            File.WriteAllText(Path.Combine(directory,"runtime-report.json"),JsonUtility.ToJson(report,true));Application.logMessageReceived-=collect;Debug.Log("RUNTIME SMOKE "+report.status);Application.Quit(report.status=="PASS"?0:1);
        }
    }
}

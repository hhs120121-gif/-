using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretVirus.Editor
{
    [InitializeOnLoad]
    public static class ProjectTools
    {
        [Serializable] class Command { public string id,action,argument; }
        [Serializable] class Result { public string id,status,message; public bool playing; }
        static double next;static bool boot;static string pendingId;static string root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        static ProjectTools(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.timeSinceStartup<next||EditorApplication.isCompiling||EditorApplication.isUpdating)return;next=EditorApplication.timeSinceStartup+.5;
            if(!boot){boot=true;if(!File.Exists("Assets/Game/Scenes/Campaign.unity"))Setup();}
            string commandPath=Path.Combine(root,"Tools","command.json");if(!File.Exists(commandPath))return;
            Command command;
            try{command=JsonUtility.FromJson<Command>(File.ReadAllText(commandPath));if(command==null||string.IsNullOrEmpty(command.id))return;}catch{return;}
            string ack=Path.Combine(root,"Tools","last-command.txt");if(File.Exists(ack)&&File.ReadAllText(ack)==command.id)return;
            File.WriteAllText(ack,command.id);pendingId=command.id;
            try{
                switch(command.action){
                    case "setup":Setup();Reply("ok","Scene and build settings configured.");break;
                    case "tests":Reply("ok",RulesChecks.Run());break;
                    case "3d":if(VirusGame.Instance==null)throw new Exception("Enter Play Mode first.");Reply("ok",VirusGame.Instance.Verify3D());break;
                    case "play":EditorApplication.isPlaying=true;Reply("ok","Play requested.");break;
                    case "stop":EditorApplication.isPlaying=false;Reply("ok","Stop requested.");break;
                    case "state":Reply("ok",VirusGame.Instance==null?"Editor ready":VirusGame.Instance.VerificationState());break;
                    case "view":var view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));view.maximized=true;view.Focus();Reply("ok","Game view focused.");break;
                    case "scenario":if(VirusGame.Instance==null)throw new Exception("Enter Play Mode first.");VirusGame.Instance.VerificationScenario(command.argument);Reply("ok","Scenario: "+command.argument);break;
                    case "journey":if(VirusGame.Instance==null)throw new Exception("Enter Play Mode first.");Reply("ok",VirusGame.Instance.VerifyJourney());break;
                    case "traversal":if(VirusGame.Instance==null)throw new Exception("Enter Play Mode first.");Reply("ok",VirusGame.Instance.VerifyTraversal());break;
                    case "branches":if(VirusGame.Instance==null)throw new Exception("Enter Play Mode first.");Reply("ok",VirusGame.Instance.VerifyBranches());break;
                    case "capture":if(VirusGame.Instance==null)throw new Exception("Enter Play Mode first.");ScreenCapture.CaptureScreenshot(Path.Combine(root,"Tools","verification-"+command.argument+".png"));Reply("ok","Screenshot scheduled.");break;
                    case "build":if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode before building.");Build();Reply("ok","Windows build completed.");break;
                    default:throw new Exception("Unknown local command.");
                }
            }catch(Exception e){Debug.LogException(e);Reply("error",e.ToString());}
        }
        static void Reply(string status,string message){var value=new Result{id=pendingId,status=status,message=message,playing=EditorApplication.isPlaying};string path=Path.Combine(root,"Tools","result.json");Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(value,true));}
        [MenuItem("Secret of Virus/Configure Campaign")]
        public static void Setup()
        {
            if(EditorApplication.isPlaying)return;
            if(!File.Exists("Assets/Game/Resources/World3DMaterial.mat")){var material=new Material(Shader.Find("Standard"));material.EnableKeyword("_EMISSION");AssetDatabase.CreateAsset(material,"Assets/Game/Resources/World3DMaterial.mat");}
            string path="Assets/Game/Scenes/Campaign.unity";
            if(!File.Exists(path)){
                Scene current=SceneManager.GetActiveScene();if(current.IsValid()&&current.isDirty&&current.rootCount>0)EditorSceneManager.SaveScene(current,string.IsNullOrEmpty(current.path)?"Assets/Game/Scenes/BeforeCampaign.unity":current.path);
                Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);new GameObject("Campaign").AddComponent<VirusGame>();EditorSceneManager.SaveScene(scene,path);
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(path,true)};
            PlayerSettings.companyName="Secret Virus Studio";PlayerSettings.productName="바이러스의 비밀";PlayerSettings.defaultScreenWidth=1920;PlayerSettings.defaultScreenHeight=1080;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;PlayerSettings.resizableWindow=true;PlayerSettings.colorSpace=ColorSpace.Gamma;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            AssetDatabase.SaveAssets();Debug.Log("Secret of Virus: campaign configured. Press Play.");
        }
        [MenuItem("Secret of Virus/Run Rules Verification")]
        public static void Check(){Debug.Log(RulesChecks.Run());}
        [MenuItem("Secret of Virus/Build Windows Game")]
        public static void Build()
        {
            Setup();Directory.CreateDirectory("Builds/Windows");var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Game/Scenes/Campaign.unity"},locationPathName="Builds/Windows/SecretOfVirus.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result+" errors "+report.summary.totalErrors);
            Directory.CreateDirectory("Builds/Windows/ThirdPartyNotices");File.Copy("Assets/Game/Resources/NotoSansKR-OFL.txt","Builds/Windows/ThirdPartyNotices/NotoSansKR-OFL.txt",true);
            File.WriteAllText("Builds/Windows/PLAY.txt","The Secret of Virus\nRun SecretOfVirus.exe with its SecretOfVirus_Data folder next to it.\nArrow keys: move / navigate\nZ or Enter: confirm / interact\nX or Shift: cancel / run\nC or 1,2,3: switch hero\nI or Tab: inventory and journal\nEscape: pause\nKorean font: Noto Sans CJK KR, SIL OFL; see ThirdPartyNotices.\n");
            File.WriteAllText("Tools/build-report.json",JsonUtility.ToJson(new BuildNote{status="Succeeded",errors=(int)report.summary.totalErrors,warnings=(int)report.summary.totalWarnings,bytes=(long)report.summary.totalSize},true));
        }
        [Serializable]class BuildNote{public string status;public int errors,warnings;public long bytes;}
    }
}

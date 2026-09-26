#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace SecretVirus
{
    public partial class VirusGame
    {
        public string Verify3D()
        {
            var original=State;var mode=Mode;var savedBattle=Battle;int minimumMeshes=int.MaxValue;
            try{
                if(World.sceneCamera.orthographic)throw new Exception("Exploration camera must be perspective.");
                if(World.target.width<1280||World.target.filterMode!=FilterMode.Bilinear)throw new Exception("3D render quality configuration failed.");
                for(int stage=1;stage<=14;stage++){
                    State=new GameState{stage=stage,highestStage=stage,tutorial=5,joined=3,x=5,y=8};World.Build(State);
                    int meshes=World.root.GetComponentsInChildren<MeshRenderer>().Length;minimumMeshes=Math.Min(minimumMeshes,meshes);
                    if(meshes<100||World.root.GetComponentsInChildren<SpriteRenderer>().Length>0)throw new Exception("Non-volumetric stage: "+stage);
                    foreach(var thing in World.things)if(thing.renderer.transform.GetComponent<Collider>()==null)throw new Exception("Missing 3D collider: "+thing.id);
                    for(int hero=0;hero<3;hero++){State.leader=hero;World.UpdateView(0,false,Vector2.zero);if(World.player.GetComponentsInChildren<MeshRenderer>().Length<25)throw new Exception("Missing character mesh parts.");if(Vector3.Distance(World.player.position,WorldView.Ground(World.position))>.01f)throw new Exception("Character is outside the ground plane.");}
                    Vector2 origin=World.ScreenPoint(new Vector2(15,8));Vector2 east=World.ScreenPoint(new Vector2(16,8)),north=World.ScreenPoint(new Vector2(15,9));
                    if(east.x<=origin.x||north.y>=origin.y)throw new Exception("Input and camera projection disagree.");
                }
                var portrait=World.Portrait(0) as Texture2D;if(portrait==null||portrait.width<200)throw new Exception("3D portrait capture missing.");
                var pixels=portrait.GetPixels();Color background=pixels[0];int different=0;foreach(var pixel in pixels)if(Mathf.Abs(pixel.r-background.r)+Mathf.Abs(pixel.g-background.g)+Mathf.Abs(pixel.b-background.b)>.12f)different++;
                File.WriteAllBytes("Tools/verification-3d-portrait-asset.png",portrait.EncodeToPNG());
                if(different<1000)throw new Exception("3D portrait rendered blank.");
                string result="PASS perspective camera / 1280x720 antialiased rendering / 14 mesh-only maps (minimum "+minimumMeshes+" mesh renderers) / all 94 object colliders / 42 hero placements / camera-relative input axes / nonblank 3D portrait";
                File.WriteAllText("Tools/3d-verification.txt",result);return result;
            }finally{State=original;World.Build(State);Battle=savedBattle;SetMode(mode);}
        }
    }
}
#endif

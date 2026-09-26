using System.Collections.Generic;
using UnityEngine;

namespace SecretVirus
{
    // Native pixel artwork: no resampling or modification of the reference sheets.
    public static class PixelArt
    {
        static readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        static readonly Dictionary<string,Texture2D> portraits=new Dictionary<string,Texture2D>();
        public static Color C(string hex){ColorUtility.TryParseHtmlString(hex,out var c);return c;}
        class Canvas
        {
            public Texture2D texture; int w,h;
            public Canvas(int width,int height){w=width;h=height;texture=new Texture2D(w,h,TextureFormat.RGBA32,false);texture.filterMode=FilterMode.Point;texture.wrapMode=TextureWrapMode.Clamp;texture.SetPixels(new Color[w*h]);}
            public void R(int x,int y,int width,int height,Color c){for(int yy=Mathf.Max(0,y);yy<Mathf.Min(h,y+height);yy++)for(int xx=Mathf.Max(0,x);xx<Mathf.Min(w,x+width);xx++)texture.SetPixel(xx,h-yy-1,c);}
            public void R(int x,int y,int width,int height,string c){R(x,y,width,height,C(c));}
            public void Dot(int x,int y,Color c){R(x,y,1,1,c);}
            public void Ellipse(int x,int y,int width,int height,Color c){for(int yy=0;yy<height;yy++)for(int xx=0;xx<width;xx++){float a=(xx-width*.5f)/(width*.5f),b=(yy-height*.5f)/(height*.5f);if(a*a+b*b<1)Dot(x+xx,y+yy,c);}}
            public Texture2D Done(){texture.Apply();return texture;}
        }
        static Sprite Sprite(string key,Canvas p,Vector2 pivot,float ppu=32){var s=UnityEngine.Sprite.Create(p.Done(),new Rect(0,0,p.texture.width,p.texture.height),pivot,ppu);sprites[key]=s;return s;}
        public static Sprite Tile(Color baseColor,int seed,bool wall=false)
        {
            string key="tile"+baseColor+seed+wall;if(sprites.TryGetValue(key,out var s))return s;
            var p=new Canvas(32,32);p.R(0,0,32,32,baseColor);var random=new System.Random(seed);
            if(wall){p.R(0,0,32,3,baseColor*1.4f);p.R(0,26,32,6,baseColor*.55f);p.R(0,16,32,1,baseColor*.7f);p.R(seed%2==0?7:23,0,1,16,baseColor*.75f);p.R(seed%2==0?23:7,17,1,9,baseColor*.75f);}
            else {p.R(0,31,32,1,baseColor*.90f);p.R(31,0,1,32,baseColor*.90f);for(int i=0;i<28;i++)p.R(random.Next(1,30),random.Next(1,30),random.Next(1,4),1,baseColor*(.85f+(float)random.NextDouble()*.3f));}
            return Sprite(key,p,new Vector2(.5f,.5f));
        }
        public static Sprite Actor(int hero,int facing,int frame,bool asleep=false)
        {
            string key="actor"+hero+"/"+facing+"/"+frame+asleep;if(sprites.TryGetValue(key,out var cached))return cached;
            var p=new Canvas(32,48);int bob=(frame%2),stride=frame==1?2:frame==3?-2:0;
            Color outline=C("#17232C"),skin=C(hero==0?"#B89779":hero==1?"#E6C3AD":hero==3?"#C9B09A":"#C89176"),hair=C(hero==0?"#403B35":hero==1?"#E9C95E":hero==2?"#BB603F":hero==3?"#AAB7B6":hero==5?"#443E49":"#374959"),shirt=C(hero==0?"#5C7A91":hero==1?"#465666":hero==2?"#566263":hero==3?"#354A59":hero==5?"#B7CDC6":"#486B7D");
            int height=hero==2?0:2;
            p.Ellipse(5,42,23,5,new Color(0,0,0,.28f));
            p.R(9,33+bob,6,10+stride,outline);p.R(18,33+bob,6,10-stride,outline);
            p.R(9,33+bob,5,7+stride,shirt*.7f);p.R(18,33+bob,5,7-stride,shirt*.7f);
            p.R(7,41+stride,8,3,outline);p.R(18,41-stride,8,3,outline);
            p.R(6,22+bob,22,15,outline);p.R(7,23+bob,20,12,shirt);p.R(7,23+bob,3,12,shirt*1.2f);
            p.R(5,25+bob-stride,4,10,outline);p.R(24,25+bob+stride,4,10,outline);p.R(5,32+bob-stride,4,4,skin);p.R(24,32+bob+stride,4,4,skin);
            if(hero==0){p.R(7,23+bob,5,7,"#D0CEB8");p.R(22,23+bob,5,7,"#D0CEB8");p.R(12,23+bob,2,12,shirt*1.15f);p.R(21,23+bob,2,12,shirt*1.15f);p.R(18,32+bob,6,4,"#98A6AC");p.R(18,34+bob,6,1,"#384C59");}
            if(hero==1){p.R(14,23+bob,7,13,"#D4D1BE");p.R(6,34+bob,7,6,shirt);p.R(22,34+bob,6,6,shirt);}
            if(hero==2){p.R(13,23+bob,8,12,"#442F32");p.R(7,23+bob,6,3,"#754C3E");p.R(21,23+bob,6,3,"#754C3E");}
            if(hero==3){p.R(13,23+bob,8,12,"#D4D2BE");p.R(16,25+bob,2,10,"#87584D");}
            if(hero==4){p.R(10,25+bob,15,8,"#344C5A");p.R(18,27+bob,4,3,"#D6B873");}
            if(hero==5){p.R(14,24+bob,5,11,"#557773");p.R(7,34+bob,8,5,shirt);p.R(20,34+bob,8,5,shirt);}
            int head=5+bob+height;p.R(8,head,18,18,outline);p.R(9,head+2,16,15,skin);p.R(9,head+12,3,4,skin*.85f);p.R(8,head,18,7,hair);p.R(6,head+3,4,9,hair);p.R(24,head+3,4,10,hair);p.R(10,head-1,13,2,hair);
            if(hero==0){p.R(19,head-5,6,5,outline);p.R(20,head-4,5,5,hair);p.R(9,head+5,5,3,hair);}
            else if(hero==1){p.R(8,head+2,10,6,hair*1.15f);p.R(23,head+4,4,7,hair);}
            else {p.R(7,head+1,7,7,hair*1.12f);p.R(17,head+5,8,3,hair);}
            if(hero==4){p.R(7,head,20,6,"#3B6073");p.R(7,head+6,20,2,"#21343F");p.R(15,head+2,4,3,"#C8B375");}
            if(facing==1){p.R(9,head+4,16,11,hair);p.R(12,head+13,11,3,hair*.82f);}
            else {
                int eyeX=facing==2?10:facing==3?19:11;
                p.R(eyeX,head+10,3,2,outline);if(facing==0)p.R(20,head+10,3,2,outline);
                if(hero==1){p.R(eyeX-1,head+8,6,1,outline);p.R(eyeX-1,head+12,6,1,outline);if(facing==0){p.R(19,head+8,6,1,outline);p.R(19,head+12,6,1,outline);p.R(16,head+10,3,1,outline);}}
                p.R(facing==2?11:facing==3?22:16,head+15,3,1,"#785449");
                if(hero==2){p.R(11,head+13,1,1,"#9C5945");p.R(22,head+13,1,1,"#9C5945");}
            }
            if(asleep){var tex=p.Done();var q=new Canvas(48,32);for(int y=0;y<48;y++)for(int x=0;x<32;x++)q.texture.SetPixel(y,x,tex.GetPixel(x,y));return Sprite(key,q,new Vector2(.5f,.15f));}
            return Sprite(key,p,new Vector2(.5f,.08f));
        }
        public static Texture2D Portrait(int hero,int mood=0)
        {
            string key=hero+"/"+mood;if(portraits.TryGetValue(key,out var t))return t;
            var p=new Canvas(80,96);Color skin=C(hero==0?"#BB987D":hero==1?"#E9CCB7":hero==3?"#C9B09A":"#CD977B"),hair=C(hero==0?"#443D36":hero==1?"#EACF6A":hero==2?"#C26A49":hero==3?"#AAB7B6":"#443E49"),coat=C(hero==0?"#607D93":hero==1?"#3E4D60":hero==2?"#596767":hero==3?"#354A59":"#B7CDC6");
            p.R(0,0,80,96,"#1F3038");p.R(4,4,72,88,"#2D4148");p.R(8,82,64,10,"#23353D");
            p.Ellipse(13,5,56,76,hair*.52f);p.R(29,62,24,15,skin*.82f);p.R(15,74,53,22,coat);p.R(10,81,10,15,coat);p.R(65,82,8,14,coat*.8f);
            p.R(22,20,39,43,skin);p.R(17,31,47,25,skin);p.R(26,60,29,9,skin);p.R(18,49,5,9,skin*.85f);p.R(53,54,6,8,skin*.9f);
            p.R(18,14,44,13,hair);p.R(13,21,10,26,hair);p.R(58,21,10,32,hair);p.R(22,9,33,12,hair*1.06f);p.R(23,22,20,8,hair);p.R(24,27,11,7,hair);p.R(51,24,9,12,hair);
            if(hero==0){p.R(47,3,14,12,hair);p.R(53,1,10,5,hair);p.R(15,76,13,16,"#D7D3C5");p.R(54,76,12,16,"#D7D3C5");p.R(26,74,5,22,coat);p.R(50,74,5,22,coat);p.R(38,84,14,10,coat*1.22f);}
            if(hero==1){p.R(16,18,23,10,"#F1DF92");p.R(32,75,18,21,"#DBD9C9");p.R(24,39,15,2,"#1C2934");p.R(24,49,15,2,"#1C2934");p.R(24,39,2,12,"#1C2934");p.R(37,39,2,12,"#1C2934");p.R(45,39,15,2,"#1C2934");p.R(45,49,15,2,"#1C2934");p.R(45,39,2,12,"#1C2934");p.R(58,39,2,12,"#1C2934");p.R(39,43,6,2,"#1C2934");}
            if(hero==2){p.R(15,75,17,6,"#7F5240");p.R(51,75,17,6,"#7F5240");p.R(34,75,16,21,"#442F32");for(int i=0;i<5;i++){p.R(24+i*2,53+i%2,1,1,"#A35C49");p.R(48+i*2,54-i%2,1,1,"#A35C49");}}
            if(hero==3){p.R(33,74,18,22,"#D4D2BE");p.R(40,76,5,20,"#87584D");p.R(19,32,40,2,hair*.8f);}
            if(hero==5){p.R(34,75,15,21,"#557773");p.R(51,82,9,7,"#E0E7D9");}
            p.R(27,43,9,4,"#F0E8D6");p.R(48,43,9,4,"#F0E8D6");Color eyes=C(hero==0?"#5B3D29":hero==1?"#4E93AE":"#87A456");p.R(30,42,4,6,eyes);p.R(50,42,4,6,eyes);p.R(31,43,2,4,"#1D2933");p.R(51,43,2,4,"#1D2933");
            int brow=mood==1?38:mood==2?40:37;p.R(26,brow,11,2,hair*.7f);p.R(47,brow,11,2,hair*.7f);p.R(40,49,2,5,skin*.8f);
            p.R(35,60,14,mood==2?3:1,"#795447");if(mood==2)p.R(37,60,10,2,"#E1D6BA");if(mood==3){p.R(56,52,4,2,"#B46156");p.R(20,57,4,2,"#B46156");}
            portraits[key]=p.Done();return portraits[key];
        }
        public static Sprite Prop(string type,int variant=0)
        {
            string key="prop"+type+variant;if(sprites.TryGetValue(key,out var cached))return cached;
            int w=type=="bed"?64:type=="shelf"?64:type=="machine"?64:48,h=type=="shelf"?64:type=="machine"?80:48;
            var p=new Canvas(w,h);Color dark=C("#1B2A31"),metal=C("#657476"),light=C("#A5B2A7"),amber=C("#D0AC67"),mint=C("#71C6AE");
            p.R(3,h-7,w-3,7,new Color(0,0,0,.3f));
            switch(type){
                case "bed": p.R(2,13,59,29,dark);p.R(4,15,55,24,"#85765C");p.R(6,16,14,21,"#CCC2A0");p.R(22,17,34,20,"#57716C");p.R(25,19,28,2,"#6C8881");p.R(50,26,7,8,"#425B59");p.R(3,9,4,36,metal);p.R(59,12,3,33,metal);break;
                case "shelf":p.R(3,3,58,55,dark);p.R(5,5,3,54,metal);p.R(56,5,3,54,metal);for(int row=0;row<3;row++){int y=8+row*16;p.R(8,y+12,48,3,metal);for(int i=0;i<4;i++){p.R(10+i*11,y+2,8,10,i%2==0?C("#9F8B69"):C("#6C8580"));p.R(11+i*11,y+4,5,1,light);}}break;
                case "desk":case "bench":p.R(3,17,42,20,dark);p.R(5,15,40,17,"#8A7556");p.R(5,15,40,3,"#B09A70");p.R(7,33,5,11,dark);p.R(36,33,5,11,dark);p.R(8,19,14,10,type=="desk"?C("#D4CAAA"):metal);p.R(10,21,10,1,"#7B7767");p.R(10,24,7,1,"#7B7767");p.R(28,20,12,4,metal);p.R(34,22,4,7,metal);break;
                case "terminal":p.R(10,5,29,23,dark);p.R(12,7,25,18,"#426968");for(int i=0;i<4;i++)p.R(15,10+i*3,17-i*2,1,mint);p.R(20,28,8,4,metal);p.R(5,32,39,8,metal);p.R(9,34,23,3,dark);p.R(36,34,4,3,amber);break;
                case "door":p.R(4,0,40,47,dark);p.R(7,2,34,42,metal);p.R(9,4,14,36,"#445B60");p.R(25,4,14,36,"#52676A");p.R(22,8,2,31,dark);p.R(31,22,4,5,amber);p.R(7,43,34,3,light);break;
                case "rubble":for(int i=0;i<6;i++){int x=(i*13)%31,y=13+(i*7)%19;p.R(x,y,14,14,dark);p.R(x+1,y,12,10,i%2==0?C("#7B7A6B"):C("#646F6C"));p.R(x+2,y+1,9,2,light);}p.R(4,38,39,3,"#8B644F");break;
                case "vial":p.R(15,7,19,5,dark);p.R(16,12,17,26,"#95B8AF");p.R(18,20,13,16,variant==5?mint:variant==0?C("#C78388"):amber);p.R(18,13,3,20,"#BCD8CC");p.R(19,26,11,6,"#D4D2AC");break;
                case "crate":p.R(6,15,36,27,dark);p.R(8,16,32,22,"#897854");p.R(10,19,28,3,"#B2A078");p.R(12,16,3,22,"#594F3F");p.R(33,16,3,22,"#594F3F");p.R(21,25,6,5,metal);break;
                case "machine":p.R(6,4,52,68,dark);p.R(8,6,48,63,"#536F70");p.R(12,11,24,42,"#294B53");p.R(16,16,16,30,"#67B7A9");p.R(18,19,3,24,"#B1E3CC");p.R(40,13,10,8,amber);p.R(40,27,10,2,mint);p.R(40,32,10,2,mint);p.R(40,37,10,2,mint);p.R(13,58,37,6,metal);p.R(0,61,8,5,metal);p.R(57,16,7,5,metal);break;
                case "plant":p.R(17,30,19,12,"#8C674F");p.R(23,10,5,25,"#66765A");p.R(12,14,13,7,"#638565");p.R(27,7,12,8,"#7C9467");p.R(28,23,11,6,"#52765D");break;
                case "lamp":p.R(20,18,4,25,metal);p.R(10,39,26,4,dark);p.R(12,7,21,13,amber);p.R(15,8,15,8,"#EDDA9B");break;
                case "vent":p.R(4,16,40,25,dark);p.R(6,18,36,20,metal);for(int i=0;i<5;i++)p.R(9,21+i*3,30,1,dark);break;
                case "notice":p.R(10,9,28,32,"#7D7055");p.R(12,11,24,26,"#CEC1A0");for(int i=0;i<6;i++)p.R(15,15+i*3,17-i%3*3,1,"#7D7B65");break;
                default:p.R(10,20,28,20,metal);p.R(12,22,24,3,light);p.R(16,28,14,7,amber);break;
            }
            return Sprite(key,p,new Vector2(.5f,.1f));
        }
        public static Texture2D Icon(string id)
        {
            string key="icon"+id;if(portraits.TryGetValue(key,out var result))return result;var p=new Canvas(24,24);var color=Catalog.Items.ContainsKey(id)?Catalog.Items[id].color:C("#D7B269");
            p.R(4,6,17,15,"#14252C");p.R(5,6,15,13,color*.75f);p.R(6,7,13,3,color*1.2f);
            if(id=="wire"){p.R(7,6,3,12,color);p.R(7,15,11,3,color);p.R(15,6,3,12,color);p.R(10,9,5,6,"#203038");}
            else if(id=="med"||id=="antidote"){p.R(10,9,4,9,"#EAE2CB");p.R(7,12,10,3,"#EAE2CB");}
            else if(id=="chip"||id=="board"){p.R(9,10,7,6,"#263F44");for(int i=0;i<4;i++){p.R(4+i*4,4,1,3,"#D5C486");p.R(4+i*4,19,1,3,"#D5C486");}}
            else if(id=="trap"){p.R(7,8,3,9,"#D1D9CA");p.R(16,8,3,9,"#D1D9CA");p.R(8,16,9,2,"#D1D9CA");}
            else {p.R(9,12,8,3,color*1.35f);p.R(12,9,3,9,color*1.35f);}
            portraits[key]=p.Done();return portraits[key];
        }
        public static Sprite Detail(string type,int seed=0)
        {
            string key="detail"+type+seed;if(sprites.TryGetValue(key,out var found))return found;
            int width=type=="rug"?128:type=="pipe"?96:64,height=type=="rug"?96:type=="pipe"?32:64;var p=new Canvas(width,height);
            if(type=="rug"){p.R(0,0,128,96,"#383E36");p.R(2,2,124,92,"#6C6250");p.R(5,5,118,86,"#424C43");p.R(9,9,110,78,"#716753");for(int x=12;x<120;x+=6)p.R(x,12,2,72,"#655D4D");p.R(12,12,103,2,"#A28F68");p.R(12,81,103,2,"#A28F68");}
            else if(type=="window"){p.R(4,3,56,54,"#182E37");p.R(7,6,50,45,"#405966");p.R(10,9,45,38,"#28414D");for(int i=0;i<6;i++){p.R(11+i*7,25-(i*5)%13,6,22+(i*5)%13,"#1D333F");p.R(12+i*7,28,2,3,"#8F825E");}p.R(28,7,4,42,"#788782");p.R(7,29,49,3,"#788782");p.R(1,53,62,7,"#89938B");}
            else if(type=="pipe"){p.R(0,12,96,10,"#253C42");p.R(0,13,96,6,"#617773");p.R(0,13,96,2,"#87938A");for(int x=6;x<96;x+=24){p.R(x,9,5,16,"#364C51");p.R(x+1,10,2,14,"#8A9990");}p.R(41,7,15,3,"#9B6550");p.R(47,3,3,14,"#9B6550");}
            else if(type=="vent"){p.R(5,7,55,42,"#1D3038");p.R(7,9,51,38,"#566C6E");for(int i=0;i<8;i++){p.R(11,13+i*4,43,2,"#243D43");}p.R(8,10,2,2,"#A1ABA0");p.R(54,42,2,2,"#A1ABA0");}
            else if(type=="hazard"){p.R(0,20,64,8,"#494B37");for(int i=0;i<8;i++)p.R(i*8,20,4,8,"#A79458");}
            else if(type=="stain"){var random=new System.Random(seed);for(int i=0;i<40;i++){int x=random.Next(0,58),y=random.Next(0,58);p.R(x,y,random.Next(1,7),random.Next(1,4),new Color(.06f,.10f,.10f,.18f));}p.R(13,43,27,1,new Color(.65f,.62f,.5f,.3f));p.R(27,41,18,1,new Color(.65f,.62f,.5f,.25f));}
            else if(type=="lamp"){p.R(6,10,53,10,"#26373C");p.R(8,12,49,6,"#B6B89B");p.R(12,13,40,3,"#EADBA8");p.R(10,20,46,2,"#6C735F");}
            return Sprite(key,p,new Vector2(.5f,.5f));
        }
        public static Sprite AbilityTool(int hero)
        {
            string key="ability"+hero;if(sprites.TryGetValue(key,out var found))return found;var p=new Canvas(32,32);
            if(hero==0){p.R(13,8,5,21,"#A3B7AF");p.R(7,3,7,10,"#BCD0C2");p.R(18,3,7,10,"#BCD0C2");p.R(10,11,13,5,"#8CA5A3");p.R(14,19,2,7,"#D6DFCA");}
            else if(hero==1){p.R(11,3,11,4,"#AEC3B7");p.R(13,7,7,9,"#B9D6C8");p.R(8,17,17,11,"#8AC9B4");p.R(10,19,13,7,"#67A69F");p.R(11,18,2,8,"#D4E8CF");}
            else{p.R(5,12,23,12,"#384D55");p.R(8,14,5,8,"#E0BE81");p.R(15,14,5,8,"#E0BE81");p.R(22,14,3,8,"#E0BE81");p.R(3,8,3,5,"#D6B679");p.R(26,5,3,5,"#D6B679");}
            return Sprite(key,p,new Vector2(.5f,.5f));
        }
        public static Sprite Vision()
        {
            const string key="vision";if(sprites.TryGetValue(key,out var found))return found;var p=new Canvas(128,90);
            for(int x=0;x<128;x++)for(int y=0;y<90;y++)if(Mathf.Abs(y-45)<x*.35f)p.Dot(x,y,new Color(.85f,.73f,.39f,.05f+.045f*x/128f));return Sprite(key,p,new Vector2(0,.5f));
        }
    }
}

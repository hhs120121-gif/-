using UnityEngine;
using System.Collections.Generic;

namespace SecretVirus
{
    public class GameAudio:MonoBehaviour
    {
        AudioSource musicSource,effectsSource,fadingSource;float blend=1;readonly Dictionary<string,AudioClip> effects=new Dictionary<string,AudioClip>();AudioClip[] themes;Preferences preferences;float lastTone;
        public void Initialize(Preferences settings)
        {
            preferences=settings;musicSource=gameObject.AddComponent<AudioSource>();effectsSource=gameObject.AddComponent<AudioSource>();fadingSource=gameObject.AddComponent<AudioSource>();musicSource.loop=true;fadingSource.loop=true;
            themes=new[]{Theme(0),Theme(1),Theme(2),Theme(3)};Apply();SetTheme(0);
        }
        public void Apply(){if(musicSource==null)return;musicSource.volume=preferences.master*preferences.music*blend;fadingSource.volume=preferences.master*preferences.music*(1-blend);effectsSource.volume=preferences.master*preferences.effects;}
        public void SetTheme(int theme){if(musicSource.clip==themes[theme])return;var old=musicSource;musicSource=fadingSource;fadingSource=old;musicSource.clip=themes[theme];blend=0;Apply();musicSource.Play();}
        void Update(){if(blend>=1)return;blend=Mathf.Min(1,blend+Time.unscaledDeltaTime/.8f);Apply();if(blend>=1)fadingSource.Stop();}
        void OnDestroy(){foreach(var clip in effects.Values)Destroy(clip);if(themes!=null)foreach(var clip in themes)Destroy(clip);}
        public void Play(string id)
        {
            if(id=="step"&&Time.unscaledTime-lastTone<.15f)return;lastTone=Time.unscaledTime;if(effects.TryGetValue(id,out var cached)){effectsSource.PlayOneShot(cached);return;}
            float freq=id=="success"?660:id=="error"?120:id=="paper"?340:id=="step"?85:id=="hit"?65:id=="capture"?280:id=="persuade"?520:id=="switch"?430:id=="alarm"?780:360;
            int rate=22050;float duration=id=="success"?.45f:id=="step"?.065f:.15f;int n=(int)(rate*duration);float[] samples=new float[n];
            for(int i=0;i<n;i++){float t=(float)i/rate,env=Mathf.Pow(1-(float)i/n,2);float tone=Mathf.Sin(t*freq*6.283185f)*(id=="step"?.12f:.25f);if(id=="success")tone+=Mathf.Sin(t*freq*1.5f*6.283185f)*.15f;if(id=="hit"||id=="paper"||id=="step")tone+=(Mathf.PerlinNoise(i*.71f,2)*2-1)*.2f;samples[i]=tone*env;}
            var clip=AudioClip.Create("SFX "+id,n,1,rate,false);clip.SetData(samples,0);effects[id]=clip;effectsSource.PlayOneShot(clip);
        }
        AudioClip Theme(int theme)
        {
            const int rate=22050;float beat=theme==2?.38f:.7f;int length=(int)(beat*32*rate);float[] data=new float[length];int[] notes=theme==0?new[]{57,60,64,67,64,60,55,59}:theme==1?new[]{50,57,60,62,53,60,64,62}:theme==2?new[]{45,52,57,60,46,53,58,61}:new[]{60,64,67,72,59,62,67,71};
            for(int i=0;i<length;i++){float time=(float)i/rate;int step=(int)(time/beat);float local=time%beat;float frequency=440*Mathf.Pow(2,(notes[step%notes.Length]-69)/12f);float envelope=Mathf.Min(local/.04f,1)*Mathf.Exp(-local*3.7f);float melody=(Mathf.Sin(time*frequency*6.283185f)+.22f*Mathf.Sin(time*frequency*12.56637f))*envelope*.065f;float bass=55*Mathf.Pow(2,(notes[(step/4*4)%notes.Length]-57)/12f);float pad=Mathf.Sin(time*bass*6.283185f)*.03f;data[i]=melody+pad;}
            var clip=AudioClip.Create("Original theme "+theme,length,1,rate,false);clip.SetData(data,0);return clip;
        }
    }
}

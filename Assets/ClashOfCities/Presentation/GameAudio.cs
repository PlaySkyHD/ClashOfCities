using System.Collections.Generic;
using UnityEngine;
using ClashOfCities.Core;

namespace ClashOfCities.Presentation
{
    public sealed class GameAudio : MonoBehaviour
    {
        readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        readonly Dictionary<string,float> lastPlayed=new Dictionary<string,float>();
        AudioSource menu,battle;
        readonly AudioSource[] voices=new AudioSource[8],charge=new AudioSource[2];
        int nextVoice;
        bool unlocked,wasQuiet;
        float blend;
        public float MusicVolume {get;private set;}
        public float EffectsVolume {get;private set;}
        public int PlayedCues {get;private set;}
        public bool MusicPlaying {get{return menu.isPlaying||battle.isPlaying;}}
        public bool AssetsReady {get{return clips.Count==15;}}
        void Awake()
        {
            MusicVolume=Mathf.Clamp01(PlayerPrefs.GetFloat("audio.music",.35f));
            EffectsVolume=Mathf.Clamp01(PlayerPrefs.GetFloat("audio.effects",.65f));
            foreach(var name in new[]{"menu","battle","ui","attack","hit","projectile","melee","zone","buff","dodge","stun","ultimate","ko","victory","charge"})
            {
                var clip=Resources.Load<AudioClip>("Audio/"+name);
                if(clip==null){Debug.LogError("Missing audio: "+name);continue;}
                clips.Add(name,clip);
            }
            menu=Source(true);battle=Source(true);
            for(int i=0;i<voices.Length;i++)voices[i]=Source(false);
            for(int i=0;i<charge.Length;i++){charge[i]=Source(true);charge[i].clip=Clip("charge");}
            menu.clip=Clip("menu");battle.clip=Clip("battle");
            unlocked=Application.platform!=RuntimePlatform.WebGLPlayer;
        }
        AudioClip Clip(string name){AudioClip clip;return clips.TryGetValue(name,out clip)?clip:null;}
        AudioSource Source(bool loop)
        {
            var source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;
            source.loop=loop;source.spatialBlend=0;source.volume=0;return source;
        }
        public void Unlock(){unlocked=true;}
        public void SetVolumes(float music,float effects)
        {
            MusicVolume=Mathf.Clamp01(music);EffectsVolume=Mathf.Clamp01(effects);
            PlayerPrefs.SetFloat("audio.music",MusicVolume);PlayerPrefs.SetFloat("audio.effects",EffectsVolume);PlayerPrefs.Save();
            foreach(var voice in voices)voice.volume=EffectsVolume*.55f;
        }
        public void Tick(bool inBattle,bool quiet,BattleSimulation simulation)
        {
            if(!unlocked)return;
            if(quiet&&!wasQuiet)foreach(var voice in voices)voice.Stop();
            wasQuiet=quiet;
            if(!menu.isPlaying&&menu.clip!=null)menu.Play();
            if(!battle.isPlaying&&battle.clip!=null)battle.Play();
            blend=Mathf.MoveTowards(blend,inBattle?1:0,Time.unscaledDeltaTime*2);
            float volume=MusicVolume*.65f*(quiet?.22f:1);
            int active=0;foreach(var voice in voices)if(voice.isPlaying)active++;
            float effectGain=EffectsVolume*.55f/Mathf.Sqrt(Mathf.Max(1,active));
            foreach(var voice in voices)voice.volume=effectGain;
            menu.volume=volume*(1-blend);battle.volume=volume*blend;
            for(int i=0;i<charge.Length;i++)
            {
                bool charging=inBattle&&!quiet&&simulation!=null&&!simulation.IsFinished&&simulation.Fighters[i].IsCharging;
                if(charging)
                {
                    charge[i].volume=EffectsVolume*.35f;
                    charge[i].pitch=.8f+(float)simulation.Fighters[i].Charge*1.1f;
                    charge[i].panStereo=i==0?-.2f:.2f;
                    if(!charge[i].isPlaying)charge[i].Play();
                }
                else charge[i].Stop();
            }
        }
        public void Cue(string name,int side=-1,float gap=.06f)
        {
            if(!unlocked||EffectsVolume<=0)return;
            float last;if(lastPlayed.TryGetValue(name,out last)&&Time.unscaledTime-last<gap)return;
            var clip=Clip(name);if(clip==null)return;
            lastPlayed[name]=Time.unscaledTime;
            var source=voices[nextVoice++%voices.Length];source.Stop();source.clip=clip;
            int active=1;foreach(var voice in voices)if(voice.isPlaying)active++;
            source.volume=EffectsVolume*.55f/Mathf.Sqrt(active);source.panStereo=side<0?0:side==0?-.18f:.18f;
            source.pitch=1;source.Play();PlayedCues++;
        }
        public void BattleEvent(BattleEvent e)
        {
            switch(e.Type)
            {
                case "Attack":Cue(e.Delivery=="Melee"?"attack":"projectile",e.Source);break;
                case "Hit":if(e.Amount>=2&&e.Source>=0)Cue("hit",e.Target,.09f);break;
                case "Ability":
                    if(e.AbilityId!=null&&e.AbilityId.EndsWith("ultimate"))break;
                    Cue(e.Delivery=="Self"?"buff":e.Delivery=="Zone"?"zone":e.Delivery=="Melee"||e.Delivery=="Dash"?"melee":"projectile",e.Source);break;
                case "Dodge":Cue("dodge",e.Source);break;
                case "Stun":case "Root":Cue("stun",e.Target,.15f);break;
                case "Heal":case "Shield":Cue("buff",e.Target,.2f);break;
                case "ChargeRelease":Cue("ultimate",e.Source,.2f);break;
                case "Death":Cue("ko",e.Target,.5f);break;
            }
        }
    }
}

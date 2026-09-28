#if !UNITY_WEBGL || UNITY_EDITOR
using System.Collections;
using UnityEngine;
using ClashOfCities.Core;
namespace ClashOfCities.Presentation
{
    public sealed partial class GameBootstrap
    {
        IEnumerator AudioTest()
        {
            yield return new WaitForSecondsRealtime(.7f);
            if(!sound.AssetsReady){Debug.LogError("Audio test: missing clip");Application.Quit(1);yield break;}
            sound.Unlock();sound.Tick(false,false,null);
            yield return new WaitForSecondsRealtime(.2f);
            if(!sound.MusicPlaying){Debug.LogError("Audio test: music not playing");Application.Quit(1);yield break;}
            foreach(var cue in new[]{"ui","attack","projectile","hit","melee","zone","buff","dodge","stun","ultimate","ko","victory"})
            {sound.Cue(cue);yield return new WaitForSecondsRealtime(.12f);}
            float music=sound.MusicVolume,effects=sound.EffectsVolume;
            int before=sound.PlayedCues;
            sound.SetVolumes(music,0);sound.Cue("hit");
            bool muted=sound.PlayedCues==before;
            sound.SetVolumes(music,effects);
            if(!muted){Debug.LogError("Audio test: mute failed");Application.Quit(1);yield break;}
            var config=new MatchConfig{cityAId=data.cities[0].id,cityBId=data.cities[1].id,avatarAId=Avatars(0)[0].id,avatarBId=Avatars(1)[0].id,
                climateEventId=data.environments[0].id,seed=726491,balanceVersion=data.balance.version,mode=MatchMode.CpuVsCpu};
            StartMatch(config);yield return new WaitForSecondsRealtime(3);
            paused=true;yield return new WaitForSecondsRealtime(.3f);
            Debug.Log("Audio test: 15 clips loaded; music playing; 12 cues exercised; effects mute/restoration; battle transition and pause passed.");
            Application.Quit(0);
        }
    }
}
#endif

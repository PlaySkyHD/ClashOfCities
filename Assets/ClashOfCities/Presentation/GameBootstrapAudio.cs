using UnityEngine;
namespace ClashOfCities.Presentation
{
    public sealed partial class GameBootstrap
    {
        GameAudio sound;
        void UpdateAudio()
        {
            if(sound==null)return;
            if(Input.anyKeyDown||Input.GetMouseButtonDown(0))sound.Unlock();
            sound.Tick(screen==ScreenMode.Battle,(screen==ScreenMode.Battle&&(paused||showSkillHelp))||!Application.isFocused,simulation);
        }
        void DrawAudio(Rect rect)
        {
            if(sound==null)return;
            Panel(rect);
            for(int i=0;i<2;i++)
            {
                float value=i==0?sound.MusicVolume:sound.EffectsVolume;
                float y=rect.y+7+i*35;
                GUI.Label(new Rect(rect.x+12,y+3,rect.width-126,26),(i==0?"Musik":"Sounds")+"  "+Mathf.RoundToInt(value*100)+" %",small);
                float change=0;
                if(NavButton(new Rect(rect.xMax-101,y,40,29),"-",button))change=-.1f;
                if(NavButton(new Rect(rect.xMax-53,y,40,29),"+",button))change=.1f;
                if(change!=0){if(i==0)sound.SetVolumes(value+change,sound.EffectsVolume);else sound.SetVolumes(sound.MusicVolume,value+change);}
            }
        }
    }
}

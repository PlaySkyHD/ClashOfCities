using UnityEngine;
using ClashOfCities.Core;
namespace ClashOfCities.Presentation
{
    public sealed partial class GameBootstrap
    {
        void DrawTouchSelection()
        {
            Panel(new Rect(20,20,1240,760));
            GUI.Label(new Rect(45,33,1100,52),"Wähle dein Duell",title);
            var large=new GUIStyle(button){fontSize=25};
            var modes=new[]{MatchMode.PlayerVsCpu,MatchMode.PlayerVsPlayer,MatchMode.CpuVsCpu};
            for(int i=0;i<3;i++)
            {
                var old=GUI.backgroundColor;GUI.backgroundColor=selectedMode==modes[i]?cyan:Color.white;
                if(NavButton(new Rect(45+i*397,100,385,82),i==0?"Gegen CPU":i==1?"Lokal zu zweit":"CPU gegen CPU",large))selectedMode=modes[i];GUI.backgroundColor=old;
            }
            TouchCity(0,45,ref cityA,ref avatarA,large);TouchCity(1,655,ref cityB,ref avatarB,large);
            if(NavButton(new Rect(45,530,580,76),selectedMode==MatchMode.PlayerVsPlayer?"Zufällige Städte":"KI: "+DifficultyName(selectedDifficulty),large))
            {
                if(selectedMode==MatchMode.PlayerVsPlayer)RandomizeSelection();
                else selectedDifficulty=selectedDifficulty==CpuDifficulty.Easy?CpuDifficulty.Normal:selectedDifficulty==CpuDifficulty.Normal?CpuDifficulty.Hard:CpuDifficulty.Easy;
            }
            if(NavButton(new Rect(655,530,580,76),"Wetter: "+(environment==data.environments.Length?"Zufall":data.environments[environment].name),large))environment=(environment+1)%(data.environments.Length+1);
            GUI.Label(new Rect(45,614,1190,42),selectedMode==MatchMode.PlayerVsPlayer?"P1: Touch · P2: Tastatur oder eigener Controller":"Bewegen + A halten · B ausweichen · R halten und loslassen",new GUIStyle(small){fontSize=23});
            if(NavButton(new Rect(45,670,230,82),"Menü",large))screen=ScreenMode.Menu;
            if(NavButton(new Rect(290,670,300,82),"Alles zufällig",large))RandomizeSelection();
            if(NavButton(new Rect(655,670,580,82),"Duell starten",large))StartSelectedMatch();
        }
        void TouchCity(int side,float x,ref int city,ref int avatar,GUIStyle style)
        {
            GUI.Label(new Rect(x,198,580,34),side==0?(selectedMode==MatchMode.CpuVsCpu?"CPU 1":"P1 · Touch"):(selectedMode==MatchMode.PlayerVsPlayer?"P2":"CPU"),new GUIStyle(heading){fontSize=25});
            if(NavButton(new Rect(x,244,580,82),data.cities[city].name+"  ▸",style)){city=(city+1)%data.cities.Length;avatar=0;}
            var avatars=Avatars(city);avatar=Mathf.Clamp(avatar,0,avatars.Length-1);
            if(NavButton(new Rect(x,342,580,82),avatars[avatar].displayName+"  ▸",new GUIStyle(style){fontSize=23}))avatar=(avatar+1)%avatars.Length;
            bool human=selectedMode!=MatchMode.CpuVsCpu&&(side==0||selectedMode==MatchMode.PlayerVsPlayer);
            if(human&&side==1)
            {
                string device=UsesPad(side)?"Controller":"Tastatur";
                if(NavButton(new Rect(x,440,580,70),device+" wechseln",style))CycleDevice(side);
            }
            else GUI.Label(new Rect(x,441,580,58),"Basis: "+(avatars[avatar].attackStyle=="Melee"?"Nahkampf":"Fernkampf"),new GUIStyle(label){fontSize=26});
        }
    }
}

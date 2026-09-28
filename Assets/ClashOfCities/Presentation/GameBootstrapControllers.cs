using System.Linq;
using UnityEngine;
using ClashOfCities.Core;

namespace ClashOfCities.Presentation
{
    public sealed partial class GameBootstrap
    {
        CpuDifficulty selectedDifficulty=CpuDifficulty.Easy;
        static string DifficultyName(CpuDifficulty difficulty){return difficulty==CpuDifficulty.Easy?"Leicht":difficulty==CpuDifficulty.Hard?"Schwer":"Normal";}
        LinuxGamepads gamepads;
        // -1 keyboard, -2 waiting for a device; nonnegative IDs remain stable across hotplug.
        readonly int[] inputDevice={-1,-1};
        readonly string[] deviceIdentity=new string[2];
        bool connectionBlocked;
        bool touchWasAvailable;
        bool TouchMode { get { return gamepads!=null&&gamepads.Find(1000)!=null&&gamepads.Find(1000).Name=="Touch"; } }
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void ClashTouchMode(int active);
#endif
        bool UsesPad(int side){return inputDevice[side]!=-1;}
        string SkillKey(int side,int slot){return UsesPad(side)?new[]{"X","Y","R"}[slot]:side==0?new[]{"Q","E","R"}[slot]:new[]{"K","L","I"}[slot];}
        string DeviceProblem(MatchMode mode)
        {
            int count=mode==MatchMode.CpuVsCpu?0:mode==MatchMode.PlayerVsCpu?1:2;
            for(int side=0;side<count;side++)if(UsesPad(side)&&(gamepads==null||gamepads.Find(inputDevice[side])==null))return "Spieler "+(side+1)+": Controller verbinden oder Tastatur wählen.";
            if(count==2&&UsesPad(0)&&inputDevice[0]==inputDevice[1])return "Jeder Spieler benötigt einen eigenen Controller.";
            return "";
        }
        void CycleDevice(int side)
        {
            if(screen==ScreenMode.Battle)return;
            int reserved=selectedMode==MatchMode.PlayerVsPlayer?inputDevice[1-side]:-1;
            var ids=gamepads.Devices.Where(p=>p.Id!=reserved).Select(p=>p.Id).ToList();ids.Insert(0,-1);if(ids.Count==1)ids.Add(-2);
            inputDevice[side]=ids[(ids.IndexOf(inputDevice[side])+1)%ids.Count];error="";ClearInput();
        }
        void DrawInputDevice(int side)
        {
            bool human=selectedMode!=MatchMode.CpuVsCpu&&(side==0||selectedMode==MatchMode.PlayerVsPlayer);
            if(!human)return;
            var pad=gamepads.Find(inputDevice[side]);
            string name=pad!=null&&pad.Name=="Touch"?"Touch":!UsesPad(side)?"Tastatur":pad==null?"Controller · nicht verbunden":"Controller "+(gamepads.Devices.IndexOf(pad)+1)+" · "+pad.Name;
            var style=new GUIStyle(button){fontSize=12,clipping=TextClipping.Clip};
            if(NavButton(new Rect(315+side*612,188,300,29),name+"  ▸",style))CycleDevice(side);
        }
        void StartSelectedMatch()
        {
            string problem=DeviceProblem(selectedMode);if(problem!=""){error=problem;return;}
            int seed;if(!int.TryParse(seedText,out seed)){error="Bitte einen gültigen ganzzahligen Seed eingeben.";return;}
            var aa=Avatars(cityA);var bb=Avatars(cityB);
            StartMatch(new MatchConfig {cityAId=data.cities[cityA].id,avatarAId=aa[avatarA].id,cityBId=data.cities[cityB].id,avatarBId=bb[avatarB].id,seed=seed,climateEventId=environment==data.environments.Length?"":data.environments[environment].id,balanceVersion=data.balance.version,mode=selectedMode,difficulty=selectedDifficulty});
        }
        void UpdateControllers(bool verification=false)
        {
            if(gamepads==null)return;gamepads.Poll();
#if UNITY_WEBGL && !UNITY_EDITOR
            bool touch=TouchMode;
            if(touch&&!touchWasAvailable){inputDevice[0]=1000;ClearInput();}
            if(!touch&&touchWasAvailable&&inputDevice[0]==1000){inputDevice[0]=-1;ClearInput();if(screen==ScreenMode.Battle)paused=true;}
            touchWasAvailable=touch;
            ClashTouchMode(screen==ScreenMode.Battle&&!paused&&!showSkillHelp&&LiveHuman&&!simulation.IsFinished&&inputDevice[0]==1000?1:0);
#endif
            if(((smokeTesting||!Application.isFocused)&&!verification)||data==null)return;
            for(int side=0;side<2;side++)if(inputDevice[side]==-2)
            {
                var free=gamepads.Devices.FirstOrDefault(p=>p.Id!=inputDevice[1-side]);if(free!=null)inputDevice[side]=free.Id;
            }
            for(int side=0;side<2;side++)if(UsesPad(side))
            {
                var current=gamepads.Find(inputDevice[side]);
                if(current!=null)deviceIdentity[side]=current.Identity;
                else if(inputDevice[side]>=0&&deviceIdentity[side]!=null)
                {
                    var matches=gamepads.Devices.Where(p=>p.Identity==deviceIdentity[side]&&p.Id!=inputDevice[1-side]).ToArray();
                    if(matches.Length==1)inputDevice[side]=matches[0].Id;
                }
            }
            if(screen==ScreenMode.Battle&&LiveHuman)
            {
                string problem=DeviceProblem(simulation.Config.mode);
                if(problem!=""){paused=true;ClearInput();error=problem;connectionBlocked=true;}
                else if(connectionBlocked){connectionBlocked=false;error="";}
            }
            foreach(var pad in gamepads.Devices)
            {
                if(pad.Down(6)||pad.Down(4))menuNavigation=true;
                if(screen==ScreenMode.Battle&&!paused)
                {
                    int side=System.Array.IndexOf(inputDevice,pad.Id);
                    if(LiveHuman&&(side<0||!simulation.IsHuman(side)))continue;
                    if(pad.Down(6))TogglePause();else if(pad.Down(4))ToggleSkillHelp();
                    continue;
                }
                if(screen==ScreenMode.Battle&&pad.Down(6)){TogglePause();continue;}
                if(showSkillHelp&&pad.Down(4)){ToggleSkillHelp();continue;}
                NavigateMenu(pad);
                if(screen==ScreenMode.Selection)
                {
                    if(menuItems.Find(x=>x.Id==menuFocus)!=null)
                    {
                        int side=selectionSheet==3?0:selectionSheet==4?1:menuItems.Find(x=>x.Id==menuFocus).Rect.center.x<640?0:1;
                        if(side==0)scrollA.y=Mathf.Max(0,scrollA.y+(float)pad.Scroll*250*Time.unscaledDeltaTime);
                        else scrollB.y=Mathf.Max(0,scrollB.y+(float)pad.Scroll*250*Time.unscaledDeltaTime);
                    }
                }
            }
        }

        void OnDestroy(){if(gamepads!=null)gamepads.Dispose();}
    }
}

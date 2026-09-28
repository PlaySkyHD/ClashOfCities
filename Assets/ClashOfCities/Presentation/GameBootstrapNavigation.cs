using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ClashOfCities.Presentation
{
    public sealed partial class GameBootstrap
    {
        sealed class MenuItem {public string Id;public Rect Rect;}
        readonly List<MenuItem> menuItems=new List<MenuItem>();
        string menuFocus,menuActivate,menuContext,seedDraft;
        bool menuNavigation,seedEditing;
        int menuDirection,menuOwner=-1;
        float nextMenuRepeat;
        string NavigationContext {get{return screen+":"+paused+":"+showSkillHelp+":"+showMapHelp+":"+seedEditing+":"+selectionSheet;}}
        void BeginMenuNavigation()
        {
            string context=NavigationContext;
            if(context!=menuContext){menuContext=context;menuFocus=null;menuActivate=null;menuDirection=0;}
            menuItems.Clear();
        }
        bool NavButton(Rect rect,string text,GUIStyle style)
        {
            string id=NavigationContext+":"+rect.ToString();
            bool navigable=GUI.enabled&&(screen!=ScreenMode.Battle||!paused||rect.y>=215);
            if(navigable)
            {
                menuItems.Add(new MenuItem{Id=id,Rect=rect});
                if(menuFocus==null)menuFocus=id;
                if(menuNavigation&&menuFocus==id)
                {
                    var tint=new Color(1,.79f,.35f);
                    Fill(new Rect(rect.x-3,rect.y-3,rect.width+6,3),tint);Fill(new Rect(rect.x-3,rect.yMax,rect.width+6,3),tint);
                    Fill(new Rect(rect.x-3,rect.y,3,rect.height),tint);Fill(new Rect(rect.xMax,rect.y,3,rect.height),tint);
                }
            }
            bool clicked=GUI.Button(rect,text,style);
            bool activated=navigable&&menuActivate==id;
            if(activated)menuActivate=null;
            if(clicked){menuFocus=id;menuNavigation=false;}
            if((clicked||activated)&&sound!=null){sound.Unlock();sound.Cue("ui");}
            return clicked||activated;
        }
        void MoveMenuFocus(int direction)
        {
            if(menuItems.Count==0)return;
            var current=menuItems.Find(x=>x.Id==menuFocus);
            if(current==null){menuFocus=menuItems[0].Id;return;}
            Vector2 vector=direction==1?Vector2.up:direction==2?Vector2.down:direction==3?Vector2.left:Vector2.right;
            MenuItem best=null;float score=float.MaxValue;
            foreach(var item in menuItems)
            {
                if(item==current)continue;Vector2 delta=item.Rect.center-current.Rect.center;
                float forward=Vector2.Dot(delta,vector);if(forward<=1)continue;
                float lateral=Mathf.Abs(delta.x*vector.y-delta.y*vector.x);
                float candidate=forward+lateral*2.5f;
                if(candidate<score){score=candidate;best=item;}
            }
            if(best!=null)menuFocus=best.Id;
        }
        void NavigateMenu(LinuxGamepads.Pad pad)
        {
            // UI Y grows downwards; SDL movement Z grows upwards.
            int direction=pad.Z>.55?2:pad.Z<-.55?1:pad.X<-.55?3:pad.X>.55?4:0;
            if(direction==0&&!pad.Down(0)&&!pad.Down(1)&&menuOwner!=pad.Id)return;
            if(direction!=0||pad.Down(0)||pad.Down(1)){if(menuOwner!=pad.Id)menuDirection=0;menuOwner=pad.Id;}
            if(direction==0)menuDirection=0;
            else if(direction!=menuDirection||Time.unscaledTime>=nextMenuRepeat)
            {
                menuNavigation=true;MoveMenuFocus(direction);nextMenuRepeat=Time.unscaledTime+(direction==menuDirection?.13f:.36f);menuDirection=direction;
            }
            if(pad.Down(1))
            {
                menuActivate=null;
                if(seedEditing){seedEditing=false;return;}
                if(selectionSheet!=0){selectionSheet=0;return;}
                if(showSkillHelp){ToggleSkillHelp();return;}
                if(screen==ScreenMode.Battle&&paused){TogglePause();return;}
                if(screen==ScreenMode.Selection||screen==ScreenMode.Result){screen=ScreenMode.Menu;error="";return;}
            }
            if(pad.Down(0))
            {
                menuNavigation=true;
                if(screen==ScreenMode.Menu&&inputDevice[0]<0)inputDevice[0]=pad.Id;
                if(menuItems.Count>0){if(!menuItems.Any(x=>x.Id==menuFocus))menuFocus=menuItems[0].Id;menuActivate=menuFocus;}
            }
        }
        void DrawSeedEditor()
        {
            menuItems.Clear();
            Panel(new Rect(400,190,480,430));
            GUI.Label(new Rect(425,210,430,40),"Arena-Code eingeben",heading);
            GUI.Label(new Rect(425,253,430,38),seedDraft,label);
            for(int digit=1;digit<=9;digit++)
                if(NavButton(new Rect(430+(digit-1)%3*140,300+(digit-1)/3*58,130,48),digit.ToString(),button)&&seedDraft.TrimStart('-').Length<10)seedDraft+=digit;
            if(NavButton(new Rect(430,474,130,48),"← Löschen",button)&&seedDraft.Length>0)seedDraft=seedDraft.Substring(0,seedDraft.Length-1);
            if(NavButton(new Rect(570,474,130,48),"0",button)&&seedDraft.TrimStart('-').Length<10)seedDraft+="0";
            if(NavButton(new Rect(710,474,130,48),"±",button))seedDraft=seedDraft.StartsWith("-")?seedDraft.Substring(1):"-"+seedDraft;
            if(NavButton(new Rect(430,543,190,48),"Abbrechen",button))seedEditing=false;
            if(NavButton(new Rect(635,543,205,48),"Übernehmen",button))
            {
                int value;if(int.TryParse(seedDraft,out value)){seedText=value.ToString();seedEditing=false;error="";}else error="Arena-Code muss zwischen −2147483648 und 2147483647 liegen.";
            }
        }
    }
}

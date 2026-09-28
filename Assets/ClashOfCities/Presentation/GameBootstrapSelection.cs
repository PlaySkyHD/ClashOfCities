using UnityEngine;
using ClashOfCities.Core;

namespace ClashOfCities.Presentation
{
    public sealed partial class GameBootstrap
    {
        // 1 arena, 2 controls, 3/4 city details. Only the active sheet receives focus.
        int selectionSheet;
        void DrawSelectionCard(int side,Rect r,Color tint)
        {
            int city=side==0?cityA:cityB,avatar=side==0?avatarA:avatarB;
            bool human=selectedMode!=MatchMode.CpuVsCpu&&(side==0||selectedMode==MatchMode.PlayerVsPlayer);
            Panel(r);Fill(new Rect(r.x,r.y,3,r.height),tint);
            GUI.Label(new Rect(r.x+24,r.y+18,200,28),human?"SPIELER "+(side+1):"CPU",small);
            if(human)
            {
                var pad=gamepads.Find(inputDevice[side]);
                string device=!UsesPad(side)?"Tastatur":pad==null?"Controller fehlt":pad.Name=="Touch"?"Touch":"Controller "+(gamepads.Devices.IndexOf(pad)+1);
                if(NavButton(new Rect(r.x+300,r.y+14,244,40),device+"  ▸",button))CycleDevice(side);
            }
            if(NavButton(new Rect(r.x+24,r.y+76,48,60),"<",button)){city=(city+data.cities.Length-1)%data.cities.Length;avatar=0;}
            if(NavButton(new Rect(r.x+80,r.y+76,384,60),data.cities[city].name,heading)){city=(city+1)%data.cities.Length;avatar=0;}
            if(NavButton(new Rect(r.x+472,r.y+76,72,60),">",button)){city=(city+1)%data.cities.Length;avatar=0;}
            var avatars=Avatars(city);avatar=Mathf.Clamp(avatar,0,avatars.Length-1);
            if(NavButton(new Rect(r.x+24,r.y+157,520,48),avatars[avatar].displayName+(avatars.Length>1?"  ▸":""),button))avatar=(avatar+1)%avatars.Length;
            GUI.Label(new Rect(r.x+24,r.y+220,520,32),avatars[avatar].combatClass+"  ·  "+(avatars[avatar].attackStyle=="Melee"?"Nahkampf":"Fernkampf"),small);
            if(NavButton(new Rect(r.x+24,r.y+275,248,44),"Skills & Stadtinfo",button))selectionSheet=3+side;
            if(NavButton(new Rect(r.x+288,r.y+275,256,44),"Zufällige Stadt",button)){city=selectionRandom.Next(data.cities.Length);avatar=0;}
            if(side==0){cityA=city;avatarA=avatar;}else{cityB=city;avatarB=avatar;}
        }
        void DrawSelectionSheet()
        {
            menuItems.Clear();
            Fill(new Rect(0,0,1280,800),new Color(0,0,0,.78f));
            GUI.enabled=!seedEditing;
            if(selectionSheet>=3)
            {
                if(selectionSheet==3)DrawCityPanel(new Rect(346,65,588,590),ref cityA,ref avatarA,ref scrollA,cyan,"SKILLS & STADTINFO · P1");
                else DrawCityPanel(new Rect(346,65,588,590),ref cityB,ref avatarB,ref scrollB,amber,"SKILLS & STADTINFO · P2");
                if(NavButton(new Rect(346,678,588,52),"Schließen",button))selectionSheet=0;
            }
            else
            {
                Panel(new Rect(300,150,680,490));
                GUI.Label(new Rect(334,180,610,48),selectionSheet==1?"ARENA":"SO SPIELT MAN",heading);
                if(selectionSheet==1)
                {
                    GUI.Label(new Rect(334,248,610,28),"Wetter verändert die Arena und ihre Effekte.",small);
                    if(NavButton(new Rect(334,288,610,52),"Wetter · "+(environment==data.environments.Length?"Zufällig":data.environments[environment].name),button))environment=(environment+1)%(data.environments.Length+1);
                    GUI.Label(new Rect(334,368,610,30),"Arena-Code (Seed) · gleicher Code, gleiche Map",small);
                    if(NavButton(new Rect(334,409,370,52),seedText,button)){seedDraft=seedText;seedEditing=true;}
                    if(NavButton(new Rect(720,409,224,52),"Neue Map",button))seedText=selectionRandom.Next().ToString();
                }
                else
                {
                    GUI.Label(new Rect(334,247,610,80),"Angriff halten statt Tasten spammen. Skills gezielt einsetzen, um Treffer vorzubereiten. Die Ulti halten, aufladen und loslassen.",label);
                    GUI.Label(new Rect(334,348,610,85),selectedMode==MatchMode.CpuVsCpu?"Die CPU kämpft selbstständig. Im Kampf kannst du das Tempo ändern und mit Escape pausieren.":Controls(0),small);
                    GUI.Label(new Rect(334,444,610,90),selectedMode==MatchMode.PlayerVsPlayer?Controls(1):"Nutze Hindernisse als Deckung. Weiche sichtbaren Angriffen aus. F1 öffnet im Kampf die Skill- und Kartenhilfe.",small);
                }
                if(NavButton(new Rect(334,555,610,52),"Schließen",button))selectionSheet=0;
            }
            GUI.enabled=true;
        }
    }
}

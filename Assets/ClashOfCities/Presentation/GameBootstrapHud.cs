using System;
using System.Linq;
using UnityEngine;
using ClashOfCities.Core;

namespace ClashOfCities.Presentation
{
    public sealed partial class GameBootstrap
    {
        Texture2D keyDisc;
        GUIStyle hudTiny,hudName,hudCenter,hudRight,hudKey,hudLongKey,hudTimer,hudKo,hudAvailable,hudUnavailable,hudSmall;
        readonly GUIStyle[] hudMarkers=new GUIStyle[2],hudArrows=new GUIStyle[2];
        void HudStyles()
        {
            if(hudTiny!=null)return;
            hudTiny=Style(12,FontStyle.Normal);hudTiny.wordWrap=false;hudTiny.clipping=TextClipping.Clip;
            hudName=Style(20,FontStyle.Bold);hudName.wordWrap=false;
            hudCenter=new GUIStyle(hudTiny){alignment=TextAnchor.MiddleCenter};
            hudRight=new GUIStyle(hudTiny){alignment=TextAnchor.MiddleRight};
            hudKey=new GUIStyle(hudCenter){fontSize=15,fontStyle=FontStyle.Bold};
            hudLongKey=new GUIStyle(hudKey){fontSize=10};
            hudTimer=new GUIStyle(hudCenter){fontSize=28,fontStyle=FontStyle.Bold};
            hudKo=new GUIStyle(hudTimer){fontSize=44};
            hudAvailable=new GUIStyle(hudRight);hudAvailable.normal.textColor=new Color(.83f,.94f,.9f);
            hudUnavailable=new GUIStyle(hudRight);hudUnavailable.normal.textColor=new Color(.57f,.64f,.68f);
            hudSmall=new GUIStyle(hudTiny){fontSize=10};
            for(int i=0;i<2;i++){hudMarkers[i]=new GUIStyle(hudKey);hudMarkers[i].normal.textColor=i==0?cyan:amber;hudArrows[i]=new GUIStyle(hudMarkers[i]){fontSize=12};}
            keyDisc=new Texture2D(64,64,TextureFormat.RGBA32,false);var pixels=new Color[64*64];
            for(int y=0;y<64;y++)for(int x=0;x<64;x++){float r=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(32,32));pixels[y*64+x]=new Color(1,1,1,Mathf.Clamp01(32-r));}
            keyDisc.SetPixels(pixels);keyDisc.Apply(false,true);
        }
        void KeyGlyph(Rect rect,string key,bool controller)
        {
            bool circle=controller&&(key=="A"||key=="B"||key=="X"||key=="Y"||key=="+"||key=="−");
            Color old=GUI.color;
            GUI.color=new Color(.83f,.89f,.92f);GUI.DrawTexture(rect,circle?keyDisc:Texture2D.whiteTexture);
            GUI.color=new Color(.07f,.11f,.15f);GUI.DrawTexture(new Rect(rect.x+1.5f,rect.y+1.5f,rect.width-3,rect.height-3),circle?keyDisc:Texture2D.whiteTexture);GUI.color=old;
            GUI.Label(rect,key,key.Length>2?hudLongKey:hudKey);
        }
        string SkillType(FighterState f,AbilityDefinition a,int slot)
        {
            if(slot==3)return "AUSWEICHEN";
            if(a==null)return f.Avatar.attackStyle=="Melee"?"NAHKAMPF":"FERNKAMPF";
            return a.delivery=="Self"?"SELBST / BUFF":a.delivery=="Melee"?"NAHKAMPF":a.delivery=="Dash"?"NAH / VORSTOSS":a.delivery=="Zone"?"FERN / FLÄCHE":"FERNKAMPF";
        }
        void DrawPlayerMarkers()
        {
            if(views==null||gameCamera==null)return;
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/800f);
            float offsetX=(Screen.width-1280*scale)*.5f,offsetY=(Screen.height-800*scale)*.5f;
            for(int side=0;side<2;side++)
            {
                if(simulation.Fighters[side].Health<=0)continue;
                Vector3 point=gameCamera.WorldToScreenPoint(views[side].transform.position+Vector3.up*2.8f);
                if(point.z<=0)continue;
                float x=(point.x-offsetX)/scale,y=(Screen.height-point.y-offsetY)/scale;
                Color tint=side==0?cyan:amber;
                var rect=new Rect(x-25,y-24,50,24);
                Fill(rect,new Color(.025f,.045f,.065f,.94f));
                Fill(new Rect(rect.x,rect.yMax-2,rect.width,2),tint);
                GUI.Label(rect,simulation.IsHuman(side)?"P"+(side+1):"CPU",hudMarkers[side]);
                GUI.Label(new Rect(x-9,y-2,18,15),"▼",hudArrows[side]);
            }
        }
        void DrawCompactBattle()
        {
            HudStyles();
            DrawPlayerMarkers();
            CompactFighter(simulation.Fighters[0],new Rect(24,20,400,89),cyan);
            CompactFighter(simulation.Fighters[1],new Rect(856,20,400,89),amber);
            GUI.Label(new Rect(510,16,260,39),TimeSpan.FromSeconds(simulation.Elapsed).ToString(@"mm\:ss"),hudTimer);
            GUI.Label(new Rect(445,53,390,23),simulation.Environment.name+(simulation.Config.mode==MatchMode.PlayerVsPlayer?"":" · KI "+DifficultyName(simulation.Config.difficulty)),hudCenter);
            if(simulation.IsFinished)GUI.Label(new Rect(445,240,390,60),"K. O.",hudKo);
            if(replayViewing)GUI.Label(new Rect(445,180,390,30),"WIEDERHOLUNG · keine Live-Steuerung",hudCenter);
            if(LiveHuman)
            {
                int side=simulation.IsHuman(0)?0:1;bool usingPad=UsesPad(side);
                string attack=usingPad?"A":"Leertaste",dodge=usingPad?"B":"Shift",ulti=usingPad?"R":"R";
                Fill(new Rect(220,646,840,27),new Color(.035f,.065f,.09f,.94f));
                GUI.Label(new Rect(225,647,830,25),attack+" halten: Angriff  ·  Skills einmal antippen  ·  "+dodge+": ausweichen / Abbruch  ·  "+ulti+" halten: Ulti laden",hudCenter);
            }
            bool pad=(simulation.IsHuman(0)&&UsesPad(0))||(simulation.IsHuman(1)&&UsesPad(1));
            if(NavButton(new Rect(540,83,95,28),"     Hilfe",hudCenter))ToggleSkillHelp();KeyGlyph(new Rect(546,86,22,22),pad?"−":"F1",pad);
            if(NavButton(new Rect(645,83,95,28),"     Pause",hudCenter))TogglePause();KeyGlyph(new Rect(651,86,22,22),pad?"+":"Esc",pad);
            if(!LiveHuman&&NavButton(new Rect(604,116,72,23),speed.ToString("F0")+"×",hudCenter))speed=speed==1?2:speed==2?4:1;
            string ring=simulation.Elapsed<data.balance.ringStartSeconds?"": "Kampfzone schrumpft · Außen Schaden";
            GUI.Label(new Rect(430,154,420,20),ring,hudCenter);
            for(int side=0;side<2;side++)
            {
                var fighter=simulation.Fighters[side];Color tint=side==0?cyan:amber;float x=side==0?24:674;
                string[] statuses={"Stun","Root","Focus","Buff","Haste","Vulnerable","Slow"};float sx=side==0?24:856;
                foreach(string status in statuses.Where(t=>BattleSimulation.HasStatus(fighter,t)).Take(3))
                {
                    Fill(new Rect(sx,117,126,24),new Color(.04f,.08f,.11f,.9f));GUI.Label(new Rect(sx+5,118,116,22),EffectName(status),hudCenter);sx+=134;
                }
                if(fighter.ControlImmunityRemaining>0)GUI.Label(new Rect(side==0?24:856,147,400,20),"Kontrollschutz",hudTiny);
                SkillCard(new Rect(x,685,110,91),fighter,null,-1,tint);
                for(int slot=0;slot<3;slot++)SkillCard(new Rect(x+(slot+1)*118,685,110,91),fighter,data.abilities.First(a=>a.id==fighter.Avatar.abilityIds[slot]),slot,tint);
                SkillCard(new Rect(x+472,685,110,91),fighter,null,3,tint);
            }
        }
        void CompactFighter(FighterState fighter,Rect rect,Color tint)
        {
            Fill(rect,new Color(.035f,.065f,.09f,.94f));Fill(new Rect(rect.x,rect.y,3,rect.height),tint);
            GUI.Label(new Rect(rect.x+12,rect.y+6,250,28),fighter.City.name,hudName);
            GUI.Label(new Rect(rect.x+262,rect.y+8,124,23),"HP "+fighter.Health.ToString("F0"),hudRight);
            GUI.Label(new Rect(rect.x+12,rect.y+34,270,18),fighter.Avatar.displayName,hudTiny);
            if(fighter.Shield>0)GUI.Label(new Rect(rect.x+280,rect.y+34,106,18),"Schild "+fighter.Shield.ToString("F0"),hudRight);
            Meter(new Rect(rect.x+12,rect.y+57,376,10),fighter.Health/fighter.Stats.MaxHealth,tint);
            Meter(new Rect(rect.x+12,rect.y+73,376,4),fighter.Energy/fighter.Stats.Energy,new Color(.58f,.57f,1));
        }
        string ShortSkill(AbilityDefinition ability)
        {
            switch(ability.id){case "droste_ultimate":return "Blättersturm";case "beethoven_ultimate":return "Finale";case "gutenberg_ultimate":return "Druckerpresse";default:return ability.name;}
        }
        void SkillCard(Rect rect,FighterState fighter,AbilityDefinition ability,int slot,Color tint)
        {
            bool human=simulation.IsHuman(fighter.Index)&&!replayViewing,controller=human&&UsesPad(fighter.Index);
            string key=slot==-1?(controller?"A":fighter.Index==0?"Space":"J"):slot==3?(controller?"B":fighter.Index==0?"LShift":"RShift"):SkillKey(fighter.Index,slot);
            string name=slot==-1?"Angriff":slot==3?"Ausweichen":ShortSkill(ability),state="Bereit";
            double fill=1;bool available=true;Color bar=tint;
            if(slot==-1&&fighter.BasicAttackCooldown>0){fill=1-fighter.BasicAttackCooldown/data.balance.basicAttackInterval;state="Erholung";available=false;}
            if(slot==-1&&available)
            {
                var enemy=simulation.Fighters[1-fighter.Index];double dx=fighter.X-enemy.X,dz=fighter.Z-enemy.Z;
                if(dx*dx+dz*dz>fighter.Avatar.attackRange*fighter.Avatar.attackRange){state="Distanz";available=false;}
                else if(!simulation.HasLineOfSight(fighter.Index,enemy.Index)){state="Deckung";available=false;}
            }
            if(slot==3)
            {
                fill=1-fighter.DodgeCooldown/data.balance.dodgeCooldown;
                if(fighter.DodgeCooldown>0){state="Erholung";available=false;}
                else if(BattleSimulation.HasStatus(fighter,"Root")){state="Fixiert";available=false;}
                else if(fighter.Energy<data.balance.dodgeEnergyCost){state="Energie";available=false;}
            }
            else if(ability!=null)
            {
                double cooldown=fighter.Cooldowns[ability.id];
                if(ability.isUltimate&&fighter.IsCharging){fill=fighter.Charge;state="Lädt";bar=new Color(1,.83f,.35f);}
                else if(cooldown>0){fill=1-cooldown/(ability.cooldown*fighter.Stats.CooldownModifier);state="Erholung";available=false;}
                else if(ability.isUltimate&&simulation.Elapsed<data.balance.ultimateUnlockSeconds){fill=simulation.Elapsed/data.balance.ultimateUnlockSeconds;state="Gesperrt";available=false;}
                else if(fighter.Energy<ability.energyCost){state="Energie";available=false;}
                else if(ability.effectType=="Heal"&&fighter.Health>=fighter.Stats.MaxHealth){state="HP voll";available=false;}
            }
            if(ability!=null&&available&&!fighter.IsCharging)
            {
                var enemy=simulation.Fighters[1-fighter.Index];double dx=fighter.X-enemy.X,dz=fighter.Z-enemy.Z;
                if(fighter.CastingAbilityId==ability.id){state="Wirkt";available=false;}
                else if(ability.delivery!="Self"&&dx*dx+dz*dz>ability.range*ability.range){state="Distanz";available=false;}
                else if(ability.delivery!="Self"&&!simulation.HasLineOfSight(fighter.Index,enemy.Index)){state="Deckung";available=false;}
            }
            if(BattleSimulation.HasStatus(fighter,"Stun")){state="Betäubt";available=false;}
            Fill(rect,new Color(.035f,.065f,.09f,.94f));
            if(human)KeyGlyph(new Rect(rect.x+9,rect.y+8,!controller&&key.Length>2?38:controller&&key=="R"?34:28,controller&&key=="R"?24:28),key,controller);
            else GUI.Label(new Rect(rect.x+9,rect.y+8,30,28),slot==-1?"ATK":slot==3?"DEF":slot==2?"ULT":(slot+1).ToString(),hudCenter);
            GUI.Label(new Rect(rect.x+44,rect.y+10,60,23),state,available?hudAvailable:hudUnavailable);
            GUI.Label(new Rect(rect.x+7,rect.y+42,rect.width-14,18),name,name.Length>14?hudSmall:hudTiny);
            GUI.Label(new Rect(rect.x+7,rect.y+60,rect.width-14,17),SkillType(fighter,ability,slot),hudSmall);
            Meter(new Rect(rect.x+7,rect.y+82,rect.width-14,4),fill,available?bar:Color.Lerp(bar,new Color(.2f,.27f,.31f),.5f));
        }
    }
}

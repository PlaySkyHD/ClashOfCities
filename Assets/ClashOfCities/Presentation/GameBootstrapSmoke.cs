#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Linq;
using UnityEngine;
using ClashOfCities.Core;

namespace ClashOfCities.Presentation
{
    public sealed partial class GameBootstrap
    {
        // Opt-in player verification; ordinary launches always open the menu.
        IEnumerator SmokeTest()
        {
            smokeTesting=true;
            string output=Path.Combine(Application.persistentDataPath,"SmokeTest");
            var args=Environment.GetCommandLineArgs();
            int flag=Array.IndexOf(args,"-clash-smoke-output");
            if(flag>=0 && flag+1<args.Length) output=args[flag+1];
            Directory.CreateDirectory(output);
            savePath=Path.Combine(output,"last-match.json");
            yield return new WaitForSecondsRealtime(1);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"01-menu.png"));
            yield return null;
            screen=ScreenMode.Selection;
            yield return new WaitForSecondsRealtime(.5f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"02-selection.png"));
            yield return null;
            StartMatch(new MatchConfig {
                cityAId=data.cities[0].id,avatarAId=Avatars(0)[0].id,
                cityBId=data.cities[1].id,avatarBId=Avatars(1)[0].id,
                seed=726491,climateEventId="normal",balanceVersion=data.balance.version,mode=MatchMode.CpuVsCpu
            });
            if(screen!=ScreenMode.Battle) { Debug.LogError("Clash smoke: could not start match."); Application.Quit(1); yield break; }
            speed=4;
            yield return new WaitForSecondsRealtime(2);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"03-battle.png"));
            float sampleStart=Time.realtimeSinceStartup;
            for(int frame=0;frame<90;frame++)yield return null;
            Debug.Log("Clash render sample: "+(90/(Time.realtimeSinceStartup-sampleStart)).ToString("F1")+" FPS average over 90 CPU-battle frames; "+Resources.FindObjectsOfTypeAll<Material>().Length+" loaded materials, "+Resources.FindObjectsOfTypeAll<Texture2D>().Length+" loaded textures.");
            float deadline=Time.realtimeSinceStartup+120;
            while(screen==ScreenMode.Battle && Time.realtimeSinceStartup<deadline) yield return null;
            if(screen!=ScreenMode.Result) { Debug.LogError("Clash smoke: match did not finish."); Application.Quit(1); yield break; }
            var replay=new BattleSimulation(data,simulation.Result.config).RunToEnd();
            if(JsonUtility.ToJson(replay)!=JsonUtility.ToJson(simulation.Result))
            { Debug.LogError("Clash smoke: rendering-speed replay mismatch."); Application.Quit(1); yield break; }
            yield return new WaitForSecondsRealtime(.5f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"04-result.png"));
            File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(simulation.Result,true));
            yield return new WaitForSecondsRealtime(1);
            foreach (var mode in new[] { MatchMode.PlayerVsCpu, MatchMode.PlayerVsPlayer })
            {
                var config=JsonUtility.FromJson<MatchConfig>(JsonUtility.ToJson(simulation.Config));
                config.mode=mode;
                if(mode==MatchMode.PlayerVsPlayer)
                {
                    config.cityAId=data.cities[2].id;config.avatarAId=Avatars(2)[0].id;
                    config.cityBId=data.cities[3].id;config.avatarBId=Avatars(3)[0].id;
                }
                var manual=new BattleSimulation(data,config);
                while(!manual.IsFinished)
                    manual.Step(new FighterInput { moveX=manual.Tick%100<50 ? 1 : -1, basicAttack=true, skill1=true, skill2=true, ultimate=manual.Tick%100<36, dodge=manual.Tick%130==0 },
                        new FighterInput { moveX=manual.Tick%100<50 ? -1 : 1, basicAttack=true, skill1=true, skill2=true, ultimate=manual.Tick%110<32, dodge=manual.Tick%135==0 });
                var recorded=new BattleSimulation(data,config,manual.Result.inputs).RunToEnd();
                if(JsonUtility.ToJson(recorded)!=JsonUtility.ToJson(manual.Result))
                { Debug.LogError("Clash smoke: manual replay mismatch: "+mode); Application.Quit(1); yield break; }
                StartMatch(config);
                for(int tick=0;tick<60 && !simulation.IsFinished;tick++)
                {
                    simulation.Step(new FighterInput { moveX=1, basicAttack=true, skill1=tick%20==0, dodge=tick==45 },
                        new FighterInput { moveX=-1, basicAttack=true, skill2=tick%20==0, dodge=tick==45 });
                    foreach(var view in views) view.CaptureTick();
                }
                paused=false;
                yield return new WaitForSecondsRealtime(.5f);
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(output,mode==MatchMode.PlayerVsCpu ? "05-player-cpu.png" : "06-player-player.png"));
                yield return null;
            }
            // Exercise all weather maps and all eight avatars with a held charge and visible release.
            for(int weather=0;weather<data.environments.Length;weather++)
            {
                int left=weather*2%data.cities.Length,right=(left+1)%data.cities.Length;
                StartMatch(new MatchConfig {cityAId=data.cities[left].id,avatarAId=Avatars(left)[0].id,
                    cityBId=data.cities[right].id,avatarBId=Avatars(right)[0].id,seed=9721+weather,
                    climateEventId=data.environments[weather].id,balanceVersion=data.balance.version,mode=MatchMode.PlayerVsPlayer});
                double chargeRange=Math.Min(data.abilities.First(a=>a.id==simulation.Fighters[0].Avatar.abilityIds[2]).range,data.abilities.First(a=>a.id==simulation.Fighters[1].Avatar.abilityIds[2]).range);
                double separation=Math.Min(7,Math.Max(1.5,chargeRange-.3));
                simulation.Fighters[0].X=-separation*.5;simulation.Fighters[1].X=separation*.5;
                for(int tick=0;tick<180;tick++)
                {
                    simulation.Step();
                    foreach(var view in views)view.CaptureTick();
                }
                simulation.Fighters[0].ControlImmunityRemaining=10;simulation.Fighters[1].ControlImmunityRemaining=10;
                for(int tick=0;tick<28;tick++)
                {
                    simulation.Step(new FighterInput {ultimate=true},new FighterInput {ultimate=true});
                    foreach(var view in views)view.CaptureTick();
                    yield return new WaitForSecondsRealtime(.025f);
                }
                if(!simulation.Fighters[0].IsCharging || simulation.Fighters[0].Charge<.5)
                {Debug.LogError("Clash smoke: held charge failed.");Application.Quit(1);yield break;}
                yield return new WaitForSecondsRealtime(.3f);
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"07-"+data.environments[weather].id+"-charge.png"));
                yield return null;
                for(int tick=0;tick<5;tick++)
                {
                    simulation.Step();foreach(var view in views)view.CaptureTick();
                    yield return new WaitForSecondsRealtime(.05f);
                }
                if(simulation.Fighters[0].AbilityUsage[simulation.Fighters[0].Avatar.abilityIds[2]]==0)
                {Debug.LogError("Clash smoke: charged skill release missing.");Application.Quit(1);yield break;}
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"08-"+data.environments[weather].id+"-release.png"));
                yield return null;
            }
            // Verify every delivery in an isolated rendered skill scenario.
            foreach(string delivery in new[]{"Melee","Dash","Projectile","Zone","Self"})
            {
                var ability=data.abilities.Where(a=>a.delivery==delivery).OrderBy(a=>a.isUltimate).First();
                var avatar=data.avatars.First(a=>a.abilityIds.Contains(ability.id));
                StartMatch(new MatchConfig {cityAId=avatar.cityId,avatarAId=avatar.id,cityBId=data.cities[3].id,avatarBId=Avatars(3)[0].id,
                    seed=9991,climateEventId="normal",balanceVersion=data.balance.version,mode=MatchMode.PlayerVsPlayer});
                double distance=delivery=="Self" ? 4 : delivery=="Melee" ? 1.8 : Math.Min(5.5,ability.range-.3);
                simulation.Fighters[0].X=-distance*.5;simulation.Fighters[1].X=distance*.5;
                simulation.Fighters[0].Health-=100;
                for(int tick=0;tick<8;tick++){simulation.Step();foreach(var view in views)view.CaptureTick();yield return new WaitForSecondsRealtime(.04f);}
                int slot=Array.IndexOf(avatar.abilityIds,ability.id);
                bool fired=false;
                Action<BattleEvent> observe=e=>{if(e.Source==0 && (e.Type=="MeleeStrike"||e.Type=="DashStrike"||e.Type=="ProjectileLaunch"||e.Type=="ZoneCreated"||e.Type=="Impact"))fired=true;};
                simulation.Event+=observe;
                if(ability.isUltimate)
                {
                    while(simulation.Elapsed<data.balance.ultimateUnlockSeconds+.1)simulation.Step();
                    for(int tick=0;tick<28;tick++){simulation.Step(new FighterInput {ultimate=true},default(FighterInput));foreach(var view in views)view.CaptureTick();}
                }
                simulation.Step(new FighterInput {skill1=slot==0,skill2=slot==1},default(FighterInput));
                foreach(var view in views)view.CaptureTick();
                for(int tick=0;tick<Math.Max(1,(int)(ability.castTime/data.balance.tickSeconds)-1);tick++)
                {simulation.Step();foreach(var view in views)view.CaptureTick();yield return new WaitForSecondsRealtime(.05f);}
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"09-"+delivery+"-windup.png"));yield return null;
                for(int tick=0;tick<80&&!fired;tick++){simulation.Step();foreach(var view in views)view.CaptureTick();yield return new WaitForSecondsRealtime(.025f);}
                simulation.Event-=observe;
                if(!fired){Debug.LogError("Clash smoke: delivery failed: "+delivery);Application.Quit(1);yield break;}
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"10-"+delivery+"-effect.png"));yield return null;
                if(delivery=="Zone")
                {
                    for(int tick=0;tick<Math.Ceiling((ability.telegraphSeconds+.3)/data.balance.tickSeconds);tick++){simulation.Step();foreach(var view in views)view.CaptureTick();yield return new WaitForSecondsRealtime(.025f);}
                    yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"11-zone-active.png"));yield return null;
                }
            }
            StartMatch(new MatchConfig {cityAId=data.cities[0].id,avatarAId=Avatars(0)[0].id,cityBId=data.cities[1].id,avatarBId=Avatars(1)[0].id,
                seed=721,climateEventId="drought",balanceVersion=data.balance.version,mode=MatchMode.PlayerVsPlayer});
            simulation.Fighters[0].X=-2;simulation.Fighters[1].X=2;
            while(simulation.Elapsed<data.balance.ringEndSeconds+1 && !simulation.IsFinished){simulation.Step();foreach(var view in views)view.CaptureTick();}
            if(simulation.SafeRadius>data.balance.ringFinalRadius+.01){Debug.LogError("Clash smoke: arena pressure did not activate.");Application.Quit(1);yield break;}
            yield return new WaitForSecondsRealtime(.5f);yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"12-final-ring.png"));yield return null;
            showSkillHelp=true;paused=true;
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"13-skill-help.png"));yield return null;
            showMapHelp=true;
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"14-map-help.png"));yield return null;
            showSkillHelp=false;showMapHelp=false;
            var comboAvatar=data.avatars.First(a=>a.cityId=="ulm");
            StartMatch(new MatchConfig {cityAId="ulm",avatarAId=comboAvatar.id,cityBId=data.cities[0].id,avatarBId=Avatars(0)[0].id,
                seed=721,climateEventId="normal",balanceVersion=data.balance.version,mode=MatchMode.PlayerVsPlayer});
            simulation.Fighters[0].X=-2.5;simulation.Fighters[1].X=2.5;
            while(simulation.Elapsed<data.balance.ultimateUnlockSeconds+.1)simulation.Step();
            simulation.Step(new FighterInput {skill1=true},default(FighterInput));
            for(int tick=0;tick<30&&!BattleSimulation.HasStatus(simulation.Fighters[1],"Stun");tick++){simulation.Step();foreach(var view in views)view.CaptureTick();}
            if(!BattleSimulation.HasStatus(simulation.Fighters[1],"Stun")||!BattleSimulation.HasStatus(simulation.Fighters[0],"Focus"))
            {Debug.LogError("Clash smoke: stun-focus setup failed.");Application.Quit(1);yield break;}
            for(int tick=0;tick<8;tick++){simulation.Step(new FighterInput {ultimate=true},default(FighterInput));foreach(var view in views)view.CaptureTick();yield return new WaitForSecondsRealtime(.05f);}
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"15-stun-focus-charge.png"));yield return null;
            for(int tick=0;tick<3;tick++){simulation.Step();foreach(var view in views)view.CaptureTick();yield return new WaitForSecondsRealtime(.05f);}
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"16-combo-release.png"));yield return null;
            for(int zoneIndex=0;zoneIndex<2;zoneIndex++)
            {
                var zone=simulation.TerrainZones.First(z=>BattleSimulation.IsTerrainBeneficial(z.Kind)==(zoneIndex==0));
                simulation.Fighters[zoneIndex].X=zone.X;simulation.Fighters[zoneIndex].Z=zone.Z;simulation.Fighters[zoneIndex].Health-=100;
            }
            for(int tick=0;tick<8;tick++){simulation.Step();foreach(var view in views)view.CaptureTick();yield return new WaitForSecondsRealtime(.05f);}
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"17-terrain-status.png"));yield return null;
            RandomizeSelection();
            if(cityA==cityB || environment!=data.environments.Length || !int.TryParse(seedText,out var randomSeed))
            {Debug.LogError("Clash smoke: random selection failed.");Application.Quit(1);yield break;}
            yield return ControllerSmoke(output);
            yield return PhysicsSmoke(output);
            Debug.Log("Clash smoke: menu, random selection, rendered CPU battle, result, both manual mode replays, all climate arenas, eight avatars, five skill deliveries, charged releases, obstacles, pressure ring and skill help passed.");
            Application.Quit(0);
        }
    }
}

#endif

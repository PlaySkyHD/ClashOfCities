#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using ClashOfCities.Core;
namespace ClashOfCities.Presentation
{
    public sealed partial class GameBootstrap
    {
        IEnumerator AvatarAttachmentTest()
        {
            smokeTesting=true;
            yield return null;
            var config=new MatchConfig{cityAId=data.cities[0].id,cityBId=data.cities[1].id,
                avatarAId=Avatars(0)[0].id,avatarBId=Avatars(1)[0].id,
                climateEventId=data.environments[0].id,seed=726491,
                balanceVersion=data.balance.version,mode=MatchMode.PlayerVsPlayer};
            StartMatch(config);paused=true;
            simulation.Obstacles.Clear();simulation.TerrainZones.Clear();
            simulation.Fighters[0].X=-1;simulation.Fighters[1].X=1;
            for(int i=0;i<240;i++)
            {
                simulation.Step(new FighterInput{moveZ=i%120<60?.6:-.6,basicAttack=true,skill1=i%60==0,dodge=i%90==0},new FighterInput{basicAttack=true});
                foreach(var view in views)view.CaptureTick();
                yield return new WaitForEndOfFrame();
                foreach(var view in views)if(!view.AnimatedJointsAttached){Debug.LogError("Avatar attachment failed during movement/combat");Application.Quit(1);yield break;}
            }
            paused=false;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(Application.persistentDataPath,"avatar-attachment.png"));
            yield return null;
            Debug.Log("Avatar attachment: 240 rendered combat frames passed; head stays attached; living bones do not interpolate physics poses.");
            simulation.Fighters[0].Health=0;
            views[0].OnBattleEvent(new BattleEvent{Type="Death",Target=0});
            yield return new WaitForSecondsRealtime(.4f);
            if(!views[0].PhysicalCollapse){Debug.LogError("Ragdoll transition failed");Application.Quit(1);yield break;}
            Debug.Log("Avatar attachment: ragdoll transition passed.");Application.Quit(0);
        }
    }
}
#endif

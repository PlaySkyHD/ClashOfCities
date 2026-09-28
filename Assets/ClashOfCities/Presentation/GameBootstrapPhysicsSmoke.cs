#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using UnityEngine;
using ClashOfCities.Core;
namespace ClashOfCities.Presentation
{
    public sealed partial class GameBootstrap
    {
        IEnumerator PhysicsSmoke(string output)
        {
            var config=simulation.Config;config.mode=MatchMode.PlayerVsPlayer;StartMatch(config);
            for(int i=0;i<45;i++)
            {
                simulation.Step(new FighterInput{moveX=.5,moveZ=.7},new FighterInput{moveX=-.5,moveZ=-.7});foreach(var view in views)view.CaptureTick();
                yield return new WaitForSecondsRealtime(.02f);
            }
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"25-weighted-stride.png"));yield return null;
            simulation.Fighters[0].Health=0;simulation.Step();views[0].OnBattleEvent(new BattleEvent{Type="Death",Target=0});
            for(int i=0;i<45;i++)yield return new WaitForSecondsRealtime(.02f);
            ControllerCheck(views[0].PhysicalCollapse,"knockout activates jointed rigidbody collapse");
            foreach(var rb in views[0].GetComponentsInChildren<Rigidbody>())
                if(float.IsNaN(rb.position.y)||rb.position.y<-.5f||rb.velocity.magnitude>30)throw new InvalidOperationException("Unstable ragdoll body: "+rb.name);
            ControllerCheck(true,"ragdoll bodies remain finite and above arena floor");
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"26-physical-knockout.png"));yield return null;
        }
    }
}

#endif

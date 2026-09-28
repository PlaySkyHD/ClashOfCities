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
        void MotionCheck(bool condition,string message)
        {if(!condition){Debug.LogError(message);Application.Quit(1);throw new InvalidOperationException(message);}}
        // Scripted world-space paths isolate presentation from combat/AI decisions.
        IEnumerator LocomotionTest()
        {
            smokeTesting=true;Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
            string output="/tmp/clash-locomotion";Directory.CreateDirectory(output);
            yield return null;
            StartMatch(new MatchConfig{cityAId="ulm",avatarAId="einstein",cityBId="bonn",avatarBId="beethoven",
                climateEventId=data.environments[0].id,seed=726491,balanceVersion=data.balance.version,mode=MatchMode.PlayerVsPlayer});
            simulation.Obstacles.Clear();simulation.TerrainZones.Clear();
            var fighter=simulation.Fighters[0];var rival=simulation.Fighters[1];
            foreach(var view in views){view.CaptureTick();view.CaptureTick();}
            string[] phases={"idle","forward","stop","backward","strafe","turn","charge","dodge","settle","stun","recover"};
            float lowestShoe=100,highestShoe=-100,maxHeadStep=0;
            Vector3 lastHead=Vector3.zero;bool hadHead=false;
            Transform head=views[0].transform.Find("Articulated historical avatar/Flexible spine/Head and face");
            int samples=0,stanceSamples=0;float stanceTravel=0,maxStanceTravel=0,maxFootError=0,maxFootStep=0;
            var lastFoot=new Vector3[2];var lastPlanted=new bool[2];
            for(int phase=0;phase<phases.Length;phase++)
            {
                fighter.Effects.Clear();
                if(phase==9)fighter.Effects.Add(new StatusEffect{Type="Stun",Remaining=2,Source=1});
                for(int frame=0;frame<90;frame++)
                {
                    float dt=Mathf.Min(Time.deltaTime,.04f);
                    Vector3 velocity=phase==1?Vector3.right*2.2f:phase==3?Vector3.left*2.2f:phase==4?Vector3.forward*2.2f:
                        phase==5?new Vector3(Mathf.Cos(frame*.045f),0,Mathf.Sin(frame*.045f))*2.2f:phase==7&&frame<20?Vector3.forward*7:Vector3.zero;
                    fighter.X+=velocity.x*dt;fighter.Z+=velocity.z*dt;fighter.VelocityX=velocity.x;fighter.VelocityZ=velocity.z;
                    fighter.IsCharging=phase==6;fighter.Charge=phase==6?frame/90.0:0;
                    fighter.DodgeRemaining=phase==7&&frame<20?.2:0;
                    fighter.AimX=1;fighter.AimZ=0;
                    if(phase==7&&frame==0)views[0].OnBattleEvent(new BattleEvent{Type="Dodge",Source=0});
                    foreach(var view in views){view.CaptureTick();view.CaptureTick();}
                    yield return null;
                    foreach(var view in views)MotionCheck(view.AnimatedJointsAttached,"Locomotion detached an animated bone");
                    var root=views[0].transform;
                    MotionCheck(!float.IsNaN(root.position.x)&&Vector3.Distance(root.position,new Vector3((float)fighter.X,0,(float)fighter.Z))<=.01f,"Presentation changed or lagged the gameplay root");
                    if(hadHead)maxHeadStep=Mathf.Max(maxHeadStep,Vector3.Distance(lastHead,root.InverseTransformPoint(head.position)));
                    lastHead=root.InverseTransformPoint(head.position);hadHead=true;
                    foreach(var child in root.GetComponentsInChildren<Transform>())if(child.name=="Leather shoe")
                    {float y=child.position.y;lowestShoe=Mathf.Min(lowestShoe,y);highestShoe=Mathf.Max(highestShoe,y);MotionCheck(!float.IsNaN(y)&&y>=-.15f&&y<=.9f,"Foot left plausible ground/swing bounds in "+phases[phase]+" frame "+frame+": "+y);}
                    for(int side=0;side<2;side++)
                    {
                        bool planted=side==0?views[0].LeftFootPlanted:views[0].RightFootPlanted;
                        Vector3 foot=side==0?views[0].LeftFootPosition:views[0].RightFootPosition;
                        Vector3 target=side==0?views[0].LeftFootTarget:views[0].RightFootTarget;
                        if(samples>0&&phase!=7)maxFootStep=Mathf.Max(maxFootStep,Vector3.Distance(foot,lastFoot[side]));
                        if(phase!=7&&frame>10)
                        {
                            maxFootError=Mathf.Max(maxFootError,Vector3.Distance(foot,target));
                            if(planted&&lastPlanted[side]&&velocity.sqrMagnitude>1)
                            {float travel=Vector3.Distance(foot,lastFoot[side]);stanceTravel+=travel;stanceSamples++;maxStanceTravel=Mathf.Max(maxStanceTravel,travel);}
                        }
                        lastFoot[side]=foot;lastPlanted[side]=planted;
                    }
                    samples++;
                    if(frame==45||((phase==1||phase==4)&&(frame==35||frame==55))){ScreenCapture.CaptureScreenshot(Path.Combine(output,phases[phase]+"-"+frame+".png"));}
                }
                Debug.Log("Locomotion phase "+phases[phase]+" passed; max foot error so far "+maxFootError.ToString("F4"));
            }
            Debug.Log("Locomotion presentation: "+samples+" frames; gameplay root exact; joints attached; shoe center range "+lowestShoe.ToString("F3")+".."+highestShoe.ToString("F3")+" m; max local head step "+maxHeadStep.ToString("F3")+" m.");
            Debug.Log("Locomotion feet: mean stance travel per frame "+(stanceTravel/Mathf.Max(1,stanceSamples)).ToString("F4")+" m; max "+maxStanceTravel.ToString("F4")+" m; max ankle-target error "+maxFootError.ToString("F4")+" m.");
            Debug.Log("Locomotion max foot travel per frame (excluding dodge): "+maxFootStep.ToString("F4")+" m.");
            MotionCheck(maxHeadStep<.30f,"Head transition snapped more than 30 cm in one rendered frame");
            MotionCheck(maxFootError<.04f,"Grounded IK deviated more than 4 cm from foot targets");
            Application.Quit(0);
        }
    }
}
#endif

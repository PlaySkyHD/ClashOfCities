#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using ClashOfCities.Core;

namespace ClashOfCities.Presentation
{
    public sealed partial class GameBootstrap
    {
        const string SdlTest="libSDL2-2.0.so.0";
        [DllImport(SdlTest)] static extern int SDL_JoystickAttachVirtual(int type,int axes,int buttons,int hats);
        [DllImport(SdlTest)] static extern int SDL_JoystickDetachVirtual(int index);
        [DllImport(SdlTest)] static extern IntPtr SDL_JoystickOpen(int index);
        [DllImport(SdlTest)] static extern void SDL_JoystickClose(IntPtr joystick);
        [DllImport(SdlTest)] static extern int SDL_JoystickGetDeviceInstanceID(int index);
        [DllImport(SdlTest)] static extern int SDL_JoystickSetVirtualButton(IntPtr joystick,int button,byte value);
        [DllImport(SdlTest)] static extern int SDL_JoystickSetVirtualAxis(IntPtr joystick,int axis,short value);
        void ControllerCheck(bool condition,string description)
        {
            if(condition){Debug.Log("Clash controller check: "+description);return;}
            Debug.LogError("Clash controller failure: "+description);Application.Quit(1);throw new InvalidOperationException(description);
        }
        IEnumerator ControllerSmoke(string output)
        {
            int ia=SDL_JoystickAttachVirtual(1,6,15,0),ib=SDL_JoystickAttachVirtual(1,6,15,0);
            ControllerCheck(ia>=0&&ib>=0,"two virtual controllers attach in Unity");
            IntPtr a=SDL_JoystickOpen(ia),b=SDL_JoystickOpen(ib);
            try
            {
                gamepads.Poll();int ida=SDL_JoystickGetDeviceInstanceID(ia),idb=SDL_JoystickGetDeviceInstanceID(ib);
                ControllerCheck(gamepads.Find(ida)!=null&&gamepads.Find(idb)!=null,"Unity discovers both devices");
                yield return MenuSmoke(a,output);
                inputDevice[0]=-1;inputDevice[1]=ida;selectedMode=MatchMode.PlayerVsCpu;
                bool selectable=false;for(int i=0;i<gamepads.Devices.Count+1;i++){CycleDevice(0);selectable|=inputDevice[0]==ida;}
                ControllerCheck(selectable,"inactive CPU-side assignment does not reserve a controller");
                inputDevice[0]=ida;inputDevice[1]=-1;selectedMode=MatchMode.PlayerVsPlayer;screen=ScreenMode.Selection;
                ControllerCheck(DeviceProblem(selectedMode)=="","controller and keyboard combination accepted");
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"18-mixed-input-selection.png"));yield return null;
                inputDevice[1]=ida;ControllerCheck(DeviceProblem(selectedMode)!="","same controller cannot drive both players");
                inputDevice[1]=idb;ControllerCheck(DeviceProblem(selectedMode)=="","two independent controllers accepted");
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"19-dual-controller-selection.png"));yield return null;
                cityA=3;avatarA=0;cityB=0;avatarB=0;environment=0;StartSelectedMatch();paused=false;
                ControllerCheck(screen==ScreenMode.Battle,"selected controller match starts");
                CycleDevice(0);ControllerCheck(inputDevice[0]==ida&&SkillKey(0,0)=="X","battle cannot change controller binding or glyphs through device selector");
                simulation.Fighters[0].X=-2;simulation.Fighters[1].X=2;
                while(simulation.Elapsed<data.balance.ultimateUnlockSeconds+.1)simulation.Step();
                SDL_JoystickSetVirtualAxis(a,0,32767);gamepads.Poll();CaptureInput();var ca=ReadInput(0);var cb=ReadInput(1);
                ControllerCheck(ca.moveX>.99&&cb.moveX==0,"per-player movement routing");SDL_JoystickSetVirtualAxis(a,0,0);
                SDL_JoystickSetVirtualButton(a,10,1);
                for(int i=0;i<18;i++){gamepads.Poll();CaptureInput();simulation.Step(ReadInput(0),ReadInput(1));foreach(var view in views)view.CaptureTick();}
                ControllerCheck(simulation.Fighters[0].IsCharging&&!simulation.Fighters[1].IsCharging,"holding controller R charges only its fighter");
                SDL_JoystickSetVirtualButton(a,10,0);gamepads.Poll();CaptureInput();simulation.Step(ReadInput(0),ReadInput(1));
                ControllerCheck(simulation.Fighters[0].AbilityUsage[simulation.Fighters[0].Avatar.abilityIds[2]]>0,"releasing controller R fires ultimate");
                SDL_JoystickSetVirtualButton(a,2,1);gamepads.Poll();CaptureInput();SDL_JoystickSetVirtualButton(a,2,0);gamepads.Poll();CaptureInput();
                ControllerCheck(ReadInput(0).skill1&&!ReadInput(0).skill1,"short skill tap survives between simulation ticks and is consumed once");
                SDL_JoystickSetVirtualButton(a,6,1);UpdateControllers(true);ControllerCheck(paused,"plus pauses the match");
                SDL_JoystickSetVirtualButton(a,6,0);UpdateControllers(true);SDL_JoystickSetVirtualButton(a,6,1);UpdateControllers(true);ControllerCheck(!paused,"fresh plus press resumes");SDL_JoystickSetVirtualButton(a,6,0);UpdateControllers(true);
                SDL_JoystickClose(b);b=IntPtr.Zero;SDL_JoystickDetachVirtual(ib);UpdateControllers(true);
                ControllerCheck(paused&&DeviceProblem(simulation.Config.mode)!="","disconnect pauses with a useful device error");
                ib=SDL_JoystickAttachVirtual(1,6,15,0);b=SDL_JoystickOpen(ib);UpdateControllers(true);
                ControllerCheck(DeviceProblem(simulation.Config.mode)==""&&paused,"unambiguous reconnect restores assignment but remains paused");
                showSkillHelp=true;showMapHelp=false;
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"20-controller-skill-help.png"));yield return null;
                yield return PauseAndResultMenuSmoke(a,output);
                StartMatch(simulation.Config);simulation.Fighters[0].Health=0;simulation.Step();
                SDL_JoystickSetVirtualButton(a,0,1);gamepads.Poll();FinishMatch();
                yield return null;UpdateControllers(true);yield return null;
                ControllerCheck(screen==ScreenMode.Result&&!resultInputReleased&&simulation.IsFinished,"KO stays finished while attack is held");
                yield return new WaitForSecondsRealtime(.9f);
                SDL_JoystickSetVirtualButton(a,0,0);UpdateControllers(true);yield return null;yield return null;
                ControllerCheck(resultInputReleased&&screen==ScreenMode.Result,"releasing attack leaves KO result visible");
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"24-knockout.png"));yield return null;
                yield return MenuPress(a,0);
                ControllerCheck(screen==ScreenMode.Battle&&!replayViewing&&UsesPad(0),"fresh confirmation starts rematch with controller binding preserved");
            }
            finally
            {
                if(b!=IntPtr.Zero)SDL_JoystickClose(b);if(a!=IntPtr.Zero)SDL_JoystickClose(a);
                SDL_JoystickDetachVirtual(ib);SDL_JoystickDetachVirtual(ia);gamepads.Poll();inputDevice[0]=-1;inputDevice[1]=-1;
            }
        }
    }
}

#endif

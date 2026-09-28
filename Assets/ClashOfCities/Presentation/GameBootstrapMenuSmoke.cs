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
        IEnumerator SelectionMenuTest()
        {
            smokeTesting=true;
            string output="/tmp/clash-selection-test";Directory.CreateDirectory(output);
            int index=SDL_JoystickAttachVirtual(1,6,15,0);
            IntPtr pad=SDL_JoystickOpen(index);
            try
            {
                gamepads.Poll();yield return null;yield return MenuSmoke(pad,output);
                yield return null;yield return null;
                FocusRect(74,500);yield return MenuPress(pad,0);
                ControllerCheck(selectionSheet==3,"city details open separately");
                ControllerCheck(menuItems.All(i=>i.Rect.x>=346&&i.Rect.xMax<=934),"details focus excludes background selection");
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"details.png"));yield return null;
                yield return MenuPress(pad,1);ControllerCheck(selectionSheet==0,"B closes details");
                FocusRect(990,594);yield return MenuPress(pad,0);ControllerCheck(selectionSheet==2,"tips open separately");
                yield return MenuPress(pad,1);ControllerCheck(selectionSheet==0,"B closes tips");
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"selection.png"));yield return null;
                Debug.Log("Selection menu test passed: controller, nested arena keypad, modal focus, details and tips.");
            }
            finally{SDL_JoystickClose(pad);SDL_JoystickDetachVirtual(index);}
            Application.Quit(0);
        }
        IEnumerator MenuPress(IntPtr pad,int key)
        {
            SDL_JoystickSetVirtualButton(pad,key,1);UpdateControllers(true);yield return null;
            SDL_JoystickSetVirtualButton(pad,key,0);UpdateControllers(true);yield return null;
        }
        void FocusRect(float x,float y){menuFocus=menuItems.First(i=>Mathf.Abs(i.Rect.x-x)<1&&Mathf.Abs(i.Rect.y-y)<1).Id;}
        IEnumerator MenuSmoke(IntPtr pad,string output)
        {
            screen=ScreenMode.Menu;paused=false;showSkillHelp=false;menuNavigation=true;
            var saved=savedResult;savedResult=null;
            yield return null;yield return null;
            yield return MenuPress(pad,12);
            ControllerCheck(menuItems.Find(i=>i.Id==menuFocus).Rect.y==617,"menu navigation skips disabled replay");
            yield return MenuPress(pad,11);
            ControllerCheck(menuItems.Find(i=>i.Id==menuFocus).Rect.y==478,"D-pad up returns focus to Play");
            SDL_JoystickSetVirtualAxis(pad,1,32767);UpdateControllers(true);
            ControllerCheck(menuItems.Find(i=>i.Id==menuFocus).Rect.y==617,"stick moves menu focus");
            SDL_JoystickSetVirtualAxis(pad,1,0);UpdateControllers(true);
            yield return MenuPress(pad,11);
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"21-menu-focus.png"));yield return null;
            yield return MenuPress(pad,0);ControllerCheck(screen==ScreenMode.Selection,"A activates focused Play button");savedResult=saved;
            selectedMode=MatchMode.PlayerVsCpu;selectedDifficulty=CpuDifficulty.Normal;
            yield return null;yield return null;FocusRect(396,594);yield return MenuPress(pad,0);
            ControllerCheck(selectedDifficulty==CpuDifficulty.Hard,"controller selects Hard difficulty");
            yield return MenuPress(pad,0);ControllerCheck(selectedDifficulty==CpuDifficulty.Easy,"controller selects Easy difficulty");
            yield return MenuPress(pad,0);ControllerCheck(selectedDifficulty==CpuDifficulty.Normal,"controller returns to Normal difficulty");
            yield return null;FocusRect(50,594);yield return MenuPress(pad,0);
            ControllerCheck(selectionSheet==1,"controller opens arena settings");
            yield return null;FocusRect(334,409);yield return MenuPress(pad,0);
            ControllerCheck(seedEditing,"controller opens numeric arena-code editor");seedDraft="";
            yield return null;FocusRect(430,300);yield return MenuPress(pad,0);FocusRect(570,300);yield return MenuPress(pad,0);
            ControllerCheck(seedDraft=="12","A enters digits using focused keypad buttons");
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"22-controller-keypad.png"));yield return null;
            FocusRect(635,543);yield return MenuPress(pad,0);ControllerCheck(!seedEditing&&seedText=="12","controller commits arena code");
            FocusRect(334,409);yield return MenuPress(pad,0);seedDraft="999";yield return MenuPress(pad,1);
            ControllerCheck(!seedEditing&&seedText=="12","B cancels arena-code editing without changing seed");
            yield return MenuPress(pad,1);ControllerCheck(selectionSheet==0&&screen==ScreenMode.Selection,"B closes arena settings before leaving selection");
            yield return MenuPress(pad,1);ControllerCheck(screen==ScreenMode.Menu,"B returns from selection to main menu");
            yield return MenuPress(pad,0);ControllerCheck(screen==ScreenMode.Selection,"menu navigation remains usable after returning");
        }
        IEnumerator PauseAndResultMenuSmoke(IntPtr pad,string output)
        {
            paused=true;showSkillHelp=false;yield return null;yield return null;
            ControllerCheck(menuItems.All(i=>i.Rect.y>=215),"pause focus excludes background HUD controls");
            yield return MenuPress(pad,12);yield return MenuPress(pad,0);
            ControllerCheck(showSkillHelp,"pause menu opens skill help with A");
            yield return MenuPress(pad,1);ControllerCheck(!showSkillHelp&&paused,"B closes help and preserves pause");
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"23-pause-focus.png"));yield return null;
            FocusRect(475,448);yield return MenuPress(pad,0);ControllerCheck(screen==ScreenMode.Selection,"pause menu returns to selection with A");
            simulation=new BattleSimulation(data,simulation.Config);simulation.RunToEnd();screen=ScreenMode.Result;
            yield return null;yield return null;FocusRect(770,622);yield return MenuPress(pad,0);
            ControllerCheck(screen==ScreenMode.Selection,"result-menu city selection activates with controller");
        }
    }
}

#endif

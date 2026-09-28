#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ClashOfCities.Core;

namespace ClashOfCities.Presentation
{
    // SDL supplies Linux Switch USB/Bluetooth mappings; Unity's keyboard path stays independent.
    public sealed class LinuxGamepads : IDisposable
    {
        const string Library="libSDL2-2.0.so.0";
        const uint ControllerSubsystem=0x2000;
        public sealed class Pad
        {
            public int Id; public string Name,Identity; public IntPtr Handle;
            public uint Buttons,Pressed; public double X,Z,Scroll;
            internal bool Fresh=true;
            public bool Down(int button){return (Pressed&(1u<<button))!=0;}
            public bool Held(int button){return (Buttons&(1u<<button))!=0;}
            public FighterInput Command(){return new FighterInput {moveX=X,moveZ=Z,basicAttack=Held(0),skill1=Down(2),skill2=Down(3),ultimate=Held(10),dodge=Down(1)};}
        }
        public readonly List<Pad> Devices=new List<Pad>();
        public string Error {get;private set;}
        bool initialized;
        public LinuxGamepads()
        {
            try
            {
                SDL_SetHint("SDL_GAMECONTROLLER_USE_BUTTON_LABELS","1");
                SDL_SetHint("SDL_JOYSTICK_HIDAPI_SWITCH","1");
                SDL_SetHint("SDL_JOYSTICK_HIDAPI_COMBINE_JOY_CONS","1");
                // SDL owns no window here. Unity focus is checked before consuming commands.
                SDL_SetHint("SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS","1");
                initialized=SDL_InitSubSystem(ControllerSubsystem)==0;
                if(!initialized)Error="Controller-Dienst konnte nicht gestartet werden.";
            }
            catch(DllNotFoundException){Error="Controller benötigen die SDL2-Laufzeit auf Linux.";}
            catch(EntryPointNotFoundException){Error="Die installierte SDL2-Version unterstützt diese Controller nicht.";}
        }
        public Pad Find(int id){return Devices.Find(p=>p.Id==id);}
        public static void Stick(short rawX,short rawY,out double x,out double z)
        {
            x=rawX/32768.0;z=-rawY/32768.0;double length=Math.Sqrt(x*x+z*z);
            if(length<=.18){x=0;z=0;return;}
            double scale=Math.Min(1,(length-.18)/.82)/length;x*=scale;z*=scale;
        }
        public void Poll()
        {
            if(!initialized)return;
            SDL_PumpEvents();SDL_GameControllerUpdate();
            for(int i=Devices.Count-1;i>=0;i--)if(SDL_GameControllerGetAttached(Devices[i].Handle)==0){SDL_GameControllerClose(Devices[i].Handle);Devices.RemoveAt(i);}
            for(int i=0;i<SDL_NumJoysticks();i++)
            {
                int id=SDL_JoystickGetDeviceInstanceID(i);if(Find(id)!=null||SDL_IsGameController(i)==0)continue;
                var handle=SDL_GameControllerOpen(i);if(handle==IntPtr.Zero)continue;
                string name=Marshal.PtrToStringAnsi(SDL_GameControllerName(handle))??"Controller";
                Devices.Add(new Pad {Id=id,Name=name,Identity=name+"/"+(Marshal.PtrToStringAnsi(SDL_GameControllerGetSerial(handle))??""),Handle=handle});
            }
            foreach(var pad in Devices)
            {
                uint buttons=0;for(int b=0;b<15;b++)if(SDL_GameControllerGetButton(pad.Handle,b)!=0)buttons|=1u<<b;
                pad.Pressed=pad.Fresh?0:buttons&~pad.Buttons;pad.Buttons=buttons;pad.Fresh=false;
                Stick(SDL_GameControllerGetAxis(pad.Handle,0),SDL_GameControllerGetAxis(pad.Handle,1),out pad.X,out pad.Z);
                pad.Scroll=SDL_GameControllerGetAxis(pad.Handle,3)/32768.0;if(Math.Abs(pad.Scroll)<.2)pad.Scroll=0;
                int dx=(pad.Held(14)?1:0)-(pad.Held(13)?1:0),dz=(pad.Held(11)?1:0)-(pad.Held(12)?1:0);
                if(dx!=0||dz!=0){double length=Math.Sqrt(dx*dx+dz*dz);pad.X=dx/length;pad.Z=dz/length;}
            }
        }
        public void Dispose()
        {
            if(!initialized)return;
            foreach(var pad in Devices)SDL_GameControllerClose(pad.Handle);Devices.Clear();SDL_QuitSubSystem(ControllerSubsystem);initialized=false;
        }
        [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern int SDL_SetHint(string name,string value);
        [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern int SDL_InitSubSystem(uint flags);
        [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern void SDL_QuitSubSystem(uint flags);
        [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern void SDL_PumpEvents();
        [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern void SDL_GameControllerUpdate();
        [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern int SDL_NumJoysticks();
        [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern int SDL_JoystickGetDeviceInstanceID(int index);
        [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern int SDL_IsGameController(int index);
        [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern IntPtr SDL_GameControllerOpen(int index);
        [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern IntPtr SDL_GameControllerGetSerial(IntPtr pad);
        [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern IntPtr SDL_GameControllerName(IntPtr pad);
        [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern int SDL_GameControllerGetAttached(IntPtr pad);
        [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern void SDL_GameControllerClose(IntPtr pad);
        [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern byte SDL_GameControllerGetButton(IntPtr pad,int button);
        [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern short SDL_GameControllerGetAxis(IntPtr pad,int axis);
    }
}

#endif

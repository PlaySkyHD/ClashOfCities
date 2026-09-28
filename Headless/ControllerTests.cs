using System.Runtime.InteropServices;
using ClashOfCities.Presentation;

static class ControllerTests
{
    const string Lib="libSDL2-2.0.so.0";
    [DllImport(Lib)] static extern int SDL_JoystickAttachVirtual(int type,int axes,int buttons,int hats);
    [DllImport(Lib)] static extern int SDL_JoystickDetachVirtual(int index);
    [DllImport(Lib)] static extern IntPtr SDL_JoystickOpen(int index);
    [DllImport(Lib)] static extern void SDL_JoystickClose(IntPtr joystick);
    [DllImport(Lib)] static extern int SDL_JoystickGetDeviceInstanceID(int index);
    [DllImport(Lib)] static extern int SDL_JoystickSetVirtualAxis(IntPtr joystick,int axis,short value);
    [DllImport(Lib)] static extern int SDL_JoystickSetVirtualButton(IntPtr joystick,int button,byte value);
    static void Check(bool value,string message){if(!value)throw new Exception(message);Console.WriteLine("PASS: "+message);}
    public static int Run()
    {
        using var pads=new LinuxGamepads();Check(pads.Error==null,"SDL controller backend initializes");
        int indexA=SDL_JoystickAttachVirtual(1,6,15,0),indexB=SDL_JoystickAttachVirtual(1,6,15,0);
        Check(indexA>=0&&indexB>=0,"Two virtual SDL controllers attach");
        var a=SDL_JoystickOpen(indexA);var b=SDL_JoystickOpen(indexB);
        try
        {
            int idA=SDL_JoystickGetDeviceInstanceID(indexA),idB=SDL_JoystickGetDeviceInstanceID(indexB);pads.Poll();
            var pa=pads.Find(idA);var pb=pads.Find(idB);Check(pa!=null&&pb!=null&&pa.Id!=pb.Id,"Independent stable controller IDs discovered");
            SDL_JoystickSetVirtualAxis(a,0,2000);pads.Poll();Check(pa.Command().moveX==0,"Stick drift rejected by dead zone");
            SDL_JoystickSetVirtualAxis(a,0,32767);SDL_JoystickSetVirtualAxis(a,1,-32768);pads.Poll();var command=pa.Command();
            Check(command.moveX>0&&command.moveZ>0&&Math.Abs(command.moveX*command.moveX+command.moveZ*command.moveZ-1)<.001,"Diagonal movement normalized and vertical axis inverted");
            Check(pb.Command().moveX==0&&pb.Command().moveZ==0,"Other controller movement remains independent");
            SDL_JoystickSetVirtualButton(a,0,1);SDL_JoystickSetVirtualButton(a,2,1);SDL_JoystickSetVirtualButton(a,3,1);SDL_JoystickSetVirtualButton(a,1,1);SDL_JoystickSetVirtualButton(a,10,1);pads.Poll();command=pa.Command();
            Check(command.basicAttack&&command.skill1&&command.skill2&&command.dodge&&command.ultimate,"A/B/X/Y and shoulder R map to all combat actions");
            pads.Poll();command=pa.Command();Check(command.basicAttack&&command.ultimate&&!command.skill1&&!command.skill2&&!command.dodge,"Held attacks/charge persist while skills are edge triggered");
            Check(!pb.Command().basicAttack&&!pb.Command().ultimate,"Other controller receives no combat input");
            SDL_JoystickSetVirtualButton(a,10,0);pads.Poll();Check(!pa.Command().ultimate,"Releasing shoulder releases ultimate");
            SDL_JoystickSetVirtualButton(b,6,1);SDL_JoystickSetVirtualButton(b,4,1);pads.Poll();Check(pb.Down(6)&&pb.Down(4),"Plus and minus map to pause and skill help");
            SDL_JoystickSetVirtualAxis(a,0,0);SDL_JoystickSetVirtualAxis(a,1,0);SDL_JoystickSetVirtualButton(a,13,1);pads.Poll();Check(pa.Command().moveX==-1,"D-pad supports movement without stick");
            SDL_JoystickClose(b);b=IntPtr.Zero;SDL_JoystickDetachVirtual(indexB);pads.Poll();Check(pads.Find(idB)==null&&pads.Find(idA)!=null,"Disconnect removes only the disconnected controller");
            indexB=SDL_JoystickAttachVirtual(1,6,15,0);b=SDL_JoystickOpen(indexB);SDL_JoystickSetVirtualButton(b,6,1);pads.Poll();var reconnected=pads.Find(SDL_JoystickGetDeviceInstanceID(indexB));Check(reconnected!=null,"Reconnected controller discovered");
            Check(!reconnected.Down(6),"Held button on reconnect does not generate a fresh pause/resume press");
        }
        finally {if(b!=IntPtr.Zero)SDL_JoystickClose(b);SDL_JoystickClose(a);SDL_JoystickDetachVirtual(indexB);SDL_JoystickDetachVirtual(indexA);}
        return 0;
    }
}

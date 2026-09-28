#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using ClashOfCities.Core;
namespace ClashOfCities.Presentation
{
    // Keeps the desktop input contract while reading the browser Gamepad API.
    public sealed class LinuxGamepads : IDisposable
    {
        [Serializable] sealed class Snapshot { public Device[] devices; }
        [Serializable] sealed class Device { public int id;public string name;public uint buttons;public float x,y,scroll; }
        public sealed class Pad
        {
            public int Id;public string Name,Identity;public IntPtr Handle;
            public uint Buttons,Pressed;public double X,Z,Scroll;internal bool Fresh=true;
            public bool Down(int button){return (Pressed&(1u<<button))!=0;}
            public bool Held(int button){return (Buttons&(1u<<button))!=0;}
            public FighterInput Command(){return new FighterInput{moveX=X,moveZ=Z,basicAttack=Held(0),skill1=Down(2),skill2=Down(3),ultimate=Held(10),dodge=Down(1)};}
        }
        public readonly List<Pad> Devices=new List<Pad>();
        public string Error {get;private set;}
        [DllImport("__Internal")] static extern string ClashGamepads();
        public Pad Find(int id){return Devices.Find(p=>p.Id==id);}
        public void Poll()
        {
            var snapshot=JsonUtility.FromJson<Snapshot>(ClashGamepads());
            if(snapshot==null||snapshot.devices==null)return;
            Devices.RemoveAll(p=>!Array.Exists(snapshot.devices,d=>d.id==p.Id));
            foreach(var d in snapshot.devices)
            {
                var p=Find(d.id);
                if(p!=null&&p.Identity!=d.name){Devices.Remove(p);p=null;}
                if(p==null){p=new Pad{Id=d.id,Name=d.name,Identity=d.name};Devices.Add(p);}
                p.Pressed=p.Fresh?0:d.buttons&~p.Buttons;p.Buttons=d.buttons;p.Fresh=false;
                double length=Math.Sqrt(d.x*d.x+d.y*d.y),scale=length<=.18?0:Math.Min(1,(length-.18)/.82)/length;
                p.X=d.x*scale;p.Z=-d.y*scale;p.Scroll=Math.Abs(d.scroll)<.2?0:d.scroll;
                int dx=(p.Held(14)?1:0)-(p.Held(13)?1:0),dz=(p.Held(11)?1:0)-(p.Held(12)?1:0);
                if(dx!=0||dz!=0){length=Math.Sqrt(dx*dx+dz*dz);p.X=dx/length;p.Z=dz/length;}
            }
        }
        public void Dispose(){Devices.Clear();}
    }
}
#endif

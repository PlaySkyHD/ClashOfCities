using System.Collections.Generic;
using UnityEngine;
using ClashOfCities.Core;

namespace ClashOfCities.Presentation
{
    // Every visible shot corresponds to one live deterministic collision object.
    public sealed class ProjectileView : MonoBehaviour
    {
        sealed class Shot
        {
            public Transform Root;
            public Vector3 Previous, Current;
        }
        readonly Dictionary<int,Shot> shots=new Dictionary<int,Shot>();
        readonly HashSet<int> live=new HashSet<int>();
        readonly List<int> expired=new List<int>();
        BattleSimulation simulation;
        int capturedTick=-1;
        public void Bind(BattleSimulation value){Clear();simulation=value;capturedTick=-1;}
        void Clear(){foreach(var shot in shots.Values)if(shot.Root!=null)Destroy(shot.Root.gameObject);shots.Clear();}
        void OnDestroy(){Clear();}
        Shot Create(ProjectileState p)
        {
            var root=new GameObject("Live projectile "+p.Id+" "+p.AbilityId).transform;root.SetParent(transform,false);
            Color c=p.Source==0?new Color(.3f,.85f,1):new Color(1,.46f,.29f);
            float radius=(float)p.Radius;
            string id=(p.AbilityId??"").ToLowerInvariant();
            if(id.Contains("bach")||id.Contains("beethoven")||id.Contains("brahms"))
            {
                ArenaView.Primitive("Music note head",PrimitiveType.Sphere,root,new Vector3(-radius*.25f,0,0),new Vector3(radius*1.1f,radius*.68f,radius*.7f),c);
                ArenaView.Primitive("Music note stem",PrimitiveType.Cube,root,new Vector3(radius*.22f,radius*.65f,0),new Vector3(.07f,radius*1.6f,.07f),c);
                var flag=ArenaView.Primitive("Music note flag",PrimitiveType.Cube,root,new Vector3(radius*.65f,radius*1.25f,0),new Vector3(radius,.09f,.08f),c);flag.transform.localRotation=Quaternion.Euler(0,0,-24);
            }
            else if(id.Contains("einstein"))
            {
                ArenaView.Primitive("Atom nucleus",PrimitiveType.Sphere,root,Vector3.zero,Vector3.one*radius*.7f,c);
                for(int i=0;i<(p.IsUltimate?3:2);i++){var ring=ArenaView.Ring(p.IsUltimate?"Ultimate singularity orbit":"Energy orbital",root,Vector3.zero,radius,p.IsUltimate?.04f:.025f,c);ring.localRotation=Quaternion.Euler(40+i*60,i*45,0);}
                if(p.IsUltimate)for(int i=0;i<4;i++){float angle=i*Mathf.PI*.5f;ArenaView.Primitive("Singularity satellites",PrimitiveType.Sphere,root,new Vector3(Mathf.Sin(angle)*radius*.72f,Mathf.Cos(angle)*radius*.72f,0),Vector3.one*radius*.25f,Color.white);}
            }
            else if(id.Contains("humboldt"))
            {
                ArenaView.Primitive("Razor leaf",PrimitiveType.Sphere,root,Vector3.zero,new Vector3(radius*1.2f,radius*.20f,radius*2),new Color(.45f,1,.3f));
                ArenaView.Primitive("Leaf central vein",PrimitiveType.Cube,root,new Vector3(0,radius*.12f,0),new Vector3(.035f,.025f,radius*1.8f),new Color(.9f,1,.6f));
            }
            else if(id.Contains("gutenberg"))
            {
                ArenaView.Primitive("Flying printing block",PrimitiveType.Cube,root,Vector3.zero,Vector3.one*radius*1.4f,new Color(.23f,.12f,.35f));
                ArenaView.Primitive("Raised printing letter",PrimitiveType.Cube,root,new Vector3(0,radius*.75f,0),new Vector3(radius*.85f,.05f,radius*.25f),new Color(.9f,.7f,1));
            }
            else
            {
                var feather=ArenaView.Primitive("Quill blade",PrimitiveType.Sphere,root,Vector3.zero,new Vector3(radius*.85f,radius*.20f,radius*2),Color.Lerp(c,Color.white,.4f));
                ArenaView.Primitive("Quill shaft",PrimitiveType.Cube,root,Vector3.zero,new Vector3(.03f,.03f,radius*2.3f),c);
            }
            // Two short streaks belong to the shot itself; they never spawn behind its caster.
            for(int i=0;i<2;i++)ArenaView.Primitive("Shot direction streak",PrimitiveType.Cube,root,new Vector3((i==0?-1:1)*radius*.28f,0,-radius*1.4f),new Vector3(.025f,.025f,radius*.6f),c);
            var shadow=ArenaView.Ring("Projectile ground footprint",root,new Vector3(0,-1.35f,0),radius,.018f,c);
            Vector3 position=new Vector3((float)p.X,1.35f,(float)p.Z);root.position=position;
            return new Shot{Root=root,Previous=position,Current=position};
        }
        public void Render(float alpha)
        {
            if(simulation==null)return;
            bool tickChanged=capturedTick!=simulation.Tick;live.Clear();
            foreach(var p in simulation.Projectiles)
            {
                live.Add(p.Id);
                Shot shot;
                if(!shots.TryGetValue(p.Id,out shot)){shot=Create(p);shots.Add(p.Id,shot);}
                if(tickChanged){shot.Previous=shot.Current;shot.Current=new Vector3((float)p.X,1.35f,(float)p.Z);}
                shot.Root.position=Vector3.Lerp(shot.Previous,shot.Current,Mathf.Clamp01(alpha));
                Vector3 direction=new Vector3((float)p.DirectionX,0,(float)p.DirectionZ);
                if(direction.sqrMagnitude>.001f)shot.Root.rotation=Quaternion.LookRotation(direction);
            }
            expired.Clear();foreach(var pair in shots)if(!live.Contains(pair.Key))expired.Add(pair.Key);
            foreach(int id in expired){Destroy(shots[id].Root.gameObject);shots.Remove(id);}capturedTick=simulation.Tick;
        }
        public void OnBattleEvent(BattleEvent e)
        {
            if(e.Type=="ProjectileImpact")CombatFx.Hit(new Vector3((float)e.X,1.35f,(float)e.Z),e.Source==0?new Color(.3f,.85f,1):new Color(1,.46f,.29f));
        }
    }
}

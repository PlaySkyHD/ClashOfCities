using System.Collections.Generic;
using UnityEngine;
using ClashOfCities.Core;

namespace ClashOfCities.Presentation
{
    // All floor warnings represent authoritative core areas, with no separate timing or hit logic.
    public sealed class BattleEffectsView : MonoBehaviour
    {
        sealed class AreaArt { public Transform Root, Fill; public Renderer[] Renderers; public float WarningDuration; }
        readonly Dictionary<int,AreaArt> areas=new Dictionary<int,AreaArt>();
        readonly HashSet<int> live=new HashSet<int>();
        readonly List<int> expired=new List<int>();
        BattleSimulation simulation;
        Transform perimeter, terrain;
        float halfSize;
        readonly List<Transform> dangerMarks=new List<Transform>();
        public void Bind(BattleSimulation value,GameData data)
        {
            Clear();simulation=value;halfSize=(float)data.balance.arenaHalfSize;
            CreateTerrain();
            perimeter=ArenaView.Ring("Closing arena pressure boundary",transform,new Vector3(0,.10f,0),1,.008f,new Color(1,.24f,.12f));
            for(int i=0;i<64;i++)
            {
                var mark=ArenaView.Primitive("Danger beyond boundary",PrimitiveType.Cube,transform,Vector3.zero,new Vector3(.16f,.025f,.7f),new Color(.69f,.20f,.12f)).transform;
                dangerMarks.Add(mark);
            }
        }
        void Clear()
        {
            foreach(var a in areas.Values)if(a.Root!=null)Destroy(a.Root.gameObject);areas.Clear();
            if(perimeter!=null)Destroy(perimeter.gameObject);
            if(terrain!=null)Destroy(terrain.gameObject);
            foreach(var m in dangerMarks)if(m!=null)Destroy(m.gameObject);dangerMarks.Clear();
        }
        void OnDestroy(){Clear();}
        void FloorBar(Transform parent,string name,Vector3 position,Vector3 size,Color color,float angle=0)
        {var t=ArenaView.Primitive(name,PrimitiveType.Cube,parent,position,size,color).transform;t.localRotation=Quaternion.Euler(0,angle,0);}
        void CreateTerrain()
        {
            terrain=new GameObject("Persistent authoritative terrain zones").transform;terrain.SetParent(transform,false);
            foreach(var zone in simulation.TerrainZones)
            {
                var root=new GameObject("Terrain "+zone.Kind+" "+zone.Id).transform;root.SetParent(terrain,false);root.localPosition=new Vector3((float)zone.X,0,(float)zone.Z);
                float radius=(float)zone.Radius;
                Color c=zone.Kind=="Regen"?new Color(.24f,.79f,.48f):zone.Kind=="Focus"?new Color(.40f,.71f,1):zone.Kind=="Haste"?new Color(.79f,.91f,.27f):zone.Kind=="Heat"?new Color(1,.40f,.17f):new Color(.63f,.44f,.29f);
                ArenaView.Primitive("Exact terrain floor",PrimitiveType.Cylinder,root,new Vector3(0,.031f,0),new Vector3(radius*2,.012f,radius*2),Color.Lerp(new Color(.30f,.32f,.28f),c,.26f));
                ArenaView.Ring("Exact terrain boundary",root,new Vector3(0,.06f,0),radius,.035f,c);
                float symbol=Mathf.Min(.72f,radius*.55f);
                if(zone.Kind=="Regen")
                {
                    FloorBar(root,"Healing plus",new Vector3(0,.063f,0),new Vector3(symbol*1.5f,.02f,symbol*.38f),c);
                    FloorBar(root,"Healing plus",new Vector3(0,.063f,0),new Vector3(symbol*.38f,.02f,symbol*1.5f),c);
                }
                else if(zone.Kind=="Focus")
                {
                    for(int j=0;j<4;j++)FloorBar(root,"Focus star",new Vector3(0,.063f,0),new Vector3(symbol*1.65f,.02f,.075f),c,j*45);
                    ArenaView.Ring("Focus target",root,new Vector3(0,.067f,0),symbol*.55f,.023f,c);
                }
                else if(zone.Kind=="Haste")
                {
                    for(int j=0;j<2;j++)for(int side=-1;side<=1;side+=2)FloorBar(root,"Haste chevron",new Vector3(side*symbol*.20f,.063f,(j-.5f)*symbol*.7f),new Vector3(symbol*.65f,.02f,.12f),c,side*45);
                }
                else if(zone.Kind=="Heat")
                {
                    for(int j=-1;j<=1;j++)for(int k=0;k<3;k++)FloorBar(root,"Heat wave",new Vector3(j*symbol*.47f+((k%2)*.09f),.063f,(k-1)*symbol*.4f),new Vector3(.09f,.02f,symbol*.46f),c,k%2==0?20:-20);
                }
                else
                {
                    for(int j=-1;j<=1;j++)FloorBar(root,"Mud slows movement",new Vector3(0,.063f,j*symbol*.45f),new Vector3(symbol*(1.3f-Mathf.Abs(j)*.3f),.02f,.13f),c);
                }
                // Benefit zones have four square ticks; hazards use eight spokes, independently of color.
                bool positive=zone.Kind=="Regen"||zone.Kind=="Focus"||zone.Kind=="Haste";
                int count=positive?4:8;
                for(int i=0;i<count;i++){float a=i*Mathf.PI*2/count;FloorBar(root,positive?"Benefit boundary tick":"Hazard boundary spoke",new Vector3(Mathf.Sin(a)*radius*.86f,.064f,Mathf.Cos(a)*radius*.86f),new Vector3(.07f,.02f,positive?.12f:.28f),c,a*Mathf.Rad2Deg);}
            }
        }
        AreaArt Create(AreaState area)
        {
            var root=new GameObject("Telegraphed ground skill "+area.AbilityId).transform;root.SetParent(transform,false);
            float radius=(float)area.Radius;
            ArenaView.Ring("Exact damage boundary",root,new Vector3(0,.08f,0),radius,.055f,Color.white);
            var fill=new GameObject("Warning expansion").transform;fill.SetParent(root,false);
            ArenaView.Ring("Approaching activation",fill,new Vector3(0,.09f,0),radius,.023f,Color.white);
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4;
                var line=ArenaView.Primitive("Area radial rune",PrimitiveType.Cube,root,new Vector3(Mathf.Sin(a)*radius*.75f,.055f,Mathf.Cos(a)*radius*.75f),new Vector3(.045f,.015f,radius*.33f),Color.white).transform;
                line.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);
            }
            string id=area.AbilityId??"";
            if(id.Contains("bach"))
            {
                for(int i=0;i<5;i++)FloorBar(root,"Bach staff line",new Vector3(0,.065f,(i-2)*radius*.18f),new Vector3(radius*1.25f,.02f,.032f),Color.white);
            }
            else if(id.Contains("droste")||id.Contains("humboldt"))
            {
                for(int i=0;i<6;i++)
                {
                    float a=i*Mathf.PI/3;var leaf=ArenaView.Primitive(id.Contains("droste")?"Poetry leaf":"Binding vine leaf",PrimitiveType.Sphere,root,new Vector3(Mathf.Sin(a)*radius*.45f,.06f,Mathf.Cos(a)*radius*.45f),new Vector3(radius*.15f,.035f,radius*.43f),Color.white).transform;leaf.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg+35,0);
                }
                if(id.Contains("humboldt"))ArenaView.Ring("Binding vine",root,new Vector3(0,.065f,0),radius*.46f,.045f,Color.white);
            }
            return new AreaArt{Root=root,Fill=fill,Renderers=root.GetComponentsInChildren<Renderer>(),WarningDuration=Mathf.Max(.05f,(float)area.WarningRemaining)};
        }
        public void Render(float alpha)
        {
            if(simulation==null)return;live.Clear();
            foreach(var area in simulation.Areas)
            {
                live.Add(area.Id);AreaArt art;
                if(!areas.TryGetValue(area.Id,out art)){art=Create(area);areas.Add(area.Id,art);}
                art.Root.position=new Vector3((float)area.X,0,(float)area.Z);
                bool warning=area.WarningRemaining>0;
                Color c=warning?new Color(1,.71f,.12f):area.Source==0?new Color(.23f,.80f,1):new Color(1,.25f,.18f);
                foreach(var r in art.Renderers){r.sharedMaterial.color=c;r.sharedMaterial.EnableKeyword("_EMISSION");r.sharedMaterial.SetColor("_EmissionColor",c*(warning?.14f:.45f));}
                float size=warning?Mathf.Clamp01(1-(float)area.WarningRemaining/art.WarningDuration):.82f;
                art.Fill.localScale=new Vector3(Mathf.Max(.08f,size),1,Mathf.Max(.08f,size));
            }
            expired.Clear();foreach(var pair in areas)if(!live.Contains(pair.Key))expired.Add(pair.Key);
            foreach(int id in expired){Destroy(areas[id].Root.gameObject);areas.Remove(id);}
            float safe=(float)simulation.SafeRadius;
            perimeter.gameObject.SetActive(safe<=halfSize);
            perimeter.localScale=new Vector3(safe,1,safe);
            for(int i=0;i<dangerMarks.Count;i++)
            {
                float a=i*Mathf.PI*2/dangerMarks.Count;var m=dangerMarks[i];
                m.localPosition=new Vector3(Mathf.Sin(a)*(safe+.42f),.06f,Mathf.Cos(a)*(safe+.42f));m.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);
                m.gameObject.SetActive(Mathf.Abs(m.localPosition.x)<halfSize-.2f&&Mathf.Abs(m.localPosition.z)<halfSize-.2f);
            }
        }
        public void OnBattleEvent(BattleEvent e)
        {
            if(e.Type=="Blocked")CombatFx.Hit(new Vector3((float)e.X,1.3f,(float)e.Z),new Color(.71f,.69f,.57f));
        }
    }
}

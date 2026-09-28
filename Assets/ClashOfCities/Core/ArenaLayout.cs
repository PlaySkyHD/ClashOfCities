using System;
using System.Collections.Generic;
namespace ClashOfCities.Core
{
    public static class ArenaLayout
    {
        public static List<TerrainZone> CreateZones(int seed,double halfSize,string climateId)
        {
            var random=new SeededRandom(unchecked((uint)seed)^0xB17EFu);
            string positive=new[]{"Regen","Focus","Haste"}[random.NextInt(3)];
            string negative=climateId=="rain"||climateId=="heavyrain"||climateId=="heavy_rain"?"Mud":climateId=="normal"?"Mud":"Heat";
            var zones=new List<TerrainZone>();double offset=halfSize*.48,radius=halfSize*.13;
            bool swap=random.NextInt(2)==0;
            for(int sign=-1;sign<=1;sign+=2) { zones.Add(new TerrainZone{Id=zones.Count+1,Kind=positive,X=swap?sign*offset:0,Z=swap?0:sign*offset,Radius=radius});zones.Add(new TerrainZone{Id=zones.Count+1,Kind=negative,X=swap?0:sign*offset,Z=swap?sign*offset:0,Radius=radius});}
            return zones;
        }
        public static List<ArenaObstacle> Create(int seed,double halfSize)
        {
            var result=new List<ArenaObstacle>();var random=new SeededRandom(unchecked((uint)seed)^0xA51EAu);
            double offset=Math.Min(halfSize*.48,3.6+random.NextDouble()*.6),radius=Math.Min(halfSize*.1,.85+random.NextDouble()*.2);
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)result.Add(new ArenaObstacle{Id=result.Count+1,X=x*offset,Z=z*offset,Radius=radius,Height=1.8+random.NextDouble()*.5});
            return result;
        }
    }
}

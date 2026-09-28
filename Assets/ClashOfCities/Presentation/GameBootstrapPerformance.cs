#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using ClashOfCities.Core;
namespace ClashOfCities.Presentation
{
    public sealed partial class GameBootstrap
    {
        IEnumerator PerformanceTest()
        {
            yield return null;
            var config=new MatchConfig{cityAId=data.cities[0].id,cityBId=data.cities[1].id,
                avatarAId=Avatars(0)[0].id,avatarBId=Avatars(1)[0].id,
                climateEventId=data.environments.First(e=>e.id.ToLower().Contains("rain")).id,
                seed=726491,balanceVersion=data.balance.version,mode=MatchMode.CpuVsCpu,difficulty=CpuDifficulty.Normal};
            StartMatch(config);
            for(int i=0;i<120;i++)yield return null;
            var frames=new float[600];
            for(int i=0;i<frames.Length;i++){yield return null;frames[i]=Time.unscaledDeltaTime*1000;}
            Array.Sort(frames);
            string report=string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "Clash frame sample: 600 frames, mean {0:F2} ms, p95 {1:F2} ms, p99 {2:F2} ms, max {3:F2} ms, frames >33.3 ms: {4}; {5}x{6}; device {7}",
                frames.Average(),frames[569],frames[593],frames[599],frames.Count(t=>t>33.3f),Screen.width,Screen.height,SystemInfo.graphicsDeviceName);
            Debug.Log(report);
            ScreenCapture.CaptureScreenshot(Path.Combine(Application.persistentDataPath,"performance.png"));
            yield return null;Application.Quit(0);
        }
    }
}
#endif

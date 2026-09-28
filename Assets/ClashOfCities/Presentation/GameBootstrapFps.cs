using UnityEngine;
namespace ClashOfCities.Presentation
{
    public sealed partial class GameBootstrap
    {
        float fpsElapsed;
        int fpsFrames;
        string fpsText="FPS —";
        GUIStyle fpsStyle;
        void UpdateFps()
        {
            if(!Application.isFocused){fpsElapsed=0;fpsFrames=0;return;}
            fpsElapsed+=Time.unscaledDeltaTime;fpsFrames++;
            if(fpsElapsed<.5f)return;
            fpsText=Mathf.RoundToInt(fpsFrames/fpsElapsed)+" FPS";
            fpsFrames=0;fpsElapsed=0;
        }
        void DrawFps()
        {
            if(fpsStyle==null)fpsStyle=new GUIStyle(GUI.skin.label){fontSize=12,alignment=TextAnchor.UpperRight,normal={textColor=new Color(.8f,.85f,.88f)}};
            var matrix=GUI.matrix;GUI.matrix=Matrix4x4.identity;
            GUI.Label(new Rect(Screen.width-88,3,78,18),fpsText,fpsStyle);
            GUI.matrix=matrix;
        }
    }
}

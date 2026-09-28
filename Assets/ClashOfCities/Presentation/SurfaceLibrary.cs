using System.Collections.Generic;
using UnityEngine;

namespace ClashOfCities.Presentation
{
    // Small seamless surface maps are generated once and shared across every match.
    // Animated fighter materials remain independent so hit flashes never affect scenery.
    public static class SurfaceLibrary
    {
        sealed class Maps { public Texture2D Color, Normal; }
        static readonly Dictionary<string,Maps> maps=new Dictionary<string,Maps>();
        static bool Has(string text,params string[] words){foreach(var word in words)if(text.Contains(word))return true;return false;}
        public static string Kind(string name)
        {
            string n=name.ToLowerInvariant();
            if(Has(n,"paving","floor","pillar","column","facade","foundation","promenade","planter","cornice"))return "stone";
            if(Has(n,"trunk","branch"))return "wood";
            if(Has(n,"foliage","moss"))return "leaf";
            if(Has(n,"shoe","volume"))return "leather";
            if(Has(n,"coat","sleeve","skirt","dress","collar","shirt","lapel","scarf","trouser","stocking","cuff","cravat","cap crown","hat","banner","tie"))return "cloth";
            if(Has(n,"hair","wig","curl","beard","moustache"))return "hair";
            return "";
        }
        static float Wave(float u,float v,int x,int y,float phase=0){return Mathf.Sin((u*x+v*y)*Mathf.PI*2+phase);}
        static float Noise(float u,float v,float frequency)
        {
            u=Mathf.Repeat(u,1);v=Mathf.Repeat(v,1);float a=u*u*(3-2*u),b=v*v*(3-2*v);
            return Mathf.Lerp(Mathf.Lerp(Mathf.PerlinNoise(u*frequency+17,v*frequency+29),Mathf.PerlinNoise((u-1)*frequency+17,v*frequency+29),a),Mathf.Lerp(Mathf.PerlinNoise(u*frequency+17,(v-1)*frequency+29),Mathf.PerlinNoise((u-1)*frequency+17,(v-1)*frequency+29),a),b)-.5f;
        }
        static float Height(string kind,float u,float v)
        {
            float grain=Wave(u,v,19,13)*Wave(u,v,31,-17,.7f);
            if(kind=="cloth")return .5f+.055f*(Wave(u,v,64,0)+Wave(u,v,0,64))+.02f*grain;
            if(kind=="wood"||kind=="hair")return .5f+.15f*Mathf.Sin(u*Mathf.PI*32+Wave(u,v,2,3)*.7f)+.025f*grain;
            if(kind=="leather")return .5f+.035f*grain+.035f*Wave(u,v,37,23);
            if(kind=="leaf")return .5f+.24f*Noise(u,v,12)+.08f*Noise(u,v,41);
            return .5f+.28f*Noise(u,v,7)+.16f*Noise(u,v,29)+.045f*Noise(u,v,83);
        }
        static Maps Get(string kind)
        {
            Maps result;if(maps.TryGetValue(kind,out result))return result;
            const int size=256;var color=new Color[size*size];var normals=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=x/(float)size,v=y/(float)size,h=Height(kind,u,v),step=1f/size;
                float shade=.94f+(h-.5f)*(kind=="cloth"?.6f:1.15f);
                color[y*size+x]=new Color(shade,shade,shade,1);
                var n=new Vector3((Height(kind,u-step,v)-Height(kind,u+step,v))*.9f,(Height(kind,u,v-step)-Height(kind,u,v+step))*.9f,1).normalized;
                // Raw RGBA normals: alpha must remain one for Standard's red-times-alpha decoding.
                normals[y*size+x]=new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f,1);
            }
            result=new Maps {Color=Texture(kind+" albedo",color,false),Normal=Texture(kind+" micro normal",normals,true)};maps.Add(kind,result);return result;
        }
        static Texture2D Texture(string name,Color[] pixels,bool linear)
        {
            var texture=new Texture2D(256,256,TextureFormat.RGBA32,true,linear){name=name,wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};
            texture.SetPixels(pixels);texture.Apply(true,true);return texture;
        }
        public static void Apply(Material material,string name)
        {
            string n=name.ToLowerInvariant(),kind=Kind(name);
            material.enableInstancing=true;
            if(kind!="")
            {
                var texture=Get(kind);material.mainTexture=texture.Color;material.SetTexture("_BumpMap",texture.Normal);material.EnableKeyword("_NORMALMAP");
                material.SetFloat("_BumpScale",kind=="cloth"?.45f:.7f);
                material.SetFloat("_Glossiness",kind=="cloth"?.12f:kind=="leather"?.32f:kind=="leaf"?.27f:.23f);
            }
            if(Has(n,"brass","bronze","gilded","buckle","brooch","lantern post","cornice")){material.SetFloat("_Metallic",.55f);material.SetFloat("_Glossiness",.48f);}
            if(Has(n,"face","nose","ear","hand","neck")){material.SetFloat("_Glossiness",.28f);}
            if(Has(n,"puddle","window")){material.SetFloat("_Glossiness",.82f);material.SetFloat("_Metallic",.2f);}
            if(n=="warm lantern"){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",material.color*.8f);}
        }
    }
}

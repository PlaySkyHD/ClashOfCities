using UnityEngine;
using ClashOfCities.Core;

namespace ClashOfCities.Presentation
{
    public sealed class ArenaView : MonoBehaviour
    {
        public string MapName { get; private set; }
        readonly System.Collections.Generic.List<Transform> rain = new System.Collections.Generic.List<Transform>();
        float rainHalfSize;
        readonly System.Collections.Generic.Dictionary<string,Material> surfaceMaterials=new System.Collections.Generic.Dictionary<string,Material>();
        void OnDestroy(){foreach(var material in surfaceMaterials.Values)if(material!=null)Destroy(material);}
        Material SharedSurface(string name,Color color)
        {
            // Palette quantization allows paving and architecture to share materials.
            color=new Color(Mathf.Round(color.r*32)/32,Mathf.Round(color.g*32)/32,Mathf.Round(color.b*32)/32,color.a);
            string key=name+":"+ColorUtility.ToHtmlStringRGBA(color);
            Material material;if(surfaceMaterials.TryGetValue(key,out material))return material;
            material=MaterialFor(color);SurfaceLibrary.Apply(material,name);surfaceMaterials.Add(key,material);return material;
        }
        public void Build(float halfSize) { Build(halfSize, 0, "normal"); }
        public void Build(float halfSize, int seed, string climateId)
        {
            var rng = new System.Random(seed ^ 0x5a71c3);
            bool dry=climateId=="drought", hot=climateId=="heatwave", wet=climateId=="heavyrain" || climateId=="heavyRain";
            int layout=rng.Next(3);
            string[] names=dry?new[]{"Ausgetrockneter Platz", "Staubiger Garten", "Rissiger Hof"}:hot?new[]{"Glühender Boulevard", "Sonnenplatz", "Heißer Innenhof"}:wet?new[]{"Regenpromenade", "Nasser Marktplatz", "Wolkenbruch-Hof"}:new[]{"Stadtgarten", "Grüne Promenade", "Parkpavillon"};
            MapName=names[layout]; rainHalfSize=halfSize;
            Color floor=dry?new Color(.49f,.36f,.23f):hot?new Color(.37f,.28f,.24f):wet?new Color(.18f,.28f,.33f):new Color(.30f,.36f,.38f);
            Color trim=dry?new Color(.74f,.55f,.28f):hot?new Color(.92f,.53f,.23f):wet?new Color(.42f,.68f,.76f):new Color(.5f,.72f,.53f);
            Primitive("Arena floor",PrimitiveType.Cube,transform,new Vector3(0,-.3f,0),new Vector3(halfSize*2+.4f,.6f,halfSize*2+.4f),floor);
            // Individually cut paving catches the light without adding physical obstacles.
            for(int tx=-5;tx<=5;tx++)for(int tz=-5;tz<=5;tz++)
            {
                float tile=halfSize*2/11,shade=Range(rng,.85f,1.12f);
                Primitive("Cut limestone paving",PrimitiveType.Cube,transform,new Vector3(tx*tile,-.015f,tz*tile),new Vector3(tile-.035f,.045f,tile-.035f),floor*shade);
            }
            BuildSurroundings(halfSize,trim,floor,rng,dry);
            if(!dry)
            {
                for(int x=-(int)halfSize+2;x<halfSize;x+=3)
                    Primitive("Paving joint",PrimitiveType.Cube,transform,new Vector3(x,.005f,0),new Vector3(.025f,.01f,halfSize*2),floor*.76f);
                for(int z=-(int)halfSize+2;z<halfSize;z+=3)
                    Primitive("Paving joint",PrimitiveType.Cube,transform,new Vector3(0,.006f,z),new Vector3(halfSize*2,.01f,.025f),floor*.76f);
            }
            // Ground patches are decorative, never cover or invisible movement obstacles.
            for(int i=0;i<7;i++)
            {
                float x=Range(rng,-halfSize+1,halfSize-1),z=Range(rng,-halfSize+1,halfSize-1);
                if(dry)
                {
                    Vector3 p=new Vector3(x,.02f,z);
                    for(int j=0;j<3;j++)
                    {
                        float angle=Range(rng,0,360);float length=Range(rng,.7f,2.2f);
                        var crack=Primitive("Dry earth fissure",PrimitiveType.Cube,transform,p,new Vector3(.035f,.015f,length),floor*.54f);
                        crack.transform.localRotation=Quaternion.Euler(0,angle,0);p+=new Vector3(Mathf.Sin(angle*Mathf.Deg2Rad),0,Mathf.Cos(angle*Mathf.Deg2Rad))*length*.42f;
                    }
                }
                else
                {
                    var patch=Primitive(wet?"Rain puddle":hot?"Scorched paving":"Moss between stones",PrimitiveType.Cylinder,transform,new Vector3(x,.012f,z),new Vector3(Range(rng,.4f,1.4f),.009f,Range(rng,.4f,1)),wet?new Color(.27f,.43f,.49f):hot?floor*.73f:new Color(.28f,.40f,.29f));
                    if(wet)patch.GetComponent<Renderer>().sharedMaterial.SetFloat("_Glossiness",.9f);
                }
            }
            Ring("Civic crest outer",transform,new Vector3(0,.04f,0),2.7f,.035f,trim);
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4;
                var ray=Primitive("Compass rose",PrimitiveType.Cube,transform,new Vector3(Mathf.Sin(a)*1.9f,.035f,Mathf.Cos(a)*1.9f),new Vector3(.075f,.02f,.7f),trim);
                ray.transform.localRotation=Quaternion.Euler(0,i*45,0);
            }
            if(layout==0)
            {
                Ring("Central paving circle",transform,new Vector3(0,.033f,0),3.3f,.045f,trim*.7f);
                Ring("Inner paving circle",transform,new Vector3(0,.033f,0),1.8f,.025f,trim*.65f);
            }
            else if(layout==1)
            {
                for(int side=-1;side<=1;side+=2)Primitive("Promenade stripe",PrimitiveType.Cube,transform,new Vector3(side*3,.03f,0),new Vector3(.09f,.015f,halfSize*2),trim*.7f);
            }
            else
            {
                for(int i=0;i<4;i++)
                {
                    var inlay=Primitive("Diamond courtyard inlay",PrimitiveType.Cube,transform,new Vector3(i%2==0?0:(i==1?3:-3),.03f,i%2==1?0:(i==0?3:-3)),new Vector3(4.24f,.015f,.08f),trim*.65f);
                    inlay.transform.localRotation=Quaternion.Euler(0,i%2==0?45:-45,0);
                }
            }
            for(int side=-1;side<=1;side+=2)
            {
                Primitive("Arena boundary",PrimitiveType.Cube,transform,new Vector3(side*halfSize,.04f,0),new Vector3(.1f,.08f,halfSize*2),trim);
                Primitive("Arena boundary",PrimitiveType.Cube,transform,new Vector3(0,.04f,side*halfSize),new Vector3(halfSize*2,.08f,.1f),trim);
                for(int j=0;j<3;j++)
                {
                    Vector3 p=new Vector3(side*(halfSize+2.1f),0,(j-1)*(halfSize*.8f)+Range(rng,-.5f,.5f));
                    Primitive("Outer planter",PrimitiveType.Cylinder,transform,p,new Vector3(2.2f,.18f,2.2f),floor*1.2f);
                    Primitive("Tree trunk",PrimitiveType.Cylinder,transform,p+Vector3.up*.9f,new Vector3(.2f,.9f,.2f),new Color(.31f,.23f,.17f));
                    for(int branch=0;branch<2;branch++)
                    {
                        var limb=Primitive("Tree branch",PrimitiveType.Cylinder,transform,p+new Vector3(branch==0?-.25f:.25f,1.6f,0),new Vector3(.12f,.6f,.12f),new Color(.31f,.23f,.17f));limb.transform.localRotation=Quaternion.Euler(0,0,branch==0?35:-35);
                    }
                    if(!dry)
                    {
                        Color leaves=hot?new Color(.45f,.41f,.21f):new Color(.21f,.44f,.31f);
                        Primitive("Tree foliage",PrimitiveType.Sphere,transform,p+Vector3.up*2.15f,new Vector3(1.6f,1.8f,1.6f),leaves);
                    }
                }
            }
            // Exact deterministic cylinder silhouettes match movement and shot collision in the core.
            foreach(var obstacle in ArenaLayout.Create(seed,halfSize))
            {
                float radius=(float)obstacle.Radius,height=(float)obstacle.Height;
                Vector3 p=new Vector3((float)obstacle.X,height*.5f,(float)obstacle.Z);
                Color stone=dry?new Color(.59f,.42f,.25f):wet?new Color(.31f,.40f,.44f):new Color(.48f,.49f,.43f);
                Primitive("Solid cover pillar "+obstacle.Id,PrimitiveType.Cylinder,transform,p,new Vector3(radius*2,height*.5f,radius*2),stone);
                Ring("Cover collision footprint",transform,new Vector3(p.x,.05f,p.z),radius,.035f,trim);
                for(int rib=0;rib<12;rib++)
                {
                    float angle=rib*Mathf.PI/6;
                    var flute=Primitive("Carved column flute",PrimitiveType.Cube,transform,new Vector3(p.x+Mathf.Sin(angle)*(radius+.002f),height*.52f,p.z+Mathf.Cos(angle)*(radius+.002f)),new Vector3(.07f,height*.64f,.035f),stone*.78f);
                    flute.transform.localRotation=Quaternion.Euler(0,rib*30,0);
                }
                Ring("Gilded capital",transform,new Vector3(p.x,height-.12f,p.z),radius+.002f,.014f,trim);
                // Rings stay inside the collision silhouette, so cover never promises a wider safe area.
                for(int band=0;band<3;band++)
                    Ring("Masonry courses",transform,new Vector3(p.x,height*(.18f+band*.3f),p.z),radius+.002f,.012f,stone*.65f);
                Primitive("Weathered pillar cap",PrimitiveType.Cylinder,transform,new Vector3(p.x,height-.05f,p.z),new Vector3(radius*1.94f,.05f,radius*1.94f),stone*1.14f);
            }
            if(wet)
            {
                for(int i=0;i<30;i++)
                {
                    var drop=Primitive("Rain streak",PrimitiveType.Cube,transform,new Vector3(Range(rng,-halfSize,halfSize),Range(rng,.3f,6),Range(rng,-halfSize,halfSize)),new Vector3(.014f,.34f,.014f),new Color(.58f,.72f,.8f));
                    drop.transform.localRotation=Quaternion.Euler(0,0,-12);rain.Add(drop.transform);
                }
            }
            var sun=new GameObject("Climate sunlight").AddComponent<Light>();sun.transform.SetParent(transform);sun.type=LightType.Directional;sun.intensity=wet?.8f:hot?1.3f:1.05f;
            sun.color=hot?new Color(1,.79f,.58f):dry?new Color(1,.9f,.73f):wet?new Color(.74f,.85f,1):new Color(1,.96f,.88f);
            sun.transform.rotation=Quaternion.Euler(hot?65:48,Range(rng,-55,-20),0);sun.shadows=LightShadows.Soft;sun.shadowStrength=wet?.4f:.6f;
            QualitySettings.antiAliasing=2;QualitySettings.anisotropicFiltering=AnisotropicFiltering.Enable;
            QualitySettings.shadowDistance=45;QualitySettings.shadowResolution=ShadowResolution.Medium;
            var fill=new GameObject("Soft sky fill").AddComponent<Light>();fill.transform.SetParent(transform);fill.type=LightType.Directional;fill.intensity=.28f;fill.color=new Color(.65f,.80f,1);fill.transform.rotation=Quaternion.Euler(30,145,0);fill.shadows=LightShadows.None;
            var scenery=new System.Collections.Generic.List<GameObject>();
            foreach(var renderer in GetComponentsInChildren<MeshRenderer>())if(!rain.Contains(renderer.transform))scenery.Add(renderer.gameObject);
            StaticBatchingUtility.Combine(scenery.ToArray(),gameObject);
            RenderSettings.ambientLight=wet?new Color(.48f,.56f,.65f):hot?new Color(.66f,.55f,.44f):new Color(.43f,.49f,.57f);
        }
        void BuildSurroundings(float size,Color trim,Color stone,System.Random rng,bool dry)
        {
            Color dark=new Color(.095f,.15f,.19f),gold=new Color(.72f,.57f,.32f);
            Primitive("Foundation",PrimitiveType.Cube,transform,new Vector3(0,-.7f,0),new Vector3(size*2+3,.7f,size*2+3),dark);
            for(int side=-1;side<=1;side+=2)
            {
                Primitive("Side promenade",PrimitiveType.Cube,transform,new Vector3(side*(size+1),-.05f,0),new Vector3(1.8f,.18f,size*2+2),stone*.8f);
                Primitive("End promenade",PrimitiveType.Cube,transform,new Vector3(0,-.05f,side*(size+1)),new Vector3(size*2+2,.18f,1.8f),stone*.8f);
                for(int z=-2;z<=2;z++)
                {
                    Vector3 p=new Vector3(side*(size+1.2f),0,z*5);
                    Primitive("Bronze lamp foot",PrimitiveType.Cylinder,transform,p+Vector3.up*.15f,new Vector3(.6f,.15f,.6f),dark);
                    Primitive("Promenade lantern post",PrimitiveType.Cylinder,transform,p+Vector3.up*1.4f,new Vector3(.10f,1.3f,.10f),gold);
                    Primitive("Warm lantern",PrimitiveType.Cube,transform,p+Vector3.up*2.7f,new Vector3(.32f,.46f,.32f),new Color(1,.84f,.48f));
                    Primitive("Lantern canopy",PrimitiveType.Cube,transform,p+Vector3.up*2.96f,new Vector3(.48f,.07f,.48f),dark);
                }
            }
            // Architecture stays behind the combat area and below the HUD sightlines.
            for(int i=-3;i<=3;i++)
            {
                float x=i*4.8f,height=Range(rng,3.3f,5.6f),z=size+5;
                Color facade=Color.Lerp(stone,dark,.35f+(i+3)%3*.12f);
                Primitive("City facade",PrimitiveType.Cube,transform,new Vector3(x,height*.5f,z),new Vector3(4.3f,height,3),facade);
                Primitive("Cornice",PrimitiveType.Cube,transform,new Vector3(x,height,z),new Vector3(4.5f,.2f,3.2f),gold*.8f);
                for(int row=0;row<2;row++)for(int col=-1;col<=1;col++)
                    Primitive("Recessed city window",PrimitiveType.Cube,transform,new Vector3(x+col*1.15f,1.1f+row*1.65f,z-1.51f),new Vector3(.48f,.87f,.05f),new Color(.14f,.26f,.31f));
                Primitive("City banner",PrimitiveType.Cube,transform,new Vector3(x,height*.7f,z-1.56f),new Vector3(.5f,1.3f,.06f),i%2==0?new Color(.16f,.55f,.59f):gold);
            }
        }
        static float Range(System.Random random,float low,float high) { return low+(high-low)*(float)random.NextDouble(); }
        void Update()
        {
            foreach(var drop in rain)
            {
                var p=drop.localPosition;p+=new Vector3(1.1f,-7,0)*Time.deltaTime;
                if(p.y<.1f){p.y=6;p.x-=.9f;if(p.x>rainHalfSize)p.x=-rainHalfSize;}
                drop.localPosition=p;
            }
        }
        static readonly System.Collections.Generic.Dictionary<PrimitiveType,Mesh> primitiveMeshes=new System.Collections.Generic.Dictionary<PrimitiveType,Mesh>();
        static Mesh PrimitiveMesh(PrimitiveType shape)
        {
            Mesh mesh;if(primitiveMeshes.TryGetValue(shape,out mesh)&&mesh!=null)return mesh;
            var source=GameObject.CreatePrimitive(shape);source.SetActive(false);
            mesh=source.GetComponent<MeshFilter>().sharedMesh;primitiveMeshes[shape]=mesh;Object.Destroy(source);return mesh;
        }
        static Material MaterialFor(Color color)
        {
            var template=Resources.Load<Material>("ArenaMaterial");var material=template!=null?new Material(template):new Material(Shader.Find("Standard"));material.color=color;material.SetFloat("_Glossiness",.20f);return material;
        }
        public static GameObject Primitive(string name,PrimitiveType shape,Transform parent,Vector3 position,Vector3 scale,Color color)
        {
            var go=new GameObject(name);go.AddComponent<MeshFilter>().sharedMesh=PrimitiveMesh(shape);go.AddComponent<MeshRenderer>();if(parent!=null)go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;
            var arena=parent==null?null:parent.GetComponentInParent<ArenaView>();
            var material=arena!=null?arena.SharedSurface(name,color):MaterialFor(color);
            if(arena==null){if(parent!=null&&parent.GetComponentInParent<FighterView>()!=null)SurfaceLibrary.Apply(material,name);go.AddComponent<OwnedMaterial>().Value=material;}
            go.GetComponent<Renderer>().sharedMaterial=material;return go;
        }
        static GameObject MeshObject(string name,Transform parent,Vector3 p,Mesh mesh,Color color)
        {
            var go=new GameObject(name);if(parent!=null)go.transform.SetParent(parent,false);go.transform.localPosition=p;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=MaterialFor(color);SurfaceLibrary.Apply(go.GetComponent<Renderer>().sharedMaterial,name);go.AddComponent<OwnedMaterial>().Value=go.GetComponent<Renderer>().sharedMaterial;go.AddComponent<ProceduralMeshOwner>().Value=mesh;return go;
        }
        public static Transform Ring(string name,Transform parent,Vector3 p,float radius,float width,Color color)
        {
            const int segments=48,sides=6;var vertices=new Vector3[segments*sides];var triangles=new int[segments*sides*6];
            for(int i=0;i<segments;i++)for(int j=0;j<sides;j++)
            {
                float a=i*Mathf.PI*2/segments,b=j*Mathf.PI*2/sides;float r=radius+Mathf.Cos(b)*width;
                vertices[i*sides+j]=new Vector3(Mathf.Sin(a)*r,Mathf.Sin(b)*width,Mathf.Cos(a)*r);
                int index=(i*sides+j)*6,n=((i+1)%segments)*sides+j,q=i*sides+(j+1)%sides,t=((i+1)%segments)*sides+(j+1)%sides;
                triangles[index]=i*sides+j;triangles[index+1]=n;triangles[index+2]=q;triangles[index+3]=q;triangles[index+4]=n;triangles[index+5]=t;
            }
            var uv=new Vector2[vertices.Length];for(int i=0;i<segments;i++)for(int j=0;j<sides;j++)uv[i*sides+j]=new Vector2(i/(float)segments,j/(float)sides);
            var mesh=new Mesh{name=name};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return MeshObject(name,parent,p,mesh,color).transform;
        }
        public static Transform Frustum(string name,Transform parent,Vector3 p,float lower,float upper,float height,Color color,int segments=24)
        {
            var vertices=new Vector3[(segments+1)*2];var triangles=new int[segments*6];
            for(int i=0;i<=segments;i++)
            {
                float a=i*Mathf.PI*2/segments;vertices[i*2]=new Vector3(Mathf.Sin(a)*lower,0,Mathf.Cos(a)*lower);vertices[i*2+1]=new Vector3(Mathf.Sin(a)*upper,height,Mathf.Cos(a)*upper);
                if(i==segments)continue;int t=i*6;triangles[t]=i*2;triangles[t+1]=i*2+2;triangles[t+2]=i*2+1;triangles[t+3]=i*2+1;triangles[t+4]=i*2+2;triangles[t+5]=i*2+3;
            }
            var uv=new Vector2[vertices.Length];for(int i=0;i<=segments;i++){uv[i*2]=new Vector2(i/(float)segments,0);uv[i*2+1]=new Vector2(i/(float)segments,1);}
            var mesh=new Mesh{name=name};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return MeshObject(name,parent,p,mesh,color).transform;
        }
    }
}

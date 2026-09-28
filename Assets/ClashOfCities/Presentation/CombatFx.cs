using UnityEngine;

namespace ClashOfCities.Presentation
{
    // Small, bounded mesh effects keep the build independent of the particle package.
    public sealed class CombatFx : MonoBehaviour
    {
        static int active;
        static readonly System.Collections.Generic.Stack<CombatFx> spheres=new System.Collections.Generic.Stack<CombatFx>();
        static readonly System.Collections.Generic.Stack<CombatFx> cubes=new System.Collections.Generic.Stack<CombatFx>();
        System.Collections.Generic.Stack<CombatFx> pool;
        Renderer cachedRenderer;
        Vector3 velocity, acceleration, spin, initialScale;
        float lifetime=.5f, age;
        bool counted, wave;
        void OnDestroy(){if(counted)active--;}
        static CombatFx Animate(GameObject go,float duration,Vector3 motion,Vector3 gravity,Vector3 angular)
        {
            var fx=go.AddComponent<CombatFx>();fx.lifetime=duration;fx.velocity=motion;fx.acceleration=gravity;fx.spin=angular;fx.initialScale=go.transform.localScale;fx.counted=true;active++;return fx;
        }
        void Update()
        {
            float dt=Time.deltaTime;age+=dt;float t=age/lifetime;
            if(t>=1)
            {
                if(pool!=null&&pool.Count<96){if(counted){active--;counted=false;}gameObject.SetActive(false);pool.Push(this);}
                else Destroy(gameObject);
                return;
            }
            if(wave){float expansion=Mathf.Lerp(.25f,1.8f,t);transform.localScale=new Vector3(expansion,Mathf.Max(.005f,1-t),expansion);return;}
            velocity+=acceleration*dt;transform.position+=velocity*dt;transform.Rotate(spin*dt,Space.Self);
            float fade=1-Mathf.SmoothStep(.5f,1,t);transform.localScale=initialScale*(.75f+Mathf.Sin(t*Mathf.PI)*.35f)*fade;
        }
        static GameObject Shape(string name,PrimitiveType shape,Vector3 p,Vector3 size,Color color)
        {return ArenaView.Primitive(name,shape,null,p,size,color);}
        static void Chip(Vector3 p,Vector3 v,Color color,float size,float duration,PrimitiveType type=PrimitiveType.Sphere)
        {
            if(active>=180)return;
            var pool=type==PrimitiveType.Cube?cubes:spheres;
            CombatFx fx=null;while(pool.Count>0&&fx==null)fx=pool.Pop();
            if(fx==null)
            {
                fx=Animate(Shape("Impact mote",type,p,Vector3.one*size,color),duration,v,new Vector3(0,-1.8f,0),new Vector3(70,95,25));
                fx.pool=pool;fx.cachedRenderer=fx.GetComponent<Renderer>();
                fx.cachedRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                fx.cachedRenderer.receiveShadows=false;
            }
            else
            {
                fx.transform.position=p;fx.transform.rotation=Quaternion.identity;fx.transform.localScale=Vector3.one*size;
                fx.initialScale=Vector3.one*size;fx.age=0;fx.lifetime=duration;fx.velocity=v;
                fx.cachedRenderer.sharedMaterial.color=color;fx.counted=true;active++;fx.gameObject.SetActive(true);
            }
        }
        static void Wave(Vector3 p,Color color,float radius,float duration)
        {
            if(active>=180)return;
            var ring=ArenaView.Ring("Expanding combat wave",null,p,radius,.035f,color);
            var fx=Animate(ring.gameObject,duration,Vector3.zero,Vector3.zero,Vector3.zero);fx.wave=true;ring.localScale=Vector3.one*.25f;
        }
        public static void Dust(Vector3 p,Color c,bool dodge)
        {
            int count=dodge?6:2;
            for(int i=0;i<count;i++){float a=i*2.399f;Chip(p+new Vector3(Mathf.Sin(a)*.24f,.08f,Mathf.Cos(a)*.24f),new Vector3(Mathf.Sin(a)*.5f,.25f,Mathf.Cos(a)*.5f),Color.Lerp(new Color(.56f,.64f,.60f),c,.2f),dodge?.15f:.08f,.36f);}
        }
        public static void Hit(Vector3 p,Color c)
        {
            for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Chip(p,new Vector3(Mathf.Sin(a)*2.0f,Mathf.Cos(a)*1.7f+.4f,(i%3-1)*1.3f),c,.085f,.35f,PrimitiveType.Cube);}
        }
        public static void Slash(Vector3 p,Vector3 target,Color c)
        {
            if(active>=170)return;
            Vector3 d=target-p;d.y=0;if(d.sqrMagnitude<.01f)d=Vector3.forward;d.Normalize();
            Vector3 side=Vector3.Cross(Vector3.up,d);
            for(int i=0;i<5;i++)
            {
                float a=(i-2)*.42f;Vector3 point=p+d*(.8f+Mathf.Cos(a)*.45f)+side*Mathf.Sin(a)*.8f;
                var go=Shape("Baton / quill attack arc",PrimitiveType.Cube,point,new Vector3(.055f,.06f,.33f),Color.Lerp(c,Color.white,.5f));go.transform.rotation=Quaternion.LookRotation(side+d*a);
                Animate(go,.22f,d*.5f,Vector3.zero,Vector3.zero);
            }
        }
        public static void Melee(Vector3 p,Vector3 direction,Color c,float radius,float coneDegrees=100)
        {
            direction.y=0;if(direction.sqrMagnitude<.01f)direction=Vector3.forward;direction.Normalize();
            Vector3 side=Vector3.Cross(Vector3.up,direction);
            for(int i=0;i<11&&active<175;i++)
            {
                float angle=(i-5)*(coneDegrees*Mathf.Deg2Rad/10);
                Vector3 d=direction*Mathf.Cos(angle)+side*Mathf.Sin(angle);
                var arc=Shape("Forward melee cutting arc",PrimitiveType.Cube,p+d*radius,new Vector3(.065f,.10f,Mathf.Max(.12f,radius*.14f)),Color.Lerp(c,Color.white,.4f));
                arc.transform.rotation=Quaternion.LookRotation(Vector3.Cross(Vector3.up,d));
                Animate(arc,.23f,d*.12f,Vector3.zero,Vector3.zero);
            }
        }
        public static void DashTrail(Vector3 p,Vector3 direction,Color c,float length)
        {
            direction.y=0;if(direction.sqrMagnitude<.01f)return;direction.Normalize();
            for(int i=-1;i<=1;i+=2)
            {
                Vector3 side=Vector3.Cross(Vector3.up,direction)*i*.25f;
                var streak=Shape("Dash strike motion line",PrimitiveType.Cube,p-direction*length*.5f+side,new Vector3(.05f,.06f,Mathf.Max(.1f,length)),c);
                streak.transform.rotation=Quaternion.LookRotation(direction);Animate(streak,.20f,Vector3.zero,Vector3.zero,Vector3.zero);
            }
        }
        public static void Ability(Vector3 p,Vector3 target,Color c,int theme,bool ultimate)
        {
            // Self buffs remain at the caster. Traveling attacks are rendered by ProjectileView.
            Wave(new Vector3(p.x,.08f,p.z),c,ultimate?1.3f:.75f,ultimate?.8f:.5f);
        }
        public static void Empower(Vector3 p,Color color,bool printingArmor)
        {
            Wave(p+Vector3.up*.08f,color,1,.65f);
            for(int i=0;i<6&&active<170;i++)
            {
                float a=i*Mathf.PI/3;var glyph=new GameObject(printingArmor?"Printing armor seal":"Brahms crescendo note");
                glyph.transform.position=p+new Vector3(Mathf.Sin(a)*.8f,.5f,Mathf.Cos(a)*.8f);
                if(printingArmor)
                {
                    ArenaView.Primitive("Raised printing block",PrimitiveType.Cube,glyph.transform,Vector3.zero,new Vector3(.22f,.3f,.08f),color);
                    ArenaView.Primitive("Seal letter",PrimitiveType.Cube,glyph.transform,new Vector3(0,0,.05f),new Vector3(.04f,.22f,.03f),Color.white);
                }
                else
                {
                    ArenaView.Primitive("Crescendo note head",PrimitiveType.Sphere,glyph.transform,Vector3.zero,new Vector3(.2f,.14f,.12f),color);
                    ArenaView.Primitive("Crescendo note stem",PrimitiveType.Cube,glyph.transform,new Vector3(.07f,.15f,0),new Vector3(.035f,.32f,.035f),color);
                }
                glyph.transform.rotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);
                Animate(glyph,.85f,Vector3.up*.8f,Vector3.zero,Vector3.zero);
            }
        }
        public static void Heal(Vector3 p)
        {
            Color green=new Color(.33f,1,.62f);Wave(p+Vector3.up*.08f,green,.85f,.6f);
            for(int i=0;i<5&&active<170;i++)
            {
                float a=i*Mathf.PI*2/5;var root=new GameObject("Healing cross");root.transform.position=p+new Vector3(Mathf.Sin(a)*.6f,.6f+i*.12f,Mathf.Cos(a)*.6f);
                ArenaView.Primitive("Cross vertical",PrimitiveType.Cube,root.transform,Vector3.zero,new Vector3(.05f,.22f,.04f),green);
                ArenaView.Primitive("Cross horizontal",PrimitiveType.Cube,root.transform,Vector3.zero,new Vector3(.18f,.05f,.04f),green);
                Animate(root,.85f,Vector3.up*.95f,Vector3.zero,Vector3.zero);
            }
        }
        public static void Death(Vector3 p,Color c)
        {Wave(p+Vector3.up*.09f,c,1.2f,.8f);for(int i=0;i<12;i++){float a=i*Mathf.PI/6;Chip(p+Vector3.up*.8f,new Vector3(Mathf.Sin(a),1.3f,Mathf.Cos(a)),c,.12f,.8f);}}
    }
}

using UnityEngine;
using ClashOfCities.Core;

namespace ClashOfCities.Presentation
{
    // Art is driven only by simulation state. Animation and effects never apply damage.
    public sealed partial class FighterView : MonoBehaviour
    {
        public Animator animator;
        public FighterState Fighter { get; private set; }
        Transform body, head, leftArm, rightArm, leftElbow, rightElbow, leftHip, rightHip, leftKnee, rightKnee, marker, shield;
        Renderer coatRenderer;
        Transform stunArt, rootArt, focusArt, buffArt, vulnerableArt, immunityArt, hasteArt;
        Color color, coat;
        Vector3 previousPosition, targetPosition, enemyPosition;
        float attackPulse, hitPulse, dodgePulse;
        Transform chargeSphere, chargeGuide; Light chargeLight; Renderer[] bodyRenderers;
        GameData gameData; BattleSimulation simulation; string guideAbility; float guideReach;
        public void Configure(GameData data,BattleSimulation battle=null){gameData=data;simulation=battle;}
        AbilityDefinition Ability(string id){if(gameData!=null)foreach(var a in gameData.abilities)if(a.id==id)return a;return null;}
        string activeAbility, delivery="Projectile"; int theme; float castDuration=.35f; bool facingInitialized;
        bool dead, poet, scientist;

        Transform Pivot(string name, Transform parent, Vector3 position)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = position; return t;
        }
        Transform Part(string name, PrimitiveType shape, Transform parent, Vector3 p, Vector3 s, Color c)
        { return ArenaView.Primitive(name, shape, parent, p, s, c).transform; }
        Transform Ball(string name, Transform parent, Vector3 p, Vector3 s, Color c)
        { return Part(name, PrimitiveType.Sphere, parent, p, s, c); }
        Transform Box(string name, Transform parent, Vector3 p, Vector3 s, Color c)
        { return Part(name, PrimitiveType.Cube, parent, p, s, c); }
        Transform Limb(string name, Transform parent, Vector3 p, float length, float width, Color c)
        { return Part(name, PrimitiveType.Capsule, parent, p, new Vector3(width,length*.5f,width), c); }

        public void Initialize(FighterState fighter, Color tint)
        {
            Fighter = fighter; color = tint;
            string identity = (fighter.Avatar.id + fighter.Avatar.displayName).ToLowerInvariant();
            poet = identity.Contains("droste"); scientist = identity.Contains("einstein");
            bool bach = identity.Contains("bach"), goethe=identity.Contains("goethe"), gutenberg=identity.Contains("gutenberg"), brahms=identity.Contains("brahms"), humboldt=identity.Contains("humboldt");
            theme=scientist||humboldt?2:poet||goethe||gutenberg?0:1;
            coat = poet ? new Color(.14f,.28f,.24f) : scientist ? new Color(.39f,.43f,.49f) : bach ? new Color(.20f,.15f,.30f) : new Color(.12f,.17f,.24f);
            if(goethe)coat=new Color(.20f,.37f,.48f); if(gutenberg)coat=new Color(.43f,.18f,.12f); if(brahms)coat=new Color(.28f,.23f,.18f); if(humboldt)coat=new Color(.25f,.36f,.19f);
            Color skin = new Color(.90f,.71f,.55f), shoes = new Color(.085f,.075f,.065f), cream = new Color(.94f,.89f,.74f), hair = scientist || bach ? new Color(.91f,.91f,.86f) : new Color(.19f,.12f,.085f);
            transform.position = new Vector3((float)fighter.X, 0, (float)fighter.Z);
            previousPosition = targetPosition = transform.position;
            var initialAim=new Vector3((float)fighter.AimX,0,(float)fighter.AimZ);
            transform.rotation=Quaternion.LookRotation(initialAim.sqrMagnitude>.001f?initialAim:(fighter.Index==0?Vector3.right:Vector3.left));
            body = Pivot("Articulated historical avatar", transform, Vector3.zero);
            for (int side=-1; side<=1; side+=2)
            {
                Transform hip=Pivot(side<0?"Left hip":"Right hip",body,new Vector3(side*.20f,1.07f,0));
                Limb("Trouser thigh",hip,new Vector3(0,-.30f,0),.60f,.24f,coat*.72f);
                Transform knee=Pivot("Knee joint",hip,new Vector3(0,-.60f,0));
                Limb("Stocking / lower leg",knee,new Vector3(0,-.20f,0),.48f,.18f,bach?cream:coat*.67f);
                Ball("Leather shoe",knee,new Vector3(0,-.48f,.1f),new Vector3(.25f,.15f,.43f),shoes);
                if(bach) Box("Shoe buckle",knee,new Vector3(0,-.36f,.22f),new Vector3(.12f,.06f,.04f),new Color(.85f,.66f,.32f));
                if(side<0){leftHip=hip;leftKnee=knee;}else{rightHip=hip;rightKnee=knee;}
            }
            coatRenderer=Part("Tailored coat bodice",PrimitiveType.Capsule,body,new Vector3(0,1.47f,0),new Vector3(.73f,.41f,.46f),coat).GetComponent<Renderer>();
            if(poet)
            {
                ArenaView.Frustum("Victorian bell skirt",body,new Vector3(0,.48f,0),.52f,.25f,.82f,coat,24);
                ArenaView.Frustum("Dress hem",body,new Vector3(0,.47f,0),.535f,.525f,.055f,cream,24);
                Box("High lace collar",body,new Vector3(0,1.79f,.04f),new Vector3(.40f,.14f,.34f),cream);
                Ball("Brooch",body,new Vector3(0,1.69f,.25f),new Vector3(.09f,.12f,.055f),new Color(.82f,.62f,.29f));
            }
            else
            {
                Box("Ivory shirt front",body,new Vector3(0,1.55f,.227f),new Vector3(.22f,.43f,.025f),cream);
                for(int side=-1;side<=1;side+=2)
                {
                    var lapel=Box("Turned lapel",body,new Vector3(side*.19f,1.62f,.255f),new Vector3(.15f,.39f,.055f),coat*1.38f);lapel.localRotation=Quaternion.Euler(0,0,side*20);
                    Box("Coat tail",body,new Vector3(side*.24f,1.03f,-.13f),new Vector3(.25f,.55f,.26f),coat);
                }
                Box(scientist?"Black tie":"Cravat",body,new Vector3(0,1.62f,.27f),new Vector3(.08f,.25f,.065f),scientist?shoes:cream);
                for(int i=0;i<3;i++) Ball("Brass waistcoat button",body,new Vector3(.10f,1.42f-i*.12f,.25f),Vector3.one*.045f,bach?new Color(.89f,.68f,.33f):cream);
            }
            if(identity.Contains("beethoven"))
            {
                Limb("Beethoven crimson scarf",body,new Vector3(0,1.78f,.06f),.17f,.44f,new Color(.66f,.13f,.13f));
                var scarf=Box("Trailing red scarf",body,new Vector3(.18f,1.51f,.30f),new Vector3(.16f,.51f,.06f),new Color(.77f,.17f,.14f));scarf.localRotation=Quaternion.Euler(0,0,-15);
            }
            Limb("Neck",body,new Vector3(0,1.90f,0),.22f,.21f,skin);
            head=Pivot("Head and face",body,new Vector3(0,2.12f,0));
            Ball("Sculpted face",head,Vector3.zero,new Vector3(.46f,.55f,.42f),skin);
            Ball("Nose",head,new Vector3(0,-.025f,.225f),new Vector3(.10f,.13f,.13f),skin*.95f);
            for(int side=-1;side<=1;side+=2)
            {
                Ball("Ear",head,new Vector3(side*.23f,-.02f,0),new Vector3(.09f,.15f,.10f),skin);
                Ball("Eye white",head,new Vector3(side*.095f,.05f,.195f),new Vector3(.09f,.054f,.055f),cream);
                Ball("Expressive pupil",head,new Vector3(side*.095f,.05f,.222f),Vector3.one*.028f,shoes);
                var brow=Box("Eyebrow",head,new Vector3(side*.093f,.107f,.207f),new Vector3(.115f,.031f,.033f),hair*.65f);brow.localRotation=Quaternion.Euler(0,0,-side*9);
            }
            Box("Mouth",head,new Vector3(0,-.136f,.196f),new Vector3(.13f,.019f,.027f),new Color(.49f,.25f,.20f));
            if(poet)
            {
                Ball("Parted brown hair",head,new Vector3(0,.18f,-.02f),new Vector3(.48f,.26f,.42f),hair);
                Ball("Braided bun",head,new Vector3(0,.045f,-.255f),new Vector3(.28f,.29f,.24f),hair);
                for(int side=-1;side<=1;side+=2) Ball("Side curl",head,new Vector3(side*.205f,.08f,-.015f),new Vector3(.13f,.29f,.22f),hair);
            }
            else if(bach)
            {
                Ball("Powdered wig crown",head,new Vector3(0,.20f,-.03f),new Vector3(.53f,.25f,.48f),hair);
                for(int side=-1;side<=1;side+=2)for(int j=0;j<4;j++) Ball("Rolled wig curl",head,new Vector3(side*.25f,.14f-j*.115f,-.035f),new Vector3(.16f,.135f,.34f),hair*(1-j*.035f));
                Box("Wig ribbon",head,new Vector3(0,-.21f,-.24f),new Vector3(.26f,.08f,.09f),shoes);
            }
            else if(goethe||gutenberg||brahms||humboldt)
            {
                Ball("Swept historical hair",head,new Vector3(0,.18f,-.07f),new Vector3(.49f,.26f,.43f),brahms?cream:hair);
                if(brahms||gutenberg) Ball("Long sculpted beard",head,new Vector3(0,-.26f,.06f),new Vector3(.34f,brahms?.48f:.36f,.28f),brahms?cream:hair);
                if(gutenberg){Part("Printer cap",PrimitiveType.Cylinder,head,new Vector3(0,.30f,0),new Vector3(.61f,.08f,.53f),coat);Ball("Cap crown",head,new Vector3(0,.34f,-.06f),new Vector3(.50f,.23f,.45f),coat);}
                if(humboldt){Part("Explorer hat brim",PrimitiveType.Cylinder,head,new Vector3(0,.27f,0),new Vector3(.72f,.025f,.65f),cream);Part("Explorer hat crown",PrimitiveType.Cylinder,head,new Vector3(0,.35f,0),new Vector3(.43f,.08f,.4f),cream);}
            }
            else
            {
                // Each lock has a deliberately arranged silhouette, no procedural randomness.
                for(int i=0;i<11;i++)
                {
                    float a=(i/10f*250+145)*Mathf.Deg2Rad;
                    var lockT=Ball("Wild swept hair lock",head,new Vector3(Mathf.Sin(a)*.25f,.13f+Mathf.Cos(a)*.11f,-.07f),new Vector3(.20f,scientist?.32f:.29f,.30f),hair*(.88f+(i%3)*.06f));
                    lockT.localRotation=Quaternion.Euler(0,0,-Mathf.Sin(a)*55);
                }
                if(scientist)for(int side=-1;side<=1;side+=2)
                {
                    var moustache=Ball("Einstein white moustache",head,new Vector3(side*.057f,-.10f,.226f),new Vector3(.145f,.065f,.08f),hair);moustache.localRotation=Quaternion.Euler(0,0,side*-15);
                }
            }
            for(int side=-1;side<=1;side+=2)
            {
                Transform shoulder=Pivot(side<0?"Left shoulder":"Right shoulder",body,new Vector3(side*.41f,1.73f,0));
                Limb("Upper sleeve",shoulder,new Vector3(0,-.19f,0),.43f,.22f,coat);
                Transform elbow=Pivot("Elbow",shoulder,new Vector3(0,-.37f,0));
                Limb("Forearm sleeve",elbow,new Vector3(0,-.16f,0),.36f,.18f,coat);
                Limb("White cuff",elbow,new Vector3(0,-.29f,0),.09f,.19f,cream);
                Ball("Hand",elbow,new Vector3(0,-.37f,.01f),new Vector3(.16f,.19f,.15f),skin);
                if(side<0){leftArm=shoulder;leftElbow=elbow;}else{rightArm=shoulder;rightElbow=elbow;}
            }
            if(poet||goethe||gutenberg)
            {
                var book=Box("Droste poetry volume",leftElbow,new Vector3(0,-.37f,.10f),new Vector3(.34f,.12f,.43f),new Color(.32f,.12f,.08f));
                Box("Visible parchment pages",book,new Vector3(0,.57f,0),new Vector3(.88f,.17f,.89f),cream);
                var quill=Ball("Quill feather",rightElbow,new Vector3(.02f,-.24f,.19f),new Vector3(.085f,.42f,.045f),cream);quill.localRotation=Quaternion.Euler(35,0,-15);
            }
            else if(scientist||humboldt)
            {
                Ball("Scientific atom core",leftElbow,new Vector3(0,-.38f,.16f),Vector3.one*.18f,new Color(.4f,.9f,1));
                for(int i=0;i<3;i++) {var orbit=ArenaView.Ring("Atom orbital",leftElbow,new Vector3(0,-.38f,.16f),.21f,.016f,color);orbit.localRotation=Quaternion.Euler(35+i*55,i*60,0);}
            }
            else
            {
                var baton=Part("Conductor baton",PrimitiveType.Cylinder,rightElbow,new Vector3(0,-.33f,.27f),new Vector3(.035f,.34f,.035f),cream);baton.localRotation=Quaternion.Euler(75,0,0);
                if(bach) Box("Sheet music folio",leftElbow,new Vector3(0,-.39f,.13f),new Vector3(.31f,.08f,.39f),cream);
            }
            marker=ArenaView.Ring("Team position ring",transform,new Vector3(0,.04f,0),.74f,.04f,tint);
            var arrow=Box("Facing marker",transform,new Vector3(0,.045f,.84f),new Vector3(.19f,.025f,.30f),tint);arrow.localRotation=Quaternion.Euler(0,45,0);
            shield=Pivot("Protective shield arcs",transform,new Vector3(0,1.22f,0));
            for(int i=0;i<3;i++){var ring=ArenaView.Ring("Shield energy band",shield,Vector3.zero,1.05f,.026f,new Color(.31f,.81f,1));ring.localRotation=Quaternion.Euler(i*55,0,i*40);}
            shield.gameObject.SetActive(false);
            bodyRenderers=body.GetComponentsInChildren<Renderer>();
            CreateStatusArt();
            BuildMotion();
            chargeSphere=Ball("Charge focused in forward palm",transform,new Vector3(0,1.48f,.9f),Vector3.one*.1f,color);
            chargeLight=chargeSphere.gameObject.AddComponent<Light>();chargeLight.color=color;chargeLight.range=2.5f;chargeLight.intensity=0;
            chargeSphere.gameObject.SetActive(false);
            chargeGuide=Pivot("Charged shot aim warning",transform,Vector3.zero);

            chargeGuide.gameObject.SetActive(false);
        }

        Transform StatusRoot(string name,Vector3 position)
        { var root=Pivot(name,transform,position);root.gameObject.SetActive(false);return root; }
        void StatusBar(Transform parent,Vector3 position,Vector3 size,Color tint,float angle=0)
        {var bar=Box("Status symbol",parent,position,size,tint);bar.localRotation=Quaternion.Euler(0,angle,0);}
        void CreateStatusArt()
        {
            Color gold=new Color(1,.83f,.15f), cyan=new Color(.35f,.92f,1), purple=new Color(.86f,.4f,1);
            stunArt=StatusRoot("Stun orbit stars",new Vector3(0,2.65f,0));
            for(int i=0;i<3;i++)
            {
                float a=i*Mathf.PI*2/3;var star=Pivot("Stun star",stunArt,new Vector3(Mathf.Sin(a)*.48f,0,Mathf.Cos(a)*.48f));
                for(int j=0;j<3;j++)StatusBar(star,Vector3.zero,new Vector3(.31f,.065f,.065f),gold,j*60);
            }
            rootArt=StatusRoot("Root foot shackles",new Vector3(0,.13f,0));
            ArenaView.Ring("Root boundary",rootArt,Vector3.zero,.62f,.055f,purple);
            for(int i=0;i<6;i++){float a=i*Mathf.PI/3;var link=ArenaView.Ring("Root chain link",rootArt,new Vector3(Mathf.Sin(a)*.46f,.1f,Mathf.Cos(a)*.46f),.13f,.028f,purple);link.localRotation=Quaternion.Euler(70,i*60,0);}
            focusArt=StatusRoot("Focus hand sigils",new Vector3(0,1.35f,.55f));
            for(int side=-1;side<=1;side+=2){var glyph=Pivot("Focus diamond",focusArt,new Vector3(side*.45f,0,0));ArenaView.Ring("Focus halo",glyph,Vector3.zero,.17f,.023f,cyan);StatusBar(glyph,Vector3.zero,new Vector3(.18f,.035f,.04f),cyan);StatusBar(glyph,Vector3.zero,new Vector3(.18f,.035f,.04f),cyan,90);}
            buffArt=StatusRoot("Power upward chevrons",new Vector3(0,.6f,0));
            for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f;var arrow=Pivot("Power arrow",buffArt,new Vector3(Mathf.Sin(a)*.7f,0,Mathf.Cos(a)*.7f));var left=Box("Upstroke",arrow,new Vector3(-.07f,0,0),new Vector3(.055f,.28f,.055f),gold);left.localRotation=Quaternion.Euler(0,0,-35);var right=Box("Upstroke",arrow,new Vector3(.07f,0,0),new Vector3(.055f,.28f,.055f),gold);right.localRotation=Quaternion.Euler(0,0,35);}
            vulnerableArt=StatusRoot("Vulnerability broken armor",new Vector3(0,2.72f,0));
            for(int side=-1;side<=1;side+=2){var half=Box("Broken shield half",vulnerableArt,new Vector3(side*.14f,0,0),new Vector3(.16f,.30f,.07f),new Color(1,.3f,.3f));half.localRotation=Quaternion.Euler(0,0,side*20);}
            immunityArt=StatusRoot("Control immunity silver halo",new Vector3(0,2.51f,0));
            ArenaView.Ring("Control immunity",immunityArt,Vector3.zero,.38f,.019f,new Color(.85f,.95f,1));
            hasteArt=StatusRoot("Haste ankle wings",new Vector3(0,.32f,0));
            for(int side=-1;side<=1;side+=2)for(int i=0;i<3;i++){var wing=Box("Haste wing",hasteArt,new Vector3(side*(.32f+i*.09f),i*.06f,-.12f),new Vector3(.08f,.05f,.3f-i*.055f),new Color(.62f,1,.28f));wing.localRotation=Quaternion.Euler(0,-side*25,0);}
        }
        bool HasEffect(string type){foreach(var effect in Fighter.Effects)if(effect.Type==type&&effect.Remaining>0)return true;return false;}
        void RenderStatus(float dt)
        {
            bool stunned=!dead&&HasEffect("Stun");
            stunArt.gameObject.SetActive(stunned);stunArt.Rotate(0,dt*100,0);
            rootArt.gameObject.SetActive(!dead&&HasEffect("Root"));
            focusArt.gameObject.SetActive(!dead&&HasEffect("Focus"));
            buffArt.gameObject.SetActive(!dead&&HasEffect("Buff"));
            vulnerableArt.gameObject.SetActive(!dead&&HasEffect("Vulnerable"));
            immunityArt.gameObject.SetActive(!dead&&Fighter.ControlImmunityRemaining>0);
            hasteArt.gameObject.SetActive(!dead&&HasEffect("Haste"));
            if(stunned)
            {
                head.localRotation=Quaternion.Euler(24,0,Mathf.Sin(Time.time*4)*9);
                leftArm.localRotation=Quaternion.Euler(8,0,-8);rightArm.localRotation=Quaternion.Euler(8,0,8);
                leftElbow.localRotation=rightElbow.localRotation=Quaternion.Euler(-8,0,0);
                leftHip.localRotation=rightHip.localRotation=Quaternion.Euler(-18,0,0);
                leftKnee.localRotation=rightKnee.localRotation=Quaternion.Euler(35,0,0);
            }
        }

        void RenderGuide(bool visible)
        {
            var ability=Ability(Fighter.ChargingAbilityId??Fighter.CastingAbilityId);
            visible=visible&&ability!=null;chargeGuide.gameObject.SetActive(visible);
            if(!visible){guideAbility=null;return;}
            if(guideAbility!=ability.id)
            {
                for(int i=chargeGuide.childCount-1;i>=0;i--)Destroy(chargeGuide.GetChild(i).gameObject);
                Color warning=Color.Lerp(color,Color.white,.4f);
                float reach=(float)ability.range;
                if(ability.delivery=="Zone"||ability.delivery=="Self")
                    ArenaView.Ring("Exact skill target radius",chargeGuide,new Vector3(0,.08f,0),Mathf.Max(.5f,(float)ability.areaOfEffect),.035f,warning);
                else if(ability.delivery=="Melee")
                {
                    float cone=(float)ability.coneDegrees;
                    for(int i=0;i<=20;i++)
                    {
                        float a=(-cone*.5f+cone*i/20)*Mathf.Deg2Rad;
                        var segment=Box("Melee reach arc",chargeGuide,new Vector3(Mathf.Sin(a)*reach,.08f,Mathf.Cos(a)*reach),new Vector3(.035f,.025f,Mathf.Max(.06f,reach*cone*Mathf.Deg2Rad/20)),warning);
                        segment.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg+90,0);
                    }
                    for(int side=-1;side<=1;side+=2)
                    {
                        float a=side*cone*.5f*Mathf.Deg2Rad;
                        var edge=Box("Melee cone side",chargeGuide,new Vector3(Mathf.Sin(a)*reach*.5f,.07f,Mathf.Cos(a)*reach*.5f),new Vector3(.025f,.025f,reach),warning);edge.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);
                    }
                }
                else
                {
                    if(ability.delivery=="Dash")reach=Mathf.Min(reach,(float)ability.dashDistance);
                    int count=Mathf.Max(1,Mathf.CeilToInt(reach/.8f));
                    for(int i=0;i<count;i++)Box("Skill trajectory warning",chargeGuide,new Vector3(0,.07f,reach*(i+.5f)/count),new Vector3(.06f,.025f,reach/count*.55f),warning);
                }
                guideReach=reach;guideAbility=ability.id;
            }
            chargeGuide.localScale=Vector3.one;
            if(ability.delivery=="Dash"||ability.delivery=="Projectile")
            {
                float limit=guideReach;
                if(ability.delivery=="Dash")limit=Mathf.Min(limit,Mathf.Max(0,Vector3.Distance(new Vector3((float)Fighter.X,0,(float)Fighter.Z),enemyPosition)-(float)gameData.balance.minimumFighterDistance));
                if(simulation!=null)
                {
                    float padding=ability.delivery=="Dash"?(float)gameData.balance.fighterHitRadius:(float)(ability.projectileRadius>0?ability.projectileRadius:gameData.balance.projectileRadius)*(ability.isUltimate?1.8f:1);
                    foreach(var obstacle in simulation.Obstacles)
                    {
                        Vector2 delta=new Vector2((float)(obstacle.X-Fighter.X),(float)(obstacle.Z-Fighter.Z));
                        float along=Vector2.Dot(delta,new Vector2((float)Fighter.AimX,(float)Fighter.AimZ));
                        float radius=(float)obstacle.Radius+padding,sideSquared=delta.sqrMagnitude-along*along;
                        if(sideSquared<=radius*radius&&along>=0)limit=Mathf.Min(limit,Mathf.Max(0,along-Mathf.Sqrt(Mathf.Max(0,radius*radius-sideSquared))));
                    }
                    float half=(float)gameData.balance.arenaHalfSize;
                    if(Mathf.Abs((float)Fighter.AimX)>.001f)limit=Mathf.Min(limit,((Fighter.AimX>0?half:-half)-(float)Fighter.X)/(float)Fighter.AimX);
                    if(Mathf.Abs((float)Fighter.AimZ)>.001f)limit=Mathf.Min(limit,((Fighter.AimZ>0?half:-half)-(float)Fighter.Z)/(float)Fighter.AimZ);
                }
                chargeGuide.localScale=new Vector3(1,1,Mathf.Max(0,limit)/Mathf.Max(.01f,guideReach));
            }
            chargeGuide.position=ability.delivery=="Zone"?new Vector3((float)Fighter.CastTargetX,0,(float)Fighter.CastTargetZ):new Vector3((float)Fighter.X,0,(float)Fighter.Z);
            var aim=new Vector3((float)Fighter.AimX,0,(float)Fighter.AimZ);chargeGuide.rotation=aim.sqrMagnitude>.001f?Quaternion.LookRotation(aim):Quaternion.identity;
        }

        public void CaptureTick()
        {
            previousPosition = targetPosition;
            targetPosition = new Vector3((float)Fighter.X, 0, (float)Fighter.Z);
        }
        public void Render(float interpolation, Vector3 opponent)
        {
            transform.position=Vector3.Lerp(previousPosition,targetPosition,interpolation); enemyPosition=opponent;
            if(dead){Collapse();return;}
            Vector3 direction=opponent-transform.position;direction.y=0;
            if(Fighter.IsCharging||Fighter.CastingAbilityId!=null){var aim=new Vector3((float)Fighter.AimX,0,(float)Fighter.AimZ);if(aim.sqrMagnitude>.001f)direction=aim;}
            if(direction.sqrMagnitude>.01f&&!dead){transform.rotation=facingInitialized?Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction),Time.deltaTime*10):Quaternion.LookRotation(direction);facingInitialized=true;}
            float dt=Time.deltaTime;
            attackPulse=Mathf.Max(0,attackPulse-dt*2.8f);hitPulse=Mathf.Max(0,hitPulse-dt*5);dodgePulse=Mathf.Max(0,dodgePulse-dt*2.4f);
            float speed=new Vector2((float)Fighter.VelocityX,(float)Fighter.VelocityZ).magnitude;
            bool moving=speed>.08f&&!dead;float stride=Mathf.Clamp01(speed/3.0f);
            float step=Mathf.Sin(gait)*stride;
            Vector3 localVelocity=transform.InverseTransformDirection(new Vector3((float)Fighter.VelocityX,0,(float)Fighter.VelocityZ));
            float backwards=localVelocity.z<-.15f?-1:1;
            float cast=Fighter.CastRemaining>0?1:0;
            float charge=Mathf.Clamp01((float)Fighter.Charge);
            bool charging=Fighter.IsCharging&&!dead;
            int slot=System.Array.IndexOf(Fighter.Avatar.abilityIds,activeAbility);
            // Wind-up folds the elbow and draws the casting hand back; release extends it forward.
            float windup=cast>0?Mathf.SmoothStep(.18f,1,1-Mathf.Clamp01((float)Fighter.CastRemaining/Mathf.Max(.01f,castDuration))):0;
            float anticipation=Mathf.Max(windup,charging?1:0);
            leftArm.localRotation=Quaternion.Euler(-step*26-anticipation*(theme==0?65:35)-attackPulse*60,anticipation*-15,-12-anticipation*15);
            rightArm.localRotation=Quaternion.Euler(step*28-anticipation*(slot==1?120:45)-attackPulse*75,anticipation*-25,12+anticipation*25-attackPulse*12);
            leftElbow.localRotation=Quaternion.Euler(-25-anticipation*55+attackPulse*15,0,0);
            rightElbow.localRotation=Quaternion.Euler(-20-anticipation*85+attackPulse*15,0,0);
            // Distinct silhouettes communicate how to counter each delivery before it resolves.
            if(!charging&&(anticipation>0||attackPulse>0))
            {
                if(delivery=="Melee")
                {
                    rightArm.localRotation=Quaternion.Euler(-55,-95*anticipation+100*attackPulse,60);
                    rightElbow.localRotation=Quaternion.Euler(-25-35*anticipation,0,0);
                    leftArm.localRotation=Quaternion.Euler(-45,15,-30);
                }
                else if(delivery=="Dash")
                {
                    rightArm.localRotation=Quaternion.Euler(-15-75*attackPulse,-10,15);
                    leftArm.localRotation=Quaternion.Euler(-55,20,-15);
                    leftKnee.localRotation=Quaternion.Euler(35*anticipation,0,0);
                    rightKnee.localRotation=Quaternion.Euler(25*anticipation,0,0);
                }
                else if(delivery=="Zone")
                {
                    leftArm.localRotation=Quaternion.Euler(-145*anticipation-35*attackPulse,-25,-40);
                    rightArm.localRotation=Quaternion.Euler(-145*anticipation-35*attackPulse,25,40);
                    leftElbow.localRotation=rightElbow.localRotation=Quaternion.Euler(-15,0,0);
                }
                else if(delivery=="Self")
                {
                    leftArm.localRotation=Quaternion.Euler(-40,35,-15);
                    rightArm.localRotation=Quaternion.Euler(-40,-35,15);
                    leftElbow.localRotation=rightElbow.localRotation=Quaternion.Euler(-75,0,0);
                }
            }
            if(charging)
            {
                leftArm.localRotation=Quaternion.Euler(-50,20,-12);rightArm.localRotation=Quaternion.Euler(-50,-20,12);leftElbow.localRotation=rightElbow.localRotation=Quaternion.Euler(-35,0,0);
                if(delivery=="Zone"){leftArm.localRotation=Quaternion.Euler(-145,-20,-30);rightArm.localRotation=Quaternion.Euler(-145,20,30);leftElbow.localRotation=rightElbow.localRotation=Quaternion.Euler(-15,0,0);}
                else if(delivery=="Melee"){rightArm.localRotation=Quaternion.Euler(-65,-65,65);rightElbow.localRotation=Quaternion.Euler(-60,0,0);}
                else if(delivery=="Self"){leftArm.localRotation=Quaternion.Euler(-35,40,-15);rightArm.localRotation=Quaternion.Euler(-35,-40,15);leftElbow.localRotation=rightElbow.localRotation=Quaternion.Euler(-80,0,0);}
            }
            head.localRotation=Quaternion.Euler(-cast*8+hitPulse*12,Mathf.Sin(gait*.5f)*stride*2,hitPulse*12);
            RenderStatus(dt);
            bool stunned=!dead&&HasEffect("Stun");
            float dodge=Fighter.DodgeRemaining>0?1:dodgePulse;
            Vector3 pose=new Vector3(0,moving?Mathf.Abs(Mathf.Cos(gait))*.022f*stride:Mathf.Sin(Time.time*2.3f)*.008f,attackPulse*.1f);
            if(dead)pose=new Vector3(0,.16f,-.55f);else pose.y-=dodge*.28f;
            Vector3 bodyTarget=stunned?new Vector3(0,-.18f,0):pose;
            Quaternion rotationTarget=stunned?Quaternion.Euler(17,0,4):Quaternion.Euler(dead?82:stride*backwards*8-attackPulse*13+hitPulse*14,0,dead?-16:-localVelocity.x*3-dodge*26);
            float bodyBlend=1-Mathf.Exp(-14*dt),rotationBlend=1-Mathf.Exp(-10*dt);
            body.localPosition=Vector3.Lerp(body.localPosition,bodyTarget,bodyBlend);
            body.localRotation=Quaternion.Slerp(body.localRotation,rotationTarget,rotationBlend);
            WeightedMotion(dt,speed,localVelocity);
            coatRenderer.sharedMaterial.color=Color.Lerp(coat,Color.white,hitPulse*.75f);
            chargeSphere.gameObject.SetActive(charging);RenderGuide(!dead&&(charging||Fighter.CastRemaining>0));
            if(charging){chargeSphere.localPosition=delivery=="Zone"?new Vector3(0,2.3f,.5f):delivery=="Self"?new Vector3(0,1.45f,.35f):new Vector3(0,1.48f,.9f);chargeSphere.localScale=Vector3.one*(.20f+charge*.62f);chargeLight.intensity=.25f+charge*.85f;}
            foreach(var renderer in bodyRenderers){var material=renderer.sharedMaterial;material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",charging?color*(.035f+charge*.35f):!dead&&HasEffect("Buff")?new Color(.10f,.07f,.015f):!dead&&HasEffect("Focus")?new Color(.015f,.055f,.085f):Color.black);}
            var chargeMaterial=chargeSphere.GetComponent<Renderer>().sharedMaterial;chargeMaterial.EnableKeyword("_EMISSION");chargeMaterial.SetColor("_EmissionColor",color*(1+charge*2));
            marker.gameObject.SetActive(!dead);
            shield.gameObject.SetActive(Fighter.Shield>0&&!dead);shield.Rotate(0,dt*60,dt*20,Space.Self);
            if(animator!=null&&animator.runtimeAnimatorController!=null){SetAnimatorBool("Moving",moving);SetAnimatorBool("Dead",dead);}
        }
        void SetAnimatorBool(string name,bool value)
        {foreach(var p in animator.parameters)if(p.name==name&&p.type==AnimatorControllerParameterType.Bool)animator.SetBool(name,value);}
        void Trigger(string name)
        {if(animator==null||animator.runtimeAnimatorController==null)return;foreach(var p in animator.parameters)if(p.name==name&&p.type==AnimatorControllerParameterType.Trigger)animator.SetTrigger(name);}
        public void OnBattleEvent(BattleEvent e)
        {
            bool own=e.Source==Fighter.Index, target=e.Target==Fighter.Index;
            if(own&&(e.Type=="Attack"||e.Type=="Ability"||e.Type=="ChargeStart")){activeAbility=e.AbilityId;if(!string.IsNullOrEmpty(e.Delivery))delivery=e.Delivery;castDuration=Ability(e.AbilityId)!=null?Mathf.Max(.05f,(float)Ability(e.AbilityId).castTime):Mathf.Max(.05f,(float)Fighter.CastRemaining);Trigger(e.Type);}
            if(own&&e.Type=="MeleeStrike")
            {
                attackPulse=1;delivery="Melee";
                CombatFx.Melee(new Vector3((float)Fighter.X,1.15f,(float)Fighter.Z),new Vector3((float)Fighter.AimX,0,(float)Fighter.AimZ),color,Mathf.Max(.8f,(float)e.Radius),Ability(e.AbilityId)!=null?(float)Ability(e.AbilityId).coneDegrees:120);
            }
            if(own&&e.Type=="DashStrike")
            {
                attackPulse=1;delivery="Dash";
                Vector3 end=new Vector3((float)Fighter.X,.8f,(float)Fighter.Z),delta=end-new Vector3(previousPosition.x,.8f,previousPosition.z);
                CombatFx.DashTrail(end,delta,color,delta.magnitude);
            }
            if(own&&e.Type=="ZoneCreated"){attackPulse=1;delivery="Zone";}
            if(own&&(e.Type=="ProjectileLaunch"||e.Type=="ChargeRelease")){attackPulse=1;}
            if(own&&target&&e.Type=="Impact")
            {
                attackPulse=1;
                var ability=Ability(e.AbilityId);
                if(ability!=null&&ability.isUltimate)CombatFx.Empower(transform.position,color,(e.AbilityId??"").Contains("gutenberg"));
                else CombatFx.Ability(transform.position+Vector3.up*1.3f,transform.position+Vector3.up*1.3f,color,theme,false);
            }
            if(target&&e.Type=="Hit"&&e.Amount>0){hitPulse=1;Trigger("Hit");CombatFx.Hit(transform.position+Vector3.up*1.15f,new Color(1,.73f,.28f));}
            if(target&&e.Type=="Heal")CombatFx.Heal(transform.position);
            if(own&&e.Type=="Dodge"){dodgePulse=1;CombatFx.Dust(transform.position,color,true);}
            if(target&&e.Type=="Shield")CombatFx.Ability(transform.position,transform.position,new Color(.3f,.8f,1),2,false);
            if(!dead&&Fighter.Health<=0){dead=true;Trigger("Death");CombatFx.Death(transform.position,color);}
        }
    }
}

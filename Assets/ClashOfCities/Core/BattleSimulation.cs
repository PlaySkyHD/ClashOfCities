using System;
using System.Collections.Generic;
namespace ClashOfCities.Core
{
    public sealed class BattleSimulation
    {
        readonly BalanceConfig balance;
        readonly Dictionary<string,AbilityDefinition> abilities=new Dictionary<string,AbilityDefinition>(StringComparer.Ordinal);
        readonly SeededRandom random;
        long ticks; int nextProjectileId, nextAreaId;
        readonly double[] queuedUntil=new double[2];
        readonly int[] queuedSkill={-1,-1};
        readonly double[] oldX=new double[2],oldZ=new double[2];
        readonly double[] routeUntil=new double[2],routeX=new double[2],routeZ=new double[2];
        // Tactical timings stay in real seconds when the simulation frequency changes.
        int TacticalTick { get { return (int)Math.Floor(Elapsed/.05+1e-7); } }
        public readonly List<ArenaObstacle> Obstacles;
        public readonly List<TerrainZone> TerrainZones;
        public readonly List<AreaState> Areas=new List<AreaState>();
        public double SafeRadius { get {double t=Math.Max(0,Math.Min(1,(Elapsed-balance.ringStartSeconds)/(balance.ringEndSeconds-balance.ringStartSeconds)));return balance.arenaHalfSize*1.42+(Math.Min(balance.ringFinalRadius,balance.arenaHalfSize)-balance.arenaHalfSize*1.42)*t;} }
        public readonly List<ProjectileState> Projectiles=new List<ProjectileState>();
        readonly InputFrame[] replayInputs;
        readonly List<InputFrame> recordedInputs=new List<InputFrame>();
        public bool IsReplay { get { return replayInputs!=null; } }
        public int Tick { get { return (int)ticks; } }
        public bool IsHuman(int index) { return Config.mode==MatchMode.PlayerVsPlayer || (Config.mode==MatchMode.PlayerVsCpu && index==0); }
        public FighterState[] Fighters { get; private set; }
        public MatchConfig Config { get; private set; }
        public ClimateEventDefinition Environment { get; private set; }
        public double Elapsed { get { return ticks*balance.tickSeconds; } }
        public bool IsFinished { get; private set; }
        public MatchResult Result { get; private set; }
        public event Action<BattleEvent> Event;
        public BattleSimulation(GameData data,MatchConfig config,InputFrame[] replayInputs=null)
        {
            DataValidator.Validate(data); if(config==null) throw new ArgumentNullException(nameof(config));
            balance=DataValidator.Copy(data.balance); Config=DataValidator.Copy(config);
            if(!Enum.IsDefined(typeof(CpuDifficulty),Config.difficulty))throw new ArgumentException("Unknown difficulty.");
            if(!Enum.IsDefined(typeof(MatchMode),Config.mode)) throw new ArgumentException("Unknown match mode.");
            if(replayInputs!=null && replayInputs.Length>0)
            {
                if(Config.mode==MatchMode.CpuVsCpu) throw new ArgumentException("CPU matches do not require player input recordings.");
                if(replayInputs.Length>Math.Ceiling(balance.maxDuration/balance.tickSeconds)+1) throw new ArgumentException("Replay exceeds match duration.");
                this.replayInputs=new InputFrame[replayInputs.Length];
                for(int i=0;i<replayInputs.Length;i++)
                {
                    if(replayInputs[i]==null || replayInputs[i].tick!=i+1) throw new ArgumentException("Replay input ticks must be contiguous, starting at 1.");
                    this.replayInputs[i]=new InputFrame {tick=i+1,a=Normalize(replayInputs[i].a),b=Normalize(replayInputs[i].b)};
                }
            }
            if(!string.IsNullOrEmpty(Config.balanceVersion)&&Config.balanceVersion!=balance.version) throw new ArgumentException("Balance version mismatch.");
            Config.balanceVersion=balance.version; random=new SeededRandom(unchecked((uint)Config.seed));
            var environment=(string.IsNullOrEmpty(Config.climateEventId)||Config.climateEventId=="random")?data.environments[new SeededRandom(unchecked((uint)Config.seed)).NextInt(data.environments.Length)]:Array.Find(data.environments,e=>e.id==Config.climateEventId);
            if(environment==null) throw new ArgumentException("Unknown climate event."); Environment=DataValidator.Copy(environment); Config.climateEventId=Environment.id;
            foreach(var a in data.abilities) abilities.Add(a.id,DataValidator.Copy(a));
            Obstacles=ArenaLayout.Create(Config.seed,balance.arenaHalfSize);
            TerrainZones=ArenaLayout.CreateZones(Config.seed,balance.arenaHalfSize,Environment.id);
            Fighters=new[]{Create(data,Config.cityAId,Config.avatarAId,0),Create(data,Config.cityBId,Config.avatarBId,1)};
        }
        FighterState Create(GameData data,string cityId,string avatarId,int index)
        {
            var city=Array.Find(data.cities,c=>c.id==cityId); var avatar=Array.Find(data.avatars,a=>a.id==avatarId);
            if(city==null||avatar==null||avatar.cityId!=city.id) throw new ArgumentException("Unknown city/avatar or avatar does not belong to city.");
            var stats=ClimateCalculator.Map(city,Environment,balance);
            var f=new FighterState { Index=index, City=DataValidator.Copy(city), Avatar=DataValidator.Copy(avatar),Stats=stats,Health=stats.MaxHealth,Energy=stats.Energy,X=(index==0?-1:1)*Math.Min(6,balance.arenaHalfSize),State=CombatState.Idle };
            foreach(var id in avatar.abilityIds) { f.Cooldowns.Add(id,0); f.AbilityUsage.Add(id,0); } return f;
        }
        void Emit(string type,int source,int target,double amount=0,string ability=null) { var handler=Event; if(handler!=null) handler(new BattleEvent{Type=type,Source=source,Target=target,Amount=amount,AbilityId=ability,X=source>=0?Fighters[source].X:0,Z=source>=0?Fighters[source].Z:0,DirectionX=source>=0?Fighters[source].AimX:0,DirectionZ=source>=0?Fighters[source].AimZ:0,Radius=ability!=null?abilities[ability].areaOfEffect:2.4,Delivery=ability!=null?abilities[ability].delivery:(source>=0?Fighters[source].Avatar.attackStyle:null)}); }
        public void Step() { Step(default(FighterInput),default(FighterInput)); }
        public void Step(FighterInput a,FighterInput b)
        {
            if(IsFinished) return;
            if(IsReplay)
            {
                if(ticks>=replayInputs.Length) throw new InvalidOperationException("Replay ended before the match finished.");
                a=replayInputs[ticks].a; b=replayInputs[ticks].b;
            }
            a=IsHuman(0)?Normalize(a):default(FighterInput);
            b=IsHuman(1)?Normalize(b):default(FighterInput);
            ticks++; double dt=balance.tickSeconds;
            if(Config.mode!=MatchMode.CpuVsCpu) recordedInputs.Add(new InputFrame {tick=Tick,a=a,b=b});
            for(int i=0;i<2;i++){oldX[i]=Fighters[i].X;oldZ[i]=Fighters[i].Z;}
            for(int i=0;i<2;i++) Update(Fighters[i],dt);
            if(!CheckFinished())
            {
                int first=(int)(ticks%2);
                Act(Fighters[first],Fighters[1-first],dt,first==0?a:b);
                if(!CheckFinished()) { Act(Fighters[1-first],Fighters[first],dt,first==0?b:a); CheckFinished(); }
            }
            SeparateFighters();
            if(!IsFinished) { AdvanceProjectiles(dt); if(!CheckFinished()) { AdvanceAreas(dt); CheckFinished(); } }
            for(int i=0;i<2;i++) { Fighters[i].VelocityX=(Fighters[i].X-oldX[i])/dt; Fighters[i].VelocityZ=(Fighters[i].Z-oldZ[i])/dt; }
            if(IsFinished && IsReplay && ticks!=replayInputs.Length) throw new InvalidOperationException("Replay contains trailing input after match end.");
        }
        static FighterInput Normalize(FighterInput input)
        {
            if(double.IsNaN(input.moveX)||double.IsInfinity(input.moveX)||double.IsNaN(input.moveZ)||double.IsInfinity(input.moveZ)) throw new ArgumentException("Movement must be finite.");
            input.moveX=Math.Max(-1,Math.Min(1,input.moveX)); input.moveZ=Math.Max(-1,Math.Min(1,input.moveZ));
            double length=Math.Sqrt(input.moveX*input.moveX+input.moveZ*input.moveZ);
            if(length>1+1e-12) {input.moveX/=length;input.moveZ/=length;} return input;
        }
        void SeparateFighters()
        {
            var a=Fighters[0];var b=Fighters[1]; if(a.Health<=0||b.Health<=0)return;
            double dx=b.X-a.X,dz=b.Z-a.Z,d=Math.Sqrt(dx*dx+dz*dz);
            if(d>=balance.minimumFighterDistance)return;
            if(d<1e-9) {dx=1;dz=0;d=1;} else {dx/=d;dz/=d;}
            double push=(balance.minimumFighterDistance-Distance(a,b))*.5;
            Translate(a,-dx*push,-dz*push);Translate(b,dx*push,dz*push);
        }
        void Update(FighterState f,double dt)
        {
            if(f.Health<=0)return;
            foreach(var id in f.Avatar.abilityIds) f.Cooldowns[id]=Math.Max(0,f.Cooldowns[id]-dt);
            f.ControlImmunityRemaining=Math.Max(0,f.ControlImmunityRemaining-dt);
            f.RecoveryRemaining=Math.Max(0,f.RecoveryRemaining-dt);
            f.BasicAttackCooldown=Math.Max(0,f.BasicAttackCooldown-dt);
            f.DodgeCooldown=Math.Max(0,f.DodgeCooldown-dt);
            f.DodgeRemaining=Math.Max(0,f.DodgeRemaining-dt);
            f.TacticalRetreatRemaining=Math.Max(0,f.TacticalRetreatRemaining-dt);
            for(int i=f.Effects.Count-1;i>=0;i--) { var s=f.Effects[i]; double active=Math.Min(dt,s.Remaining); if(s.Type=="DamageOverTime") Hurt(Fighters[s.Source],f,s.Magnitude*active,false,null); s.Remaining-=dt; if(s.Remaining<=0)f.Effects.RemoveAt(i); }
            if(f.Health<=0)return;
            ApplyTerrain(f,dt);
            if(f.Health<=0)return;
            if(Environment.heatDamagePerSecond>0) Hurt(null,f,Environment.heatDamagePerSecond*(1-f.Stats.HeatResistance)*dt,false,null);
            if(f.Health<=0)return;
            if(Math.Sqrt(f.X*f.X+f.Z*f.Z)>SafeRadius) Hurt(null,f,(balance.ringDamagePerSecond+Math.Max(0,Elapsed-balance.ringStartSeconds)*.12)*dt,false,null);
            if(f.Health<=0)return;
            f.Energy=Math.Min(f.Stats.Energy,f.Energy+f.Stats.EnergyRegeneration*dt);
            // Passive regeneration is counted in total healing but does not emit per-tick VFX.
            double heal=Math.Min(f.Stats.HealthRegeneration*dt,f.Stats.MaxHealth-f.Health); f.Health+=heal; f.HealingDone+=heal;
        }
        static double Distance(FighterState a,FighterState b) { double x=a.X-b.X,z=a.Z-b.Z;return Math.Sqrt(x*x+z*z); }
        static double Modifier(FighterState f,string type) { double sum=0;foreach(var e in f.Effects) if(e.Type==type)sum+=e.Magnitude;return Math.Min(.65,sum); }
        double CpuReaction {get{return Config.difficulty==CpuDifficulty.Easy?.45:Config.difficulty==CpuDifficulty.Hard?.13:balance.cpuReactionSeconds;}}
        bool CpuDodges(int projectile,int side){return Config.difficulty==CpuDifficulty.Easy?(projectile+side)%4==0:Config.difficulty==CpuDifficulty.Hard?(projectile+side)%5!=0:(projectile+side)%3!=0;}
        void Act(FighterState f,FighterState enemy,double dt,FighterInput input)
        {
            if(f.Health<=0||enemy.Health<=0)return;
            bool human=IsHuman(f.Index);
            if(human)
            {
                // One pending action, never a backlog. A slightly early tap survives recovery.
                if(input.skill1||input.skill2){queuedSkill[f.Index]=input.skill2?1:0;queuedUntil[f.Index]=Elapsed+.20;}
                if(Elapsed>queuedUntil[f.Index])queuedSkill[f.Index]=-1;
            }
            if(HasStatus(f,"Stun")) {f.State=CombatState.Defend;return;}
            if(f.DodgeRemaining>0) { Move(f,f.DodgeX,f.DodgeZ,balance.dodgeSpeedMultiplier,dt,CombatState.Dodge);return; }
            if(f.CastingAbilityId!=null)
            {
                if(human&&input.dodge&&TryDodge(f,enemy,input.moveX,input.moveZ))
                {f.CastingAbilityId=null;f.CastRemaining=0;queuedSkill[f.Index]=-1;Move(f,f.DodgeX,f.DodgeZ,balance.dodgeSpeedMultiplier,dt,CombatState.Dodge);return;}
                if(human)Move(f,input.moveX,input.moveZ,.7,dt,CombatState.UseAbility);
                f.State=CombatState.UseAbility;f.CastRemaining-=dt;
                if(f.CastRemaining<=0)
                {
                    var ability=abilities[f.CastingAbilityId];f.CastingAbilityId=null;
                    Resolve(f,enemy,ability);f.RecoveryRemaining=Math.Min(balance.abilityRecoverySeconds,balance.basicAttackInterval);
                }
                return;
            }
            if(f.IsCharging)
            {
                Aim(f,enemy);
                bool held=human?input.ultimate:f.Charge<(HasStatus(enemy,"Stun")||HasStatus(enemy,"Root")?.48:.78);
                if(!human && f.DodgeCooldown<=0)
                    foreach(var p in Projectiles) {double rx=f.X-p.X,rz=f.Z-p.Z; if(p.Target==f.Index&&p.Age>CpuReaction&&rx*p.DirectionX+rz*p.DirectionZ>0&&rx*rx+rz*rz<10&&Math.Abs(rx*p.DirectionZ-rz*p.DirectionX)<.8&&(Config.difficulty==CpuDifficulty.Hard||p.Id%3==0)) {if(TryDodge(f,enemy,-p.DirectionZ,p.DirectionX))return;}}
                if(input.dodge&&TryDodge(f,enemy,input.moveX,input.moveZ))return;
                if(held) { f.ChargeElapsed+=dt; f.Charge=Math.Min(1,f.Charge+dt/(balance.chargeDuration*(1-Modifier(f,"Focus")))); f.State=CombatState.UseAbility; if(human) {Move(f,input.moveX,input.moveZ,.35,dt,CombatState.UseAbility); f.State=CombatState.UseAbility;} return; }
                ReleaseCharge(f,enemy);return;
            }
            double distance=Distance(f,enemy);
            if(human)
            {
                if(input.dodge && TryDodge(f,enemy,input.moveX,input.moveZ)) { Move(f,f.DodgeX,f.DodgeZ,balance.dodgeSpeedMultiplier,dt,CombatState.Dodge);return; }
                // Holding attack offers a short approach; explicit movement always wins.
                double mx=input.moveX,mz=input.moveZ;
                if(input.basicAttack&&mx*mx+mz*mz<.01&&distance>f.Avatar.attackRange*.9&&distance<f.Avatar.attackRange+3&&HasLineOfSight(f.Index,enemy.Index))
                {mx=(enemy.X-f.X)/distance;mz=(enemy.Z-f.Z)/distance;}
                Move(f,mx,mz,1,dt,f.RecoveryRemaining>0?CombatState.Recover:CombatState.MoveToTarget);
                if(f.RecoveryRemaining>1e-9)return;
                if(input.ultimate && StartCharge(f,enemy))return;
                int queued=queuedSkill[f.Index];
                if(queued>=0&&TryAbility(f,enemy,queued,false)){queuedSkill[f.Index]=-1;return;}
                if(input.basicAttack && f.BasicAttackCooldown<=1e-9 && Distance(f,enemy)<=f.Avatar.attackRange) BasicAttack(f,enemy);
                return;
            }
            if(TerrainZones.Exists(zone=>!IsTerrainBeneficial(zone.Kind)&&Math.Pow(f.X-zone.X,2)+Math.Pow(f.Z-zone.Z,2)<Math.Pow(zone.Radius+.5,2)) || Math.Sqrt(f.X*f.X+f.Z*f.Z)>SafeRadius-.6 || Areas.Exists(area=>area.Source!=f.Index&&(f.X-area.X)*(f.X-area.X)+(f.Z-area.Z)*(f.Z-area.Z)<Math.Pow(area.Radius+.6,2))) {MoveCpu(f,enemy,dt);return;}
            if(Config.difficulty==CpuDifficulty.Easy&&(TacticalTick+f.Index*3)%8!=0){MoveCpu(f,enemy,dt);return;}
            // React only after a projectile has been visible; dodge predicted paths, not every cast.
            foreach(var p in Projectiles)
            {
                if(p.Target!=f.Index||p.Age<CpuReaction||f.DodgeCooldown>0)continue;
                double rx=f.X-p.X,rz=f.Z-p.Z,along=rx*p.DirectionX+rz*p.DirectionZ;
                double across=Math.Abs(rx*p.DirectionZ-rz*p.DirectionX);
                if(along>0&&along<p.Speed*.55&&across<balance.fighterHitRadius+p.Radius+.25&&CpuDodges(p.Id,f.Index))
                    if(TryDodge(f,enemy,-p.DirectionZ,p.DirectionX)) {Move(f,f.DodgeX,f.DodgeZ,balance.dodgeSpeedMultiplier,dt,CombatState.Dodge);return;}
            }
            if(f.RecoveryRemaining<=1e-9)
            {
                // Prefer a control setup while the ultimate is ready, then convert that window.
                bool ready=Elapsed>=balance.ultimateUnlockSeconds&&f.Cooldowns[f.Avatar.abilityIds[2]]<=1e-9;
                if(Config.difficulty!=CpuDifficulty.Easy&&ready&&!HasStatus(enemy,"Stun")&&!HasStatus(enemy,"Root")&&enemy.ControlImmunityRemaining<=0)
                    for(int slot=0;slot<2;slot++){var setup=abilities[f.Avatar.abilityIds[slot]];if((setup.effectType=="Stun"||setup.effectType=="Root"||setup.onHitTargetEffect=="Stun"||setup.onHitTargetEffect=="Root")&&TryAbility(f,enemy,slot,true))return;}
                if((abilities[f.Avatar.abilityIds[2]].delivery=="Self"||HasLineOfSight(f.Index,enemy.Index))&&StartCharge(f,enemy))return;
                if(TryAbility(f,enemy,0,true)||TryAbility(f,enemy,1,true))return;
                if(f.BasicAttackCooldown<=1e-9&&distance<=f.Avatar.attackRange&&HasLineOfSight(f.Index,enemy.Index)) { BasicAttack(f,enemy);return; }
            }
            MoveCpu(f,enemy,dt);
        }
        bool TryAbility(FighterState f,FighterState enemy,int slot,bool automatic)
        {
            var a=abilities[f.Avatar.abilityIds[slot]];
            if(f.Cooldowns[a.id]>1e-9||f.Energy+1e-9<a.energyCost||(a.isUltimate&&Elapsed<balance.ultimateUnlockSeconds))return false;
            bool self=a.effectType=="Heal"||a.effectType=="Shield"||a.effectType=="Buff"||a.effectType=="Haste"||a.effectType=="Focus";
            if(a.effectType=="Heal"&&f.Health>=f.Stats.MaxHealth-1e-9)return false;
            if(automatic)
            {
                if(a.effectType=="Heal"&&f.Health>f.Stats.MaxHealth*balance.healThreshold)return false;
                if(a.effectType=="Shield"&&f.Shield>0)return false;
                if(a.effectType=="Buff"&&Modifier(f,"Buff")>0)return false;
            }
            if(!self&&Distance(f,enemy)>a.range)return false;
            if(automatic&&!self&&!HasLineOfSight(f.Index,enemy.Index))return false;
            Aim(f,enemy);
            f.Energy=Math.Max(0,f.Energy-a.energyCost);f.Cooldowns[a.id]=a.cooldown*f.Stats.CooldownModifier;
            f.AbilitiesUsed++;f.AbilityUsage[a.id]++;f.State=CombatState.UseAbility;
            Emit("Ability",f.Index,self?f.Index:enemy.Index,0,a.id);
            if(a.castTime>0) { f.CastRemaining=a.castTime*(1-Modifier(f,"Focus"));f.CastingAbilityId=a.id; }
            else { Resolve(f,enemy,a);f.RecoveryRemaining=Math.Min(balance.abilityRecoverySeconds,balance.basicAttackInterval); }
            return true;
        }
        void BasicAttack(FighterState f,FighterState enemy)
        {
            f.State=CombatState.BasicAttack;f.BasicAttacks++;Aim(f,enemy);Emit("Attack",f.Index,enemy.Index);
             if(f.Avatar.attackStyle=="Melee") Strike(f,enemy,null,f.Stats.AttackPower,f.Avatar.attackRange,120,"MeleeStrike");else Launch(f,enemy,null,f.Stats.AttackPower,f.Avatar.attackRange);
            f.Energy=Math.Min(f.Stats.Energy,f.Energy+balance.basicEnergyGain);f.BasicAttackCooldown=balance.basicAttackInterval;f.RecoveryRemaining=Math.Min(.16,balance.basicAttackInterval);
        }
        bool TryDodge(FighterState f,FighterState enemy,double x,double z)
        {
            if(HasStatus(f,"Root")||HasStatus(f,"Stun")||f.DodgeCooldown>1e-9||f.Energy<balance.dodgeEnergyCost)return false;
            CancelCharge(f);queuedSkill[f.Index]=-1;
            double length=Math.Sqrt(x*x+z*z);
            if(length<1e-9) {x=f.X-enemy.X;z=f.Z-enemy.Z;length=Math.Sqrt(x*x+z*z);}
            if(length<1e-9) {x=f.Index==0?-1:1;z=0;length=1;}
            f.DodgeX=x/length;f.DodgeZ=z/length;f.DodgeRemaining=balance.dodgeDuration;f.DodgeCooldown=balance.dodgeCooldown;
            f.Energy-=balance.dodgeEnergyCost;f.State=CombatState.Dodge;Emit("Dodge",f.Index,f.Index);return true;
        }
        void MoveCpu(FighterState f,FighterState enemy,double dt)
        {
            double radius=Math.Sqrt(f.X*f.X+f.Z*f.Z);
            if(radius>SafeRadius-.6) {Route(f,0,0,dt);return;}
            foreach(var area in Areas) if(area.Source!=f.Index && Math.Sqrt((f.X-area.X)*(f.X-area.X)+(f.Z-area.Z)*(f.Z-area.Z))<area.Radius+.6) {double ax=f.X-area.X,az=f.Z-area.Z;if(Math.Abs(ax)+Math.Abs(az)<.1)ax=f.Index==0?1:-1;Move(f,ax,az,1,dt,CombatState.Strafe);return;}
            foreach(var zone in TerrainZones) if(!IsTerrainBeneficial(zone.Kind)&&Math.Pow(f.X-zone.X,2)+Math.Pow(f.Z-zone.Z,2)<Math.Pow(zone.Radius+.5,2)) {double zx=f.X-zone.X,zz=f.Z-zone.Z;if(Math.Abs(zx)+Math.Abs(zz)<.01)zz=f.Index==0?1:-1;Move(f,zx,zz,1,dt,CombatState.Strafe);return;}
            // Seek a nearby resource briefly; never abandon combat to camp a distant healing field.
            if(f.Health<f.Stats.MaxHealth*.45 && TacticalTick%160<40)
                foreach(var zone in TerrainZones)if(IsTerrainBeneficial(zone.Kind)&&!Inside(f,zone)&&Math.Pow(f.X-zone.X,2)+Math.Pow(f.Z-zone.Z,2)<16){Route(f,zone.X,zone.Z,dt);return;}
            double dx=enemy.X-f.X,dz=enemy.Z-f.Z,d=Math.Sqrt(dx*dx+dz*dz);
            double preferred=Math.Max(balance.minimumFighterDistance+.2,f.Avatar.attackRange*.78);
            if(d>Math.Min(f.Avatar.attackRange*.95,preferred+.35) || !HasLineOfSight(f.Index,enemy.Index)) {Route(f,enemy.X,enemy.Z,dt);return;}
            if(d<preferred-1.4&&f.Avatar.attackStyle!="Melee") {Move(f,-dx,-dz,.8,dt,CombatState.Retreat);return;}
            if((TacticalTick+f.Index*23)%60<35) {f.State=CombatState.Recover;return;}
            double side=((TacticalTick/80+f.Index)%2==0)?1:-1;
            Move(f,-dz*side,dx*side,balance.strafeSpeedMultiplier,dt,CombatState.Strafe);
        }
        double Speed(FighterState f) {return f.Avatar.baseMovementSpeed*f.Stats.MovementStamina*Math.Max(.2,1+Modifier(f,"Haste")-Modifier(f,"Slow"));}
        void Move(FighterState f,double x,double z,double factor,double dt,CombatState state)
        {
            if(HasStatus(f,"Root")||HasStatus(f,"Stun")) {f.State=CombatState.Defend;return;}
            double length=Math.Sqrt(x*x+z*z);
            if(length<1e-9) {f.State=f.RecoveryRemaining>0?CombatState.Recover:CombatState.Idle;return;}
            if(length>1) {x/=length;z/=length;}
            var enemy=Fighters[1-f.Index];double away=x*(f.X-enemy.X)+z*(f.Z-enemy.Z);
            if(away>0&&state!=CombatState.Dodge)factor*=1-(1-balance.backpedalMultiplier)*Math.Min(1,away/Math.Max(.01,Distance(f,enemy)));
            Translate(f,x*Speed(f)*factor*dt,z*Speed(f)*factor*dt);f.State=state;
        }
        double Clamp(double x) { return Math.Max(-balance.arenaHalfSize,Math.Min(balance.arenaHalfSize,x)); }
        double Scale(FighterState f,AbilityDefinition a) { double s=f.Stats.ClimatePower;switch(a.climateScaling){case "green":s=f.City.greenScore;break;case "water":s=f.City.waterScore;break;case "heatProtection":s=f.City.heatProtectionScore;break;case "resilience":s=f.City.resilienceScore;break;case "unsealed":s=100-f.City.sealingScore;break;}return a.baseDamage*(balance.abilityScalingBase+s/100); }
        void Status(FighterState target,FighterState source,string type,double duration,double magnitude)
        {
            if(duration<=0||target.Health<=0)return;
            if(type=="Stun"||type=="Root")
            {
                if(target.ControlImmunityRemaining>1e-9)return;
                duration=Math.Min(duration,type=="Stun"?balance.maxStunSeconds:balance.maxRootSeconds);
                target.ControlImmunityRemaining=duration+balance.controlImmunitySeconds;
                target.DodgeRemaining=0;
                if(type=="Stun"){target.CastingAbilityId=null;target.CastRemaining=0;CancelCharge(target);}
                Emit(type,source.Index,target.Index,duration);
            }
            // Refresh same-source/type rather than permit unbounded stacking.
            var old=target.Effects.Find(e=>e.Type==type&&e.Source==source.Index);
            if(old==null)target.Effects.Add(new StatusEffect{Type=type,Remaining=duration,Magnitude=magnitude,Source=source.Index});else {old.Remaining=duration;old.Magnitude=magnitude;}
            Emit("Status",source.Index,target.Index,duration);
        }
        void Resolve(FighterState f,FighterState enemy,AbilityDefinition a)
        {
            double amount=Scale(f,a);bool self=a.effectType=="Heal"||a.effectType=="Shield"||a.effectType=="Buff"||a.effectType=="Haste"||a.effectType=="Focus";
            if(!self) {Deliver(f,enemy,a,amount);return;}
            ApplyAbility(f,enemy,a,amount);
        }
        void ApplyAbility(FighterState f,FighterState enemy,AbilityDefinition a,double amount,double durationMultiplier=1)
        {
            bool self=a.effectType=="Heal"||a.effectType=="Shield"||a.effectType=="Buff"||a.effectType=="Haste"||a.effectType=="Focus";
            if(!self&&enemy.DodgeRemaining>0)return;
            Emit("Impact",f.Index,self?f.Index:enemy.Index,amount,a.id);
            switch(a.effectType) {
                case "Heal":double heal=Math.Min(amount,f.Stats.MaxHealth-f.Health);f.Health+=heal;f.HealingDone+=heal;Emit("Heal",f.Index,f.Index,heal,a.id);break;
                case "Shield":f.Shield=Math.Min(f.Stats.MaxHealth,f.Shield+amount);Emit("Shield",f.Index,f.Index,amount,a.id);break;
                case "Buff":case "Haste":case "Focus":Status(f,f,a.effectType,a.duration*durationMultiplier,a.magnitude);break;
                case "Debuff":Status(enemy,f,"Debuff",a.duration,a.magnitude);break;
                case "DamageOverTime":if(a.duration>0)Status(enemy,f,"DamageOverTime",a.duration,amount/a.duration);else Hurt(f,enemy,amount,true,a.id);break;
                default:Hurt(f,enemy,amount,true,a.id); if(a.effectType=="Slow"||a.effectType=="Stun"||a.effectType=="Root"||a.effectType=="Vulnerable")Status(enemy,f,a.effectType,a.duration,a.magnitude);if(a.effectType=="Knockback"&&enemy.Health>0){double d=Distance(f,enemy);if(d>0){Translate(enemy,(enemy.X-f.X)/d*a.magnitude,(enemy.Z-f.Z)/d*a.magnitude);}}break;
            }
            if(!string.IsNullOrEmpty(a.statusEffect)&&a.statusEffect!=a.effectType) Status(a.statusEffect=="Buff"||a.statusEffect=="Focus"||a.statusEffect=="Haste"?f:enemy,f,a.statusEffect,a.duration,a.statusEffect=="DamageOverTime"?(a.duration>0?amount/a.duration:0):a.magnitude);
            if(!self){OnHitSelf(f,a);if(!string.IsNullOrEmpty(a.onHitTargetEffect))Status(enemy,f,a.onHitTargetEffect,a.onHitTargetDuration,a.onHitTargetMagnitude);}
        }
        public static bool HasStatus(FighterState f,string type) {return f.Effects.Exists(e=>e.Type==type&&e.Remaining>0);}
        public static bool IsTerrainBeneficial(string kind) {return kind=="Regen"||kind=="Haste"||kind=="Focus";}
        static bool Inside(FighterState f,TerrainZone z) {return (f.X-z.X)*(f.X-z.X)+(f.Z-z.Z)*(f.Z-z.Z)<=z.Radius*z.Radius;}
        void ApplyTerrain(FighterState f,double dt)
        {
            foreach(var z in TerrainZones) if(Inside(f,z))
            {
                if(z.Kind=="Heat")Hurt(null,f,7*(1-f.Stats.HeatResistance*.5)*dt,false,null);
                else if(z.Kind=="Regen"){double h=Math.Min(4*dt,f.Stats.MaxHealth-f.Health);f.Health+=h;f.HealingDone+=h;f.Energy=Math.Min(f.Stats.Energy,f.Energy+5*dt);}
                else {
                    string type=z.Kind=="Mud"?"Slow":z.Kind;var old=f.Effects.Find(e=>e.Type==type&&e.Source==-1);
                    double magnitude=type=="Focus"?.75:.25;
                    if(old==null)f.Effects.Add(new StatusEffect{Type=type,Source=-1,Remaining=.15,Magnitude=magnitude});else old.Remaining=.15;
                }
            }
        }
        void OnHitSelf(FighterState f,AbilityDefinition a)
        {
            if(f.Health<=0)return;
            if(a.onHitSelfEffect=="Heal"){double heal=Math.Min(a.onHitSelfMagnitude*f.Stats.MaxHealth,f.Stats.MaxHealth-f.Health);f.Health+=heal;f.HealingDone+=heal;Emit("Heal",f.Index,f.Index,heal,a.id);}
            else if(a.onHitSelfEffect=="Shield"){double shield=a.onHitSelfMagnitude*f.Stats.MaxHealth;f.Shield=Math.Min(f.Stats.MaxHealth,f.Shield+shield);Emit("Shield",f.Index,f.Index,shield,a.id);}
            else if(!string.IsNullOrEmpty(a.onHitSelfEffect))Status(f,f,a.onHitSelfEffect,a.onHitSelfDuration,a.onHitSelfMagnitude);
        }
        void Hurt(FighterState source,FighterState target,double raw,bool roll,string ability)
        {
            if(target.Health<=0)return;
            if(roll&&(target.DodgeRemaining>0)) {target.State=CombatState.Defend;Emit("Hit",source.Index,target.Index,0,ability);return;}
            if(source!=null)raw*=Math.Max(0,1+Modifier(source,"Buff")-Modifier(source,"Debuff"))*(1+Math.Min(.5,Modifier(target,"Vulnerable")));
            if(roll) { raw*=1+(random.NextDouble()*2-1)*balance.damageVariance;if(random.NextDouble()<balance.criticalChance)raw*=balance.criticalMultiplier; }
            double damage=source==null?Math.Max(0,raw):DamageCalculator.Calculate(raw,target.Stats.Defense,balance);
            double absorbed=Math.Min(target.Shield,damage);target.Shield-=absorbed;damage=Math.Min(target.Health,damage-absorbed);target.Health-=damage;
            if(roll&&damage>0)target.TacticalRetreatRemaining=balance.retreatDuration;
            if(source!=null)source.DamageDealt+=damage;Emit("Hit",source==null?-1:source.Index,target.Index,damage,ability);
            if(target.Health<=0) { target.Health=0;target.State=CombatState.Dead;target.CastingAbilityId=null;target.CastRemaining=0;CancelCharge(target);Emit("Death",source==null?-1:source.Index,target.Index); }
        }
        static void Aim(FighterState f,FighterState enemy)
        {
            double x=enemy.X-f.X,z=enemy.Z-f.Z,d=Math.Sqrt(x*x+z*z);
            f.AimX=d>1e-9?x/d:1;f.AimZ=d>1e-9?z/d:0;f.CastTargetX=enemy.X;f.CastTargetZ=enemy.Z;
        }
        bool StartCharge(FighterState f,FighterState enemy)
        {
            var a=abilities[f.Avatar.abilityIds[2]];
            if(Elapsed<balance.ultimateUnlockSeconds||f.Cooldowns[a.id]>1e-9||f.Energy<a.energyCost||(a.delivery!="Self"&&Distance(f,enemy)>a.range))return false;
            f.IsCharging=true;f.Charge=0;f.ChargeElapsed=0;f.ChargingAbilityId=a.id;f.State=CombatState.UseAbility;Aim(f,enemy);
            Emit("ChargeStart",f.Index,enemy.Index,0,a.id);return true;
        }
        void CancelCharge(FighterState f)
        {
            if(!f.IsCharging)return;
            Emit("ChargeCancel",f.Index,f.Index,0,f.ChargingAbilityId);f.IsCharging=false;f.Charge=0;f.ChargingAbilityId=null;
        }
        void ReleaseCharge(FighterState f,FighterState enemy)
        {
            var a=abilities[f.ChargingAbilityId];double charge=f.Charge;
            if(f.ChargeElapsed+1e-9<balance.chargeMinSeconds){CancelCharge(f);return;}
            f.IsCharging=false;f.ChargingAbilityId=null;f.Charge=0;
            f.Energy-=a.energyCost;f.Cooldowns[a.id]=a.cooldown*f.Stats.CooldownModifier;f.AbilitiesUsed++;f.AbilityUsage[a.id]++;
            Emit("ChargeRelease",f.Index,enemy.Index,charge,a.id);Emit("Ability",f.Index,enemy.Index,charge,a.id);
            Deliver(f,enemy,a,Scale(f,a)*(.6+charge*.9),.6+charge*.9);f.RecoveryRemaining=balance.abilityRecoverySeconds;
        }
        void Launch(FighterState f,FighterState enemy,AbilityDefinition ability,double power,double range)
        {
            double speed=ability!=null&&ability.projectileSpeed>0?ability.projectileSpeed:balance.projectileSpeed;
            var p=new ProjectileState {Id=++nextProjectileId,Source=f.Index,Target=enemy.Index,AbilityId=ability==null?null:ability.id,
                X=f.X,Z=f.Z,DirectionX=f.AimX,DirectionZ=f.AimZ,Speed=speed,
                Radius=(ability!=null&&ability.projectileRadius>0?ability.projectileRadius:balance.projectileRadius)*(ability!=null&&ability.isUltimate?1.8:1),Remaining=range/speed,Power=power,IsUltimate=ability!=null&&ability.isUltimate};
            double wall=2;foreach(var o in Obstacles)wall=Math.Min(wall,SegmentHit(f.X,f.Z,f.X+f.AimX*.75,f.Z+f.AimZ*.75,o.X,o.Z,o.Radius+p.Radius));
            if(wall<=1) {p.X+=f.AimX*.75*wall;p.Z+=f.AimZ*.75*wall;EmitProjectile("Blocked",p);return;}
            p.X+=f.AimX*.75;p.Z+=f.AimZ*.75;p.Remaining=Math.Max(0,(range-.75)/speed);Projectiles.Add(p);EmitProjectile("ProjectileLaunch",p);
        }
        void EmitProjectile(string type,ProjectileState p)
        {
            var handler=Event;if(handler!=null)handler(new BattleEvent{Type=type,Source=p.Source,Target=p.Target,AbilityId=p.AbilityId,Amount=p.Power,X=p.X,Z=p.Z,DirectionX=p.DirectionX,DirectionZ=p.DirectionZ,ProjectileId=p.Id,Radius=p.Radius,Delivery="Projectile"});
        }
        void AdvanceProjectiles(double dt)
        {
            for(int i=Projectiles.Count-1;i>=0;i--)
            {
                var p=Projectiles[i];var target=Fighters[p.Target];double oldX=p.X,oldZ=p.Z;
                double active=Math.Min(dt,p.Remaining);p.X+=p.DirectionX*p.Speed*active;p.Z+=p.DirectionZ*p.Speed*active;p.Remaining-=dt;p.Age+=dt;
                double vx=p.X-oldX,vz=p.Z-oldZ;
                double hit=SegmentHit(oldX,oldZ,p.X,p.Z,target.X,target.Z,balance.fighterHitRadius+p.Radius),wall=2;
                foreach(var o in Obstacles) wall=Math.Min(wall,SegmentHit(oldX,oldZ,p.X,p.Z,o.X,o.Z,o.Radius+p.Radius));
                if(wall<=1&&wall<=hit) {p.X=oldX+vx*wall;p.Z=oldZ+vz*wall;EmitProjectile("Blocked",p);Projectiles.RemoveAt(i);}
                else if(hit<=1&&target.Health>0)
                {
                    p.X=oldX+vx*hit;p.Z=oldZ+vz*hit;
                    if(target.DodgeRemaining>0)EmitProjectile("ProjectileMiss",p);
                    else {EmitProjectile("ProjectileImpact",p);if(p.AbilityId==null)Hurt(Fighters[p.Source],target,p.Power,true,null);else ApplyAbility(Fighters[p.Source],target,abilities[p.AbilityId],p.Power);}
                    Projectiles.RemoveAt(i);
                }
                else if(p.Remaining<=0||Math.Abs(p.X)>balance.arenaHalfSize+2||Math.Abs(p.Z)>balance.arenaHalfSize+2){EmitProjectile("ProjectileMiss",p);Projectiles.RemoveAt(i);}
            }
        }
        public bool HasLineOfSight(int source,int target) {return ClearPath(Fighters[source].X,Fighters[source].Z,Fighters[target].X,Fighters[target].Z,0);}
        static double SegmentHit(double x,double z,double ex,double ez,double cx,double cz,double radius)
        {
            double dx=ex-x,dz=ez-z,rx=x-cx,rz=z-cz,c=rx*rx+rz*rz-radius*radius;
            if(c<0)return 0;double a=dx*dx+dz*dz;if(a<1e-12)return 2;
            double b=rx*dx+rz*dz,disc=b*b-a*c;if(disc<0)return 2;
            double t=(-b-Math.Sqrt(disc))/a;return t>=0&&t<=1?t:2;
        }
        bool ClearPath(double x,double z,double ex,double ez,double padding)
        {foreach(var o in Obstacles)if(SegmentHit(x,z,ex,ez,o.X,o.Z,o.Radius+padding)<=1)return false;return true;}
        void Translate(FighterState f,double dx,double dz)
        {
            // Short swept moves permit sliding without tunnelling through cover on dash/knockback.
            int steps=Math.Max(1,(int)Math.Ceiling(Math.Sqrt(dx*dx+dz*dz)/.15));
            for(int i=0;i<steps;i++)
            {
                double nx=Clamp(f.X+dx/steps),nz=Clamp(f.Z+dz/steps);
                if(ClearPath(f.X,f.Z,nx,nz,balance.fighterHitRadius)) {f.X=nx;f.Z=nz;}
                else {if(ClearPath(f.X,f.Z,nx,f.Z,balance.fighterHitRadius))f.X=nx;if(ClearPath(f.X,f.Z,f.X,nz,balance.fighterHitRadius))f.Z=nz;}
            }
        }
        bool ClearRoute(double x,double z,double ex,double ez)
        {
            if(!ClearPath(x,z,ex,ez,balance.fighterHitRadius))return false;
            foreach(var zone in TerrainZones)if(!IsTerrainBeneficial(zone.Kind))
            {
                double r=zone.Radius+.3;
                // Permit escape and pursuit into a deliberately occupied field.
                if(Math.Pow(x-zone.X,2)+Math.Pow(z-zone.Z,2)<r*r||Math.Pow(ex-zone.X,2)+Math.Pow(ez-zone.Z,2)<r*r)continue;
                if(SegmentHit(x,z,ex,ez,zone.X,zone.Z,r)<=1)return false;
            }
            return true;
        }
        void Route(FighterState f,double targetX,double targetZ,double dt)
        {
            if(ClearRoute(f.X,f.Z,targetX,targetZ)){Move(f,targetX-f.X,targetZ-f.Z,1,dt,CombatState.MoveToTarget);return;}
            int side=f.Index;
            double cachedX=routeX[side]-f.X,cachedZ=routeZ[side]-f.Z;
            if(Elapsed<routeUntil[side]&&cachedX*cachedX+cachedZ*cachedZ>.0625&&ClearRoute(f.X,f.Z,routeX[side],routeZ[side]))
            {double length=Math.Sqrt(cachedX*cachedX+cachedZ*cachedZ);Move(f,cachedX/length,cachedZ/length,1,dt,CombatState.MoveToTarget);return;}
            var xs=new List<double>{f.X,targetX};var zs=new List<double>{f.Z,targetZ};
            foreach(var o in Obstacles)for(int i=0;i<8;i++) {double angle=i*Math.PI/4,r=(o.Radius+balance.fighterHitRadius+.2)/Math.Cos(Math.PI/8);xs.Add(o.X+Math.Cos(angle)*r);zs.Add(o.Z+Math.Sin(angle)*r);}
            foreach(var zone in TerrainZones)if(!IsTerrainBeneficial(zone.Kind))for(int i=0;i<8;i++){double angle=i*Math.PI/4,r=(zone.Radius+.6)/Math.Cos(Math.PI/8);xs.Add(zone.X+Math.Cos(angle)*r);zs.Add(zone.Z+Math.Sin(angle)*r);}
            int n=xs.Count;var cost=new double[n];var previous=new int[n];var visited=new bool[n];
            for(int i=0;i<n;i++){cost[i]=double.PositiveInfinity;previous[i]=-1;}cost[0]=0;
            for(int k=0;k<n;k++) {int best=-1;for(int i=0;i<n;i++)if(!visited[i]&&(best<0||cost[i]<cost[best]))best=i;if(best<0||double.IsInfinity(cost[best]))break;if(best==1)break;visited[best]=true;
                for(int j=0;j<n;j++)if(!visited[j]&&ClearRoute(xs[best],zs[best],xs[j],zs[j])) {double dx=xs[j]-xs[best],dz=zs[j]-zs[best],c=cost[best]+Math.Sqrt(dx*dx+dz*dz);if(c<cost[j]){cost[j]=c;previous[j]=best;}}}
            int next=1;if(previous[next]>=0){while(previous[next]>0)next=previous[next];routeX[side]=xs[next];routeZ[side]=zs[next];routeUntil[side]=Elapsed+.2;double dx=xs[next]-f.X,dz=zs[next]-f.Z,length=Math.Sqrt(dx*dx+dz*dz);if(length>1e-9)Move(f,dx/length,dz/length,1,dt,CombatState.MoveToTarget);}else Move(f,targetX-f.X,targetZ-f.Z,1,dt,CombatState.MoveToTarget);
        }
        void Deliver(FighterState f,FighterState enemy,AbilityDefinition a,double power,double durationMultiplier=1)
        {
            switch(a.delivery)
            {
                case "Self":ApplyAbility(f,enemy,a,power,durationMultiplier);break;
                case "Melee":Strike(f,enemy,a,power,a.range,a.coneDegrees,"MeleeStrike");break;
                case "Dash":
                    double travel=Math.Min(a.dashDistance,Math.Max(0,Distance(f,enemy)-balance.minimumFighterDistance));
                    if(!HasStatus(f,"Root"))Translate(f,f.AimX*travel,f.AimZ*travel);Strike(f,enemy,a,power,Math.Max(2,a.areaOfEffect),a.coneDegrees,"DashStrike");break;
                case "Zone":
                    if(!ClearPath(f.X,f.Z,f.CastTargetX,f.CastTargetZ,0)) {Emit("Blocked",f.Index,enemy.Index,0,a.id);break;}
                    var area=new AreaState {Id=++nextAreaId,Source=f.Index,Target=enemy.Index,AbilityId=a.id,X=f.CastTargetX,Z=f.CastTargetZ,Radius=Math.Max(.5,a.areaOfEffect),WarningRemaining=a.telegraphSeconds,Remaining=a.zoneDuration,Power=power};
                    Areas.Add(area);EmitArea("ZoneCreated",area);break;
                default:Launch(f,enemy,a,power,a.range);break;
            }
        }
        void Strike(FighterState f,FighterState enemy,AbilityDefinition a,double power,double range,double cone,string type)
        {
            var handler=Event;if(handler!=null)handler(new BattleEvent{Type=type,Source=f.Index,Target=enemy.Index,Amount=power,AbilityId=a==null?null:a.id,X=f.X,Z=f.Z,DirectionX=f.AimX,DirectionZ=f.AimZ,Radius=range,Delivery=a==null?"Melee":a.delivery});
            double distance=Distance(f,enemy),dot=distance>1e-9?((enemy.X-f.X)*f.AimX+(enemy.Z-f.Z)*f.AimZ)/distance:1;
            if(distance>range||dot<Math.Cos(cone*Math.PI/360)||!HasLineOfSight(f.Index,enemy.Index)||enemy.DodgeRemaining>0) {Emit("Miss",f.Index,enemy.Index,0,a==null?null:a.id);return;}
            if(a==null)Hurt(f,enemy,power,true,null);else ApplyAbility(f,enemy,a,power);
        }
        void EmitArea(string type,AreaState area)
        {var handler=Event;if(handler!=null)handler(new BattleEvent{Type=type,Source=area.Source,Target=area.Target,AbilityId=area.AbilityId,X=area.X,Z=area.Z,Radius=area.Radius,Amount=area.Power,Delivery="Zone"});}
        void AdvanceAreas(double dt)
        {
            for(int i=Areas.Count-1;i>=0;i--)
            {
                var area=Areas[i];if(area.WarningRemaining>0){area.WarningRemaining=Math.Max(0,area.WarningRemaining-dt);continue;}
                double active=Math.Min(dt,area.Remaining);area.PulseElapsed+=active;area.Remaining-=active;
                if(area.PulseElapsed>=.5-1e-9||area.Remaining<=1e-9)
                {
                    var target=Fighters[area.Target];var ability=abilities[area.AbilityId];double dx=target.X-area.X,dz=target.Z-area.Z;
                    EmitArea("ZonePulse",area);
                    if(dx*dx+dz*dz<=area.Radius*area.Radius&&target.DodgeRemaining<=0&&ClearPath(area.X,area.Z,target.X,target.Z,0))
                    {
                        double power=area.Power*area.PulseElapsed/ability.zoneDuration;
                        // A persistent field budgets damage over its lifetime; statuses refresh only inside.
                        if(ability.effectType=="DamageOverTime"){var caster=Fighters[area.Source];Hurt(caster,target,power,true,ability.id);OnHitSelf(caster,ability);if(!string.IsNullOrEmpty(ability.onHitTargetEffect))Status(target,caster,ability.onHitTargetEffect,ability.onHitTargetDuration,ability.onHitTargetMagnitude);if(!string.IsNullOrEmpty(ability.statusEffect))Status(target,caster,ability.statusEffect,ability.duration,ability.magnitude);}
                        else ApplyAbility(Fighters[area.Source],target,ability,power);
                    }
                    area.PulseElapsed=0;
                }
                if(area.Remaining<=1e-9)Areas.RemoveAt(i);
            }
        }
        bool CheckFinished()
        {
            if(Fighters[0].Health>0&&Fighters[1].Health>0&&Elapsed+1e-9<balance.maxDuration)return false;
            IsFinished=true;double a=Fighters[0].Health/Fighters[0].Stats.MaxHealth,b=Fighters[1].Health/Fighters[1].Stats.MaxHealth;int winner=Math.Abs(a-b)<1e-9?-1:(a>b?0:1);
            Result=new MatchResult{config=DataValidator.Copy(Config),winnerIndex=winner,winnerCityId=winner<0?null:Fighters[winner].City.id,outcome=winner<0?"Draw":(Fighters[0].Health<=0||Fighters[1].Health<=0?"Knockout":"Timeout"),duration=Elapsed,fighterA=Summarize(Fighters[0]),fighterB=Summarize(Fighters[1]),inputs=recordedInputs.ToArray()};return true;
        }
        static FighterResult Summarize(FighterState f) { var ids=(string[])f.Avatar.abilityIds.Clone();var counts=new int[ids.Length];for(int i=0;i<ids.Length;i++)counts[i]=f.AbilityUsage[ids[i]];return new FighterResult{cityId=f.City.id,avatarId=f.Avatar.id,remainingHealth=f.Health,damageDealt=f.DamageDealt,healingDone=f.HealingDone,basicAttacks=f.BasicAttacks,abilitiesUsed=f.AbilitiesUsed,abilityIds=ids,abilityCounts=counts}; }
        public MatchResult RunToEnd() { while(!IsFinished)Step();return Result; }
    }
}

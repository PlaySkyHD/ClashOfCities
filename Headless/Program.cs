using System.Text.Json;
using ClashOfCities.Core;

internal static class Program
{
    static readonly JsonSerializerOptions Json = new() { IncludeFields = true, WriteIndented = true };
    static GameData Data;
    static int Main(string[] args)
    {
        try
        {
            Data = JsonSerializer.Deserialize<GameData>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "GameData.json")), Json);
            if (args.Length > 0 && args[0] == "responsiveness") {TestResponsiveness();TestSkillDepth();return 0;}
            if (args.Length > 0 && args[0] == "controllers") return ControllerTests.Run();
            if (args.Length == 0 || args[0] == "test") return Test();
            if (args[0] == "replay")
            {
                var saved = JsonSerializer.Deserialize<MatchResult>(File.ReadAllText(args[1]), Json);
                var replay = new BattleSimulation(Data, saved.config, saved.inputs).RunToEnd();
                Console.WriteLine(JsonSerializer.Serialize(replay, Json));
                return JsonSerializer.Serialize(saved, Json) == JsonSerializer.Serialize(replay, Json) ? 0 : 1;
            }
            if (args[0] == "simulate")
            {
                var count = args.Length > 1 ? int.Parse(args[1]) : 1000;
                if(count < 1 || count > 1000000) throw new ArgumentException("count must be 1..1000000");
                var a = args.Length > 2 ? args[2] : "muenster";
                var b = args.Length > 3 ? args[3] : "bonn";
                var seed = args.Length > 4 ? int.Parse(args[4]) : 726491;
                var env = args.Length > 5 ? args[5] : "random";
                int winsA=0,winsB=0,draws=0; double duration=0;
                for(int i=0;i<count;i++)
                {
                    var result=new BattleSimulation(Data, Config(a,b,unchecked(seed+i),env)).RunToEnd();
                    if(result.winnerIndex==0) winsA++; else if(result.winnerIndex==1) winsB++; else draws++;
                    duration+=result.duration;
                    if(count==1) Console.WriteLine(JsonSerializer.Serialize(result,Json));
                }
                var summary=$"DEMO DATA | {a} vs {b}: {count} matches, wins {winsA}:{winsB}, draws {draws}, mean duration {duration/count:F2}s, first seed {seed}";
                if(count==1) Console.Error.WriteLine(summary); else Console.WriteLine(summary);
                return 0;
            }
            throw new ArgumentException("Usage: dotnet run --project Headless -- [test | simulate [count cityA cityB seed environment] | replay result.json]");
        }
        catch(Exception e) { Console.Error.WriteLine(e.ToString()); return 1; }
    }
    static MatchConfig Config(string a="muenster",string b="bonn",int seed=726491,string environment="normal") => new()
    { cityAId=a, cityBId=b, avatarAId=Data.avatars.First(x=>x.cityId==a).id, avatarBId=Data.avatars.First(x=>x.cityId==b).id, seed=seed, climateEventId=environment, balanceVersion=Data.balance.version };
    static int Ticks(double seconds) => (int)Math.Ceiling(seconds/Data.balance.tickSeconds-1e-8);
    static void Check(bool condition,string name) { if(!condition) throw new Exception("FAIL: "+name); Console.WriteLine("PASS: "+name); }
    static void Reject(Action action,string name) { bool rejected=false; try { action(); } catch(ArgumentException) {rejected=true;} catch(InvalidOperationException) {rejected=true;} Check(rejected,name); }
    static int Test()
    {
        var rngA=new SeededRandom(42); var rngB=new SeededRandom(42);
        bool same=true,bounded=true; for(int i=0;i<10000;i++) {var v=rngA.NextDouble(); same &= v==rngB.NextDouble(); bounded &= v>=0&&v<1;}
        Check(same&&bounded,"Seeded RNG reproducible and bounded");
        var env=Data.environments.First(e=>e.id=="normal");
        var best=new CityDefinition {greenScore=100,waterScore=100,heatProtectionScore=100,resilienceScore=100,sealingScore=0,heatRiskScore=0};
        var worst=new CityDefinition {sealingScore=100,heatRiskScore=100};
        Check(Math.Abs(ClimateCalculator.Calculate(best,env)-100)<1e-8 && Math.Abs(ClimateCalculator.Calculate(worst,env))<1e-8,"Climate endpoints and sealing direction");
        var bestStats=ClimateCalculator.Map(best,env,Data.balance); var worstStats=ClimateCalculator.Map(worst,env,Data.balance);
        Check(bestStats.MaxHealth>worstStats.MaxHealth&&bestStats.AttackPower>worstStats.AttackPower&&bestStats.HeatResistance>worstStats.HeatResistance,"Climate improves combat statistics");
        Check(Math.Abs(DamageCalculator.Calculate(100,100,Data.balance)-50)<1e-8,"Defense formula");
        var cfg=Config(); var sim=new BattleSimulation(Data,cfg); var result=sim.RunToEnd();
        var again=new BattleSimulation(Data,Config()).RunToEnd();
        Check(JsonSerializer.Serialize(result,Json)==JsonSerializer.Serialize(again,Json),"Identical seed and inputs yield identical complete results");
        Check(sim.IsFinished&&sim.Elapsed<=Data.balance.maxDuration+Data.balance.tickSeconds,"Match ends within time limit");
        Check(result.fighterA.basicAttacks+result.fighterB.basicAttacks>0,"Basic attacks used");
        Check(result.fighterA.abilitiesUsed>0&&result.fighterB.abilitiesUsed>0,"Both fighters use abilities");
        Check(sim.Fighters.All(f=>f.Health>=0&&f.Health<=f.Stats.MaxHealth&&f.Energy>=0&&f.Energy<=f.Stats.Energy),"Health and energy bounded");
        var snapshot=JsonSerializer.Serialize(result,Json); sim.Step();
        Check(snapshot==JsonSerializer.Serialize(sim.Result,Json),"Completed match immutable on subsequent tick");
        var invalid=Config(); invalid.balanceVersion="unknown"; Reject(()=>new BattleSimulation(Data,invalid),"Wrong balance version rejected");
        invalid=Config(); invalid.avatarAId=Data.avatars.First(x=>x.cityId=="ulm").id; Reject(()=>new BattleSimulation(Data,invalid),"City/avatar mismatch rejected");
        foreach(var environment in Data.environments)
        foreach(var city in Data.cities)
        {
            var run=new BattleSimulation(Data,Config(city.id,"bonn",123,environment.id));run.RunToEnd();
            Check(run.IsFinished&&run.Fighters.All(f=>!double.IsNaN(f.Health)),city.id+" in "+environment.id);
        }
        var randomMatch=new BattleSimulation(Data,Config(environment:"random")).RunToEnd();
        Check(JsonSerializer.Serialize(randomMatch,Json)==JsonSerializer.Serialize(new BattleSimulation(Data,randomMatch.config).RunToEnd(),Json),"Resolved random environment replays exactly");
        Check(result.fighterA.abilityCounts[2]+result.fighterB.abilityCounts[2]>0,"Ultimate used");
        var lethal=Clone(); lethal.balance.baseAttack=100000; lethal.balance.attackPerPower=0;
        lethal.balance.dodgeChance=0;
        foreach(var ability in lethal.abilities) ability.energyCost=1000000;
        var lethalConfig=Config();lethalConfig.mode=MatchMode.PlayerVsPlayer;
        var death=new BattleSimulation(lethal,lethalConfig);death.Fighters[0].X=-3;death.Fighters[1].X=3;while(!death.IsFinished)death.Step(new FighterInput{basicAttack=true},default);
        Check(death.Fighters.Any(f=>f.Health==0&&f.State==CombatState.Dead)&&death.Result.outcome=="Knockout","Lethal damage enters death state and ends match");
        var timed=Clone(); timed.balance.maxDuration=.1;
        var timeout=new BattleSimulation(timed,Config()); timeout.Fighters[0].Health-=10;
        Check(timeout.RunToEnd().outcome=="Timeout","Timeout resolves winner without death");
        Check(new BattleSimulation(timed,Config()).RunToEnd().outcome=="Draw","Equal health fraction at timeout draws");
        var bad=Clone();bad.cities[0].greenScore=101; Reject(()=>new BattleSimulation(bad,Config()),"Out-of-range climate score rejected");
        bad=Clone();bad.abilities[0].effectType="Unknown"; Reject(()=>new BattleSimulation(bad,Config()),"Unknown effect rejected");
        foreach(var effect in new[]{"Damage","Heal","Shield","Knockback","Slow","Buff","Debuff","AreaDamage","DamageOverTime"}) TestEffect(effect);
        TestPlayerModes();
        TestProjectilesAndCharge();
        TestSkillDepth();
        TestCombosAndTerrain();
        TestDifficulty();
        TestBeginnerCombat();
        TestResponsiveness();
        return 0;
    }
    static void TestPlayerModes()
    {
        foreach(var mode in new[]{MatchMode.PlayerVsCpu,MatchMode.PlayerVsPlayer})
        {
            var cfg=Config();cfg.mode=mode;
            var sim=new BattleSimulation(Data,cfg);
            int step=0;double startingX=sim.Fighters[0].X;
            sim.Step(new FighterInput{moveX=1,moveZ=1},default);
            Check(sim.Fighters[0].X>startingX&&sim.Fighters[0].Z>0,"Human directional movement: "+mode);
            while(!sim.IsFinished)
            {
                double dx=sim.Fighters[1].X-sim.Fighters[0].X,dz=sim.Fighters[1].Z-sim.Fighters[0].Z;
                sim.Step(new FighterInput{moveX=dx,moveZ=dz,basicAttack=true,skill1=step%35==0,skill2=step%65==0,ultimate=step%75<25,dodge=step%90==0},new FighterInput{moveX=-dx,moveZ=-dz,basicAttack=true,skill1=step%40==0,skill2=step%60==0,ultimate=step%80<30,dodge=step%95==0});step++;
            }
            Check(sim.Result.inputs.Length==sim.Tick,"Every manual tick recorded: "+mode);
            var json=JsonSerializer.Serialize(sim.Result,Json);
            var saved=JsonSerializer.Deserialize<MatchResult>(json,Json);
            var replay=new BattleSimulation(Data,saved.config,saved.inputs).RunToEnd();
            Check(json==JsonSerializer.Serialize(replay,Json),"Serialized manual input replay exact: "+mode);
            Check(sim.Result.fighterA.basicAttacks>0&&sim.Result.fighterA.abilitiesUsed>0,"Human commanded attacks/skills: "+mode);
            Check(sim.Fighters.All(f=>Math.Abs(f.X)<=Data.balance.arenaHalfSize&&Math.Abs(f.Z)<=Data.balance.arenaHalfSize),"Movement inside arena: "+mode);
            var shortLog=saved.inputs.Take(1).ToArray();
            Reject(()=>new BattleSimulation(Data,cfg,shortLog).RunToEnd(),"Truncated manual replay rejected: "+mode);
        }
        var controls=Config();controls.mode=MatchMode.PlayerVsPlayer;
        var idle=new BattleSimulation(Data,controls);for(int i=0;i<Ticks(1.25);i++)idle.Step();
        Check(idle.Fighters.All(f=>f.BasicAttacks==0&&f.AbilitiesUsed==0&&f.Z==0),"Human sides never auto-attack or auto-move");
        var fullHeal=new BattleSimulation(Data,controls);fullHeal.Step(new FighterInput{skill2=true},default);
        Check(fullHeal.Fighters[0].AbilitiesUsed==0,"Full-health heal does not spend a skill");
        var diagonal=new BattleSimulation(Data,controls);var straight=new BattleSimulation(Data,controls);
        double before=diagonal.Fighters[0].X;diagonal.Step(new FighterInput{moveX=1,moveZ=1},default);straight.Step(new FighterInput{moveX=1},default);
        double distance=Math.Sqrt(Math.Pow(diagonal.Fighters[0].X-before,2)+Math.Pow(diagonal.Fighters[0].Z,2));
        Check(Math.Abs(distance-(straight.Fighters[0].X-before))<1e-9,"Diagonal input has no speed boost");
        var dodge=new BattleSimulation(Data,controls);double energy=dodge.Fighters[0].Energy;
        dodge.Step(new FighterInput{moveZ=1,dodge=true},default);
        Check(dodge.Fighters[0].State==CombatState.Dodge&&dodge.Fighters[0].DodgeRemaining>0&&dodge.Fighters[0].Energy<energy,"Dodge starts and spends energy");
        double cooldown=dodge.Fighters[0].DodgeCooldown;dodge.Step(new FighterInput{dodge=true},default);
        Check(dodge.Fighters[0].DodgeCooldown<cooldown,"Held dodge cannot reset cooldown");
        for(int i=0;i<Ticks(1);i++)dodge.Step();
        Check(dodge.Fighters[0].DodgeRemaining==0,"Dodge invulnerability expires");
        var evade=new BattleSimulation(Data,controls);evade.Fighters[0].X=-.55;evade.Fighters[1].X=.55;
        evade.Step(new FighterInput{moveZ=1,dodge=true},default);double hp=evade.Fighters[0].Health;
        evade.Step(default,new FighterInput{basicAttack=true});
        Check(evade.Fighters[1].BasicAttacks==1&&evade.Fighters[0].Health==hp,"Active dodge evades an in-range direct attack");
        Reject(()=>idle.Step(new FighterInput{moveX=double.NaN},default),"Nonfinite player input rejected");
        var invalid=Config();invalid.mode=(MatchMode)99;Reject(()=>new BattleSimulation(Data,invalid),"Unknown mode rejected");
        var cpu=new BattleSimulation(Data,Config()); bool strafed=false,retreated=false,dodged=false;
        while(!cpu.IsFinished){cpu.Step();strafed|=cpu.Fighters.Any(f=>f.State==CombatState.Strafe);retreated|=cpu.Fighters.Any(f=>f.State==CombatState.Retreat);dodged|=cpu.Fighters.Any(f=>f.State==CombatState.Dodge);}
        Console.WriteLine($"CPU movement: strafe={strafed} retreat={retreated} dodge={dodged}");
        Check(strafed&&retreated,"CPU circles and repositions during battle");
        var reaction=new BattleSimulation(Data,Config());reaction.Projectiles.Add(new ProjectileState{Id=2,Source=1,Target=0,X=reaction.Fighters[0].X+2,Z=0,DirectionX=-1,Speed=9,Radius=.2,Age=1,Remaining=2,Power=1});reaction.Step();Check(reaction.Fighters[0].DodgeRemaining>0,"CPU dodges an approaching visible projectile");
    }
    static void TestProjectilesAndCharge()
    {
        var data=Clone();foreach(var a in data.abilities){if(a.isUltimate){a.delivery="Projectile";a.range=15;a.effectType="Damage";a.statusEffect=null;a.onHitSelfEffect=null;a.onHitTargetEffect=null;}}data.balance.healthRegenPerGreen=0;data.balance.ultimateUnlockSeconds=0;data.balance.dodgeChance=1;
        var cfg=Config();cfg.mode=MatchMode.PlayerVsPlayer;
        BattleSimulation Setup() {var s=new BattleSimulation(data,cfg);s.Fighters[0].X=-3;s.Fighters[1].X=3;return s;}
        var sim=Setup();double hp=sim.Fighters[1].Health;
        sim.Step(new FighterInput{basicAttack=true},default);
        Check(sim.Projectiles.Count==1&&sim.Fighters[1].Health==hp,"Ranged attack has travel time before damage");
        for(int i=0;i<Ticks(0.75);i++)sim.Step();
        Check(sim.Fighters[1].Health<hp&&sim.Projectiles.Count==0,"Stationary target hit without random dodge roll");
        sim=Setup();hp=sim.Fighters[1].Health;sim.Step(new FighterInput{basicAttack=true},default);
        for(int i=0;i<Ticks(1.5);i++)sim.Step(default,new FighterInput{moveZ=1});
        Check(sim.Fighters[1].Health==hp&&sim.Projectiles.Count==0,"Sideways movement dodges fixed aim projectile");
        var fast=Clone();fast.balance.projectileSpeed=500;fast.balance.healthRegenPerGreen=0;
        sim=new BattleSimulation(fast,cfg);sim.Fighters[0].X=-3;sim.Fighters[1].X=3;hp=sim.Fighters[1].Health;
        sim.Step(new FighterInput{basicAttack=true},default);
        Check(sim.Fighters[1].Health<hp,"Swept collision detects fast projectile crossing target");
        sim=Setup();sim.Step(new FighterInput{ultimate=true},default);
        for(int i=0;i<Ticks(1);i++)sim.Step(new FighterInput{ultimate=true},default);
        Check(sim.Fighters[0].IsCharging&&sim.Fighters[0].Charge>.5&&sim.Projectiles.Count==0&&sim.Fighters[0].AbilitiesUsed==0,"Held ultimate charges visibly without firing early");
        sim.Step();Check(!sim.Fighters[0].IsCharging&&sim.Projectiles.Any(p=>p.IsUltimate)&&sim.Fighters[0].AbilityUsage[sim.Fighters[0].Avatar.abilityIds[2]]==1,"Release launches charged ultimate and starts cooldown");
        double partialPower=sim.Projectiles.First(p=>p.IsUltimate).Power;
        sim=Setup();for(int i=0;i<Ticks(3);i++)sim.Step(new FighterInput{ultimate=true},default);
        Check(sim.Fighters[0].Charge==1&&sim.Projectiles.Count==0,"Full charge stays held until release");sim.Step();
        Check(sim.Projectiles.First(p=>p.IsUltimate).Power>partialPower,"Full charge yields greater projectile power than partial charge");
        sim=Setup();sim.Step(new FighterInput{ultimate=true},default);sim.Step();
        Check(!sim.Fighters[0].IsCharging&&sim.Fighters[0].AbilitiesUsed==0,"Too-short charge cancels without skill expenditure");
        sim=Setup();sim.Step(new FighterInput{ultimate=true},default);for(int i=0;i<Ticks(0.5);i++)sim.Step(new FighterInput{ultimate=true},default);
        sim.Step(new FighterInput{ultimate=true,dodge=true,moveZ=1},default);
        Check(!sim.Fighters[0].IsCharging&&sim.Fighters[0].Charge==0&&sim.Projectiles.Count==0,"Dodge cancels charge without firing");
        var replayData=Clone();replayData.balance.ultimateUnlockSeconds=0;replayData.balance.maxDuration=4;foreach(var a in replayData.abilities)if(a.isUltimate)a.range=30;
        sim=new BattleSimulation(replayData,cfg);while(!sim.IsFinished)sim.Step(new FighterInput{ultimate=sim.Tick<24,moveX=sim.Tick<8?1:0},default);
        Check(sim.Result.fighterA.abilityCounts[2]==1&&JsonSerializer.Serialize(sim.Result,Json)==JsonSerializer.Serialize(new BattleSimulation(replayData,sim.Result.config,sim.Result.inputs).RunToEnd(),Json),"Charged ultimate input recording replays exactly");
        var statusData=Clone();statusData.balance.healthRegenPerGreen=0;
        var skill=statusData.abilities.First(a=>a.id==statusData.avatars.First(a=>a.cityId=="muenster").abilityIds[0]);skill.delivery="Projectile";skill.projectileSpeed=9;skill.effectType="Slow";skill.statusEffect="DamageOverTime";skill.castTime=0;skill.energyCost=0;
        sim=new BattleSimulation(statusData,cfg);sim.Fighters[0].X=-1;sim.Fighters[1].X=1;hp=sim.Fighters[1].Health;
        sim.Step(new FighterInput{skill1=true},new FighterInput{dodge=true,moveZ=1});for(int i=0;i<Ticks(0.4);i++)sim.Step();
        Check(sim.Fighters[1].Health==hp&&sim.Fighters[1].Effects.Count==0,"Dodged skill applies neither damage nor secondary status");
    }
    static void TestSkillDepth()
    {
        var data=Clone();data.balance.healthRegenPerGreen=0;data.balance.criticalChance=0;data.balance.damageVariance=0;data.balance.ultimateUnlockSeconds=0;
        var cfg=Config();cfg.mode=MatchMode.PlayerVsPlayer;
        var avatar=data.avatars.First(a=>a.cityId=="muenster");var skill=data.abilities.First(a=>a.id==avatar.abilityIds[0]);
        skill.effectType="Damage";skill.statusEffect=null;skill.onHitSelfEffect=null;skill.onHitTargetEffect=null;skill.energyCost=0;skill.castTime=.2;skill.range=3;skill.areaOfEffect=2;skill.baseDamage=100;skill.delivery="Melee";
        BattleSimulation Setup(){var s=new BattleSimulation(data,cfg);s.Obstacles.Clear();s.Fighters[0].X=0;s.Fighters[1].X=2;return s;}
        var sim=Setup();var hp=sim.Fighters[1].Health;sim.Step(new FighterInput{skill1=true},default);for(int i=0;i<Ticks(0.3);i++)sim.Step();Check(sim.Fighters[1].Health<hp&&sim.Projectiles.Count==0,"Melee skill strikes directly after windup");
        sim=Setup();hp=sim.Fighters[1].Health;sim.Step(new FighterInput{skill1=true},default);sim.Fighters[1].X=-2;for(int i=0;i<Ticks(0.3);i++)sim.Step();Check(sim.Fighters[1].Health==hp,"Leaving the aimed melee cone avoids its strike");
        sim=Setup();hp=sim.Fighters[1].Health;sim.Step(new FighterInput{skill1=true},default);sim.Fighters[1].X=4;for(int i=0;i<Ticks(0.3);i++)sim.Step();Check(sim.Fighters[1].Health==hp,"Leaving melee range during windup avoids damage");
        skill.castTime=0;sim=Setup();hp=sim.Fighters[1].Health;sim.Fighters[1].DodgeRemaining=.2;sim.Step(new FighterInput{skill1=true},default);Check(sim.Fighters[1].Health==hp,"Dodge invulnerability avoids melee skill");
        skill.delivery="Dash";skill.range=8;skill.dashDistance=5;sim=Setup();sim.Fighters[1].X=6;hp=sim.Fighters[1].Health;sim.Step(new FighterInput{skill1=true},default);Check(sim.Fighters[0].X>3&&sim.Fighters[1].Health<hp,"Dash closes a gap then strikes at melee range");
        sim=Setup();sim.Fighters[1].X=6;sim.Obstacles.Add(new ArenaObstacle{X=3,Z=0,Radius=1});hp=sim.Fighters[1].Health;sim.Step(new FighterInput{skill1=true},default);Check(sim.Fighters[0].X<1.6&&sim.Fighters[1].Health==hp,"Dash cannot cross solid cover or strike through it");
        sim=Setup();sim.Fighters[1].X=8;sim.Obstacles.Add(new ArenaObstacle{X=3,Z=0,Radius=1});for(int i=0;i<Ticks(2);i++)sim.Step(new FighterInput{moveX=1},default);Check(sim.Fighters[0].X<1.6,"Walking stops at obstacle collision radius");
        sim=Setup();sim.Fighters[1].X=8;sim.Obstacles.Add(new ArenaObstacle{X=3,Z=0,Radius=1});sim.Step(new FighterInput{moveX=1,dodge=true},default);for(int i=0;i<Ticks(0.4);i++)sim.Step();Check(sim.Fighters[0].X<1.6,"Dodge cannot tunnel through cover");
        sim=Setup();sim.Fighters[1].X=6;sim.Obstacles.Add(new ArenaObstacle{X=3,Z=0,Radius=1});hp=sim.Fighters[1].Health;sim.Step(new FighterInput{basicAttack=true},default);for(int i=0;i<Ticks(1);i++)sim.Step();Check(sim.Fighters[1].Health==hp&&sim.Projectiles.Count==0&&!sim.HasLineOfSight(0,1),"Cover blocks swept projectiles before the target");
        skill.delivery="Melee";skill.effectType="Knockback";skill.range=3;skill.magnitude=8;sim=Setup();sim.Obstacles.Add(new ArenaObstacle{X=4,Z=0,Radius=1});sim.Step(new FighterInput{skill1=true},default);Check(sim.Fighters[1].X<2.6,"Knockback cannot push a fighter through cover");
        skill.delivery="Zone";skill.effectType="AreaDamage";skill.range=10;skill.telegraphSeconds=.7;skill.zoneDuration=2;skill.areaOfEffect=1.5;
        sim=Setup();hp=sim.Fighters[1].Health;sim.Step(new FighterInput{skill1=true},default);for(int i=0;i<Ticks(0.5);i++)sim.Step();Check(sim.Areas.Count==1&&sim.Fighters[1].Health==hp,"Ground warning deals no early damage");
        for(int i=0;i<Ticks(3);i++)sim.Step();double damage=hp-sim.Fighters[1].Health;Check(damage>0&&sim.Areas.Count==0,"Persistent zone pulses then expires");
        sim=Setup();hp=sim.Fighters[1].Health;sim.Step(new FighterInput{skill1=true},default);for(int i=0;i<Ticks(3.5);i++)sim.Step(default,new FighterInput{moveZ=1});Check(sim.Fighters[1].Health==hp,"Moving out of a warned area avoids all zone damage");
        var towards=Setup();towards.Step(new FighterInput{moveX=1},default);var away=Setup();away.Step(new FighterInput{moveX=-1},default);Check(Math.Abs(away.Fighters[0].X)<towards.Fighters[0].X,"Backpedalling is slower than closing distance");
        var side=Setup();side.Step(new FighterInput{moveZ=1},default);Check(Math.Abs(side.Fighters[0].Z-towards.Fighters[0].X)<1e-9,"Lateral dodging retains full walking speed");
        data.balance.ringStartSeconds=0;data.balance.ringEndSeconds=1;data.balance.ringFinalRadius=3;sim=Setup();sim.Fighters[0].X=-10;sim.Fighters[1].X=0;hp=sim.Fighters[0].Health;for(int i=0;i<Ticks(2.5);i++)sim.Step();Check(sim.SafeRadius==3&&sim.Fighters[0].Health<hp&&sim.Fighters[1].Health==sim.Fighters[1].Stats.MaxHealth,"Shrinking arena damages only fighters outside safety");
        var cpuCfg=Config();var cpuData=Clone();foreach(var a in cpuData.abilities)a.energyCost=1000000;foreach(var a in cpuData.avatars)a.attackRange=1.5;
        sim=new BattleSimulation(cpuData,cpuCfg);sim.Obstacles.Clear();sim.Obstacles.Add(new ArenaObstacle{X=0,Z=0,Radius=1.5});sim.Fighters[0].X=-3;sim.Fighters[1].X=3;bool inside=false;for(int i=0;i<Ticks(9);i++){sim.Step();inside|=sim.Fighters.Any(f=>f.X*f.X+f.Z*f.Z<Math.Pow(1.5+cpuData.balance.fighterHitRadius-.02,2));}Console.WriteLine("Route check inside="+inside+" fighters="+string.Join(";",sim.Fighters.Select(f=>$"{f.X:F2},{f.Z:F2} attacks={f.BasicAttacks} state={f.State}")));Check(!inside&&sim.Fighters.Sum(f=>f.BasicAttacks)>0,"CPU routes around blocking cover and resumes combat");
        sim=Setup();sim.Fighters[0].X=-10;for(int i=0;i<Ticks(1.5);i++)sim.Step();sim.Fighters[0].Health=.1;sim.Fighters[0].Stats.HealthRegeneration=100;sim.Step();Check(sim.IsFinished&&sim.Fighters[0].Health==0&&sim.Fighters[0].State==CombatState.Dead,"Lethal ring damage cannot regenerate a dead fighter");
        sim=new BattleSimulation(cpuData,cpuCfg);sim.Obstacles.Clear();sim.Obstacles.Add(new ArenaObstacle{X=0,Z=0,Radius=1.5});sim.Fighters[0].X=-(1.5+cpuData.balance.fighterHitRadius+.005);sim.Fighters[1].X=3;for(int i=0;i<Ticks(12);i++)sim.Step();Check(sim.Fighters.Sum(f=>f.BasicAttacks)>0,"CPU escapes contact with cover and routes to its opponent");
        var runnerCfg=Config("muenster","bonn");runnerCfg.mode=MatchMode.PlayerVsCpu;sim=new BattleSimulation(Data,runnerCfg);double runnerHp=sim.Fighters[0].Health;bool engaged=false;while(!sim.IsFinished&&sim.Elapsed<80){double x=sim.Fighters[0].X-sim.Fighters[1].X,z=sim.Fighters[0].Z-sim.Fighters[1].Z;sim.Step(new FighterInput{moveX=x,moveZ=z},default);engaged|=sim.Fighters[1].BasicAttacks>0||sim.Fighters[1].DamageDealt>0;}Check(engaged&&sim.Fighters[0].Health<runnerHp,"CPU engages a fleeing human and the arena prevents endless escape");
        foreach(string delivery in new[]{"Projectile","Melee","Dash","Zone","Self"})
        {
            var replayData=Clone();replayData.balance.maxDuration=3;replayData.balance.ultimateUnlockSeconds=0;var ultimate=replayData.abilities.First(a=>a.id==replayData.avatars.First(a=>a.cityId=="muenster").abilityIds[2]);ultimate.delivery=delivery;ultimate.range=30;
            if(delivery=="Self")ultimate.effectType="Shield";
            sim=new BattleSimulation(replayData,cfg);while(!sim.IsFinished)sim.Step(new FighterInput{ultimate=sim.Tick<20},default);
            Check(sim.Result.fighterA.abilityCounts[2]==1&&JsonSerializer.Serialize(sim.Result,Json)==JsonSerializer.Serialize(new BattleSimulation(replayData,sim.Result.config,sim.Result.inputs).RunToEnd(),Json),"Charged delivery replay exact: "+delivery);
        }
    }
    static void TestCombosAndTerrain()
    {
        var data=Clone();data.balance.healthRegenPerGreen=0;data.balance.ultimateUnlockSeconds=0;data.balance.damageVariance=0;data.balance.criticalChance=0;
        var cfg=Config();cfg.mode=MatchMode.PlayerVsPlayer;
        var avatar=data.avatars.First(a=>a.cityId==cfg.cityAId);var skill=data.abilities.First(a=>a.id==avatar.abilityIds[0]);var ult=data.abilities.First(a=>a.id==avatar.abilityIds[2]);
        skill.delivery="Projectile";skill.effectType="Stun";skill.statusEffect=null;skill.onHitSelfEffect=null;skill.onHitTargetEffect=null;skill.range=15;skill.projectileSpeed=20;skill.projectileRadius=.4;skill.castTime=0;skill.energyCost=0;skill.duration=1.3;skill.baseDamage=20;
        skill.onHitSelfEffect="Focus";skill.onHitSelfDuration=3;skill.onHitSelfMagnitude=.6;skill.onHitTargetEffect=null;
        ult.delivery="Projectile";ult.effectType="Damage";ult.statusEffect=null;ult.projectileSpeed=22;ult.range=15;ult.energyCost=0;ult.onHitSelfEffect=null;ult.onHitTargetEffect=null;
        BattleSimulation Setup(){var s=new BattleSimulation(data,cfg);s.Obstacles.Clear();s.TerrainZones.Clear();s.Fighters[0].X=-2;s.Fighters[1].X=2;return s;}
        void Hit(BattleSimulation s){s.Step(new FighterInput{skill1=true},default);for(int i=0;i<Ticks(0.2);i++)s.Step();}
        var sim=Setup();var enemy=sim.Fighters[1];enemy.IsCharging=true;enemy.ChargingAbilityId=enemy.Avatar.abilityIds[2];enemy.Charge=.5;enemy.CastingAbilityId=enemy.Avatar.abilityIds[0];enemy.CastRemaining=5;
        Hit(sim);Check(BattleSimulation.HasStatus(enemy,"Stun")&&!enemy.IsCharging&&enemy.CastingAbilityId==null,"Stun hit interrupts cast and charge");
        Check(BattleSimulation.HasStatus(sim.Fighters[0],"Focus")&&enemy.ControlImmunityRemaining>2,"Successful setup grants self Focus and target control immunity");
        var x=enemy.X;var z=enemy.Z;int attacks=enemy.BasicAttacks;sim.Step(default,new FighterInput{moveZ=1,basicAttack=true,skill1=true,ultimate=true,dodge=true});
        Check(enemy.X==x&&enemy.Z==z&&enemy.BasicAttacks==attacks&&!enemy.IsCharging&&enemy.DodgeRemaining==0,"Stun locks movement attacks charge and dodge");
        double immunity=enemy.ControlImmunityRemaining;sim.Fighters[0].Cooldowns[skill.id]=0;Hit(sim);
        Check(enemy.ControlImmunityRemaining<immunity,"Repeated stun cannot refresh immunity or extend control");
        for(int i=0;i<Ticks(1.5);i++)sim.Step();Check(!BattleSimulation.HasStatus(enemy,"Stun")&&enemy.ControlImmunityRemaining>0,"Stun expires before anti-chain immunity");
        sim.Step(default,new FighterInput{moveZ=1});Check(enemy.Z>z,"Movement resumes after stun");
        sim=Setup();sim.Step(new FighterInput{skill1=true},new FighterInput{dodge=true,moveZ=1});for(int i=0;i<Ticks(0.75);i++)sim.Step();
        Check(!BattleSimulation.HasStatus(sim.Fighters[0],"Focus")&&!BattleSimulation.HasStatus(sim.Fighters[1],"Stun"),"Dodged setup grants neither self buff nor stun");
        sim=Setup();sim.Obstacles.Add(new ArenaObstacle{X=0,Z=0,Radius=.6});Hit(sim);
        Check(!BattleSimulation.HasStatus(sim.Fighters[0],"Focus"),"Cover-blocked setup grants no on-hit self buff");
        skill.effectType="Root";sim=Setup();Hit(sim);enemy=sim.Fighters[1];enemy.Avatar.attackRange=10;x=enemy.X;z=enemy.Z;sim.Step(default,new FighterInput{moveZ=1,basicAttack=true,dodge=true});
        Check(enemy.X==x&&enemy.Z==z&&enemy.DodgeRemaining==0&&enemy.BasicAttacks==1,"Root blocks movement and dodge but allows attacks");
        skill.effectType="Stun";sim=Setup();Hit(sim);enemy=sim.Fighters[1];double hp=enemy.Health;for(int i=0;i<Ticks(0.25);i++)sim.Step();
        sim.Step(new FighterInput{ultimate=true},default);for(int i=0;i<Ticks(0.75);i++)sim.Step(new FighterInput{ultimate=true},default);
        Check(sim.Fighters[0].Charge==1,"On-hit Focus fully charges ultimate within 0.75 seconds");
        sim.Step();for(int i=0;i<Ticks(0.25);i++)sim.Step(default,new FighterInput{moveZ=1});
        Check(enemy.Health<hp,"Stun into focused charged projectile lands before target escapes");
        sim=Setup();sim.Step(new FighterInput{ultimate=true},default);for(int i=0;i<Ticks(0.75);i++)sim.Step(new FighterInput{ultimate=true},new FighterInput{moveZ=1});
        Check(sim.Fighters[0].Charge<.5&&sim.Fighters[0].AimZ>.1,"Unbuffed charge takes longer and aim tracks until release");
        sim.Step();var projectile=sim.Projectiles.First(p=>p.IsUltimate);double direction=projectile.DirectionZ;sim.Step(default,new FighterInput{moveZ=-1});
        Check(projectile.DirectionZ==direction,"Released ultimate never homes after aim lock");
        skill.effectType="Damage";skill.onHitSelfEffect=null;skill.onHitTargetEffect="Vulnerable";skill.onHitTargetMagnitude=.3;skill.onHitTargetDuration=3;
        sim=Setup();Hit(sim);hp=sim.Fighters[1].Health;sim.Fighters[0].RecoveryRemaining=0;sim.Step(new FighterInput{basicAttack=true},default);for(int i=0;i<Ticks(0.6);i++)sim.Step();double increased=hp-sim.Fighters[1].Health;
        skill.onHitTargetEffect=null;sim=Setup();Hit(sim);hp=sim.Fighters[1].Health;sim.Fighters[0].RecoveryRemaining=0;sim.Step(new FighterInput{basicAttack=true},default);for(int i=0;i<Ticks(0.6);i++)sim.Step();
        Check(increased>hp-sim.Fighters[1].Health,"Vulnerable increases subsequent direct damage");
        ult.delivery="Self";ult.effectType="Buff";ult.duration=4;ult.magnitude=.3;ult.statusEffect="Haste";
        sim=Setup();for(int i=0;i<Ticks(0.4);i++)sim.Step(new FighterInput{ultimate=true},default);sim.Step();double shortDuration=sim.Fighters[0].Effects.First(e=>e.Type=="Buff").Remaining;
        sim=Setup();for(int i=0;i<Ticks(2);i++)sim.Step(new FighterInput{ultimate=true},default);sim.Step();
        Check(sim.Fighters[0].Effects.First(e=>e.Type=="Buff").Remaining>shortDuration&&BattleSimulation.HasStatus(sim.Fighters[0],"Haste")&&!BattleSimulation.HasStatus(sim.Fighters[1],"Haste"),"Charging self ultimate extends buff and positive secondary targets caster");
        foreach(var climate in data.environments)
        {
            var zones=ArenaLayout.CreateZones(123,12,climate.id);Check(zones.Count==4&&zones.All(q=>zones.Any(r=>r.Kind==q.Kind&&r.X==-q.X&&r.Z==-q.Z))&&JsonSerializer.Serialize(zones,Json)==JsonSerializer.Serialize(ArenaLayout.CreateZones(123,12,climate.id),Json),"Terrain zones symmetric and deterministic: "+climate.id);
        }
        sim=Setup();var f=sim.Fighters[0];f.Health-=100;f.Energy-=50;hp=f.Health;double energy=f.Energy;
        sim.TerrainZones.Add(new TerrainZone{Kind="Regen",X=f.X,Z=f.Z,Radius=1});for(int i=0;i<Ticks(1);i++)sim.Step();
        Check(f.Health>hp&&f.Energy>energy,"Beneficial terrain regenerates health and energy");
        sim.TerrainZones.Clear();sim.TerrainZones.Add(new TerrainZone{Kind="Heat",X=f.X,Z=f.Z,Radius=1});hp=f.Health;for(int i=0;i<Ticks(1);i++)sim.Step();Check(f.Health<hp,"Hazard terrain deals continuous damage");
        f.X=-8;hp=f.Health;for(int i=0;i<Ticks(1);i++)sim.Step();Check(f.Health==hp,"Leaving heat terrain stops damage");
        foreach(var kind in new[]{"Focus","Haste","Mud"})
        {
            sim=Setup();f=sim.Fighters[0];sim.TerrainZones.Add(new TerrainZone{Kind=kind,X=f.X,Z=f.Z,Radius=1});sim.Step();string status=kind=="Mud"?"Slow":kind;
            Check(BattleSimulation.HasStatus(f,status),"Terrain applies "+status);f.X=-8;for(int i=0;i<Ticks(0.25);i++)sim.Step();Check(!BattleSimulation.HasStatus(f,status),"Terrain "+status+" expires promptly after exit");
        }
        var routeData=Clone();foreach(var ability in routeData.abilities)ability.energyCost=1000000;
        foreach(var routeAvatar in routeData.avatars){routeAvatar.attackRange=1.8;routeAvatar.attackStyle="Melee";}
        var routeConfig=Config();routeConfig.mode=MatchMode.PlayerVsCpu;
        var routing=new BattleSimulation(routeData,routeConfig);routing.Obstacles.Clear();routing.TerrainZones.Clear();
        routing.TerrainZones.Add(new TerrainZone{Kind="Heat",X=0,Z=0,Radius=1.4});
        routing.Fighters[0].X=4;routing.Fighters[0].Z=0;routing.Fighters[1].X=-4;routing.Fighters[1].Z=0;
        bool stayedClear=true;
        for(int i=0;i<400&&!routing.IsFinished&&routing.Fighters[1].BasicAttacks==0;i++){routing.Step();var cpu=routing.Fighters[1];stayedClear&=cpu.X*cpu.X+cpu.Z*cpu.Z>1.4*1.4;}
        Check(stayedClear&&routing.Fighters[1].BasicAttacks>0,"CPU routes around a hazard and reaches attack range without edge oscillation");
        var invalid=Clone();invalid.abilities[0].onHitTargetEffect="Unknown";Reject(()=>new BattleSimulation(invalid,cfg),"Unknown on-hit status rejected");
    }
    static void TestBeginnerCombat()
    {
        BattleSimulation Setup(){var c=Config("bonn","muenster");c.mode=MatchMode.PlayerVsPlayer;var s=new BattleSimulation(Data,c);s.Obstacles.Clear();s.TerrainZones.Clear();s.Fighters[0].X=0;s.Fighters[0].Z=0;s.Fighters[1].X=4;s.Fighters[1].Z=0;return s;}
        var approach=Setup();approach.Step(new FighterInput{basicAttack=true},default);
        Check(approach.Fighters[0].X>0,"Held attack approaches nearby enemy without stick input");
        var manual=Setup();manual.Step(new FighterInput{basicAttack=true,moveX=-1},default);
        Check(manual.Fighters[0].X<0,"Manual retreat overrides attack approach");
        var far=Setup();far.Fighters[1].X=9;far.Step(new FighterInput{basicAttack=true},default);
        Check(far.Fighters[0].X==0,"Attack approach never chases distant targets automatically");
        var dead=Setup();dead.Fighters[0].Health=0;dead.Step();
        Check(dead.IsFinished&&dead.Result.outcome=="Knockout"&&dead.Result.winnerIndex==1,"First zero health ends the match as knockout");
        double time=dead.Elapsed;dead.Step(new FighterInput{basicAttack=true,skill1=true,skill2=true},default);
        Check(dead.Elapsed==time&&dead.Fighters[0].Health==0,"Finished match cannot revive or continue on attack input");
    }
    static void TestDifficulty()
    {
        var bad=Config();bad.difficulty=(CpuDifficulty)99;Reject(()=>new BattleSimulation(Data,bad),"Invalid difficulty rejected");
        foreach(var difficulty in new[]{CpuDifficulty.Easy,CpuDifficulty.Normal,CpuDifficulty.Hard})
        {
            var config=Config();config.difficulty=difficulty;config.mode=MatchMode.PlayerVsCpu;
            var sim=new BattleSimulation(Data,config);var before=sim.Fighters[1].Stats.MaxHealth;
            while(!sim.IsFinished)sim.Step(new FighterInput{basicAttack=true,skill1=sim.Tick%20==0,ultimate=sim.Tick%100<30},default);
            var replay=new BattleSimulation(Data,sim.Result.config,sim.Result.inputs).RunToEnd();
            Check(JsonSerializer.Serialize(sim.Result,Json)==JsonSerializer.Serialize(replay,Json),"Difficulty survives exact input replay: "+difficulty);
            Check(sim.Result.config.difficulty==difficulty&&before==new BattleSimulation(Data,Config()).Fighters[1].Stats.MaxHealth,"Difficulty preserved without CPU health bonus: "+difficulty);
        }
        BattleSimulation Threat(CpuDifficulty difficulty)
        {
            var config=Config();config.difficulty=difficulty;var sim=new BattleSimulation(Data,config);sim.Obstacles.Clear();sim.TerrainZones.Clear();
            var f=sim.Fighters[0];sim.Projectiles.Add(new ProjectileState{Id=2,Source=1,Target=0,X=f.X+2,Z=f.Z,DirectionX=-1,Speed=9,Radius=.2,Age=.14,Remaining=2,Power=1});return sim;
        }
        var easy=Threat(CpuDifficulty.Easy);var normal=Threat(CpuDifficulty.Normal);var hard=Threat(CpuDifficulty.Hard);
        easy.Step();normal.Step();hard.Step();
        Check(hard.Fighters[0].DodgeRemaining>0&&normal.Fighters[0].DodgeRemaining==0&&easy.Fighters[0].DodgeRemaining==0,"Hard reacts to a young visible projectile before Normal or Easy");
        var playerEasy=Config();playerEasy.mode=MatchMode.PlayerVsPlayer;playerEasy.difficulty=CpuDifficulty.Easy;
        var playerHard=Config();playerHard.mode=MatchMode.PlayerVsPlayer;playerHard.difficulty=CpuDifficulty.Hard;
        var a=new BattleSimulation(Data,playerEasy);var b=new BattleSimulation(Data,playerHard);
        for(int tick=0;tick<100;tick++){var input=new FighterInput{moveX=1,basicAttack=true,skill1=tick%20==0};a.Step(input,default);b.Step(input,default);}
        Check(a.Fighters[0].X==b.Fighters[0].X&&a.Fighters[1].Health==b.Fighters[1].Health,"Difficulty has no effect on human-only matches");
    }
    static void TestResponsiveness()
    {
        var data=Clone();var cfg=Config();cfg.mode=MatchMode.PlayerVsPlayer;
        var avatar=data.avatars.First(a=>a.id==cfg.avatarAId);
        foreach(var id in avatar.abilityIds.Take(2)){var ability=data.abilities.First(a=>a.id==id);ability.castTime=.3;ability.delivery="Melee";ability.range=10;ability.effectType="Damage";ability.energyCost=0;ability.onHitSelfEffect=null;ability.onHitTargetEffect=null;}
        BattleSimulation Setup(){var s=new BattleSimulation(data,cfg);s.Obstacles.Clear();s.TerrainZones.Clear();s.Fighters[0].X=-1;s.Fighters[1].X=1;s.Fighters[0].Z=s.Fighters[1].Z=0;return s;}
        Check(data.balance.tickSeconds<=1.0/60+1e-9,"Live simulation reacts at 60 Hz");
        var sim=Setup();var f=sim.Fighters[0];f.RecoveryRemaining=.12;
        sim.Step(new FighterInput{skill1=true},default);for(int i=0;i<Ticks(.15);i++)sim.Step();
        Check(f.AbilitiesUsed==1&&f.CastingAbilityId==avatar.abilityIds[0],"Early skill tap executes once after recovery without repeated presses");
        sim=Setup();f=sim.Fighters[0];f.RecoveryRemaining=.6;sim.Step(new FighterInput{skill1=true},default);
        for(int i=0;i<Ticks(.8);i++)sim.Step();Check(f.AbilitiesUsed==0,"Expired skill tap cannot cause a delayed surprise attack");
        sim=Setup();f=sim.Fighters[0];f.RecoveryRemaining=.12;sim.Step(new FighterInput{skill1=true},default);sim.Step(new FighterInput{skill2=true},default);
        for(int i=0;i<Ticks(.15);i++)sim.Step();Check(f.AbilitiesUsed==1&&f.CastingAbilityId==avatar.abilityIds[1],"Latest skill replaces pending action instead of creating a spam queue");
        sim=Setup();f=sim.Fighters[0];sim.Step(new FighterInput{skill1=true},default);double z=f.Z;
        sim.Step(new FighterInput{moveZ=1},default);Check(f.Z>z&&f.CastingAbilityId!=null,"Player can reposition during skill windup");
        double cooldown=f.Cooldowns[avatar.abilityIds[0]];
        sim.Step(new FighterInput{dodge=true,moveZ=1},default);
        Check(f.CastingAbilityId==null&&f.DodgeRemaining>0&&f.Cooldowns[avatar.abilityIds[0]]>cooldown-.1,"Dodge cancels windup without refunding its cooldown");
        sim=Setup();f=sim.Fighters[0];sim.Step(new FighterInput{basicAttack=true},default);
        sim.Step(new FighterInput{skill1=true},default);for(int i=0;i<Ticks(.18);i++)sim.Step();
        Check(f.BasicAttacks==1&&f.AbilitiesUsed==1,"Basic attack chains into a skill without a 1.2 second input lock");
        sim=Setup();f=sim.Fighters[0];for(int i=0;i<Ticks(.8);i++)sim.Step(new FighterInput{basicAttack=true},default);
        Check(f.BasicAttacks==1,"Short recovery does not bypass the basic attack cooldown");
    }
    static GameData Clone() => JsonSerializer.Deserialize<GameData>(JsonSerializer.Serialize(Data,Json),Json);
    static void TestEffect(string effect)
    {
        var data=Clone(); data.balance.dodgeEnergyCost=1000000; data.balance.baseAttack=0;data.balance.attackPerPower=0;data.balance.healthRegenPerGreen=0;data.balance.dodgeChance=0;data.balance.criticalChance=0;data.balance.damageVariance=0;
        foreach(var ability in data.abilities) ability.energyCost=1000000;
        var avatar=data.avatars.First(a=>a.cityId=="muenster");
        var skill=data.abilities.First(a=>a.id==avatar.abilityIds[0]);
        skill.delivery="Projectile";skill.projectileSpeed=9;skill.effectType=effect;skill.statusEffect=null;skill.onHitSelfEffect=null;skill.onHitTargetEffect=null;skill.cooldown=100;skill.castTime=.1;skill.energyCost=0;skill.baseDamage=100;skill.range=20;skill.duration=2;skill.magnitude=.5;
        var effectConfig=Config();effectConfig.mode=MatchMode.PlayerVsPlayer;
        var sim=new BattleSimulation(data,effectConfig);var a=sim.Fighters[0];var b=sim.Fighters[1]; a.X=-1;b.X=1;
        a.Health-=200;double initialA=a.Health,initialB=b.Health,initialX=b.X;
        sim.Step(new FighterInput{skill1=true},default); Check(a.CastingAbilityId==skill.id&&a.AbilitiesUsed==1&&b.Health==initialB,"Cast starts before effect: "+effect);
        for(int i=0;i<Ticks(0.3);i++)sim.Step();
        bool applied=effect=="Heal"?a.Health>initialA:effect=="Shield"?a.Shield>0:effect=="Buff"?a.Effects.Any(e=>e.Type=="Buff"):effect=="Debuff"?b.Effects.Any(e=>e.Type=="Debuff"):effect=="Slow"?b.Effects.Any(e=>e.Type=="Slow"):effect=="Knockback"?b.X>initialX:b.Health<initialB;
        Check(applied,"Generic effect resolves: "+effect);
        double cooldown=a.Cooldowns[skill.id];sim.Step();
        Check(a.Cooldowns[skill.id]<cooldown&&a.AbilityUsage[skill.id]==1,"Cooldown ticks without recast: "+effect);
        if(effect=="Slow"||effect=="Buff"||effect=="Debuff"||effect=="DamageOverTime")
        {for(int i=0;i<Ticks(2.5);i++)sim.Step();Check(a.Effects.Count==0&&b.Effects.Count==0,"Status expires: "+effect);}
    }
}

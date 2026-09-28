using System;
using System.Collections.Generic;
using System.Reflection;
namespace ClashOfCities.Core
{
    public static class DataValidator
    {
        static void Require(bool condition,string message) { if(!condition) throw new ArgumentException(message); }
        static HashSet<string> Ids<T>(T[] entries,Func<T,string> id,string label) where T:class
        {
            Require(entries!=null&&entries.Length>0,label+" must not be empty."); var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var entry in entries) { Require(entry!=null,label+" contains null."); string key=id(entry); Require(!string.IsNullOrWhiteSpace(key)&&ids.Add(key),label+" IDs must be nonempty and unique."); Finite(entry); } return ids;
        }
        static void Finite(object value) { foreach(var f in value.GetType().GetFields()) if(f.FieldType==typeof(double)) { double d=(double)f.GetValue(value); Require(!double.IsNaN(d)&&!double.IsInfinity(d)&&d>=0&&d<=1000000,"Invalid numeric field: "+f.Name); } }
        public static void Validate(GameData data)
        {
            Require(data!=null&&data.balance!=null,"Missing game data/balance."); var cities=Ids(data.cities,c=>c.id,"Cities"); Ids(data.avatars,a=>a.id,"Avatars"); var abilities=Ids(data.abilities,a=>a.id,"Abilities"); Ids(data.environments,e=>e.id,"Environments");
            foreach(var c in data.cities) foreach(var f in typeof(CityDefinition).GetFields()) if(f.FieldType==typeof(double)) Require((double)f.GetValue(c)<=100,"City scores must be in [0,100].");
            foreach(var a in data.avatars) { Require(cities.Contains(a.cityId??""),"Unknown avatar city."); Require(a.attackStyle=="Projectile"||a.attackStyle=="Melee","Unknown attack style."); Require(a.baseMovementSpeed>0&&a.attackRange>0,"Avatar movement/range must be positive."); Require(a.abilityIds!=null&&a.abilityIds.Length==3,"Each avatar needs two active abilities and an ultimate."); var used=new HashSet<string>(); for(int i=0;i<3;i++) { Require(abilities.Contains(a.abilityIds[i]??"")&&used.Add(a.abilityIds[i]),"Unknown or duplicate avatar ability."); var ability=Array.Find(data.abilities,x=>x.id==a.abilityIds[i]); Require(ability.isUltimate==(i==2),"Only third ability slot is ultimate."); } }
            foreach(var c in data.cities) Require(Array.Exists(data.avatars,a=>a.cityId==c.id),"Every city needs an avatar.");
            var effects=new HashSet<string>{"Damage","Heal","Shield","Knockback","Slow","Buff","Debuff","AreaDamage","DamageOverTime","Stun","Root","Haste","Focus","Vulnerable"};
            var scaling=new HashSet<string>{"power","green","water","heatProtection","resilience","unsealed"};
            foreach(var a in data.abilities) { Require(a.delivery=="Projectile"||a.delivery=="Melee"||a.delivery=="Dash"||a.delivery=="Zone"||a.delivery=="Self","Unknown skill delivery."); Require(a.coneDegrees>0&&a.coneDegrees<=360&&a.zoneDuration>0,"Invalid skill geometry."); Require(effects.Contains(a.effectType??""),"Unknown ability effect: "+a.effectType); Require(scaling.Contains(a.climateScaling??""),"Unknown climate scaling."); Require(a.cooldown>0&&(a.range>0||a.delivery=="Self"),"Ability cooldown/range must be positive."); Require(string.IsNullOrEmpty(a.statusEffect)||a.statusEffect=="Slow"||a.statusEffect=="Buff"||a.statusEffect=="Debuff"||a.statusEffect=="DamageOverTime"||a.statusEffect=="Stun"||a.statusEffect=="Root"||a.statusEffect=="Haste"||a.statusEffect=="Focus"||a.statusEffect=="Vulnerable","Unknown status effect."); Require(string.IsNullOrEmpty(a.onHitSelfEffect)||new HashSet<string>{"Buff","Haste","Focus","Shield","Heal"}.Contains(a.onHitSelfEffect),"Invalid on-hit self effect."); Require(string.IsNullOrEmpty(a.onHitTargetEffect)||new HashSet<string>{"Stun","Root","Slow","Debuff","Vulnerable","DamageOverTime"}.Contains(a.onHitTargetEffect),"Invalid on-hit target effect."); }
            foreach(var e in data.environments) Require(e.greenWeight+e.waterWeight+e.heatProtectionWeight+e.unsealedWeight+e.resilienceWeight+e.lowHeatRiskWeight>0,"Climate event weights sum to zero.");
            var b=data.balance; Finite(b); Require(b.controlImmunitySeconds>=0&&b.maxStunSeconds>0&&b.maxStunSeconds<=2&&b.maxRootSeconds>0&&b.maxRootSeconds<=3,"Invalid control limits."); Require(b.backpedalMultiplier>0&&b.backpedalMultiplier<=1&&b.ringEndSeconds>b.ringStartSeconds&&b.ringFinalRadius>0,"Invalid arena pressure settings."); Require(b.projectileSpeed>0&&b.projectileRadius>0&&b.fighterHitRadius>0&&b.chargeDuration>0&&b.chargeMinSeconds>0&&b.chargeMinSeconds<=b.chargeDuration,"Invalid projectile or charge settings."); Require(b.dodgeDuration>0&&b.dodgeCooldown>=b.dodgeDuration&&b.dodgeSpeedMultiplier>=1&&b.minimumFighterDistance>0&&b.minimumFighterDistance<b.arenaHalfSize&&b.cpuPreferredRangeRatio>0&&b.cpuPreferredRangeRatio<=1,"Invalid movement or dodge settings."); Require(!string.IsNullOrWhiteSpace(b.version),"Balance version is required."); Require(b.tickSeconds>=.001&&b.tickSeconds<=1&&b.maxDuration>=b.tickSeconds&&b.maxDuration<=3600,"Invalid tick or duration."); Require(b.baseHealth>0&&b.defenseConstant>0&&b.basicAttackInterval>0&&b.arenaHalfSize>0,"Health, defense constant, attack interval and arena size must be positive."); Require(b.healThreshold<=1,"Heal threshold must be a health fraction."); Require(b.criticalChance<=1&&b.dodgeChance<=1&&b.damageVariance<=1&&b.criticalMultiplier>=1,"Invalid probability or damage variance.");
        }
        // Snapshot public data so external edits cannot change a match already in progress.
        internal static T Copy<T>(T source) where T:class,new() { if(source==null)return null; var target=new T(); foreach(var f in typeof(T).GetFields(BindingFlags.Instance|BindingFlags.Public)) { object value=f.GetValue(source); if(value is Array) value=((Array)value).Clone(); f.SetValue(target,value); } return target; }
    }
}

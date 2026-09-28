using System;
namespace ClashOfCities.Core
{
    public static class ClimateCalculator
    {
        // Sealing is sealed surface percentage: less is better. Every input is 0..100.
        public static double Calculate(CityDefinition c, ClimateEventDefinition e)
        {
            if(c==null || e==null) throw new ArgumentNullException();
            double sum=e.greenWeight+e.waterWeight+e.heatProtectionWeight+e.unsealedWeight+e.resilienceWeight+e.lowHeatRiskWeight;
            if(sum<=0) throw new ArgumentException("Climate weights must have positive sum.");
            return (c.greenScore*e.greenWeight+c.waterScore*e.waterWeight+c.heatProtectionScore*e.heatProtectionWeight+(100-c.sealingScore)*e.unsealedWeight+c.resilienceScore*e.resilienceWeight+(100-c.heatRiskScore)*e.lowHeatRiskWeight)/sum;
        }
        public static CombatStats Map(CityDefinition c, ClimateEventDefinition e, BalanceConfig b)
        {
            double p=Calculate(c,e);
            return new CombatStats { ClimatePower=p, MaxHealth=b.baseHealth+p*b.healthPerPower, AttackPower=b.baseAttack+p*b.attackPerPower,
                Defense=b.baseDefense+c.greenScore*b.defensePerGreen+c.resilienceScore*b.defensePerResilience,
                Energy=b.baseEnergy+c.waterScore*b.energyPerWater, EnergyRegeneration=b.baseEnergyRegen+c.waterScore*b.energyRegenPerWater,
                HealthRegeneration=c.greenScore*b.healthRegenPerGreen, HeatResistance=c.heatProtectionScore/100,
                MovementStamina=1+(100-c.sealingScore)*b.staminaPerUnsealed, CooldownModifier=Math.Max(.1,1-c.resilienceScore*b.cooldownReductionPerResilience) };
        }
    }
    public static class DamageCalculator
    {
        public static double Calculate(double raw, double defense, BalanceConfig b)
        {
            if(double.IsNaN(raw)||double.IsInfinity(raw)||double.IsNaN(defense)||double.IsInfinity(defense)||b==null||b.defenseConstant<=0||double.IsInfinity(b.defenseConstant)||double.IsNaN(b.defenseConstant)) throw new ArgumentException("Damage inputs must be finite; defense constant must be positive.");
            return Math.Max(0,raw)*(b.defenseConstant/(b.defenseConstant+Math.Max(0,defense)));
        }
    }
    // Explicit uint algorithm is stable across Unity/Mono/.NET versions, including seed zero.
    public sealed class SeededRandom
    {
        uint state;
        public SeededRandom(uint seed) { state=seed; }
        uint Next() { unchecked { state+=0x9e3779b9u; uint z=state; z=(z^(z>>16))*0x21f0aaadu; z=(z^(z>>15))*0x735a2d97u; return z^(z>>15); } }
        public double NextDouble() { return Next()/4294967296.0; }
        public int NextInt(int exclusiveMax) { if(exclusiveMax<=0) throw new ArgumentOutOfRangeException(nameof(exclusiveMax)); uint bound=(uint)exclusiveMax; uint threshold=unchecked(0u-bound)%bound; uint r; do { r=Next(); } while(r<threshold); return (int)(r%bound); }
    }
}

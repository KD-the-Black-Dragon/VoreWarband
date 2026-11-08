// Added by this fork for the VoreWar mercenary-company conversion.
// 2026-11-06: Score new summon, control, healing and area-vore spell options (spec 40C).
// Added by this fork for the VoreWar mercenary-company conversion.
// 2026-11-06: Score the actual frozen AP of existing characters (spec 40A).
// 2026-11-05: Remove the retired Flight capability estimate.
// 2026-11-05: Include rescued grazes and KeenEye in expected weapon capability (spec 38 phase A).
// 2026-11-04: Explainable healthy capability scoring, bounded health scaling and force estimates (spec 34 slice 3).
// 2026-11-04: Price existing weapon accuracy and equipment armor/evasion (spec 34 slice 4).
// See CHANGES.md and License.txt for GPL-3.0 terms; existing notices remain applicable.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

internal static class CompanyPower
{
    // Provisional point weights, not a promise of equal win rates. No RNG or live-state mutations.
    internal sealed class Report
    {
        internal readonly Dictionary<string,double> Parts=new Dictionary<string,double>();
        internal readonly List<string> Unpriced=new List<string>();
        internal double Total, Availability=1;
        internal double Strength, Agility, Intelligence, Voracity;
        internal string Describe(Unit unit)
        {
            var text=new StringBuilder("Healthy power: "+Total.ToString("0.00")+"\n");
            foreach(var p in Parts)text.AppendLine(p.Key+": "+p.Value.ToString("+0.00;-0.00;0.00"));
            text.AppendLine("Action availability multiplier: "+Availability.ToString("0.00"));
            text.AppendLine("Current-condition estimate: "+Current(unit,this).ToString("0.00")+" (diagnostic only)");
            if(Unpriced.Count>0)text.AppendLine("Not priced: "+string.Join(", ",Unpriced.ToArray()));
            return text.ToString();
        }
    }
    internal static Report Evaluate(Unit unit)
    {
        var report=new Report();var traits=unit.GetTraits;var boosts=new PermanentBoosts();
        foreach(var trait in traits)
        {
            var booster=TraitList.GetTrait(trait) as AbstractBooster;
            if(booster!=null)booster.Boost(boosts);
        }
        TieredTraits.Apply(boosts,traits);
        Func<TierEffect,double> tier=e=>boosts.TierValues[(int)e];
        Func<Stat,double> stat=s=>Math.Max(1,unit.GetStatBase(s)*boosts.Scale*boosts.StatMult*(s==Stat.Strength?Strongest(boosts.StrengthMult,boosts.EnduranceMult):
            s==Stat.Agility?Strongest(boosts.AgilityMult,boosts.DexterityMult):s==Stat.Intelligence?Strongest(boosts.MindMult,boosts.WillMult):Strongest(boosts.VoracityMult,boosts.StomachMult)));
        double str=stat(Stat.Strength),agi=stat(Stat.Agility),intel=stat(Stat.Intelligence),vor=stat(Stat.Voracity);
        report.Strength=str;report.Agility=agi;report.Intelligence=intel;report.Voracity=vor;
        report.Parts["Combat stats (healthy)"]=(str+agi+intel+vor)*.30;
        report.Parts["Normal maximum HP"]=unit.MaxHealth*.20;
        report.Parts["Species AP access"]=(unit.SpeciesSettings.GetBaseAP(unit.Race)-2)*3;
        var melee=unit.GetBestMelee();var ranged=unit.GetBestRanged();
        double meleeStat=traits.Contains(Traits.BoundWeapon)?intel:str;
        double rangedStat=traits.Contains(Traits.BoundWeapon)?intel:agi;
        if(traits.Contains(Traits.SpellBlade)){meleeStat+=intel*.5;rangedStat+=intel*.5;}
        if(traits.Contains(Traits.Finesse)){meleeStat=.8*str+.3*agi;rangedStat=meleeStat;}
        double m=Math.Max(1,(melee==null?2:melee.Damage)*(60+meleeStat)/60*boosts.Outgoing.MeleeDamage+tier(TierEffect.MeleeDamage));
        double r=ranged==null?0:Math.Max(1,ranged.Damage*(60+rangedStat)/60*boosts.Outgoing.RangedDamage+tier(TierEffect.RangedDamage));
        if(traits.Contains(Traits.Feral))m*=3;
        double meleeAim=Math.Max(.05,Math.Min(.95,WeaponAim(melee)+tier(TierEffect.MeleeAccuracy)-boosts.Outgoing.MeleeShift*.10));
        double rangedAim=Math.Max(.05,Math.Min(.95,WeaponAim(ranged)+tier(TierEffect.RangedAccuracy)-boosts.Outgoing.RangedShift*.10));
        double critical=Math.Max(0,Math.Min(1,Math.Min(.35,.05+intel*.0025)+tier(TierEffect.CriticalChance)));
        double criticalDamage = Config.CritDamageMod * boosts.Outgoing.CritDamageMult;
        double grazeDamage = Config.GrazeDamageMod * boosts.Outgoing.GrazeDamageMult;
        bool keenEye = traits.Contains(Traits.KeenEye);
        m *= WeaponOutcomeFactor(meleeAim, critical, criticalDamage, grazeDamage, keenEye);
        r *= WeaponOutcomeFactor(rangedAim, critical, criticalDamage, grazeDamage, keenEye);
        if(ranged!=null)r+=Math.Min(8,ranged.Range)*.35;
        double weapons=Math.Max(m,r)+Math.Min(m,r)*.25;
        double maxMana=Math.Max(0,3*unit.GetStatBase(Stat.Intelligence)*boosts.ManaMultiplier+tier(TierEffect.MaxMana)*unit.Level);
        var spells=(unit.UseableSpells??new List<Spell>()).Where(s=>!s.IsRetired && s.ManaCost<=maxMana).GroupBy(s=>s.SpellType).Select(g=>g.First()).ToArray();
        double spellAim=Math.Max(.05,Math.Min(.95,.65+tier(TierEffect.MagicAccuracy)-boosts.Outgoing.MagicShift*.10));
        var spellScores=spells.Select(s=>s.SpellType == SpellTypes.CinderField ? (8 + Math.Min(5, intel / 5)) * spellAim : s is DamageSpell?
            Math.Max(1,(3+intel*.12+Math.Min(4,s.Tier)*2)*boosts.Outgoing.MagicDamage+tier(TierEffect.MagicDamage))*(s.Resistable?spellAim:1)+Math.Min(8,s.Range.Max)*.25+Math.Min(3,s.AreaOfEffect)*2:
            s.SpellType == SpellTypes.Summon ? 10 + Math.Min(4, intel / 5) :
            s.SpellType == SpellTypes.GateMaw ? 12 + Math.Min(5, intel / 5) :
            s.SpellType == SpellTypes.Trance ? 8 + Math.Min(4, intel / 8) :
            s.SpellType == SpellTypes.Mending ? 6 + Math.Min(3, intel / 10) :
            3+Math.Min(4,s.Tier)+Math.Min(3,s.AreaOfEffect)).OrderByDescending(x=>x).ToArray();
        double magic=spellScores.Length==0?0:spellScores[0]*.8+spellScores.Skip(1).Take(3).Sum()*.20;
        // Most attacks spend all remaining AP: alternatives add flexibility, not another full attack.
        report.Parts["Weapon options"]=weapons>=magic?weapons:weapons*.25;
        report.Parts["Spell options"]=magic>weapons?magic:magic*.25;
        report.Parts["Mana endurance"]=spells.Length==0?0:Math.Min(120,maxMana)*.025;
        report.Parts["Damage defenses"]=(1-boosts.Incoming.MeleeDamage)*5+(1-boosts.Incoming.RangedDamage)*3+(1-boosts.Incoming.MagicDamage)*3-
            tier(TierEffect.PhysicalDamageReceived)*1.25-tier(TierEffect.MagicDamageReceived)*.9;
        report.Parts["Evasion"]=-tier(TierEffect.AttackAccuracyReceived)*20+boosts.Incoming.MeleeShift*2+boosts.Incoming.RangedShift*2+boosts.Incoming.MagicShift*2;
        report.Parts["Injury protection"]=Enumerable.Range(0,4).Sum(i=>CompanyEquipment.Protection(unit,(CompanySlot)i))*.75;
        report.Parts["Equipment armor / evasion"]=CompanyEquipment.ArmorClass(unit)*.8+CompanyEquipment.Evasion(unit)*20;
        report.Parts["Vore capability"]=!unit.Predator?0:vor*.05+tier(TierEffect.VoreAccuracy)*12+
            Log(boosts.Outgoing.VoreOddsMult)*3+Log(boosts.CapacityMult)*2+Log(boosts.Outgoing.DigestionRate)*2-Log(boosts.Outgoing.ChanceToEscape)*2;
        report.Parts["Vore survival"]=-tier(TierEffect.VoreAccuracyReceived)*12-Log(boosts.Incoming.VoreOddsMult)*3-
            Log(boosts.Incoming.DigestionRate)*2+Log(boosts.Incoming.ChanceToEscape)*2;
        report.Parts["Recovery in combat"]=boosts.HealthRegen*.6+boosts.ManaRegen*.3;
        double abilities=0;
        foreach(var trait in traits)
        {
            double value;
            if(Special.TryGetValue(trait,out value))abilities+=value;
            else if(TieredTraits.Find(trait)==null && !(TraitList.GetTrait(trait) is AbstractBooster) && !Included.Contains(trait))report.Unpriced.Add(TraitsMethods.DisplayName(trait));
        }
        report.Parts["Special / situational abilities"]=abilities;
        if(traits.Contains(Traits.Juggernaut))report.Availability*=.70; // Half the turns; defenses remain present.
        report.Total=Math.Round(Math.Max(1,report.Parts.Values.Sum()*report.Availability),2);
        return report;
    }
    static double Strongest(double a,double b)=>Math.Abs(a-1)>=Math.Abs(b-1)?a:b;
    static double WeaponAim(Weapon weapon)
    { double rating=Math.Max(1,weapon==null?1:weapon.AccuracyModifier);return .65*rating/(.35+.65*rating); }
    static double Log(double value)=>Math.Log(Math.Max(.1,value),2);
    // Reference-target estimate. Native combat resolves target-specific floors and statuses.
    internal static double WeaponOutcomeFactor(double accuracy, double criticalChance, double criticalDamage, double grazeDamage, bool keenEye)
    {
        accuracy = Math.Max(0, Math.Min(1, accuracy));
        criticalChance = Math.Max(0, Math.Min(1, criticalChance));
        double factor = accuracy * (1 + criticalChance * (criticalDamage - 1));
        if (accuracy >= .51)
            factor += (1 - accuracy) * (keenEye ? 1 : accuracy) * grazeDamage;
        return factor;
    }
    internal static double Healthy(Unit unit)=>Evaluate(unit).Total;
    internal static double Current(Unit unit,Report healthy)
    {
        if(unit.Downed || unit.Digested)return 0;
        double condition=Math.Min(1,(unit.GetStat(Stat.Strength)+unit.GetStat(Stat.Agility)+unit.GetStat(Stat.Intelligence)+unit.GetStat(Stat.Voracity))/
            Math.Max(1,healthy.Strength+healthy.Agility+healthy.Intelligence+healthy.Voracity));
        double hp=Math.Max(0,Math.Min(1,unit.LastingHealth/(double)Math.Max(1,unit.MaxHealth)));
        double readiness=Math.Max(.1,1-CompanyInjuries.AccuracyPenalty(unit))*Math.Max(.1,1-CompanyInjuries.DamagePenalty(unit)/10.0)*
            Math.Max(1,unit.SpeciesSettings.GetBaseAP(unit.Race)-CompanyInjuries.MovementPenalty(unit))/(double)unit.SpeciesSettings.GetBaseAP(unit.Race);
        return Math.Round(healthy.Total*condition*Math.Sqrt(hp)*readiness,2);
    }
    internal static double FormationMultiplier(int count)=>1+Math.Min(.20,Math.Max(0,count-3)*.04);
    internal static double Force(IEnumerable<Unit> force)
    { var units=force.ToArray();return Math.Round(units.Sum(Healthy)*FormationMultiplier(units.Length),2); }
    internal static double HealthMultiplier(IEnumerable<Unit> company)
    {
        var units=company.ToArray();double maximum=units.Sum(u=>(double)Math.Max(1,u.MaxHealth));
        double hp=units.Sum(u=>(double)Math.Max(0,Math.Min(u.MaxHealth,u.LastingHealth)));
        double missing=maximum==0?0:1-hp/maximum;
        return 1-.5*Math.Min(.40,Math.Max(0,missing));
    }
    internal static double RecruitCeiling(int reputation)=>60+Math.Max(0,reputation)*.5;
    static readonly HashSet<Traits> Included=new HashSet<Traits>{Traits.NaturalCaster,Traits.KeenEye}; // Available spells and weapon outcomes already priced.
    static readonly Dictionary<Traits,double> Special=new Dictionary<Traits,double>
    {
        {Traits.Maul,3},{Traits.DoubleAttack,3},{Traits.FastCaster,3},{Traits.Pounce,4},
        {Traits.Frenzy,2},{Traits.ThrillSeeker,2},{Traits.Berserk,3},{Traits.Tenacious,3},
        {Traits.PackStrength,2},{Traits.PackDexterity,2},{Traits.PackMind,2},{Traits.PackVoracity,2},{Traits.PackTactics,5},
        {Traits.Paralyzer,3},{Traits.Stinger,2},{Traits.BoggingSlime,2},{Traits.Vampirism,2},{Traits.Biter,2},{Traits.VenomousBite,1},
        {Traits.Dazzle,3},{Traits.DefensiveStance,2},{Traits.FocusedDodge,2},{Traits.Intimidating,2},
        {Traits.MagicSynthesis,3},{Traits.ArcaneMagistrate,3},{Traits.ManaBurn,3},{Traits.ManaBarrier,3},{Traits.Petrifier,4},
        {Traits.SweepingStrikes,3},{Traits.ForcefulBlow,2},{Traits.DexterousDefense,3},{Traits.AllOutFirstStrike,2},
        {Traits.DyingStrike,2},{Traits.EvasiveBattler,1},{Traits.Cowardly,-4},{Traits.Timid,-2}
    };
}

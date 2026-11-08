// Added by this fork for the VoreWar mercenary-company conversion.
// 2026-11-07: Display and categorize post-battle Grudge tiers (spec 40D).
// 2026-11-04: Define exclusive baseline trait tiers, flat modifiers and Help categories (spec 32).
// See CHANGES.md and License.txt for GPL-3.0 terms.
using System;
using System.Collections.Generic;
using System.Linq;

enum TierEffect
{
    MeleeAccuracy, RangedAccuracy, MagicAccuracy, VoreAccuracy,
    MeleeDamage, RangedDamage, MagicDamage, PhysicalDamageReceived, MagicDamageReceived,
    AttackAccuracyReceived, VoreAccuracyReceived, MaxHealth, MaxMana, CriticalChance
}

static class TieredTraits
{
    internal sealed class Family
    {
        internal readonly string Name;
        internal readonly TierEffect Effect;
        internal readonly int Sign;
        internal readonly Traits[] Tiers;
        internal Family(string name, TierEffect effect, int sign, params Traits[] tiers)
        { Name = name; Effect = effect; Sign = sign; Tiers = tiers; }
    }

    internal static readonly Family[] Families =
    {
        new Family("Precise", TierEffect.MeleeAccuracy, 1, Traits.Precise, Traits.PreciseII, Traits.PreciseIII),
        new Family("Clumsy", TierEffect.MeleeAccuracy, -1, Traits.Clumsy, Traits.ClumsyII, Traits.ClumsyIII),
        new Family("Deadeye", TierEffect.RangedAccuracy, 1, Traits.Deadeye, Traits.DeadeyeII, Traits.DeadeyeIII),
        new Family("Nearsighted", TierEffect.RangedAccuracy, -1, Traits.Nearsighted, Traits.NearsightedII, Traits.NearsightedIII),
        new Family("MagicFinesse", TierEffect.MagicAccuracy, 1, Traits.MagicProwess, Traits.MagicFinesseII, Traits.MagicFinesseIII),
        new Family("ErrantMage", TierEffect.MagicAccuracy, -1, Traits.ErrantMage, Traits.ErrantMageII, Traits.ErrantMageIII),
        new Family("WideMaw", TierEffect.VoreAccuracy, 1, Traits.WideMaw, Traits.WideMawII, Traits.WideMawIII),
        new Family("WeakGullet", TierEffect.VoreAccuracy, -1, Traits.WeakGullet, Traits.WeakGulletII, Traits.WeakGulletIII),
        new Family("StrongMelee", TierEffect.MeleeDamage, 1, Traits.StrongMelee, Traits.StrongMeleeII, Traits.StrongMeleeIII),
        new Family("WeakAttack", TierEffect.MeleeDamage, -1, Traits.WeakAttack, Traits.WeakAttackII, Traits.WeakAttackIII),
        new Family("KeenShot", TierEffect.RangedDamage, 1, Traits.KeenShot, Traits.KeenShotII, Traits.KeenShotIII),
        new Family("RangedIneptitude", TierEffect.RangedDamage, -1, Traits.RangedIneptitude, Traits.RangedIneptitudeII, Traits.RangedIneptitudeIII),
        new Family("PowerCaster", TierEffect.MagicDamage, 1, Traits.PowerCaster, Traits.PowerCasterII, Traits.PowerCasterIII),
        new Family("MagicIneptitude", TierEffect.MagicDamage, -1, Traits.MagicIneptitude, Traits.MagicIneptitudeII, Traits.MagicIneptitudeIII),
        new Family("Resilient", TierEffect.PhysicalDamageReceived, -1, Traits.Resilient, Traits.ResilientII, Traits.ResilientIII),
        new Family("WeakBones", TierEffect.PhysicalDamageReceived, 1, Traits.WeakBones, Traits.WeakBonesII, Traits.WeakBonesIII),
        new Family("MagicResistance", TierEffect.MagicDamageReceived, -1, Traits.MagicResistance, Traits.MagicResistanceII, Traits.MagicResistanceIII),
        new Family("MagicVulnerable", TierEffect.MagicDamageReceived, 1, Traits.MagicVulnerable, Traits.MagicVulnerableII, Traits.MagicVulnerableIII),
        new Family("Dodger", TierEffect.AttackAccuracyReceived, -1, Traits.AccuteDodge, Traits.DodgerII, Traits.DodgerIII),
        new Family("Flatfooted", TierEffect.AttackAccuracyReceived, 1, Traits.Flatfooted, Traits.FlatfootedII, Traits.FlatfootedIII),
        new Family("HardPrey", TierEffect.VoreAccuracyReceived, -1, Traits.AwkwardShape, Traits.HardPreyII, Traits.HardPreyIII),
        new Family("EasyPrey", TierEffect.VoreAccuracyReceived, 1, Traits.Defenseless, Traits.EasyPreyII, Traits.EasyPreyIII),
        new Family("Hearty", TierEffect.MaxHealth, 1, Traits.Hearty, Traits.HeartyII, Traits.HeartyIII),
        new Family("Frail", TierEffect.MaxHealth, -1, Traits.Frail, Traits.FrailII, Traits.FrailIII),
        new Family("ManaBattery", TierEffect.MaxMana, 1, Traits.ManaBattery, Traits.ManaBatteryII, Traits.ManaBatteryIII),
        new Family("Fizzler", TierEffect.MaxMana, -1, Traits.Fizzler, Traits.FizzlerII, Traits.FizzlerIII),
        new Family("Assassin", TierEffect.CriticalChance, 1, Traits.Assassin, Traits.AssassinII, Traits.AssassinIII),
        new Family("Brutish", TierEffect.CriticalChance, -1, Traits.Brutish, Traits.BrutishII, Traits.BrutishIII),
    };

    static readonly Dictionary<Traits, Family> Lookup = Families.SelectMany(f => f.Tiers.Select(t => new { Trait = t, Family = f }))
        .ToDictionary(p => p.Trait, p => p.Family);

    internal static Traits Canonical(Traits trait) => trait == Traits.MetalBody ? Traits.AwkwardShape :
        trait == Traits.GrudgeII || trait == Traits.GrudgeIII ? Traits.Grudge : trait;
    internal static Family Find(Traits trait)
    {
        trait = Canonical(trait);
        Family family;
        return Lookup.TryGetValue(trait, out family) ? family : null;
    }
    internal static string DisplayName(Traits trait)
    {
        if (trait == Traits.Grudge) return "Grudge I";
        if (trait == Traits.GrudgeII) return "Grudge II";
        if (trait == Traits.GrudgeIII) return "Grudge III";
        var family = Find(trait);
        return family == null ? null : family.Name + " " + new[] { "I", "II", "III" }[Array.IndexOf(family.Tiers, Canonical(trait))];
    }

    // Resolve legacy/directly assigned conflicts too: strongest tier wins; ties use stable IDs.
    // Opposite signs share an effect, so they cannot stack or cancel each other.
    internal static List<Traits> Resolve(IEnumerable<Traits> traits)
    {
        var source = traits.Where(t => !TraitsMethods.IsRetired(t)).Select(Canonical).Distinct().ToList();
        var result = source.Where(t => Find(t) == null).ToList();
        foreach (var group in source.Where(t => Find(t) != null).GroupBy(t => Find(t).Effect))
            result.Add(group.OrderByDescending(t => Array.IndexOf(Find(t).Tiers, t)).ThenBy(t => (int)t).First());
        return result;
    }

    internal static float Amount(Unit unit, TierEffect effect)
    {
        float amount = unit.TraitBoosts.TierValues[(int)effect];
        return effect == TierEffect.MaxHealth || effect == TierEffect.MaxMana ? amount * Math.Max(1, unit.Level) : amount;
    }

    internal static void Apply(PermanentBoosts boosts, IEnumerable<Traits> traits)
    {
        foreach (var trait in Resolve(traits))
        {
            var family = Find(trait);
            if (family == null) continue;
            int tier = Array.IndexOf(family.Tiers, trait);
            float value = family.Effect == TierEffect.CriticalChance ? new[] { .05f, .10f, .15f }[tier] :
                IsAccuracy(family.Effect) ? new[] { .10f, .20f, .30f }[tier] : new[] { 1f, 2f, 4f }[tier];
            boosts.TierValues[(int)family.Effect] = value * family.Sign;
        }
    }

    static bool IsAccuracy(TierEffect effect) => effect == TierEffect.MeleeAccuracy || effect == TierEffect.RangedAccuracy ||
        effect == TierEffect.MagicAccuracy || effect == TierEffect.VoreAccuracy || effect == TierEffect.AttackAccuracyReceived || effect == TierEffect.VoreAccuracyReceived;

    internal static Trait GetTrait(Traits trait)
    {
        var family = Find(trait);
        return family == null ? null : new Booster(Description(trait), s => { });
    }

    internal static string Description(Traits trait)
    {
        var family = Find(trait);
        if (family == null) return null;
        int tier = Array.IndexOf(family.Tiers, Canonical(trait));
        string verb = family.Sign > 0 ? "increased" : "reduced";
        int flat = new[] { 1, 2, 4 }[tier];
        switch (family.Effect)
        {
            case TierEffect.MaxHealth: return "Maximum HP is " + verb + " by " + flat + " per character level, to a minimum of 1.";
            case TierEffect.MaxMana: return "Maximum mana is " + verb + " by " + flat + " per character level, to a minimum of 0.";
            case TierEffect.CriticalChance: return "Critical hit chance is " + verb + " by " + new[] { 5, 10, 15 }[tier] + " percentage points.";
        }
        string subject;
        switch (family.Effect)
        {
            case TierEffect.MeleeAccuracy: subject = "Melee attack accuracy"; break;
            case TierEffect.RangedAccuracy: subject = "Ranged weapon accuracy"; break;
            case TierEffect.MagicAccuracy: subject = "Resistible spell accuracy"; break;
            case TierEffect.VoreAccuracy: subject = "Vore accuracy"; break;
            case TierEffect.AttackAccuracyReceived: subject = "Enemy melee, ranged and resistible spell accuracy against this unit"; break;
            case TierEffect.VoreAccuracyReceived: subject = "Enemy vore accuracy against this unit"; break;
            case TierEffect.MeleeDamage: subject = "Melee attack damage"; break;
            case TierEffect.RangedDamage: subject = "Ranged weapon damage"; break;
            case TierEffect.MagicDamage: subject = "Spell damage"; break;
            case TierEffect.PhysicalDamageReceived: subject = "Incoming melee and ranged weapon damage"; break;
            default: subject = "Incoming spell damage"; break;
        }
        return subject + " is " + verb + " by " + (IsAccuracy(family.Effect) ? (tier + 1) * 10 + " percentage points." : flat + ". Damaging hits always inflict at least 1 damage.");
    }

    internal static string Category(Traits trait)
    {
        if (trait == Traits.Grudge || trait == Traits.GrudgeII || trait == Traits.GrudgeIII) return "Personal Development";
        var family = Find(trait);
        if (family != null)
        {
            bool incoming = family.Effect == TierEffect.PhysicalDamageReceived || family.Effect == TierEffect.MagicDamageReceived ||
                family.Effect == TierEffect.AttackAccuracyReceived || family.Effect == TierEffect.VoreAccuracyReceived;
            return (incoming ? family.Sign < 0 : family.Sign > 0) ? "Combat Bonuses" : "Combat Penalties";
        }
        if (Development.Contains(trait)) return "Personal Development";
        if (Biology.Contains(trait)) return "Species and Biology";
        if (Vore.Contains(trait)) return "Vore and Digestion";
        if (Bonuses.Contains(trait)) return "Combat Bonuses";
        if (Penalties.Contains(trait)) return "Combat Penalties";
        return "Special Abilities";
    }

    static HashSet<Traits> Set(params Traits[] traits) => new HashSet<Traits>(traits);
    static readonly HashSet<Traits> Development = Set(Traits.AdaptiveTactics, Traits.Foolish, Traits.AdeptLearner);
    static readonly HashSet<Traits> Biology = Set(Traits.Large, Traits.Small, Traits.Titanic, Traits.Tiny, Traits.Huge, Traits.Colossal,
        Traits.Featherweight, Traits.GelatinousBody, Traits.Tempered, Traits.HardSkin, Traits.Crystaline, Traits.AcellularBody,
        Traits.ViralBiology, Traits.Feral, Traits.DualStomach, Traits.StretchyInsides, Traits.Bulky, Traits.MutualBiology);
    static readonly HashSet<Traits> Vore = Set(Traits.EasyToVore, Traits.EscapeArtist, Traits.Lethargic, Traits.Submissive,
        Traits.Ravenous, Traits.EasilySatisfied, Traits.Nauseous, Traits.IronGut, Traits.SteadyStomach, Traits.Slippery,
        Traits.FastDigestion, Traits.SlowDigestion, Traits.FasterDigestion, Traits.SlowerDigestion, Traits.SlowMetabolism,
        Traits.SlowerMetabolism, Traits.FastAbsorption, Traits.SlowAbsorption, Traits.FasterAbsorption, Traits.SlowerAbsorption,
        Traits.Tasty, Traits.Disgusting, Traits.HighlyAbsorbable, Traits.IdealSustenance, Traits.ProteinRich, Traits.EfficientGuts,
        Traits.WastefulProcessing, Traits.AcidResistant, Traits.AcidImmunity, Traits.SoftBody, Traits.PleasantDigestion,
        Traits.UnpleasantDigestion, Traits.SedativeStomach, Traits.SerialSwallower, Traits.BornToMove, Traits.Greedy,
        Traits.MetabolicSurge, Traits.FearsomeAppetite, Traits.TasteForBlood, Traits.DyingStrike);
    static readonly HashSet<Traits> Bonuses = Set(Traits.Fit, Traits.PeakCondition, Traits.Elite, Traits.KeenEye, Traits.KeenReflexes,
        Traits.Dazzle, Traits.DefensiveStance, Traits.FocusedDodge, Traits.PackStrength, Traits.PackDexterity, Traits.PackMind,
        Traits.PackVoracity, Traits.PackTactics, Traits.ThrillSeeker, Traits.Frenzy, Traits.Tenacious, Traits.Berserk,
        Traits.HotBlooded, Traits.Grounded, Traits.ColdTolerance, Traits.HealingBlood, Traits.ManaRich, Traits.ManaDynamo,
        Traits.Perseverance, Traits.SenseWeakness, Traits.BladeDance, Traits.VenomShock, Traits.GiantSlayer, Traits.Crusher,
        Traits.Competitive, Traits.Duelist, Traits.Fervor, Traits.SpellBlade, Traits.BoundWeapon, Traits.Finesse, Traits.DexterousDefense);
    static readonly HashSet<Traits> Penalties = Set(Traits.Illness, Traits.Diseased, Traits.FireVulnerable, Traits.IceVulnerable,
        Traits.ElecVulnerable, Traits.Farsighted, Traits.AwfulAim, Traits.Timid, Traits.Cowardly, Traits.PoorConstitution,
        Traits.ManaAttuned, Traits.FoodComaProne, Traits.IntrusiveAppetite);
}

// Modified for the VoreWar mercenary-company conversion.
// 2026-11-08: Resolve generic company weapon tiers to known species-native combat names.
// 2026-11-08: Remove descriptive modifiers from species-specific weapon names shown in combat.
// 2026-10-30: Return a missing-target context rather than constructing a Human narrative dummy (spec 04B1).
// 2026-10-30: Replace flavor descriptors with neutral unit/species labels (spec 04B2).
// 2026-11-03: Remove permanently retired identity branches/entries while preserving shared behavior (spec 21).
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
using System;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

static class LogUtilities
{
    static int rand;
    internal static string GetRandomStringFrom(List<string> messages)
    {
        if (messages.Count == 0)
            return "";
        rand = Random.Range(0, messages.Count);
        return messages[rand];
    }

    internal static string GetRandomStringFrom(params string[] messages)
    {
        rand = Random.Range(0, messages.Length);
        return messages[rand];
    }

    internal static string GetGenderString(Unit unit, string female, string male, string mixed)
    {
        if (unit.HasBreasts && (unit.HasDick != unit.HasVagina))
            return female;
        else if (!unit.HasBreasts && (unit.HasDick != unit.HasVagina))
            return male;
        return mixed;
    }

    internal static string Capitalize(String str)
    {
        if (str == null)
            return null;
        return char.ToUpper(str[0]) + str.Substring(1);

    }

    /// <summary>
    /// Returns given unit's nominative pronoun.<br></br>(e.g. he/she/they)
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
    internal static string GPPHe(Unit unit) => unit.GetPronoun(0);

    /// <summary>
    /// Returns given unit's nominative pronoun appended with present-tense auxillary.<br></br>(e.g. he is/she is/they are)
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
    internal static string GPPHeIs(Unit unit) => unit.GetPronoun(0) + (unit.GetPronoun(5) == "plural" ? " are" : " is");

    /// <summary>
    /// Returns given unit's nominative pronoun appended with present-tense auxillary as a contraction.<br></br>(e.g. he's/she's/they're)
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
    internal static string GPPHeIsAbbr(Unit unit) => unit.GetPronoun(0) + (unit.GetPronoun(5) == "plural" ? "'re" : "'s");

    /// <summary>
    /// Returns given unit's nominative pronoun appended with past-tense auxillary.<br></br>(e.g. he was/she was/they were)
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
    internal static string GPPHeWas(Unit unit) => unit.GetPronoun(0) + (unit.GetPronoun(5) == "plural" ? " were" : " was");

    /// <summary>
    /// Returns given unit's accusative pronoun.<br></br>(e.g. him/her/them)
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
    internal static string GPPHim(Unit unit) => unit.GetPronoun(1);

    /// <summary>
    /// Returns given unit's pronomial possessive pronoun.<br></br>(e.g. ...<u>their</u> belly...)
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
    internal static string GPPHis(Unit unit) => unit.GetPronoun(2);

    /// <summary>
    /// Returns given unit's reflexive pronoun.<br></br>(e.g. ...can't help <u>themself</u>...)
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
    internal static string GPPHimself(Unit unit) => unit.GetPronoun(4);

    /// <summary>
    /// Returns "s" if given unit is referred to with singular grammar.
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
    internal static string SIfSingular(Unit unit) => (unit.GetPronoun(5) == "plural" ? "" : "s");

    /// <summary>
    /// Returns "es" if given unit is referred to with singular grammar.
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
    internal static string EsIfSingular(Unit unit) => (unit.GetPronoun(5) == "plural" ? "" : "es");

    /// <summary>
    /// Returns "y" or "ies" based on plurality of given unit.
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
    internal static string IesIfSingular(Unit unit) => (unit.GetPronoun(5) == "plural" ? "y" : "ies");

    /// <summary>
    /// Returns "has" or "have" based on plurality of given unit.
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
    internal static string HasHave(Unit unit) => (unit.GetPronoun(5) == "plural" ? "have" : "has");

    /// <summary>
    /// Returns "is" or "are" based on plurality of given unit.
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
    internal static string IsAre(Unit unit) => (unit.GetPronoun(5) == "plural" ? "are" : "is");

    /// <summary>
    /// Returns "was" or "were" based on plurality of given unit.
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
    internal static string WasWere(Unit unit) => (unit.GetPronoun(5) == "plural" ? "were" : "was");

    internal static string PluralForPart(PreyLocation location)
    {
        switch (location)
        {
            case PreyLocation.breasts:
            case PreyLocation.balls:
                return "";
            case PreyLocation.stomach:
            case PreyLocation.stomach2:
            case PreyLocation.womb:
            case PreyLocation.tail:
            case PreyLocation.anal:
            case PreyLocation.bladder:
                return "s";
        }
        return "";
    }

    internal static string BoyGirl(Unit unit)
    {
        if (unit.DefaultBreastSize >= 0 && unit.DickSize < 0)
            return "girl";
        if (unit.DefaultBreastSize < 0 && unit.DickSize >= 0)
            return "boy";
        return unit.Race.ToString();
    }

    //internal static string FriendlyWarrior(Unit unit)
    //{
    //    var friendlies = TacticalUtilities.Units.Where(s => s.Unit.Side == unit.Side && s.Unit != unit && s.Visible && s.Targetable && s.Unit.IsDead == false).ToArray();
    //    if (friendlies.Length == 0)
    //        return "NULL";
    //    return friendlies[State.Rand.Next(friendlies.Length)].Unit.Name;  
    //}
    internal static Unit RandomAlliedWarrior(Unit unit)//Implementing modified code, but kept original because I'm not sure if someone else was planning on using it.
    {
        var friendlies = TacticalUtilities.Units.Where(s => s.Unit.Side == unit.Side && s.Unit != unit && s.Visible && s.Targetable && s.Unit.IsDead == false).ToArray();
        if (friendlies.Length == 0)
            return null;
        return friendlies[State.Rand.Next(friendlies.Length)].Unit;
    }

    internal static Unit CompetitionWarrior(Unit unit)
    {
        var friendlies = TacticalUtilities.Units.Where(s => s.Unit.Side == unit.Side && s.Unit != unit && s.Visible && s.Targetable && s.Unit.IsDead == false && RomanticTarget(unit, s.Unit) == false).ToArray();
        if (friendlies.Length == 0)
            return null;
        return friendlies[State.Rand.Next(friendlies.Length)].Unit;
    }

    internal static Unit PotentialNextPrey(Unit unit)
    {
        var preyList = TacticalUtilities.Units.Where(s => s.Unit.Side != unit.Side && s.Visible && s.Targetable && !s.Unit.IsDead);
        var preyChanceMap = new Dictionary<Actor_Unit, float>();
        foreach (Actor_Unit prey in preyList)
        {
            float chance = prey.GetDevourChance(TacticalUtilities.Units.Where(actor => actor.Unit == unit)?.FirstOrDefault(), true);
            preyChanceMap.Add(prey, chance);
        }
        var primePrey = preyChanceMap.OrderBy(x => x.Value).LastOrDefault();
        if (!primePrey.Equals(default(KeyValuePair<Actor_Unit, float>)))
        {
            return primePrey.Key.Unit;
        }
        return null; // No suitable existing target; never create a species as text context.
    }

    internal static Unit AttractedWarrior(Unit unit)
    {
        if (unit.AttractedTo != null)
        {
            var actor = TacticalUtilities.Units.Where(s => s.Unit == unit.AttractedTo && s.Unit.Side == unit.Side && s.Unit != unit && RomanticTarget(unit, s.Unit)).FirstOrDefault(); //If this fails, reassign
            if (actor != null)
            {
                if (actor.Visible && actor.Targetable && actor.Unit.IsDead == false)
                {
                    return actor.Unit;
                }
                return null; //Avoid picking a new target during the same battle
            }
        }

        var friendlies = TacticalUtilities.Units.Where(s => s.Unit.Side == unit.Side && s.Unit != unit && s.Visible && s.Targetable && s.Unit.IsDead == false && RomanticTarget(unit, s.Unit)).ToArray();
        if (friendlies.Length == 0)
            return null;
        return friendlies[State.Rand.Next(friendlies.Length)].Unit;
    }

    internal static bool ActorHumanoid(Unit s)
    {
        return s.Race < (Race)100 || s.Race >= (Race)170;
    }

    internal static bool RomanticTarget(Unit unit, Unit target)
    {
        if (unit.GetGender() == Gender.Hermaphrodite || target.GetGender() == Gender.Hermaphrodite)
            return true;
        if (unit.GetGender() == Gender.Female)
        {
            switch (Config.FemalesLike)
            {
                case Orientation.Straight:
                    return target.GetGender() == Gender.Male;
                case Orientation.Gay:
                    return target.GetGender() == Gender.Female;
                case Orientation.Bi:
                    return true;
            }
        }
        if (unit.GetGender() == Gender.Male)
        {
            switch (Config.MalesLike)
            {
                case Orientation.Straight:
                    return target.GetGender() == Gender.Female;
                case Orientation.Gay:
                    return target.GetGender() == Gender.Male;
                case Orientation.Bi:
                    return true;
            }
        }
        //Should never make it here
        return false;
    }



    /// <summary>
    /// Determines whether the string supplied should have either a or an before it and returns the original string with the right "thing" in front of it.
    /// Done this way since otherwise the string might get randomized again and wouldn't match the returned bit.
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    internal static string GetAorAN(string str)
    {
        if (str.StartsWith("a", true, null) || str.StartsWith("e", true, null) || str.StartsWith("i", true, null) || str.StartsWith("o", true, null) || str.StartsWith("u", true, null) || str.StartsWith("y", true, null))
        {
            return "an " + str;
        }
        if (str.StartsWith("b", true, null) || str.StartsWith("c", true, null) || str.StartsWith("d", true, null) || str.StartsWith("f", true, null) || str.StartsWith("g", true, null) || str.StartsWith("h", true, null) ||
            str.StartsWith("j", true, null) || str.StartsWith("k", true, null) || str.StartsWith("l", true, null) || str.StartsWith("m", true, null) || str.StartsWith("n", true, null) || str.StartsWith("p", true, null) ||
            str.StartsWith("q", true, null) || str.StartsWith("r", true, null) || str.StartsWith("s", true, null) || str.StartsWith("t", true, null) || str.StartsWith("v", true, null) || str.StartsWith("w", true, null) || str.StartsWith("x", true, null) || str.StartsWith("z", true, null))
        {
            return "a " + str;
        }
        if (str.StartsWith("'", true, null))
        {
            if (str.StartsWith("'a", true, null) || str.StartsWith("'e", true, null) || str.StartsWith("'i", true, null) || str.StartsWith("'o", true, null) || str.StartsWith("'u", true, null) || str.StartsWith("'y", true, null))
            {
                return "an " + str;
            }
            if (str.StartsWith("'b", true, null) || str.StartsWith("'c", true, null) || str.StartsWith("'d", true, null) || str.StartsWith("'f", true, null) || str.StartsWith("'g", true, null) || str.StartsWith("'h", true, null) ||
                str.StartsWith("'j", true, null) || str.StartsWith("'k", true, null) || str.StartsWith("'l", true, null) || str.StartsWith("'m", true, null) || str.StartsWith("'n", true, null) || str.StartsWith("'p", true, null) ||
                str.StartsWith("'q", true, null) || str.StartsWith("'r", true, null) || str.StartsWith("'s", true, null) || str.StartsWith("'t", true, null) || str.StartsWith("'v", true, null) || str.StartsWith("'w", true, null) || str.StartsWith("'x", true, null) || str.StartsWith("'z", true, null))
            {
                return "a " + str;
            }
        }
        return str;
    }

    internal static string ApostrophizeWithOrWithoutS(string str)
    {
        return str + (str.EndsWith("s") ? "'" : "'s");
    }

    /// <summary>
    /// <para>Gets a descriptive string that fits sentences like "Edmond stuffs Sidney down his maw, enjoying the * morsels squirms on her way down."</para>
    /// <para>Generally meant for the prey/loser/weaker unit. Has mostly demeaning, belittling, weakness indicating or fear portraying terms.</para>
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
    internal static string GetPreyDesc(Unit unit)
    {
        return "unit";
    }

    /// <summary>
    ///<para>Gets a descriptive string that fits sentences like "Edmond stuffs Sidney down his maw, the prey filling his * body nicely."</para>
    ///<para>Generally meant for the predator/winner/stronger unit. Strength describing, contentment/pleasure indicating, etc. terms.</para>
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
    internal static string GetPredDesc(Unit unit)
    {
        return "unit";
    }

    /// <summary>
    /// <para>Gets a descriptive string that fits situations like "Jeanne graps Timothy's head, pushing the *'s face in her slit and soon forcing rest of him after it."</para>
    /// <para>This is either the species name, a name of the genus the species belongs to or something similar. Can also be a synonym of the species name.</para>
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
    internal static string GetRaceDescSingl(Unit unit)
    {
        return unit == null ? "unit" : unit.Race.ToString();
    }

    /// <summary>
    /// Gets a name that fits the weapon the unit's graphics show it using.
    /// </summary>
    /// <param name="weapon"></param>
    /// <param name="unit"></param>
    /// <returns></returns>
    internal static string GetWeaponTrueName(Weapon weapon, Unit unit)
    {
        if (weapon == null) return "weapon";
        string originalName = weapon.Name;
        string weaponName = originalName;
        if (unit == null) return weaponName;
        if (unit.CompanyLoadout)
        {
            string companyWeaponName = CompanyNativeWeaponName(weapon, unit);
            if (!string.IsNullOrEmpty(companyWeaponName)) return companyWeaponName;
            switch (weapon.Graphic)
            {
                case CompanyWeaponArtCatalog.LightMelee: weaponName = "Mace"; break;
                case CompanyWeaponArtCatalog.HeavyMelee: weaponName = "Axe"; break;
                case CompanyWeaponArtCatalog.LightRanged: weaponName = "Simple Bow"; break;
                case CompanyWeaponArtCatalog.HeavyRanged: weaponName = "Compound Bow"; break;
            }
        }
        if (unit.Race == Race.Wyvern) return "Claws";
        else if (unit.Race == Race.WyvernMatron) return "Claws";
        else if (unit.Race == Race.Serpents) return "Blade";
        else if (unit.Race == Race.FeralWolves) return "Fangs";
        else if (unit.Race == Race.Harvesters) return "Scythes";
        else if (unit.Race == Race.Kangaroos)
        {
            if (weaponName == "Mace") return "Club";
            else if (weaponName == "Axe") return "Spear";
            else if (weaponName == "Simple Bow") return "Boomerang";
            else if (weaponName == "Compound Bow") return "Woomera";
            else if (weaponName == "Claw") return "Claws";
        }
        else if (unit.Race == Race.Alligators)
        {
            if (weaponName == "Mace") return "Club";
            else if (weaponName == "Axe") return "Spear";
            else if (weaponName == "Claw") return "Claws";
        }
        else if (unit.Race == Race.Badgers)
        {
            if (weaponName == "Mace") return "Axe";
            else if (weaponName == "Axe") return "Glaive";
            else if (weaponName == "Claw") return "Claws";
        }
        else if (unit.Race == Race.Komodos)
        {
            if (weaponName == "Mace") return "Knife";
            else if (weaponName == "Axe") return "Scythe";
            else if (weaponName == "Claw") return "Claws";
        }
        else if (unit.Race == Race.Sergal)
        {
            if (weaponName == "Mace") return "Lance";
            else if (weaponName == "Axe") return "Glaive";
            else if (weaponName == "Simple Bow") return "Speargun";
            else if (weaponName == "Compound Bow") return "Railgun";
        }
        else if (unit.Race == Race.Bees)
        {
            if (weaponName == "Mace") return "Mace";
            else if (weaponName == "Axe") return "Claws";
            else if (weaponName == "Simple Bow") return "Javelin";
            else if (weaponName == "Compound Bow") return "Javelin";
        }
        else if (unit.Race == Race.Bats)
        {
            if (weaponName == "Mace") return "Dagger";
            else if (weaponName == "Axe") return "Katar";
            else if (weaponName == "Simple Bow") return "Throwing Knife";
            else if (weaponName == "Compound Bow") return "Throwing Knife";
        }
        else if (unit.Race == Race.Panthers)
        {
            if (weaponName == "Mace") return "Karambit";
            else if (weaponName == "Axe") return "Kukri";
            else if (weaponName == "Simple Bow") return "Chakram";
            else if (weaponName == "Compound Bow") return "Onzil";
        }
        else if (unit.Race == Race.Ants)
        {
            if (weaponName == "Mace") return "Spear";
            else if (weaponName == "Axe") return "Blades";
            else if (weaponName == "Simple Bow") return "Simple Bow";
            else if (weaponName == "Compound Bow") return "Compound Bow";
        }
        else if (unit.Race == Race.Avians)
        {
            if (weaponName == "Mace") return "Knife";
            else if (weaponName == "Axe") return "Sword";
            else if (weaponName == "Simple Bow") return "Bow";
            else if (weaponName == "Compound Bow") return "Crossbow";
        }
        else if (unit.Race == Race.Sharks)
        {
            if (weaponName == "Mace") return "Sabre";
            else if (weaponName == "Axe") return "Cutlass";
            else if (weaponName == "Simple Bow") return "Harpoon";
            else if (weaponName == "Compound Bow") return "Speargun";
        }
        else if (unit.Race == Race.Frogs)
        {
            if (weaponName == "Mace") return "Mace";
            else if (weaponName == "Axe") return "Axe";
            else if (weaponName == "Simple Bow") return "Slingshot";
            else if (weaponName == "Compound Bow") return "Bow";
            else if (weaponName == "Claw") return "Fist";
        }
        else if (unit.Race == Race.Hippos)
        {
            if (weaponName == "Mace") return "Knife";
            else if (weaponName == "Axe") return "Axe";
            else if (weaponName == "Simple Bow") return "Simple Bow";
            else if (weaponName == "Compound Bow") return "Compound Bow";
            else if (weaponName == "Claw") return "Fist";
        }
        else if (unit.Race == Race.Kobolds)
        {
            if (weaponName == "Mace") return "Pickax";
            else if (weaponName == "Axe") return "Pickax";
            else if (weaponName == "Simple Bow") return "Dart";
            else if (weaponName == "Compound Bow") return "Dart";
            else if (weaponName == "Claw") return "Fist";
        }
        else if (unit.Race == Race.Mice)
        {
            if (weaponName == "Mace") return "Sword";
            else if (weaponName == "Axe") return "Bardiche";
            else if (weaponName == "Simple Bow") return "Crossbow";
            else if (weaponName == "Compound Bow") return "Bow";
        }
        else if (unit.Race == Race.Bunnies)
        {
            if (weaponName == "Mace" && unit.BasicMeleeWeaponType == 1) return "Sword";
        }
        else if (weaponName == "Claw") return "Claws";
        return unit.CompanyLoadout ? originalName : weaponName;
    }

    static string CompanyNativeWeaponName(Weapon weapon, Unit unit)
    {
        if (unit.Race == Race.Avians)
        {
            switch (weapon.Graphic)
            {
                case CompanyWeaponArtCatalog.LightMelee: return "Knife";
                case CompanyWeaponArtCatalog.HeavyMelee: return "Sword";
                case CompanyWeaponArtCatalog.LightRanged: return "Bow";
                case CompanyWeaponArtCatalog.HeavyRanged: return "Crossbow";
            }
        }
        else if (unit.Race == Race.Lizards)
        {
            if (weapon.Graphic == CompanyWeaponArtCatalog.LightMelee) return "Macuahuitl";
            if (weapon.Graphic == CompanyWeaponArtCatalog.HeavyMelee) return "Axe";
            if (weapon.Graphic == CompanyWeaponArtCatalog.LightRanged) return "Bow";
            if (weapon.Graphic == CompanyWeaponArtCatalog.HeavyRanged) return "Javelin";
        }
        else if (unit.Race == Race.Gnolls)
        {
            if (weapon.Graphic == CompanyWeaponArtCatalog.LightMelee) return "Club";
            if (weapon.Graphic == CompanyWeaponArtCatalog.HeavyMelee) return "Cleaver";
            if (weapon.Graphic == CompanyWeaponArtCatalog.LightRanged || weapon.Graphic == CompanyWeaponArtCatalog.HeavyRanged)
                return "Javelin";
        }
        return null;
    }
}


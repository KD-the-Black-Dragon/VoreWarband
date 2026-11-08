// Modified for the VoreWar mercenary-company conversion.
// 2026-11-06: Validate metadata safely and store authored AP/weapon access without loading graphics (spec 40A).
// 2026-10-30: Preserve retired override records without hydration; use correct new mapping defaults (spec 03B3A).
// 2026-11-01: Separate stored species reads from intentional override creation (spec 03B3C).
// 2026-11-03: Remove permanently retired identity branches/entries while preserving shared behavior (spec 21).
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
// 2026-11-03: Implement four active stats and agreed combat rules (spec 29).
using OdinSerializer;
using System;
using System.Collections.Generic;
using System.Linq;


class RaceSettings
{
    [OdinSerialize]
    private Dictionary<Race, RaceSettingsItem> Races;

    public RaceSettings(bool seedLegacy = true)
    {
        Races = new Dictionary<Race, RaceSettingsItem>();
        if (!seedLegacy) return;
        Races[Race.FeralBats] = new RaceSettingsItem(Race.FeralBats, false);
        Races[Race.FeralBats].MaleTraits = new List<Traits> { Traits.Small };
    }

    internal IEnumerable<KeyValuePair<Race, RaceSettingsItem>> Entries => Races;
    internal void Set(Race race, RaceSettingsItem item) { Races[race] = item; }
    internal int GetBaseAP(Race race)
    {
        var item = GetExisting(race);
        return item != null && item.BaseAP > 0 ? item.BaseAP : WarbandStats.BuiltinAP(race);
    }
    internal void Sanitize()
    {
        if (Races == null) throw new System.IO.InvalidDataException("Missing race settings dictionary.");
        foreach (var entry in Races)
        {
            // Preserve unavailable records for explicit import review without hydrating them.
            if (SpeciesPolicy.IsRetired(entry.Key)) continue;
            var metadata = RaceParameters.GetRaceTraits(entry.Key);
            if (metadata == null) continue;
            var item = entry.Value;
            if (item == null || item.Stats == null) throw new System.IO.InvalidDataException("Incomplete settings for " + entry.Key);
            if (item.ConversionRace == Race.none) item.ConversionRace = metadata.ConversionRace;
            if (item.LeaderRace == Race.none) item.LeaderRace = metadata.LeaderRace;
            if (item.SpawnRace == Race.none) item.SpawnRace = metadata.SpawnRace;
            if (item.MorphRace == Race.none) item.MorphRace = metadata.MorphRace;
            item.Stats.Consolidate();
            item.Stats.Strength.Roll = Math.Max(1, item.Stats.Strength.Roll);
            item.Stats.Agility.Roll = Math.Max(1, item.Stats.Agility.Roll);
            item.Stats.Mind.Roll = Math.Max(1, item.Stats.Mind.Roll);
            item.Stats.Voracity.Roll = Math.Max(1, item.Stats.Voracity.Roll);
            item.Stats.Mirror();
        }
    }

    // Reading a stored override must not create defaults or load graphics.
    internal RaceSettingsItem GetExisting(Race race)
    {
        RaceSettingsItem item;
        return Races.TryGetValue(race, out item) ? item : null;
    }

    // Only intentional edits may materialize a complete override record.
    internal RaceSettingsItem GetOrCreate(Race race)
    {
        var item = GetExisting(race);
        if (item != null)
            return item;
        item = new RaceSettingsItem(race);
        Races[race] = item;
        return item;
    }

    internal float GetPowerAdjustment(Race race)
    {
        var item = GetExisting(race);
        float power = item == null ? RaceParameters.GetRaceTraits(race).PowerAdjustment : item.PowerAdjustment;
        return power == 0 ? RaceParameters.GetRaceTraits(race).PowerAdjustment : power;
    }

    internal bool Exists(Race race)
    {
        return Races.TryGetValue(race, out RaceSettingsItem item);
    }

    internal bool GetOverrideGender(Race race)
    {
        if (Races.ContainsKey(race))
            return GetExisting(race).OverrideGender;
        return false;
    }

    internal bool GetOverrideFurry(Race race)
    {
        if (Races.ContainsKey(race))
            return GetExisting(race).OverrideFurry;
        return false;
    }

    internal bool GetOverrideClothed(Race race)
    {
        if (Races.ContainsKey(race))
            return GetExisting(race).overrideClothes;
        return false;
    }

    internal bool GetOverrideWeight(Race race)
    {
        if (Races.ContainsKey(race))
            return GetExisting(race).overrideWeight;
        return false;
    }

    internal bool GetOverrideBreasts(Race race)
    {
        if (Races.ContainsKey(race))
            return GetExisting(race).overrideBoob;
        return false;
    }

    internal bool GetOverrideDick(Race race)
    {
        if (Races.ContainsKey(race))
            return GetExisting(race).overrideDick;
        return false;
    }


    internal int GetBodySize(Race race)
    {
        if (Races.ContainsKey(race))
            return GetExisting(race).BodySize;
        return RaceParameters.GetRaceTraits(race).BodySize;
    }

    internal int GetStomachSize(Race race)
    {
        if (Races.ContainsKey(race))
            return GetExisting(race).StomachSize;
        return RaceParameters.GetRaceTraits(race).StomachSize;
    }

    internal float GetDeployCost(Race race)
    {
        if (Races.ContainsKey(race))
            return GetExisting(race).DeployCost;
        return RaceParameters.GetRaceTraits(race).DeployCost;
    }

    internal float GetUpkeep(Race race)
    {
        if (Races.ContainsKey(race))
            return GetExisting(race).Upkeep;
        return RaceParameters.GetRaceTraits(race).Upkeep;
    }

    internal List<Traits> GetRaceTraits(Race? race)
    {
        if (race == null)
            return null;
        if (Races.ContainsKey((Race)race))
            return GetExisting((Race)race).RaceTraits;
        return RaceParameters.GetRaceTraits((Race)race).RacialTraits;
    }

    internal List<int> GetRaceTags(Race? race)
    {
        if (race == null)
            return null;
        if (Races.ContainsKey((Race)race))
            return GetExisting((Race)race).RaceTags;
        return RaceParameters.GetRaceTraits((Race)race).RacialTags;
    }

    internal List<Traits> GetMaleRaceTraits(Race race)
    {
        if (Races.ContainsKey(race))
            return GetExisting(race).MaleTraits;
        return null;
    }

    internal List<Traits> GetFemaleRaceTraits(Race race)
    {
        if (Races.ContainsKey(race))
            return GetExisting(race).FemaleTraits;
        return null;
    }

    internal List<Traits> GetHermRaceTraits(Race race)
    {
        if (Races.ContainsKey(race))
            return GetExisting(race).HermTraits;
        return null;
    }

    internal List<Traits> GetSpawnRaceTraits(Race race)
    {
        if (Races.ContainsKey(race))
            return GetExisting(race).SpawnTraits;
        return RaceParameters.GetRaceTraits(race).SpawnTraits;
    }

    internal List<Traits> GetLeaderRaceTraits(Race race)
    {
        if (Races.ContainsKey(race))
            return GetExisting(race).LeaderTraits;
        return RaceParameters.GetRaceTraits(race).LeaderTraits;
    }

    internal Stat GetFavoredStat(Race race)
    {
        if (Races.ContainsKey(race))
        {
            var par = GetExisting(race);
            if (par.FavoredStatSet)
            {
                return WarbandStats.Canonical(par.FavoredStat);
            }
        }

        return WarbandStats.Canonical(RaceParameters.GetRaceTraits(race).FavoredStat);
    }

    internal SpellTypes GetInnateSpell(Race race)
    {
        if (Races.ContainsKey(race))
        {
            return GetExisting(race).InnateSpell;
        }
        return SpellTypes.None;
    }

    internal RaceAI GetRaceAI(Race race)
    {
        if (Races.ContainsKey(race))
        {
            return GetExisting(race).RaceAI;
        }
        return RaceAI.Standard;
    }

    internal string ListTraits(Race race)
    {
        var Tags = RaceParameters.GetRaceTraits(race).RacialTraits;
        if (Tags.Count == 0)
            return "";
        string ret = "";
        for (int i = 0; i < Tags.Count; i++)
        {
            ret += Tags[i].ToString();
            if (i + 1 < Tags.Count)
                ret += "\n";
        }
        return ret;
    }

    internal List<VoreType> GetVoreTypes(Race race)
    {
        if (Races.ContainsKey(race))
        {
            return GetExisting(race).AllowedVoreTypes;
        }

        return RaceParameters.GetRaceTraits(race).AllowedVoreTypes;
    }

    internal RaceStats GetRaceStats(Race race)
    {
        var stats = Races.ContainsKey(race) ? GetExisting(race).Stats : RaceParameters.GetRaceTraits(race).RaceStats;
        stats.Consolidate();
        return stats;
    }

    internal Race GetSpawnRace(Race race)
    {
        Race spawnRace = Race.none;
        if (Races.ContainsKey(race))
            spawnRace = GetExisting(race).SpawnRace;
        if (spawnRace == Race.none)
            spawnRace = RaceParameters.GetRaceTraits(race).SpawnRace; 
        return (spawnRace == Race.none) ? race : spawnRace;
    }

    internal Race GetConversionRace(Race race)
    {
        Race conversionRace = Race.none;
        if (Races.ContainsKey(race))
            conversionRace = GetExisting(race).ConversionRace;
        if (conversionRace == Race.none)
            conversionRace = RaceParameters.GetRaceTraits(race).ConversionRace; 
        return (conversionRace == Race.none) ? race : conversionRace;
    }

    internal Race GetLeaderRace(Race race)
    {
        Race leaderRace = Race.none;
        if (Races.ContainsKey(race))
            leaderRace = GetExisting(race).LeaderRace;
        if (leaderRace == Race.none)
            leaderRace = RaceParameters.GetRaceTraits(race).LeaderRace; 
        return (leaderRace == Race.none) ? race : leaderRace;
    }

    internal Race GetMorphRace(Race race)
    {
        Race morphRace = Race.none;
        if (Races.ContainsKey(race))
            morphRace = GetExisting(race).MorphRace;
        if (morphRace == Race.none)
            morphRace = RaceParameters.GetRaceTraits(race).MorphRace; 
        return (morphRace == Race.none) ? race : morphRace;
    }

    //internal Race GetDisplayedGraphic(Race race)
    //{
    //    if (Races.ContainsKey(race))
    //        return GetExisting(race).DisplayGraphics;
    //    return race;
    //}

    internal void Reset(Race race)
    {
        Races.Remove(race);
        if (race == Race.FeralBats)
        {
            Races[Race.FeralBats] = new RaceSettingsItem(Race.FeralBats);
            Races[Race.FeralBats].MaleTraits = new List<Traits> { Traits.Small };
        }


    }

    internal void ResetAll()
    {
        Races = new Dictionary<Race, RaceSettingsItem>();
        Races[Race.FeralBats] = new RaceSettingsItem(Race.FeralBats);
        Races[Race.FeralBats].MaleTraits = new List<Traits> { Traits.Small };

    }
}

class RaceSettingsItem
{
    [OdinSerialize] internal int BaseAP;
    [OdinSerialize] internal bool? CanUseMeleeWeapons;
    [OdinSerialize] internal bool? CanUseRangedWeapons;


    [OdinSerialize]
    internal bool OverrideGender;
    [OdinSerialize]
    internal float maleFraction;
    [OdinSerialize]
    internal float hermFraction;
    [OdinSerialize]
    internal bool OverrideFurry;
    [OdinSerialize]
    internal float furryFraction;

    [OdinSerialize]
    internal int BodySize;
    [OdinSerialize]
    internal int StomachSize;
    [OdinSerialize]
    internal List<Traits> RaceTraits;
    [OdinSerialize]
    internal List<int> RaceTags;

    [OdinSerialize]
    internal List<VoreType> AllowedVoreTypes;

    [OdinSerialize]
    internal Race SpawnRace;
    [OdinSerialize]
    internal Race ConversionRace;
    [OdinSerialize]
    internal Race LeaderRace;
    [OdinSerialize]
    internal Race MorphRace;

    [OdinSerialize]
    internal RaceStats Stats;

    [OdinSerialize]
    internal bool overrideClothes;
    [OdinSerialize]
    internal float clothedFraction;

    [OdinSerialize]
    internal int BannerType;

    [OdinSerialize]
    internal bool overrideWeight;
    [OdinSerialize]
    internal int MinWeight;
    [OdinSerialize]
    internal int MaxWeight;

    [OdinSerialize]
    internal bool overrideBoob;
    [OdinSerialize]
    internal int MinBoob;
    [OdinSerialize]
    internal int MaxBoob;

    [OdinSerialize]
    internal bool overrideDick;
    [OdinSerialize]
    internal int MinDick;
    [OdinSerialize]
    internal int MaxDick;

    [OdinSerialize]
    internal List<Traits> MaleTraits;
    [OdinSerialize]
    internal List<Traits> FemaleTraits;
    [OdinSerialize]
    internal List<Traits> HermTraits;
    [OdinSerialize]
    internal List<Traits> SpawnTraits;
    [OdinSerialize]
    internal List<Traits> LeaderTraits;

    [OdinSerialize]
    internal bool FavoredStatSet;
    [OdinSerialize]
    internal Stat FavoredStat;

    [OdinSerialize]
    internal SpellTypes InnateSpell;

    [OdinSerialize]
    internal RaceAI RaceAI;

    [OdinSerialize]
    internal float PowerAdjustment;
    [OdinSerialize]
    internal float Upkeep;
    [OdinSerialize]
    internal float DeployCost;



    //[OdinSerialize]
    //internal Race DisplayGraphics;

    public RaceSettingsItem(Race race, bool loadAppearance = true)
    {
        if (SpeciesPolicy.IsRetired(race))
            throw new InvalidOperationException("Cannot create settings for retired species: " + race);

        OverrideGender = false;
        maleFraction = Config.MaleFraction;
        hermFraction = Config.HermFraction;
        OverrideFurry = false;
        furryFraction = Config.FurryFraction;

        var racePar = RaceParameters.GetRaceTraits(race);
        var raceData = loadAppearance ? Races.GetRace(race) : null;
        if (racePar == null) throw new InvalidOperationException("Species metadata is unavailable: " + race);

        BodySize = racePar.BodySize;
        StomachSize = racePar.StomachSize;

        RaceTraits = racePar.RacialTraits.ToList();
        RaceTags = racePar.RacialTags.ToList();
        AllowedVoreTypes = racePar.AllowedVoreTypes.ToList();

        SpawnRace = racePar.SpawnRace;
        ConversionRace = racePar.ConversionRace;
        LeaderRace = racePar.LeaderRace;
        MorphRace = racePar.MorphRace;

        var baseStats = racePar.RaceStats;

        Stats = new RaceStats() //Generates a clean copy instead of a reference.  
        {
            FourStatVersion = baseStats.FourStatVersion,
            Strength = baseStats.Strength,
            Dexterity = baseStats.Dexterity,
            Endurance = baseStats.Endurance,
            Mind = baseStats.Mind,
            Will = baseStats.Will,
            Agility = baseStats.Agility,
            Voracity = baseStats.Voracity,
            Stomach = baseStats.Stomach,
        };

        overrideClothes = false;
        clothedFraction = Config.ClothedFraction;

        BannerType = 0;

        overrideWeight = false;
        overrideBoob = false;
        overrideDick = false;

        MinBoob = 0;
        MaxBoob = Math.Max((raceData == null ? 1 : raceData.BreastSizes) - 1, 0);

        MinDick = 0;
        MaxDick = Math.Max((raceData == null ? 1 : raceData.DickSizes) - 1, 0);

        MinWeight = 0;
        MaxWeight = Math.Max((raceData == null ? 1 : raceData.BodySizes) - 1, 0);

        MaleTraits = new List<Traits>();
        FemaleTraits = new List<Traits>();
        HermTraits = new List<Traits>();
        SpawnTraits = new List<Traits>();
        LeaderTraits = new List<Traits>();

        FavoredStatSet = true;
        FavoredStat = racePar.FavoredStat;

        RaceAI = racePar.RaceAI;
        BaseAP = WarbandStats.BuiltinAP(race);
        CanUseMeleeWeapons = racePar.CanUseMeleeWeapons;
        CanUseRangedWeapons = racePar.CanUseRangedWeapons;

        PowerAdjustment = racePar.PowerAdjustment;
        Upkeep = racePar.Upkeep;
        DeployCost = racePar.DeployCost;

        //DisplayGraphics = race;

    }
}


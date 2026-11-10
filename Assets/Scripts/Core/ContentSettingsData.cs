// Added by the VoreWar mercenary-company conversion fork.
// 2026-10-30: Separate content-setting defaults and preference loading from menu widgets (spec 02A).
// 2026-10-30: Lock retired settings to code defaults; persist only retained preferences (spec 02C).
// 2026-10-30: Route species preferences through the approved roster (spec 03A1).
// 2026-11-02: Retire nine additional content preferences to their existing defaults (spec 15B).
// 2026-11-03: Remove permanently retired identity branches/entries while preserving shared behavior (spec 21).
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
// Defaults and loading logic are extracted from the upstream ContentSettings component.
// 2026-11-03: Implement four active stats and agreed combat rules (spec 29).
// 2026-11-08: Lock downed-predator auto-regurgitation to a 100% chance.
// 2026-11-08: Lock provisional absorption tuning and enable enemy belly-rub preference by default (balance item 1).
using System;
using System.Collections.Generic;
using UnityEngine;

// Runtime preference loading must not depend on whether a settings page exists.
internal static class ContentSettingsData
{
    private static readonly Dictionary<string, bool> ToggleDefaults = new Dictionary<string, bool>
    {
        ["WeightGain"] = true,
        ["WeightLoss"] = true,
        ["FurryHandsAndFeet"] = true,
        ["FurryFluff"] = true,
        ["FriendlyRegurgitation"] = true,
        ["HairMatchesFur"] = false,
        ["MaleHairForFemales"] = true,
        ["FemaleHairForMales"] = true,
        ["HermsOnlyUseFemaleHair"] = false,
        ["HideBreasts"] = false,
        ["HideCocks"] = false,
        ["RaceTraitsEnabled"] = true,
        ["RagsForSlaves"] = true,
        ["VisibleCorpses"] = true,
        ["EdibleCorpses"] = false,
        ["FogOfWar"] = false,
        ["LeadersUseCustomizations"] = false,
        ["HermsCanUB"] = false,
        ["Unbirth"] = false,
        ["CockVore"] = false,
        ["BreastVore"] = false,
        ["AnalVore"] = false,
        ["TailVore"] = false,
        ["BladderVore"] = false,
        ["CockVoreHidesClothes"] = false,
        ["AltVoreOralGain"] = false,
        ["AllowHugeBreasts"] = false,
        ["AllowHugeDicks"] = false,
        ["AllowTopless"] = false,
        ["FurryGenitals"] = false,
        ["LamiaUseTailAsSecondBelly"] = false,
        ["AnimatedBellies"] = true,
        ["DigestionSkulls"] = true,
        ["BellyRubHands"] = true,
        ["SurrenderFlag"] = true,
        ["ShowUnitSides"] = true,
        ["Bones"] = true,
        ["CleanDisposal"] = false,
        ["Scat"] = false,
        ["ScatV2"] = false,
        ["BirdScat"] = false,
        ["ScatBones"] = false,
        ["CondomsForCV"] = false,
        ["ErectionsFromVore"] = false,
        ["ErectionsFromCockVore"] = false,
        ["AutoSurrender"] = false,
        ["EatSurrenderedAllies"] = false,
        ["FlatExperience"] = false,
        ["BoostedAccuracy"] = false,
        ["ClothingDiscards"] = true,
        ["ExtraRandomHairColors"] = false,
        ["GoblinCaravans"] = true,
        ["Diplomacy"] = false,
        ["LizardsHaveNoBreasts"] = false,
        ["LewdDialog"] = false,
        ["KuroTenkoEnabled"] = false,
        ["OverhealEXP"] = true,
        ["TransferAllowed"] = true,
        ["CumGestation"] = true,
        ["HardVoreDialog"] = false,
        ["NoAIRetreat"] = false,
        ["AICanHireSpecialMercs"] = false,
        ["AICanCheatSpecialMercs"] = false,
        ["DisorientedPrey"] = true,
        ["MonstersDropSpells"] = true,
        ["AllowInfighting"] = false,
        ["EventsRepeat"] = false,
        ["SurrenderedCanConvert"] = false,
        ["LockedAIRelations"] = false,
        ["Defections"] = true,
        ["RaceSizeLimitsWeightGain"] = false,
        ["MultiRaceVillages"] = false,
        ["RaceSpecificVoreGraphicsDisabled"] = false,
        ["MonstersCanReform"] = true,
        ["MultiRaceFlip"] = false,
        ["AdventurersDisabled"] = false,
        ["MercenariesDisabled"] = false,
        ["TroopScatter"] = false,
        ["AlwaysRandomizeConverted"] = false,
        ["SpecialMercsCanConvert"] = false,
        ["LeadersRerandomizeOnDeath"] = false,
        ["NoScatForDeadTransfers"] = false,
        ["HideViperSlit"] = false,
        ["Cumstains"] = true,
        ["BurpOnDigest"] = false,
        ["FartOnAbsorb"] = false,
        ["CanUseStomachRubOnEnemies"] = true,
        ["DayNightEnabled"] = false,
        ["DayNightCosmetic"] = false,
        ["DayNightSchedule"] = true,
        ["DayNightRandom"] = true,
        ["NightMonsters"] = false,
        ["NightMoveMonsters"] = false,
        ["CombatComplicationsEnabled"] = true,
        ["StatCrit"] = true,
        ["StatGraze"] = false,
        ["StatBoostsAffectMaxHP"] = false,
        ["OverfeedingDamage"] = false,
        ["DigestionDamageDivision"] = false,
        ["AbsorbRateDivision"] = false,
        ["AbsorbLoss"] = false,
        ["AbsorbBoostDeadOnly"] = false,
        ["SizeAccuracyInverse"] = true,
        ["SizeDamageInverse"] = true,
        ["PotionSystemEnabled"] = false,
    };

    // Explicit allowlist: hidden settings never read or write old player overrides.
    private static readonly HashSet<string> PreferenceToggles = new HashSet<string>
    {
        "FurryHandsAndFeet",
        "FurryFluff",
        "FriendlyRegurgitation",
        "HairMatchesFur",
        "MaleHairForFemales",
        "FemaleHairForMales",
        "HermsOnlyUseFemaleHair",
        "HideBreasts",
        "HideCocks",
        "HermsCanUB",
        "Unbirth",
        "CockVore",
        "AnalVore",
        "TailVore",
        "CockVoreHidesClothes",
        "AllowHugeBreasts",
        "AllowHugeDicks",
        "AllowTopless",
        "FurryGenitals",
        "AnimatedBellies",
        "DigestionSkulls",
        "BellyRubHands",
        "SurrenderFlag",
        "ShowUnitSides",
        "Bones",
        "CondomsForCV",
        "ErectionsFromVore",
        "ErectionsFromCockVore",
        "ClothingDiscards",
        "ExtraRandomHairColors",
        "LizardsHaveNoBreasts",
        "HideViperSlit",
        "Cumstains",
        "BurpOnDigest",
        "CanUseStomachRubOnEnemies",
    };

    internal static void ApplyLockedValues(WorldConfig world)
    {
        if (world.Toggles == null)
            world.ResetDictionary();
        foreach (var toggle in ToggleDefaults)
        {
            if (!PreferenceToggles.Contains(toggle.Key))
                world.Toggles[toggle.Key] = toggle.Value;
        }
        // Compatibility keys mirror the approved generation policy, never old player overrides.
        // This changes no existing individual or army.
        foreach (Race race in Enum.GetValues(typeof(Race)))
        {
            if ((int)race >= 0)
                world.Toggles[$"Merc {race}"] = SpeciesPolicy.IsEnabled(race);
        }
        world.WeightLossFractionBreasts = .2f;
        world.WeightLossFractionBody = .2f;
        world.WeightLossFractionDick = .2f;
        world.GrowthCap = 5f;
        world.GrowthMod = 1f;
        world.GrowthDecayOffset = 0;
        world.GrowthDecayIncreaseRate = 0.04f;
        world.TacticalTerrainFrequency = 10f;
        world.TacticalWaterValue = .29f;
        world.AutoSurrenderChance = 1;
        world.AutoSurrenderDefectChance = 0.25f;
        world.BreastWeight = 40;
        world.BladderWeight = 40;
        world.MonsterConquest = (Config.MonsterConquestType)0;
        world.MalesLike = (Orientation)0;
        world.FemalesLike = (Orientation)0;
        world.WinterStuff = (Config.SeasonalType)0;
        world.FartFraction = .1f;
        world.WeightGainFraction = .5f;
        world.DigestionSpeedMult = 1;
        world.AbsorbSpeedMult = 1.25f;
        world.AbsorbResourceMod = 0.5f;
        world.BellyRubEffMult = 1;
        world.BellyRubsPerTurn = 1;
        world.DigestionRamp = 0;
        world.DigestionRampTurn = 1;
        world.DigestionRampCap = -1;
        world.DigestionRampLoss = 1;
        world.AbsorbResourceModBoost = 0;
        world.AbsorbRamp = 0;
        world.DigestionCap = 0;
        world.DigestionFlatDmg = 0;
        world.DigestionGraceTurns = 0;
        world.SurrenderedPredEscapeMult = 1;
        world.SurrenderedPredAutoRegur = 1f;
        world.ArmyMP = 3;
        world.ScoutMP = 3;
        world.ScoutMax = 4;
        world.ArmyCreationMPMod = 0;
        world.ArmyCreationMPCurve = 1f;
        world.CustomEventFrequency = .25f;
        world.MaxArmies = 32;
        world.MonsterConquestTurns = 1;
        world.VoreRate = 0;
        world.FairyBVType = (FairyBVType)0;
        world.FeedingType = (FeedingType)0;
        world.FourthWallBreakType = (FourthWallBreakType)0;
        world.UBConversion = (UBConversion)0;
        world.GoddessMercy = (GoddessMercy)0;
        world.SucklingPermission = (SucklingPermission)0;
        world.EscapeRate = 0;
        world.FogDistance = 0;
        world.RandomEventRate = 0;
        world.RandomAIEventRate = 0;
        world.DiplomacyScale = (DiplomacyScale)0;
        world.MaxSpellLevelDrop = 4;
        world.MaxEquipmentLevelDrop = 4;
        world.LeaderTraits = RaceEditorPanel.TextToTraitList("");
        world.MaleTraits = RaceEditorPanel.TextToTraitList("");
        world.FemaleTraits = RaceEditorPanel.TextToTraitList("");
        world.HermTraits = RaceEditorPanel.TextToTraitList("");
        world.SpawnTraits = RaceEditorPanel.TextToTraitList("");
        world.OverallMonsterCapModifier = 1;
        world.OverallMonsterSpawnRateModifier = 1;
        world.RevealTurn = 50;
        world.TacticalMovementSoftCap = -1;
        world.TacticalMovementHardCap = -1;
        world.SizeAccuracyMod = 0.01f;
        world.SizeAccuracyLowerBound = 10;
        world.SizeAccuracyInterval = 5;
        world.SizeAccuracyCap = -1;
        world.SizeDamageMod = 0.01f;
        world.SizeDamageLowerBound = 10;
        world.SizeDamageInterval = 5;
        world.SizeDamageCap = -1;
        // These controls had writes but no matching legacy preference reads.
        world.NightRounds = 2;
        world.BaseNightChance = .01f;
        world.NightChanceIncrease = .01f;
        world.DefualtTacticalSightRange = 1;
        world.NightStrategicSightReduction = 1;
        world.DayNightMonsterMovemnt = Config.DayNightMovemntType.Off;
        world.BaseCritChance = .05f;
        world.CritDamageMod = 1.5f;
        world.BaseGrazeChance = .10f;
        world.GrazeDamageMod = .30f;
        if (world.SpawnerInfo == null)
            world.ResetSpawnerDictionary();
        foreach (Race race in Enum.GetValues(typeof(Race)))
        {
            if (HasLegacySpawnerControls(race))
                world.SpawnerInfo[race] = world.SpecifySpawnerDefaults(race);
        }
    }

    internal static void LoadSaved(WorldConfig world)
    {
        SpeciesPolicy.LoadSavedPreferences();
        ApplyLockedValues(world);
        foreach (string key in PreferenceToggles)
            world.Toggles[key] = PlayerPrefs.GetInt(key, ToggleDefaults[key] ? 1 : 0) == 1;
        world.MaleFraction = PlayerPrefs.GetFloat("MaleFraction", .5f);
        world.HermFraction = PlayerPrefs.GetFloat("HermFraction", 0);
        world.HermNameFraction = PlayerPrefs.GetFloat("HermNameFraction", .66f);
        world.ClothedFraction = PlayerPrefs.GetFloat("ClothedFraction", .85f);
        world.FurryFraction = PlayerPrefs.GetFloat("FurryFraction", .5f);
        world.BreastSizeModifier = PlayerPrefs.GetInt("BreastSizeModifier", 0);
        world.CockSizeModifier = PlayerPrefs.GetInt("CockSizeModifier", 0);
        world.DefaultStartingWeight = PlayerPrefs.GetInt("StartingWeight", 2);
        world.OralWeight = PlayerPrefs.GetInt("OralWeight", 40);
        world.AnalWeight = PlayerPrefs.GetInt("AnalWeight", 40);
        world.UnbirthWeight = PlayerPrefs.GetInt("UnbirthWeight", 40);
        world.CockWeight = PlayerPrefs.GetInt("CockWeight", 40);
        world.TailWeight = PlayerPrefs.GetInt("TailWeight", 40);
        world.BurpFraction = PlayerPrefs.GetFloat("BurpFraction", .1f);
        world.HermBreastSizeModifier = PlayerPrefs.GetInt("HermBreastSizeModifier", 0);
    }

    internal static void SavePreferences(WorldConfig world)
    {
        foreach (string key in PreferenceToggles)
            PlayerPrefs.SetInt(key, world.GetValue(key) ? 1 : 0);
        PlayerPrefs.SetFloat("MaleFraction", world.MaleFraction);
        PlayerPrefs.SetFloat("HermFraction", world.HermFraction);
        PlayerPrefs.SetFloat("HermNameFraction", world.HermNameFraction);
        PlayerPrefs.SetFloat("ClothedFraction", world.ClothedFraction);
        PlayerPrefs.SetFloat("FurryFraction", world.FurryFraction);
        PlayerPrefs.SetInt("BreastSizeModifier", world.BreastSizeModifier);
        PlayerPrefs.SetInt("CockSizeModifier", world.CockSizeModifier);
        PlayerPrefs.SetInt("StartingWeight", world.DefaultStartingWeight);
        PlayerPrefs.SetInt("OralWeight", world.OralWeight);
        PlayerPrefs.SetInt("AnalWeight", world.AnalWeight);
        PlayerPrefs.SetInt("UnbirthWeight", world.UnbirthWeight);
        PlayerPrefs.SetInt("CockWeight", world.CockWeight);
        PlayerPrefs.SetInt("TailWeight", world.TailWeight);
        PlayerPrefs.SetFloat("BurpFraction", world.BurpFraction);
        PlayerPrefs.SetInt("HermBreastSizeModifier", world.HermBreastSizeModifier);
        SpeciesPolicy.SavePreferences();
        PlayerPrefs.Save();
    }

    // This is the old page's spawner set, not the future curated species policy.
    internal static bool HasLegacySpawnerControls(Race race)
    {
        return race >= (Race)100 && race < (Race)170 && race != Race.WyvernMatron && race != Race.CoralSlugs
            && race != Race.SpitterSlugs && race != Race.SpringSlugs && race != Race.WarriorAnts;
    }
}

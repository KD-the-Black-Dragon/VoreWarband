// Modified for the VoreWar mercenary-company conversion.
// 2026-11-06: Load packaged balance by default and save developer edits as explicit immutable drafts (spec 40A).
// 2026-10-30: Enforce code-owned settings and retained global preferences (spec 02C).
// 2026-10-30: Remove legacy event initialization and startup/reset event-file seeding (spec 04A).
// 2026-10-30: Retire rejected-species name seeds and reset active lists without deleting paths (spec 03B2).
// 2026-10-31: Retire legacy world load/preview and empire campaign entry routes (spec 05A1).
// 2026-11-01: Keep legacy tactical saves from writing incomplete company campaigns (spec 10).
// 2026-11-02: Replace obsolete campaign-not-available save/tool messages with current checkpoint guidance (spec 12).
// 2026-11-02: Clear the routine race-settings success message and match main-menu load/campaign guidance (spec 15C).
// 2026-11-03: Retire legacy World save writes and route user save/load requests through company slots (spec 19).
// 2026-11-03: Silence the routine default-race-settings message and match the Load Game label (spec 20).
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
using OdinSerializer;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

public static class State
{
    public const string Version = "45D";
    public static World World;
    public static Rand Rand = new Rand();
    public static NameGenerator NameGen;
    public static GameManager GameManager;
    public static AssimilateList AssimilateList;
    public static Dictionary<Traits, TaggedTrait> TieredTraitsList;
    public static List<String> TieredTraitsTagsList;
    public static List<RandomizeList> RandomizeLists;
    public static List<CustomTraitBoost> CustomTraitList;
    public static List<ConditionalTraitContainer> ConditionalTraitList;
    public static List<UnitTag> UnitTagList;
    public static Dictionary<Traits, List<int>> UnitTagAssociatedTraitDictionary;
    public static Dictionary<TaggedTrait, bool> UntaggedTraits;


    internal static RaceSettings RaceSettings;

    public static bool TutorialMode;
    public static bool Warned = false;

    public static string SaveDirectory;
    public static string StorageDirectory;
    public static string MapDirectory;
    public static string CustomTraitDirectory;
    public static string ConditionalTraitDirectory;
    public static string UnitTagDirectory;
    public static string NameFileDirectory;

    public static int RaceSlot;
    public static string RaceSaveDataName;
    // Only active generic/candidate name files are seeded or reset; old personal files remain untouched.
    public static string[] nameTextFileNames = new string[]
    {
        "armyNames", "males", "females", "monsters", "femaleFeralLions", "maleFeralLions",
        "CoralSlugs", "Dragonfly", "Catfish", "femaleAlligators", "maleAlligators", "femaleAnts",
        "maleAnts", "femaleAvians", "maleAvians", "femaleBats", "maleBats", "femaleBees",
        "maleBees", "femaleBunnies", "maleBunnies", "femaleDeer", "maleDeer", "femaleDragon",
        "maleDragon", "femaleDratopyr", "maleDratopyr", "femaleEasternDragon", "maleEasternDragon", "femaleEquines",
        "maleEquines", "femaleFeralBats", "maleFeralBats", "femaleFeralFox", "maleFeralFox", "femaleFeralHorses",
        "maleFeralHorses", "femaleFeralLizards", "maleFeralLizards", "femaleFrogs", "maleFrogs", "femaleGazelle",
        "maleGazelle", "femaleGryphons", "maleGryphons", "femaleHippos", "maleHippos", "femaleKangaroos",
        "maleKangaroos", "femaleKobolds", "maleKobolds", "femaleKomodos", "maleKomodos", "femaleLizards",
        "maleLizards", "femaleMantis", "maleMantis", "femaleMonitors", "maleMonitors", "femalePanthers",
        "malePanthers", "femaleSergal", "maleSergal", "femaleSharks", "maleSharks", "femaleTerrorbird",
        "maleTerrorbird", "femaleVargul", "maleVargul", "femaleWyvern", "maleWyvern", "FeralAnts",
        "FeralWolves", "Harvesters", "RockSlugs", "Salamanders", "Serpents", "SpitterSlugs",
        "SpringSlugs", "WarriorAnts", "WyvernMatron", "femaleGnolls", "maleGnolls", "FeralSlimes",
        "maleLupine", "femaleLupine", "femaleMatronsMinions", "maleMatronsMinions", "femaleJackals", "maleJackals",
        "TwistedVines", "femaleOtachi", "maleOtachi", "femaleRaiju", "maleRaiju", "femaleSmudger",
        "maleSmudger", "femaleBadgers", "maleBadgers", "femaleUtahraptor", "maleUtahraptor", "femaleTrex",
        "maleTrex", "femaleSpaceCroach", "maleSpaceCroach", "femaleMice", "maleMice", "Terminid",
        "femaleFeralUmbreon", "maleFeralUmbreon", "maleDraconians", "femaleDraconians", "maleBlackWidow", "femaleBlackWidow",
    };


    static State()
    {
        if (Application.platform == RuntimePlatform.OSXPlayer)
        {
            SaveDirectory = Application.persistentDataPath + $"Saves{Path.DirectorySeparatorChar}";
            StorageDirectory = Application.persistentDataPath + Path.DirectorySeparatorChar;
            MapDirectory = Application.persistentDataPath + $"Maps{Path.DirectorySeparatorChar}";
            CustomTraitDirectory = Application.persistentDataPath + $"CustomTraits{Path.DirectorySeparatorChar}";
            ConditionalTraitDirectory = Application.persistentDataPath + $"ConditionalTraits{Path.DirectorySeparatorChar}";
            UnitTagDirectory = Application.persistentDataPath + $"UnitTags{Path.DirectorySeparatorChar}";
            NameFileDirectory = Application.persistentDataPath + $"NameFiles{Path.DirectorySeparatorChar}";
        }
        else
        {
            SaveDirectory = $"UserData{Path.DirectorySeparatorChar}Saves{Path.DirectorySeparatorChar}";
            StorageDirectory = $"UserData{Path.DirectorySeparatorChar}";
            MapDirectory = $"UserData{Path.DirectorySeparatorChar}Maps{Path.DirectorySeparatorChar}";
            CustomTraitDirectory = $"UserData{Path.DirectorySeparatorChar}CustomTraits{Path.DirectorySeparatorChar}";
            ConditionalTraitDirectory = $"UserData{Path.DirectorySeparatorChar}ConditionalTraits{Path.DirectorySeparatorChar}";
            UnitTagDirectory = $"UserData{Path.DirectorySeparatorChar}UnitTags{Path.DirectorySeparatorChar}";
            NameFileDirectory = $"UserData{Path.DirectorySeparatorChar}NameFiles{Path.DirectorySeparatorChar}";
        }
#if UNITY_EDITOR
        string diagnosticRoot = Environment.GetEnvironmentVariable("VOREWAR_AUTHORING_USERDATA");
        if (!string.IsNullOrEmpty(diagnosticRoot))
        {
            string logs = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs")) + Path.DirectorySeparatorChar;
            diagnosticRoot = Path.GetFullPath(diagnosticRoot);
            if (!diagnosticRoot.StartsWith(logs, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Authoring diagnostics must stay inside project Logs.");
            StorageDirectory = diagnosticRoot + Path.DirectorySeparatorChar;
            SaveDirectory = Path.Combine(diagnosticRoot, "Saves") + Path.DirectorySeparatorChar;
            MapDirectory = Path.Combine(diagnosticRoot, "Maps") + Path.DirectorySeparatorChar;
            CustomTraitDirectory = Path.Combine(diagnosticRoot, "CustomTraits") + Path.DirectorySeparatorChar;
            ConditionalTraitDirectory = Path.Combine(diagnosticRoot, "ConditionalTraits") + Path.DirectorySeparatorChar;
            UnitTagDirectory = Path.Combine(diagnosticRoot, "UnitTags") + Path.DirectorySeparatorChar;
            NameFileDirectory = Path.Combine(diagnosticRoot, "NameFiles") + Path.DirectorySeparatorChar;
        }
#endif
        try
        {
            Directory.CreateDirectory(StorageDirectory.TrimEnd(new char[] { '\\', '/' }));
            Directory.CreateDirectory(MapDirectory.TrimEnd(new char[] { '\\', '/' }));
            Directory.CreateDirectory(SaveDirectory.TrimEnd(new char[] { '\\', '/' }));
            Directory.CreateDirectory(CustomTraitDirectory.TrimEnd(new char[] { '\\', '/' }));
            Directory.CreateDirectory(ConditionalTraitDirectory.TrimEnd(new char[] { '\\', '/' }));
            Directory.CreateDirectory(UnitTagDirectory.TrimEnd(new char[] { '\\', '/' }));
            Directory.CreateDirectory(NameFileDirectory.TrimEnd(new char[] { '\\', '/' }));
        }
        catch
        {
            SaveDirectory = Application.persistentDataPath + $"Saves{Path.DirectorySeparatorChar}";
            StorageDirectory = Application.persistentDataPath + Path.DirectorySeparatorChar;
            MapDirectory = Application.persistentDataPath + $"Maps{Path.DirectorySeparatorChar}";
            CustomTraitDirectory = Application.persistentDataPath + $"CustomTraits{Path.DirectorySeparatorChar}";
            ConditionalTraitDirectory = Application.persistentDataPath + $"ConditionalTraits{Path.DirectorySeparatorChar}";
            UnitTagDirectory = Application.persistentDataPath + $"UnitTags{Path.DirectorySeparatorChar}";
            NameFileDirectory = Application.persistentDataPath + $"NameFiles{Path.DirectorySeparatorChar}";
            Directory.CreateDirectory(StorageDirectory.TrimEnd(new char[] { '\\', '/' }));
            Directory.CreateDirectory(MapDirectory.TrimEnd(new char[] { '\\', '/' }));
            Directory.CreateDirectory(SaveDirectory.TrimEnd(new char[] { '\\', '/' }));
            Directory.CreateDirectory(CustomTraitDirectory.TrimEnd(new char[] { '\\', '/' }));
            Directory.CreateDirectory(ConditionalTraitDirectory.TrimEnd(new char[] { '\\', '/' }));
            Directory.CreateDirectory(UnitTagDirectory.TrimEnd(new char[] { '\\', '/' }));
            Directory.CreateDirectory(NameFileDirectory.TrimEnd(new char[] { '\\', '/' }));
        }



        string[] systemTextFileNames = new string[] { "customTraits" };

        try
        {
            foreach (string text in systemTextFileNames)
            {
                if (File.Exists($"{StorageDirectory}{text}.txt") == false)
                    File.Copy($"{Application.streamingAssetsPath}{Path.DirectorySeparatorChar}{text}.txt", $"{StorageDirectory}{text}.txt");
            }
        }
        catch
        {
            Debug.Log("Initial setup failed!");
        }
        try
        {
            foreach (string nameList in nameTextFileNames)
            {
                if (File.Exists($"{NameFileDirectory}{nameList}.txt") == false)
                    File.Copy($"{Application.streamingAssetsPath}{Path.DirectorySeparatorChar}{nameList}.txt", $"{NameFileDirectory}{nameList}.txt");
            }
        }
        catch
        {
            Debug.Log("Name setup failed!");
        }


        try
        {
            if (File.Exists($"{StorageDirectory}taggedTraits.json") == false)
                File.Copy($"{Application.streamingAssetsPath}{Path.DirectorySeparatorChar}taggedTraits.json", $"{StorageDirectory}taggedTraits.json");
            if (File.Exists($"{StorageDirectory}buildingConfig.json") == false)
                File.Copy($"{Application.streamingAssetsPath}{Path.DirectorySeparatorChar}buildingConfig.json", $"{StorageDirectory}buildingConfig.json");
        }
        catch
        {
            Debug.Log("Initial setup failed!");
        }

        string bundlePath = Path.Combine($"{Application.streamingAssetsPath}{Path.DirectorySeparatorChar}AssetBundles", "abakhanskya");

        AssetBundleLoader.LoadAssetBundles();

        FlagLoader.FlagLoader flagLoader = new FlagLoader.FlagLoader();
        flagLoader.LoadFlags();
        NameGen = new NameGenerator();
        AssimilateList = new AssimilateList();
        CustomTraitList = new List<CustomTraitBoost>();
        ConditionalTraitList = new List<ConditionalTraitContainer>();
        UnitTagList = new List<UnitTag>();
        UnitTagAssociatedTraitDictionary = new Dictionary<Traits, List<int>>();
        UntaggedTraits = new Dictionary<TaggedTrait, bool>();

        TieredTraitsList = ExternalTraitHandler.TaggedTraitParser(); 
        TieredTraitsList = ExternalTraitHandler.TaggedTraitUpdater();
        TieredTraitsTagsList = new List<string>();
        ExternalTraitHandler.CustomTraitParser();
        ExternalTraitHandler.ConditionalTraitParser();
        ExternalTraitHandler.UnitTagParser();
        TagConditionChecker.CompileTraitTagAssociateDict();
        Encoding encoding = Encoding.GetEncoding("iso-8859-1");
        List<string> lines;
        RandomizeLists = new List<RandomizeList>();
        if (File.Exists($"{State.StorageDirectory}customTraits.txt"))
        {
            var logFile = File.ReadAllLines($"{State.StorageDirectory}customTraits.txt", encoding);
            if (logFile.Any())
            {
                lines = new List<string>(logFile);
                int count = 0;
                lines.ForEach(line =>
                {
                    count++;
                    RandomizeList custom = new RandomizeList();
                    line = new string(line
                       .Where(c => !Char.IsWhiteSpace(c)).ToArray());
                    string[] strings = line.Split(',');
                    if (strings.Length == 4)
                    {
                        custom.id = int.Parse(strings[0]);
                        custom.name = strings[1];
                        custom.chance = float.Parse(strings[2], new CultureInfo("en-US"));
                        custom.level = 0;
                        custom.count = 1;
                        custom.RandomTraits = strings[3].Split('|').ToList().ConvertAll(s => (Traits)int.Parse(s));
                        RandomizeLists.Add(custom);
                    } else if (strings.Length == 6)
                    {
                        custom.id = int.Parse(strings[0]);
                        custom.name = strings[1];
                        custom.chance = float.Parse(strings[2], new CultureInfo("en-US"));
                        custom.count = int.Parse(strings[3]);
                        custom.level = int.Parse(strings[4]);
                        custom.RandomTraits = strings[5].Split('|').ToList().ConvertAll(s => (Traits)int.Parse(s));
                        RandomizeLists.Add(custom);
                    }
                });
            }
        }

        foreach (Traits trait in (Traits[])Enum.GetValues(typeof(Traits)))
        {
            if (TieredTraitsList.Keys.Contains(trait))
            {
                if (TieredTraitsList[trait].tags == null)
                {
                    if (UntaggedTraits.ContainsKey(TieredTraitsList[trait]))
                    {
                        continue;
                    }
                    UntaggedTraits.Add(TieredTraitsList[trait], true);
                    continue;
                }
                if (TieredTraitsList[trait].tags.Count <= 0)
                {
                    UntaggedTraits.Add(TieredTraitsList[trait], true);
                }
            }
            else
            {
                TaggedTrait newTrait = new TaggedTrait();
                newTrait.name = trait.ToString();
                newTrait.tierValue = TraitTier.Neutral;
                newTrait.tier = newTrait.tierValue.ToString();
                newTrait.traitEnum = trait;
                UntaggedTraits.Add(newTrait, false);
            }
        }

        List<TaggedTrait> newTraits = new List<TaggedTrait>();
        foreach (var newTrait in UntaggedTraits)
        {
            if (newTrait.Value)
            {
                continue;
            }
            newTraits.Add(newTrait.Key);
        }

        ExternalTraitHandler.AppendTaggedTrait(newTraits);


    }

    public static void WipeUserdata()
    {
        if (Application.platform == RuntimePlatform.OSXPlayer)
        {
            if (Directory.Exists(Application.persistentDataPath))
                Directory.Delete(Application.persistentDataPath, true);
            SaveDirectory = Application.persistentDataPath + $"Saves{Path.DirectorySeparatorChar}";
            StorageDirectory = Application.persistentDataPath + Path.DirectorySeparatorChar;
            MapDirectory = Application.persistentDataPath + $"Maps{Path.DirectorySeparatorChar}";
            CustomTraitDirectory = Application.persistentDataPath + $"CustomTraits{Path.DirectorySeparatorChar}";
            ConditionalTraitDirectory = Application.persistentDataPath + $"ConditionalTraits{Path.DirectorySeparatorChar}";
            UnitTagDirectory = Application.persistentDataPath + $"UnitTags{Path.DirectorySeparatorChar}";
            NameFileDirectory = Application.persistentDataPath + $"NameFiles{Path.DirectorySeparatorChar}";
        }
        else
        {
            if (Directory.Exists($"UserData"))
                Directory.Delete($"UserData", true);
            SaveDirectory = $"UserData{Path.DirectorySeparatorChar}Saves{Path.DirectorySeparatorChar}";
            StorageDirectory = $"UserData{Path.DirectorySeparatorChar}";
            MapDirectory = $"UserData{Path.DirectorySeparatorChar}Maps{Path.DirectorySeparatorChar}";
            CustomTraitDirectory = $"UserData{Path.DirectorySeparatorChar}CustomTraits{Path.DirectorySeparatorChar}";
            ConditionalTraitDirectory = $"UserData{Path.DirectorySeparatorChar}ConditionalTraits{Path.DirectorySeparatorChar}";
            UnitTagDirectory = $"UserData{Path.DirectorySeparatorChar}UnitTags{Path.DirectorySeparatorChar}";
            NameFileDirectory = $"UserData{Path.DirectorySeparatorChar}NameFiles{Path.DirectorySeparatorChar}";
        }
        try
        {
            Directory.CreateDirectory(StorageDirectory.TrimEnd(new char[] { '\\', '/' }));
            Directory.CreateDirectory(MapDirectory.TrimEnd(new char[] { '\\', '/' }));
            Directory.CreateDirectory(SaveDirectory.TrimEnd(new char[] { '\\', '/' }));
            Directory.CreateDirectory(CustomTraitDirectory.TrimEnd(new char[] { '\\', '/' }));
            Directory.CreateDirectory(ConditionalTraitDirectory.TrimEnd(new char[] { '\\', '/' }));
            Directory.CreateDirectory(UnitTagDirectory.TrimEnd(new char[] { '\\', '/' }));
            Directory.CreateDirectory(NameFileDirectory.TrimEnd(new char[] { '\\', '/' }));
        }
        catch
        {
            SaveDirectory = Application.persistentDataPath + $"Saves{Path.DirectorySeparatorChar}";
            StorageDirectory = Application.persistentDataPath + Path.DirectorySeparatorChar;
            MapDirectory = Application.persistentDataPath + $"Maps{Path.DirectorySeparatorChar}";
            CustomTraitDirectory = Application.persistentDataPath + $"CustomTraits{Path.DirectorySeparatorChar}";
            ConditionalTraitDirectory = Application.persistentDataPath + $"ConditionalTraits{Path.DirectorySeparatorChar}";
            UnitTagDirectory = Application.persistentDataPath + $"UnitTags{Path.DirectorySeparatorChar}";
            NameFileDirectory = Application.persistentDataPath + $"NameFiles{Path.DirectorySeparatorChar}";
            Directory.CreateDirectory(StorageDirectory.TrimEnd(new char[] { '\\', '/' }));
            Directory.CreateDirectory(MapDirectory.TrimEnd(new char[] { '\\', '/' }));
            Directory.CreateDirectory(SaveDirectory.TrimEnd(new char[] { '\\', '/' }));
            Directory.CreateDirectory(CustomTraitDirectory.TrimEnd(new char[] { '\\', '/' }));
            Directory.CreateDirectory(ConditionalTraitDirectory.TrimEnd(new char[] { '\\', '/' }));
            Directory.CreateDirectory(UnitTagDirectory.TrimEnd(new char[] { '\\', '/' }));
            Directory.CreateDirectory(NameFileDirectory.TrimEnd(new char[] { '\\', '/' }));
        }



        string[] systemTextFileNames = new string[] { "customTraits" };

        try
        {
            foreach (string text in systemTextFileNames)
            {
                if (File.Exists($"{StorageDirectory}{text}.txt") == false)
                    File.Copy($"{Application.streamingAssetsPath}{Path.DirectorySeparatorChar}{text}.txt", $"{StorageDirectory}{text}.txt");
            }
        }
        catch
        {
            Debug.Log("Initial setup failed!");
        }
        try
        {
            foreach (string nameList in nameTextFileNames)
            {
                if (File.Exists($"{NameFileDirectory}{nameList}.txt") == false)
                    File.Copy($"{Application.streamingAssetsPath}{Path.DirectorySeparatorChar}{nameList}.txt", $"{NameFileDirectory}{nameList}.txt");
            }
        }
        catch
        {
            Debug.Log("Name setup failed!");
        }


        try
        {
            if (File.Exists($"{StorageDirectory}taggedTraits.json") == false)
                File.Copy($"{Application.streamingAssetsPath}{Path.DirectorySeparatorChar}taggedTraits.json", $"{StorageDirectory}taggedTraits.json");
            if (File.Exists($"{StorageDirectory}buildingConfig.json") == false)
                File.Copy($"{Application.streamingAssetsPath}{Path.DirectorySeparatorChar}buildingConfig.json", $"{StorageDirectory}buildingConfig.json");
        }
        catch
        {
            Debug.Log("Initial setup failed!");
        }

        FlagLoader.FlagLoader flagLoader = new FlagLoader.FlagLoader();
        flagLoader.LoadFlags();
        NameGen = new NameGenerator();
        AssimilateList = new AssimilateList();
        CustomTraitList = new List<CustomTraitBoost>();
        ConditionalTraitList = new List<ConditionalTraitContainer>();
        UnitTagList = new List<UnitTag>();
        UnitTagAssociatedTraitDictionary = new Dictionary<Traits, List<int>>();
        UntaggedTraits = new Dictionary<TaggedTrait, bool>();

        TieredTraitsList = ExternalTraitHandler.TaggedTraitParser();
        TieredTraitsTagsList = new List<string>();
        ExternalTraitHandler.CustomTraitParser();
        ExternalTraitHandler.ConditionalTraitParser();
        ExternalTraitHandler.UnitTagParser();
        TagConditionChecker.CompileTraitTagAssociateDict();
        Encoding encoding = Encoding.GetEncoding("iso-8859-1");
        List<string> lines;
        RandomizeLists = new List<RandomizeList>();
        if (File.Exists($"{State.StorageDirectory}customTraits.txt"))
        {
            var logFile = File.ReadAllLines($"{State.StorageDirectory}customTraits.txt", encoding);
            if (logFile.Any())
            {
                lines = new List<string>(logFile);
                int count = 0;
                lines.ForEach(line =>
                {
                    count++;
                    RandomizeList custom = new RandomizeList();
                    line = new string(line
                       .Where(c => !Char.IsWhiteSpace(c)).ToArray());
                    string[] strings = line.Split(',');
                    if (strings.Length == 4)
                    {
                        custom.id = int.Parse(strings[0]);
                        custom.name = strings[1];
                        custom.chance = float.Parse(strings[2], new CultureInfo("en-US"));
                        custom.level = 0;
                        custom.count = 1;
                        custom.RandomTraits = strings[3].Split('|').ToList().ConvertAll(s => (Traits)int.Parse(s));
                        RandomizeLists.Add(custom);
                    } else if (strings.Length == 6)
                    {
                        custom.id = int.Parse(strings[0]);
                        custom.name = strings[1];
                        custom.chance = float.Parse(strings[2], new CultureInfo("en-US"));
                        custom.count = int.Parse(strings[3]);
                        custom.level = int.Parse(strings[4]);
                        custom.RandomTraits = strings[5].Split('|').ToList().ConvertAll(s => (Traits)int.Parse(s));
                        RandomizeLists.Add(custom);
                    }
                });
            }
        }

        foreach (Traits trait in (Traits[])Enum.GetValues(typeof(Traits)))
        {
            if (TieredTraitsList.Keys.Contains(trait))
            {
                if (TieredTraitsList[trait].tags == null)
                {
                    UntaggedTraits.Add(TieredTraitsList[trait], true);
                    continue;
                }
                if (TieredTraitsList[trait].tags.Count <= 0)
                {
                    UntaggedTraits.Add(TieredTraitsList[trait], true);
                }
            }
            else
            {
                TaggedTrait newTrait = new TaggedTrait();
                newTrait.name = trait.ToString();
                newTrait.tierValue = TraitTier.Neutral;
                newTrait.tier = newTrait.tierValue.ToString();
                newTrait.traitEnum = trait;
                UntaggedTraits.Add(newTrait, false);
            }
        }

        List<TaggedTrait> newTraits = new List<TaggedTrait>();
        foreach (var newTrait in UntaggedTraits)
        {
            if (newTrait.Value)
            {
                continue;
            }
            newTraits.Add(newTrait.Key);
        }

        ExternalTraitHandler.AppendTaggedTrait(newTraits);


    }

    public static void SaveEditedRaces()
    {
        try { BalanceAuthoring.SaveDraft(); BalanceAuthoring.UpdateStatus(); }
        catch (Exception ex) { GameManager.CreateMessageBox("Balance draft was not saved. " + ex.Message); }
    }

    public static void LoadRaceData()
    {
        ChangeRaceSlotUsed(PlayerPrefs.GetInt("RaceEditorSlot", 1));
    }

    public static void ChangeRaceSlotUsed(int num)
    {
        RaceSlot = Math.Max(1, Math.Min(3, num));
        PlayerPrefs.SetInt("RaceEditorSlot", RaceSlot);
        RaceSaveDataName = StorageDirectory + (RaceSlot == 1 ? "EditedRaces.dat" : "EditedRaces" + RaceSlot + ".dat");
        if (!BalanceAuthoring.IsDraft) LoadEditedRaces();
    }

    public static void LoadEditedRaces()
    {
        // Local binary overrides are imported explicitly through the developer workflow.
        BalanceAuthoring.UseOfficial();
    }

    public static void ResetNamelists()
    {
        try
        {
            foreach (string nameList in nameTextFileNames)
            {
                File.Copy($"{Application.streamingAssetsPath}{Path.DirectorySeparatorChar}{nameList}.txt", $"{NameFileDirectory}{nameList}.txt", true);
            }
            NameGen = new NameGenerator();
        }
        catch
        {
            Debug.LogWarning("Namelist refresh failed!");
        }
    }

    public static void ReloadNamelists()
    {
        NameGen = new NameGenerator();
    }


    public static void Save(string filename)
    {
        CompanySaveMenu.QuickSave();
    }

    public const string LegacyLoadUnavailableMessage = "Legacy VoreWar saves cannot be loaded in this conversion. Use Load Game from the main menu to resume a company.";
    public const string CampaignUnavailableMessage = "The empire campaign is retired. Choose New Campaign from the main menu.";

    public static World PreviewSave(string filename)
    {
        // Do not deserialize the unsupported World format, even for a preview.
        return null;
    }

    public static void Load(string filename, bool tutorial = false)
    {
        CompanySaveMenu.Show(GameManager.Start_Mode.CompanySaveView);
    }
}



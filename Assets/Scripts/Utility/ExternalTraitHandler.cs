// 2026-11-06: Isolate developer Unit Tag persistence from normal gameplay (spec 40A).
// Modified for the VoreWar mercenary-company conversion.
// 2026-11-03: Tolerate retired trait metadata and preserve developer Unit Tag identity/save paths (spec 22).
// See CHANGES.md and License.txt for GPL-3.0 terms.
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public enum TraitTier
{
    Harmful,
    Negative,
    Neutral,
    Common,
    Uncommon,
    Rare,
    Epic,
    Elite,
    Legendary,
    Cheat,
    Hidden,
}

public class TaggedTrait
{
    internal string name;
    internal string tier;
    internal TraitTier tierValue;
    internal List<string> tags;
    internal Traits traitEnum;
}

public class ExternalTraitHandler
{
    // Obsolete names are ignored per row, including in existing local metadata files.
    static Dictionary<Traits, TaggedTrait> ReadTaggedTraits(string path)
    {
        var traits = new Dictionary<Traits, TaggedTrait>();
        if (!File.Exists(path)) return traits;
        JArray rows;
        try { rows = JObject.Parse(File.ReadAllText(path))["traits"] as JArray; }
        catch (Exception ex)
        {
            Debug.LogWarning("Could not read trait categories from " + path + ": " + ex.Message);
            return traits;
        }
        if (rows == null) return traits;
        foreach (JToken row in rows)
        {
            string name = null;
            try
            {
                if (!(row is JObject)) continue;
                name = (string)row["name"];
                Traits id;
                TraitTier tier;
                if (!Enum.TryParse(name, out id) || !Enum.IsDefined(typeof(Traits), id))
                    continue; // Retired names must not abort later valid entries.
                if (!Enum.TryParse((string)row["tier"], out tier) || !Enum.IsDefined(typeof(TraitTier), tier))
                    tier = TraitTier.Neutral;
                traits[id] = new TaggedTrait
                {
                    name = id.ToString(), traitEnum = id, tierValue = tier, tier = tier.ToString(),
                    tags = row["tags"] is JArray ? row["tags"].ToObject<List<string>>() : new List<string>()
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Could not read categories for " + name + ": " + ex.Message);
            }
        }
        return traits;
    }

    public static Dictionary<Traits, TaggedTrait> TaggedTraitParser()
    {
        return ReadTaggedTraits(Path.Combine(State.StorageDirectory, "taggedTraits.json"));
    }

    // Keep valid local edits; fill missing metadata from the shipped catalog.
    public static Dictionary<Traits, TaggedTrait> TaggedTraitUpdater()
    {
        var updated = new Dictionary<Traits, TaggedTrait>(State.TieredTraitsList);
        foreach (var pair in ReadTaggedTraits(Path.Combine(Application.streamingAssetsPath, "taggedTraits.json")))
            if (!updated.ContainsKey(pair.Key)) updated.Add(pair.Key, pair.Value);
        string path = Path.Combine(State.StorageDirectory, "taggedTraits.json");
        if (File.Exists(path)) File.Copy(path, path + ".bak", true);
        var root = new RootObject { traits = new List<TaggedTraitTempClass>() };
        foreach (TaggedTrait trait in updated.Values)
            root.traits.Add(new TaggedTraitTempClass
            {
                name = trait.name, tier = trait.tier, tags = trait.tags,
                traitEnum = trait.traitEnum, tierValue = trait.tierValue
            });
        File.WriteAllText(path, JsonConvert.SerializeObject(root));
        return updated;
    }

    public static void AppendTaggedTrait(List<TaggedTrait> newTrait)
    {
        string json = File.ReadAllText(State.StorageDirectory + "\\taggedTraits.json");
        var rootObject = new RootObject();
        JsonConvert.PopulateObject(json, rootObject);
        foreach (TaggedTrait trait in newTrait) 
        {
            TaggedTraitTempClass toBeAdded = new TaggedTraitTempClass();
            toBeAdded.name = trait.name;
            toBeAdded.tier = trait.tier;
            toBeAdded.tier = trait.tier.ToString();
            toBeAdded.traitEnum = trait.traitEnum;
            toBeAdded.tierValue = trait.tierValue;
            rootObject.traits.Add(toBeAdded);
        }
        using (StreamWriter file = File.CreateText(State.StorageDirectory + "\\taggedTraits.json"))
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Serialize(file, rootObject);
        }
    }

    public static void CustomTraitParser()
    {
        string readContents;
        string[] files = Directory.GetFiles(State.CustomTraitDirectory, "*.json", SearchOption.AllDirectories);
        List<CustomTraitBoost> customTraitsList = new List<CustomTraitBoost>();

        foreach (string file in files) 
        {
            using (StreamReader streamReader = new StreamReader(file))
            {
                readContents = streamReader.ReadToEnd();
            }
            JObject results = JObject.Parse(readContents);
            
            CustomTraitBoost traitBoost = new CustomTraitBoost();
            traitBoost.id = results["id"].ToObject<int>();
            traitBoost.name = results["name"].ToString();
            traitBoost.description = results["description"].ToString();
            traitBoost.tags = results["tags"].ToObject<List<string>>();
            traitBoost.tier = results["tier"].ToObject<TraitTier>();
            traitBoost.comps = results["comps"].ToObject<Dictionary<CustomTraitComp, float>>();

            State.CustomTraitList.Add(traitBoost);

        }
    }
    public static bool CustomTraitSaver(CustomTraitBoost trait)
    {
        bool modifying = false;
        foreach (var item in State.CustomTraitList)
        {
            if (item.name.ToLower() == trait.name.ToLower() && item.id == trait.id)
            {
                modifying = true;
                break;
            }
        }

        if (!modifying && File.Exists($"{State.CustomTraitDirectory}{trait.name}.json")) 
        { 
            return false;
        }

        using (StreamWriter sw = new StreamWriter($"{State.CustomTraitDirectory}{trait.name}.json"))
        {
            sw.WriteLine(JsonConvert.SerializeObject(trait));

        }
        return true;
    }

    public static void CustomTraitRemover(CustomTraitBoost trait)
    {
        if (File.Exists($"{State.CustomTraitDirectory}{trait.name}.json"))
            File.Delete($"{State.CustomTraitDirectory}{trait.name}.json");
    }

    public static void ConditionalTraitParser()
    {
        string readContents;
        string[] files = Directory.GetFiles(State.ConditionalTraitDirectory, "*.json", SearchOption.AllDirectories);
        List<ConditionalTraitContainer> customTraitsList = new List<ConditionalTraitContainer>();

        foreach (string file in files)
        {
            using (StreamReader streamReader = new StreamReader(file))
            {
                readContents = streamReader.ReadToEnd();
            }
            JObject results = JObject.Parse(readContents);

            ConditionalTraitContainer conditionalCont = new ConditionalTraitContainer();
            conditionalCont.OperationBlocks = new Dictionary<ConditionalTraitOperationBlock, TraitConditionLogicalOperator>();
            conditionalCont.id = results["id"].ToObject<int>();
            conditionalCont.name = results["name"].ToString();
            conditionalCont.classification = results["classification"].ToObject<TraitConditionalClassification>();
            conditionalCont.trigger = results["trigger"].ToObject<TraitConditionTrigger>();
            conditionalCont.associatedTrait = results["associatedTrait"].ToObject<Traits>();
            var opBlockKeys = results["operationBlocksKeys"].ToObject<List<OperationBlockTempClass>>();
            var opBlockValues = results["operationBlocksValues"].ToObject<List<TraitConditionLogicalOperator>>();
            int counter = 0;
            foreach (var opBlockKey in opBlockKeys)
            {
                ConditionalTraitOperationBlock opBlock = new ConditionalTraitOperationBlock();
                opBlock.summary = opBlockKey.summary;
                opBlock.compareOp = opBlockKey.compareOp;
                opBlock.compareValue = opBlockKey.compareValue;
                opBlock.filled = opBlockKey.filled;
                opBlock.conditionVariable = opBlockKey.conditionVariable;
                opBlock.arithmeticOperator = opBlockKey.arithmeticOperator;

                conditionalCont.OperationBlocks.Add(opBlock, opBlockValues[counter]);
                counter++;
            }

            State.ConditionalTraitList.Add(conditionalCont);

        }
    }
    public static bool ConditionalTraitSaver(ConditionalTraitContainer trait)
    {
        ConditionalTraitRemover(trait);

        var rootObject = new ConditionalTraitTempClass();
        rootObject.id = trait.id;
        rootObject.name = trait.name;
        rootObject.classification = trait.classification;
        rootObject.trigger = trait.trigger;
        rootObject.associatedTrait = trait.associatedTrait;
        rootObject.operationBlocksKeys = new List<OperationBlockTempClass>();
        rootObject.operationBlocksValues = new List<TraitConditionLogicalOperator>();

        foreach (var item in trait.OperationBlocks)
        {
            OperationBlockTempClass opBlock = new OperationBlockTempClass();
            opBlock.summary = item.Key.summary;
            opBlock.compareOp = item.Key.compareOp;
            opBlock.compareValue = item.Key.compareValue;
            opBlock.filled = item.Key.filled;
            opBlock.conditionVariable = item.Key.conditionVariable;
            opBlock.arithmeticOperator = item.Key.arithmeticOperator;

            rootObject.operationBlocksKeys.Add(opBlock);
            rootObject.operationBlocksValues.Add(item.Value);
        }

        using (StreamWriter file = File.CreateText($"{State.ConditionalTraitDirectory}{trait.name}.json"))
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Serialize(file, rootObject);
        }

        return true;

    }
    public static void ConditionalTraitRemover(ConditionalTraitContainer trait)
    {
        if (File.Exists($"{State.ConditionalTraitDirectory}{trait.name}.json"))
            File.Delete($"{State.ConditionalTraitDirectory}{trait.name}.json");
    }

    static readonly Dictionary<int, string> unitTagPaths = new Dictionary<int, string>();

    public static bool UnitTagSaver(UnitTag tag)
    {
        if (BalanceAuthoring.IsDraft)
        {
            try
            {
                BalanceAuthoring.SaveDraft();
                TagConditionChecker.CompileTraitTagAssociateDict();
                return true;
            }
            catch (Exception ex)
            {
                State.GameManager.CreateMessageBox("Unit Tag draft was not saved. " + ex.Message);
                return false;
            }
        }

        string path;
        if (!unitTagPaths.TryGetValue(tag.id, out path))
        {
            string filename = string.Concat(tag.name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
            if (string.IsNullOrWhiteSpace(filename)) filename = "UnitTag";
            path = Path.Combine(State.UnitTagDirectory, filename + "-" + tag.id + ".json");
        }
        File.WriteAllText(path, JsonConvert.SerializeObject(tag));
        unitTagPaths[tag.id] = path;
        TagConditionChecker.CompileTraitTagAssociateDict();
        return true;
    }

    public static void UnitTagRemover(UnitTag tag)
    {
        if (BalanceAuthoring.IsDraft) return; // Draft generations retain earlier versions.

        string path;
        if (!unitTagPaths.TryGetValue(tag.id, out path))
            path = Path.Combine(State.UnitTagDirectory, tag.name + ".json");
        if (File.Exists(path)) File.Delete(path);
        unitTagPaths.Remove(tag.id);
    }

    public static void UnitTagParser()
    {
        // Official definitions load with the official profile. Legacy imports are explicit.
    }


    class RootObject
    {
        public List<TaggedTraitTempClass> traits { get; set; }
    }
    class TaggedTraitTempClass
    {
        public string name { get; set; }
        public string tier { get; set; }
        public List<string> tags { get; set; }
        public TraitTier tierValue { get; set; }
        public Traits traitEnum { get; set; }
    }


    class ConditionalTraitTempClass
    {
        public int id { get; set; }
        public string name { get; set; }
        public TraitConditionalClassification classification { get; set; }
        public bool active { get; set; }
        public TraitConditionTrigger trigger { get; set; }
        public Traits associatedTrait { get; set; }
        public List<OperationBlockTempClass> operationBlocksKeys { get; set; }
        public List<TraitConditionLogicalOperator> operationBlocksValues { get; set; }
    }
    class OperationBlockTempClass
    {
        public string summary { get; set; }
        public TraitConditionCompareOperator compareOp { get; set; }
        public float compareValue { get; set; }
        public bool filled { get; set; }
        public List<TraitCondition> conditionVariable { get; set; }
        public List<TraitConditionArithmeticOperator> arithmeticOperator { get; set; }
    }

    class UnitTagTempClass 
    {
        public int id { get; set; }
        public string name { get; set; }
        public List<UnitTagModifierTempClass> modifiers { get; set; }
        public List<Traits> AssociatedTraits { get; set; }
    }

    class UnitTagModifierTempClass
    {
        public int id { get; set; }
        public UnitTagModifierEffect tagEffect { get; set; }
        public UnitTagModifierCase tagCase { get; set; }
        public UnitTagModifierTarget target { get; set; }
        public float modifierValue { get; set; }
        public int targetValue { get; set; }
    }
}

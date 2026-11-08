// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-06: Versioned official species balance, isolated developer drafts and validated interchange (spec 40A).
// See CHANGES.md and License.txt for GPL-3.0 terms. Existing dependency notices remain applicable.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using OdinSerializer;
using UnityEngine;

[Serializable]
internal sealed class SpeciesBalanceEntry
{
    [JsonProperty(Required = Required.Always)]
    public Race Species;
    [JsonProperty(Required = Required.Always)]
    public RaceSettingsItem Settings;
}

[Serializable]
internal sealed class BalanceProfile
{
    [JsonProperty(Required = Required.Always)] public int Schema = 1;
    [JsonProperty(Required = Required.Always)] public string Revision;
    public string ParentRevision;
    public string ParentHash;
    [JsonProperty(Required = Required.Always)] public string ModifiedDate;
    [JsonProperty(Required = Required.Always)] public List<SpeciesBalanceEntry> Species = new List<SpeciesBalanceEntry>();
    [JsonProperty(Required = Required.Always)] public List<UnitTag> UnitTags = new List<UnitTag>();
    // Reserved for the reviewed 40D inventory. Empty means unclassified, not Neutral.
    public Dictionary<string, string> TraitClassifications = new Dictionary<string, string>();
}

internal static class BalanceAuthoring
{
    internal const string ResourceRoot = "WarbandBalance";
    internal const string LegacyRevision = "legacy-pre40a";
    const int MaximumBytes = 2 * 1024 * 1024;
    static BalanceProfile official;
    static string officialHash;
    static string draftParentRevision, draftParentHash;
    static BalanceProfile editorBackup;
    static bool editorWasDraft;
    internal static bool IsDraft { get; private set; }
    internal static string Diagnostic { get; private set; }
    internal static string OfficialRevision { get { EnsureLoaded(); return official.Revision; } }
    internal static string OfficialHash { get { EnsureLoaded(); return officialHash; } }
    internal static string Identity => OfficialRevision + (IsDraft ? " / developer draft " + Hash(Write(Capture())).Substring(0, 12) : "");

    sealed class SettingsResolver : DefaultContractResolver
    {
        protected override JsonObjectContract CreateObjectContract(Type type)
        {
            var contract = base.CreateObjectContract(type);
            if (type == typeof(RaceSettingsItem))
            {
                contract.DefaultCreator = () => System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
                contract.OverrideCreator = null;
                contract.CreatorParameters.Clear();
            }
            return contract;
        }
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization serialization)
        {
            if (type != typeof(RaceSettingsItem)) return base.CreateProperties(type, serialization);
            return type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .OrderBy(f => f.Name, StringComparer.Ordinal).Select(f =>
                {
                    var property = base.CreateProperty(f, MemberSerialization.Fields);
                    property.Readable = property.Writable = true;
                    property.Required = Required.Always;
                    return property;
                }).ToList();
        }
    }

    sealed class StatsConverter : JsonConverter
    {
        public override bool CanConvert(Type type) { return type == typeof(RaceStats); }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var stats = Copy((RaceStats)value);
            stats.Consolidate();
            var result = new JObject();
            foreach (var pair in new[] {
                new KeyValuePair<string, RaceStats.StatRange>("Strength", stats.Strength),
                new KeyValuePair<string, RaceStats.StatRange>("Agility", stats.Agility),
                new KeyValuePair<string, RaceStats.StatRange>("Intelligence", stats.Mind),
                new KeyValuePair<string, RaceStats.StatRange>("Voracity", stats.Voracity) })
                result[pair.Key] = new JObject { ["Minimum"] = pair.Value.Minimum, ["Maximum"] = pair.Value.Minimum + pair.Value.Roll - 1 };
            result.WriteTo(writer);
        }
        public override object ReadJson(JsonReader reader, Type type, object existing, JsonSerializer serializer)
        {
            var value = JObject.Load(reader);
            if (value.Properties().Count() != 4) throw new InvalidDataException("Stats must contain exactly STR, AGI, INT and VOR ranges.");
            var ranges = new List<RaceStats.StatRange>();
            foreach (string key in new[] { "Strength", "Agility", "Intelligence", "Voracity" })
            {
                var range = value[key] as JObject;
                if (range == null || range.Properties().Count() != 2 || range["Minimum"] == null || range["Maximum"] == null ||
                    range["Minimum"].Type != JTokenType.Integer || range["Maximum"].Type != JTokenType.Integer)
                    throw new InvalidDataException("Missing stat range " + key);
                int minimum = range["Minimum"].Value<int>(), maximum = range["Maximum"].Value<int>();
                if (minimum < 1 || maximum < minimum || maximum > 10000) throw new InvalidDataException("Invalid stat range " + key);
                ranges.Add(new RaceStats.StatRange(minimum, maximum));
            }
            var stats = new RaceStats { Strength = ranges[0], Agility = ranges[1], Mind = ranges[2], Voracity = ranges[3] };
            stats.Mirror();
            return stats;
        }
    }

    static JsonSerializerSettings JsonSettings()
    {
        return new JsonSerializerSettings
        {
            ContractResolver = new SettingsResolver(),
            TypeNameHandling = TypeNameHandling.None,
            MissingMemberHandling = MissingMemberHandling.Error,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            Culture = System.Globalization.CultureInfo.InvariantCulture,
            MaxDepth = 32,
            Converters = new List<JsonConverter> { new StringEnumConverter { AllowIntegerValues = false }, new StatsConverter() }
        };
    }
    internal static T Copy<T>(T value)
    {
        return SerializationUtility.DeserializeValue<T>(SerializationUtility.SerializeValue(value, DataFormat.Binary), DataFormat.Binary);
    }
    internal static string Write(BalanceProfile profile)
    {
        using (var text = new StringWriter(System.Globalization.CultureInfo.InvariantCulture))
        {
            text.NewLine = "\n";
            using (var writer = new JsonTextWriter(text))
            {
                writer.Formatting = Formatting.Indented;
                JsonSerializer.Create(JsonSettings()).Serialize(writer, profile);
                return text.ToString();
            }
        }
    }
    internal static BalanceProfile Read(string text)
    {
        if (string.IsNullOrEmpty(text) || Encoding.UTF8.GetByteCount(text) > MaximumBytes) throw new InvalidDataException("Balance package is empty or too large.");
        var profile = JsonConvert.DeserializeObject<BalanceProfile>(text, JsonSettings());
        Validate(profile);
        return profile;
    }
    internal static string Hash(string text)
    {
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant();
    }
    internal static bool HasMetadata(Race race)
    {
        return race != Race.none && !SpeciesPolicy.IsRetired(race) && RaceParameters.GetRaceTraits(race) != null;
    }
    internal static bool SafeRevision(string revision)
    {
        return revision != null && System.Text.RegularExpressions.Regex.IsMatch(revision, "^[a-zA-Z0-9][a-zA-Z0-9-]{0,95}$");
    }
    internal static void Validate(BalanceProfile profile)
    {
        if (profile == null || profile.Schema != 1 || !SafeRevision(profile.Revision)) throw new InvalidDataException("Unsupported balance schema or revision.");
        if ((profile.ParentRevision != null && !SafeRevision(profile.ParentRevision)) ||
            (profile.ParentHash != null && !System.Text.RegularExpressions.Regex.IsMatch(profile.ParentHash, "^[a-f0-9]{64}$")))
            throw new InvalidDataException("Invalid parent revision or hash.");
        DateTime date;
        if (!DateTime.TryParseExact(profile.ModifiedDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out date)) throw new InvalidDataException("Balance modification date must be YYYY-MM-DD.");
        if (profile.Species == null || profile.Species.Count == 0 || profile.Species.Count > 256 || profile.UnitTags == null || profile.UnitTags.Count > 1000)
            throw new InvalidDataException("Missing or excessive species/tag data.");
        var races = new HashSet<Race>();
        var tags = new HashSet<int>();
        foreach (var tag in profile.UnitTags)
        {
            if (tag == null || tag.id < 0 || !tags.Add(tag.id) || string.IsNullOrWhiteSpace(tag.name) || tag.modifiers == null || tag.AssociatedTraits == null)
                throw new InvalidDataException("Unit Tags need unique nonnegative IDs, names and modifier/trait lists.");
            ValidateTraits(tag.AssociatedTraits);
        }
        foreach (var tag in profile.UnitTags)
            foreach (var modifier in tag.modifiers)
            {
                if (modifier == null || !Enum.IsDefined(typeof(UnitTagModifierEffect), modifier.tagEffect) ||
                    !Enum.IsDefined(typeof(UnitTagModifierCase), modifier.tagCase) || !Enum.IsDefined(typeof(UnitTagModifierTarget), modifier.target) ||
                    float.IsNaN(modifier.modifierValue) || float.IsInfinity(modifier.modifierValue)) throw new InvalidDataException("Invalid modifier in Unit Tag " + tag.name);
                if (modifier.target == UnitTagModifierTarget.IsRace && !HasMetadata((Race)modifier.targetValue)) throw new InvalidDataException("Unit Tag references unavailable species.");
                if (modifier.target == UnitTagModifierTarget.HasTrait) ValidateTraits(new[] { (Traits)modifier.targetValue });
                if (modifier.target == UnitTagModifierTarget.HasTag && !tags.Contains(modifier.targetValue)) throw new InvalidDataException("Unit Tag references a missing tag.");
            }
        foreach (var entry in profile.Species)
        {
            if (entry == null || !HasMetadata(entry.Species) || !races.Add(entry.Species) || entry.Settings == null)
                throw new InvalidDataException("Duplicate, retired or unavailable species in balance package.");
            var item = entry.Settings;
            if (item.Stats == null || item.BaseAP < 1 || item.BaseAP > 100 || item.BodySize < 1 || item.StomachSize < 1 ||
                item.CanUseMeleeWeapons == null || item.CanUseRangedWeapons == null || item.RaceTags == null || item.AllowedVoreTypes == null)
                throw new InvalidDataException("Incomplete species balance for " + entry.Species);
            item.Stats.Consolidate();
            foreach (var range in new[] { item.Stats.Strength, item.Stats.Agility, item.Stats.Mind, item.Stats.Voracity })
                if (range.Minimum < 1 || range.Roll < 1 || (long)range.Minimum + range.Roll - 1 > 10000) throw new InvalidDataException("Invalid species stats.");
            foreach (var list in new[] { item.RaceTraits, item.MaleTraits, item.FemaleTraits, item.HermTraits, item.LeaderTraits, item.SpawnTraits }) ValidateTraits(list);
            if (item.RaceTags.Any(id => !tags.Contains(id))) throw new InvalidDataException("Missing Unit Tag definition for " + entry.Species);
            foreach (Race mapping in new[] { item.SpawnRace, item.LeaderRace, item.ConversionRace, item.MorphRace })
                if (mapping != Race.none && !HasMetadata(mapping)) throw new InvalidDataException("Unavailable mapping for " + entry.Species);
            if (!Enum.IsDefined(typeof(RaceAI), item.RaceAI) || !WarbandStats.Active.Contains(WarbandStats.Canonical(item.FavoredStat))) throw new InvalidDataException("Invalid species specialty or AI.");
            if (item.InnateSpell != SpellTypes.None && (!Enum.IsDefined(typeof(SpellTypes), item.InnateSpell) || new Spell { SpellType = item.InnateSpell }.IsRetired))
                throw new InvalidDataException("Unavailable innate spell for " + entry.Species);
            foreach (float number in new[] { item.maleFraction, item.hermFraction, item.furryFraction, item.clothedFraction, item.PowerAdjustment, item.Upkeep, item.DeployCost })
                if (float.IsNaN(number) || float.IsInfinity(number) || number < 0) throw new InvalidDataException("Invalid species numeric value.");
            if (item.maleFraction > 1 || item.hermFraction > 1 || item.furryFraction > 1 || item.clothedFraction > 1 || item.PowerAdjustment <= 0 ||
                item.MinWeight < 0 || item.MaxWeight < item.MinWeight || item.MinBoob < 0 || item.MaxBoob < item.MinBoob || item.MinDick < 0 || item.MaxDick < item.MinDick ||
                item.AllowedVoreTypes.Any(v => !Enum.IsDefined(typeof(VoreType), v))) throw new InvalidDataException("Invalid species range or fraction.");
        }
        if (SpeciesPolicy.GetCuratedSpecies().Any(r => !races.Contains(r))) throw new InvalidDataException("A curated species is missing from the package.");
        if (profile.TraitClassifications == null) throw new InvalidDataException("Missing trait classification map.");
        foreach (var pair in profile.TraitClassifications)
        {
            Traits trait;
            if (!Enum.TryParse(pair.Key, out trait)) throw new InvalidDataException("Unknown classified trait.");
            ValidateTraits(new[] { trait });
            if (pair.Value != "Good" && pair.Value != "Bad" && pair.Value != "Neutral") throw new InvalidDataException("Invalid trait classification.");
        }
    }
    static void ValidateTraits(IEnumerable<Traits> traits)
    {
        if (traits == null || traits.Any(t => !Enum.IsDefined(typeof(Traits), t) || TraitsMethods.IsRetired(t)))
            throw new InvalidDataException("A trait is missing, retired or depends on unsupported external trait code.");
    }
    internal static BalanceProfile BuiltinProfile()
    {
        var settings = new RaceSettings();
        var profile = new BalanceProfile { Revision = LegacyRevision, ModifiedDate = "2026-11-06" };
        foreach (Race race in Enum.GetValues(typeof(Race)))
        {
            if (!HasMetadata(race)) continue;
            var item = Copy(settings.GetExisting(race) ?? new RaceSettingsItem(race, false));
            item.LeaderTraits = new List<Traits>(settings.GetLeaderRaceTraits(race) ?? new List<Traits>());
            item.SpawnTraits = new List<Traits>(settings.GetSpawnRaceTraits(race) ?? new List<Traits>());
            item.RaceAI = settings.GetRaceAI(race);
            Normalize(item, race);
            profile.Species.Add(new SpeciesBalanceEntry { Species = race, Settings = item });
        }
        Validate(profile);
        return profile;
    }
    static void Normalize(RaceSettingsItem item, Race race)
    {
        item.Stats.Consolidate();
        item.BaseAP = item.BaseAP == 0 ? WarbandStats.BuiltinAP(race) : item.BaseAP;
        item.CanUseMeleeWeapons = item.CanUseMeleeWeapons ?? RaceParameters.GetRaceTraits(race).CanUseMeleeWeapons;
        item.CanUseRangedWeapons = item.CanUseRangedWeapons ?? RaceParameters.GetRaceTraits(race).CanUseRangedWeapons;
        foreach (var list in new[] { item.RaceTraits, item.MaleTraits, item.FemaleTraits, item.HermTraits, item.LeaderTraits, item.SpawnTraits })
            if (list != null) list.RemoveAll(t => !Enum.IsDefined(typeof(Traits), t) || TraitsMethods.IsRetired(t));
    }
    internal static BalanceProfile LoadCatalog(string catalog, Func<string, string> read, out string diagnostic)
    {
        diagnostic = null;
        var lines = (catalog ?? "").Split('\n').Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
        for (int i = lines.Length - 1; i >= 0; i--)
        {
            try
            {
                var fields = lines[i].Trim().Split('|');
                if (fields.Length != 2 || !SafeRevision(fields[0])) throw new InvalidDataException("Incomplete balance commit.");
                string text = read(fields[0]);
                if (Hash(text) != fields[1]) throw new InvalidDataException("Balance checksum mismatch.");
                var profile = Read(text);
                if (profile.Revision != fields[0]) throw new InvalidDataException("Balance revision mismatch.");
                return profile;
            }
            catch (Exception ex) { diagnostic = "Balance commit recovery: " + ex.Message; }
        }
        throw new InvalidDataException("No valid official balance commit. " + diagnostic);
    }
    static void EnsureLoaded()
    {
        if (official != null) return;
        var catalog = Resources.Load<TextAsset>(ResourceRoot + "/active");
        if (catalog == null)
        {
#if UNITY_EDITOR
            official = BuiltinProfile();
            Diagnostic = "Official balance needs initial project promotion.";
#else
            throw new InvalidDataException("Packaged official balance is missing.");
#endif
        }
        else
        {
            string diagnostic;
            official = LoadCatalog(catalog.text, revision =>
            {
                var asset = Resources.Load<TextAsset>(ResourceRoot + "/Profiles/" + revision);
                if (asset == null) throw new InvalidDataException("Missing balance revision " + revision);
                return asset.text;
            }, out diagnostic);
            Diagnostic = diagnostic;
        }
        officialHash = Hash(Write(official));
    }
    internal static RaceSettings Settings(BalanceProfile profile)
    {
        var settings = new RaceSettings(false);
        foreach (var entry in profile.Species) settings.Set(entry.Species, Copy(entry.Settings));
        return settings;
    }
    static void Install(BalanceProfile profile, bool draft)
    {
        Validate(profile);
        var settings = Settings(profile);
        var tags = Copy(profile.UnitTags);
        State.RaceSettings = settings;
        State.UnitTagList = tags;
        TagConditionChecker.CompileTraitTagAssociateDict();
        IsDraft = draft;
    }
    internal static void UseOfficial()
    {
        EnsureLoaded();
        Install(official, false);
        UpdateStatus();
    }
#if UNITY_EDITOR
    internal static void ReloadOfficial()
    {
        official = null;
        UseOfficial();
    }
#endif
    internal static BalanceProfile Capture()
    {
        EnsureLoaded();
        var profile = Copy(official);
        profile.ParentRevision = IsDraft ? draftParentRevision : official.Revision;
        profile.ParentHash = IsDraft ? draftParentHash : officialHash;
        profile.UnitTags = Copy(State.UnitTagList);
        foreach (var entry in profile.Species)
        {
            entry.Settings = Copy(State.RaceSettings.GetExisting(entry.Species) ?? entry.Settings);
            Normalize(entry.Settings, entry.Species);
        }
        return profile;
    }
    internal static void BeginDraft()
    {
        if (IsDraft) return;
        EnsureLoaded();
        Install(official, true);
        draftParentRevision = official.Revision;
        draftParentHash = officialHash;
        UpdateStatus();
    }
    internal static void BeginEditorSession()
    {
        EnsureLoaded();
        editorWasDraft = IsDraft;
        editorBackup = IsDraft ? Capture() : Copy(official);
        BeginDraft();
    }
    internal static void CancelEditorSession()
    {
        if (editorWasDraft && editorBackup != null) ApplyDraft(editorBackup);
        else UseOfficial();
        editorBackup = null;
    }
    internal static void ResetSpecies(Race race)
    {
        EnsureLoaded();
        var entry = official.Species.FirstOrDefault(e => e.Species == race);
        if (entry == null) throw new InvalidOperationException("No official balance for this species.");
        State.RaceSettings.Set(race, Copy(entry.Settings));
    }
    internal static BalanceProfile LegacyProfile()
    {
        var asset = Resources.Load<TextAsset>(ResourceRoot + "/Profiles/" + LegacyRevision);
        return asset == null ? BuiltinProfile() : Read(asset.text);
    }
    internal static void ApplyDraft(BalanceProfile profile)
    {
        Install(profile, true);
        draftParentRevision = profile.ParentRevision;
        draftParentHash = profile.ParentHash;
        UpdateStatus();
    }
    internal static string Export(string path)
    {
        if (!IsDraft) throw new InvalidOperationException("Open an explicit developer draft first.");
        var profile = Capture();
        profile.Revision = "draft-" + Guid.NewGuid().ToString("N");
        profile.ModifiedDate = DateTime.Now.ToString("yyyy-MM-dd");
        Validate(profile);
        string text = Write(profile);
        Read(text);
        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        using (var writer = new StreamWriter(new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None), new UTF8Encoding(false))) writer.Write(text);
        return fullPath;
    }
    internal static string SaveDraft()
    {
        string path = Path.Combine(State.StorageDirectory, "BalanceDrafts", "slot-" + State.RaceSlot,
            DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfffffff") + "-" + Guid.NewGuid().ToString("N") + ".json");
        return Export(path);
    }
    internal static string LoadSavedDraft()
    {
        string directory = Path.Combine(State.StorageDirectory, "BalanceDrafts", "slot-" + State.RaceSlot);
        if (!Directory.Exists(directory)) throw new FileNotFoundException("No saved balance draft exists in this slot.");
        int skipped = 0;
        foreach (string file in Directory.GetFiles(directory, "*.json").OrderByDescending(f => f, StringComparer.Ordinal))
        {
            BalanceProfile profile;
            try { profile = Read(ReadFile(file)); }
            catch { skipped++; continue; }
            ApplyDraft(profile);
            return "Loaded " + Path.GetFileName(file) + (skipped == 0 ? "" : ". Skipped " + skipped + " invalid draft(s), retained for recovery.");
        }
        throw new InvalidDataException("No valid saved draft. All original files are retained.");
    }
    internal static void ImportDraft(string path) { ApplyDraft(Read(ReadFile(path))); }
    internal static string Describe(BalanceProfile before, BalanceProfile after)
    {
        var lines = new List<string>();
        var oldRows = JArray.Parse(JObject.Parse(Write(before))["Species"].ToString());
        var newRows = JArray.Parse(JObject.Parse(Write(after))["Species"].ToString());
        foreach (var row in newRows)
        {
            string race = row["Species"].Value<string>();
            var previous = oldRows.FirstOrDefault(r => r["Species"].Value<string>() == race);
            if (previous == null) { lines.Add(race + " added"); continue; }
            foreach (var property in ((JObject)row["Settings"]).Properties())
                if (!JToken.DeepEquals(previous["Settings"][property.Name], property.Value))
                    lines.Add(race + " / " + property.Name + "\n  " + previous["Settings"][property.Name].ToString(Formatting.None) +
                        " -> " + property.Value.ToString(Formatting.None));
        }
        foreach (var row in oldRows)
            if (!newRows.Any(r => r["Species"].Value<string>() == row["Species"].Value<string>())) lines.Add(row["Species"] + " removed");
        if (!JToken.DeepEquals(JToken.FromObject(before.UnitTags), JToken.FromObject(after.UnitTags)))
            lines.Add("Unit Tag definitions\n" + JsonConvert.SerializeObject(after.UnitTags, Formatting.Indented));
        if (!JToken.DeepEquals(JToken.FromObject(before.TraitClassifications), JToken.FromObject(after.TraitClassifications)))
            lines.Add("Trait classifications\n" + JsonConvert.SerializeObject(after.TraitClassifications));
        return lines.Count == 0 ? "No effective balance changes." : string.Join("\n\n", lines.ToArray());
    }
    internal static BalanceProfile OfficialProfile() { EnsureLoaded(); return Copy(official); }
    internal static string ReadFile(string path)
    {
        if (new FileInfo(path).Length > MaximumBytes) throw new InvalidDataException("Balance file is too large.");
        return File.ReadAllText(path);
    }
    internal static string ImportLegacy(string path)
    {
        if (new FileInfo(path).Length > MaximumBytes) throw new InvalidDataException("Legacy settings are too large.");
        var settings = SerializationUtility.DeserializeValue<RaceSettings>(File.ReadAllBytes(path), DataFormat.Binary);
        if (settings == null) throw new InvalidDataException("Empty legacy settings.");
        settings.Sanitize();
        EnsureLoaded();
        var profile = Copy(official);
        var ignored = new List<string>();
        foreach (var pair in settings.Entries)
        {
            var entry = profile.Species.FirstOrDefault(e => e.Species == pair.Key);
            if (entry == null || pair.Value == null) { ignored.Add(pair.Key.ToString()); continue; }
            entry.Settings = Copy(pair.Value);
            Normalize(entry.Settings, pair.Key);
        }
        profile.UnitTags = new List<UnitTag>();
        if (Directory.Exists(State.UnitTagDirectory))
            foreach (string file in Directory.GetFiles(State.UnitTagDirectory, "*.json", SearchOption.AllDirectories).OrderBy(f => f))
                profile.UnitTags.Add(JsonConvert.DeserializeObject<UnitTag>(ReadFile(file)));
        profile.ParentRevision = official.Revision;
        profile.ParentHash = officialHash;
        ApplyDraft(profile);
        return "Imported legacy draft. Ignored unavailable records: " + (ignored.Count == 0 ? "none" : string.Join(", ", ignored.ToArray())) + ". Original files retained.";
    }
    internal static void UpdateStatus()
    {
        if (State.GameManager != null && State.GameManager.Start_Mode != null)
            State.GameManager.Start_Mode.miscText.text = IsDraft ? "Developer balance draft active — " + OfficialRevision : Diagnostic ?? "";
    }
    internal static void RequireSnapshotPreserved(Unit source, Unit copy)
    {
        if (source.SpeciesBalance == null) return;
        if (copy.SpeciesBalance == null || source.SpeciesBalance.Fingerprint != copy.SpeciesBalance.Fingerprint)
            throw new InvalidDataException("Saved per-character species balance changed during serialization.");
    }
}

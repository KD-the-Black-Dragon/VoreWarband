// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-08: Preserve company naming and terminal history fields in saved campaign state (spec 40E).
// 2026-11-08: Preserve the calendar-difficulty epoch and rules version across save round trips (spec 40F).
// 2026-11-07: Verify Grudge wage state and trait outcomes survive campaign saves (spec 40D).
// 2026-11-06: Background-safe snapshot writing, atomic publication and lightweight save summaries.
// 2026-11-06: Verify interrupted route intent alongside native travel state (spec 40B).
// 2026-11-06: Verify per-character balance snapshots survive checkpoint serialization (spec 40A).
// 2026-11-05: Verify upkeep, personal debts and insolvency across save round trips (spec 38 phase B).
// 2026-11-01: Versioned company checkpoints, validation and corruption recovery (spec 06).
// 2026-11-01: Rebind nonserialized company item services without re-equipping or changing stats (specs 07–08).
// 2026-11-01: Verify health and fate values after checkpoint round trips instead of checking only company/day (09–10 feedback).
// 2026-11-01: Reject lossy injury, treatment and immunity serialization alongside the existing HP preservation guard (spec 11).
// 2026-11-01: Verify queued wage-warning preservation across interrupted-day checkpoint serialization (spec 11 feedback).
// 2026-11-02: Centralize validated normal/Ironman resumption and verify mana, progression, gear/cooldowns and raw stats on round trips (spec 12).
// 2026-11-02: Verify event outcome, participants, source, cooldown and committed travel survive save round trips (spec 13).
// 2026-11-02: Resume all validated company map sizes through the draggable viewport (spec 14 feedback).
// 2026-11-03: Persist named/autosave generations and complete battlefield snapshots (spec 19).
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using OdinSerializer;

internal static class CompanyCampaignSave
{
    const string Magic = "VoreWar.Company.1";
    const string SummaryMagic = "VoreWar.Company.2";
    const int MaximumBytes = 32 * 1024 * 1024;
#if UNITY_EDITOR
    // Focused editor checks redirect all save access to preserved synthetic data under Logs.
    internal static string DiagnosticDirectory;
    internal static int Hydrations;
#endif
    static string storageDirectory;
#if UNITY_EDITOR
    static string diagnosticRoot;
#endif
    public static string DefaultDirectory
    {
        get
        {
#if UNITY_EDITOR
            if (DiagnosticDirectory != null)
            {
                if (diagnosticRoot == null) diagnosticRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../Logs")) + Path.DirectorySeparatorChar;
                string root = diagnosticRoot;
                string directory = Path.GetFullPath(DiagnosticDirectory);
                if (!directory.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Save diagnostics must stay inside project Logs.");
                return directory;
            }
#endif
            return storageDirectory ?? (storageDirectory = Path.Combine(UnityEngine.Application.persistentDataPath, "CompanyCampaigns"));
        }
    }
    internal static byte[] Encode(CompanyCampaign state) { return SerializationUtility.SerializeValue(state, DataFormat.Binary); }
    internal static void Restore(CompanyCampaign target, byte[] snapshot)
    {
        var restored = Decode(snapshot);
        foreach (var field in typeof(CompanyCampaign).GetFields(BindingFlags.Instance | BindingFlags.Public)) field.SetValue(target, field.GetValue(restored));
    }
    static CompanyCampaign Decode(byte[] payload)
    {
#if UNITY_EDITOR
        System.Threading.Interlocked.Increment(ref Hydrations);
#endif
        var state = SerializationUtility.DeserializeValue<CompanyCampaign>(payload, DataFormat.Binary);
        if (state == null) throw new InvalidDataException("Empty company checkpoint.");
        CompanyEconomy.Bind(state);
        CompanyBattleSnapshot.Bind(state);
        state.Validate();
        CompanyBattleSnapshot.Validate(state);
        return state;
    }
    internal static void RequireHealthPreserved(CompanyCampaign source, CompanyCampaign restored)
    {
        if (source.BalanceRevision != null && (source.BalanceRevision != restored.BalanceRevision || source.DeveloperBalance != restored.DeveloperBalance))
            throw new InvalidDataException("Company balance identity changed during serialization.");
        if (source.CompanyRulesVersion != restored.CompanyRulesVersion || source.CalendarDifficultyStartDay != restored.CalendarDifficultyStartDay ||
            source.EconomyVersion != restored.EconomyVersion || source.UpkeepBillsIssued != restored.UpkeepBillsIssued ||
            source.UpkeepDebt != restored.UpkeepDebt || source.UpkeepDueDay != restored.UpkeepDueDay ||
            source.NextPayday != restored.NextPayday || source.Gold != restored.Gold ||
            source.Ended != restored.Ended || source.EndReason != restored.EndReason ||
            source.CompanyName != restored.CompanyName || source.EndingId != restored.EndingId || source.EndingKind != restored.EndingKind ||
            source.LeaderDigestions != restored.LeaderDigestions ||
            source.PendingDecision != restored.PendingDecision || source.PendingDecisionKind != restored.PendingDecisionKind ||
            source.DeferredDecisionKind != restored.DeferredDecisionKind)
            throw new InvalidDataException("Company finances or interrupted decisions changed during serialization.");
        if (source.RestingDay != restored.RestingDay || source.RestDaysRemaining != restored.RestDaysRemaining ||
            source.UpdatingDay != restored.UpdatingDay || source.DailyStage != restored.DailyStage ||
            source.DeferredWageDecision != restored.DeferredWageDecision)
            throw new InvalidDataException("Interrupted recovery progress changed during serialization.");
        if (source.NextRoadEventDay != restored.NextRoadEventDay || source.RandomState != restored.RandomState ||
            source.Position != restored.Position || source.Actions != restored.Actions || source.TravelDestination != restored.TravelDestination ||
            !CompanyRoadEvents.Same(source.RoadEvent, restored.RoadEvent) ||
            (source.Battle == null ? null : source.Battle.RoadEventId) != (restored.Battle == null ? null : restored.Battle.RoadEventId))
            throw new InvalidDataException("Road-event progress changed during serialization.");
        CompanyBattleSnapshot.RequirePreserved(source, restored);
        var characters = restored.Characters.ToDictionary(c => c.Id);
        if (characters.Count != source.Characters.Count) throw new InvalidDataException("Character records changed while saving.");
        foreach (var c in source.Characters)
        {
            CompanyCharacter other;
            if (!characters.TryGetValue(c.Id, out other) || c.Unit.Health != other.Unit.Health ||
                c.Debt != other.Debt || c.BaseWage != other.BaseWage || c.GrudgeWageSurcharge != other.GrudgeWageSurcharge || c.DebtDueDay != other.DebtDueDay ||
                c.WarningShownDay != other.WarningShownDay ||
                c.Unit.TemporaryHealth != other.Unit.TemporaryHealth || c.Unit.Downed != other.Unit.Downed || c.Unit.Digested != other.Unit.Digested || !CompanyInjuries.Same(c.Unit, other.Unit))
                throw new InvalidDataException("Character health or injuries changed during serialization. The previous checkpoint was preserved.");
            BalanceAuthoring.RequireSnapshotPreserved(c.Unit, other.Unit);
            var unit = c.Unit; var copy = other.Unit;
            if (unit.Mana != copy.Mana || unit.Level != copy.Level || unit.Experience != copy.Experience ||
                !unit.GetTraits.SequenceEqual(copy.GetTraits) ||
                !unit.ItemUses.SequenceEqual(copy.ItemUses) || !unit.ItemCooldowns.SequenceEqual(copy.ItemCooldowns) ||
                !unit.Items.Select(i => i == null ? null : i.CompanyInstanceId).SequenceEqual(copy.Items.Select(i => i == null ? null : i.CompanyInstanceId)) ||
                Enumerable.Range(0, (int)Stat.None).Any(i => unit.GetStatBase((Stat)i) != copy.GetStatBase((Stat)i)))
                throw new InvalidDataException("Character resources, progression or equipment changed during serialization. The previous checkpoint was preserved.");
        }
    }
    public static string Save(CompanyCampaign state, string directory) { return CompanySaveSlots.Auto(state, directory); }
    internal static string WriteGeneration(CompanyCampaign state, string directory) { return WriteGeneration(state, directory, null); }
    internal static string WriteGeneration(CompanyCampaign state, string directory, CompanySaveWorker.Request request)
    {
        state.Validate();
        CompanyBattleSnapshot.Validate(state);
        if (!System.Text.RegularExpressions.Regex.IsMatch(state.SaveSlot ?? "legacy", "^[a-zA-Z0-9-]+$")) throw new InvalidDataException("Invalid save slot identity.");
        Directory.CreateDirectory(directory);
        byte[] bytes = Encode(state), hash;
        if (bytes.Length > MaximumBytes) throw new InvalidDataException("Company checkpoint is too large.");
        using (var sha = SHA256.Create()) hash = sha.ComputeHash(bytes);
        string path = Path.Combine(directory, DateTime.UtcNow.Ticks.ToString("D19") + "-" + Guid.NewGuid().ToString("N") + "~" + state.SaveSlot + ".company");
        string staging = path + ".pending";
        using (var stream = new FileStream(staging, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(SummaryMagic);
            writer.Write(Newtonsoft.Json.JsonConvert.SerializeObject(CompanySaveSummary.From(state)));
            writer.Write(bytes.Length); writer.Write(hash); writer.Write(bytes);
            writer.Flush(); stream.Flush();
        }
        // Incomplete/unverified files are never visible to the browser or recovery search.
        ReadPayload(staging); // Check the actual bytes, without rehydrating the entire gameplay graph.
        bool published = request == null ? Publish(staging, path) : request.Publish(() => File.Move(staging, path));
        if (!published) CompanySaveSlots.RemoveFile(staging);
        return published ? path : null;
    }
    static bool Publish(string staging, string path) { File.Move(staging, path); return true; }
    static byte[] ReadPayload(string path)
    {
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var reader = new BinaryReader(stream))
        {
            ReadHeader(reader);
            int length = reader.ReadInt32();
            if (length < 1 || length > MaximumBytes || stream.Length - stream.Position != length + 32L) throw new InvalidDataException("Incomplete company checkpoint.");
            byte[] expected = reader.ReadBytes(32), bytes = reader.ReadBytes(length), actual;
            using (var sha = SHA256.Create()) actual = sha.ComputeHash(bytes);
            if (!actual.SequenceEqual(expected)) throw new InvalidDataException("Company checkpoint checksum failed.");
            return bytes;
        }
    }
    static CompanySaveSummary ReadHeader(BinaryReader reader)
    {
        if (reader.BaseStream.Length < 40 || reader.BaseStream.Length > MaximumBytes + 65536L) throw new InvalidDataException("Invalid company checkpoint size.");
        string magic = reader.ReadString();
        if (magic == Magic) return null;
        if (magic != SummaryMagic) throw new InvalidDataException("This is not a supported company checkpoint.");
        string json = reader.ReadString();
        if (json.Length > 16384) throw new InvalidDataException("Invalid company summary.");
        return Newtonsoft.Json.JsonConvert.DeserializeObject<CompanySaveSummary>(json) ?? throw new InvalidDataException("Missing company summary.");
    }
    internal static void CheckFile(string path) { ReadPayload(path); }
    internal static CompanySaveSummary ReadSummary(string path)
    {
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var reader = new BinaryReader(stream))
        {
            var summary = ReadHeader(reader);
            if (summary == null)
            {
#if UNITY_EDITOR
                System.Threading.Interlocked.Increment(ref Hydrations);
#endif
                return CompanySaveSummary.From(OdinSerializer.SerializationUtility.DeserializeValue<CompanyCampaign>(ReadPayload(path), DataFormat.Binary));
            }
            int length = reader.ReadInt32();
            if (length < 1 || length > MaximumBytes || stream.Length - stream.Position != length + 32L) throw new InvalidDataException("Incomplete company checkpoint.");
            var expected = reader.ReadBytes(32);
            byte[] actual;
            using (var sha = SHA256.Create()) actual = sha.ComputeHash(stream);
            if (!actual.SequenceEqual(expected)) throw new InvalidDataException("Company checkpoint checksum failed.");
            return summary;
        }
    }
    public static CompanyCampaign Load(string path) { return Decode(ReadPayload(path)); }
    internal static CompanyCampaign LoadForResume(string path)
    {
        // Validate fully before a view installs the prospective state.
        var state = Load(path);
        string directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (File.Exists(Path.Combine(directory, "fallen")) || Path.GetFileName(directory) != state.Id)
            throw new InvalidDataException("Unsupported company checkpoint or mismatched company folder.");
        if (state.Ironman)
        {
            int skipped; string latest;
            LoadLatest(directory, out skipped, out latest);
            if (!string.Equals(Path.GetFullPath(path), Path.GetFullPath(latest), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Ironman can only resume its latest valid checkpoint.");
        }
        return state;
    }
    public static string[] Generations(string directory)
    {
        return Directory.Exists(directory) ? Directory.GetFiles(directory, "*.company").OrderByDescending(Path.GetFileName, StringComparer.Ordinal).ToArray() : new string[0];
    }
    public static CompanyCampaign LoadLatest(string directory, out int skipped)
    {
        string selected;
        return LoadLatest(directory, out skipped, out selected);
    }
    public static CompanyCampaign LoadLatest(string directory, out int skipped, out string selected)
    {
        if (File.Exists(Path.Combine(directory, "fallen"))) throw new InvalidDataException("This Ironman company has ended.");
        skipped = 0; selected = null;
        foreach (string path in Generations(directory))
        {
            try { var state = Load(path); selected = path; return state; }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            { skipped++; }
        }
        throw new InvalidDataException("No valid company checkpoint was found. Existing files were preserved.");
    }
}

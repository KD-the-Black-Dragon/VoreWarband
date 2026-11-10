// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-09: Allow High Scores to read and validate archive records on a background worker (40E performance review).
// 2026-11-09: Multiply reputation's contribution to ending scores by ten and version the formula.
// 2026-11-08: Add immutable company endings, provisional scoring and a persistent high-score archive (spec 40E).
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

[Serializable]
internal sealed class CompanyScoreBreakdown
{
    public int ReputationPoints;
    public int DaysPoints;
    public int MercenaryLossPenalty;
    public int LeaderDigestionPenalty;
    public double BeforeWipeoutMultiplier;
    public double WipeoutMultiplier;
    public double FinalScore;
}

internal static class CompanyScoring
{
    internal const string FormulaVersion = "40E-2";
    internal static CompanyScoreBreakdown Calculate(CompanyCampaign state, bool companyWipedOut = false)
    {
        if (state == null) throw new ArgumentNullException("state");
        var score = new CompanyScoreBreakdown
        {
            ReputationPoints = state.Reputation * 10,
            DaysPoints = Math.Min(100, Math.Max(0, state.ElapsedDays)),
            MercenaryLossPenalty = -10 * (state.Memorial == null ? 0 : state.Memorial.Count),
            LeaderDigestionPenalty = -20 * Math.Max(0, state.LeaderDigestions),
            WipeoutMultiplier = companyWipedOut ? .9 : 1.0
        };
        score.BeforeWipeoutMultiplier = score.ReputationPoints + score.DaysPoints +
            score.MercenaryLossPenalty + score.LeaderDigestionPenalty;
        score.FinalScore = score.BeforeWipeoutMultiplier * score.WipeoutMultiplier;
        return score;
    }
}

[Serializable]
internal sealed class CompanyHistoryMemorial
{
    public string CharacterId, Name, Species, Fate;
    public int Day;
}

[Serializable]
internal sealed class CompanyHistoryRecord
{
    public int Schema = 1;
    public string CampaignId, EndingId, CompanyName, LeaderName, EndReason, EndingKind, BalanceRevision, ScoreFormulaVersion;
    public int Day, Reputation, TotalGoldEarned, MercenariesHired, MercenariesLost, ContractsCompleted, EnemiesDefeated, LeaderDigestions;
    public bool Ironman, DeveloperBalance, CompanyWipedOut;
    public CompanyScoreBreakdown Score;
    public CompanyHistoryMemorial[] Memorial;

    internal static CompanyHistoryRecord From(CompanyCampaign state)
    {
        bool wipedOut = state.EndingKind == CompanyEndingKind.Wipeout;
        return new CompanyHistoryRecord
        {
            CampaignId = state.Id,
            EndingId = state.EndingId,
            CompanyName = CompanyHistoryStore.CompanyName(state.CompanyName),
            LeaderName = state.Characters.FirstOrDefault(c => c.Id == state.LeaderId)?.Unit.Name ?? "Unknown Leader",
            EndReason = state.EndReason ?? "The company ended.",
            EndingKind = string.IsNullOrEmpty(state.EndingKind) ? CompanyEndingKind.Other : state.EndingKind,
            BalanceRevision = state.BalanceRevision,
            ScoreFormulaVersion = CompanyScoring.FormulaVersion,
            Day = Math.Max(0, state.ElapsedDays),
            Reputation = state.Reputation,
            TotalGoldEarned = state.TotalGoldEarned,
            MercenariesHired = state.MercenariesHired,
            MercenariesLost = state.Memorial == null ? 0 : state.Memorial.Count,
            ContractsCompleted = state.ContractsCompleted,
            EnemiesDefeated = state.EnemiesDefeated,
            LeaderDigestions = Math.Max(0, state.LeaderDigestions),
            Ironman = state.Ironman,
            DeveloperBalance = state.DeveloperBalance,
            CompanyWipedOut = wipedOut,
            Score = CompanyScoring.Calculate(state, wipedOut),
            Memorial = (state.Memorial ?? new List<CompanyMemorial>()).Where(m => m != null).Select(m => new CompanyHistoryMemorial
            { CharacterId = m.CharacterId, Name = m.Name, Species = m.Species, Fate = m.Fate, Day = m.Day }).ToArray()
        };
    }
}

internal static class CompanyEndingKind
{
    internal const string Retired = "Retired";
    internal const string Wipeout = "Wipeout";
    internal const string LeaderLost = "LeaderLost";
    internal const string Insolvency = "Insolvency";
    internal const string Other = "Other";
}

internal static class CompanyHistoryStore
{
    const long MaximumRecordBytes = 1024 * 1024;
    static readonly object ArchiveGate = new object();
    static readonly object FileGate = new object();
    static readonly HashSet<string> PendingIds = new HashSet<string>();
    static readonly HashSet<string> CompletedIds = new HashSet<string>();
    static readonly Dictionary<string, string> Failures = new Dictionary<string, string>();
    static readonly Dictionary<string, CompanyHistoryRecord> PendingRecords = new Dictionary<string, CompanyHistoryRecord>();
    static bool cleanupRecoveryQueued;
    internal sealed class Catalog
    {
        internal readonly List<CompanyHistoryRecord> Records = new List<CompanyHistoryRecord>();
        internal int CorruptRecords, PendingWrites, FailedWrites;
    }

    static string DirectoryPath { get { return Path.Combine(CompanyCampaignSave.DefaultDirectory, "HighScores"); } }
    internal static string GetDirectoryPath() { return DirectoryPath; }
    internal static string CompanyName(string name) { return string.IsNullOrWhiteSpace(name) ? "Unnamed Company" : name.Trim(); }

    internal static void RecoverIronmanCleanup()
    {
        lock (ArchiveGate)
        {
            if (cleanupRecoveryQueued) return;
            cleanupRecoveryQueued = true;
        }
        string historyDirectory = DirectoryPath;
        string saveRoot = CompanyCampaignSave.DefaultDirectory;
        try
        {
            Task.Run(() =>
            {
                if (!Directory.Exists(historyDirectory)) return;
                // Complete a record that was staged before a crash, then apply any Ironman cleanup
                // whose record reached disk before the prior process stopped.
                lock (FileGate)
                {
                    foreach (string pending in Directory.GetFiles(historyDirectory, "*.score.pending"))
                    {
                        try
                        {
                            var recovered = ReadFile(pending);
                            string expected = recovered.EndingId + ".score";
                            if (Path.GetFileName(pending) == expected + ".pending")
                            {
                                string final = Path.Combine(historyDirectory, expected);
                                if (!File.Exists(final)) File.Move(pending, final);
                            }
                        }
                        catch { }
                    }
                }
                foreach (string path in Directory.GetFiles(historyDirectory, "*.score"))
                {
                    try
                    {
                        var record = ReadFile(path);
                        string saveDirectory = Path.Combine(saveRoot, record.CampaignId);
                        if (record.Ironman && Directory.Exists(saveDirectory)) CompanySaveSlots.RemoveCompany(record.CampaignId, saveRoot);
                    }
                    catch { }
                }
            });
        }
        catch
        {
            lock (ArchiveGate) cleanupRecoveryQueued = false;
        }
    }

    internal static string ArchiveEnding(CompanyCampaign state)
    {
        if (state == null || !state.Ended) return null;
        // No ending ID means this is a save created before 40E. Never invent a past record.
        if (string.IsNullOrEmpty(state.EndingId)) return null;
        if (string.IsNullOrEmpty(state.EndingKind)) state.EndingKind = CompanyEndingKind.Other;
        var record = CompanyHistoryRecord.From(state);
        string historyDirectory = DirectoryPath;
        string saveRoot = CompanyCampaignSave.DefaultDirectory;
        string previousFailure = null;
        lock (ArchiveGate)
        {
            if (CompletedIds.Contains(record.EndingId)) return null;
            if (PendingIds.Contains(record.EndingId)) return "Final score is being saved in the background; it will appear in High Scores when ready.";
            Failures.TryGetValue(record.EndingId, out previousFailure);
            PendingIds.Add(record.EndingId);
            PendingRecords[record.EndingId] = record;
            Failures.Remove(record.EndingId);
        }
        try
        {
            Task.Run(() =>
            {
                try
                {
                    Commit(record, historyDirectory);
                    // Ironman removal is game-owned and only starts after the immutable record is published.
                    if (record.Ironman) CompanySaveSlots.RemoveCompany(record.CampaignId, saveRoot);
                    lock (ArchiveGate)
                    {
                        PendingIds.Remove(record.EndingId); PendingRecords.Remove(record.EndingId);
                        CompletedIds.Add(record.EndingId); Failures.Remove(record.EndingId);
                    }
                }
                catch (Exception ex)
                {
                    lock (ArchiveGate)
                    {
                        PendingIds.Remove(record.EndingId); PendingRecords.Remove(record.EndingId);
                        Failures[record.EndingId] = ex.Message;
                    }
                }
            });
        }
        catch (Exception ex)
        {
            lock (ArchiveGate)
            {
                PendingIds.Remove(record.EndingId); PendingRecords.Remove(record.EndingId);
                Failures[record.EndingId] = ex.Message;
            }
        }
        return previousFailure == null
            ? "Final score is being saved in the background; it will appear in High Scores when ready."
            : "Retrying the final score save after an earlier error: " + previousFailure;
    }

    internal static CompanyHistoryRecord Commit(CompanyHistoryRecord record, string historyDirectory)
    {
        Guid campaignId, endingId;
        if (record == null || !Guid.TryParseExact(record.CampaignId, "N", out campaignId)) throw new InvalidOperationException("The company has no valid campaign identity.");
        if (!Guid.TryParseExact(record.EndingId, "N", out endingId)) throw new InvalidOperationException("The ending has no valid history identity.");
        string finalPath = Path.Combine(historyDirectory, record.EndingId + ".score");
        if (File.Exists(finalPath))
        {
            var existing = ReadFile(finalPath);
            if (existing.EndingId == record.EndingId && existing.CampaignId == record.CampaignId) return existing;
            throw new InvalidDataException("A different history record already uses this ending identity.");
        }

        Directory.CreateDirectory(historyDirectory);
        string writingPath = finalPath + ".writing";
        string pendingPath = finalPath + ".pending";
        File.WriteAllText(writingPath, JsonConvert.SerializeObject(record, Formatting.None), new UTF8Encoding(false));
        var verified = ReadFile(writingPath);
        if (verified.EndingId != record.EndingId || verified.CampaignId != record.CampaignId)
            throw new InvalidDataException("The final score record did not pass its read-back check.");
        lock (FileGate)
        {
            if (File.Exists(finalPath))
            {
                var existing = ReadFile(finalPath);
                if (existing.EndingId == record.EndingId && existing.CampaignId == record.CampaignId) return existing;
                throw new InvalidDataException("A different history record already uses this ending identity.");
            }
            if (File.Exists(pendingPath))
            {
                var staged = ReadFile(pendingPath);
                if (staged.EndingId != record.EndingId || staged.CampaignId != record.CampaignId)
                    throw new InvalidDataException("A different staged history record already uses this ending identity.");
            }
            else File.Move(writingPath, pendingPath);
            File.Move(pendingPath, finalPath);
        }
        return record;
    }

    internal static Catalog ReadAll() { return ReadAll(DirectoryPath); }

    internal static Catalog ReadAll(string historyDirectory)
    {
        var catalog = new Catalog();
        CompanyHistoryRecord[] initiallyPending;
        lock (ArchiveGate) initiallyPending = PendingRecords.Values.ToArray();
        // A completed staging file can be promoted after a crash between writing and publishing.
        if (Directory.Exists(historyDirectory)) lock (FileGate)
        {
            foreach (string pending in Directory.GetFiles(historyDirectory, "*.score.pending"))
            {
                try
                {
                    var recovered = ReadFile(pending);
                    string expected = recovered.EndingId + ".score";
                    if (Path.GetFileName(pending) == expected + ".pending")
                    {
                        string final = Path.Combine(historyDirectory, expected);
                        if (!File.Exists(final)) File.Move(pending, final);
                    }
                }
                catch { catalog.CorruptRecords++; }
            }
        }
        if (Directory.Exists(historyDirectory)) foreach (string path in Directory.GetFiles(historyDirectory, "*.score"))
        {
            try { catalog.Records.Add(ReadFile(path)); }
            catch { catalog.CorruptRecords++; }
        }
        foreach (var record in initiallyPending)
            if (!catalog.Records.Any(existing => existing.EndingId == record.EndingId)) catalog.Records.Add(record);
        lock (ArchiveGate)
        {
            foreach (var record in PendingRecords.Values)
                if (!catalog.Records.Any(existing => existing.EndingId == record.EndingId)) catalog.Records.Add(record);
            catalog.PendingWrites = PendingIds.Count;
            catalog.FailedWrites = Failures.Count;
        }
        return catalog;
    }

    static CompanyHistoryRecord ReadFile(string path)
    {
        var info = new FileInfo(path);
        if (info.Length < 2 || info.Length > MaximumRecordBytes) throw new InvalidDataException("Invalid history record size.");
        var record = JsonConvert.DeserializeObject<CompanyHistoryRecord>(File.ReadAllText(path));
        Guid campaignId, endingId;
        if (record == null || record.Schema != 1 || !Guid.TryParseExact(record.CampaignId, "N", out campaignId) ||
            !Guid.TryParseExact(record.EndingId, "N", out endingId) || record.Score == null || record.Memorial == null ||
            record.Day < 0 || record.MercenariesLost < 0 || record.LeaderDigestions < 0 || string.IsNullOrWhiteSpace(record.CompanyName))
            throw new InvalidDataException("Invalid company history record.");
        return record;
    }
}

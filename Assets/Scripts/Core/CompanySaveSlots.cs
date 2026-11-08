// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-08: Keep Ironman saves until their 40E ending record is published (spec 40E).
// 2026-11-06: Summary-only catalogs, filename-based retention and serialized worker/manual publication.
// 2026-11-03: Named save slots, progress summaries, autosave retention and game-owned deletion (spec 19).
// See CHANGES.md and License.txt for GPL-3.0 terms; inherited notices remain applicable.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

internal static class CompanySaveSlots
{
    internal const int BackupCount = 3;
    internal sealed class Entry
    {
        internal string Path, Directory, Slot, Name;
        internal CompanySaveSummary Summary;
        // Compatibility for developer checks. The browser uses Summary and never hydrates State.
        internal CompanyCampaign State => CompanyCampaignSave.Load(Path);
        internal bool Recovered;
        internal string Details => Summary.Details + (Recovered ? "\nRecovered from backup" : "");
    }
    internal static string Auto(CompanyCampaign state, string directory)
    {
        if (state.Ended) return null;
        return Write(state, directory, state.InBattle ? "battle" : "map", state.InBattle ? "Battle autosave" : "Map autosave", state.InBattle ? 2 : 1);
    }
    internal static string Manual(CompanyCampaign state, string name)
    {
        if (state.Ironman) throw new InvalidOperationException("Ironman uses autosaves only.");
        name = (name ?? "").Trim();
        string slot = ManualSlot(name);
        CompanySaveWorker.Cancel(state.Id);
        return Write(state, DirectoryFor(state.Id), slot, name, 3);
    }
    internal static string ManualSlot(string name)
    {
        if (string.IsNullOrEmpty(name) || name.Length > 64) throw new InvalidOperationException("Enter a save name (1-64 characters).");
        using (var sha = SHA256.Create()) return "manual-" + BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(name.ToUpperInvariant()))).Replace("-", "").Substring(0, 24);
    }
    internal static string Write(CompanyCampaign state, string directory, string slot, string name, int kind)
    { return Write(state, directory, slot, name, kind, null); }
    internal static string Write(CompanyCampaign state, string directory, string slot, string name, int kind, CompanySaveWorker.Request request)
    {
        lock (CompanySaveWorker.FileGate)
        {
            if (File.Exists(Path.Combine(directory, "fallen"))) throw new InvalidOperationException("This Ironman company has ended.");
            state.SaveSlot = slot; state.SaveLabel = name; state.SaveKind = kind;
            string path = CompanyCampaignSave.WriteGeneration(state, directory, request);
            if (path == null) return null;
            // Retain by the immutable filename's slot, without decoding every old graph.
            if (IsOwned(directory, state.Id, request == null ? null : request.Root))
            {
                int kept = 0;
                foreach (var file in CompanyCampaignSave.Generations(directory))
                {
                    string filename = Path.GetFileNameWithoutExtension(file);
                    int separator = filename.LastIndexOf('~');
                    if (separator < 0 || filename.Substring(separator + 1) != slot) continue;
                    try { CompanyCampaignSave.CheckFile(file); } catch { continue; }
                    if (kept++ <= BackupCount) continue;
                    try { RemoveFile(file); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                }
            }
            return path;
        }
    }
    internal static string DirectoryFor(string id)
    {
        Guid value;
        if (!Guid.TryParseExact(id, "N", out value)) throw new InvalidOperationException("Invalid company identity.");
        return Path.Combine(CompanyCampaignSave.DefaultDirectory, id);
    }
    static bool IsOwned(string directory, string id, string root = null)
    {
        if (!string.Equals(Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar), Path.GetFullPath(root == null ? DirectoryFor(id) : Path.Combine(root, id)), StringComparison.OrdinalIgnoreCase)) return false;
        var current = new DirectoryInfo(directory);
        while (current != null)
        {
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0) return false;
            current = current.Parent;
        }
        return true;
    }
    internal static List<Entry> List() { return List(CompanyCampaignSave.DefaultDirectory); }
    internal static List<Entry> List(string root)
    {
        root = root ?? CompanyCampaignSave.DefaultDirectory;
        var entries = new List<Entry>();
        if (!Directory.Exists(root)) return entries;
        foreach (var directory in Directory.GetDirectories(root))
        {
            Guid id;
            if (!Guid.TryParseExact(Path.GetFileName(directory), "N", out id) || !IsOwned(directory, Path.GetFileName(directory), root)) continue;
            if (File.Exists(Path.Combine(directory, "fallen"))) continue;
            var seen = new HashSet<string>(); var failedSlots = new HashSet<string>(); bool skipped = false;
            foreach (var path in CompanyCampaignSave.Generations(directory))
            {
                CompanySaveSummary state;
                string filename = Path.GetFileNameWithoutExtension(path);
                int separator = filename.LastIndexOf('~');
                string fileSlot = separator < 0 ? "legacy" : filename.Substring(separator + 1);
                // A newer verified generation already represents this slot; its older backups stay private.
                if (seen.Contains(fileSlot)) continue;
                try { state = CompanyCampaignSave.ReadSummary(path); } catch { skipped = true; failedSlots.Add(fileSlot); continue; }
                if (Path.GetFileName(directory) != state.Id || state.Preview || (state.Ironman && state.Ended)) continue;
                string slot = string.IsNullOrEmpty(state.Slot) ? "legacy" : state.Slot;
                if (!seen.Add(slot)) continue;
                entries.Add(new Entry { Path = path, Directory = directory, Slot = slot, Summary = state,
                    Name = state.Name ?? "Autosave", Recovered = state.Ironman ? skipped : failedSlots.Contains(slot) });
                if (state.Ironman) break;
            }
        }
        return entries.OrderByDescending(e => Path.GetFileName(e.Path), StringComparer.Ordinal).ToList();
    }
    internal static void Delete(Entry entry)
    {
        CompanySaveWorker.Cancel(entry.Summary.Id, true);
        lock (CompanySaveWorker.FileGate) DeleteLocked(entry);
    }
    static void DeleteLocked(Entry entry)
    {
        if (!IsOwned(entry.Directory, entry.Summary.Id)) throw new InvalidOperationException("Save location is not owned by this company.");
        if (entry.Summary.Ironman) { RemoveCompany(entry.Summary.Id); return; }
        foreach (var path in CompanyCampaignSave.Generations(entry.Directory))
        {
            string filename = Path.GetFileNameWithoutExtension(path);
            int separator = filename.LastIndexOf('~');
            if (separator >= 0)
            {
                if (filename.Substring(separator + 1) == entry.Slot) RemoveFile(path);
                continue;
            }
            // Pre-slot generations belong to the one legacy autosave group in this owned folder.
            if (entry.Slot == "legacy") RemoveFile(path);
        }
    }
    internal static void RemoveCompany(string id)
    {
        CompanySaveWorker.Cancel(id, true);
        lock (CompanySaveWorker.FileGate) RemoveCompanyLocked(id, null);
    }
    internal static void RemoveCompany(string id, string root)
    {
        CompanySaveWorker.Cancel(id, true);
        lock (CompanySaveWorker.FileGate) RemoveCompanyLocked(id, root);
    }
    static void RemoveCompanyLocked(string id, string root)
    {
        Guid parsed;
        if (!Guid.TryParseExact(id, "N", out parsed)) throw new InvalidOperationException("Invalid company identity.");
        string directory = root == null ? DirectoryFor(id) : Path.Combine(root, id);
        if (!Directory.Exists(directory)) return;
        if (!IsOwned(directory, id, root)) throw new InvalidOperationException("Invalid company save location.");
        // Durable defeat marker prevents resurrection even if a subsequent deletion fails.
        File.WriteAllText(Path.Combine(directory, "fallen"), "Ironman company ended.");
        foreach (string path in CompanyCampaignSave.Generations(directory)) RemoveFile(path);
    }
    internal static void RemoveFile(string path)
    {
#if UNITY_EDITOR
        // Automated checks may inspect deletion choices, but must never destroy a file.
        if (CompanyCampaignSave.DiagnosticDirectory != null)
        {
            string root = Path.GetFullPath(CompanyCampaignSave.DefaultDirectory) + Path.DirectorySeparatorChar;
            if (!Path.GetFullPath(path).StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Diagnostic path escaped its root.");
            DiagnosticRemovals.Add(path);
            return;
        }
#endif
        File.Delete(path);
    }
#if UNITY_EDITOR
    internal static readonly List<string> DiagnosticRemovals = new List<string>();
#endif
}

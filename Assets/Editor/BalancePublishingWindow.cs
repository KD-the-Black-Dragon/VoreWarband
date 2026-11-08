// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-06: Review and promote immutable official balance revisions with dated change records (spec 40A).
// See CHANGES.md and License.txt for GPL-3.0 terms.
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class BalancePublishingWindow : EditorWindow
{
    BalanceProfile candidate;
    string report = "Choose an exported developer balance package.";
    Vector2 scroll;
    static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    internal static string BalanceDirectory => Path.Combine(Application.dataPath, "Resources", BalanceAuthoring.ResourceRoot);

    [MenuItem("Warband/Balance/Import and promote")]
    public static void Open() { GetWindow<BalancePublishingWindow>("Official balance").Show(); }
    void OnGUI()
    {
        EditorGUILayout.LabelField("Official balance promotion", EditorStyles.boldLabel);
        if (GUILayout.Button("Choose exported package"))
        {
            string path = EditorUtility.OpenFilePanel("Balance package", Application.persistentDataPath, "json");
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    candidate = BalanceAuthoring.Read(BalanceAuthoring.ReadFile(path));
                    var current = Current();
                    report = "Current " + current.Revision + "\nDraft parent " + candidate.ParentRevision + "\n\n" + BalanceAuthoring.Describe(current, candidate);
                }
                catch (Exception ex) { candidate = null; report = ex.Message; }
            }
        }
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.TextArea(report, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
        GUI.enabled = candidate != null;
        if (GUILayout.Button("Promote reviewed values to shipped defaults"))
        {
            try
            {
                string revision = Promote(candidate);
                report = "Promoted " + revision + ". The next build includes these defaults.\n\n" + report;
                candidate = null;
            }
            catch (Exception ex) { report = ex.Message + "\n\n" + report; }
        }
        if (GUILayout.Button("Rebase reviewed values onto current official revision"))
        {
            var current = Current();
            if (EditorUtility.DisplayDialog("Rebase reviewed balance", "This retains every reviewed draft value, including differences from newer defaults. Review the full comparison before promotion.", "Rebase", "Cancel"))
            {
                candidate.ParentRevision = current.Revision;
                candidate.ParentHash = BalanceAuthoring.Hash(BalanceAuthoring.Write(current));
                report = "Rebased for review.\n\n" + BalanceAuthoring.Describe(current, candidate);
            }
        }
        GUI.enabled = true;
    }
    internal static BalanceProfile Current()
    {
        string diagnostic;
        return BalanceAuthoring.LoadCatalog(File.ReadAllText(Path.Combine(BalanceDirectory, "active.txt")),
            revision => File.ReadAllText(Path.Combine(BalanceDirectory, "Profiles", revision + ".json")), out diagnostic);
    }
    internal static string Promote(BalanceProfile draft)
    {
        BalanceAuthoring.Validate(draft);
        var current = Current();
        if (draft.ParentRevision != current.Revision || draft.ParentHash != BalanceAuthoring.Hash(BalanceAuthoring.Write(current)))
            throw new InvalidDataException("Draft base is stale. Review differences and explicitly rebase before promoting.");
        var profile = BalanceAuthoring.Copy(draft);
        profile.Revision = "balance-" + DateTime.Now.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N");
        profile.ModifiedDate = DateTime.Now.ToString("yyyy-MM-dd");
        Commit(profile, ProjectRoot, draft.ParentHash);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        return profile.Revision;
    }
    // Immutable profiles plus an append-only commit catalog. A partial final catalog
    // line cannot replace the previous usable commit. No file cleanup is performed.
    internal static void Commit(BalanceProfile profile, string projectRoot, string expectedParentHash = null)
    {
        string root = Path.GetFullPath(projectRoot);
        string lockDirectory = Path.Combine(root, "Logs", "BalancePromotions");
        Directory.CreateDirectory(lockDirectory);
        using (var gate = new FileStream(Path.Combine(lockDirectory, "promotion.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            CommitLocked(profile, root, expectedParentHash);
    }
    static void CommitLocked(BalanceProfile profile, string root, string expectedParentHash)
    {
        string directory = Path.Combine(root, "Assets", "Resources", BalanceAuthoring.ResourceRoot);
        BalanceAuthoring.Validate(profile);
        if (expectedParentHash != null)
        {
            string diagnostic;
            var current = BalanceAuthoring.LoadCatalog(File.ReadAllText(Path.Combine(directory, "active.txt")),
                revision => File.ReadAllText(Path.Combine(directory, "Profiles", revision + ".json")), out diagnostic);
            if (BalanceAuthoring.Hash(BalanceAuthoring.Write(current)) != expectedParentHash)
                throw new InvalidDataException("Another promotion changed the official base. Review and rebase this draft.");
        }
        string text = BalanceAuthoring.Write(profile);
        string relative = "Assets/Resources/" + BalanceAuthoring.ResourceRoot + "/Profiles/" + profile.Revision + ".json";
        string path = Path.GetFullPath(Path.Combine(root, relative));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Profile destination must stay inside the project.");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        if (File.Exists(path))
        {
            if (File.ReadAllText(path) != text) throw new IOException("A different profile already uses this revision.");
        }
        else File.WriteAllText(path, text, new UTF8Encoding(false));
        BalanceAuthoring.Read(File.ReadAllText(path));
        string heading = "## " + profile.ModifiedDate + " — Official balance " + profile.Revision;
        string changesPath = Path.Combine(root, "CHANGES.md");
        string changes = File.Exists(changesPath) ? File.ReadAllText(changesPath) : "# VoreWar fork changes\n";
        if (!changes.Contains(heading)) WriteRecord(changesPath, changes + "\n\n" + heading + "\n\n" +
            "- Promoted species balance for the VoreWar mercenary-company conversion from " + (profile.ParentRevision ?? "the pre-40A baseline") + ".\n" +
            "- Data paths are `" + relative + "`, `Assets/Resources/WarbandBalance/active.txt` and `Assets/Resources/WarbandBalance/NOTICE.txt`. See spec 40A.\n" +
            "- Existing copyright, attribution and GPL-3.0 terms in License.txt are preserved. This data promotion does not establish release readiness.\n", root, profile.Revision);
        string noticePath = Path.Combine(directory, "NOTICE.txt");
        string notice = File.Exists(noticePath) ? File.ReadAllText(noticePath) : "VoreWar mercenary-company conversion balance data.\nExisting upstream attribution and GPL-3.0 terms remain applicable. See CHANGES.md and License.txt.\n";
        string record = profile.ModifiedDate + " " + relative;
        if (!notice.Contains(record)) WriteRecord(noticePath, notice + record + "\n", root, profile.Revision);
        string catalog = Path.Combine(directory, "active.txt");
        string commit = profile.Revision + "|" + BalanceAuthoring.Hash(text);
        string previous = File.Exists(catalog) ? File.ReadAllText(catalog) : "";
        if (previous.Split('\n').LastOrDefault(line => !string.IsNullOrWhiteSpace(line)) != commit)
            File.AppendAllText(catalog, "\n" + commit + "\n", new UTF8Encoding(false));
    }
    static void WriteRecord(string path, string text, string root, string revision)
    {
        string directory = Path.Combine(root, "Logs", "BalancePromotions", revision);
        Directory.CreateDirectory(directory);
        string backup = Path.Combine(directory, Path.GetFileName(path) + ".before.txt");
        if (File.Exists(path) && !File.Exists(backup)) File.WriteAllText(backup, File.ReadAllText(path), new UTF8Encoding(false));
        string staged = Path.Combine(directory, Path.GetFileName(path) + ".prepared.txt");
        File.WriteAllText(staged, text, new UTF8Encoding(false));
        File.WriteAllText(path, text, new UTF8Encoding(false));
    }
    internal static void Bootstrap()
    {
        if (File.Exists(Path.Combine(BalanceDirectory, "active.txt"))) return;
        var legacy = BalanceAuthoring.BuiltinProfile();
        Commit(legacy, ProjectRoot);
        var initial = BalanceAuthoring.Copy(legacy);
        initial.ParentRevision = legacy.Revision;
        initial.ParentHash = BalanceAuthoring.Hash(BalanceAuthoring.Write(legacy));
        initial.Revision = "balance-40a-initial";
        Commit(initial, ProjectRoot);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }
}

public sealed class BalanceBuildGuard : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report)
    {
        var current = BalancePublishingWindow.Current();
        string directory = BalancePublishingWindow.BalanceDirectory;
        string last = File.ReadAllLines(Path.Combine(directory, "active.txt")).LastOrDefault(line => !string.IsNullOrWhiteSpace(line));
        if (last == null || last.Trim() != current.Revision + "|" + BalanceAuthoring.Hash(BalanceAuthoring.Write(current)))
            throw new BuildFailedException("Repair the incomplete official balance commit before building.");
        if (!File.ReadAllText(Path.Combine(directory, "NOTICE.txt")).Contains(current.Revision + ".json") ||
            !File.ReadAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "../CHANGES.md"))).Contains("Official balance " + current.Revision))
            throw new BuildFailedException("The official balance modification record is missing.");
    }
}

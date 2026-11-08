// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-06: Focused native authoring, promotion recovery and character-preservation checks (spec 40A).
// See CHANGES.md and License.txt for GPL-3.0 terms. Synthetic fixtures are retained without deletion.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class BalanceAuthoringAudit
{
    const string Key = "Warband.Balance40A.Audit";
    static IEnumerator work;
    static int checks;
    static double started;
    static string directory;
    static BalanceAuthoringAudit() { EditorApplication.update += Update; }
    public static void Run()
    {
        directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/balance40a-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(Application.dataPath, "../Logs/balance40a-latest.txt"), directory);
        SessionState.SetString(Key + ".directory", directory);
        Environment.SetEnvironmentVariable("VOREWAR_AUTHORING_USERDATA", Path.Combine(directory, "UserData"));
        EditorSceneManager.OpenScene("Assets/base.unity");
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }
    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        checks++;
    }
    static void Reject(Action action, string message)
    {
        bool rejected = false;
        try { action(); } catch { rejected = true; }
        Check(rejected, message);
    }
    static void Update()
    {
        int exit = SessionState.GetInt(Key + ".exit", 0);
        if (exit != 0 && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.EraseInt(Key + ".exit");
            Environment.SetEnvironmentVariable("VOREWAR_AUTHORING_USERDATA", null);
            EditorApplication.Exit(exit - 1);
            return;
        }
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
        try
        {
            if (work == null) { started = EditorApplication.timeSinceStartup; work = Review(); }
            if (EditorApplication.timeSinceStartup - started > 300) throw new TimeoutException("Balance authoring audit timed out.");
            if (!work.MoveNext()) Finish(null);
        }
        catch (Exception ex) { Finish(ex); }
    }
    static void Finish(Exception error)
    {
        directory = SessionState.GetString(Key + ".directory", "");
        File.WriteAllText(Path.Combine(directory, "result.json"), JsonUtility.ToJson(new Result { passed = error == null, assertions = checks, error = error == null ? null : error.ToString() }, true));
        if (error != null) Debug.LogException(error);
        SessionState.SetBool(Key, false); SessionState.SetInt(Key + ".exit", error == null ? 1 : 2);
        EditorApplication.isPlaying = false;
    }
    [Serializable] class Result { public bool passed; public int assertions; public string error; }
    static IEnumerator Review()
    {
        yield return null; yield return null;
        directory = SessionState.GetString(Key + ".directory", "");
        var gm = UnityEngine.Object.FindObjectOfType<GameManager>();
        Check(gm != null, "Native scene initialized"); gm.enabled = false;
        Check(Path.GetFullPath(State.StorageDirectory).StartsWith(directory, StringComparison.OrdinalIgnoreCase), "Personal settings isolated before startup");
        BalancePublishingWindow.Bootstrap();
        BalanceAuthoring.ReloadOfficial();
        var baseline = BalanceAuthoring.OfficialProfile();
        string baselineText = BalanceAuthoring.Write(baseline);
        Check(BalanceAuthoring.Write(BalanceAuthoring.Read(baselineText)) == baselineText, "Canonical profile round trip");
        Check(!BalanceAuthoring.IsDraft, "Normal startup uses official data");
        Check(!BalanceAuthoring.HasMetadata(Race.MatronsMinions) && !BalanceAuthoring.HasMetadata(Race.FeralUmbreon), "Absent metadata identified");
        var unavailable = new RaceSettings(false); unavailable.Set(Race.MatronsMinions, null); unavailable.Sanitize();
        Check(unavailable.Entries.Count() == 1, "Unavailable legacy metadata retained without hydration");
        var invalid = new RaceSettings(false); invalid.Set(Race.Avians, null);
        Reject(invalid.Sanitize, "Malformed active entry rejected");
        Reject(() => BalanceAuthoring.Read("{"), "Corrupt package rejected");
        var candidate = BalanceAuthoring.Copy(baseline); candidate.Schema = 99;
        Reject(() => BalanceAuthoring.Validate(candidate), "Unsupported schema rejected");
        candidate = BalanceAuthoring.Copy(baseline); candidate.Species.Add(candidate.Species[0]);
        Reject(() => BalanceAuthoring.Validate(candidate), "Duplicate species rejected");
        candidate = BalanceAuthoring.Copy(baseline); candidate.Species[0].Settings.RaceTraits.Add(Traits.Flight);
        Reject(() => BalanceAuthoring.Validate(candidate), "Retired traits rejected");
        candidate = BalanceAuthoring.Copy(baseline); candidate.Species[0].Settings.RaceTags.Add(9123);
        Reject(() => BalanceAuthoring.Validate(candidate), "Missing tag dependency rejected");
        candidate = BalanceAuthoring.Copy(baseline); candidate.Species[0].Settings.BaseAP = 0;
        Reject(() => BalanceAuthoring.Validate(candidate), "Invalid AP rejected");

        gm.Menu.OpenRaceEditor();
        var prompt = UnityEngine.Object.FindObjectsOfType<InputBox>().Single();
        prompt.InputField.text = "214214214";
        prompt.YesClicked();
        var editor = gm.Menu.RaceEditor;
        Check(editor.gameObject.activeInHierarchy && gm.Menu.IsRaceEditorSessionActive, "Password gate opens native Race Editor");
        editor.RaceDropdown.value = editor.RaceDropdown.options.FindIndex(o => o.text == "Avian");
        editor.LoadRace();
        editor.MinStrength.text = "19"; editor.MaxStrength.text = "19";
        editor.SaveRace();
        Check(editor.LastSaveSucceeded && State.RaceSettings.GetRaceStats(Race.Avians).Strength.Minimum == 19, "Native four-stat controls edit draft");
        editor.ActivateTraits(); Check(editor.TraitsPanel.activeSelf, "Trait tab available");
        editor.ActivateTags(); Check(editor.UnitTagsPanel.activeSelf, "Unit Tags tab available");
        editor.ActivateGeneral();
        var workflow = editor.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(b => b.name == "Balance workflow");
        workflow.onClick.Invoke();
        Check(editor.GetComponent<DeveloperBalancePanel>().IsVisible, "Authoring workflow opens through native button");
        editor.GetComponent<DeveloperBalancePanel>().Close();
        editor.CloseWithoutSave();
        Check(!BalanceAuthoring.IsDraft && !gm.Menu.HasRaceEditorAccess, "Cancel restores official profile and closes authorization");
        yield return null;

        var existing = new Unit(0, Race.Avians, 0, true, itemContext: CompanyEquipment.Repository, companyLoadout: true);
        string fingerprint = existing.SpeciesBalance.Fingerprint;
        int oldAP = existing.SpeciesSettings.GetBaseAP(existing.Race), oldStrength = existing.GetStatBase(Stat.Strength);
        BalanceAuthoring.BeginEditorSession();
        var item = State.RaceSettings.GetOrCreate(Race.Avians);
        item.BaseAP = 4; item.BodySize += 9;
        item.Stats.Strength = new RaceStats.StatRange(23, 23); item.Stats.Mirror();
        item.RaceTraits.Add(Traits.KeenEye);
        State.UnitTagList.Add(new UnitTag { id = 9123, name = "Synthetic accuracy tag", AssociatedTraits = new List<Traits> { Traits.KeenEye },
            modifiers = new List<UnitTagModifier> { new UnitTagModifier { id = 1, tagEffect = UnitTagModifierEffect.MeleeShift, tagCase = UnitTagModifierCase.AgainstAnyTarget, target = UnitTagModifierTarget.All, modifierValue = .1f } } });
        item.RaceTags.Add(9123);
        var generated = new Unit(0, Race.Avians, 0, true, itemContext: CompanyEquipment.Repository, companyLoadout: true);
        Check(generated.GetStatBase(Stat.Strength) == 23, "New unit uses authored stats");
        Check(generated.SpeciesSettings.GetBaseAP(generated.Race) == 4, "New unit uses authored AP");
        Check(generated.HasTrait(Traits.KeenEye), "New unit receives authored trait");
        Check(TagConditionChecker.GetCombinedUnitTags(generated).Any(t => t.id == 9123), "New unit uses authored tag");
        Check(Math.Abs(TagConditionChecker.ApplyTagEffect(generated, existing, UnitTagModifierEffect.MeleeShift) - .1f) < .0001f, "Authored modifier affects native combat calculation");
        Check(Math.Abs(TagConditionChecker.ApplyTagEffect(existing, generated, UnitTagModifierEffect.MeleeShift) - 1f) < .0001f, "Modifier retains incoming/outgoing targeting");
        existing.ReloadTraits(); existing.InitializeTraits(); existing.UpdateSpells();
        Check(existing.GetStatBase(Stat.Strength) == oldStrength && existing.SpeciesSettings.GetBaseAP(existing.Race) == oldAP, "Existing base stats and AP preserved");
        Check(existing.SpeciesBalance.Fingerprint == fingerprint && !TagConditionChecker.GetCombinedUnitTags(existing).Any(t => t.id == 9123), "Existing species and tags frozen");
        var restored = BalanceAuthoring.Copy(generated);
        BalanceAuthoring.RequireSnapshotPreserved(generated, restored);
        Check(restored.SpeciesSettings.GetBaseAP(restored.Race) == 4, "Unit snapshot serialized");
        Check(Math.Abs(TagConditionChecker.ApplyTagEffect(restored, existing, UnitTagModifierEffect.MeleeShift) - .1f) < .0001f, "Serialized modifier retains native effect");
        State.UnitTagList.Single(t => t.id == 9123).modifiers[0].modifierValue = .5f;
        Check(Math.Abs(TagConditionChecker.ApplyTagEffect(generated, existing, UnitTagModifierEffect.MeleeShift) - .1f) < .0001f, "Later tag edits preserve existing character effect");
        string exported = BalanceAuthoring.Export(Path.Combine(directory, "export.json"));
        string draftSave = BalanceAuthoring.SaveDraft();
        Check(File.Exists(draftSave) && File.Exists(exported), "Draft and export retained");
        var draft = BalanceAuthoring.Read(File.ReadAllText(exported));
        Check(draft.ParentRevision == baseline.Revision && draft.ParentHash == BalanceAuthoring.Hash(baselineText), "Export tracks official base");
        BalanceAuthoring.CancelEditorSession();
        Check(!BalanceAuthoring.IsDraft && State.RaceSettings.GetBaseAP(Race.Avians) == oldAP, "Cancel restores official settings");
        Check(BalanceAuthoring.LoadSavedDraft().Contains("Loaded"), "Saved draft resumes through normal entry point");
        Check(State.RaceSettings.GetBaseAP(Race.Avians) == 4, "Resumed draft retains AP");
        string corruptDraft = Path.Combine(Path.GetDirectoryName(draftSave), "zz-corrupt.json");
        File.WriteAllText(corruptDraft, "{");
        Check(BalanceAuthoring.LoadSavedDraft().Contains("Skipped 1"), "Corrupt draft recovers preceding valid generation");
        BalanceAuthoring.ImportDraft(exported);
        Check(State.RaceSettings.GetBaseAP(Race.Avians) == 4, "Reviewed package can activate as draft");
        Check(BalanceAuthoring.Describe(baseline, draft).Contains("BaseAP"), "Review lists changed settings");
        candidate = BalanceAuthoring.Copy(draft); candidate.ParentHash = new string('0', 64);
        Reject(() => BalancePublishingWindow.Promote(candidate), "Stale promotion rejected before writing");
        string syntheticProject = Path.Combine(directory, "SyntheticProject");
        BalancePublishingWindow.Commit(baseline, syntheticProject);
        BalancePublishingWindow.Commit(draft, syntheticProject);
        string balancePath = Path.Combine(syntheticProject, "Assets", "Resources", "WarbandBalance");
        string catalog = File.ReadAllText(Path.Combine(balancePath, "active.txt")), diagnostic;
        var promoted = BalanceAuthoring.LoadCatalog(catalog, r => File.ReadAllText(Path.Combine(balancePath, "Profiles", r + ".json")), out diagnostic);
        Check(promoted.Revision == draft.Revision && diagnostic == null, "Clean-client catalog selects promoted data");
        Check(File.ReadAllText(Path.Combine(syntheticProject, "CHANGES.md")).Contains(draft.Revision), "Promotion records modification");
        var recovered = BalanceAuthoring.LoadCatalog(catalog + "\nincomplete", r => File.ReadAllText(Path.Combine(balancePath, "Profiles", r + ".json")), out diagnostic);
        Check(recovered.Revision == draft.Revision && diagnostic != null, "Interrupted commit preserves previous profile");
        Check(File.Exists(Path.Combine(syntheticProject, "Logs", "BalancePromotions", draft.Revision, "CHANGES.md.before.txt")), "Promotion retains original modification record");
        using (var gate = new FileStream(Path.Combine(syntheticProject, "Logs", "BalancePromotions", "promotion.lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            Reject(() => BalancePublishingWindow.Commit(draft, syntheticProject), "Concurrent promotion cannot interleave");
        Reject(() => BalancePublishingWindow.Commit(draft, syntheticProject, new string('0', 64)), "Commit rechecks stale base under lock");
        int commits = catalog.Split('\n').Length;
        BalancePublishingWindow.Commit(draft, syntheticProject);
        Check(File.ReadAllText(Path.Combine(balancePath, "active.txt")).Split('\n').Length == commits, "Promotion retry is idempotent");
        BalanceAuthoring.UseOfficial();
        var cleanProfile = BalanceAuthoring.Read(File.ReadAllText(Path.Combine(balancePath, "Profiles", draft.Revision + ".json")));
        var officialField = typeof(BalanceAuthoring).GetField("official", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var hashField = typeof(BalanceAuthoring).GetField("officialHash", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        officialField.SetValue(null, cleanProfile); hashField.SetValue(null, BalanceAuthoring.Hash(BalanceAuthoring.Write(cleanProfile)));
        BalanceAuthoring.UseOfficial();
        var fresh = new Unit(0, Race.Avians, 0, true, itemContext: CompanyEquipment.Repository, companyLoadout: true);
        Check(!BalanceAuthoring.IsDraft && fresh.GetStatBase(Stat.Strength) == 23 && fresh.SpeciesSettings.GetBaseAP(fresh.Race) == 4, "Fresh client uses promoted data without local overrides");
        Check(existing.SpeciesBalance.Fingerprint == fingerprint, "Promoted defaults preserve existing character snapshot");
        BalanceAuthoring.ReloadOfficial();
        var company = CompanyCampaign.Generate(40, false);
        CompanyEconomy.Begin(company, CompanyEconomy.Create(company, Race.Avians, 1, true));
        CompanyCampaignSave.DiagnosticDirectory = Path.Combine(directory, "Saves");
        string save = CompanyCampaignSave.WriteGeneration(company, CompanyCampaignSave.DefaultDirectory);
        var loaded = CompanyCampaignSave.Load(save);
        Check(loaded.Characters.Count == company.Characters.Count, "Native company checkpoint restored");
        foreach (var person in company.Characters)
        {
            BalanceAuthoring.RequireSnapshotPreserved(person.Unit, loaded.Characters.Single(c => c.Id == person.Id).Unit);
            checks++;
        }
        var legacy = BalanceAuthoring.Copy(company);
        foreach (var person in legacy.Characters) person.Unit.SpeciesBalance = null;
        CompanyEconomy.Bind(legacy);
        Check(legacy.Characters.All(c => c.Unit.SpeciesBalance.Revision == BalanceAuthoring.LegacyRevision), "Older characters enroll against frozen baseline");
        Check(legacy.Characters.Select(c => c.Unit.Health).SequenceEqual(company.Characters.Select(c => c.Unit.Health)), "Legacy enrollment preserves health");
        var legacySettings = new RaceSettings(false);
        var legacyItem = BalanceAuthoring.Copy(baseline.Species.Single(e => e.Species == Race.Avians).Settings);
        legacyItem.Stats.Strength = new RaceStats.StatRange(25, 25); legacyItem.Stats.Mirror();
        legacySettings.Set(Race.Avians, legacyItem); legacySettings.Set(Race.FeralUmbreon, null);
        string legacyPath = Path.Combine(directory, "legacy-draft.dat");
        byte[] legacyBytes = OdinSerializer.SerializationUtility.SerializeValue(legacySettings, OdinSerializer.DataFormat.Binary);
        File.WriteAllBytes(legacyPath, legacyBytes);
        string legacyMessage = BalanceAuthoring.ImportLegacy(legacyPath);
        Check(legacyMessage.Contains("FeralUmbreon") && State.RaceSettings.GetRaceStats(Race.Avians).Strength.Minimum == 25, "Explicit binary import reports unavailable metadata and retains supported edits");
        Check(File.ReadAllBytes(legacyPath).SequenceEqual(legacyBytes), "Original legacy binary remains unchanged");
        legacySettings.Set(Race.Avians, null);
        File.WriteAllBytes(Path.Combine(directory, "invalid-legacy.dat"), OdinSerializer.SerializationUtility.SerializeValue(legacySettings, OdinSerializer.DataFormat.Binary));
        Reject(() => BalanceAuthoring.ImportLegacy(Path.Combine(directory, "invalid-legacy.dat")), "Malformed binary import rejected");
        Check(State.RaceSettings.GetRaceStats(Race.Avians).Strength.Minimum == 25, "Failed binary import preserves active draft");
        File.WriteAllText(Path.Combine(directory, "official-profile.json"), baselineText);
        BalanceAuthoring.UseOfficial();
    }
}

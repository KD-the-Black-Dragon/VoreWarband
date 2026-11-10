// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-09: Pass the optional enemy flag explicitly in synthetic reflected character creation.
// 2026-11-05: Render isolated player UI fixtures and inspect native layout/input across resolutions (spec 38 phase C).
// See CHANGES.md and License.txt for GPL-3.0 terms. Existing attribution remains applicable.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class DisplayInputAudit
{
    const string Key = "DisplayInputAudit";
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
    static IEnumerator steps;
    static Report report;
    static EditorWindow gameView;
    static object sizeGroup;
    static int addedSizes;
    static double started;
    static GameManager gm;
    [Serializable] public sealed class Report
    {
        public bool complete;
        public string directory, error, graphics;
        public List<string> checks = new List<string>();
        public List<string> observations = new List<string>();
        public List<Frame> frames = new List<Frame>();
    }
    [Serializable] public sealed class Frame
    {
        public string name, image;
        public int width, height;
        public List<Control> controls = new List<Control>();
    }
    [Serializable] public sealed class Control
    {
        public string path, kind, text;
        public float x, y, width, height, preferredWidth, preferredHeight;
        public bool raycast, scrollable, onScreen;
    }
    static DisplayInputAudit() { EditorApplication.update += Update; }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/base.unity");
        gameView = EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
        SessionState.SetInt(Key + ".size", (int)Get(gameView, "selectedSizeIndex"));
        SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
    }
    static object Get(object obj, string name)
    {
        var type = obj as Type ?? obj.GetType(); var field = type.GetField(name, Flags);
        return field != null ? field.GetValue(obj is Type ? null : obj) : type.GetProperty(name, Flags).GetValue(obj is Type ? null : obj, null);
    }
    static void Set(object obj, string name, object value)
    {
        var type = obj as Type ?? obj.GetType(); var field = type.GetField(name, Flags);
        if (field != null) field.SetValue(obj is Type ? null : obj, value);
        else type.GetProperty(name, Flags).SetValue(obj is Type ? null : obj, value, null);
    }
    static object Call(object obj, string name, params object[] args)
    {
        var type = obj as Type ?? obj.GetType();
        return type.GetMethods(Flags).First(m => m.Name == name && m.GetParameters().Length == args.Length)
            .Invoke(obj is Type ? null : obj, args);
    }
    static Type Runtime(string name) => typeof(GameManager).Assembly.GetType(name, true);
    static void Check(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); report.checks.Add(message); }
    static void Write() { File.WriteAllText(Path.Combine(report.directory, "report.json"), JsonUtility.ToJson(report, true)); }
    static void Update()
    {
        int exit = SessionState.GetInt(Key + ".exit", 0);
        if (exit != 0 && !EditorApplication.isPlayingOrWillChangePlaymode)
        { SessionState.EraseInt(Key + ".exit"); EditorApplication.Exit(exit - 1); return; }
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
        try
        {
            if (steps == null) { started = EditorApplication.timeSinceStartup; steps = Review(); }
            if (EditorApplication.timeSinceStartup - started > 600) throw new TimeoutException("Display review timed out");
            if (!steps.MoveNext()) Finish(null);
        }
        catch (Exception ex) { Finish(ex); }
    }
    static void Finish(Exception ex)
    {
        SessionState.SetBool(Key, false);
        if (report != null) { report.complete = ex == null; report.error = ex == null ? null : ex.ToString(); Write(); }
        if (gameView != null) Set(gameView, "selectedSizeIndex", SessionState.GetInt(Key + ".size", 0));
        if (sizeGroup != null)
            for (int i = addedSizes - 1; i >= 0; i--)
                Call(sizeGroup, "RemoveCustomSize", (int)Call(sizeGroup, "GetBuiltinCount") + (int)Call(sizeGroup, "GetCustomCount") - 1);
        SessionState.SetInt(Key + ".exit", ex == null ? 1 : 2); EditorApplication.isPlaying = false;
    }
    static void Resolution(int width, int height)
    {
        gameView = EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
        var editor = typeof(Editor).Assembly; var sizes = Get(editor.GetType("UnityEditor.GameViewSizes"), "instance");
        sizeGroup = Call(sizes, "GetGroup", Enum.Parse(editor.GetType("UnityEditor.GameViewSizeGroupType"), "Standalone"));
        var kind = Enum.Parse(editor.GetType("UnityEditor.GameViewSizeType"), "FixedResolution");
        var size = Activator.CreateInstance(editor.GetType("UnityEditor.GameViewSize"), Flags, null,
            new object[] { kind, width, height, "Phase C temporary " + width + "x" + height }, null);
        Call(sizeGroup, "AddCustomSize", size); addedSizes++;
        Set(gameView, "selectedSizeIndex", (int)Call(sizeGroup, "GetBuiltinCount") + (int)Call(sizeGroup, "GetCustomCount") - 1);
        gameView.Repaint();
    }
    static IEnumerator Review()
    {
        for (int i = 0; i < 12; i++) yield return null;
        report = new Report { directory = Path.GetFullPath("Logs/display38c-" + Guid.NewGuid().ToString("N")), graphics = SystemInfo.graphicsDeviceType.ToString() };
        Directory.CreateDirectory(report.directory); File.WriteAllText("Logs/display38c-latest.txt", report.directory);
        Set(Runtime("CompanyCampaignSave"), "DiagnosticDirectory", report.directory);
        gm = UnityEngine.Object.FindObjectOfType<GameManager>(); Check(gm != null, "Native base scene initialized");
        gm.enabled = false;
        var view = Get(gm.Start_Mode, "CompanySaveView");
        var state = Call(Runtime("CompanyCampaign"), "Generate", 38031, false);
        var leader = Call(Runtime("CompanyEconomy"), "Create", state, Race.Lizards, 2, true, 1, false);
        Call(Runtime("CompanyEconomy"), "Begin", state, leader);
        Call(view, "Install", state);
        var town = ((IList)Get(state, "Towns")).Cast<object>().First(t => (int)Get(t, "Tile") == (int)Get(state, "Position"));
        foreach (var candidate in ((IEnumerable)Call(Runtime("CompanyEconomy"), "Offers", state, town)).Cast<object>().Take(2).ToArray())
            Call(Runtime("CompanyEconomy"), "Hire", state, candidate, town);
        var ui = Get(view, "management"); var missions = Get(view, "missions");
        var resolutions = new[] { new Vector2Int(1920,1080), new Vector2Int(1280,720), new Vector2Int(1024,768),
            new Vector2Int(1280,1024), new Vector2Int(2560,1440) };
        foreach (var resolution in resolutions.Where(_ => !Environment.GetCommandLineArgs().Contains("-displayStressOnly") &&
            !Environment.GetCommandLineArgs().Contains("-displayTacticalOnly")))
        {
            Resolution(resolution.x, resolution.y);
            for (int i = 0; i < 5; i++) { gameView.Repaint(); yield return null; }
            Check(Screen.width == resolution.x && Screen.height == resolution.y, "Actual game resolution " + resolution);
            Hide(); gm.Start_Mode.UI.SetActive(true); yield return null; Capture("main-menu");
            Hide(); gm.Menu.Options.gameObject.SetActive(true); gm.Menu.Options.Open(); yield return null;
            Capture("options-top");
            foreach (var step in SliderChecks(resolution)) yield return step;
            foreach (var step in TooltipChecks()) yield return step;
            var optionScroll = gm.Menu.Options.GetComponentsInChildren<ScrollRect>().First();
            optionScroll.verticalNormalizedPosition = 0; yield return null; Capture("options-bottom");
            Hide(); gm.Menu.ContentSettings.gameObject.SetActive(true); gm.Menu.ContentSettings.Refresh(); gm.Menu.ContentSettings.Open(); yield return null;
            var selector = gm.Menu.ContentSettings.PageSelector;
            for (int page = 0; page < selector.options.Count; page++)
            {
                selector.value = page; yield return null; Capture("content-" + page);
                CheckVisibleSliders("Content page " + page);
            }
            Hide(); gm.Start_Mode.CreateTactical(); yield return null; Capture("single-battle");
            var setup = gm.Start_Mode.CreateTacticalGame;
            setup.Attacker.Race.Show(); yield return null; Capture("single-battle-dropdown"); setup.Attacker.Race.Hide();
            Hide(); ((GameObject)Get(view, "root")).SetActive(true); Call(ui, "StartCreator", false); yield return null;
            Capture("leader-creator");
            Call(view, "Install", state); ((GameObject)Get(view, "root")).SetActive(true); Call(view, "ShowMap"); yield return null; Capture("world-map");
            Call(ui, "Company", 0); yield return null; Capture("company");
            var nav = (GameObject)Get(view, "navigation");
            var mapButton = (Button)Get(view, "mapButton"); var mapPosition = ScreenRect(mapButton.GetComponent<RectTransform>());
            Click(mapButton); yield return null;
            Check(((GameObject)Get(view, "mapPage")).activeSelf, "Map navigation invokes native page action " + resolution);
            Click((Button)Get(view, "companyButton")); yield return null;
            Check(ScreenRect(mapButton.GetComponent<RectTransform>()) == mapPosition && nav.activeInHierarchy,
                "Persistent navigation retains position " + resolution);
            Call(ui, "Recruit", 0); yield return null; Capture("recruitment");
            Call(ui, "Shop", 0); yield return null; Capture("merchant");
            Call(missions, "Contracts", false, 0); yield return null; Capture("contracts");
            Call(missions, "Contracts", true, 0); yield return null; Capture("contract-offers");
            Call(missions, "Record", 0); yield return null; Capture("records");
            Call(missions, "Help", 0, false, false); yield return null; Capture("help");
            gm.Menu.HelpUI.gameObject.SetActive(false);
            byte[] ordinary = (byte[])Call(Runtime("CompanyCampaignSave"), "Encode", state);
            Set(state, "Gold", 2000000000); Set(state, "UpkeepDebt", 2000000000); Set(state, "UpkeepDueDay", Get(state, "NextPayday"));
            var first = ((IList)Get(state, "Characters")).Cast<object>().First(c => (string)Get(c, "Id") == (string)Get(state, "LeaderId"));
            ((Unit)Get(first, "Unit")).Name = new string('W', 100);
            foreach (var member in ((IList)Get(state, "Members")).Cast<object>())
            {
                var person = ((IList)Get(state, "Characters")).Cast<object>().Single(c => (string)Get(c, "Id") == (string)Get(member, "CharacterId"));
                Set(person, "Debt", 1000000000);
            }
            Call(ui, "Company", 0); yield return null; Capture("company-long-values");
            CheckRosterText((GameObject)Get(ui, "Page"), "Long roster " + resolution);
            Call(view, "ShowMap"); yield return null; Capture("map-long-values");
            if (resolution == resolutions[0])
                for (int save = 0; save < 9; save++) Call(Runtime("CompanySaveSlots"), "Manual", state, "Display review " + save);
            Call(Runtime("CompanySaveSlots"), "Manual", state, new string('W', 64));
            Call(Runtime("CompanySaveMenu"), "Show", view); yield return null; Capture("save-long-values");
            var longSaveMenu = Get(Runtime("CompanySaveMenu"), "instance");
            var longButton = ((GameObject)Get(longSaveMenu, "panel")).GetComponentsInChildren<Button>().First(b => b.name.StartsWith(new string('W', 64)));
            var caption = longButton.GetComponentInChildren<Text>();
            Check(caption.text.EndsWith("…") && caption.preferredWidth <= caption.rectTransform.rect.width + 1,
                "Long save list caption fits with ellipsis " + resolution);
            ExecuteEvents.Execute(longButton.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
            Check(((Text)Get(longSaveMenu, "preview")).text.Contains(new string('W', 100)), "Save hover preserves full leader name " + resolution);
            Click(longButton); yield return null;
            Check(((Text)Get(longSaveMenu, "preview")).text.Contains(new string('W', 64)), "Save selection preserves full save name " + resolution);
            var next = ((GameObject)Get(longSaveMenu, "panel")).GetComponentsInChildren<Button>().Single(b => b.name == "Next");
            Click(next); yield return null; Check((int)Get(longSaveMenu, "page") == 1, "Save list pagination " + resolution);
            Call(longSaveMenu, "Close", false);
            state = Call(Runtime("CompanyCampaignSave"), "Decode", ordinary); Call(view, "Install", state);
            Call(Runtime("CompanySaveMenu"), "Show", view); yield return null; Capture("save-load");
            var saveMenu = Get(Runtime("CompanySaveMenu"), "instance"); Call(saveMenu, "Close", false);
        }
        if (!Environment.GetCommandLineArgs().Contains("-displayTacticalOnly"))
            foreach (var step in CrowdedLists(view, ui, state, resolutions)) yield return step;
        Hide(); ((GameObject)Get(view, "root")).SetActive(true); Call(view, "ShowMap");
        var contract = ((IList)Get(state, "Contracts")).Cast<object>().First();
        Call(Runtime("CompanyContracts"), "Accept", state, contract); Call(Runtime("CompanyBattleSession"), "Prepare", state, contract);
        Set(Get(state, "Battle"), "PositionCommitted", true); Call(view, "LaunchBattle");
        var actor = ((IList)Get(gm.TacticalMode, "units")).Cast<Actor_Unit>().First(a => a.Unit.Side == 0);
        gm.TacticalMode.SelectedUnit = actor; Call(Get(gm.TacticalMode, "InfoPanel"), "RefreshTacticalUnitInfo", actor);
        for (int i = 0; i < 12; i++) gm.TacticalMode.Log.RegisterMiscellaneous("Display review line " + i);
        gm.TacticalMode.Log.RefreshListing();
        // Presentation stress only. No injury probabilities or battle outcomes are evaluated here.
        foreach (var trait in new[] { Traits.Berserk, Traits.KeenEye, Traits.Submissive, Traits.Maul }) actor.Unit.AddTrait(trait);
        foreach (var trait in new[] { Traits.EscapeArtist, Traits.FastDigestion, Traits.StrongMelee, Traits.Resilient, Traits.Ravenous,
            Traits.Frenzy, Traits.DefensiveStance, Traits.Tasty, Traits.Disgusting, Traits.MetalBody, Traits.AcidResistant, Traits.KeenReflexes })
            actor.Unit.AddTrait(trait);
        Call(Get(gm.TacticalMode, "InfoPanel"), "RefreshTacticalUnitInfo", actor);
        foreach (var resolution in resolutions)
        {
            Resolution(resolution.x, resolution.y);
            for (int i = 0; i < 4; i++) yield return null;
            var resizeScroll = gm.TacticalMode.InfoUI.InfoText.GetComponentInParent<ScrollRect>();
            float beforeRefit = resizeScroll.GetComponent<RectTransform>().offsetMin.y;
            Call(Runtime("TacticalHudView"), "FitTraitViewport", gm.TacticalMode);
            Check(Math.Abs(beforeRefit - resizeScroll.GetComponent<RectTransform>().offsetMin.y) < .01f,
                "Resizing fits trait viewport before any log action " + resolution);
            Capture("tactical");
            gm.TacticalMode.LogUI.Expand(); yield return null; Capture("tactical-expanded-log"); gm.TacticalMode.LogUI.Shrink(); yield return null;
            var traitScroll = gm.TacticalMode.InfoUI.InfoText.GetComponentInParent<ScrollRect>();
            report.observations.Add("Trait viewport " + ScreenRect(traitScroll.viewport) + " / content " + ScreenRect(traitScroll.content) + " / log " + ScreenRect(gm.TacticalMode.LogUI.GetComponent<RectTransform>()));
            Check(traitScroll != null, "Tactical traits retain their scroll viewport " + resolution);
            if (traitScroll.content.rect.height > traitScroll.viewport.rect.height)
            {
                var beforeScroll = traitScroll.content.anchoredPosition;
                ExecuteEvents.Execute(traitScroll.gameObject, new PointerEventData(EventSystem.current) { scrollDelta = Vector2.down * 5 }, ExecuteEvents.scrollHandler);
                yield return null;
                Check(traitScroll.content.anchoredPosition != beforeScroll, "Native scroll handler moves crowded trait content " + resolution);
            }
            else report.observations.Add("All stressed traits fit without scrolling " + resolution);
            traitScroll.verticalNormalizedPosition = 0; yield return null; Capture("tactical-traits-bottom");
            Check(ScreenRect(traitScroll.content).yMin >= ScreenRect(traitScroll.viewport).yMin - 1,
                "Final trait and innate spell can reach visible viewport " + resolution);
            traitScroll.verticalNormalizedPosition = 1;
            float reservedBottom = traitScroll.GetComponent<RectTransform>().offsetMin.y;
            gm.TacticalMode.LogUI.Shrink(); yield return null;
            float hiddenBottom = traitScroll.GetComponent<RectTransform>().offsetMin.y;
            Check(Math.Abs(hiddenBottom - 25) < .01f && hiddenBottom <= reservedBottom + .01f,
                "Hidden log restores the inherited trait viewport inset " + resolution);
            Capture("tactical-log-hidden");
            gm.TacticalMode.LogUI.Expand(); yield return null;
            gm.OpenMenu(); yield return null; Capture("escape-menu"); gm.CloseMenu();
            var screen = gm.StatScreen;
            screen.VictoryType.text = "Defenders victorious"; screen.AttackerTitle.text = "Avian"; screen.DefenderTitle.text = "Lizard";
            screen.AttackerText.text = (string)Call(gm.TacticalMode.TacticalStats, "AttackerSummary", 3);
            screen.DefenderText.text = (string)Call(gm.TacticalMode.TacticalStats, "DefenderSummary", 3, 0);
            screen.Open(false); yield return null; Capture("single-battle-results"); screen.gameObject.SetActive(false);
        }
    }
    static void Hide()
    {
        foreach (var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>().Where(c => c.isRootCanvas && c.renderMode != RenderMode.WorldSpace).ToArray())
            canvas.gameObject.SetActive(false);
    }

    static IEnumerable CrowdedLists(object view, object ui, object state, Vector2Int[] resolutions)
    {
        string originalDirectory = report.directory;
        string empty = Path.Combine(originalDirectory, "empty-saves"); Directory.CreateDirectory(empty);
        Set(Runtime("CompanyCampaignSave"), "DiagnosticDirectory", empty);
        Hide(); Call(Runtime("CompanySaveMenu"), "Show", view); yield return null;
        var menu = Get(Runtime("CompanySaveMenu"), "instance");
        Check(((IList)Get(menu, "entries")).Count == 0, "Empty synthetic save browser");
        Capture("save-empty"); Call(menu, "Close", false);
        Set(Runtime("CompanyCampaignSave"), "DiagnosticDirectory", originalDirectory);
        byte[] ordinary = (byte[])Call(Runtime("CompanyCampaignSave"), "Encode", state);
        var town = ((IList)Get(state, "Towns")).Cast<object>().First(t => (int)Get(t, "Tile") == (int)Get(state, "Position"));
        Set(state, "Gold", 2000000000);
        for (int i = 0; i < 8; i++)
        {
            var candidate = Call(Runtime("CompanyEconomy"), "Create", state, Race.Gnolls, 0, false, 1, false);
            Set(candidate, "TownId", Get(town, "Id")); ((IList)Get(state, "Characters")).Add(candidate);
            Call(Runtime("CompanyEconomy"), "Hire", state, candidate, town);
        }
        foreach (var candidate in ((IEnumerable)Call(Runtime("CompanyEconomy"), "Offers", state, town)).Cast<object>().ToArray())
            Call(Runtime("CompanyEconomy"), "Hire", state, candidate, town);
        var member = ((IList)Get(state, "Characters")).Cast<object>().First(c => Get(c, "Employment").ToString() == "Employed" && (string)Get(c,"Id") != (string)Get(state,"LeaderId"));
        var unit = (Unit)Get(member, "Unit");
        for (int i = 0; i < 8; i++)
        {
            var injury = Activator.CreateInstance(Runtime("CompanyInjury"), true);
            Set(injury, "Part", Enum.ToObject(Runtime("CompanyBodyPart"), i));
            Set(injury, "Grade", Enum.Parse(Runtime("CompanyInjuryGrade"), "Mangled")); Set(injury, "DaysRemaining", 28);
            ((IList)Get(unit, "Injuries")).Add(injury);
        }
        Call(view, "Install", state);
        foreach (var resolution in resolutions)
        {
            Resolution(resolution.x,resolution.y); for (int i = 0; i < 4; i++) yield return null;
            ((GameObject)Get(view,"root")).SetActive(true); Call(ui, "Company", 0); yield return null; Capture("company-full-list");
            var page = (GameObject)Get(ui,"Page");
            CheckRosterText(page, "Full roster " + resolution);
            var next = page.GetComponentsInChildren<Button>().Single(b => b.name == "Next"); Click(next); yield return null;
            Check((int)Get(ui,"offset") > 0, "Full roster next page " + resolution); Capture("company-next-page");
            Call(Get(view,"Recovery"), "Injuries", Get(member,"Id"), 0); yield return null; Capture("injuries-first-page");
            Click(page.GetComponentsInChildren<Button>().Single(b => b.name == "Next")); yield return null; Capture("injuries-next-page");
            Check(page.GetComponentsInChildren<Text>().Any(t => t.text.StartsWith("Mangled") && t.text.Contains("Knee")), "Injury next page reaches final body part " + resolution);
            Call(ui, "Recruit", 0); yield return null; Capture("recruit-empty");
            Check(!((IEnumerable)Call(Runtime("CompanyEconomy"), "Offers", state, town)).Cast<object>().Any() &&
                !page.GetComponentsInChildren<Button>().Any(b => b.name.StartsWith("Hire ")), "No phantom recruit action in empty pool " + resolution);
        }
        Call(Runtime("CompanyCampaignSave"), "Restore", state, ordinary); Call(view,"Install",state);
    }

    static GameObject Hit(Vector2 point)
    {
        Canvas.ForceUpdateCanvases();
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
        return hits.Count == 0 ? null : hits[0].gameObject;
    }
    static void Click(Button button)
    {
        Check(button.IsInteractable(), "Native button enabled: " + button.name);
        var hit = Hit(ScreenRect(button.GetComponent<RectTransform>()).center);
        Check(hit != null && hit.GetComponentInParent<Button>() == button, "Native raycast reaches button: " + button.name);
        ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
    }
    static void CheckVisibleSliders(string context)
    {
        int count = 0;
        foreach (var slider in UnityEngine.Object.FindObjectsOfType<Slider>())
        {
            if (!slider.IsInteractable() || slider.GetComponentInChildren<SliderRevealer>() == null) continue;
            var rect = ScreenRect(slider.GetComponent<RectTransform>());
            var scroll = slider.GetComponentInParent<ScrollRect>();
            if (scroll != null && !ScreenRect(scroll.viewport).Contains(rect.center)) continue;
            if (!new Rect(0, 0, Screen.width, Screen.height).Contains(rect.center)) continue;
            var hit = Hit(rect.center);
            Check(hit != null && hit.GetComponentInParent<Slider>() == slider, context + " raycast only intended slider " + slider.name);
            count++;
        }
        report.observations.Add(context + " visible slider centers inspected: " + count);
    }
    static IEnumerable SliderChecks(Vector2Int resolution)
    {
        CheckVisibleSliders("Options " + resolution);
        var slider = gm.Menu.Options.GetComponentsInChildren<Slider>().First(s => s.minValue == .005f);
        var sliderRect = ScreenRect(slider.GetComponent<RectTransform>());
        var outside = Hit(new Vector2(sliderRect.xMin - 40, sliderRect.center.y));
        Check(outside == null || outside.GetComponentInParent<Slider>() == null, "Row label outside slider has no slider target " + resolution);
        var revealer = slider.GetComponentInChildren<SliderRevealer>();
        float original = slider.value;
        var other = gm.Menu.Options.GetComponentsInChildren<Slider>().Where(s => s != slider).ToArray();
        var values = other.Select(s => s.value).ToArray();
        Call(revealer, "OpenValueDialog"); yield return null;
        var box = UnityEngine.Object.FindObjectsOfType<InputBox>().Single();
        Check((bool)Get(gm, "ActiveInput") && box.InputField.isFocused, "Slider popup owns input focus " + resolution);
        Check(box.InputField.characterLimit >= 5, "Slider accepts displayed minimum precision " + resolution);
        Check(box.InputField.textComponent.color == Color.black, "Popup input remains black " + resolution);
        Check(box.InputField.placeholder.color == new Color32(128,128,128,255), "Popup placeholder remains grey " + resolution);
        box.InputField.text = "0.005"; Capture("slider-input");
        var blocked = Hit(ScreenRect(slider.GetComponent<RectTransform>()).center);
        Check(blocked == null || blocked.GetComponentInParent<Slider>() == null, "Modal blocks underlying slider " + resolution);
        Call(revealer, "TrySetValue", "NaN"); Call(revealer, "TrySetValue", "Infinity"); Call(revealer, "TrySetValue", "invalid");
        Check(slider.value == original, "Invalid/non-finite values do not change slider " + resolution);
        box.YesClicked(); yield return null;
        Check(Math.Abs(slider.value - .005f) < .00001f && !(bool)Get(gm, "ActiveInput"), "Single slider value applies and popup releases focus " + resolution);
        Check(other.Select(s => s.value).SequenceEqual(values), "Other sliders unchanged " + resolution);
        Call(revealer, "TrySetValue", "100"); Check(slider.value == slider.maxValue, "Finite slider entry still clamps to maximum " + resolution);
        slider.value = original;
        Call(revealer, "OpenValueDialog"); yield return null;
        box = UnityEngine.Object.FindObjectsOfType<InputBox>().Single();
        if (resolution == new Vector2Int(1920,1080))
        {
            Resolution(1024,768); for (int i = 0; i < 4; i++) yield return null;
            Capture("resized-open-popup");
            var bounds = ScreenRect(box.GetComponent<RectTransform>());
            Check(bounds.xMin >= -1 && bounds.xMax <= Screen.width + 1 && bounds.yMin >= -1 && bounds.yMax <= Screen.height + 1,
                "Open popup remains visible after resolution change");
            Resolution(resolution.x, resolution.y); for (int i = 0; i < 4; i++) yield return null;
        }
        Click(box.No); yield return null;
        Check(!(bool)Get(gm, "ActiveInput") && UnityEngine.Object.FindObjectsOfType<InputBox>().Length == 0, "One Cancel dismisses slider entry " + resolution);
    }
    static IEnumerable TooltipChecks()
    {
        var tooltip = gm.HoveringTooltip;
        foreach (var canvas in tooltip.GetComponentsInParent<Canvas>(true)) canvas.gameObject.SetActive(true);
        tooltip.gameObject.SetActive(true); yield return null;
        foreach (var corner in new[] { new Vector2(0,0), new Vector2(Screen.width,0), new Vector2(0,Screen.height), new Vector2(Screen.width,Screen.height) })
        {
            Call(tooltip, "InfoUpdate", "Slider Value: 0.005\nRight Click to type in the number.", true);
            Call(tooltip, "PositionAt", corner);
            var bounds = ScreenRect(tooltip.GetComponent<RectTransform>());
            Check(bounds.xMin >= 3 && bounds.yMin >= 3 && bounds.xMax <= Screen.width - 3 && bounds.yMax <= Screen.height - 3,
                "Tooltip stays inside screen at " + corner + " / " + Screen.width + "x" + Screen.height);
        }
        Capture("tooltip-edge");
        Check(tooltip.GetComponentsInChildren<Graphic>().All(g => !g.raycastTarget), "Tooltip does not intercept pointer");
        tooltip.gameObject.SetActive(false);
    }
    static void CheckRosterText(GameObject page, string context)
    {
        foreach (var label in page.GetComponentsInChildren<Text>().Where(t => t.text.Contains("\nHP ") && t.text.Contains("\nInjuries ")))
            Check(label.preferredHeight <= label.rectTransform.rect.height + 1, context + " full name/HP/injury/debt summary fits");
    }

    static string PathFor(Transform item)
    { return item.parent == null ? item.name : PathFor(item.parent) + "/" + item.name; }
    static Rect ScreenRect(RectTransform rect)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners.Min(p => p.x), corners.Min(p => p.y), corners.Max(p => p.x), corners.Max(p => p.y));
    }
    static void Capture(string name)
    {
        Canvas.ForceUpdateCanvases();
        var frame = new Frame { name = name, width = Screen.width, height = Screen.height,
            image = Path.Combine(report.directory, Screen.width + "x" + Screen.height + "-" + name + ".png") };
        foreach (var rect in UnityEngine.Object.FindObjectsOfType<RectTransform>())
        {
            if (!rect.gameObject.activeInHierarchy) continue;
            var text = rect.GetComponent<Text>(); var tmp = rect.GetComponent<TMPro.TMP_Text>(); var select = rect.GetComponent<Selectable>();
            if (text == null && tmp == null && select == null) continue;
            var bounds = ScreenRect(rect);
            frame.controls.Add(new Control { path = PathFor(rect), kind = select != null ? select.GetType().Name : text != null ? "Text" : "TMP_Text",
                text = text != null ? text.text : tmp != null ? tmp.text : "", x = bounds.x, y = bounds.y, width = bounds.width, height = bounds.height,
                preferredWidth = (text != null ? text.preferredWidth : tmp != null ? tmp.preferredWidth : 0) * rect.lossyScale.x,
                preferredHeight = (text != null ? text.preferredHeight : tmp != null ? tmp.preferredHeight : 0) * rect.lossyScale.y,
                raycast = select != null && select.IsInteractable(), scrollable = rect.GetComponentInParent<ScrollRect>() != null,
                onScreen = bounds.xMin >= -1 && bounds.yMin >= -1 && bounds.xMax <= Screen.width + 1 && bounds.yMax <= Screen.height + 1 });
        }
        Render(frame.image); report.frames.Add(frame); Write();
    }
    static void Render(string path)
    {
        var worldCameras = UnityEngine.Object.FindObjectsOfType<Camera>().Where(c => c.isActiveAndEnabled && c.targetTexture == null).OrderBy(c => c.depth).ToArray();
        var roots = UnityEngine.Object.FindObjectsOfType<Canvas>().Where(c => c.isRootCanvas && c.isActiveAndEnabled && c.renderMode != RenderMode.WorldSpace).ToArray();
        var modes = roots.Select(c => c.renderMode).ToArray(); var cameras = roots.Select(c => c.worldCamera).ToArray();
        var planes = roots.Select(c => c.planeDistance).ToArray();
        var layers = roots.SelectMany(c => c.GetComponentsInChildren<Transform>(true)).Distinct().ToDictionary(t => t, t => t.gameObject.layer);
        var go = new GameObject("Display review render camera"); var camera = go.AddComponent<Camera>();
        camera.enabled = false; camera.orthographic = true; camera.orthographicSize = Screen.height / 2f;
        camera.transform.position = new Vector3(0, 0, -10); camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(12, 26, 46, 255); camera.cullingMask = 1 << 30;
        var target = new RenderTexture(Screen.width, Screen.height, 24); camera.targetTexture = target;
        var previous = RenderTexture.active;
        try
        {
            foreach (var layer in layers) layer.Key.gameObject.layer = 30;
            foreach (var root in roots) { root.renderMode = RenderMode.ScreenSpaceCamera; root.worldCamera = camera; root.planeDistance = 1; }
            Canvas.ForceUpdateCanvases();
            foreach (var worldCamera in worldCameras)
            {
                worldCamera.targetTexture = target;
                try { worldCamera.Render(); } finally { worldCamera.targetTexture = null; }
            }
            if (worldCameras.Length > 0) camera.clearFlags = CameraClearFlags.Depth;
            camera.Render(); RenderTexture.active = target;
            var pixels = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); pixels.Apply();
            File.WriteAllBytes(path, pixels.EncodeToPNG()); UnityEngine.Object.Destroy(pixels);
        }
        finally
        {
            for (int i = 0; i < roots.Length; i++) { roots[i].renderMode = modes[i]; roots[i].worldCamera = cameras[i]; roots[i].planeDistance = planes[i]; }
            foreach (var layer in layers) layer.Key.gameObject.layer = layer.Value;
            RenderTexture.active = previous; camera.targetTexture = null; target.Release();
            UnityEngine.Object.Destroy(target); UnityEngine.Object.Destroy(go); Canvas.ForceUpdateCanvases();
        }
    }
}

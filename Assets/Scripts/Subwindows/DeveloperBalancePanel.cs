// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-08: Add the password-gated weapon artwork gallery entry beside balance workflow.
// 2026-11-06: Password-gated balance draft review, import/export and AP authoring (spec 40A).
// 2026-11-06: Anchor the workflow entry in the Race Editor's bottom-left action bar.
// See CHANGES.md and License.txt for GPL-3.0 terms.
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

internal sealed class DeveloperBalancePanel : MonoBehaviour
{
    RaceEditorPanel owner;
    GameObject window;
    InputField pathField, apField;
    Text status, review;
    BalanceProfile pending;
    internal bool IsVisible => window != null && window.activeSelf;

    internal static void Attach(RaceEditorPanel editor)
    {
        if (editor.GetComponent<DeveloperBalancePanel>() != null) return;
        var panel = editor.gameObject.AddComponent<DeveloperBalancePanel>();
        panel.owner = editor;
        var root = UITheme.Node("Balance workflow entry", editor.transform);
        UITheme.Stretch(root.GetComponent<RectTransform>());
        var canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 400;
        var scaler = root.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 800); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        root.AddComponent<GraphicRaycaster>();
        var button = UITheme.Node("Balance workflow", root.transform);
        var rect = button.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
        rect.sizeDelta = new Vector2(220, 36); rect.anchoredPosition = new Vector2(24, 32);
        UITheme.Surface(button, UITheme.Control, true);
        var control = button.AddComponent<Button>(); UITheme.StyleSelectable(control);
        var label = UITheme.Label(button.transform, "Balance workflow", 0, 0, 220, 36, 18); label.alignment = TextAnchor.MiddleCenter;
        control.onClick.AddListener(panel.Open);
        DeveloperWeaponArtGallery.Attach(editor, root.transform);
    }
    void Open()
    {
        if (!State.GameManager.Menu.IsRaceEditorSessionActive) return;
        owner.SaveRace();
        if (!owner.LastSaveSucceeded) return;
        if (window == null) Build();
        window.SetActive(true);
        apField.text = State.RaceSettings.GetBaseAP(owner.SelectedSpecies).ToString();
        review.text = "Edit with Race Editor, then save or export a draft. Test the active draft in Battle Lab.\n\n" +
            "In Unity, use Warband > Balance > Import and promote to include approved values in future builds.\n\n" +
            "Unit Tags travel with the package. New individuals use the active balance; existing company characters keep their saved settings.\n\n" +
            "Trait assignments and tag modifiers are editable data. New trait behavior requires code. Power adjustment, upkeep/deployment and mapping controls serve legacy systems. Auto gear flags control legacy automatic weapon selection.";
        status.text = BalanceAuthoring.Identity;
        pending = null;
    }
    void Build()
    {
        window = UITheme.Node("Balance workflow window", transform);
        var canvas = window.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 500;
        var scaler = window.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 800); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        window.AddComponent<GraphicRaycaster>();
        var dim = UITheme.Node("Dimmer", window.transform); UITheme.Stretch(dim.GetComponent<RectTransform>()); UITheme.Surface(dim, new Color(0, 0, 0, .8f));
        var panel = UITheme.Node("Balance authoring", window.transform);
        var rect = panel.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(960, 700);
        UITheme.Surface(panel, UITheme.Panel, true);
        UITheme.Label(panel.transform, "Balance authoring", 24, 14, 900, 34, 25);
        status = UITheme.Label(panel.transform, "", 24, 52, 900, 40, 16);
        UITheme.Label(panel.transform, "Package path", 24, 104, 140, 30);
        pathField = Field(panel.transform, 170, 100, 760, 36);
        pathField.text = Path.Combine(Application.persistentDataPath, "BalanceExports", "balance-" + Guid.NewGuid().ToString("N") + ".json");
        Button(panel.transform, "Save draft", 24, 148, 160, () => Run(() => status.text = "Saved " + BalanceAuthoring.SaveDraft()));
        Button(panel.transform, "Export package", 196, 148, 182, () => Run(() =>
        {
            string path = pathField.text;
            if (File.Exists(path)) path = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path)), Path.GetFileNameWithoutExtension(path) + "-" + Guid.NewGuid().ToString("N") + ".json");
            pathField.text = BalanceAuthoring.Export(path);
            status.text = "Exported " + pathField.text;
        }));
        Button(panel.transform, "Review import", 390, 148, 170, () => Run(() =>
        {
            pending = BalanceAuthoring.Read(BalanceAuthoring.ReadFile(pathField.text));
            review.text = BalanceAuthoring.Describe(BalanceAuthoring.Capture(), pending);
            status.text = "Reviewed " + pending.Revision + ". Apply reviewed import to activate this draft.";
        }));
        Button(panel.transform, "Apply reviewed import", 572, 148, 248, () => Run(() =>
        {
            if (pending == null) throw new InvalidOperationException("Review a package first.");
            BalanceAuthoring.ApplyDraft(pending); pending = null; owner.ShowPanel();
            status.text = BalanceAuthoring.Identity;
        }));
        Button(panel.transform, "Import legacy slot", 24, 198, 200, () => Run(() =>
        {
            review.text = BalanceAuthoring.ImportLegacy(State.RaceSaveDataName);
            owner.ShowPanel(); status.text = BalanceAuthoring.Identity;
        }));
        Button(panel.transform, "Review against official", 236, 198, 240, () => Run(() => review.text = BalanceAuthoring.Describe(BalanceAuthoring.OfficialProfile(), BalanceAuthoring.Capture())));
        Button(panel.transform, "Show export folder", 488, 198, 210, () => Run(() =>
        {
            string directory = Path.GetDirectoryName(Path.GetFullPath(pathField.text));
            if (!Directory.Exists(directory)) throw new InvalidOperationException("Export a package first.");
            Application.OpenURL(new Uri(directory + Path.DirectorySeparatorChar).AbsoluteUri);
        }));
        Button(panel.transform, "Load saved draft", 710, 198, 220, () => Run(() =>
        {
            status.text = BalanceAuthoring.LoadSavedDraft(); owner.ShowPanel();
            apField.text = State.RaceSettings.GetBaseAP(owner.SelectedSpecies).ToString();
            pending = null;
        }));
        UITheme.Label(panel.transform, "Selected species base AP", 24, 253, 250, 30);
        apField = Field(panel.transform, 280, 250, 80, 34);
        Button(panel.transform, "Set AP", 372, 250, 120, () => Run(() =>
        {
            int value;
            if (!int.TryParse(apField.text, out value) || value < 1 || value > 100) throw new InvalidOperationException("Base AP must be 1–100.");
            State.RaceSettings.GetOrCreate(owner.SelectedSpecies).BaseAP = value;
            status.text = "New " + SpeciesPolicy.DisplayName(owner.SelectedSpecies) + " individuals will use " + value + " base AP.";
        }));
        Button(panel.transform, "Auto melee gear", 504, 250, 210, () => ToggleWeapon(true));
        Button(panel.transform, "Auto ranged gear", 726, 250, 210, () => ToggleWeapon(false));
        var scrollNode = UITheme.Node("Review", panel.transform); UITheme.Place(scrollNode.GetComponent<RectTransform>(), 24, 300, 912, 312);
        UITheme.Surface(scrollNode, UITheme.Background, true);
        var viewport = UITheme.Node("Viewport", scrollNode.transform); UITheme.Stretch(viewport.GetComponent<RectTransform>(), 8);
        viewport.AddComponent<Image>().color = Color.white; viewport.AddComponent<Mask>().showMaskGraphic = false;
        review = UITheme.Label(viewport.transform, "", 0, 0, 880, 300, 16); review.alignment = TextAnchor.UpperLeft;
        var content = review.rectTransform; content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(.5f, 1); content.sizeDelta = new Vector2(0, 0);
        var fit = review.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = scrollNode.AddComponent<ScrollRect>(); scroll.viewport = viewport.GetComponent<RectTransform>(); scroll.content = content; scroll.horizontal = false; scroll.scrollSensitivity = 28;
        Button(panel.transform, "Use official defaults", 24, 636, 242, () => Run(() =>
        {
            BalanceAuthoring.UseOfficial(); Close(); owner.gameObject.SetActive(false);
        }));
        Button(panel.transform, "Close", 736, 636, 200, Close);
    }
    void ToggleWeapon(bool melee)
    {
        Run(() =>
        {
            var item = State.RaceSettings.GetOrCreate(owner.SelectedSpecies);
            if (melee) item.CanUseMeleeWeapons = !(item.CanUseMeleeWeapons ?? true);
            else item.CanUseRangedWeapons = !(item.CanUseRangedWeapons ?? true);
            status.text = "Legacy auto gear for " + SpeciesPolicy.DisplayName(owner.SelectedSpecies) + ". Melee " + item.CanUseMeleeWeapons + ", ranged " + item.CanUseRangedWeapons;
        });
    }
    void Run(Action action)
    {
        try { action(); }
        catch (Exception ex) { status.text = ex.Message; }
    }
    internal void Close() { if (window != null) window.SetActive(false); }
    static InputField Field(Transform parent, float x, float y, float width, float height)
    {
        var node = UITheme.Node("Input", parent); UITheme.Place(node.GetComponent<RectTransform>(), x, y, width, height);
        UITheme.Surface(node, Color.white, true);
        var field = node.AddComponent<InputField>();
        var text = UITheme.Label(node.transform, "", 8, 2, width - 16, height - 4, 17); text.color = Color.black;
        field.textComponent = text; field.targetGraphic = node.GetComponent<Image>();
        return field;
    }
    static void Button(Transform parent, string title, float x, float y, float width, Action action)
    {
        var node = UITheme.Node(title, parent); UITheme.Place(node.GetComponent<RectTransform>(), x, y, width, 38);
        UITheme.Surface(node, UITheme.Control, true);
        var button = node.AddComponent<Button>(); UITheme.StyleSelectable(button);
        var text = UITheme.Label(node.transform, title, 4, 0, width - 8, 38, 16); text.alignment = TextAnchor.MiddleCenter;
        button.onClick.AddListener(() => action());
    }
}

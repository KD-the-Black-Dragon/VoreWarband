// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-06: Load summary catalogs in the background and reuse them while paging/hovering.
// 2026-11-05: Ellipsize long save-list captions while preserving full preview details (spec 38 phase C).
// 2026-11-03: Unified named Save/Load browser, progress previews, confirmations and Ironman controls (spec 19).
// See CHANGES.md and License.txt for GPL-3.0 terms; inherited notices remain applicable.
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

internal sealed class CompanySaveMenu : MonoBehaviour
{
    static CompanySaveMenu instance;
    internal static bool IsVisible => instance != null && instance.window != null && instance.window.activeSelf;
    GameObject window, panel;
    CompanyCampaignView view;
    List<CompanySaveSlots.Entry> entries;
    CompanySaveSlots.Entry selected;
    Text preview, status;
    InputField nameField;
    string saveName = "Manual save";
    bool returnToMenu, confirming;
    int page;
    Task<List<CompanySaveSlots.Entry>> catalog;
    Task<string> saving;
    string catalogError;
    internal static void Show(CompanyCampaignView owner)
    {
        if (instance == null) instance = State.GameManager.gameObject.AddComponent<CompanySaveMenu>();
        if (IsVisible) return;
        instance.view = State.GameManager.CompanyBattle == null ? owner : State.GameManager.CompanyBattle.View;
        instance.returnToMenu = State.GameManager.Menu.UIPanel.activeSelf;
        State.GameManager.Menu.UIPanel.SetActive(false);
        instance.page = 0; instance.selected = null; instance.BeginCatalog();
    }
    CompanyCampaign Current => State.GameManager.CompanyBattle != null ? State.GameManager.CompanyBattle.Campaign : view != null && view.HasActiveCompany ? view.Campaign : null;
    bool CanSave => Current != null && !Current.Ironman && !Current.Ended && (!Current.InBattle || State.GameManager.TacticalMode.CanSaveCompanyBattle);
    internal static void QuickSave()
    {
        var battle = State.GameManager.CompanyBattle;
        var owner = battle == null ? CompanyCampaignView.ActiveView : battle.View;
        if (owner == null || (battle == null && !owner.HasActiveCompany)) { State.GameManager.CreateMessageBox("Manual saving is available in a company campaign."); return; }
        var state = battle == null ? owner.Campaign : battle.Campaign;
        if (state.Ironman) { State.GameManager.CreateMessageBox("Ironman uses autosaves only."); return; }
        if (battle != null)
        {
            if (!State.GameManager.TacticalMode.CanSaveCompanyBattle) { State.GameManager.CreateMessageBox("Wait until your side is ready to act before saving."); return; }
        }
        try { if (battle != null) battle.Capture(); ObserveQuickSave(CompanySaveWorker.Manual(state, "Quicksave")); }
        catch (Exception ex) { State.GameManager.CreateMessageBox("Save failed: " + ex.Message); }
    }
    static async void ObserveQuickSave(Task<string> result)
    {
        try { await result; } catch { } // The worker's nonblocking notice reports failures.
    }
    void BeginCatalog()
    {
        string directory = CompanyCampaignSave.DefaultDirectory;
        entries = new List<CompanySaveSlots.Entry>(); catalogError = null;
        catalog = Task.Run(() => CompanySaveSlots.List(directory));
        Render();
    }
    void Render()
    {
        if (window == null)
        {
            window = UITheme.Node("Save Load", transform);
            var canvas = window.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 200;
            var scaler = window.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 800); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            window.AddComponent<GraphicRaycaster>();
            var dim = UITheme.Node("Dimmer", window.transform); UITheme.Stretch(dim.GetComponent<RectTransform>()); UITheme.Surface(dim, new Color(0, 0, 0, .7f));
            panel = UITheme.Node("Saved games", window.transform);
            var rect = panel.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(1140, 700);
            UITheme.Surface(panel, UITheme.Panel, true);
        }
        window.SetActive(true);
        foreach (Transform child in panel.transform) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        if (selected != null) selected = entries.FirstOrDefault(e => e.Path == selected.Path);
        if (selected == null) selected = entries.FirstOrDefault();
        page = Mathf.Clamp(page, 0, Math.Max(0, (entries.Count - 1) / 8));
        Label("Save / Load Game", 24, 18, 1090, 40, 28);
        Label(Current == null ? "Choose a saved game." : Current.Ironman ? "Ironman: autosaves only. The latest valid save is restored automatically." : "Save the current company or load another saved game.", 24, 66, 1090, 40, 17);
        preview = Label("", 680, 126, 430, 350, 19);
        RefreshPreview();
        for (int i = page * 8; i < Math.Min(entries.Count, page * 8 + 8); i++)
        {
            var entry = entries[i];
            string leader = entry.Summary.Leader;
            var button = Button(entry.Name + " — " + leader + " — Day " + entry.Summary.Day + (entry.Summary.Kind == 3 ? "" : " [Autosave]"), 24, 122 + (i % 8) * 48, 620, 42, () =>
            { selected = entry; if (entry.Summary.Kind == 3) saveName = entry.Name; Render(); });
            FitSaveCaption(button);
            var trigger = button.gameObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => preview.text = entry.Name + "\n\n" + entry.Details); trigger.triggers.Add(enter);
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => RefreshPreview()); trigger.triggers.Add(exit);
        }
        Button("Previous", 24, 520, 140, 38, () => { page--; Render(); }, page > 0);
        Button("Next", 176, 520, 140, 38, () => { page++; Render(); }, (page + 1) * 8 < entries.Count);
        Button("Load", 680, 500, 200, 44, () => Confirm("Load this saved game? Unsaved progress will be replaced.", "Load", () =>
        {
            string path = selected.Path;
            // Read/validate before closing the active battle or installing another company.
            var candidate = CompanyCampaignSave.LoadForResume(path);
            Close(false); view.LoadCheckpoint(path, candidate);
        }), selected != null);
        Button("Delete", 896, 500, 200, 44, () => Confirm(selected.Summary.Ironman ? "Delete this Ironman company and its recovery saves?" : "Delete this saved game and its backup copies?", "Delete", () =>
        {
            bool activeIronman = Current != null && Current.Ironman && Current.Id == selected.Summary.Id;
            CompanySaveSlots.Delete(selected);
            if (activeIronman)
            {
                Close(false);
                if (State.GameManager.CompanyBattle != null) State.GameManager.CompanyBattle.AbandonForLoad();
                view.ReturnToMainMenu();
            }
            else { selected = null; BeginCatalog(); }
        }), selected != null);
        Label("Save name", 24, 566, 420, 25, 16);
        var go = UITheme.Node("Save name", panel.transform); UITheme.Place(go.GetComponent<RectTransform>(), 24, 596, 420, 38);
        UITheme.Surface(go, UITheme.Control, true); nameField = go.AddComponent<InputField>();
        nameField.textComponent = UITheme.Label(go.transform, "", 8, 2, 404, 34, 18);
        nameField.text = saveName; nameField.characterLimit = 64; nameField.interactable = CanSave;
        nameField.onValueChanged.AddListener(value => saveName = value);
        Button("Save", 464, 596, 180, 38, Save, CanSave && saving == null);
        Button("Close", 896, 596, 200, 38, () => Close(true));
        status = Label(saving != null ? "Saving…" : catalog != null ? "Reading saved games…" : catalogError != null ? catalogError : entries.Count == 0 ? "No saved games yet." : "Select a save or hover over it to view company progress.", 24, 650, 1072, 36, 16);
    }
    void Save()
    {
        if (!CanSave) throw new InvalidOperationException("Saving is unavailable until a stable player action boundary.");
        Action save = () =>
        {
            if (State.GameManager.CompanyBattle != null) State.GameManager.CompanyBattle.Capture();
            saving = CompanySaveWorker.Manual(Current, saveName); Render(); status.text = "Saving…";
        };
        if (entries.Any(e => e.Summary.Id == Current.Id && e.Summary.Kind == 3 && string.Equals(e.Name, saveName.Trim(), StringComparison.OrdinalIgnoreCase)))
            Confirm("Replace the saved game named " + saveName + "?", "Save", save);
        else save();
    }
    void Confirm(string message, string action, Action callback)
    {
        if (confirming) return;
        confirming = true;
        var box = State.GameManager.CreateDialogBox();
        foreach (var canvas in box.GetComponentsInChildren<Canvas>(true)) canvas.sortingOrder = 1000;
        box.SetData(() => { confirming = false; Try(callback); }, action, "Cancel", message, () => confirming = false);
    }
    void RefreshPreview() { if (preview != null) preview.text = selected == null ? "No saved game selected." : selected.Name + "\n\n" + selected.Details; }
    static void FitSaveCaption(Button button)
    {
        var label = button.GetComponentInChildren<Text>();
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        string caption = label.text;
        int length = caption.Length;
        while (label.preferredWidth > label.rectTransform.rect.width && length > 0)
            label.text = caption.Substring(0, --length) + "…";
    }
    Text Label(string words, float x, float y, float width, float height, int size) { return UITheme.Label(panel.transform, words, x, y, width, height, size); }
    Button Button(string words, float x, float y, float width, float height, Action action, bool enabled = true)
    {
        var go = UITheme.Node(words, panel.transform); UITheme.Place(go.GetComponent<RectTransform>(), x, y, width, height);
        UITheme.Surface(go, UITheme.Control, true); var button = go.AddComponent<Button>(); UITheme.StyleSelectable(button);
        button.interactable = enabled; button.onClick.AddListener(() => Try(action));
        var label = UITheme.Label(go.transform, words, 6, 2, width - 12, height - 4, 17); label.alignment = TextAnchor.MiddleCenter; return button;
    }
    void Try(Action action) { try { action(); } catch (Exception ex) { if (status != null) status.text = ex.Message; else State.GameManager.CreateMessageBox(ex.Message); } }
    void Close(bool restoreMenu)
    {
        window.SetActive(false);
        if (restoreMenu && returnToMenu) State.GameManager.Menu.UIPanel.SetActive(true);
        else State.GameManager.CloseMenu();
    }
    void Update()
    {
        if (saving != null && saving.IsCompleted)
        {
            string result = saving.IsCanceled ? "Save cancelled." : saving.IsFaulted ? "Save failed: " + saving.Exception.GetBaseException().Message : "Game saved.";
            saving = null;
            if (IsVisible) { BeginCatalog(); status.text = result; }
        }
        if (catalog != null && catalog.IsCompleted)
        {
            if (catalog.IsFaulted) catalogError = "Could not read saves: " + catalog.Exception.GetBaseException().Message;
            else entries = catalog.Result;
            catalog = null;
            if (IsVisible) Render();
        }
        if (IsVisible && !confirming && !State.GameManager.ActiveInput && Input.GetButtonDown("Menu")) Close(true);
    }
}

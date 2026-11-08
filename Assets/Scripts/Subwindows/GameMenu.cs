// 2026-11-06: Enter explicit balance drafts behind the developer gate (spec 40A).
// Modified for the VoreWar mercenary-company conversion.
// 2026-11-08: Close the developer weapon art gallery when leaving or revoking a Race Editor session.
// 2026-11-04: Add isolated password-gated developer battle/recovery controls (spec 34 slice 2).
// 2026-10-30: Gate Race Editor sessions and nested tags; retire obsolete menu routes (spec 02B).
// 2026-11-03: Present Esc menu as a compact overlay; omit Race Editor and shorten Help label (spec 17).
// 2026-11-03: Replace the legacy save/load entry with the unified company interface (spec 19).
// 2026-11-03: Open the shared campaign guide from the existing Help callbacks (spec 20).
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameMenu : MonoBehaviour
{
    // Convenience gate only; the project owner accepts source discoverability.
    private const string RaceEditorPassword = "214214214";
    private bool raceEditorUnlocked;
    private bool presentationReady;
    private InputBox raceEditorPrompt;

    internal bool HasRaceEditorAccess => raceEditorUnlocked;
    internal bool IsRaceEditorSessionActive => raceEditorUnlocked && RaceEditor != null && RaceEditor.gameObject.activeInHierarchy;

    internal void EndRaceEditorSession()
    {
        raceEditorUnlocked = false;
        if (RaceEditor != null)
        {
            var weaponGallery = RaceEditor.GetComponent<DeveloperWeaponArtGallery>();
            if (weaponGallery != null) weaponGallery.Close();
        }
        if (UnitTagEditor != null)
        {
            if (UnitTagEditor.UnitTagMenu != null)
                UnitTagEditor.UnitTagMenu.gameObject.SetActive(false);
            UnitTagEditor.gameObject.SetActive(false);
        }
    }

    internal void ShowRetiredToolMessage()
    {
        State.GameManager.CreateMessageBox("This tool is retired in this conversion.");
    }

    internal bool HandleRaceEditorInput()
    {
        if (!IsRaceEditorSessionActive)
            return false;
        if (Input.GetButtonDown("Menu"))
        {
            var balance = RaceEditor.GetComponent<DeveloperBalancePanel>();
            if (balance != null && balance.IsVisible) { balance.Close(); return true; }
            var weaponGallery = RaceEditor.GetComponent<DeveloperWeaponArtGallery>();
            if (weaponGallery != null && weaponGallery.IsVisible) { weaponGallery.Close(); return true; }
            if (UnitTagEditor.UnitTagMenu.gameObject.activeSelf)
                UnitTagEditor.UnitTagMenu.DiscardClose();
            else if (UnitTagEditor.gameObject.activeSelf)
                UnitTagEditor.Close();
            else
                RaceEditor.CloseWithoutSave();
        }
        return true;
    }

    public Options Options;
    public GameObject UIPanel;
    public WorldSettings WorldSettingsUI;
    public EmpireSettings EmpireSettingsUI;

    public SaveLoad SaveLoadScreen;
    public CheatMenu CheatMenu;
    public RaceEditorPanel RaceEditor;
    public RandomizerTraitEditor RandomizerTraitEditor;
    public CustomTraitEditor CustomTraitEditor;
    public ConditionalTraitsEditor CondTraitEditor;
    public UnitTagEditor UnitTagEditor;

    public HelpPanel HelpUI;

    public Button SaveLoadButton;
    public Button OpenMapEditorButton;
    public Button WorldSettingsButton;
    public Button EmpireSettingsButton;

    public GameObject HelpScreen;

    public ContentSettings ContentSettings;

    public void UpdateButtons()
    {
        OpenMapEditorButton.gameObject.SetActive(false);
        WorldSettingsButton.gameObject.SetActive(false);
        EmpireSettingsButton.gameObject.SetActive(false);
        SaveLoadButton.interactable = State.TutorialMode == false;
        EnsurePresentation();
    }

    private void EnsurePresentation()
    {
        if (presentationReady) return;
        presentationReady = true;
        var buttons = UIPanel.GetComponentsInChildren<Button>(true);
        // This canvas contains only the menu background/buttons, not Options or Race Editor.
        foreach (var image in UIPanel.GetComponentsInChildren<Image>(true))
            if (image.GetComponentInParent<Button>() == null)
                UITheme.Surface(image.gameObject, new Color(0, 0, 0, .6f));
        var panel = UITheme.Node("Menu overlay", UIPanel.transform);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(540, 600);
        UITheme.Surface(panel, UITheme.Panel, true);
        UITheme.Label(panel.transform, "Menu", 40, 18, 460, 40, 26);
        string[] order = { "Close Menu", "Options", "Content Settings", "Save / Load Game", "Help Screens", "Main Menu", "Exit Game" };
        foreach (var button in buttons)
        {
            var label = button.GetComponentInChildren<Text>(true);
            if (label == null) continue;
            if (label.text.Trim() == "Race Editor") { button.gameObject.SetActive(false); continue; }
            int index = Array.IndexOf(order, label.text.Trim());
            if (index < 0) continue;
            button.transform.SetParent(panel.transform, false);
            button.transform.localScale = Vector3.one;
            UITheme.Place(button.GetComponent<RectTransform>(), 40, 74 + index * 70, 460, 54);
            if (label.text.Trim() == "Help Screens") label.text = "Help";
            label.fontSize = 24; label.resizeTextForBestFit = false;
        }
        UITheme.StylePopup(panel);
    }

    public void OpenSaveLoadMenu()
    {
        CompanySaveMenu.Show(State.GameManager.Start_Mode.CompanySaveView);
    }

    public void OpenCheats()
    {
        ShowRetiredToolMessage();
    }

    public void OpenRaceEditor()
    {
        RequestDeveloperAccess(() => { BalanceAuthoring.BeginEditorSession(); RaceEditor.gameObject.SetActive(true); RaceEditor.ShowPanel(); }, "Race Editor");
    }

    public void OpenBattleLab()
    {
        if (State.GameManager.CurrentScene != State.GameManager.Start_Mode || State.GameManager.CompanyBattle != null) return;
        RequestDeveloperAccess(() =>
        {
            var lab = State.GameManager.GetComponent<DeveloperBattleLab>() ?? State.GameManager.gameObject.AddComponent<DeveloperBattleLab>();
            lab.Open();
        }, "Battle Lab");
    }

    void RequestDeveloperAccess(Action open, string title)
    {
        if (IsRaceEditorSessionActive || raceEditorPrompt != null || DeveloperBattleLab.IsOpen) return;
        raceEditorPrompt = State.GameManager.CreateInputBox();
        raceEditorPrompt.SetData((string value) =>
        {
            raceEditorPrompt = null;
            if (!string.Equals(value, RaceEditorPassword, StringComparison.Ordinal))
            { State.GameManager.CreateMessageBox("Incorrect developer password."); return; }
            raceEditorUnlocked = true;
            open();
        }, "Unlock", "Cancel", "Enter the developer password to open " + title + ".", 64);
        raceEditorPrompt.InputField.contentType = TMP_InputField.ContentType.Password;
        raceEditorPrompt.InputField.text = "";
    }

    public void OpenOptions()
    {
        Options.gameObject.SetActive(true);
        Options.Open();
    }

    public void OpenRandomizerTraits()
    {
        ShowRetiredToolMessage();
    }

    public void OpenCustomTraits()
    {
        ShowRetiredToolMessage();
    }

    public void OpenCondTraits()
    {
        ShowRetiredToolMessage();
    }
    public void OpenUnitTags()
    {
        if (!IsRaceEditorSessionActive)
            return;
        UnitTagEditor.Open();
    }
    public void LoadHelp()
    {
        HelpUI.Open();
    }

    public void OpenEmpireTraits()
    {
        ShowRetiredToolMessage();
    }

    public void CloseHelp()
    {
        HelpScreen.SetActive(false);
    }

    public void ShowGameSettings()
    {
        ShowRetiredToolMessage();
    }

    public void UpdateDisplay()
    {
        if (HandleRaceEditorInput())
            return;
        Options.RefreshText();
        if (Input.GetButtonDown("Menu"))
        {
            if (HelpScreen.activeSelf)
                CloseHelp();
            else if (State.GameManager.VariableEditor.gameObject.activeSelf)
                State.GameManager.VariableEditor.Close();
            else if (SaveLoadScreen.gameObject.activeSelf)
                SaveLoadScreen.gameObject.SetActive(false);
            else if (Options.gameObject.activeSelf)
                Options.CloseAndSave();
            else if (ContentSettings.gameObject.activeSelf)
                ContentSettings.Exit();
            else
                CloseMenu();
        }

    }

    public void MainMenu()
    {
        if (State.GameManager.CurrentScene == State.GameManager.StrategyMode)
        {
            State.GameManager.StrategyMode.ClearData();
        }
        State.GameManager.SwitchToMainMenu();
        CloseMenu();
    }

    public void ExitGame()
    {
        Application.Quit();
        CloseMenu();
    }

    public void CloseMenu()
    {
        State.GameManager.CloseMenu();
        if (State.GameManager.CurrentScene == State.GameManager.StrategyMode)
        {
            State.GameManager.StrategyMode.Regenerate();
        }
        if (State.GameManager.CurrentScene == State.GameManager.TacticalMode)
        {
            State.GameManager.TacticalMode.RebuildInfo();
        }
    }



}

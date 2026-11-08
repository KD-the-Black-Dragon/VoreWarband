// Modified for the VoreWar mercenary-company conversion.
// 2026-11-04: Add isolated password-gated developer battle/recovery controls (spec 34 slice 2).
// 2026-10-30: Retire startup authoring/tutorial/map routes and route trait warning through the gate (spec 02B).
// 2026-10-30: Guard the empty approved roster before entering old campaign setup (spec 03A1).
// 2026-10-31: Retire legacy world load/preview and empire campaign entry routes (spec 05A1).
// 2026-11-01: Open the independent company foundation preview without legacy campaign setup (spec 06).
// 2026-11-02: Restyle/reorder main-menu buttons, retire presets, display version 1.0 and open company checkpoints (spec 15C).
// 2026-11-02: Enlarge menu controls, separate Credits/Help and replace title text with the supplied banner logo (spec 15C feedback).
// 2026-11-02: Add a temporary main-menu portrait comparison entry (spec 15E).
// 2026-11-02: Retire the temporary portrait-comparison main-menu entry (spec 15D feedback).
// 2026-11-03: Expose the company load view without reopening campaign setup (spec 19).
// 2026-11-03: Rename the main-menu load action to Load Game (spec 20).
// 2026-11-03: Keep developer trait-category diagnostics off the player main menu (spec 22).
// 2026-11-08: Add High Scores access and recover Ironman cleanup after history publication (spec 40E).
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
using UnityEngine;
using UnityEngine.UI;

public class StartMode : SceneBase
{
    public Text versionNumber;
    public Text miscText;

    public Button Preset1;
    public Button Preset2;
    public Button Preset3;

    public Button TagAlertButton;
    public Transform TaggedTraitFolder;
    public GameObject TaggedTraitObject;

    public TaggedTraitPrefab TaggedTraitPrefab;

    public void Start()
    {
        State.GameManager.Menu.Options.LoadFromStored();
        State.GameManager.Menu.CheatMenu.LoadFromStored();
        versionNumber.text = "Version: 1.0";
        StyleMainMenu();
        State.GameManager.Menu.ContentSettings.Refresh();
        UpdatePresetsVisible();
        State.LoadRaceData();
        CompanyHistoryStore.RecoverIronmanCleanup();
        RunTaggedTraitCheck();
    }

    CompanyCampaignView companyCampaignView;
    internal CompanyCampaignView CompanySaveView
    {
        get
        {
            if (companyCampaignView == null) companyCampaignView = gameObject.AddComponent<CompanyCampaignView>();
            companyCampaignView.PrepareSaveBrowser(this); return companyCampaignView;
        }
    }

    public CreateStrategicGame CreateStrategicGame;
    public void CreateStrategic()
    {
        if (companyCampaignView == null) companyCampaignView = gameObject.AddComponent<CompanyCampaignView>();
        companyCampaignView.Open(this);
    }

    public CreateTacticalGame CreateTacticalGame;
    public void CreateTactical()
    {
        UI.SetActive(false);
        CreateTacticalGame.gameObject.SetActive(true);
        CreateTacticalGame.Open();

    }

    public void UpdatePresetsVisible()
    {
        Preset1.gameObject.SetActive(false);
        Preset2.gameObject.SetActive(false);
        Preset3.gameObject.SetActive(false);
    }

    void StyleMainMenu()
    {
        // Expand keeps space for the banner and larger controls in short widescreen windows.
        UI.GetComponent<CanvasScaler>().screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        UITheme.Surface(UI.transform.Find("Panel").gameObject, UITheme.Background);
        foreach (Button button in UI.GetComponentsInChildren<Button>(true))
        {
            if (button.transform.parent != UI.transform) continue;
            if (button.targetGraphic != null)
                UITheme.Surface(button.targetGraphic.gameObject, UITheme.Control, true);
            UITheme.StyleSelectable(button);
            foreach (Text label in button.GetComponentsInChildren<Text>(true))
            {
                UITheme.StyleText(label, 32);
                label.alignment = TextAnchor.MiddleCenter;
            }
        }
        // These are the existing scene objects and callbacks, including developer access.
        PlaceMainButton("Create Strategic Button", "New Campaign", 420);
        PlaceMainButton("Create Tactical Button", "Single Battle", 328);
        PlaceMainButton("Load Game Button", "Load Game", 236);
        PlaceMainButton("Options Button", "Options", 144);
        PlaceHighScoresButton();
        PlaceCornerButton("Credits Button", 204);
        PlaceCornerButton("Help Button", 120);
        PlaceCornerButton("Exit Game Button", 0);
        DeveloperBattleLab.AddMenuButton(this);
        ReplaceTitleWithLogo();
        UITheme.StyleText(versionNumber, 28); versionNumber.color = UITheme.Muted;
        UITheme.StyleText(miscText, 24); miscText.color = UITheme.Muted;
    }

    void PlaceHighScoresButton()
    {
        var go = UITheme.Node("High Scores Button", UI.transform);
        var rect = go.GetComponent<RectTransform>(); UITheme.Place(rect, 0, 52, 460, 76);
        var image = UITheme.Surface(go, UITheme.Control, true);
        var button = go.AddComponent<Button>(); button.targetGraphic = image; UITheme.StyleSelectable(button);
        button.onClick.AddListener(OpenHighScores);
        var label = UITheme.Label(go.transform, "High Scores", 8, 4, 444, 68, 32);
        label.alignment = TextAnchor.MiddleCenter;
    }

    void OpenHighScores()
    {
        if (companyCampaignView == null) companyCampaignView = gameObject.AddComponent<CompanyCampaignView>();
        companyCampaignView.OpenHistory(this);
    }

    void PlaceMainButton(string objectName, string caption, float bottom)
    {
        var button = UI.transform.Find(objectName).GetComponent<Button>();
        var rect = button.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
        rect.pivot = new Vector2(.5f, 0);
        rect.sizeDelta = new Vector2(460, 76);
        rect.anchoredPosition = new Vector2(0, bottom);
        button.GetComponentInChildren<Text>(true).text = caption;
    }

    void PlaceCornerButton(string objectName, float bottom)
    {
        var rect = UI.transform.Find(objectName).GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 68);
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, bottom);
    }

    void ReplaceTitleWithLogo()
    {
        var sprite = Resources.Load<Sprite>("UI/VoreWarbandLogo");
        if (sprite == null) return; // Retain the original title if the required asset is unavailable.
        var logo = UITheme.Node("Vore Warband logo", UI.transform);
        var rect = logo.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(1200, 400);
        rect.anchoredPosition = new Vector2(0, 260);
        var image = logo.AddComponent<Image>();
        image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
        UI.transform.Find("TextMeshPro Text").gameObject.SetActive(false);
    }

    public void ChangeAssimilableTraits()
    {
        State.GameManager.Menu.ShowRetiredToolMessage();
    }

    public void ChangeRandomizableTraits()
    {
        State.GameManager.Menu.ShowRetiredToolMessage();
    }

    public void ModifyCustomTraits()
    {
        State.GameManager.Menu.ShowRetiredToolMessage();
    }

    public void ModifyCondTraits()
    {
        State.GameManager.Menu.ShowRetiredToolMessage();
    }

    public void ReturnToStart()
    {
        if (companyCampaignView != null) companyCampaignView.Close();
        if (State.GameManager.Menu.RaceEditor.gameObject.activeSelf)
            State.GameManager.Menu.RaceEditor.CloseWithoutSave();
        State.TutorialMode = false;
        State.GameManager.TutorialScript = null;
        UI.SetActive(true);
        CreateStrategicGame.gameObject.SetActive(false);
        CreateTacticalGame.gameObject.SetActive(false);
        State.GameManager.Menu.ContentSettings.Refresh();
    }

    public void TutorialMode()
    {
        State.GameManager.Menu.ShowRetiredToolMessage();
    }

    public void LoadGame()
    {
        if (companyCampaignView == null) companyCampaignView = gameObject.AddComponent<CompanyCampaignView>();
        companyCampaignView.OpenSavedGames(this);
    }

    public void MapEditor()
    {
        State.GameManager.Menu.ShowRetiredToolMessage();
    }

    public override void ReceiveInput()
    {
        if (companyCampaignView != null && companyCampaignView.IsOpen) return;
        State.GameManager.Menu.HandleRaceEditorInput();
        //Vector2 currentMousePos = GetComponent<Camera>().ScreenToWorldPoint(Input.mousePosition);
    }

    public override void CleanUp()
    {
    }


    public void RunTaggedTraitCheck()
    {
        TagAlertButton.gameObject.SetActive(false); // Trait taxonomy is a developer concern, not a player warning.
        CloseTaggedTraitCheck();
    }

    public void OpenTaggedTraitCheck()
    {
        State.GameManager.Menu.OpenRaceEditor();
    }

    public void CloseTaggedTraitCheck()
    {
        TaggedTraitFolder.gameObject.SetActive(false);
        TaggedTraitObject.gameObject.SetActive(false);
    }


}

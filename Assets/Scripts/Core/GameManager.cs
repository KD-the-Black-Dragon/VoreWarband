// Modified for the VoreWar mercenary-company conversion.
// 2026-11-04: Add isolated password-gated developer battle/recovery controls (spec 34 slice 2).
// 2026-10-30: Disable the alternate in-game map editor entry (spec 02B).
// 2026-10-31: Retire legacy world load/preview and empire campaign entry routes (spec 05A1).
// 2026-10-31: Block strategic-mode activation while preserving Pure Tactical return to menu (spec 05A2).
// 2026-10-31: Retire campaign battle and loaded-world tactical transitions (spec 05A3A).
// 2026-11-01: Enter/return company battles explicitly and route deliberate battle exit to its prebattle checkpoint (spec 10).
// 2026-11-01: Label the tactical enemy side as Enemy forces, keeping contract ratings in mission views (09–10 feedback).
// 2026-11-03: Route save/load to the company browser and pause input while it is open (spec 19).
// 2026-11-03: Keep Help input modal and close it with Escape without advancing the underlying battle (spec 20).
// 2026-11-11: Load and validate the editable in-battle vore narrative catalogs at startup.
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
#if UNITY_EDITOR == false
    bool confirmedQuit = false;
#endif

    private SceneBase currentScene;

    public SceneBase CurrentScene
    {
        get { return currentScene; }
        private set
        {
            if (currentScene != null)
                currentScene.UI.SetActive(false);
            currentScene = value;
            currentScene.UI.SetActive(true);
        }
    }

    public Camera Camera;
    public Camera_Controller CameraController;

    public PipCamera PipCamera;
    public StartMode Start_Mode;
    public StrategyMode StrategyMode;
    public TacticalMode TacticalMode;
    public Recruit_Mode Recruit_Mode;
    public EndScene EndScene;
    public MapEditor MapEditor;

    public VariableEditor VariableEditor;


    public StatScreenPanel StatScreen;

    public HoveringTooltip HoveringTooltip;
    public HoveringRacePicture HoveringRacePicture;

    public GameObject SavePrompt;
    public GameObject DialogBoxPrefab;
    public GameObject OptionsBoxPrefab;
    public GameObject MessageBoxPrefab;
    public GameObject InputBoxPrefab;
    public GameObject FullScreenMessageBoxPrefab;
    public GameObject UIUnit;

    public GameObject ParticleSystem;

    public Material ColorSwapMaterial;

    public GameObject LoadPicker;

    public GameObject DiscardedClothing;

    public GameObject SpriteRendererPrefab;
    public GameObject SpriteRenderAnimatedPrefab;
    public GameObject ImagePrefab;
    public BattleReportPanel BattleReportPanel;

    public GameObject CornerCameraView;

    public GameMenu Menu;

    public TutorialScript TutorialScript;

    bool menuOpen = false;

    public bool PureTactical = false;
    internal CompanyBattleSession CompanyBattle;

    public GameObject UnitBase;
    public UnitEditorPanel UnitEditor;
    public SpriteDictionary SpriteDictionary;
    public TacticalBuildingDictionary TacticalBuildingSpriteDictionary;
    public PaletteDictionary PaletteDictionary;
    public SoundManager SoundManager;
    public TacticalEffectPrefabList TacticalEffectPrefabList;
    public bool StrategicControlsLocked { get; private set; }
    public bool CameraTransitioning { get; private set; }
    Vector3 oldCameraPosition;
    Vector3 newCameraPosition;
    float cameraTransitionTime;
    float cameraCurrentTransitionTime;

    internal bool queuedTactical = false; // Retained read compatibility; campaign queuing is retired.

    float remainingCameraTime;

    internal bool ActiveInput;

    public enum PreviewSkip
    {
        Watch,
        SkipWithStats,
        SkipNoStats
    }

    internal PreviewSkip CurrentPreviewSkip = PreviewSkip.Watch;


    void Start()
    {
        Start_Mode.UI.SetActive(true);
        currentScene = Start_Mode;
        State.GameManager = this;
        InBattleVoreNarratives.Load();
        Application.wantsToQuit += () => WantsToQuit();
    }

    void Quit()
    {
#if UNITY_EDITOR == false
        confirmedQuit = true;
#endif
        Application.Quit();
    }

    bool WantsToQuit()
    {
#if UNITY_EDITOR
        {
            return true;
        }
#else
        var box = Instantiate(DialogBoxPrefab).GetComponent<DialogBox>();
        box.SetData(() => Quit(), "Quit Game", "Cancel", "Are you sure you want to quit?");
        return confirmedQuit;
#endif

    }

    //private void CopyFilesOfType(string extension, string destination)
    //{
    //    string[] files = Directory.GetFiles(Directory.GetCurrentDirectory());

    //    foreach (string file in files)
    //    {
    //        if (!File.Exists(file)) return;

    //        if (file.EndsWith("." + extension))
    //        {
    //            string fileName = Path.GetFileName(file);
    //            File.Move(file, destination + fileName);

    //        }
    //    }
    //}

    /// <summary>
    /// must be manually initialized
    /// </summary>
    public DialogBox CreateDialogBox()
    {
        return Instantiate(DialogBoxPrefab).GetComponent<DialogBox>();
    }

    public OptionsBox CreateOptionsBox()
    {
        return Instantiate(OptionsBoxPrefab).GetComponent<OptionsBox>();
    }

    public void CreateMessageBox(string text, int timedLife = 0)
    {
        MessageBox box = Instantiate(MessageBoxPrefab).GetComponent<MessageBox>();
        box.InfoText.text = text;
        if (timedLife > 0)
        {
            box.gameObject.AddComponent<TimedLife>();
            var timer = box.GetComponent<TimedLife>();
            timer.Life = timedLife;
        }
    }

    public void CreateFullScreenMessageBox(string text)
    {
        MessageBox box = Instantiate(FullScreenMessageBoxPrefab).GetComponent<MessageBox>();
        box.InfoText.text = text;
    }

    public InputBox CreateInputBox()
    {
        InputBox box = Instantiate(InputBoxPrefab).GetComponent<InputBox>();
        return box;
    }

    public void OpenMenu()
    {
        Menu.UpdateButtons();
        Menu.UIPanel.SetActive(true);
        menuOpen = true;
    }

    public void CloseMenu()
    {
        Menu.UIPanel.SetActive(false);
        menuOpen = false;
        if (currentScene == StrategyMode)
        {
            StrategyMode.ButtonCallback(70);
            StrategyMode.StatusBarUI.Build.gameObject.SetActive(Config.BuildConfig.BuildingSystemEnabled);
        }
    }

    public void CloseStatsScreen()
    {
        StatScreen.AutoClose = false;
        StatScreen.gameObject.SetActive(false);
        SwitchToStrategyMode();
    }

    public void CenterCameraOnTile(int x, int y)
    {
        Camera.transform.position = new Vector3(x, y, Camera.transform.position.z);
    }

    public void SlideCameraToTile(int x, int y)
    {
        oldCameraPosition = Camera.transform.position;
        newCameraPosition = new Vector3(x, y, Camera.transform.position.z);
        float distance = Vector3.Distance(oldCameraPosition, newCameraPosition);
        cameraTransitionTime = .2f + (0.1f * Mathf.Sqrt(distance));
        cameraCurrentTransitionTime = 0;
        CameraTransitioning = true;
    }

    public void SwitchToMainMenu()
    {
        if (CompanyBattle != null) { CompanyBattle.ExitToCheckpoint(); return; }
        PureTactical = false;
        CurrentScene.CleanUp();
        CurrentScene = Start_Mode;
        Start_Mode.ReturnToStart();
        State.GameManager.StrategyMode.CleanUpLingeringWindows();
    }

    private void Update()
    {
        if (DeveloperBattleLab.IsOpen) return; // Lab advances only through explicit developer controls.
        if (ActiveInput || CompanySaveMenu.IsVisible)
            return;
        if (Menu.HelpScreen.activeSelf)
        {
            Menu.HelpUI.ReceiveInput();
            return;
        }
        if (remainingCameraTime > 0)
            remainingCameraTime -= Time.deltaTime;
        if (remainingCameraTime <= 0)
            CornerCameraView.gameObject.SetActive(false);
        if (CameraTransitioning)
            UpdateSlidingCamera();
        if (menuOpen)
            Menu.UpdateDisplay();
        else if (CurrentScene != null)
            CurrentScene.ReceiveInput();
        if (State.TutorialMode)
            TutorialScript.CheckStatus();
    }

    void UpdateSlidingCamera()
    {
        cameraCurrentTransitionTime += Time.deltaTime;
        Camera.transform.position = Vector3.Lerp(oldCameraPosition, newCameraPosition, cameraCurrentTransitionTime / cameraTransitionTime);
        if (cameraCurrentTransitionTime > cameraTransitionTime)
        {
            CameraTransitioning = false;
        }

    }

    internal void CameraCall(Vector3 location)
    {
        if ((Config.TacticalCenterCameraOnAction && CurrentScene == TacticalMode) || (Config.StrategicCenterCameraOnAction && CurrentScene == StrategyMode))
        {
            Camera.transform.position = location;
        }
        else if ((Config.TacticalCameraActionPanel && CurrentScene == TacticalMode) || (Config.StrategicCameraActionPanel && CurrentScene == StrategyMode))
        {
            CornerCameraView.gameObject.SetActive(true);
            PipCamera.SetLocation(location, 5);
            remainingCameraTime = 1;
        }
    }

    internal void CameraCall(Vec2i location) => CameraCall(new Vector3(location.x, location.y, 0));

    public void SwitchToStrategyMode(bool initialLoad = false)
    {
        if (PureTactical)
        {
            CurrentScene.CleanUp();
            PureTactical = false;
            SwitchToMainMenu();
            return;
        }
        CreateMessageBox(State.CampaignUnavailableMessage);
    }

    public void SwitchToMapEditor()
    {
        Menu.ShowRetiredToolMessage();
    }

    internal void ActivateCompanyBattle(StrategicTileType terrain, Army attacker, Army defender, int rating)
    {
        PureTactical = false; CurrentScene = TacticalMode;
        TacticalMode.AttackerName = "Your company"; TacticalMode.DefenderName = "Enemy forces";
        TacticalMode.Begin(terrain, null, attacker, defender, TacticalAIType.None, TacticalAIType.Full);
        CenterCameraOnTile(Config.TacticalSizeX / 2, Config.TacticalSizeY / 2);
        CameraController.SetZoom(Config.TacticalSizeX * .5f);
    }
    internal void ResumeCompanyBattle(TacticalData data)
    {
        CurrentScene = TacticalMode; PureTactical = false;
        TacticalMode.LoadData(data);
    }
    internal void ReturnFromCompanyBattle()
    {
        CurrentScene.CleanUp(); CurrentScene = Start_Mode; PureTactical = false; CloseMenu();
    }

    public void ActivatePureTacticalMode(StrategicTileType tiletype, Village village, Army invader, Army defender, TacticalAIType attackerAI, TacticalAIType defenderAI)
    {
        PureTactical = true;
        CurrentScene = TacticalMode;
        TacticalMode.Begin(tiletype, village, invader, defender, attackerAI, defenderAI);
        if (currentScene == TacticalMode)
        {
            CenterCameraOnTile((int)(Config.TacticalSizeX * .5f), (int)(Config.TacticalSizeY * .5f));
            CameraController.SetZoom(Config.TacticalSizeX * .5f);
        }
    }

    public void ActivateTacticalWithDelay(StrategicTileType tiletype, Village village, Army invader, Army defender)
    {
        CreateMessageBox(State.CampaignUnavailableMessage);
    }


    public void ActivateQueuedTacticalMode(PreviewSkip skipType)
    {
        CreateMessageBox(State.CampaignUnavailableMessage);
    }


    public void ActivateTacticalMode(StrategicTileType tiletype, Village village, Army invader, Army defender, TacticalBattleOverride tacticalBattleOverride = TacticalBattleOverride.Ignore)
    {
        CreateMessageBox(State.CampaignUnavailableMessage);
    }

    public void SwitchToTacticalOnLoadedGame()
    {
        CreateMessageBox(State.LegacyLoadUnavailableMessage);
    }

    public void ActivateRecruitMode(Village village, Empire empire, Recruit_Mode.ActivatingEmpire activatingEmpire = Recruit_Mode.ActivatingEmpire.Self)
    {
        Recruit_Mode.Begin(village, empire, activatingEmpire);
        CurrentScene = Recruit_Mode;
    }

    public void ActivateRecruitMode(Empire actingEmpire, Army army, Recruit_Mode.ActivatingEmpire activatingEmpire = Recruit_Mode.ActivatingEmpire.Self)
    {
        Recruit_Mode.BeginWithoutVillage(actingEmpire, army, activatingEmpire);
        CurrentScene = Recruit_Mode;
    }

    public void ActivateRecruitMode(MercenaryHouse house, Empire actingEmpire, Army army, Recruit_Mode.ActivatingEmpire activatingEmpire = Recruit_Mode.ActivatingEmpire.Self)
    {
        Recruit_Mode.BeginWithMercenaries(house, actingEmpire, army, activatingEmpire);
        CurrentScene = Recruit_Mode;
    }

    public void ActivateEndSceneWin(int side)
    {
        var winners = State.World.MainEmpires.Where(s => s.IsAlly(State.World.GetEmpireOfSide(side)));
        string winner = "";
        bool first = true;
        foreach (Empire emp in winners)
        {
            if (first)
                winner += $"{emp.Name}";
            else
                winner += $", {emp.Name}";
            first = false;
        }

        EndScene.Win(winner);
        CurrentScene = EndScene;
    }

    public void ActivateEndSceneLose()
    {
        EndScene.Lose(State.World.ActingEmpire.Race);
        CurrentScene = EndScene;
    }

    public void OpenSaveLoadMenu() => Menu.OpenSaveLoadMenu();

    public void AskQuickLoad()
    {
        CompanySaveMenu.Show(Start_Mode.CompanySaveView);
    }

    public void ClearPureTactical() => PureTactical = false;
}

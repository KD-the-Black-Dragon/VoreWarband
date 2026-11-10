// Modified for the VoreWar mercenary-company conversion.
// 2026-11-08: Block campaign battle launch when more than eight company members are active (balance follow-up item 2).
// 2026-11-08: Remove the overlapping setup-page Load Game action; main-menu Load Game remains available (40E UI follow-up).
// 2026-11-08: Add company naming, retirement access and durable high-score entry points (spec 40E).
// 2026-11-06: Keep action rollback in memory and remove action-level autosave work.
// Modified for the VoreWar mercenary-company conversion.
// 2026-11-06: Preview/resume explored routes and animate native movement at 0.1 seconds per tile (spec 40B).
// 2026-11-05: Show upkeep bills, debt and deadline and resume queued insolvency warnings (spec 38 phase B).
// 2026-11-05: Render village fields with the retained strategic farm sprites (spec 36).
// 2026-11-05: Overlay connected roads on the existing virtualized, fog-aware terrain map (spec 35).
// See CHANGES.md and License.txt for GPL-3.0 terms.
// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-01: Runtime company-map preview, daily travel and new-checkpoint navigation (spec 06).
// 2026-11-01: Connect curated leader/company economy, wage decisions and retained terrain artwork (specs 07–08).
// 2026-11-01: Connect contract targets, final deployment, saved result/revival routing and campaign records (specs 09–10).
// 2026-11-01: Use individual species labels in campaign setup (09–10 feedback).
// 2026-11-01: Connect explicit chosen-day rest, recovery/sparring stages and town treatment without advancing time on load or menus (spec 11).
// 2026-11-01: Acknowledge departures separately before any simultaneous wage warning, without advancing the grace day (spec 11 feedback).
// 2026-11-02: Make help and failed-save retry reachable on all company pages and share resume/Ironman validation (spec 12).
// 2026-11-02: Show the existing weekly payday countdown on the world map (spec 12 feedback).
// 2026-11-02: Commit travel-triggered road events before presenting saved choices or forced deployment (spec 13).
// 2026-11-02: Add persistent campaign/town navigation, a larger retained-art map and grouped company status with full-action rest controls (spec 14).
// 2026-11-02: Center a bounded interface and virtualized draggable map, move contracts/records into navigation and open normal Options (spec 14 feedback).
// 2026-11-02: Expose the existing company checkpoint browser to the main-menu load button (spec 15C).
// 2026-11-03: Connect unified saves and exact battle resumption to campaign screens (spec 19).
// 2026-11-03: Retire Continue/map preview setup entries and compact the remaining creation/load choices (spec 20).
// 2026-11-03: Route campaign chrome through shared blue/gold surfaces while leaving terrain tiles intact (spec 28).
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class CompanyCampaignView : MonoBehaviour
{
    StartMode owner;
    CompanyManagementUI management;
    CompanyMissionsUI missions;
    internal CompanyRecoveryUI Recovery { get; private set; }
    Text contractList, financesText, conditionText, calendarText, townDetails;
    InputField companyNameField;
    CompanyCampaign returnedCampaign;
    CompanyBattleSession returningSession;
    string returningError;
    internal static CompanyCampaignView ActiveView;
    internal bool HasActiveCompany => state != null && !state.Preview && IsOpen && !setupPage.activeSelf && !mainMenuHistory;
    internal string SaveDirectory { get { return saveDirectory; } }
    internal void PrepareSaveBrowser(StartMode start)
    { owner = start; if (root == null) { Build(); root.SetActive(false); } ActiveView = this; }
    internal void ReturnToMainMenu()
    { Close(); State.GameManager.Start_Mode.ReturnToStart(); }

    Text stageBanner;
    internal CompanyCampaign Campaign { get { return state; } }
    GameObject root, frame, mapPage, setupPage, townPage, companyPage, navigation, townNavigation, statusPanel;
    CompanyCampaign state;
    readonly CompanyClock clock = new CompanyClock();
    Text heading, message, townHeading, companyText;
    Text[] tileLabels;
    Image[] townIcons;
    CompanyRoadGraphic[] roadGraphics;
    CompanyRouteGraphic[] routeGraphics;
    Text[] routeNumbers;
    int[] routePath = new int[0], routeIndex, routeRemaining;
    RectTransform companyMarker;
    Coroutine travelRoutine;
    bool travelling;
    Vector2? animatedCell;
    internal bool IsTravelling => travelling;
    Outline[] tileOutlines;
    Button[] tiles;
    const int TileSize = 42, MapColumns = 29, MapRows = 12;
    int[] tileIndices;
    CompanyMapScroll mapScroll;
    RectTransform mapContent, mapViewport;
    string mapCompanyId;
    int mapWidth, mapHeight, centeredTile = -1, firstMapX = int.MinValue, firstMapY = int.MinValue;
    bool optionsOpen, optionsTownServices, optionsCampaign;
    bool mainMenuHistory;
    internal bool HasCampaignOptions { get { return optionsOpen && optionsCampaign && CanNavigate && !state.Preview && !mainMenuHistory; } }
    internal bool LeaderRevivalEnabled { get { return state.LeaderRevival; } }
    internal void SetLeaderRevival(bool enabled)
    {
        if (!HasCampaignOptions) throw new InvalidOperationException("No active company options are available.");
        if (state.LeaderRevival != enabled) Mutate(() => state.LeaderRevival = enabled);
    }
    Toggle ironman;
    Button saveButton, townButton, mapButton, companyButton, contractsButton, recordsButton, retirementButton, dayButton, restOneButton, restChosenButton, resumeRestButton, cancelRestButton;
    string saveDirectory;
    public bool IsOpen { get { return root != null && root.activeSelf; } }

    public void Open(StartMode start)
    {
        owner = start; ActiveView = this; mainMenuHistory = false;
        if (root == null) Build();
        owner.UI.SetActive(false);
        root.SetActive(true);
        ShowSetup();
    }
    internal void OpenHistory(StartMode start)
    {
        owner = start; ActiveView = this; mainMenuHistory = true;
        if (root == null) Build();
        owner.UI.SetActive(false); root.SetActive(true);
        missions.HighScores(0, true);
    }
    internal void OpenSavedGames(StartMode start)
    {
        PrepareSaveBrowser(start);
        CompanySaveMenu.Show(this);
    }
    public void Close()
    {
        bool moving = travelling;
        StopTravel();
        if (moving && state != null) RefreshState("Travel paused.");
        if (root != null) root.SetActive(false);
        if (owner != null) owner.UI.SetActive(true);
    }
    internal GameObject Panel(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        return go;
    }
    internal void Place(GameObject go, float x, float y, float w, float h)
    {
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
    }
    internal Text Label(Transform parent, string words, float x, float y, float w, float h, int size = 20)
    {
        var go = Panel("Label", parent); Place(go, x, y, w, h);
        var text = go.AddComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.text = words; text.color = UITheme.Foreground; text.fontSize = size; text.raycastTarget = false;
        return text;
    }
    internal Button Action(Transform parent, string words, float x, float y, float w, Action callback)
    {
        var go = Panel(words, parent); Place(go, x, y, w, 38);
        bool chrome = !string.IsNullOrEmpty(words); // Map tile buttons must retain their artwork and fog rendering.
        var image = chrome ? UITheme.Surface(go, UITheme.Control, true) : go.AddComponent<Image>();
        var button = go.AddComponent<Button>(); button.targetGraphic = image;
        if (chrome) UITheme.StyleSelectable(button);
        button.onClick.AddListener(() => { if (!travelling || !chrome) Guard(callback); });
        var label = Label(go.transform, words, 3, 3, w - 6, 32, 17); label.alignment = TextAnchor.MiddleCenter;
        return button;
    }
    internal GameObject Box(Transform parent, string name, float x, float y, float w, float h, Color color)
    {
        var go = Panel(name, parent); Place(go, x, y, w, h);
        UITheme.Surface(go, color, true).raycastTarget = false;
        return go;
    }
    bool CanNavigate
    {
        get { return state != null && !travelling && !state.Ended && !state.InBattle &&
            (state.Battle == null || state.Battle.Phase == CompanyBattlePhase.Returned) && !CompanyRoadEvents.AwaitingChoice(state); }
    }
    void RefreshNavigation(bool townServices = false, bool campaign = true)
    {
        if (navigation == null) return;
        bool visible = campaign && state != null && !setupPage.activeSelf;
        navigation.SetActive(visible); statusPanel.SetActive(visible);
        bool available = CanNavigate;
        mapButton.interactable = companyButton.interactable = contractsButton.interactable = recordsButton.interactable = available;
        townButton.interactable = available && !state.Busy && state.Towns.Any(t => t.Tile == state.Position) && state.Actions >= state.TownEntryCost;
        if (retirementButton != null) retirementButton.interactable = available && !state.Busy && state.ElapsedDays >= 14 && state.Towns.Any(t => t.Tile == state.Position);
        townNavigation.SetActive(visible && townServices && !state.Preview);
        if (!visible) return;
        var roster = state.Members.Select(m => state.Characters.Single(c => c.Id == m.CharacterId)).ToArray();
        long hp = roster.Sum(c => (long)Math.Max(0, c.Unit.Health));
        long max = roster.Sum(c => (long)c.Unit.MaxHealth);
        int wounds = roster.Sum(c => c.Unit.Injuries.Count(CompanyInjuries.Temporary));
        int reserves = state.Members.Count(m => m.Reserve);
        financesText.text = "Gold: " + state.Gold + " | Wage debt: " + roster.Sum(c => (long)c.Debt) +
            "\nNext bills: " + CompanyEconomy.WeeklyWages(state) + " wages + " + CompanyEconomy.Upkeep(state) + " upkeep" +
            "\nUpkeep debt: " + state.UpkeepDebt + (state.UpkeepDebt > 0 ? " | Due in " + Math.Max(0, state.UpkeepDueDay - state.ElapsedDays) + " days" : "");
        conditionText.text = "Members: " + roster.Length + " (" + (roster.Length - reserves) + " active, " + reserves + " reserve)" +
            "\nHealth: " + hp + "/" + max + " (" + (max == 0 ? 0 : hp * 100 / max) + "%)" + "\nCurrent injuries: " + wounds;
        int days = Math.Max(0, state.NextPayday - state.ElapsedDays);
        calendarText.text = "Day " + (state.ElapsedDays + 1) + "    Actions: " + state.Actions + "/" + state.DailyActions +
            "\n" + (state.Preview ? "Map preview" : days == 0 ? "Payday due" : "Payday in " + days + (days == 1 ? " day" : " days")) + (state.Ironman ? "\nIronman" : "");
    }
    void OpenOptions()
    {
        optionsTownServices = townNavigation.activeSelf; optionsCampaign = navigation.activeSelf; optionsOpen = true;
        root.SetActive(false);
        try { State.GameManager.Menu.OpenOptions(); }
        catch { optionsOpen = false; root.SetActive(true); throw; }
    }
    void BindMap()
    {
        if (mapCompanyId != state.Id || mapWidth != state.Width || mapHeight != state.Height)
        {
            mapCompanyId = state.Id; mapWidth = state.Width; mapHeight = state.Height;
            mapContent.sizeDelta = new Vector2(mapWidth * TileSize, mapHeight * TileSize);
            firstMapX = firstMapY = int.MinValue; centeredTile = -1;
        }
        BuildRouteCache();
        if (!travelling && centeredTile != state.Position) CenterMap();
        PositionCompanyMarker();
        RefreshMapTiles(true);
    }
    Vector2 CenteredMapPosition(int tile)
    {
        float x = mapContent.rect.width / 2 - (tile % state.Width + .5f) * TileSize;
        float y = (tile / state.Width + .5f) * TileSize - mapContent.rect.height / 2;
        float limitX = Mathf.Max(0, (mapContent.rect.width - mapViewport.rect.width) / 2);
        float limitY = Mathf.Max(0, (mapContent.rect.height - mapViewport.rect.height) / 2);
        return new Vector2(Mathf.Clamp(x, -limitX, limitX), Mathf.Clamp(y, -limitY, limitY));
    }
    void CenterMap()
    {
        if (state == null) return;
        mapScroll.StopMovement();
        mapContent.anchoredPosition = CenteredMapPosition(state.Position);
        centeredTile = state.Position; RefreshMapTiles(true);
    }
    void BuildRouteCache()
    {
        CompanyTravel.ClearArrival(state);
        routePath = state.TravelDestination.HasValue ? CompanyTravel.Path(state, state.TravelDestination.Value) : new int[0];
        routeIndex = Enumerable.Repeat(-1, state.Terrain.Length).ToArray();
        routeRemaining = new int[routePath.Length];
        int remaining = state.Actions;
        for (int i = 0; i < routePath.Length; i++)
        {
            if (i > 0) remaining -= state.MoveCost(routePath[i]);
            routeIndex[routePath[i]] = i; routeRemaining[i] = remaining;
        }
    }
    Vector2 Cell(int tile) { return new Vector2(tile % state.Width, tile / state.Width); }
    void PositionCompanyMarker()
    {
        if (state == null || companyMarker == null) return;
        Vector2 cell = animatedCell ?? Cell(state.Position);
        companyMarker.anchoredPosition = new Vector2(cell.x * TileSize, -cell.y * TileSize);
    }
    void RefreshMapTiles(bool force = false)
    {
        if (state == null || !mapPage.activeInHierarchy || mapContent == null) return;
        var corners = new Vector3[4]; mapViewport.GetWorldCorners(corners);
        Vector3 topLeft = mapContent.InverseTransformPoint(corners[1]);
        int startX = Mathf.FloorToInt((topLeft.x + mapContent.rect.width / 2) / TileSize) - 1;
        int startY = Mathf.FloorToInt((mapContent.rect.height / 2 - topLeft.y) / TileSize) - 1;
        if (!force && startX == firstMapX && startY == firstMapY) return;
        firstMapX = startX; firstMapY = startY;
        var sprites = State.GameManager.StrategyMode.TileSprites;
        var towns = State.GameManager.StrategyMode.VillageSprites;
        int[] terrainTypes = { (int)StrategicTileType.grass, (int)StrategicTileType.forest, (int)StrategicTileType.hills, (int)StrategicTileType.desert };
        for (int i = 0; i < tiles.Length; i++)
        {
            int x = startX + i % MapColumns, y = startY + i / MapColumns;
            bool inside = x >= 0 && y >= 0 && x < state.Width && y < state.Height;
            tiles[i].gameObject.SetActive(inside);
            tileIndices[i] = inside ? y * state.Width + x : -1;
            if (!inside) continue;
            int tile = tileIndices[i]; bool seen = state.Discovered[tile];
            Place(tiles[i].gameObject, x * TileSize, y * TileSize, TileSize, TileSize);
            int distance = Math.Abs(x - state.Position % state.Width) + Math.Abs(y - state.Position / state.Width);
            tiles[i].interactable = !state.Busy;
            var terrainType = state.Farms != null && state.Farms[tile] ? StrategicTileType.field : (StrategicTileType)terrainTypes[state.Terrain[tile]];
            int art = StrategicTileInfo.GetTileType(terrainType, x, y);
            var image = tiles[i].GetComponent<Image>();
            image.sprite = seen && sprites != null && art >= 0 && art < sprites.Length ? sprites[art] : null;
            image.color = seen ? Color.white : new Color(.08f, .1f, .12f);
            roadGraphics[i].SetConnections(seen ? CompanyWorldGeneration.RoadMask(state, tile) : 0);
            bool town = seen && state.Towns.Any(t => t.Tile == tile);
            townIcons[i].sprite = towns != null && towns.Length > 6 ? towns[6] : null;
            townIcons[i].gameObject.SetActive(town && townIcons[i].sprite != null);
            bool target = state.Contracts.Any(c => c.Status == CompanyContractStatus.Active && c.Tile == tile);
            tileLabels[i].text = target ? "!" : town && townIcons[i].sprite == null ? "T" : "";
            tileLabels[i].color = tile == state.Position ? new Color(.55f, .9f, 1) : target ? new Color(1, .55f, .35f) : new Color(1, .87f, .55f);
            tileOutlines[i].enabled = (tiles[i].interactable && distance == 1 && state.Actions >= state.MoveCost(tile)) || tile == state.Position || state.TravelDestination == tile;
            tileOutlines[i].effectColor = tile == state.Position || state.TravelDestination == tile ? new Color(1, .8f, .35f) : new Color(.55f, .9f, .65f);
            int route = routeIndex == null ? -1 : routeIndex[tile];
            int from = route > 0 ? CompanyRouteGraphic.Direction(tile, routePath[route - 1], state.Width) : 0;
            int to = route >= 0 && route + 1 < routePath.Length ? CompanyRouteGraphic.Direction(tile, routePath[route + 1], state.Width) : 0;
            bool over = route >= 0 && routeRemaining[route] < 0;
            bool nextOver = route >= 0 && route + 1 < routePath.Length && routeRemaining[route + 1] < 0;
            routeGraphics[i].SetRoute(from, to, over, nextOver, route > 0 && route == routePath.Length - 1);
            routeNumbers[i].text = route >= 0 ? routeRemaining[route].ToString() : "";
            routeNumbers[i].color = over ? CompanyRouteGraphic.OverBudget : CompanyRouteGraphic.Affordable;
        }
    }
    void Guard(Action action)
    {
        try { action(); }
        catch (Exception ex) { message.text = "Could not complete this action: " + ex.Message + " Existing saves were preserved."; }
    }
    void Build()
    {
        root = Panel("Company campaign", transform);
        var canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
        var scaler = root.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 800); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        root.AddComponent<GraphicRaycaster>();
        var background = Panel("Background", root.transform); var rect = background.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        UITheme.Surface(background, UITheme.Background);
        frame = Panel("Centered campaign frame", root.transform);
        var frameRect = frame.GetComponent<RectTransform>(); frameRect.anchorMin = frameRect.anchorMax = frameRect.pivot = new Vector2(.5f, .5f);
        frameRect.sizeDelta = new Vector2(1280, 800); frameRect.anchoredPosition = Vector2.zero;
        Box(frame.transform, "Header", 0, 0, 1280, 72, UITheme.Panel);
        heading = Label(frame.transform, "Mercenary company", 80, 76, 1120, 28, 21);
        message = Label(frame.transform, "", 480, 741, 545, 55, 15);
        stageBanner = Label(frame.transform, "", 30, 640, 1220, 20, 15);
        Action(frame.transform, "Main menu", 80, 746, 130, Close);
        Action(frame.transform, "Help", 222, 746, 100, () => missions.Help(0, setupPage.activeSelf || state == null, management.IsCreatingLeader));
        Action(frame.transform, "Options", 334, 746, 120, OpenOptions);
        setupPage = Panel("Setup", frame.transform);
        Box(setupPage.transform, "Setup surface", 20, 104, 1240, 536, UITheme.Panel);
        Label(setupPage.transform, "Create your leader, recruit and equip a small company, then choose contracts and fight.\nNumeric tuning is provisional.", 30, 105, 1150, 90);
        var toggleGo = Panel("Ironman", setupPage.transform); Place(toggleGo, 30, 220, 25, 25);
        var check = UITheme.Surface(toggleGo, UITheme.Control, true);
        ironman = toggleGo.AddComponent<Toggle>(); ironman.targetGraphic = check;
        var mark = Panel("Check", toggleGo.transform); Place(mark, 5, 5, 15, 15);
        ironman.graphic = mark.AddComponent<Image>(); ironman.isOn = false;
        UITheme.StyleCheckmark(ironman); UITheme.StyleSelectable(ironman);
        Label(setupPage.transform, "Ironman (locked at creation; autosaves only; recovery backups)", 70, 216, 1100, 40);
        Label(setupPage.transform, "Company name", 30, 272, 220, 30, 17);
        var companyNameGo = Panel("Company name", setupPage.transform); Place(companyNameGo, 30, 306, 440, 42);
        UITheme.Surface(companyNameGo, UITheme.Control, true);
        companyNameField = companyNameGo.AddComponent<InputField>(); UITheme.StyleSelectable(companyNameField);
        companyNameField.characterLimit = 64; companyNameField.text = "";
        companyNameField.textComponent = Label(companyNameGo.transform, "", 8, 2, 424, 36, 18);
        var companyPlaceholder = Label(companyNameGo.transform, "Enter a company name", 8, 2, 424, 36, 18);
        companyPlaceholder.color = UITheme.Muted; companyNameField.placeholder = companyPlaceholder;
        Action(setupPage.transform, "Create company", 30, 370, 250, () => management.StartCreator(ironman.isOn, companyNameField.text));
        mapPage = Panel("World map", frame.transform);
        var viewport = Panel("Map viewport", mapPage.transform); Place(viewport, 80, 156, 1120, 420);
        UITheme.Surface(viewport, UITheme.Background); viewport.AddComponent<RectMask2D>();
        mapViewport = viewport.GetComponent<RectTransform>(); mapScroll = viewport.AddComponent<CompanyMapScroll>();
        mapScroll.viewport = mapViewport; mapScroll.horizontal = mapScroll.vertical = true;
        mapScroll.movementType = ScrollRect.MovementType.Clamped; mapScroll.inertia = false; mapScroll.scrollSensitivity = 42;
        var content = Panel("Map tiles", viewport.transform); mapContent = content.GetComponent<RectTransform>();
        mapContent.anchorMin = mapContent.anchorMax = mapContent.pivot = new Vector2(.5f, .5f); mapContent.sizeDelta = Vector2.zero;
        mapScroll.content = mapContent;
        mapScroll.onValueChanged.AddListener(_ => RefreshMapTiles());
        int tileCount = MapColumns * MapRows;
        roadGraphics = new CompanyRoadGraphic[tileCount];
        routeGraphics = new CompanyRouteGraphic[tileCount]; routeNumbers = new Text[tileCount];
        tileLabels = new Text[tileCount]; tiles = new Button[tileCount]; townIcons = new Image[tileCount]; tileOutlines = new Outline[tileCount]; tileIndices = new int[tileCount];
        for (int i = 0; i < tiles.Length; i++)
        {
            int index = i; tileIndices[i] = -1;
            tiles[i] = Action(content.transform, "", 0, 0, TileSize, () => { if (!mapScroll.SuppressTileClick && tileIndices[index] >= 0) Move(tileIndices[index]); });
            Place(tiles[i].gameObject, 0, 0, TileSize, TileSize); tiles[i].transition = Selectable.Transition.None;
            tileOutlines[i] = tiles[i].gameObject.AddComponent<Outline>(); tileOutlines[i].effectDistance = new Vector2(1, -1);
            var road = Panel("Road", tiles[i].transform); Place(road, 0, 0, TileSize, TileSize);
            roadGraphics[i] = road.AddComponent<CompanyRoadGraphic>(); roadGraphics[i].raycastTarget = false;
            var townIcon = Panel("Town", tiles[i].transform); Place(townIcon, 5, 3, 32, 36);
            townIcons[i] = townIcon.AddComponent<Image>(); townIcons[i].preserveAspect = true; townIcons[i].raycastTarget = false;
            tileLabels[i] = tiles[i].GetComponentInChildren<Text>(); tileLabels[i].transform.SetAsLastSibling();
            Place(tileLabels[i].gameObject, 0, 0, TileSize, TileSize); tileLabels[i].fontSize = 24;
            tileLabels[i].gameObject.AddComponent<Outline>().effectColor = Color.black;
            var route = Panel("Travel arrow", tiles[i].transform); Place(route, 0, 0, TileSize, TileSize);
            routeGraphics[i] = route.AddComponent<CompanyRouteGraphic>(); routeGraphics[i].raycastTarget = false;
            routeNumbers[i] = Label(tiles[i].transform, "", 1, 1, TileSize - 3, 19, 16);
            routeNumbers[i].fontStyle = FontStyle.Bold; routeNumbers[i].alignment = TextAnchor.UpperRight;
            routeNumbers[i].resizeTextForBestFit = true; routeNumbers[i].resizeTextMinSize = 10; routeNumbers[i].resizeTextMaxSize = 16;
            routeNumbers[i].gameObject.AddComponent<Outline>().effectColor = Color.black;
        }
        var marker = Label(content.transform, "C", 0, 0, TileSize, TileSize, 24);
        marker.color = new Color(.55f, .9f, 1); marker.alignment = TextAnchor.MiddleCenter;
        marker.gameObject.name = "Company marker"; marker.gameObject.AddComponent<Outline>().effectColor = Color.black;
        companyMarker = marker.rectTransform;
        var mapFrame = UITheme.Node("Map border", viewport.transform);
        UITheme.Stretch(mapFrame.GetComponent<RectTransform>()); UITheme.Frame(mapFrame);
        dayButton = Action(mapPage.transform, "End turn", 80, 108, 130, EndDay);
        restOneButton = Action(mapPage.transform, "Rest 1 day", 222, 108, 130, () => Rest(1));
        var restInput = Panel("Rest days", mapPage.transform); Place(restInput, 364, 108, 60, 38);
        UITheme.Surface(restInput, UITheme.Control, true);
        var restDays = restInput.AddComponent<InputField>(); UITheme.StyleSelectable(restDays); restDays.contentType = InputField.ContentType.IntegerNumber;
        restDays.characterLimit = 3; restDays.textComponent = Label(restInput.transform, "1", 8, 2, 44, 34, 18); restDays.text = "1";
        restChosenButton = Action(mapPage.transform, "Rest chosen days", 436, 108, 150, () =>
        {
            int days;
            if (!int.TryParse(restDays.text, out days) || days < 1 || days > 365) throw new InvalidOperationException("Choose 1–365 rest days.");
            Rest(days);
        });
        resumeRestButton = Action(mapPage.transform, "Resume rest", 598, 108, 170, ResumeRest);
        cancelRestButton = Action(mapPage.transform, "Cancel rest", 780, 108, 120, () => { state.RestDaysRemaining = 0; RefreshState("Remaining rest cancelled."); ShowMap(); });
        Box(mapPage.transform, "Active contracts", 80, 582, 1120, 75, UITheme.Panel);
        contractList = Label(mapPage.transform, "", 96, 588, 1088, 64, 16);
        saveButton = Action(mapPage.transform, "Save / Load", 912, 108, 140, () => CompanySaveMenu.Show(this));
        Action(mapPage.transform, "Center company", 1064, 108, 136, CenterMap);
        townPage = Panel("Town", frame.transform);
        Box(townPage.transform, "Town surface", 238, 88, 1022, 552, UITheme.Panel);
        townHeading = Label(townPage.transform, "", 250, 110, 950, 65, 32);
        townDetails = Label(townPage.transform, "", 250, 210, 900, 250, 22);
        companyPage = Panel("Company", frame.transform);
        companyText = Label(companyPage.transform, "Map-only preview: no company members.", 30, 120, 1150, 320);
        management = new CompanyManagementUI(this, frame.transform);
        missions = new CompanyMissionsUI(this, management);
        Recovery = new CompanyRecoveryUI(this, management);
        clock.Register(new CompanyWages()); clock.Register(new CompanyRecruitRefresh());
        clock.Register(new CompanyContractDeadlines()); clock.Register(new CompanyDailyRecovery()); clock.Register(new CompanySparring());
        navigation = Panel("Campaign navigation", frame.transform);
        contractsButton = Action(navigation.transform, "Contracts", 80, 20, 145, () => missions.Contracts());
        companyButton = Action(navigation.transform, "Company", 390, 20, 155, ShowCompany);
        mapButton = Action(navigation.transform, "Map", 562.5f, 20, 155, ShowMap);
        townButton = Action(navigation.transform, "Town", 735, 20, 155, EnterTown);
        recordsButton = Action(navigation.transform, "Records", 1055, 20, 145, () => missions.Record());
        townNavigation = Panel("Town services", frame.transform);
        Box(townNavigation.transform, "Service column", 20, 140, 205, 485, UITheme.Panel);
        Label(townNavigation.transform, "TOWN SERVICES", 30, 153, 185, 30, 17);
        Action(townNavigation.transform, "Recruitment", 30, 200, 185, () => management.Recruit());
        Action(townNavigation.transform, "Merchant", 30, 258, 185, () => management.Shop());
        Action(townNavigation.transform, "Contracts", 30, 316, 185, () => missions.Contracts(true));
        Action(townNavigation.transform, "Treatment", 30, 374, 185, () => Recovery.Treatment());
        retirementButton = Action(townNavigation.transform, "Retire company", 30, 432, 185, () => missions.RetirementPreview());
        statusPanel = Panel("Company status", frame.transform);
        Box(statusPanel.transform, "Status background", 80, 0, 1120, 72, UITheme.Panel);
        financesText = Label(statusPanel.transform, "", 96, 9, 330, 60, 17);
        conditionText = Label(statusPanel.transform, "", 454, 5, 420, 66, 17);
        calendarText = Label(statusPanel.transform, "", 900, 9, 285, 60, 17);
        var footerRect = statusPanel.GetComponent<RectTransform>(); footerRect.anchorMin = Vector2.zero; footerRect.anchorMax = new Vector2(1, 0);
        footerRect.pivot = new Vector2(0, 1); footerRect.sizeDelta = new Vector2(0, 72); footerRect.anchoredPosition = new Vector2(0, 138);
        navigation.SetActive(false); townNavigation.SetActive(false); statusPanel.SetActive(false);
    }
    void Pages(GameObject page)
    {
        if (management != null) management.Page.SetActive(page == management.Page);
        setupPage.SetActive(page == setupPage); mapPage.SetActive(page == mapPage);
        townPage.SetActive(page == townPage); companyPage.SetActive(page == companyPage);
        heading.gameObject.SetActive(page == setupPage || page == mapPage);
        RefreshNavigation(page == townPage);
    }
    internal void ShowSetup() { Pages(setupPage); heading.text = "Mercenary campaign"; message.text = ""; stageBanner.text = ""; }

    void StopTravel()
    {
        if (travelRoutine != null) StopCoroutine(travelRoutine);
        travelRoutine = null; travelling = false; animatedCell = null;
        PositionCompanyMarker();
    }
    void OnDisable() { StopTravel(); }
    void Move(int tile)
    {
        if (state.Busy || tile < 0 || tile >= state.Terrain.Length) return;
        if (travelling && state.TravelDestination == tile) return;
        bool paused = travelling;
        StopTravel();
        if (paused) RefreshState("Travel paused.");
        if (tile == state.Position) { state.TravelDestination = null; ShowMap(); return; }
        bool adjacent = CompanyContracts.Distance(state, tile, state.Position) == 1;
        bool confirmed = state.TravelDestination == tile;
        if (!adjacent && !confirmed)
        {
            if (!CompanyTravel.Select(state, tile)) message.text = "No route through explored terrain reaches that tile.";
            else message.text = "Click the same destination again to travel. Red shows steps beyond today's actions.";
            ShowMap();
            return;
        }
        if (adjacent && !confirmed)
            state.TravelDestination = state.Discovered[tile] ? (int?)tile : null;
        travelling = true; RefreshNavigation();
        travelRoutine = StartCoroutine(TravelSteps(adjacent ? (int?)tile : null));
    }
    CompanyTravelStep TryTravelStep(int tile, out string failure)
    {
        try { failure = null; return CompanyTravel.Advance(state, tile); }
        catch (Exception ex) { failure = "Could not travel. " + ex.Message; return CompanyTravelStep.Blocked; }
    }
    IEnumerator TravelSteps(int? singleTile)
    {
        bool changed = false;
        string failure = null;
        try
        {
            while (!state.Busy && root.activeInHierarchy && !CompanySaveMenu.IsVisible)
            {
                int next;
                if (singleTile.HasValue) next = singleTile.Value;
                else
                {
                    int[] path = state.TravelDestination.HasValue ? CompanyTravel.Path(state, state.TravelDestination.Value) : new int[0];
                    if (path.Length < 2)
                    {
                        if (state.TravelDestination.HasValue) failure = "No route through explored terrain reaches that destination.";
                        break;
                    }
                    next = path[1];
                }
                int previous = state.Position;
                var result = TryTravelStep(next, out failure);
                if (result == CompanyTravelStep.Blocked)
                {
                    if (failure == null) failure = "Not enough actions for the next tile. End the turn, then click the destination to resume.";
                    break;
                }
                changed = true;
                if (state.Position != previous)
                {
                    Vector2 cameraStart = mapContent.anchoredPosition, cameraEnd = CenteredMapPosition(state.Position);
                    Vector2 start = Cell(previous), end = Cell(state.Position);
                    float elapsed = 0;
                    animatedCell = start; PositionCompanyMarker();
                    while (elapsed < CompanyTravel.SecondsPerTile)
                    {
                        yield return null;
                        elapsed += Time.unscaledDeltaTime;
                        float fraction = Mathf.Clamp01(elapsed / CompanyTravel.SecondsPerTile);
                        animatedCell = Vector2.Lerp(start, end, fraction); PositionCompanyMarker();
                        mapContent.anchoredPosition = Vector2.Lerp(cameraStart, cameraEnd, fraction);
                        RefreshMapTiles();
                    }
                    animatedCell = null; centeredTile = state.Position; PositionCompanyMarker();
                    BuildRouteCache(); RefreshMapTiles(true); RefreshNavigation();
                }
                if (singleTile.HasValue || result != CompanyTravelStep.Moved) break;
            }
        }
        finally
        {
            travelling = false; travelRoutine = null; animatedCell = null;
            PositionCompanyMarker(); RefreshNavigation();
        }
        if (changed) RefreshState(failure ?? "Travel complete.");
        else { if (failure != null) message.text = failure; ShowMap(); }
    }
    internal void ShowMap()
    {
        if (state.Battle != null && state.Battle.Phase != CompanyBattlePhase.Returned || state.Ended) { missions.Resume(); return; }
        if (CompanyRoadEvents.AwaitingChoice(state)) { missions.RoadEvent(); return; }
        Pages(mapPage);
        stageBanner.text = ""; heading.text = "World map";
        BindMap();
        var active = state.Contracts.Where(c => c.Status == CompanyContractStatus.Active).ToArray();
        contractList.text = "Active contracts: " + active.Length + "    Reputation: " + state.Reputation + "\n" +
            string.Join("    |    ", active.Take(3).Select(c => "Rating " + c.Rating + " · " + c.Reward + " gold · day " + (c.Deadline + 1) +
                " · (" + c.Tile % state.Width + ", " + c.Tile / state.Width + ")").ToArray()) + (active.Length > 3 ? "    More in Contracts." : "");
        saveButton.interactable = !travelling;
        dayButton.interactable = !travelling;
        dayButton.GetComponentInChildren<Text>().text = !string.IsNullOrEmpty(state.PendingDecision) ? "Review notice" : state.UpdatingDay ? "Resume turn" : "End turn";
        restOneButton.interactable = restChosenButton.interactable = !travelling && state.CanStartRest;
        resumeRestButton.interactable = !travelling && state.CanStartRest && state.RestDaysRemaining > 0;
        cancelRestButton.interactable = !travelling && !state.Busy && state.RestDaysRemaining > 0;
        resumeRestButton.gameObject.SetActive(state.RestDaysRemaining > 0);
        cancelRestButton.gameObject.SetActive(state.RestDaysRemaining > 0);
        resumeRestButton.GetComponentInChildren<Text>().text = "Resume rest (" + state.RestDaysRemaining + ")";

    }
    void EnterTown()
    {
        var town = state.Towns.Find(t => t.Tile == state.Position);
        if (town == null || !state.Spend(state.TownEntryCost)) return;
        CompanyEconomy.Visit(state, town);
        if (!state.Preview) RefreshState("Town pool updated.");
        ShowCurrentTown();
    }
    internal void ShowCurrentTown()
    {
        var town = state == null ? null : state.Towns.FirstOrDefault(t => t.Tile == state.Position);
        if (town == null) { ShowMap(); return; }
        Pages(townPage); heading.text = "Town"; stageBanner.text = ""; townHeading.text = town.Name;
        townDetails.text = state.Preview ? "Map preview town" :
            "Recruitment: " + CompanyEconomy.Offers(state, town).Count() + " candidates available\n\n" +
            "Contracts: " + state.Contracts.Count(c => c.TownId == town.Id && c.Status == CompanyContractStatus.Offered) + " offers available\n\n" +
            "Visit the merchant for equipment, or arrange treatment for recovering injuries.";
    }
    void ShowCompany()
    {
        if (!state.Preview) { management.Company(); return; }
        Pages(companyPage);
        companyText.text = "Company preview\n\nNo leader or mercenaries have been created.\nCharacters will have stable identities shared by the roster, recruitment and battle views.\n\nTreasury: " + state.Gold + "\nFirst payday: after seven elapsed days\nElapsed days: " + state.ElapsedDays;
    }
    void RefreshState(string success)
    {
        message.text = success;
        if (state != null && state.Ended)
        {
            string error = CompanyHistoryStore.ArchiveEnding(state);
            if (!string.IsNullOrEmpty(error)) message.text = error;
        }
        if (state != null && mapPage.activeSelf) ShowMap();
    }
    void QueueAutosave()
    {
        try { CompanySaveWorker.Enqueue(state, saveDirectory); }
        catch (Exception error) { message.text = "Autosave failed: " + error.Message + " You can keep playing and save manually."; }
    }
    void EndDay() { if (!string.IsNullOrEmpty(state.PendingDecision)) management.Decision(); else AdvanceDay(false); }
    void AdvanceDay(bool resting)
    {
        if (state == null || state.Ended || state.InBattle || (state.Battle != null && state.Battle.Phase != CompanyBattlePhase.Returned) || !string.IsNullOrEmpty(state.PendingDecision)) return;
        clock.EndDay(state, resting);
        if (!state.Ended) QueueAutosave();
        RefreshState("Day advanced."); ShowMap();
        if (!string.IsNullOrEmpty(state.PendingDecision)) management.Decision();
    }
    internal void ShowManagement(bool townServices = false, bool campaign = true)
    {
        Pages(management.Page); RefreshNavigation(townServices, campaign);
        heading.text = townServices ? "Town" : "Mercenary company"; stageBanner.text = "";
    }
    internal void LoadCheckpoint(string path) { LoadCheckpoint(path, null); }
    internal void LoadCheckpoint(string path, CompanyCampaign loaded)
    {
        StopTravel();
        mainMenuHistory = false;
        var candidate = loaded ?? CompanyCampaignSave.LoadForResume(path);
        CompanySaveWorker.Cancel(candidate.Id);
        if (state != null) CompanySaveWorker.Cancel(state.Id);
        string directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (State.GameManager.CompanyBattle != null) State.GameManager.CompanyBattle.AbandonForLoad();
        else if (State.GameManager.CurrentScene == State.GameManager.TacticalMode) State.GameManager.ReturnFromCompanyBattle();
        state = candidate; saveDirectory = directory;
        ActiveView = this; root.SetActive(true); owner.UI.SetActive(false);
        if (state.InBattle)
        {
            root.SetActive(false);
            new CompanyBattleSession(this, state).Resume();
        }
        else missions.Resume();
        message.text = "Company loaded without advancing time.";
        if (!string.IsNullOrEmpty(state.PendingDecision)) management.Decision();
    }
    internal void Install(CompanyCampaign company)
    {
        StopTravel();
        company.Validate(); state = company; CompanyEconomy.Bind(state);
        saveDirectory = Path.Combine(CompanyCampaignSave.DefaultDirectory, state.Id);
        QueueAutosave(); RefreshState("Company created. Hire a couple of starters in this town."); ShowMap();
    }
    internal void LaunchBattle()
    {
        if (state.Battle == null || state.Battle.Phase != CompanyBattlePhase.Deployment) return;
        int activeCount = state.Members.Count(m => !m.Reserve || m.CharacterId == state.LeaderId);
        if (activeCount > CompanyBattleSession.MaximumCampaignBattleForceSize)
        {
            State.GameManager.CreateMessageBox("You currently have " + activeCount + " active mercenaries. You are only allowed to deploy " +
                CompanyBattleSession.MaximumCampaignBattleForceSize + ".");
            return;
        }
        Mutate(() =>
        {
            var battle = state.Battle;
            if (!battle.PositionCommitted)
            {
                state.Battle = null;
                if (!state.Move(battle.TargetTile)) throw new InvalidOperationException("Not enough actions to reach the target.");
                state.Battle = battle; battle.PositionCommitted = true;
            }
            battle.DeployedIds = state.Members.Where(m => !m.Reserve || m.CharacterId == state.LeaderId).Select(m => m.CharacterId).ToArray();
        });
        var deployment = CompanySnapshot.Take(state);
        var session = new CompanyBattleSession(this, state); root.SetActive(false);
        try { session.Start(); }
        catch (Exception ex)
        {
            var restored = deployment;
            QueueBattleReturn(restored, session);
            returningError = "Could not start this battle: " + ex.Message + " Deployment has been restored.";
        }
    }
    internal void QueueBattleReturn(CompanyCampaign result, CompanyBattleSession session)
    { returnedCampaign = result; returningSession = session; }
    void Update()
    {
        if (CompanySaveMenu.IsVisible) return;
        if (optionsOpen)
        {
            var options = State.GameManager.Menu.Options;
            if (options.gameObject.activeSelf)
            {
                if (Input.GetButtonDown("Menu")) options.CloseAndSave();
                return;
            }
            optionsOpen = false; root.SetActive(true); RefreshNavigation(optionsTownServices, optionsCampaign);
        }
        if (returnedCampaign == null) return;
        State.GameManager.ReturnFromCompanyBattle(); returningSession.RestoreServices();
        state = returnedCampaign; returnedCampaign = null; returningSession = null;
        root.SetActive(true); owner.UI.SetActive(false);
        RefreshState("Battle results settled."); missions.Resume();
        if (!string.IsNullOrEmpty(returningError)) { message.text = returningError; returningError = null; }
    }
    internal void Mutate(Action operation)
    {
        if (travelling) throw new InvalidOperationException("Wait for travel to finish before managing the company.");
        if (state.InBattle) throw new InvalidOperationException("Company management is unavailable during battle.");
        // Domain actions check their own preconditions and perform scoped rollback where needed.
        operation();
        RefreshState("Company updated.");
    }
    internal void ResolveDecision()
    {
        if (state.PendingDecisionKind == "Departures" && !string.IsNullOrEmpty(state.DeferredWageDecision))
        {
            state.PendingDecision = state.DeferredWageDecision; state.DeferredWageDecision = null;
            state.PendingDecisionKind = state.DeferredDecisionKind ?? "Payday";
            state.DeferredDecisionKind = null; state.PendingCharacterIds = new string[0];
            RefreshState("Departure acknowledged; a separate wage warning requires your response."); management.Decision(); return;
        }
        state.PendingDecision = null; state.PendingDecisionKind = null; state.PendingCharacterIds = new string[0];
        clock.Resume(state); RefreshState("Daily update settled. Any remaining rest days wait for your next rest command."); ShowMap();
        if (!string.IsNullOrEmpty(state.PendingDecision)) management.Decision();
    }
    void Rest(int days)
    {
        if (state == null || state.Busy) return;
        if (!state.CanStartRest) throw new InvalidOperationException("Rest requires all daily actions. End the turn first.");
        state.RestDaysRemaining = days;
        ResumeRest();
    }
    void ResumeRest()
    {
        if (state == null || state.Busy) return;
        if (!state.CanStartRest) throw new InvalidOperationException("Rest requires all daily actions. End the turn first.");
        while (state.RestDaysRemaining > 0 && !state.Busy) AdvanceDay(true);
    }
}

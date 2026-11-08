// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-08: Provide a password-gated catalog of campaign weapon artwork and catalog availability.
// 2026-11-08: Compare shared and curated species-specific weapon artwork side by side.
// 2026-11-08: Enumerate all retained species weapon mappings with numbered large previews.
// 2026-11-08: Add an isolated paper-doll transplant trial for curated company species.
// See CHANGES.md for the fork's change record and License.txt for GPL-3.0 terms.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

internal sealed class DeveloperWeaponArtGallery : MonoBehaviour
{
    RaceEditorPanel owner;
    GameObject window;
    CompanyWeaponArtCatalog.Discovery discovery;
    Text viewSummary;
    Transform contentRoot;
    RectTransform contentRect;
    Race? trialSpecies;
    bool trialHasBreasts = true;
    bool trialAttackFrame;

    sealed class SpriteLayerState
    {
        internal SpriteExtraInfo Layer;
        internal int Order;
        internal float X, Y;
        internal Func<Actor_Unit, Sprite> Sprite;
        internal Func<Actor_Unit, Color> Color;
        internal Func<Actor_Unit, ColorSwapPalette> Palette;

        internal void Restore()
        {
            Layer.layer = Order;
            Layer.XOffset = X;
            Layer.YOffset = Y;
            Layer.GetSprite = Sprite;
            Layer.GetColor = Color;
            Layer.GetPalette = Palette;
        }
    }

    sealed class RaceRendererState
    {
        readonly List<SpriteLayerState> layers = new List<SpriteLayerState>();
        readonly DefaultRaceData race;
        readonly Vector2 wholeBodyOffset;
        readonly Vector3 clothingShift;

        internal RaceRendererState(DefaultRaceData data)
        {
            race = data;
            wholeBodyOffset = data.WholeBodyOffset;
            clothingShift = data.ClothingShift;
            var seen = new HashSet<SpriteExtraInfo>();
            for (Type type = data.GetType(); type != null && type != typeof(object); type = type.BaseType)
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    var layer = field.GetValue(data) as SpriteExtraInfo;
                    if (layer == null || !seen.Add(layer)) continue;
                    layers.Add(new SpriteLayerState
                    {
                        Layer = layer,
                        Order = layer.layer,
                        X = layer.XOffset,
                        Y = layer.YOffset,
                        Sprite = layer.GetSprite,
                        Color = layer.GetColor,
                        Palette = layer.GetPalette
                    });
                }
            }
        }

        internal void Restore()
        {
            foreach (var layer in layers) layer.Restore();
            race.WholeBodyOffset = wholeBodyOffset;
            race.ClothingShift = clothingShift;
        }
    }

    static readonly FieldInfo ActorMode = typeof(Actor_Unit).GetField("mode", BindingFlags.Instance | BindingFlags.NonPublic);

    internal static void Attach(RaceEditorPanel editor, Transform buttonRoot)
    {
        var gallery = editor.GetComponent<DeveloperWeaponArtGallery>();
        if (gallery == null)
        {
            gallery = editor.gameObject.AddComponent<DeveloperWeaponArtGallery>();
            gallery.owner = editor;
        }

        var button = UITheme.Node("Weapon art gallery entry", buttonRoot);
        var rect = button.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
        rect.sizeDelta = new Vector2(220, 36);
        rect.anchoredPosition = new Vector2(264, 32);
        UITheme.Surface(button, UITheme.Control, true);
        var control = button.AddComponent<Button>();
        UITheme.StyleSelectable(control);
        var label = UITheme.Label(button.transform, "Weapon art gallery", 0, 0, 220, 36, 18);
        label.alignment = TextAnchor.MiddleCenter;
        control.onClick.AddListener(gallery.Open);
    }

    void Open()
    {
        if (owner == null || State.GameManager == null || State.GameManager.Menu == null ||
            !State.GameManager.Menu.IsRaceEditorSessionActive) return;
        if (window == null) Build();
        window.SetActive(true);
    }

    internal bool IsVisible => window != null && window.activeSelf;

    internal void Close()
    {
        if (window != null) window.SetActive(false);
    }

    void Build()
    {
        discovery = CompanyWeaponArtCatalog.Discover();
        window = UITheme.Node("Weapon art gallery", owner.transform);
        var canvas = window.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 550;
        var scaler = window.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 800);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        window.AddComponent<GraphicRaycaster>();

        var dim = UITheme.Node("Dimmer", window.transform);
        UITheme.Stretch(dim.GetComponent<RectTransform>());
        UITheme.Surface(dim, new Color(0, 0, 0, .8f));

        var panel = UITheme.Node("Weapon art catalog", window.transform);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(.5f, .5f);
        panelRect.sizeDelta = new Vector2(1200, 770);
        UITheme.Surface(panel, UITheme.Panel, true);
        UITheme.Label(panel.transform, "Weapon art reference — all retained species", 24, 12, 1120, 36, 25);
        viewSummary = UITheme.Label(panel.transform, "", 24, 50, 1145, 42, 14);
        viewSummary.horizontalOverflow = HorizontalWrapMode.Wrap;
        viewSummary.verticalOverflow = VerticalWrapMode.Overflow;

        Button(panel.transform, "Sprite list", 24, 96, 145, () => ShowReferenceList());
        Button(panel.transform, "Trial on Avian", 180, 96, 145, () => ShowTransplant(Race.Avians));
        Button(panel.transform, "Trial on Lizard", 336, 96, 145, () => ShowTransplant(Race.Lizards));
        Button(panel.transform, "Trial on Gnoll", 492, 96, 145, () => ShowTransplant(Race.Gnolls));
        Button(panel.transform, "With breasts", 675, 96, 150, () => SetTrialForm(true));
        Button(panel.transform, "Without breasts", 836, 96, 160, () => SetTrialForm(false));
        Button(panel.transform, "Idle frame", 1005, 96, 82, () => SetTrialFrame(false));
        Button(panel.transform, "Attack frame", 1094, 96, 86, () => SetTrialFrame(true));

        var scrollNode = UITheme.Node("Artwork list", panel.transform);
        UITheme.Place(scrollNode.GetComponent<RectTransform>(), 20, 140, 1160, 570);
        UITheme.Surface(scrollNode, UITheme.Background, true);

        var viewport = UITheme.Node("Viewport", scrollNode.transform);
        UITheme.Stretch(viewport.GetComponent<RectTransform>(), 4, 22, 4, 4);
        var viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = Color.white;
        viewportImage.raycastTarget = true;
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        var content = UITheme.Node("Content", viewport.transform);
        contentRoot = content.transform;
        contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(.5f, 1);

        var scroll = scrollNode.AddComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = 28;

        Button(panel.transform, "Close", 968, 720, 200, () => window.SetActive(false));
        ShowReferenceList();
    }

    void ShowReferenceList()
    {
        trialSpecies = null;
        viewSummary.text = discovery.References.Count + " numbered mappings across " +
            discovery.References.Select(item => item.Species).Distinct().Count() +
            " species, curated and non-curated. Each card shows the exact renderer's idle and attack frames. Graphic-slot labels describe code slots, not weapon identities. Tell me what each number depicts.";
        PopulateCards(false);
    }

    void ShowTransplant(Race species)
    {
        trialSpecies = species;
        viewSummary.text = "Transplant trial on " + SpeciesPolicy.DisplayName(species) + " (" +
            (trialHasBreasts ? "with breasts" : "without breasts") + ", " + (trialAttackFrame ? "attack" : "idle") + " frame). " +
            "Each number shows the identified source art placed on that species' paper doll using the matching graphic slot's normal placement. This preview does not change game assignments.";
        PopulateCards(true);
    }

    void SetTrialForm(bool hasBreasts)
    {
        trialHasBreasts = hasBreasts;
        if (trialSpecies.HasValue) ShowTransplant(trialSpecies.Value);
    }

    void SetTrialFrame(bool attack)
    {
        trialAttackFrame = attack;
        if (trialSpecies.HasValue) ShowTransplant(trialSpecies.Value);
    }

    void PopulateCards(bool transplant)
    {
        foreach (Transform child in contentRoot)
            Destroy(child.gameObject);

        const int columns = 4;
        const float cardWidth = 270;
        const float horizontalGap = 8;
        float cardHeight = transplant ? 270 : 224;
        int rows = Mathf.CeilToInt(discovery.References.Count / (float)columns);
        string noArtText = "Species with no weapon sprite for these company graphic slots: " +
            (discovery.NoWeaponArt.Count == 0 ? "none" : string.Join(", ", discovery.NoWeaponArt.ToArray()));
        string errorText = discovery.Errors.Count == 0 ? string.Empty :
            "\nSpecies whose preview could not be read: " + string.Join(", ", discovery.Errors.ToArray());
        string footerText = noArtText + errorText;
        float footerHeight = 54 + Mathf.CeilToInt(footerText.Length / 150f) * 18;
        contentRect.sizeDelta = new Vector2(0, rows * cardHeight + footerHeight + 12);

        Dictionary<int, GameObject[]> trialTemplates = null;
        try
        {
            if (transplant)
                trialTemplates = CreateTrialTemplates(trialSpecies.Value, trialHasBreasts);

            for (int i = 0; i < discovery.References.Count; i++)
            {
                float x = (i % columns) * (cardWidth + horizontalGap);
                float y = (i / columns) * cardHeight;
                if (transplant)
                    AddTransplantCard(contentRoot, discovery.References[i], x, y, cardWidth, cardHeight,
                        trialSpecies.Value, trialAttackFrame, trialTemplates);
                else
                    AddReferenceCard(contentRoot, discovery.References[i], x, y, cardWidth, cardHeight);
            }
        }
        finally
        {
            if (trialTemplates != null)
                foreach (var templates in trialTemplates.Values)
                    foreach (var template in templates)
                        if (template != null) Destroy(template);
        }
        AddSummary(contentRoot, rows * cardHeight + 8, footerHeight, footerText);
    }

    Dictionary<int, GameObject[]> CreateTrialTemplates(Race species, bool hasBreasts)
    {
        var templates = new Dictionary<int, GameObject[]>();
        var raceData = Races.GetRace(species);
        if (raceData == null || raceData.Weapon == null)
        {
            viewSummary.text += " No paper-doll weapon layer is available for this species.";
            return templates;
        }

        var unit = new Unit(0, species, 0, true, UnitType.Soldier,
            itemContext: CompanyEquipment.Repository, companyLoadout: true);
        unit.TotalRandomizeAppearance();
        unit.SetDefaultBreastSize(hasBreasts ? 0 : -1);
        var actor = new Actor_Unit(unit);
        var weapon = new Weapon("Trial weapon", string.Empty, 0, CompanyWeaponArtCatalog.LightMelee, 1, 1);
        unit.Items[0] = weapon;
        actor.BestMelee = weapon;
        actor.BestRanged = null;

        var root = UITheme.Node("Transplant template", window.transform);
        root.SetActive(false);
        var portrait = root.AddComponent<UIUnitSprite>();
        portrait.Name = UITheme.Label(root.transform, string.Empty, 0, 0, 1, 1);
        portrait.SetIndex(-1);

        var rendererState = new RaceRendererState(raceData);
        try
        {
            Sprite placeholder = discovery.References.Select(reference => reference.Idle ?? reference.Attack)
                .FirstOrDefault(sprite => sprite != null);
            if (placeholder == null)
                return templates;

            foreach (var entry in CompanyWeaponArtCatalog.Entries)
            {
                var pair = new GameObject[2];
                templates[entry.Graphic] = pair;
                for (int mode = 0; mode < 2; mode++)
                {
                    rendererState.Restore();
                    bool ranged = entry.Graphic == CompanyWeaponArtCatalog.LightRanged ||
                        entry.Graphic == CompanyWeaponArtCatalog.HeavyRanged;
                    weapon = new Weapon("Trial weapon", string.Empty, 0, entry.Graphic, 1, 1);
                    unit.Items[0] = weapon;
                    unit.CompanyLastUsedWeaponRanged = ranged;
                    actor.BestMelee = ranged ? null : weapon;
                    actor.BestRanged = ranged ? weapon : null;
                    if (ActorMode != null)
                        ActorMode.SetValue(actor, Enum.Parse(ActorMode.FieldType, mode == 0 ? "None" : "Attacking"));

                    raceData.Weapon.GetSprite = unused => placeholder;
                    portrait.UpdateSprites(actor);
                    portrait.Name.gameObject.SetActive(false);

                    var copy = Instantiate(root, window.transform, false);
                    copy.name = "Transplant template " + entry.Graphic + (mode == 0 ? " idle" : " attack");
                    copy.SetActive(false);
                    pair[mode] = copy;
                }
            }
        }
        catch (Exception exception)
        {
            foreach (var pair in templates.Values)
                foreach (var template in pair)
                    if (template != null) Destroy(template);
            templates.Clear();
            viewSummary.text += " The paper-doll preview could not be created (" + exception.GetType().Name + ").";
        }
        finally
        {
            rendererState.Restore();
            Destroy(root);
        }
        return templates;
    }

    static void AddTransplantCard(Transform content, CompanyWeaponArtCatalog.ArtReference reference,
        float x, float y, float width, float height, Race targetSpecies, bool attackFrame,
        Dictionary<int, GameObject[]> templates)
    {
        var card = UITheme.Node("Transplant trial " + reference.Number.ToString("D3"), content);
        UITheme.Place(card.GetComponent<RectTransform>(), x, y, width, height - 8);
        UITheme.Surface(card, UITheme.Row, true);

        string title = "#" + reference.Number.ToString("D3") + " — " + SpeciesPolicy.DisplayName(reference.Species);
        if (!string.IsNullOrEmpty(reference.Form)) title += " — " + reference.Form;
        var heading = UITheme.Label(card.transform, title, 8, 5, width - 16, 28, 14);
        heading.horizontalOverflow = HorizontalWrapMode.Wrap;
        var slot = CompanyWeaponArtCatalog.Entries.First(entry => entry.Graphic == reference.Graphic);
        var slotLabel = UITheme.Label(card.transform,
            "Graphic " + reference.Graphic + " — " + slot.Name + " → " + SpeciesPolicy.DisplayName(targetSpecies),
            8, 33, width - 16, 20, 12);
        slotLabel.color = UITheme.Muted;

        GameObject[] pair;
        GameObject template;
        if (!templates.TryGetValue(reference.Graphic, out pair) || pair == null ||
            (template = pair[attackFrame ? 1 : 0]) == null)
        {
            UITheme.Label(card.transform, "Paper-doll preview unavailable", 8, 110, width - 16, 40, 13)
                .alignment = TextAnchor.MiddleCenter;
            return;
        }

        var copy = Instantiate(template, card.transform, false);
        copy.name = "Transplanted weapon art " + reference.Number.ToString("D3");
        copy.SetActive(true);
        var rect = copy.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = new Vector2(0, -24);
        rect.localScale = Vector3.one * 1.15f;

        Sprite source = attackFrame ? (reference.Attack ?? reference.Idle) : (reference.Idle ?? reference.Attack);
        var weaponTransform = copy.transform.Find("Weapon");
        var image = weaponTransform == null ? null : weaponTransform.GetComponent<Image>();
        if (image == null || source == null)
        {
            copy.SetActive(false);
            UITheme.Label(card.transform, "Weapon frame unavailable", 8, 110, width - 16, 40, 13)
                .alignment = TextAnchor.MiddleCenter;
            return;
        }

        image.sprite = source;
        image.raycastTarget = false;
        image.color = Color.white;
        image.gameObject.SetActive(true);
        Rect sourceRect = source.rect;
        if (sourceRect.width > sourceRect.height)
            weaponTransform.localScale = new Vector3(1, sourceRect.height / sourceRect.width, 1);
        else if (sourceRect.height > sourceRect.width)
            weaponTransform.localScale = new Vector3(sourceRect.width / sourceRect.height, 1, 1);
        else
            weaponTransform.localScale = Vector3.one;

        foreach (var graphic in copy.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
    }

    static void AddReferenceCard(Transform content, CompanyWeaponArtCatalog.ArtReference reference,
        float x, float y, float width, float height)
    {
        var card = UITheme.Node("Weapon art " + reference.Number.ToString("D3"), content);
        UITheme.Place(card.GetComponent<RectTransform>(), x, y, width, height - 8);
        UITheme.Surface(card, UITheme.Row, true);

        string title = "#" + reference.Number.ToString("D3") + " — " + SpeciesPolicy.DisplayName(reference.Species);
        if (!string.IsNullOrEmpty(reference.Form)) title += " — " + reference.Form;
        var heading = UITheme.Label(card.transform, title, 8, 6, width - 16, 28, 14);
        heading.horizontalOverflow = HorizontalWrapMode.Wrap;
        var slot = CompanyWeaponArtCatalog.Entries.First(entry => entry.Graphic == reference.Graphic);
        var slotLabel = UITheme.Label(card.transform,
            "Graphic " + reference.Graphic + " — " + slot.Name + " slot", 8, 34, width - 16, 20, 12);
        slotLabel.color = UITheme.Muted;

        var idleLabel = UITheme.Label(card.transform, "Idle", 18, 55, 108, 18, 12);
        idleLabel.alignment = TextAnchor.MiddleCenter;
        idleLabel.color = UITheme.Muted;
        var attackLabel = UITheme.Label(card.transform, "Attack", 144, 55, 108, 18, 12);
        attackLabel.alignment = TextAnchor.MiddleCenter;
        attackLabel.color = UITheme.Muted;
        AddPreview(card.transform, reference.Idle, 18, 75, 108, 136);
        AddPreview(card.transform, reference.Attack, 144, 75, 108, 136);
    }

    static void AddSummary(Transform content, float y, float height, string text)
    {
        var summary = UITheme.Node("Species with no weapon sprite", content);
        UITheme.Place(summary.GetComponent<RectTransform>(), 0, y, 1104, height);
        UITheme.Surface(summary, UITheme.Row, true);
        var textNode = UITheme.Label(summary.transform, text, 12, 8, 1080, height - 16, 13);
        textNode.alignment = TextAnchor.UpperLeft;
        textNode.horizontalOverflow = HorizontalWrapMode.Wrap;
        textNode.verticalOverflow = VerticalWrapMode.Overflow;
        textNode.color = UITheme.Muted;
    }

    static void AddPreview(Transform parent, Sprite sprite, float x, float y, float width, float height)
    {
        var preview = UITheme.Node("Weapon sprite preview", parent);
        UITheme.Place(preview.GetComponent<RectTransform>(), x, y, width, height);
        UITheme.Surface(preview, UITheme.Background, true);
        var imageNode = UITheme.Node("Weapon sprite", preview.transform);
        UITheme.Stretch(imageNode.GetComponent<RectTransform>(), 4, 4, 4, 4);
        var image = imageNode.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        if (image.sprite == null)
            UITheme.Label(preview.transform, "—", 0, 42, width, 40, 24).alignment = TextAnchor.MiddleCenter;
    }

    static void Button(Transform parent, string title, float x, float y, float width, Action action)
    {
        var node = UITheme.Node(title, parent);
        UITheme.Place(node.GetComponent<RectTransform>(), x, y, width, 38);
        UITheme.Surface(node, UITheme.Control, true);
        var button = node.AddComponent<Button>();
        UITheme.StyleSelectable(button);
        var text = UITheme.Label(node.transform, title, 4, 0, width - 8, 38, 16);
        text.alignment = TextAnchor.MiddleCenter;
        button.onClick.AddListener(() => action());
    }
}

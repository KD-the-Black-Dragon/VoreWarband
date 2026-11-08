// 2026-11-06: Explicit developer drafts, validated saves and balance workflow controls (spec 40A).
// Modified for the VoreWar mercenary-company conversion.
// 2026-10-30: Require an authorized editor session and revoke it on close (spec 02B).
// 2026-10-30: Exclude retired metadata catalogs/previews and preserve unchanged mapping values (spec 03B1).
// 2026-11-01: Separate stored species reads from intentional override creation (spec 03B3C).
// 2026-11-01: Preserve species and mapping identity behind singular developer dropdown labels (09–10 feedback).
// 2026-11-03: Remove permanently retired identity branches/entries while preserving shared behavior (spec 21).
// 2026-11-03: Keep developer tag selections stable across empty lists, sparse IDs and editor refreshes (spec 22).
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
// 2026-11-03: Implement four active stats and agreed combat rules (spec 29).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RaceEditorPanel : MonoBehaviour
{

    public TMP_Dropdown RaceDropdown;
    public TMP_Dropdown DisplayRaceDropdown;

    public Toggle OverrideGender;

    public Slider MaleFraction;
    public Slider HermFraction;

    public Toggle OverrideFurry;
    public Slider FurryFraction;

    public InputField BodySize;
    public InputField StomachSize;

    public TMP_Dropdown InnateSpellDropdown;
    public TMP_Dropdown SpawnRaceDropdown;
    public TMP_Dropdown ConversionRaceDropdown;
    public TMP_Dropdown LeaderRaceDropdown;
    public TMP_Dropdown MorphRaceDropdown;

    public TMP_Dropdown TraitDropdown;
    public TextMeshProUGUI TraitList;
    public TMP_Dropdown TagDropdown;
    public TextMeshProUGUI TagList;

    public Toggle OralVoreDisabled;
    public Toggle UnbirthDisabled;
    public Toggle CockVoreDisabled;
    public Toggle BreastVoreDisabled;
    public Toggle TailVoreDisabled;
    public Toggle AnalVoreDisabled;
    public Toggle BladderVoreDisabled;

    public InputField MinStrength;
    public InputField MaxStrength;
    public InputField MinDexterity;
    public InputField MaxDexterity;
    public InputField MinEndurance;
    public InputField MaxEndurance;
    public InputField MinMind;
    public InputField MaxMind;
    public InputField MinWill;
    public InputField MaxWill;
    public InputField MinAgility;
    public InputField MaxAgility;
    public InputField MinVoracity;
    public InputField MaxVoracity;
    public InputField MinStomach;
    public InputField MaxStomach;

    public InputField PowerAdjustment;
    public InputField Upkeep;
    public InputField DeployCost;

    public Toggle OverrideClothed;
    public Slider ClothedFraction;
    public Toggle OverrideWeight;
    public InputField MinWeight;
    public InputField MaxWeight;
    public Toggle OverrideBoob;
    public InputField MinBoob;
    public InputField MaxBoob;
    public Toggle OverrideDick;
    public InputField MinDick;
    public InputField MaxDick;

    public Text WeightText;
    public Text BoobText;
    public Text DickText;

    public InputField MaleTraits;
    public InputField FemaleTraits;
    public InputField HermTraits;
    public InputField SpawnTraits;
    public InputField LeaderTraits;

    public TMP_Dropdown BannerType;

    public TMP_Dropdown FavoredStat;

    public TMP_Dropdown RaceAIDropdown;

    public TextMeshProUGUI InfoText;

    public GameObject GeneralPanel;
    public GameObject TraitsPanel;
    public GameObject TaggedTraitsPanel;
    public GameObject UnitTagsPanel;

    public Button GeneralButton;
    public Button TraitsButton;
    public Button TaggedTraitsButton;
    public Button UnitTagsButton;

    public TaggedTraitEditor taggedTraitEditor;

    List<Traits> CurrentTraits;
    List<int> CurrentTags;


    Race PreviousRace = (Race)(-1);
    bool loadingRace;
    internal bool LastSaveSucceeded;
    internal Race SelectedSpecies => PreviousRace;

    private void OnEnable()
    {
        FavoredStat.ClearOptions();
        FavoredStat.AddOptions(WarbandStats.Active.Select(WarbandStats.Name).ToList());
        foreach (var field in new[] { MinDexterity, MinEndurance, MinWill, MinStomach })
            if (field != null) field.transform.parent.gameObject.SetActive(false);
        if (MinMind != null)
            foreach (var label in MinMind.transform.parent.GetComponentsInChildren<TMP_Text>(true))
                if (label.text.Trim().TrimEnd(':') == "Mind" || label.text.Trim().TrimEnd(':') == "MND") label.text = "Intelligence";

        if (State.GameManager == null || State.GameManager.Menu == null || !State.GameManager.Menu.HasRaceEditorAccess)
        { gameObject.SetActive(false); return; }
        DeveloperBalancePanel.Attach(this);
    }

    private void OnDisable()
    {
        if (State.GameManager != null && State.GameManager.Menu != null)
            State.GameManager.Menu.EndRaceEditorSession();
    }

    // Metadata inspection is broader than gameplay approval, but never includes retired species.
    static bool IsEditableSpecies(Race race)
    {
        return (int)race >= 0 && Enum.IsDefined(typeof(Race), race) && !SpeciesPolicy.IsRetired(race) && BalanceAuthoring.HasMetadata(race);
    }

    static Race[] GetEditableSpecies()
    {
        return ((Race[])Enum.GetValues(typeof(Race))).Where(IsEditableSpecies).OrderBy(SpeciesPolicy.DisplayName).ToArray();
    }

    static bool TrySelectedSpecies(TMP_Dropdown dropdown, out Race race)
    {
        race = Race.none;
        if (dropdown.value < 0 || dropdown.value >= dropdown.options.Count)
            return false;
        Race selected;
        if (!SpeciesPolicy.TryParseName(dropdown.options[dropdown.value].text, out selected) || !IsEditableSpecies(selected))
            return false;
        race = selected;
        return true;
    }

    static void PopulateSpeciesDropdown(TMP_Dropdown dropdown, bool mapping)
    {
        string previous = dropdown.value >= 0 && dropdown.value < dropdown.options.Count ?
            dropdown.options[dropdown.value].text : null;
        Race previousRace;
        if (SpeciesPolicy.TryParseName(previous, out previousRace)) previous = SpeciesPolicy.DisplayName(previousRace);
        dropdown.ClearOptions();
        if (mapping)
            dropdown.options.Add(new TMP_Dropdown.OptionData("Keep existing mapping"));
        foreach (Race race in GetEditableSpecies())
            dropdown.options.Add(new TMP_Dropdown.OptionData(SpeciesPolicy.DisplayName(race)));
        dropdown.value = Math.Max(0, dropdown.options.FindIndex(option => option.text == previous));
        dropdown.RefreshShownValue();
    }

    static void SelectMapping(TMP_Dropdown dropdown, Race stored)
    {
        dropdown.value = Math.Max(0, dropdown.options.FindIndex(option => option.text == SpeciesPolicy.DisplayName(stored)));
        dropdown.RefreshShownValue();
    }

    static Race ReadMapping(TMP_Dropdown dropdown, Race stored)
    {
        Race selected;
        return TrySelectedSpecies(dropdown, out selected) ? selected : stored;
    }

    internal void ShowPanel()
    {
        bool wasLoading = loadingRace;
        loadingRace = true;
        try
        {
            if (!State.GameManager.Menu.IsRaceEditorSessionActive)
                return;
            PopulateSpeciesDropdown(RaceDropdown, false);

            TraitDropdown.options.Clear();
                foreach (RandomizeList rl in State.RandomizeLists)
                {
                    TraitDropdown.options.Add(new TMP_Dropdown.OptionData(rl.name.ToString()));
                }
                foreach (CustomTraitBoost ct in State.CustomTraitList)
                {
                    TraitDropdown.options.Add(new TMP_Dropdown.OptionData(ct.name.ToString()));
                }
                foreach (ConditionalTraitContainer cdt in State.ConditionalTraitList)
                {
                    TraitDropdown.options.Add(new TMP_Dropdown.OptionData(cdt.name.ToString()));
                }
                foreach (Traits traitId in ((Traits[])Enum.GetValues(typeof(Traits))).OrderBy(s =>
                 {
                     return s >= Traits.LightningSpeed ? "ZZZ" + s.ToString() : s.ToString();
                 }))
                {
                    TraitDropdown.options.Add(new TMP_Dropdown.OptionData(traitId.ToString()));
                }
                TraitDropdown.RefreshShownValue();

            RefreshUnitTags();

            if (FavoredStat.options?.Any() == false)
            {
                foreach (Stat stat in WarbandStats.Active)
                {
                    FavoredStat.options.Add(new TMP_Dropdown.OptionData(WarbandStats.Name(stat)));
                }
                FavoredStat.RefreshShownValue();
            }

            if (InnateSpellDropdown.options?.Any() == false)
            {
                foreach (SpellTypes type in ((SpellTypes[])Enum.GetValues(typeof(SpellTypes))).Where(s => (int)s < 100))
                {
                    InnateSpellDropdown.options.Add(new TMP_Dropdown.OptionData(type.ToString()));
                }
                InnateSpellDropdown.RefreshShownValue();
            }

            PopulateSpeciesDropdown(SpawnRaceDropdown, true);
            PopulateSpeciesDropdown(ConversionRaceDropdown, true);
            PopulateSpeciesDropdown(LeaderRaceDropdown, true);
            PopulateSpeciesDropdown(MorphRaceDropdown, true);

            if (BannerType.options.Count < 4)
            {
                foreach (BannerTypes type in (BannerTypes[])Enum.GetValues(typeof(BannerTypes)))
                {
                    BannerType.options.Add(new TMP_Dropdown.OptionData(type.ToString()));
                }
                for (int i = 0; i < CustomBannerTest.Sprites.Length; i++)
                {
                    if (CustomBannerTest.Sprites[i] == null)
                        break;
                    BannerType.options.Add(new TMP_Dropdown.OptionData($"Custom {i + 1}"));

                }
            }

            if (RaceAIDropdown.options?.Any() == false)
            {
                foreach (RaceAI raceAI in ((RaceAI[])Enum.GetValues(typeof(RaceAI))))
                {
                    RaceAIDropdown.options.Add(new TMP_Dropdown.OptionData(raceAI.ToString()));
                }
                RaceAIDropdown.RefreshShownValue();
            }

            LoadRace();
            UpdateInteractable();
            ActivateGeneral();

        }
        finally
        {
            loadingRace = wasLoading;
        }
    }

    public void RaceChanged()
    {
        if (loadingRace || !State.GameManager.Menu.IsRaceEditorSessionActive)
            return;
        SaveRace();
        LoadRace();
        UpdateInteractable();
    }


    public void AddTrait()
    {
        if (State.RandomizeLists.Any(rl => rl.name == TraitDropdown.options[TraitDropdown.value].text)){
            CurrentTraits.Add((Traits)State.RandomizeLists.Where(rl => rl.name == TraitDropdown.options[TraitDropdown.value].text).FirstOrDefault()?.id);
        }
        if (State.CustomTraitList.Any(ct => ct.name == TraitDropdown.options[TraitDropdown.value].text)){
            CurrentTraits.Add((Traits)State.CustomTraitList.Where(ct => ct.name == TraitDropdown.options[TraitDropdown.value].text).FirstOrDefault()?.id);
        }
        if (State.ConditionalTraitList.Any(cdt => cdt.name == TraitDropdown.options[TraitDropdown.value].text)){
            CurrentTraits.Add((Traits)State.ConditionalTraitList.Where(cdt => cdt.name == TraitDropdown.options[TraitDropdown.value].text).FirstOrDefault()?.id);
        }
        if (Enum.TryParse(TraitDropdown.options[TraitDropdown.value].text, out Traits trait))
        {
            if (CurrentTraits.Contains(trait) == false)
                CurrentTraits.Add(trait);
        }
        UpdateInteractable();
    }

    public void AddTraitALL()
    {
        if (State.RandomizeLists.Any(rl => rl.name == TraitDropdown.options[TraitDropdown.value].text))
        {
            foreach (Race race in GetEditableSpecies())
            {
                RaceSettingsItem item = State.RaceSettings.GetOrCreate(race);
                item.RaceTraits.Add((Traits)State.RandomizeLists.Where(rl => rl.name == TraitDropdown.options[TraitDropdown.value].text).FirstOrDefault()?.id);
            }
            AddTrait();
        }
        if (State.CustomTraitList.Any(ct => ct.name == TraitDropdown.options[TraitDropdown.value].text))
        {
            foreach (Race race in GetEditableSpecies())
            {
                RaceSettingsItem item = State.RaceSettings.GetOrCreate(race);
                item.RaceTraits.Add((Traits)State.CustomTraitList.Where(ct => ct.name == TraitDropdown.options[TraitDropdown.value].text).FirstOrDefault()?.id);
            }
            AddTrait();
        }
        if (State.ConditionalTraitList.Any(cdt => cdt.name == TraitDropdown.options[TraitDropdown.value].text))
        {
            foreach (Race race in GetEditableSpecies())
            {
                RaceSettingsItem item = State.RaceSettings.GetOrCreate(race);
                item.RaceTraits.Add((Traits)State.ConditionalTraitList.Where(ct => ct.name == TraitDropdown.options[TraitDropdown.value].text).FirstOrDefault()?.id);
            }
            AddTrait();
        }
        if (Enum.TryParse(TraitDropdown.options[TraitDropdown.value].text, out Traits trait))
        {
            foreach (Race race in GetEditableSpecies())
            {
                RaceSettingsItem item = State.RaceSettings.GetOrCreate(race);
                if (item.RaceTraits.Contains(trait) == false)
                    item.RaceTraits.Add(trait);
            }
            AddTrait();
        }

    }

    public void RemoveTrait()
    {
        if (State.RandomizeLists.Any(rl => rl.name == TraitDropdown.options[TraitDropdown.value].text))
        {
            CurrentTraits.Remove((Traits)State.RandomizeLists.Where(rl => rl.name == TraitDropdown.options[TraitDropdown.value].text).FirstOrDefault()?.id);
        }
        if (State.CustomTraitList.Any(ct => ct.name == TraitDropdown.options[TraitDropdown.value].text))
        {
            CurrentTraits.Remove((Traits)State.CustomTraitList.Where(ct => ct.name == TraitDropdown.options[TraitDropdown.value].text).FirstOrDefault()?.id);
        }
        if (State.ConditionalTraitList.Any(ct => ct.name == TraitDropdown.options[TraitDropdown.value].text))
        {
            CurrentTraits.Remove((Traits)State.ConditionalTraitList.Where(ct => ct.name == TraitDropdown.options[TraitDropdown.value].text).FirstOrDefault()?.id);
        }
            if (Enum.TryParse(TraitDropdown.options[TraitDropdown.value].text, out Traits trait))
        {
            CurrentTraits.Remove(trait);
        }
        UpdateInteractable();
    }

    public void RemoveTraitALL()
    {
        if (State.RandomizeLists.Any(rl => rl.name == TraitDropdown.options[TraitDropdown.value].text))
        {
            foreach (Race race in GetEditableSpecies())
            {
                RaceSettingsItem item = State.RaceSettings.GetOrCreate(race);
                item.RaceTraits.Remove((Traits)State.RandomizeLists.Where(rl => rl.name == TraitDropdown.options[TraitDropdown.value].text).FirstOrDefault()?.id);
            }
            RemoveTrait();
        }
        if (State.CustomTraitList.Any(ct => ct.name == TraitDropdown.options[TraitDropdown.value].text))
        {
            foreach (Race race in GetEditableSpecies())
            {
                RaceSettingsItem item = State.RaceSettings.GetOrCreate(race);
                item.RaceTraits.Remove((Traits)State.CustomTraitList.Where(rl => rl.name == TraitDropdown.options[TraitDropdown.value].text).FirstOrDefault()?.id);
            }
            RemoveTrait();
        }
        if (State.ConditionalTraitList.Any(ct => ct.name == TraitDropdown.options[TraitDropdown.value].text))
        {
            foreach (Race race in GetEditableSpecies())
            {
                RaceSettingsItem item = State.RaceSettings.GetOrCreate(race);
                item.RaceTraits.Remove((Traits)State.ConditionalTraitList.Where(rl => rl.name == TraitDropdown.options[TraitDropdown.value].text).FirstOrDefault()?.id);
            }
            RemoveTrait();
        }
        if (Enum.TryParse(TraitDropdown.options[TraitDropdown.value].text, out Traits trait))
        {
            foreach (Race race in GetEditableSpecies())
            {
                RaceSettingsItem item = State.RaceSettings.GetOrCreate(race);
                item.RaceTraits.Remove(trait);
            }
            RemoveTrait();
        }

    }

    readonly List<int> availableTagIds = new List<int>();

    internal void RefreshUnitTags()
    {
        int selectedId = TagDropdown.value >= 0 && TagDropdown.value < availableTagIds.Count
            ? availableTagIds[TagDropdown.value] : -1;
        availableTagIds.Clear();
        TagDropdown.options.Clear();
        foreach (UnitTag tag in State.UnitTagList)
        {
            availableTagIds.Add(tag.id);
            TagDropdown.options.Add(new TMP_Dropdown.OptionData(tag.name));
        }
        TagDropdown.interactable = availableTagIds.Count > 0;
        TagDropdown.value = System.Math.Max(0, availableTagIds.IndexOf(selectedId));
        TagDropdown.RefreshShownValue();
    }

    UnitTag SelectedUnitTag()
    {
        if (TagDropdown.value < 0 || TagDropdown.value >= availableTagIds.Count) return null;
        return State.UnitTagList.FirstOrDefault(t => t.id == availableTagIds[TagDropdown.value]);
    }

    public void AddTag()
    {
        UnitTag tag = SelectedUnitTag();
        if (tag == null) return;
        if (CurrentTags == null) CurrentTags = new List<int>();
        if (!CurrentTags.Contains(tag.id)) CurrentTags.Add(tag.id);
        UpdateInteractable();
    }

    public void AddTagALL()
    {
        UnitTag tag = SelectedUnitTag();
        if (tag == null) return;
        foreach (Race race in GetEditableSpecies())
        {
            RaceSettingsItem item = State.RaceSettings.GetOrCreate(race);
            if (item.RaceTags == null) item.RaceTags = new List<int>();
            if (!item.RaceTags.Contains(tag.id)) item.RaceTags.Add(tag.id);
        }
        AddTag();
    }

    public void RemoveTag()
    {
        UnitTag tag = SelectedUnitTag();
        if (tag == null) return;
        if (CurrentTags != null) CurrentTags.RemoveAll(id => id == tag.id);
        UpdateInteractable();
    }

    public void RemoveTagALL()
    {
        UnitTag tag = SelectedUnitTag();
        if (tag == null) return;
        foreach (Race race in GetEditableSpecies())
        {
            RaceSettingsItem item = State.RaceSettings.GetOrCreate(race);
            if (item.RaceTags != null) item.RaceTags.RemoveAll(id => id == tag.id);
        }
        RemoveTag();
    }

    internal void SaveRace()
    {
        LastSaveSucceeded = false;
        if (loadingRace || !State.GameManager.Menu.IsRaceEditorSessionActive)
            return;
        var backup = BalanceAuthoring.Copy(State.RaceSettings);
        try
        {
            if (IsEditableSpecies(PreviousRace))
            {
                RaceSettingsItem item = State.RaceSettings.GetOrCreate(PreviousRace);
                var racePar = RaceParameters.GetRaceTraits(PreviousRace);
                var raceData = Races.GetRace(PreviousRace);
                item.OverrideGender = OverrideGender.isOn;
                item.OverrideFurry = OverrideFurry.isOn;
                item.maleFraction = 1 - MaleFraction.value;
                item.hermFraction = HermFraction.value;
                item.furryFraction = FurryFraction.value;

                item.BannerType = BannerType.value;
                item.RaceAI = (RaceAI)RaceAIDropdown.value;

                item.overrideClothes = OverrideClothed.isOn;
                item.clothedFraction = ClothedFraction.value;

                item.overrideWeight = OverrideWeight.isOn;
                item.MinWeight = Convert.ToInt32(MinWeight.text) - 1;
                item.MaxWeight = Convert.ToInt32(MaxWeight.text) - 1;

                item.FavoredStat = WarbandStats.Active[FavoredStat.value];
                item.FavoredStatSet = true;

                int spellIndex = InnateSpellDropdown.value;
                var value = (SpellTypes)Enum.GetValues(typeof(SpellTypes)).GetValue(spellIndex);

                //if (value > SpellTypes.Resurrection)
                //    value = value - SpellTypes.Resurrection + SpellTypes.AlraunePuff - 1;

                item.InnateSpell = value;
                item.SpawnRace = ReadMapping(SpawnRaceDropdown, item.SpawnRace);
                item.ConversionRace = ReadMapping(ConversionRaceDropdown, item.ConversionRace);
                item.LeaderRace = ReadMapping(LeaderRaceDropdown, item.LeaderRace);
                item.MorphRace = ReadMapping(MorphRaceDropdown, item.MorphRace);

                item.overrideBoob = OverrideBoob.isOn;
                item.MinBoob = Convert.ToInt32(MinBoob.text) - 1;
                item.MaxBoob = Convert.ToInt32(MaxBoob.text) - 1;

                item.overrideDick = OverrideDick.isOn;
                item.MinDick = Convert.ToInt32(MinDick.text) - 1;
                item.MaxDick = Convert.ToInt32(MaxDick.text) - 1;

                if (item.MinWeight < 0)
                    item.MinWeight = 0;
                if (item.MaxWeight >= raceData.BodySizes)
                    item.MaxWeight = Math.Max(raceData.BodySizes - 1, 0);
                if (item.MinWeight > item.MaxWeight)
                    item.MinWeight = item.MaxWeight;

                if (item.MinBoob < 0)
                    item.MinBoob = 0;
                if (item.MaxBoob >= raceData.BreastSizes)
                    item.MaxBoob = Math.Max(raceData.BreastSizes - 1, 0);
                if (item.MinBoob > item.MaxBoob)
                    item.MinBoob = item.MaxBoob;

                if (item.MinDick < 0)
                    item.MinDick = 0;
                if (item.MaxDick >= raceData.DickSizes)
                    item.MaxDick = Math.Max(raceData.DickSizes - 1, 0);
                if (item.MinDick > item.MaxDick)
                    item.MinDick = item.MaxDick;



                item.BodySize = Convert.ToInt32(BodySize.text);
                item.StomachSize = Convert.ToInt32(StomachSize.text);

                if (item.BodySize < 1)
                    item.BodySize = 1;
                if (item.StomachSize < 1)
                    item.StomachSize = 1;



                item.RaceTraits = CurrentTraits.ToList();
                item.RaceTags = CurrentTags.ToList();
                List<VoreType> newtypes = racePar.AllowedVoreTypes.ToList();
                if (OralVoreDisabled.isOn) newtypes.Remove(VoreType.Oral);
                if (UnbirthDisabled.isOn) newtypes.Remove(VoreType.Unbirth);
                if (CockVoreDisabled.isOn) newtypes.Remove(VoreType.CockVore);
                if (AnalVoreDisabled.isOn) newtypes.Remove(VoreType.Anal);
                if (BreastVoreDisabled.isOn) newtypes.Remove(VoreType.BreastVore);
                if (TailVoreDisabled.isOn) newtypes.Remove(VoreType.TailVore);
                if (BladderVoreDisabled.isOn) newtypes.Remove(VoreType.BladderVore);
                item.AllowedVoreTypes = newtypes;



                item.Stats.Strength.Minimum = Convert.ToInt32(MinStrength.text);
                item.Stats.Strength.Roll = 1 + Convert.ToInt32(MaxStrength.text) - item.Stats.Strength.Minimum;
                if (item.Stats.Strength.Roll < 1) item.Stats.Strength.Roll = 1;
                item.Stats.Dexterity.Minimum = Convert.ToInt32(MinDexterity.text);
                item.Stats.Dexterity.Roll = 1 + Convert.ToInt32(MaxDexterity.text) - item.Stats.Dexterity.Minimum;
                if (item.Stats.Dexterity.Roll < 1) item.Stats.Strength.Roll = 1;
                item.Stats.Endurance.Minimum = Convert.ToInt32(MinEndurance.text);
                item.Stats.Endurance.Roll = 1 + Convert.ToInt32(MaxEndurance.text) - item.Stats.Endurance.Minimum;
                if (item.Stats.Endurance.Roll < 1) item.Stats.Strength.Roll = 1;
                item.Stats.Mind.Minimum = Convert.ToInt32(MinMind.text);
                item.Stats.Mind.Roll = 1 + Convert.ToInt32(MaxMind.text) - item.Stats.Mind.Minimum;
                if (item.Stats.Mind.Roll < 1) item.Stats.Strength.Roll = 1;
                item.Stats.Will.Minimum = Convert.ToInt32(MinWill.text);
                item.Stats.Will.Roll = 1 + Convert.ToInt32(MaxWill.text) - item.Stats.Will.Minimum;
                if (item.Stats.Will.Roll < 1) item.Stats.Strength.Roll = 1;
                item.Stats.Agility.Minimum = Convert.ToInt32(MinAgility.text);
                item.Stats.Agility.Roll = 1 + Convert.ToInt32(MaxAgility.text) - item.Stats.Agility.Minimum;
                if (item.Stats.Agility.Roll < 1) item.Stats.Strength.Roll = 1;
                item.Stats.Voracity.Minimum = Convert.ToInt32(MinVoracity.text);
                item.Stats.Voracity.Roll = 1 + Convert.ToInt32(MaxVoracity.text) - item.Stats.Voracity.Minimum;
                if (item.Stats.Voracity.Roll < 1) item.Stats.Strength.Roll = 1;
                item.Stats.Stomach.Minimum = Convert.ToInt32(MinStomach.text);
                item.Stats.Stomach.Roll = 1 + Convert.ToInt32(MaxStomach.text) - item.Stats.Stomach.Minimum;
                if (item.Stats.Stomach.Roll < 1) item.Stats.Strength.Roll = 1;
                item.Stats.Mirror();

                item.PowerAdjustment = Convert.ToInt32(PowerAdjustment.text)/100f;
                item.Upkeep = float.TryParse(Upkeep.text, out float outputUpkeep) ? outputUpkeep : 1;
                item.DeployCost = float.TryParse(DeployCost.text, out float outputDeploy) ? outputDeploy : 1;

                item.FemaleTraits = TextToTraitList(FemaleTraits.text);
                item.MaleTraits = TextToTraitList(MaleTraits.text);
                item.HermTraits = TextToTraitList(HermTraits.text);
                item.SpawnTraits = TextToTraitList(SpawnTraits.text);
                item.LeaderTraits = TextToTraitList(LeaderTraits.text);
                LastSaveSucceeded = true;
            }
        }
        catch
        {
            State.RaceSettings = backup;
            State.GameManager.CreateMessageBox("There's an input box that's not filled in");
        }

    }

    public void OpenImportTraitList()
    {
        var box = State.GameManager.CreateInputBox();
        box.SetData(SetBaseTraitsToImportedList, "Import", "Cancel", "Insert a list of space separated traits here to replace the current list of traits for this race with that list", 120);
    }

    void SetBaseTraitsToImportedList(string traits)
    {
        CurrentTraits = TextToTraitList(traits);
        UpdateInteractable();
    }

    public static List<Traits> TextToTraitList(string text)
    {
        List<Traits> traits = new List<Traits>();
        foreach (RandomizeList rl in State.RandomizeLists)
        {
            if (text.ToLower().Contains(rl.name.ToString().ToLower()))
            {
                traits.Add((Traits)rl.id);
            }
        }
        foreach (CustomTraitBoost ct in State.CustomTraitList)
        {
            if (text.ToLower().Contains(ct.name.ToString().ToLower()))
            {
                traits.Add((Traits)ct.id);
            }
        }
        foreach (ConditionalTraitContainer ct in State.ConditionalTraitList)
        {
            if (text.ToLower().Contains(ct.name.ToString().ToLower()))
            {
                traits.Add((Traits)ct.id);
            }
        }
        foreach (Traits trait in (Stat[])Enum.GetValues(typeof(Traits)))
        {
            if (text.ToLower().Contains(trait.ToString().ToLower()))
            {
                traits.Add(trait);
            }
        }
        traits = traits.Distinct().ToList();
        return traits;
    }



    public static string TraitListToText(List<Traits> traits, bool hideSecret = false)
    {
        if (traits == null)
            return "";
        string ret = "";
        bool first = true;
        foreach (var trait in traits)
        {
            if (hideSecret && Unit.secretTags.Contains(trait)) continue; // We even hide secret traits if they would be shown once purchased, otherwise if you read "Infiltrator" you already know it's safe.
            if (first)
                first = false;
            else
                ret += ", ";
            if (State.RandomizeLists.Any(rl => (Traits)rl.id == trait))
                ret += State.RandomizeLists.Where(rl => (Traits)rl.id == trait).FirstOrDefault().name;  
            else if (State.CustomTraitList.Any(ct => (Traits)ct.id == trait))
                ret += State.CustomTraitList.Where(ct => (Traits)ct.id == trait).FirstOrDefault().name;  
            else if (State.ConditionalTraitList.Any(ct => (Traits)ct.id == trait))
                ret += State.ConditionalTraitList.Where(ct => (Traits)ct.id == trait).FirstOrDefault().name;  
            else
                ret += trait.ToString();
        }
        return ret;
    }

    internal void LoadRace()
    {
        bool wasLoading = loadingRace;
        loadingRace = true;
        try
        {
            if (TrySelectedSpecies(RaceDropdown, out Race race))
            {
                PreviousRace = race;
                RaceSettingsItem item = State.RaceSettings.GetOrCreate(race);
                var racePar = RaceParameters.GetRaceTraits(race);
                var raceData = Races.GetRace(race);
                OverrideGender.isOn = item.OverrideGender;
                OverrideFurry.isOn = item.OverrideFurry;
                MaleFraction.value = 1 - item.maleFraction;
                HermFraction.value = item.hermFraction;
                FurryFraction.value = item.furryFraction;

                BannerType.value = item.BannerType;

                RaceAIDropdown.value = (int)item.RaceAI;
                RaceAIDropdown.RefreshShownValue();

                OverrideClothed.isOn = item.overrideClothes;
                ClothedFraction.value = item.clothedFraction;

                OverrideWeight.isOn = item.overrideWeight;
                MinWeight.text = (1 + item.MinWeight).ToString();
                MaxWeight.text = (1 + item.MaxWeight).ToString();

                OverrideBoob.isOn = item.overrideBoob;
                MinBoob.text = (1 + item.MinBoob).ToString();
                MaxBoob.text = (1 + item.MaxBoob).ToString();

                OverrideDick.isOn = item.overrideDick;
                MinDick.text = (1 + item.MinDick).ToString();
                MaxDick.text = (1 + item.MaxDick).ToString();

                WeightText.text = $"Weight 1 - {Math.Max(raceData.BodySizes, 1)}";
                BoobText.text = $"Breasts 1 - {Math.Max(raceData.BreastSizes, 1)}";
                DickText.text = $"Dick 1 - {Math.Max(raceData.DickSizes, 1)}";

                FavoredStat.value = Math.Max(0, Array.IndexOf(WarbandStats.Active, WarbandStats.Canonical(State.RaceSettings.GetFavoredStat(race))));
                FavoredStat.RefreshShownValue();

                var spell = Array.IndexOf(Enum.GetValues(typeof(SpellTypes)), State.RaceSettings.GetInnateSpell(race));
                //if (spell > SpellTypes.Resurrection)
                //    spell = spell - SpellTypes.AlraunePuff + SpellTypes.Resurrection + 1;


                InnateSpellDropdown.value = (int)spell;

                InnateSpellDropdown.RefreshShownValue();

                // Show raw saved values: a default, retired or unknown mapping stays unchanged on Save.
                SelectMapping(SpawnRaceDropdown, item.SpawnRace);
                SelectMapping(ConversionRaceDropdown, item.ConversionRace);
                SelectMapping(LeaderRaceDropdown, item.LeaderRace);
                SelectMapping(MorphRaceDropdown, item.MorphRace);

                BodySize.text = item.BodySize.ToString();
                StomachSize.text = item.StomachSize.ToString();

                CurrentTraits = item.RaceTraits.ToList();
                if (CurrentTraits == null)
                    CurrentTraits = new List<Traits>();
                CurrentTags = item.RaceTags;
                if (CurrentTags == null)
                    CurrentTags = new List<int>();
                OralVoreDisabled.isOn = !item.AllowedVoreTypes.Contains(VoreType.Oral);
                OralVoreDisabled.interactable = racePar.AllowedVoreTypes.Contains(VoreType.Oral);
                UnbirthDisabled.isOn = !item.AllowedVoreTypes.Contains(VoreType.Unbirth);
                UnbirthDisabled.interactable = racePar.AllowedVoreTypes.Contains(VoreType.Unbirth);
                CockVoreDisabled.isOn = !item.AllowedVoreTypes.Contains(VoreType.CockVore);
                CockVoreDisabled.interactable = racePar.AllowedVoreTypes.Contains(VoreType.CockVore);
                AnalVoreDisabled.isOn = !item.AllowedVoreTypes.Contains(VoreType.Anal);
                AnalVoreDisabled.interactable = racePar.AllowedVoreTypes.Contains(VoreType.Anal);
                BreastVoreDisabled.isOn = !item.AllowedVoreTypes.Contains(VoreType.BreastVore);
                BreastVoreDisabled.interactable = racePar.AllowedVoreTypes.Contains(VoreType.BreastVore);
                TailVoreDisabled.isOn = !item.AllowedVoreTypes.Contains(VoreType.TailVore);
                TailVoreDisabled.interactable = racePar.AllowedVoreTypes.Contains(VoreType.TailVore);
                BladderVoreDisabled.isOn = !item.AllowedVoreTypes.Contains(VoreType.BladderVore);
                BladderVoreDisabled.interactable = racePar.AllowedVoreTypes.Contains(VoreType.BladderVore);

                item.Stats.Consolidate();
                MinStrength.text = item.Stats.Strength.Minimum.ToString();
                MaxStrength.text = (item.Stats.Strength.Minimum + item.Stats.Strength.Roll - 1).ToString();
                MinDexterity.text = item.Stats.Dexterity.Minimum.ToString();
                MaxDexterity.text = (item.Stats.Dexterity.Minimum + item.Stats.Dexterity.Roll - 1).ToString();
                MinEndurance.text = item.Stats.Endurance.Minimum.ToString();
                MaxEndurance.text = (item.Stats.Endurance.Minimum + item.Stats.Endurance.Roll - 1).ToString();
                MinMind.text = item.Stats.Mind.Minimum.ToString();
                MaxMind.text = (item.Stats.Mind.Minimum + item.Stats.Mind.Roll - 1).ToString();
                MinWill.text = item.Stats.Will.Minimum.ToString();
                MaxWill.text = (item.Stats.Will.Minimum + item.Stats.Will.Roll - 1).ToString();
                MinAgility.text = item.Stats.Agility.Minimum.ToString();
                MaxAgility.text = (item.Stats.Agility.Minimum + item.Stats.Agility.Roll - 1).ToString();
                MinVoracity.text = item.Stats.Voracity.Minimum.ToString();
                MaxVoracity.text = (item.Stats.Voracity.Minimum + item.Stats.Voracity.Roll - 1).ToString();
                MinStomach.text = item.Stats.Stomach.Minimum.ToString();
                MaxStomach.text = (item.Stats.Stomach.Minimum + item.Stats.Stomach.Roll - 1).ToString();

                var powerAdj = item.PowerAdjustment;
                if (powerAdj == 0f)
                {
                    powerAdj = racePar.PowerAdjustment;
                }
                PowerAdjustment.text = (powerAdj*100).ToString();

                var depCost = item.DeployCost;
                if (depCost == 0f)
                {
                    depCost = racePar.DeployCost;
                }
                DeployCost.text = depCost.ToString();

                var upMult = item.Upkeep;
                if (upMult == 0f)
                {
                    upMult = racePar.Upkeep;
                }
                Upkeep.text = upMult.ToString();

                FemaleTraits.text = TraitListToText(item.FemaleTraits);
                MaleTraits.text = TraitListToText(item.MaleTraits);
                HermTraits.text = TraitListToText(item.HermTraits);
                SpawnTraits.text = TraitListToText(item.SpawnTraits);
                LeaderTraits.text = TraitListToText(item.LeaderTraits);
            }
            else
                PreviousRace = Race.none;
        }
        finally
        {
            loadingRace = wasLoading;
        }
    }

    public void UpdateInteractable()
    {
        if (!IsEditableSpecies(PreviousRace))
            return;
        var raceData = Races.GetRace(PreviousRace);

        if (raceData == null)
        {
            OverrideGender.isOn = false;
            OverrideFurry.isOn = false;
            return;
        }

        if (raceData.CanBeGender.Count() > 1)
        {
            OverrideGender.interactable = true;
        }
        else
        {
            OverrideGender.interactable = false;
            OverrideGender.isOn = false;
        }

        if (raceData.FurCapable)
        {
            OverrideFurry.interactable = true;
        }
        else
        {
            OverrideFurry.interactable = false;
            OverrideFurry.isOn = false;
        }

        if (raceData.BreastSizes > 0)
        {
            OverrideBoob.interactable = true;
        }
        else
        {
            OverrideBoob.interactable = false;
            OverrideBoob.isOn = false;
        }

        if (raceData.DickSizes > 0)
        {
            OverrideDick.interactable = true;
        }
        else
        {
            OverrideDick.interactable = false;
            OverrideDick.isOn = false;
        }

        if (raceData.BreastSizes > 0)
        {
            OverrideDick.interactable = true;
        }
        else
        {
            OverrideDick.interactable = false;
            OverrideDick.isOn = false;
        }

        if (raceData.AllowedMainClothingTypes?.Count > 0)
        {
            OverrideClothed.interactable = true;
        }
        else
        {
            OverrideClothed.interactable = false;
            OverrideClothed.isOn = false;
        }



        MaleFraction.interactable = OverrideGender.isOn;
        HermFraction.interactable = OverrideGender.isOn;
        FurryFraction.interactable = OverrideFurry.isOn;

        ClothedFraction.interactable = OverrideClothed.isOn;
        MinWeight.interactable = OverrideWeight.isOn;
        MaxWeight.interactable = OverrideWeight.isOn;
        MinBoob.interactable = OverrideBoob.isOn;
        MaxBoob.interactable = OverrideBoob.isOn;
        MinDick.interactable = OverrideDick.isOn;
        MaxDick.interactable = OverrideDick.isOn;


        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Traits:");
        if (CurrentTraits == null)
            CurrentTraits = new List<Traits>();
        foreach (Traits trait in CurrentTraits)
        {   
            if (State.RandomizeLists.Any(rl => (Traits)rl.id == trait))
            {
                sb.AppendLine(State.RandomizeLists.Where(rl => (Traits)rl.id == trait).FirstOrDefault().name);
            } 
            else if (State.CustomTraitList.Any(ct => (Traits)ct.id == trait))
            {
                sb.AppendLine(State.CustomTraitList.Where(ct => (Traits)ct.id == trait).FirstOrDefault().name);
            } 
            else if (State.ConditionalTraitList.Any(ct => (Traits)ct.id == trait))
            {
                sb.AppendLine(State.ConditionalTraitList.Where(ct => (Traits)ct.id == trait).FirstOrDefault().name);
            } 
            else
                sb.AppendLine(trait.ToString());
        }
        TraitList.text = sb.ToString();

        sb = new StringBuilder();
        sb.AppendLine("Race Tags:");
        if (CurrentTags == null)
            CurrentTags = new List<int>();
        foreach (int unitTag in CurrentTags)
        {
            UnitTag tag = State.UnitTagList.FirstOrDefault(ct => ct.id == unitTag);
            if (tag != null) sb.AppendLine(tag.name);
        }
        TagList.text = sb.ToString();
    }

    public void CloseAndSave()
    {
        SaveRace();
        if (!LastSaveSucceeded) return;
        try
        {
            BalanceAuthoring.SaveDraft();
            BalanceAuthoring.UpdateStatus();
            gameObject.SetActive(false);
        }
        catch (Exception ex) { State.GameManager.CreateMessageBox("Balance draft was not saved. " + ex.Message); }
    }

    public void CloseWithoutSave()
    {
        BalanceAuthoring.CancelEditorSession();
        gameObject.SetActive(false);
    }

    public void ResetThisRace()
    {
        if (!IsEditableSpecies(PreviousRace))
            return;
        BalanceAuthoring.ResetSpecies(PreviousRace);
        LoadRace();
        UpdateInteractable();
    }

    public void ResetAllRaces()
    {
        BalanceAuthoring.UseOfficial();
        BalanceAuthoring.BeginDraft();
        LoadRace();
        UpdateInteractable();
    }
    public void ModifyUnitTags()
    {
        State.GameManager.Menu.OpenUnitTags();
    }

    public void ActivateGeneral()
    {
        GeneralPanel.SetActive(true);
        TraitsPanel.SetActive(false);
        UnitTagsPanel.SetActive(false);
        TaggedTraitsPanel.SetActive(false);
        GeneralButton.interactable = false;
        TaggedTraitsButton.interactable = false;
        TraitsButton.interactable = true;
        UnitTagsButton.interactable = true;
    }

    public void ActivateTraits()
    {
        GeneralPanel.SetActive(false);
        TraitsPanel.SetActive(true);
        TaggedTraitsPanel.SetActive(false);
        UnitTagsPanel.SetActive(false);
        GeneralButton.interactable = true;
        TraitsButton.interactable = false;
        TaggedTraitsButton.interactable = true;
        UnitTagsButton.interactable = true;
    }
    public void ActivateTags()
    {
        GeneralPanel.SetActive(false);
        TraitsPanel.SetActive(false);
        TaggedTraitsPanel.SetActive(false);
        UnitTagsPanel.SetActive(true);
        GeneralButton.interactable = true;
        TraitsButton.interactable = true;
        TaggedTraitsButton.interactable = true;
        UnitTagsButton.interactable = false;
    }
    public void ActivateTaggedTraitsPanel()
    {
        GeneralPanel.SetActive(false);
        TraitsPanel.SetActive(false);
        UnitTagsPanel.SetActive(false);
        UnitTagsPanel.SetActive(false);
        if (!SpeciesPolicy.TryParseName(RaceDropdown.options[RaceDropdown.value].text, out Race race)) return;
        taggedTraitEditor.Open(race, CurrentTraits);
        GeneralButton.interactable = false;
        TraitsButton.interactable = false;
        TaggedTraitsButton.interactable = false;
        UnitTagsButton.interactable = true;
    }
    public void CloseTaggedTraitsPanel()
    {
        GeneralPanel.SetActive(false);
        TraitsPanel.SetActive(true);
        UnitTagsPanel.SetActive(false);
        CurrentTraits = taggedTraitEditor.Close();
        GeneralButton.interactable = true;
        TraitsButton.interactable = false;
        TaggedTraitsButton.interactable = true;
        UnitTagsButton.interactable = true;
        UpdateInteractable();
    }
}
    
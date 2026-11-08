// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-06: Publish fully initialized immutable species records for cheap detached gameplay snapshots.
// 2026-11-06: Preserve generated species balance and Unit Tag definitions per character (spec 40A).
// See CHANGES.md and License.txt for GPL-3.0 terms.
using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
// Immutable after assignment to Unit.SpeciesBalance. Replace the record for later changes;
// do not mutate its settings or tag definitions during gameplay/background serialization.
internal sealed class UnitSpeciesBalance
{
    public Race Species;
    public string Revision;
    public bool Developer;
    public RaceSettings Settings;
    public List<UnitTag> UnitTags;
    public List<SpellTypes> BaseInnateSpells;

    internal string Fingerprint
    {
        get
        {
            var profile = new BalanceProfile { Revision = Revision, ModifiedDate = "2026-11-06", UnitTags = UnitTags,
                Species = new List<SpeciesBalanceEntry> { new SpeciesBalanceEntry { Species = Species, Settings = Settings.GetExisting(Species) } } };
            return BalanceAuthoring.Hash(BalanceAuthoring.Write(profile) + Developer + string.Join(",", BaseInnateSpells.Select(s => s.ToString()).ToArray()));
        }
    }
    internal static UnitSpeciesBalance Capture(Race race, BalanceProfile source = null)
    {
        var settings = source == null ? State.RaceSettings : BalanceAuthoring.Settings(source);
        var item = BalanceAuthoring.Copy(settings.GetExisting(race) ?? new RaceSettingsItem(race, false));
        var snapshot = new RaceSettings(false);
        snapshot.Set(race, item);
        return new UnitSpeciesBalance
        {
            Species = race, Settings = snapshot,
            Revision = source == null ? BalanceAuthoring.OfficialRevision : source.Revision,
            Developer = source == null && BalanceAuthoring.IsDraft,
            UnitTags = BalanceAuthoring.Copy(source == null ? State.UnitTagList : source.UnitTags),
            BaseInnateSpells = new List<SpellTypes>(RaceParameters.GetRaceTraits(race).InnateSpells ?? new List<SpellTypes>())
        };
    }
    internal static void EnrollLegacy(Unit unit)
    {
        if (unit.SpeciesBalance != null || !BalanceAuthoring.HasMetadata(unit.Race)) return;
        var balance = Capture(unit.Race, BalanceAuthoring.LegacyProfile());
        // Existing saved trait assignments can differ from the original species baseline.
        // Preserve that observed set without treating temporary/shared effects as innate.
        var item = balance.Settings.GetExisting(unit.Race);
        item.RaceTraits = unit.StoredSpeciesTraits();
        item.MaleTraits.Clear(); item.FemaleTraits.Clear(); item.HermTraits.Clear();
        item.LeaderTraits.Clear(); item.SpawnTraits.Clear();
        unit.SpeciesBalance = balance;
    }
    internal RaceTraits Metadata()
    {
        var metadata = RaceParameters.GetRaceTraits(Species).Copy();
        var item = Settings.GetExisting(Species);
        metadata.BodySize = item.BodySize; metadata.StomachSize = item.StomachSize;
        metadata.RacialTraits = item.RaceTraits; metadata.RacialTags = item.RaceTags;
        metadata.LeaderTraits = item.LeaderTraits; metadata.SpawnTraits = item.SpawnTraits;
        metadata.FavoredStat = item.FavoredStat; metadata.RaceAI = item.RaceAI;
        metadata.RaceStats = item.Stats; metadata.PowerAdjustment = item.PowerAdjustment;
        metadata.Upkeep = item.Upkeep;
        metadata.AllowedVoreTypes = item.AllowedVoreTypes;
        metadata.InnateSpells = BaseInnateSpells;
        metadata.CanUseMeleeWeapons = item.CanUseMeleeWeapons ?? metadata.CanUseMeleeWeapons;
        metadata.CanUseRangedWeapons = item.CanUseRangedWeapons ?? metadata.CanUseRangedWeapons;
        return metadata;
    }
}

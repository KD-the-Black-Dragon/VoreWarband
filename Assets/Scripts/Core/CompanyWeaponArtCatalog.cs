// Added by the VoreWar mercenary-company conversion.
// 2026-11-08: Define shared campaign weapon artwork and its developer preview catalog.
// 2026-11-08: Include curated species-specific weapon poses in the developer catalog.
// 2026-11-08: Discover exact company weapon sprites from every retained species renderer.
// See CHANGES.md for the fork's change record and License.txt for GPL-3.0 terms.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

internal static class CompanyWeaponArtCatalog
{
    internal const int LightMelee = 0;
    internal const int HeavyMelee = 2;
    internal const int LightRanged = 4;
    internal const int HeavyRanged = 6;
    internal const int AlternateLightMelee = 8;

    internal sealed class Entry
    {
        internal readonly int Graphic;
        internal readonly string Name;

        internal Entry(int graphic, string name)
        {
            Graphic = graphic;
            Name = name;
        }
    }

    internal static readonly Entry[] Entries =
    {
        new Entry(LightMelee, "Light melee"),
        new Entry(HeavyMelee, "Heavy melee"),
        new Entry(LightRanged, "Light ranged"),
        new Entry(HeavyRanged, "Heavy ranged"),
        new Entry(AlternateLightMelee, "Alternate light melee")
    };

    internal sealed class ArtReference
    {
        internal readonly int Number;
        internal readonly Race Species;
        internal readonly int Graphic;
        internal readonly string Form;
        internal readonly Sprite Idle;
        internal readonly Sprite Attack;

        internal ArtReference(int number, Race species, int graphic, string form, Sprite idle, Sprite attack)
        {
            Number = number;
            Species = species;
            Graphic = graphic;
            Form = form;
            Idle = idle;
            Attack = attack;
        }
    }

    internal sealed class Discovery
    {
        internal readonly List<ArtReference> References = new List<ArtReference>();
        internal readonly List<string> NoWeaponArt = new List<string>();
        internal readonly List<string> Errors = new List<string>();
    }

    sealed class SpritePair
    {
        internal Sprite Idle;
        internal Sprite Attack;
    }

    static readonly FieldInfo ActorMode = typeof(Actor_Unit).GetField("mode", BindingFlags.Instance | BindingFlags.NonPublic);

    internal static Discovery Discover()
    {
        var result = new Discovery();
        foreach (Race race in Enum.GetValues(typeof(Race)).Cast<Race>().Where(r => r != Race.none && !SpeciesPolicy.IsRetired(r)).OrderBy(r => (int)r))
        {
            try
            {
                var raceData = Races.GetRace(race);
                if (raceData == null || raceData.Weapon == null || raceData.Weapon.GetSprite == null)
                {
                    result.NoWeaponArt.Add(SpeciesPolicy.DisplayName(race));
                    continue;
                }

                var withBreasts = RenderVariant(race, raceData, true, result.Errors);
                var withoutBreasts = RenderVariant(race, raceData, false, result.Errors);
                bool hasDifferentForms = Enumerable.Range(0, Entries.Length).Any(i =>
                    withBreasts[i].Idle != withoutBreasts[i].Idle || withBreasts[i].Attack != withoutBreasts[i].Attack);
                bool anyArt = withBreasts.Concat(withoutBreasts).Any(pair => pair.Idle != null || pair.Attack != null);
                if (!anyArt)
                {
                    result.NoWeaponArt.Add(SpeciesPolicy.DisplayName(race));
                    continue;
                }

                AddReferences(result, race, withBreasts, hasDifferentForms ? "With breasts" : string.Empty);
                if (hasDifferentForms)
                    AddReferences(result, race, withoutBreasts, "Without breasts");
            }
            catch (Exception exception)
            {
                result.Errors.Add(SpeciesPolicy.DisplayName(race) + " — " + exception.GetType().Name);
            }
        }
        return result;
    }

    static SpritePair[] RenderVariant(Race race, DefaultRaceData raceData, bool hasBreasts, List<string> errors)
    {
        var unit = new Unit(race);
        unit.CompanyLoadout = true;
        unit.SetDefaultBreastSize(hasBreasts ? 0 : -1);
        unit.Items = new Item[9];
        var actor = new Actor_Unit(unit);
        var previews = new SpritePair[Entries.Length];

        for (int i = 0; i < Entries.Length; i++)
        {
            var weapon = new Weapon("Weapon art preview", string.Empty, 0, Entries[i].Graphic, 0, 1);
            unit.Items[0] = weapon;
            actor.BestMelee = weapon;
            actor.BestRanged = null;
            try
            {
                SetAttacking(actor, false);
                var idle = GetWeaponPreview(raceData.Weapon, actor);
                SetAttacking(actor, true);
                var attack = GetWeaponPreview(raceData.Weapon, actor);
                SetAttacking(actor, false);
                previews[i] = new SpritePair { Idle = idle, Attack = attack };
            }
            catch (Exception exception)
            {
                SetAttacking(actor, false);
                previews[i] = new SpritePair();
                errors.Add(SpeciesPolicy.DisplayName(race) + " graphic " + Entries[i].Graphic +
                    (hasBreasts ? " with breasts" : " without breasts") + " — " + exception.GetType().Name);
            }
        }
        return previews;
    }

    static Sprite GetWeaponPreview(SpriteExtraInfo weaponLayer, Actor_Unit actor)
    {
        int originalLayer = weaponLayer.layer;
        float originalXOffset = weaponLayer.XOffset;
        float originalYOffset = weaponLayer.YOffset;
        try
        {
            return weaponLayer.GetSprite(actor);
        }
        finally
        {
            weaponLayer.layer = originalLayer;
            weaponLayer.XOffset = originalXOffset;
            weaponLayer.YOffset = originalYOffset;
        }
    }

    static void SetAttacking(Actor_Unit actor, bool attacking)
    {
        if (ActorMode == null) return;
        ActorMode.SetValue(actor, Enum.Parse(ActorMode.FieldType, attacking ? "Attacking" : "None"));
    }

    static void AddReferences(Discovery result, Race race, SpritePair[] previews, string form)
    {
        for (int i = 0; i < previews.Length; i++)
        {
            if (previews[i].Idle == null && previews[i].Attack == null) continue;
            result.References.Add(new ArtReference(result.References.Count + 1, race,
                Entries[i].Graphic, form, previews[i].Idle, previews[i].Attack));
        }
    }
}

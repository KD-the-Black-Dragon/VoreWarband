// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-06: Resolve species AP from official or explicitly selected draft balance (spec 40A).
// 2026-11-04: Apply approved injury tiers, base-STR HP, healing ceilings and recovery rules (spec 33).
// 2026-11-03: Four active stats, compatibility mapping, fixed species AP and combat tuning (spec 29).
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
using System;
using System.Linq;
using UnityEngine;

internal static class WarbandStats
{
    // Keep old numeric IDs for existing serialized units, gear and developer overrides.
    internal static readonly Stat[] Active = { Stat.Strength, Stat.Agility, Stat.Intelligence, Stat.Voracity };
    internal static Stat Canonical(Stat stat)
    {
        switch (stat)
        {
            case Stat.Endurance: return Stat.Strength;
            case Stat.Dexterity: return Stat.Agility;
            case Stat.Will: return Stat.Intelligence;
            case Stat.Stomach: return Stat.Voracity;
            default: return stat;
        }
    }
    internal static Stat[] Sources(Stat stat)
    { return Enumerable.Range(0, 8).Select(i => (Stat)i).Where(s => Canonical(s) == Canonical(stat)).ToArray(); }
    internal static string Name(Stat stat)
    { stat = Canonical(stat); return stat == Stat.Intelligence ? "Intelligence" : stat.ToString(); }
    internal static string ShortName(Stat stat)
    {
        switch (Canonical(stat))
        {
            case Stat.Strength: return "STR";
            case Stat.Agility: return "AGI";
            case Stat.Intelligence: return "INT";
            case Stat.Voracity: return "VOR";
            default: return "";
        }
    }
    internal static int BaseAP(Race race)
    {
        return State.RaceSettings == null ? BuiltinAP(race) : State.RaceSettings.GetBaseAP(race);
    }
    internal static int BuiltinAP(Race race)
    {
        // Seed for initial migration and unsupported legacy species.
        return race == Race.Avians || race == Race.Lizards || race == Race.SpitterSlugs ? 2 : 3;
    }
    internal static int Average(int a, int b) { return Math.Max(1, (int)Math.Round((a + b) / 2.0)); }
    internal static void Convert(int[] stats)
    {
        int strength = Math.Max(1, (int)Math.Round((stats[0] + 2.0 * stats[6]) / 3));
        int agility = Average(stats[1], stats[3]);
        int intelligence = Math.Max(1, (int)Math.Round((stats[5] + 2.0 * stats[4]) / 3));
        int voracity = Average(stats[2], stats[7]);
        for (int i = 0; i < stats.Length; i++) stats[i] = 0;
        stats[0] = strength; stats[3] = agility; stats[5] = intelligence; stats[2] = voracity;
    }
    // Provisional, bounded critical chance: insight benefits any weapon build.
    internal static float CriticalChance(Unit attacker)
    { return Mathf.Clamp(.05f + attacker.GetStat(Stat.Intelligence) * .0025f, .05f, .35f); }
    internal static string Description(Stat stat)
    {
        switch (Canonical(stat))
        {
            case Stat.Strength: return "Melee accuracy and damage, and resistance to digestion damage. Base STR determines maximum HP; combat STR modifiers do not.";
            case Stat.Agility: return "Ranged accuracy and damage, melee/ranged evasion, and vore evasion. Does not increase AP.";
            case Stat.Intelligence: return "Magic accuracy, damage and effects, maximum mana, magic resistance, escape chance while swallowed, and critical chance.";
            case Stat.Voracity: return "Swallow chance, stomach capacity, digestion speed, and keeping prey from escaping.";
            default: return "";
        }
    }
}

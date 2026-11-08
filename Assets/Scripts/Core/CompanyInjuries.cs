// Modified for the VoreWar mercenary-company conversion.
// 2026-11-07: Add new battle injuries to post-battle event evidence (spec 40D).
// 2026-11-05: Set ordinary/field-rest/town-rest HP recovery to 10/20/30 percent (spec 36).
// 2026-11-05: Separate faster general HP recovery from longer calendar-day injuries.
// See CHANGES.md and License.txt for GPL-3.0 terms.
// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-04: Add isolated password-gated developer battle/recovery controls (spec 34 slice 2).
// 2026-11-01: Persistent injuries, recovery, treatment, sparring and tactical announcements (spec 11).
// 2026-11-02: Restore daily mana recovery through the company/target daily stage (spec 12).
// 2026-11-03: Implement four active stats (spec 29).
// 2026-11-04: Implement approved body-part injuries, saved complication rolls and tiered old injuries (spec 33).
// See CHANGES.md and License.txt for GPL-3.0 terms; upstream notices remain applicable.
using System;
using System.Collections.Generic;
using System.Linq;

// Preserve legacy serialized numbers; Chest replaces the old Torso name.
internal enum CompanyBodyPart { Hand = 0, Arm = 1, Leg = 2, Head = 3, Chest = 4, Eyes = 5, Abdomen = 6, Knee = 7 }
internal enum CompanyInjuryGrade { Bruised = 0, Mangled = 1, Old = 2, Battered = 3 }
internal enum CompanyComplication { BodyPart, PoorFocus, DulledInstincts, SluggishRecovery, SensitiveStomach }
[Serializable]
internal sealed class CompanyInjury
{
    public string Id = Guid.NewGuid().ToString("N");
    public CompanyBodyPart Part;
    public CompanyInjuryGrade Grade;
    public int DaysRemaining;
    public bool Treated;
    public int Tier = 1;
    public CompanyComplication Complication;
    public double RecoveryRoll = -1;
}

internal static class CompanyInjuries
{
    internal const int MinorDays = 5, ModerateDays = 14, SeriousDays = 28, TreatmentCost = 10, TreatmentDays = 2;
    internal const double MinimumHitFraction = .10, ModerateDailyRisk = .05, SevereDailyRisk = .10;
    internal static bool Temporary(CompanyInjury injury) => injury.Grade != CompanyInjuryGrade.Old;
    internal static bool Leader(Unit unit) => unit.PermanentInjuryImmune || unit.Type == UnitType.Leader;
    internal static bool Healthy(Unit unit) => !unit.Digested && unit.LastingHealth >= HealingCeiling(unit) && !unit.Injuries.Any(Temporary);
    internal static int InjuryTier(CompanyInjury injury) => Temporary(injury) ?
        injury.Grade == CompanyInjuryGrade.Bruised ? 1 : injury.Grade == CompanyInjuryGrade.Battered ? 2 : 3 : Math.Max(1, Math.Min(3, injury.Tier));

    internal static int DamageTier(Unit unit)
    {
        double fraction = unit.Health / (double)Math.Max(1, unit.MaxHealth);
        return fraction < .25 ? 3 : fraction < .50 ? 2 : fraction < .75 ? 1 : 0;
    }
    internal static string DamageName(Unit unit) => new[] { "", "Light Damage", "Moderate Damage", "Severe Damage" }[DamageTier(unit)];
    internal static string DamageDescription(Unit unit) => DamageTier(unit) == 0 ? "No accumulated-damage penalty" :
        DamageName(unit) + " — all four combat stats reduced by " + DamageTier(unit) * 10 + "%. Temporary HP counts; normal maximum HP is unchanged.";

    internal static void RecordDamage(Unit unit, int lastingDamage)
    {
        if (!unit.BattleActive || unit.Digested || unit.Type == UnitType.Summon || lastingDamage <= 0) return;
        double fraction = Math.Min(1, lastingDamage / (double)Math.Max(1, unit.MaxHealth));
        if (fraction < MinimumHitFraction) return;
        var part = RandomPart(unit);
        double chance = Math.Min(.90, .10 + fraction - MinimumHitFraction) * RiskMultiplier(unit, part);
        if (State.Rand.NextDouble() >= chance) return;
        var grade = fraction >= .50 ? CompanyInjuryGrade.Mangled : fraction >= .25 ? CompanyInjuryGrade.Battered : CompanyInjuryGrade.Bruised;
        Add(unit, part, grade);
    }
    static CompanyBodyPart RandomPart(Unit unit)
    {
        bool animal = SpeciesPolicy.IsCurated(unit.Race) && SpeciesPolicy.GetKind(unit.Race) == SpeciesKind.Animal;
        return animal ? (State.Rand.Next(2) == 0 ? CompanyBodyPart.Head : CompanyBodyPart.Chest) : (CompanyBodyPart)State.Rand.Next(8);
    }
    internal static double RiskMultiplier(Unit unit, CompanyBodyPart part)
    {
        CompanySlot slot = part == CompanyBodyPart.Head || part == CompanyBodyPart.Eyes ? CompanySlot.Helmet :
            part == CompanyBodyPart.Chest || part == CompanyBodyPart.Abdomen ? CompanySlot.Torso :
            part == CompanyBodyPart.Leg || part == CompanyBodyPart.Knee ? CompanySlot.Legs : CompanySlot.Arms;
        return 1.0 / (1 + CompanyEquipment.Protection(unit, slot));
    }
    static void Announce(Unit unit, string message, string popup)
    {
        var mode = State.GameManager == null ? null : State.GameManager.TacticalMode;
        if (mode == null) return;
        mode.Log?.RegisterMiscellaneous(unit.Name + " " + message);
        if (mode.turboMode || TacticalUtilities.Units == null) return;
        var actor = TacticalUtilities.GetActorOf(unit);
        if (actor != null && actor.Visible && actor.UnitSprite != null) actor.UnitSprite.DisplayInjury(popup);
    }
    internal static void Add(Unit unit, CompanyBodyPart part, CompanyInjuryGrade grade)
    {
        var injury = new CompanyInjury { Part = part, Grade = grade,
            DaysRemaining = grade == CompanyInjuryGrade.Bruised ? MinorDays : grade == CompanyInjuryGrade.Battered ? ModerateDays : SeriousDays,
            RecoveryRoll = State.Rand.NextDouble() };
        unit.Injuries.Add(injury);
        CompanyPostBattleTraits.RecordInjury(unit);
        Announce(unit, "suffered " + Description(injury) + ".", Name(injury));
    }
    static IEnumerable<CompanyInjury> At(Unit unit, CompanyBodyPart part) => unit.Injuries.Where(i => i.Complication == CompanyComplication.BodyPart && i.Part == part);
    static float Percent(CompanyInjury injury, float[] recovering, float[] old) => (Temporary(injury) ? recovering : old)[InjuryTier(injury) - 1];
    static readonly float[] StatTemporary = { .20f, .40f, .60f }, StatOld = { .10f, .15f, .20f };
    static readonly float[] AccuracyTemporary = { .10f, .20f, .30f }, AccuracyOld = { .05f, .10f, .15f };
    internal static float StatMultiplier(Unit unit, Stat stat)
    {
        var part = stat == Stat.Strength ? CompanyBodyPart.Arm : stat == Stat.Agility ? CompanyBodyPart.Leg :
            stat == Stat.Intelligence ? CompanyBodyPart.Head : CompanyBodyPart.Abdomen;
        float penalty = DamageTier(unit) * .10f + At(unit, part).Sum(i => Percent(i, StatTemporary, StatOld));
        return Math.Max(0, 1 - penalty);
    }
    internal static float AccuracyPenalty(Unit unit) => At(unit, CompanyBodyPart.Eyes).Sum(i => Percent(i, AccuracyTemporary, AccuracyOld));
    internal static int MovementPenalty(Unit unit) => At(unit, CompanyBodyPart.Knee).Sum(InjuryTier);
    internal static int DamagePenalty(Unit unit) => At(unit, CompanyBodyPart.Hand).Sum(i =>
        Temporary(i) ? new[] { 1, 2, 4 }[InjuryTier(i) - 1] : InjuryTier(i));
    internal static float HealingCeilingFraction(Unit unit) => Math.Max(0, 1 - At(unit, CompanyBodyPart.Chest).Sum(i => Percent(i, AccuracyTemporary, AccuracyOld)));
    internal static int HealingCeiling(Unit unit) => Math.Max(1, (int)Math.Floor(unit.MaxHealth * HealingCeilingFraction(unit) + .0001));
    static int OldTier(Unit unit, CompanyComplication kind) => unit.Injuries.Where(i => !Temporary(i) && i.Complication == kind).Select(InjuryTier).DefaultIfEmpty(0).Max();
    internal static float ManaMultiplier(Unit unit) => 1 - OldTier(unit, CompanyComplication.PoorFocus) * .05f;
    internal static float CriticalPenalty(Unit unit) => OldTier(unit, CompanyComplication.DulledInstincts) * .05f;
    internal static int RecoveryPenalty(Unit unit) => OldTier(unit, CompanyComplication.SluggishRecovery);
    internal static float EscapeBonus(Unit unit) => OldTier(unit, CompanyComplication.SensitiveStomach) * .05f;
    internal static string Name(CompanyInjury injury)
    {
        if (Temporary(injury)) return injury.Grade + " " + injury.Part;
        string name;
        switch (injury.Complication)
        {
            case CompanyComplication.PoorFocus: name = "Poor Focus"; break;
            case CompanyComplication.DulledInstincts: name = "Dulled Instincts"; break;
            case CompanyComplication.SluggishRecovery: name = "Sluggish Recovery"; break;
            case CompanyComplication.SensitiveStomach: name = "Sensitive Stomach"; break;
            default: name = new[] { "Weak Grip", "Stiff Arm", "Leg Pains", "Frequent Headaches", "Shortened Breath", "Blurred Vision", "Poor Digestion", "Weak Knees" }[(int)injury.Part]; break;
        }
        return name + " " + new[] { "I", "II", "III" }[InjuryTier(injury) - 1];
    }
    internal static string Description(CompanyInjury injury)
    {
        int tier = InjuryTier(injury); string effect;
        if (injury.Complication == CompanyComplication.PoorFocus) effect = "Maximum mana -" + tier * 5 + "%";
        else if (injury.Complication == CompanyComplication.DulledInstincts) effect = "Critical chance -" + tier * 5 + " percentage points";
        else if (injury.Complication == CompanyComplication.SluggishRecovery) effect = "Daily HP recovery -" + tier + " (minimum 1)";
        else if (injury.Complication == CompanyComplication.SensitiveStomach) effect = "Swallowed enemy escape chance +" + tier * 5 + " percentage points";
        else
        {
            switch (injury.Part)
            {
                case CompanyBodyPart.Head: effect = "INT"; break;
                case CompanyBodyPart.Arm: effect = "STR"; break;
                case CompanyBodyPart.Leg: effect = "AGI"; break;
                case CompanyBodyPart.Abdomen: effect = "VOR"; break;
                case CompanyBodyPart.Eyes: effect = "Melee, ranged, magic and vore accuracy -" + Math.Round(100 * Percent(injury, AccuracyTemporary, AccuracyOld)) + " percentage points"; goto Done;
                case CompanyBodyPart.Hand: effect = "Melee, ranged and spell damage -" + (Temporary(injury) ? new[] { 1, 2, 4 }[tier - 1] : tier); goto Done;
                case CompanyBodyPart.Chest: effect = "Healing ceiling -" + Math.Round(100 * Percent(injury, AccuracyTemporary, AccuracyOld)) + "% of normal maximum HP"; goto Done;
                default: effect = "AP per turn -" + tier + " (minimum 1)"; goto Done;
            }
            effect += " -" + Math.Round(100 * Percent(injury, StatTemporary, StatOld)) + "%";
        }
        Done:
        return Name(injury) + " — " + effect + (Temporary(injury) ? " | Recovery remaining: " + injury.DaysRemaining + (injury.Treated ? " (treated)" : "") : " | Permanent");
    }
    internal static string Summary(Unit unit)
    {
        int temporary = unit.Injuries.Count(Temporary), old = unit.Injuries.Count - temporary;
        return (DamageTier(unit) == 0 ? "" : DamageName(unit) + " | ") +
            (unit.Injuries.Count == 0 ? "No body-part or old injuries" : temporary + " recovering, " + old + " old injuries") +
            (HealingCeiling(unit) < unit.MaxHealth ? " | Healing ceiling " + HealingCeiling(unit) + "/" + unit.MaxHealth : "");
    }
    internal static void AddOld(Unit unit, CompanyBodyPart part, CompanyComplication kind = CompanyComplication.BodyPart)
    {
        if (Leader(unit)) return;
        var old = unit.Injuries.FirstOrDefault(i => !Temporary(i) && i.Complication == kind && (kind != CompanyComplication.BodyPart || i.Part == part));
        if (old == null) { old = new CompanyInjury { Grade = CompanyInjuryGrade.Old, Part = part, Complication = kind, Tier = 1, RecoveryRoll = 0 }; unit.Injuries.Add(old); }
        else old.Tier = Math.Min(3, InjuryTier(old) + 1);
        unit.ClampInjuryMana();
    }
    internal static void DailyComplication(Unit unit)
    {
        int tier = DamageTier(unit);
        if (Leader(unit) || unit.Digested || unit.Type == UnitType.Summon || tier < 2 || State.Rand.NextDouble() >= (tier == 2 ? ModerateDailyRisk : SevereDailyRisk)) return;
        int choice = State.Rand.Next(11);
        if (choice < 7) AddOld(unit, new[] { CompanyBodyPart.Head, CompanyBodyPart.Eyes, CompanyBodyPart.Arm, CompanyBodyPart.Hand, CompanyBodyPart.Chest, CompanyBodyPart.Abdomen, CompanyBodyPart.Leg }[choice]);
        else AddOld(unit, CompanyBodyPart.Head, (CompanyComplication)(choice - 6));
    }
    internal static void Recover(Unit unit, int progress)
    {
        foreach (var injury in unit.Injuries.Where(Temporary).ToArray())
        {
            injury.DaysRemaining = Math.Max(0, injury.DaysRemaining - progress);
            if (injury.DaysRemaining > 0) continue;
            double risk = injury.Grade == CompanyInjuryGrade.Bruised ? 0 : injury.Grade == CompanyInjuryGrade.Battered ? (injury.Treated ? .17 : .33) : (injury.Treated ? .33 : .66);
            unit.Injuries.Remove(injury);
            if (injury.RecoveryRoll < risk) AddOld(unit, injury.Part);
        }
    }
    internal static bool CanTreat(Unit unit) => unit.Injuries.Any(i => Temporary(i) && !i.Treated && i.DaysRemaining > 0);
    internal static void Treat(CompanyCampaign state, string id)
    {
        var c = state.Characters.Single(x => x.Id == id);
        if (state.Busy || state.Preview || !state.Towns.Any(t => t.Tile == state.Position) || !state.Members.Any(m => m.CharacterId == id) || state.Gold < TreatmentCost || !CanTreat(c.Unit))
            throw new InvalidOperationException("Visit town with enough gold and an untreated temporary injury.");
        state.Gold -= TreatmentCost;
        TreatInjuries(c.Unit);
    }
    internal static void TreatInjuries(Unit unit)
    {
        foreach (var injury in unit.Injuries.Where(i => Temporary(i) && !i.Treated && i.DaysRemaining > 0))
        { injury.DaysRemaining = Math.Max(1, injury.DaysRemaining - TreatmentDays); injury.Treated = true; }
    }
    internal static void Validate(Unit unit)
    {
        var ids = new HashSet<string>();
        foreach (var i in unit.Injuries)
        {
            if (i == null || string.IsNullOrEmpty(i.Id) || !ids.Add(i.Id) || !Enum.IsDefined(typeof(CompanyBodyPart), i.Part) ||
                !Enum.IsDefined(typeof(CompanyInjuryGrade), i.Grade) || !Enum.IsDefined(typeof(CompanyComplication), i.Complication) || i.DaysRemaining < 0 || i.DaysRemaining > SeriousDays ||
                (Temporary(i) && i.DaysRemaining == 0) || (!Temporary(i) && (i.DaysRemaining != 0 || Leader(unit))) || i.Tier < 0 || i.Tier > 3)
                throw new InvalidOperationException("Invalid persistent injury state.");
            if (i.Tier == 0) i.Tier = 1; // Legacy old injuries had no tier.
            if (i.RecoveryRoll < 0) // Legacy records receive a stable value, never a load-time random reroll.
            { uint hash = 2166136261; foreach (char c in i.Id) hash = unchecked((hash ^ c) * 16777619); i.RecoveryRoll = hash / ((double)uint.MaxValue + 1); }
            if (double.IsNaN(i.RecoveryRoll) || double.IsInfinity(i.RecoveryRoll) || i.RecoveryRoll < 0 || i.RecoveryRoll >= 1) throw new InvalidOperationException("Invalid injury recovery roll.");
        }
        foreach (var group in unit.Injuries.Where(i => !Temporary(i)).GroupBy(i => i.Complication + ":" + (i.Complication == CompanyComplication.BodyPart ? i.Part.ToString() : "")).ToArray())
        {
            var first = group.First(); first.Tier = Math.Min(3, group.Sum(InjuryTier));
            foreach (var extra in group.Skip(1).ToArray()) unit.Injuries.Remove(extra);
        }
    }
    internal static bool Same(Unit first, Unit second) => first.PermanentInjuryImmune == second.PermanentInjuryImmune && first.Injuries.Count == second.Injuries.Count &&
        first.Injuries.Zip(second.Injuries, (a, b) => a.Id == b.Id && a.Part == b.Part && a.Grade == b.Grade && a.DaysRemaining == b.DaysRemaining &&
            a.Treated == b.Treated && a.Tier == b.Tier && a.Complication == b.Complication && a.RecoveryRoll == b.RecoveryRoll).All(equal => equal);
}

internal sealed class CompanyDailyRecovery : ICompanyDailySystem
{
    internal const double OrdinaryRate = .10, FieldRestRate = .20, TownRestRate = .30;
    internal static double Rate(bool resting, bool town) => !resting ? OrdinaryRate : town ? TownRestRate : FieldRestRate;
    public CompanyDailyStage Stage { get { return CompanyDailyStage.Recovery; } }
    public void Apply(CompanyCampaign state)
    {
        if (state.Preview) return;
        bool town = state.Towns.Any(t => t.Tile == state.Position);
        double rate = Rate(state.RestingDay, town);
        const int progress = 1; // Specific injuries heal in calendar days; rest primarily restores general condition.
        foreach (var member in state.Members)
            Recover(state.Characters.Single(c => c.Id == member.CharacterId).Unit, rate, progress);
        foreach (var contract in state.Contracts.Where(c => c.Status == CompanyContractStatus.Active))
            foreach (var c in CompanyContracts.Survivors(state, contract)) Recover(c.Unit, OrdinaryRate, 1);
    }
    internal static void Recover(Unit unit, double rate, int progress)
    {
        if (unit.Digested || unit.Type == UnitType.Summon) return;
        CompanyInjuries.DailyComplication(unit);
        CompanyInjuries.Recover(unit, progress);
        int healing = Math.Max(1, (int)Math.Ceiling(unit.MaxHealth * rate) - CompanyInjuries.RecoveryPenalty(unit));
        unit.Health += Math.Max(0, Math.Min(healing, unit.HealingCeiling - unit.Health));
        unit.RestoreManaPct(.6f); // Carry forward the original Army.Refresh daily mana rate, without its empire side effects.
    }
}
internal sealed class CompanySparring : ICompanyDailySystem
{
    public CompanyDailyStage Stage { get { return CompanyDailyStage.Sparring; } }
    public void Apply(CompanyCampaign state)
    {
        if (state.Preview) return;
        foreach (var member in state.Members)
        {
            var unit = state.Characters.Single(c => c.Id == member.CharacterId).Unit;
            if (CompanyInjuries.Healthy(unit)) unit.GiveExp(2); // Existing trait XP modifiers and level choices still apply.
        }
    }
}

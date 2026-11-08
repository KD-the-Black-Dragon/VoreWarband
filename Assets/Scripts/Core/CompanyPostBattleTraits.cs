// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-07: Record battle evidence and settle balanced post-battle trait outcomes (spec 40D).
// 2026-11-07: Use the retained Cowardly enum identity for the displayed Opossum trait in outcomes.
// See CHANGES.md for the fork's change record and License.txt for GPL-3.0 terms.
using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
internal sealed class CompanyBattleEvidence
{
    public int StartingMaxHealth;
    public int Injuries;
    public int EscapesOrFreed;
    public int IncomingNormalAttacks;
    public int IncomingOpportunityAttacks;
    public int DamageReceived;
    public int MeleeAttempts, MeleeHits, MeleeCriticals, MeleeDamage, MeleeDowns;
    public int RangedAttempts, RangedHits, RangedCriticals, RangedDamage, RangedDowns;
    public int MagicAttempts, MagicHits, MagicDamage, MagicDowns;
    public int ManaSpent;
    public bool WasDowned;
    public bool WasReducedToZero;
    public bool WasLastActiveCompanyMember;
    public int AdjacentEnemyDigestions;

    internal void RecordWeapon(bool ranged, int result)
    {
        // Grazes deal damage but remain distinct from hits in event hit/critical-rate comparisons.
        if (ranged)
        {
            RangedAttempts++;
            if (result == 1 || result == 2) RangedHits++;
            if (result == 2) RangedCriticals++;
        }
        else
        {
            MeleeAttempts++;
            if (result == 1 || result == 2) MeleeHits++;
            if (result == 2) MeleeCriticals++;
        }
    }

    internal void RecordSpell(bool hit) { MagicAttempts++; if (hit) MagicHits++; }
    internal void RecordIncoming(bool opportunity) { if (opportunity) IncomingOpportunityAttacks++; else IncomingNormalAttacks++; }
    internal void RecordDamage(int amount) { DamageReceived = SaturatingAdd(DamageReceived, amount); }
    internal void RecordDealt(bool ranged, int amount)
    { if (ranged) RangedDamage = SaturatingAdd(RangedDamage, amount); else MeleeDamage = SaturatingAdd(MeleeDamage, amount); }
    internal void RecordMagicDamage(int amount) { MagicDamage = SaturatingAdd(MagicDamage, amount); }
    internal void RecordDown(bool ranged, bool magic)
    { if (magic) MagicDowns++; else if (ranged) RangedDowns++; else MeleeDowns++; }
    internal void RecordInjury() { Injuries++; }
    internal void RecordEscape() { EscapesOrFreed++; }
    internal void RecordMana(int amount) { ManaSpent = SaturatingAdd(ManaSpent, amount); }

    internal bool IsValid()
    {
        return StartingMaxHealth >= 0 && Injuries >= 0 && EscapesOrFreed >= 0 && IncomingNormalAttacks >= 0 && IncomingOpportunityAttacks >= 0 &&
            DamageReceived >= 0 && MeleeAttempts >= 0 && MeleeHits >= 0 && MeleeCriticals >= 0 && MeleeDamage >= 0 && MeleeDowns >= 0 &&
            RangedAttempts >= 0 && RangedHits >= 0 && RangedCriticals >= 0 && RangedDamage >= 0 && RangedDowns >= 0 &&
            MagicAttempts >= 0 && MagicHits >= 0 && MagicDamage >= 0 && MagicDowns >= 0 && ManaSpent >= 0 && AdjacentEnemyDigestions >= 0;
    }

    static int SaturatingAdd(int current, int amount)
    { return (int)Math.Min(int.MaxValue, (long)current + Math.Max(0, amount)); }

    internal static bool Same(CompanyBattleEvidence a, CompanyBattleEvidence b)
    {
        if (a == null || b == null) return a == b;
        return a.StartingMaxHealth == b.StartingMaxHealth && a.Injuries == b.Injuries && a.EscapesOrFreed == b.EscapesOrFreed &&
            a.IncomingNormalAttacks == b.IncomingNormalAttacks && a.IncomingOpportunityAttacks == b.IncomingOpportunityAttacks && a.DamageReceived == b.DamageReceived &&
            a.MeleeAttempts == b.MeleeAttempts && a.MeleeHits == b.MeleeHits && a.MeleeCriticals == b.MeleeCriticals && a.MeleeDamage == b.MeleeDamage && a.MeleeDowns == b.MeleeDowns &&
            a.RangedAttempts == b.RangedAttempts && a.RangedHits == b.RangedHits && a.RangedCriticals == b.RangedCriticals && a.RangedDamage == b.RangedDamage && a.RangedDowns == b.RangedDowns &&
            a.MagicAttempts == b.MagicAttempts && a.MagicHits == b.MagicHits && a.MagicDamage == b.MagicDamage && a.MagicDowns == b.MagicDowns && a.ManaSpent == b.ManaSpent &&
            a.WasDowned == b.WasDowned && a.WasReducedToZero == b.WasReducedToZero && a.WasLastActiveCompanyMember == b.WasLastActiveCompanyMember &&
            a.AdjacentEnemyDigestions == b.AdjacentEnemyDigestions;
    }
}

[Serializable]
internal sealed class CompanyPostBattleEvent
{
    public string SubjectId;
    public string Description;
    public Traits Trait;
    public Traits PreviousTrait;
    public bool HasTraitChange;
    public bool WasDowngrade;
    public bool WasRemoval;

    internal static bool Same(CompanyPostBattleEvent a, CompanyPostBattleEvent b)
    {
        if (a == null || b == null) return a == b;
        return a.SubjectId == b.SubjectId && a.Description == b.Description && a.Trait == b.Trait && a.PreviousTrait == b.PreviousTrait &&
            a.HasTraitChange == b.HasTraitChange && a.WasDowngrade == b.WasDowngrade && a.WasRemoval == b.WasRemoval;
    }
}

internal enum CompanyTraitPolarity { Neutral, Good, Bad }

internal static class CompanyPostBattleTraits
{
    sealed class Participant
    {
        internal string Id;
        internal CompanyCharacter Source, Settled;
        internal Actor_Unit Actor;
        internal CompanyBattleEvidence Evidence { get { return Actor.Evidence; } }
        internal bool IsLeader;
        internal int TraitWeight { get { return TotalWeight(Settled.Unit); } }
    }

    sealed class Occurrence
    {
        internal int Priority; // 0 exceptional, 1 mid-tier, 2 mundane.
        internal string SubjectId;
        internal string[] SubjectIds;
        internal string Description;
        internal Traits[] Good, Bad;
        internal bool LeaderGlory;
    }

    sealed class TraitChange
    {
        internal Traits Before, After;
        internal bool Removal;
        internal bool Change;
    }

    static readonly HashSet<string> GoodNames = Names(@"
        AcidImmunity AcidResistant ArcaneMagistrate Berserk Biter BladeDance Blitz BoggingSlime BornToMove BoundWeapon Bulky ColdTolerance Colossal Crusher Dazzle DefensiveStance DexterousDefense DireInfection DoubleShot DualStomach Duelist EfficientGuts EscapeArtist FastAbsorption FastCaster FastDigestion FasterAbsorption FasterDigestion FearsomeAppetite Fit Finesse FocusedDodge ForcefulBlow Frenzy Fervor GiantSlayer GlueBomb Grounded HardSkin HotBlooded Huge Intimidating InvigoratingEscape IronGut KeenEye KeenReflexes Large LavaWalker MadMage MagicSynthesis ManaBurn ManaDynamo MetabolicSurge MountainWalker NaturalCaster NightEye NimbleClimber PackAgility PackMind PackStrength PackTactics PackVoracity Paralyzer PassThrough PeakCondition Perseverance Petrifier PoisonSpit PollenProjector Pounce PowerCaster QuickLearner RangedVore Ravenous Regeneration Resilient SedativeStomach SenseWeakness SmokeOut SpectralStep SpellBlade SteadyStomach Stinger StretchyInsides StunningStrike SweepingStrikes SwiftStrike TailStrike Talented TasteForBlood Tenacious ThrillSeeker Titanic Toxic Unflinching UnpleasantDigestion Vampirism VenomousBite VenomShock Vindication ViralBiology WaterWalker Webber ManaBattery Hearty KeenShot Dodger HardPrey MagicFinesse Precise WideMaw Assassin");
    static readonly HashSet<string> BadNames = Names(@"
        AwfulAim DesertImpedence Diseased EasyPrey ElecVulnerable EasyToVore Farsighted FireVulnerable FoodComaProne Foolish ForestImpedence GrassImpedence Greedy HighlyAbsorbable HillImpedence IceVulnerable IdealSustenance Illness IntrusiveAppetite Lethargic Nauseous Opossum PleasantDigestion PoorConstitution ProteinRich SlowAbsorption SlowDigestion SlowerAbsorption SlowerDigestion SlowerMetabolism SlowMetabolism Small SnowImpedence SoftBody Spartan Submissive SwampImpedence Timid Tiny VolcanicImpedence Brutish Clumsy ErrantMage Fizzler Flatfooted Frail MagicIneptitude MagicVulnerable Nearsighted RangedIneptitude WeakAttack WeakBones WeakGullet Grudge");
    static readonly HashSet<string> SoftRetiredNames = Names(@"
        AcellularBody AllOutFirstStrike Competitive Crystaline Disgusting EasilySatisfied Elite Elementist Feral GelatinousBody HeavyPounce ManaAttuned ManaBarrier ManaDrain ManaRich PleasurableTouch SerialSwallower SiphoningAura SleepItOff Tasty Tempered WastefulProcessing WeaponChanneler");
    static readonly HashSet<string> InbornNames = Names("Colossal Featherweight Huge Large Small Tiny Titanic");
    static readonly HashSet<Traits> GrudgeTiers = new HashSet<Traits> { Traits.Grudge, Traits.GrudgeII, Traits.GrudgeIII };
    static readonly Traits[] GrudgeFamily = { Traits.Grudge, Traits.GrudgeII, Traits.GrudgeIII };

    internal static CompanyPostBattleEvent Resolve(CompanyCampaign source, CompanyCampaign settled, IList<Actor_Unit> actors)
    {
        if (source == null || settled == null || source.Battle == null || source.Battle.EvidenceVersion != 1 ||
            source.Battle.DeployedIds == null || source.Battle.DeployedIds.Length < 3 || actors == null || settled.Ended)
            return null; // Incomplete/older ledgers never invent past battle events.

        var participants = new List<Participant>();
        foreach (string id in source.Battle.DeployedIds)
        {
            var old = source.Characters.FirstOrDefault(c => c.Id == id);
            var now = settled.Characters.FirstOrDefault(c => c.Id == id);
            var actor = old == null ? null : actors.FirstOrDefault(a => ReferenceEquals(a.Unit, old.Unit));
            if (old == null || now == null || actor == null) continue;
            participants.Add(new Participant { Id = id, Source = old, Settled = now, Actor = actor, IsLeader = id == source.LeaderId });
        }
        var eligible = participants.Where(p => !p.IsLeader && p.Source.Employment == CompanyEmployment.Employed &&
            settled.Members.Any(m => m.CharacterId == p.Id) && !p.Settled.Unit.Digested && !p.Settled.Unit.IsDead).ToList();
        if (eligible.Count == 0) return null; // Includes the leader-as-sole-survivor case.

        List<Occurrence> occurrences = BuildOccurrences(source, participants, eligible);
        for (int priority = 0; priority <= 2; priority++)
        {
            var candidates = occurrences.Where(o => o.Priority == priority).ToList();
            // Draw the occurrence first. An unusable result is discarded; it does not cause a generic fallback.
            // Each occurrence is removed before evaluation, so this search is finite even
            // when a large company creates many candidate events.
            while (candidates.Count > 0)
            {
                int index = settled.Next(candidates.Count);
                var occurrence = candidates[index]; candidates.RemoveAt(index);
                var subjects = occurrence.SubjectId != null ? eligible.Where(p => p.Id == occurrence.SubjectId).ToList() :
                    occurrence.SubjectIds != null ? eligible.Where(p => occurrence.SubjectIds.Contains(p.Id)).ToList() : eligible.ToList();
                while (subjects.Count > 0)
                {
                    Participant subject = ChooseFewerTraits(settled, subjects);
                    subjects.Remove(subject);
                    int direction = ChoosePolarityDirection(settled, subject.Settled.Unit);
                    CompanyTraitPolarity requested = direction > 0 ? CompanyTraitPolarity.Good : CompanyTraitPolarity.Bad;
                    Traits[] options = requested == CompanyTraitPolarity.Good ? occurrence.Good : occurrence.Bad;
                    if (options == null || options.Length == 0) continue;
                    var changed = TryApplyOutcome(settled, subject.Settled.Unit, options, requested, direction, occurrence.LeaderGlory);
                    if (changed == null) continue;
                    var result = new CompanyPostBattleEvent { SubjectId = subject.Id, HasTraitChange = true,
                        Trait = changed.After, PreviousTrait = changed.Before,
                        WasDowngrade = changed.Change && !changed.Removal && TraitWeight(changed.After) < TraitWeight(changed.Before),
                        WasRemoval = changed.Removal };
                    result.Description = Describe(subject.Settled.Unit.Name, occurrence.Description, changed);
                    return result;
                }
            }
        }
        return null;
    }

    static List<Occurrence> BuildOccurrences(CompanyCampaign campaign, List<Participant> participants, List<Participant> eligible)
    {
        var result = new List<Occurrence>();
        var company = participants.Where(p => !p.IsLeader).ToArray();
        int enemyCount = campaign.Battle.InitialEnemyCount;
        var leader = participants.FirstOrDefault(p => p.IsLeader);
        if (enemyCount >= 6)
        {
            int highestDigestions = participants.Max(p => p.Source.Unit.EnemiesDigestedThisBattle);
            if (highestDigestions > enemyCount / 2)
                AddCandidates(result, 0, eligible.Where(p => p.Source.Unit.EnemiesDigestedThisBattle == highestDigestions),
                    "digested an unusually large share of the enemy force", DigestGood, DigestBad);
            if (leader != null && leader.Source.Unit.EnemiesDigestedThisBattle > enemyCount / 2 && eligible.Count > 0)
                AddCandidates(result, 0, eligible, "the company leader digested an unusually large share of the enemy force", null, new[] { Traits.Grudge }, true);
        }

        int highestEscape = participants.Max(p => p.Evidence.EscapesOrFreed);
        if (highestEscape >= 3)
            AddCandidates(result, 0, eligible.Where(p => p.Evidence.EscapesOrFreed == highestEscape),
                "escaped or was freed from vore repeatedly", EscapeGood, EscapeBad);
        foreach (var p in eligible.Where(p => p.Evidence.Injuries >= 3))
            Add(result, 0, p.Id, "suffered three or more new injuries", InjuryGood, InjuryBad);

        int deployedLost = participants.Count(p => p.Source.Unit.Digested);
        if (participants.Count > 0 && deployedLost * 2 > participants.Count)
            AddCandidates(result, 0, eligible, "the company lost more than half of its deployed fighters", LossGood, LossBad);
        foreach (var p in eligible.Where(p => p.Evidence.AdjacentEnemyDigestions >= 2))
            Add(result, 0, p.Id, "was near an enemy when they completed multiple digestions", WitnessGood, WitnessBad);
        foreach (var p in eligible.Where(p => p.Evidence.WasLastActiveCompanyMember && participants.Count >= 4))
            Add(result, 0, p.Id, "was the last active company member standing and survived", LastGood, LastBad);
        foreach (var p in eligible.Where(p => p.Evidence.DamageReceived > p.Evidence.StartingMaxHealth && !p.Evidence.WasDowned))
            Add(result, 0, p.Id, "survived damage exceeding their normal maximum HP without being downed", SurvivalGood, SurvivalBad);
        foreach (var p in eligible.Where(p => p.Evidence.IncomingOpportunityAttacks > p.Evidence.IncomingNormalAttacks && p.Evidence.IncomingOpportunityAttacks > 0))
            Add(result, 0, p.Id, "survived more incoming opportunity attacks than ordinary attacks", SurvivalGood, SurvivalBad);

        if (enemyCount >= 6)
        {
            foreach (var p in eligible.Where(p => p.Evidence.MeleeDowns + p.Evidence.RangedDowns + p.Evidence.MagicDowns > enemyCount / 2))
            {
                int melee = p.Evidence.MeleeDowns, ranged = p.Evidence.RangedDowns, magic = p.Evidence.MagicDowns;
                var downKind = melee >= ranged && melee >= magic ? "melee" : ranged >= magic ? "ranged" : "magic";
                Add(result, 1, p.Id, "personally downed most of the enemy force", downKind == "melee" ? MeleeGood : downKind == "ranged" ? RangedGood : MagicGood,
                    downKind == "melee" ? MeleeBad : downKind == "ranged" ? RangedBad : MagicBad);
            }
        }

        foreach (var p in eligible.Where(p => p.Evidence.AdjacentEnemyDigestions == 1))
            Add(result, 1, p.Id, "was near an enemy when they completed a digestion", WitnessGood, WitnessBad);

        foreach (var p in eligible.Where(p => p.Evidence.WasReducedToZero))
            Add(result, 2, p.Id, "was reduced to 0 HP and survived", SurvivalGood, SurvivalBad);
        foreach (var p in eligible.Where(p => p.Evidence.Injuries > 0 && p.Evidence.Injuries < 3))
            Add(result, 2, p.Id, "suffered one or two new injuries", InjuryGood, InjuryBad);
        foreach (var p in eligible.Where(p => p.Evidence.EscapesOrFreed == 1))
            Add(result, 2, p.Id, "escaped or was freed from vore once", EscapeGood, EscapeBad);

        AddRanked(result, company, eligible, 2, "dealt the most melee damage", p => p.Evidence.MeleeDamage, MeleeGood, MeleeBad);
        AddRanked(result, company, eligible, 2, "dealt the most ranged damage", p => p.Evidence.RangedDamage, RangedGood, RangedBad);
        AddRanked(result, company, eligible, 2, "dealt the most magic damage", p => p.Evidence.MagicDamage, MagicGood, MagicBad);
        AddRanked(result, company, eligible, 2, "spent the most mana", p => p.Evidence.ManaSpent, ManaGood, ManaBad);
        AddRate(result, company, eligible, "melee", p => p.Evidence.MeleeAttempts, p => p.Evidence.MeleeHits, false);
        AddRate(result, company, eligible, "melee", p => p.Evidence.MeleeAttempts, p => p.Evidence.MeleeHits, true);
        AddRate(result, company, eligible, "ranged", p => p.Evidence.RangedAttempts, p => p.Evidence.RangedHits, false);
        AddRate(result, company, eligible, "ranged", p => p.Evidence.RangedAttempts, p => p.Evidence.RangedHits, true);
        AddCriticalRate(result, company, eligible, "melee", p => p.Evidence.MeleeHits, p => p.Evidence.MeleeCriticals);
        AddCriticalRate(result, company, eligible, "ranged", p => p.Evidence.RangedHits, p => p.Evidence.RangedCriticals);
        return result;
    }

    static readonly Traits[] DigestGood = { Traits.Frenzy, Traits.WideMaw, Traits.FastDigestion };
    static readonly Traits[] DigestBad = { Traits.Nauseous, Traits.WeakGullet, Traits.SlowDigestion };
    static readonly Traits[] EscapeGood = { Traits.EscapeArtist, Traits.InvigoratingEscape };
    static readonly Traits[] EscapeBad = { Traits.Timid, Traits.Cowardly };
    static readonly Traits[] InjuryGood = { Traits.Resilient, Traits.Tenacious };
    static readonly Traits[] InjuryBad = { Traits.WeakBones, Traits.Frail };
    static readonly Traits[] LossGood = { Traits.Resilient, Traits.Tenacious, Traits.PackTactics };
    static readonly Traits[] LossBad = { Traits.Timid, Traits.Cowardly, Traits.Defenseless };
    static readonly Traits[] WitnessGood = { Traits.Berserk, Traits.TasteForBlood };
    static readonly Traits[] WitnessBad = { Traits.Timid, Traits.Cowardly, Traits.PoorConstitution };
    static readonly Traits[] LastGood = { Traits.Resilient, Traits.Tenacious, Traits.Perseverance };
    static readonly Traits[] LastBad = { Traits.Timid, Traits.Cowardly, Traits.PoorConstitution };
    static readonly Traits[] SurvivalGood = { Traits.Resilient, Traits.Hearty, Traits.AwkwardShape };
    static readonly Traits[] SurvivalBad = { Traits.WeakBones, Traits.Frail, Traits.Defenseless };
    static readonly Traits[] MeleeGood = { Traits.Precise, Traits.StrongMelee };
    static readonly Traits[] MeleeBad = { Traits.Clumsy, Traits.WeakAttack };
    static readonly Traits[] RangedGood = { Traits.Deadeye, Traits.KeenShot };
    static readonly Traits[] RangedBad = { Traits.Nearsighted, Traits.RangedIneptitude };
    static readonly Traits[] MagicGood = { Traits.MagicProwess, Traits.PowerCaster };
    static readonly Traits[] MagicBad = { Traits.ErrantMage, Traits.MagicIneptitude };
    static readonly Traits[] ManaGood = { Traits.ManaBattery, Traits.PowerCaster, Traits.MagicProwess };
    static readonly Traits[] ManaBad = { Traits.Fizzler, Traits.MagicIneptitude };

    static void Add(List<Occurrence> list, int priority, string subject, string description, Traits[] good, Traits[] bad, bool leaderGlory = false)
    { list.Add(new Occurrence { Priority = priority, SubjectId = subject, Description = description, Good = good, Bad = bad, LeaderGlory = leaderGlory }); }

    static void AddCandidates(List<Occurrence> list, int priority, IEnumerable<Participant> candidates, string description,
        Traits[] good, Traits[] bad, bool leaderGlory = false)
    {
        string[] ids = candidates.Select(p => p.Id).Distinct().ToArray();
        if (ids.Length == 0) return;
        list.Add(new Occurrence { Priority = priority, SubjectIds = ids, Description = description, Good = good, Bad = bad, LeaderGlory = leaderGlory });
    }

    static void AddRanked(List<Occurrence> result, Participant[] compared, List<Participant> eligible, int priority, string description,
        Func<Participant, int> score, Traits[] good, Traits[] bad)
    {
        if (compared.Length < 2) return;
        int highest = compared.Max(score);
        if (highest <= 0) return;
        AddCandidates(result, priority, eligible.Where(p => score(p) == highest), description, good, bad);
    }

    static void AddRate(List<Occurrence> result, Participant[] compared, List<Participant> eligible, string kind,
        Func<Participant, int> attempts, Func<Participant, int> hits, bool highest)
    {
        var comparable = compared.Where(p => attempts(p) >= 5).ToArray();
        if (comparable.Length < 2) return;
        double value = highest ? comparable.Max(p => hits(p) / (double)attempts(p)) : comparable.Min(p => hits(p) / (double)attempts(p));
        var matching = comparable.Where(p => Math.Abs(hits(p) / (double)attempts(p) - value) < .0000001).ToArray();
        var good = kind == "melee" ? MeleeGood : kind == "ranged" ? RangedGood : MagicGood;
        var bad = kind == "melee" ? MeleeBad : kind == "ranged" ? RangedBad : MagicBad;
        AddCandidates(result, 2, eligible.Where(p => matching.Contains(p)), (highest ? "had the highest " : "had the lowest ") + kind + " hit rate", good, bad);
    }

    static void AddCriticalRate(List<Occurrence> result, Participant[] compared, List<Participant> eligible, string kind,
        Func<Participant, int> hits, Func<Participant, int> criticals)
    {
        var comparable = compared.Where(p => hits(p) >= 5).ToArray();
        if (comparable.Length < 2) return;
        double highest = comparable.Max(p => criticals(p) / (double)hits(p));
        var matching = comparable.Where(p => Math.Abs(criticals(p) / (double)hits(p) - highest) < .0000001).ToArray();
        AddCandidates(result, 2, eligible.Where(p => matching.Contains(p)),
            "had the highest " + kind + " critical rate", new[] { Traits.Assassin }, new[] { Traits.Brutish });
    }

    static Participant ChooseFewerTraits(CompanyCampaign campaign, List<Participant> candidates)
    {
        if (candidates.Count == 1) return candidates[0];
        int[] weights = candidates.Select(p => Math.Max(1, 1000 / (1 + p.TraitWeight))).ToArray();
        int total = weights.Sum(), roll = campaign.Next(total);
        for (int i = 0; i < candidates.Count; i++) { if (roll < weights[i]) return candidates[i]; roll -= weights[i]; }
        return candidates[candidates.Count - 1];
    }

    static int ChoosePolarityDirection(CompanyCampaign campaign, Unit unit)
    {
        int good, bad; Counts(unit, out good, out bad);
        int difference = good - bad;
        if (difference == 0) return campaign.Next(2) == 0 ? 1 : -1;
        if (Math.Abs(difference) >= 2) return -Math.Sign(difference);
        bool towardMinority = campaign.Next(100) < 75;
        return towardMinority ? -Math.Sign(difference) : Math.Sign(difference);
    }

    static TraitChange TryApplyOutcome(CompanyCampaign campaign, Unit unit, Traits[] options, CompanyTraitPolarity requested,
        int direction, bool leaderGlory)
    {
        int beforeGood, beforeBad; Counts(unit, out beforeGood, out beforeBad);
        int beforeDiff = beforeGood - beforeBad;
        int total = TotalWeight(unit);

        // With many existing traits, occasionally reduce one opposing trait instead of adding another.
        // This starts only at the curator's eight weighted-trait threshold and rises gradually to 30%.
        double reductionChance = total < 8 ? 0 : Math.Min(.30, .05 * (total - 7));
        if (!leaderGlory && reductionChance > 0 && campaign.Next(10000) < (int)(reductionChance * 10000))
        {
            var reduced = TryReduceOpposingTrait(campaign, unit, requested, beforeDiff, direction);
            if (reduced != null) return reduced;
        }

        var shuffled = options.ToList();
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = campaign.Next(i + 1);
            Traits swap = shuffled[i]; shuffled[i] = shuffled[j]; shuffled[j] = swap;
        }
        foreach (Traits option in shuffled)
        {
            if (TraitsMethods.IsRetired(option) || Polarity(option) != requested || IsInborn(option)) continue;
            var family = Family(option);
            var current = family == null ? default(Traits) : unit.GetTraits.FirstOrDefault(t => SameFamily(t, family));
            bool hasCurrent = family != null && unit.GetTraits.Any(t => SameFamily(t, family));
            var optionTiered = TieredTraits.Find(option);
            if (!hasCurrent && optionTiered != null)
            {
                var opposingTier = unit.GetTraits.FirstOrDefault(t =>
                {
                    var heldFamily = TieredTraits.Find(t);
                    return heldFamily != null && heldFamily.Effect == optionTiered.Effect;
                });
                if (opposingTier != default(Traits)) { current = opposingTier; hasCurrent = true; }
            }
            Traits after;
            var change = new TraitChange();
            if (hasCurrent)
            {
                var currentFamily = Family(current);
                int currentTier = Array.IndexOf(currentFamily, current);
                int optionTier = Array.IndexOf(family, option);
                if (currentTier < 0 || optionTier < 0 || IsInborn(current)) continue;
                bool sameFamily = SameFamily(current, family);
                if (!sameFamily)
                {
                    // Opposing tiers share one combat effect and cannot coexist. Reduce the
                    // held tier by one instead of accidentally swinging polarity by two.
                    if (total < 8) continue;
                    after = currentTier > 0 ? currentFamily[currentTier - 1] : default(Traits);
                    change.Before = current; change.After = after; change.Change = true; change.Removal = currentTier == 0;
                }
                else if (Polarity(current) == requested)
                {
                    if (currentTier >= family.Length - 1) continue;
                    after = family[currentTier + 1];
                    change.Before = current; change.After = after; change.Change = true;
                }
                else
                {
                    if (total < 8) continue;
                    after = currentTier > 0 ? currentFamily[currentTier - 1] : default(Traits);
                    change.Before = current; change.After = after; change.Change = true; change.Removal = currentTier == 0;
                }
            }
            else
            {
                if (unit.HasTrait(option)) continue;
                after = family == null ? option : family[0];
                change.Before = default(Traits); change.After = after;
            }
            if (!OnePointInRequestedDirection(unit, change, requested, direction, beforeDiff)) continue;
            if (change.Change)
            {
                unit.RemoveTrait(change.Before);
                if (!change.Removal && !unit.AddPermanentTrait(change.After))
                {
                    unit.AddPermanentTrait(change.Before);
                    continue;
                }
            }
            else if (!unit.AddPermanentTrait(change.After)) continue;
            return change;
        }
        return null;
    }

    static TraitChange TryReduceOpposingTrait(CompanyCampaign campaign, Unit unit, CompanyTraitPolarity requested,
        int beforeDiff, int direction)
    {
        var permanent = unit.GetPermanentTraits();
        if (permanent == null) return null;
        var candidates = permanent.Distinct().Where(t => !IsInborn(t) && Polarity(t) != CompanyTraitPolarity.Neutral && Polarity(t) != requested).ToList();
        while (candidates.Count > 0)
        {
            Traits current = candidates[campaign.Next(candidates.Count)]; candidates.Remove(current);
            var family = Family(current);
            int tier = family == null ? -1 : Array.IndexOf(family, current);
            Traits after = family != null && tier > 0 ? family[tier - 1] : default(Traits);
            var change = new TraitChange { Before = current, After = after, Change = true, Removal = after == default(Traits) };
            if (!OnePointInRequestedDirection(unit, change, requested, direction, beforeDiff)) continue;
            unit.RemoveTrait(current);
            if (!change.Removal && !unit.AddPermanentTrait(after)) continue;
            return change;
        }
        return null;
    }

    static bool OnePointInRequestedDirection(Unit unit, TraitChange change, CompanyTraitPolarity requested, int direction, int beforeDiff)
    {
        int beforeGood, beforeBad; Counts(unit, out beforeGood, out beforeBad);
        int newGood = beforeGood, newBad = beforeBad;
        if (change.Before != default(Traits)) AddWeight(ref newGood, ref newBad, Polarity(change.Before), -TraitWeight(change.Before));
        if (!change.Removal && change.After != default(Traits)) AddWeight(ref newGood, ref newBad, Polarity(change.After), TraitWeight(change.After));
        int afterDiff = newGood - newBad;
        if (afterDiff != beforeDiff + direction) return false;
        return Math.Abs(beforeDiff) < 2 || Math.Abs(afterDiff) < Math.Abs(beforeDiff);
    }

    static void Counts(Unit unit, out int good, out int bad)
    {
        good = 0; bad = 0;
        foreach (Traits trait in unit.GetTraits.Distinct()) AddWeight(ref good, ref bad, Polarity(trait), TraitWeight(trait));
    }
    static void AddWeight(ref int good, ref int bad, CompanyTraitPolarity polarity, int amount)
    { if (polarity == CompanyTraitPolarity.Good) good += amount; else if (polarity == CompanyTraitPolarity.Bad) bad += amount; }
    static int TotalWeight(Unit unit) { return unit.GetTraits.Distinct().Sum(TraitWeight); }

    static int TraitWeight(Traits trait)
    {
        var family = Family(trait);
        if (family == null) return 1;
        int tier = Array.IndexOf(family, trait);
        return tier < 0 ? 1 : tier + 1;
    }

    static Traits[] Family(Traits trait)
    {
        if (GrudgeTiers.Contains(trait)) return GrudgeFamily;
        var family = TieredTraits.Find(trait);
        return family == null ? null : family.Tiers;
    }
    static bool SameFamily(Traits a, Traits[] family) { return family != null && family.Contains(a); }
    internal static Traits Canonical(Traits trait)
    { var family = Family(trait); return family == null ? TieredTraits.Canonical(trait) : family[0]; }
    internal static bool IsInborn(Traits trait) { return InbornNames.Contains(Name(trait)); }

    static string Name(Traits trait)
    {
        var family = Family(trait);
        if (family != null)
        {
            if (GrudgeTiers.Contains(trait)) return "Grudge";
            var tiered = TieredTraits.Find(trait);
            return tiered == null ? trait.ToString() : tiered.Name;
        }
        return TraitsMethods.DisplayName(trait);
    }

    internal static CompanyTraitPolarity Polarity(Traits trait)
    {
        string name = Name(trait);
        if (SoftRetiredNames.Contains(name)) return CompanyTraitPolarity.Neutral;
        if (GoodNames.Contains(name)) return CompanyTraitPolarity.Good;
        if (BadNames.Contains(name)) return CompanyTraitPolarity.Bad;
        // Neutral and unclassified content contributes to total trait weight, not Good/Bad balance.
        return CompanyTraitPolarity.Neutral;
    }

    static string Describe(string subject, string occurrence, TraitChange change)
    {
        if (change.Removal)
            return subject + " " + occurrence + ". The trait " + TraitsMethods.DisplayName(change.Before) + " was lost.";
        if (change.Change)
            return subject + " " + occurrence + ". The trait " + TraitsMethods.DisplayName(change.Before) + " changed to " + TraitsMethods.DisplayName(change.After) + ".";
        return subject + " " + occurrence + ". Gained the trait " + TraitsMethods.DisplayName(change.After) + ".";
    }

    internal static void RecordWeaponAttempt(Actor_Unit actor, bool ranged, int result)
    { if (actor != null) actor.Evidence.RecordWeapon(ranged, result); }
    internal static void RecordSpellAttempt(Actor_Unit actor, bool hit)
    { if (actor != null) actor.Evidence.RecordSpell(hit); }
    internal static void RecordIncomingAttack(Actor_Unit target, bool opportunity)
    { if (target != null) target.Evidence.RecordIncoming(opportunity); }
    internal static void RecordDamageDealt(Actor_Unit attacker, bool ranged, int damage)
    { if (attacker != null) attacker.Evidence.RecordDealt(ranged, damage); }
    internal static void RecordMagicDamage(Actor_Unit attacker, int damage)
    { if (attacker != null) attacker.Evidence.RecordMagicDamage(damage); }
    internal static void RecordDownedBy(Actor_Unit attacker, bool ranged, bool magic)
    { if (attacker != null) attacker.Evidence.RecordDown(ranged, magic); }
    internal static void RecordDamageTaken(Unit unit, int amount)
    { var actor = ActorFor(unit); if (actor != null) actor.Evidence.RecordDamage(amount); }
    internal static void RecordInjury(Unit unit)
    { var actor = ActorFor(unit); if (actor != null) actor.Evidence.RecordInjury(); }
    internal static void RecordEscape(Actor_Unit actor)
    { if (actor != null) actor.Evidence.RecordEscape(); }
    internal static void RecordManaSpent(Unit unit, int amount)
    { var actor = ActorFor(unit); if (actor != null) actor.Evidence.RecordMana(amount); }
    internal static void RecordReducedToZero(Unit unit)
    { var actor = ActorFor(unit); if (actor != null) { actor.Evidence.WasDowned = true; actor.Evidence.WasReducedToZero = true; RecordActivityChange(); } }
    internal static void RecordDowned(Unit unit)
    {
        var actor = ActorFor(unit);
        if (actor != null) actor.Evidence.WasDowned = true;
        RecordActivityChange();
    }
    internal static void RecordFled(Actor_Unit actor)
    { if (actor != null) RecordActivityChange(); }

    static Actor_Unit ActorFor(Unit unit)
    {
        var units = TacticalUtilities.Units;
        return units == null || unit == null ? null : units.FirstOrDefault(a => ReferenceEquals(a.Unit, unit));
    }

    static void RecordActivityChange()
    {
        var game = State.GameManager;
        var session = game == null ? null : game.CompanyBattle;
        var units = TacticalUtilities.Units;
        if (session == null || units == null || session.Campaign.Battle == null || session.Campaign.Battle.DeployedIds == null || session.Campaign.Battle.DeployedIds.Length < 4) return;
        var active = units.Where(a => session.Campaign.Battle.DeployedIds.Any(id =>
                session.Campaign.Characters.Any(c => c.Id == id && ReferenceEquals(c.Unit, a.Unit))) &&
            !a.Unit.Digested && !a.Unit.IsDead && !a.Unit.Downed && !a.Fled && a.SelfPrey == null).ToArray();
        if (active.Length == 1 && active[0].Unit.Side == 0) active[0].Evidence.WasLastActiveCompanyMember = true;
    }

    internal static void RecordCompletedDigestion(Actor_Unit predator)
    {
        if (predator == null) return;
        var game = State.GameManager;
        var session = game == null ? null : game.CompanyBattle;
        var units = TacticalUtilities.Units;
        if (session == null || units == null || session.Campaign.Battle == null || predator.Unit.Side != 1) { RecordActivityChange(); return; }
        foreach (var witness in units.Where(a => session.Campaign.Battle.DeployedIds != null && session.Campaign.Battle.DeployedIds.Any(id =>
                     session.Campaign.Characters.Any(c => c.Id == id && ReferenceEquals(c.Unit, a.Unit))) && a.Unit.Side != predator.Unit.Side))
            if (witness.Position.GetNumberOfMovesDistance(predator.Position) == 1) witness.Evidence.AdjacentEnemyDigestions++;
        RecordActivityChange();
    }

    static int GrudgeTier(Unit unit)
    {
        Traits held = unit.GetTraits.FirstOrDefault(GrudgeTiers.Contains);
        return held == default(Traits) ? 0 : Array.IndexOf(GrudgeFamily, held) + 1;
    }

    internal static int FutureGrudgeIncrease(CompanyCharacter character, int ordinaryIncrease)
    {
        if (character == null || ordinaryIncrease <= 0) return 0;
        int tier = GrudgeTier(character.Unit);
        if (tier <= 0) return 0;
        double fraction = tier == 1 ? .25 : tier == 2 ? .50 : 1.0;
        return Math.Max(1, (int)Math.Ceiling(ordinaryIncrease * fraction));
    }

    static HashSet<string> Names(string values)
    { return new HashSet<string>(values.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal); }
}

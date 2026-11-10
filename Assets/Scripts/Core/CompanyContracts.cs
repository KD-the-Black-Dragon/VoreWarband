// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-09: Release completed boss-force records after payout while preserving contract history (mission follow-up).
// 2026-11-09: Add power-budgeted boss contracts with persistent boss objectives (campaign mission expansion).
// 2026-11-09: Select enemy mage spellbooks by enemy level rather than player reputation.
// 2026-11-08: Generate three spaced opening contracts and step new offers to rating 2 after three wins (balance follow-up item 3).
// 2026-11-08: Defer contract enemy rosters until first battle entry and preserve them for retries (spec 09 follow-up).
// 2026-11-08: Avoid local-name shadowing rejected by the Unity 2018 C# compiler in force generation.
// 2026-11-08: Cap generated campaign enemy forces at eight and advance enemy quality by level, gear and role traits (balance follow-up item 2).
// 2026-11-09: Prefer the requested four-sapient/two-animal contract roster before upgrading force quality (enemy composition follow-up).
// 2026-11-09: Select an in-band four-sapient/two-animal force when the generated pool contains one (composition feasibility follow-up).
// 2026-11-09: Shorten starting contract target distance to 5–10 tiles (balance feedback).
// 2026-11-09: Add an editor-only pressure-curve hook for synthetic late-game balance comparisons (balance follow-up).
// 2026-11-05: Quote replacement-supporting rounded rewards for new offers while preserving accepted quotes (spec 38 phase B).
// 2026-11-04: Apply explainable healthy power and approved capped encounter scaling (spec 34 slice 3).
// 2026-11-01: Saved rout contracts, bounded power-budget enemies and outcome-only reputation (spec 09).
// 2026-11-01: Name generated enemy sapients by combat archetype and animals by species (09–10 feedback).
// 2026-11-01: Accept injury/recovery campaign rules version 3 without regenerating saved enemy forces (spec 11).
// 2026-11-01: Accept progression/notice rules version 4 without regenerating saved contracts (spec 11 feedback).
// 2026-11-02: Share approved enemy construction and validate explicit road-battle sources without fake contracts (spec 13).
// 2026-11-03: Implement four active stats and agreed combat rules (spec 29).
// 2026-11-08: Add provisional calendar pressure and company-relative ratings to newly generated contracts (spec 40F).
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
using System;
using System.Collections.Generic;
using System.Linq;

internal enum CompanyContractStatus { Offered, Active, Completed, Expired, Abandoned }
internal enum CompanyContractObjective { RoutForce = 0, RoutBoss = 1 }
[Serializable]
internal sealed class CompanyContract
{
    public string Id, TownId;
    public string BossCharacterId;
    public int Rating, Reward, Tile, Deadline;
    public double Power;
    public int DifficultyRevision, CalendarAgeDays;
    public double CalendarPressure;
    public CompanyContractObjective Objective;
    public CompanyContractStatus Status;
    public List<string> EnemyIds = new List<string>();
    public List<string> DefeatedIds = new List<string>();
}

internal static class CompanyContracts
{
#if UNITY_EDITOR
    // Synthetic balance-audit hook. Player builds always use the production curve below.
    internal static Func<int, double, double> CalendarPressureAuditTransform;
#endif
    internal const int DeadlineDays = 30, RefreshDays = 7;
    internal const int DifficultyRevision = 1, ProtectedDays = 21;
    internal const int OpeningContractCount = 3, OpeningTargetMinDistance = 5, OpeningTargetMaxDistance = 10;
    internal const int OpeningTargetMinSeparation = 8, PostOpeningMinimumRating = 2;
    const int StandardOfferCount = 4;
    internal const double EasyContractPowerFraction = .40;
    internal static int Reward(int rating, int variation)
    { return checked((int)Math.Round(rating * 240.0 * (1 + variation / 100.0) / 10) * 10); }
    // Provisional fixed rating scale, independent of the player's roster or treasury.
    internal static double Budget(int rating) { return 45 + (double)rating * 30; }
    internal static int CalendarAgeDays(CompanyCampaign state)
    { return (int)Math.Min(int.MaxValue, Math.Max(0L, (long)state.ElapsedDays - state.CalendarDifficultyStartDay)); }
    internal static double CalendarPressureForAge(int ageDays)
    {
        long days = Math.Max(0L, (long)ageDays - ProtectedDays);
        // Provisional calibration curve: no early pressure, gentle weekly onset, then an accelerating quadratic ramp.
        double pressure = .05 * days + .00015 * days * days;
#if UNITY_EDITOR
        if (CalendarPressureAuditTransform != null)
            pressure = CalendarPressureAuditTransform(ageDays, pressure);
#endif
        return pressure;
    }
    internal static double CalendarPressure(CompanyCampaign state)
    { return CalendarPressureForAge(CalendarAgeDays(state)); }
    static int RoundedPressureBonus(CompanyCampaign state, double pressure)
    {
        double whole = Math.Floor(Math.Max(0, pressure));
        int result = whole >= int.MaxValue ? int.MaxValue : (int)whole;
        double fraction = pressure - whole;
        if (fraction > 0 && result < int.MaxValue && state.Next(10000) < fraction * 10000) result++;
        return result;
    }
    static int ReputationRatingCeiling(CompanyCampaign state)
    { return 1 + Math.Min(19, Math.Max(0, state.Reputation / 3)); }
    internal static double OfferReferencePower(CompanyCampaign state)
    {
        var characters = state.Characters.ToDictionary(c => c.Id);
        int active = state.Members.Count(m => !m.Reserve);
        double total = 0;
        foreach (var member in state.Members)
        {
            CompanyCharacter character;
            if (!characters.TryGetValue(member.CharacterId, out character) || character.Unit == null) continue;
            var healthy = CompanyPower.Evaluate(character.Unit);
            double current = CompanyPower.Current(character.Unit, healthy);
            total += current * (member.Reserve ? .35 : 1.0);
        }
        return total * CompanyPower.FormationMultiplier(active);
    }
    internal static int MinimumOfferRating(double companyPower, int maximumRating)
    {
        if (maximumRating < 1) return 1;
        double minimumPower = Math.Max(0, companyPower) * EasyContractPowerFraction;
        double rating = Math.Ceiling((minimumPower - 45) / 30.0);
        int minimum = rating >= int.MaxValue ? int.MaxValue : Math.Max(1, (int)rating);
        return Math.Min(maximumRating, minimum);
    }
    internal static int MinimumOfferRatingForState(CompanyCampaign state, double companyPower, int maximumRating)
    {
        int minimum = MinimumOfferRating(companyPower, maximumRating);
        if (state.ContractsCompleted >= OpeningContractCount)
            minimum = Math.Max(minimum, PostOpeningMinimumRating);
        return Math.Min(Math.Max(1, maximumRating), minimum);
    }
    static int OfferRating(CompanyCampaign state, int offerIndex, double pressure, double companyPower, int maximumRating)
    {
        int maximum = (int)Math.Min(int.MaxValue, (long)maximumRating + RoundedPressureBonus(state, pressure));
        int minimum = MinimumOfferRatingForState(state, companyPower, maximum);
        if (offerIndex == 0) return minimum;
        if (offerIndex == 3) return maximum;
        return minimum + state.Next(Math.Max(1, maximum - minimum + 1));
    }
    internal static double RoadEncounterMultiplier(double pressure)
    { return 1 + Math.Min(4 - 1, Math.Max(0, pressure) * .015); }
    internal static double Power(Unit unit) => CompanyPower.Healthy(unit);
    internal static IEnumerable<CompanyCharacter> Survivors(CompanyCampaign state, CompanyContract contract)
    { return contract.EnemyIds.Except(contract.DefeatedIds).Select(id => state.Characters.Single(c => c.Id == id)); }
    internal static CompanyCharacter Enemy(CompanyCampaign state, Race race, int rating)
    {
        if (SpeciesPolicy.GetKind(race) == SpeciesKind.Sapient)
        {
            int focus = state.Next(3);
            int levelCeiling = Math.Max(1, rating);
            int levelFloor = Math.Max(1, levelCeiling - 2);
            int level = levelFloor + state.Next(levelCeiling - levelFloor + 1);
            var enemy = CompanyEconomy.Create(state, race, focus, false, level, enemy: true);
            AddEnemySpecialtyTrait(state, enemy.Unit, focus, level);
            enemy.Unit.Name = SpeciesPolicy.DisplayName(race) + " " + new[] { "Warrior", "Archer", "Mage" }[enemy.Focus];
            return enemy;
        }
        return CompanyEconomy.Random(state, () =>
        {
            var unit = new Unit(1, race, 0, true, itemContext: CompanyEquipment.Repository, companyLoadout: true);
            unit.SetItem(CompanyEquipment.Create("natural.slug"), (int)CompanySlot.Ranged);
            unit.Name = SpeciesPolicy.DisplayName(race);
            unit.UpdateSpells(); unit.Health = unit.MaxHealth;
            return new CompanyCharacter { Id = Guid.NewGuid().ToString("N"), Unit = unit, BaseWage = 0 };
        });
    }
    static void AddEnemySpecialtyTrait(CompanyCampaign state, Unit unit, int focus, int level)
    {
        int tier = level >= 10 ? 2 : level >= 6 ? 1 : level >= 3 ? 0 : -1;
        if (tier < 0) return;
        Traits[] options;
        if (focus == 1)
            options = tier == 0 ? new[] { Traits.Deadeye, Traits.KeenShot } : tier == 1 ? new[] { Traits.DeadeyeII, Traits.KeenShotII } : new[] { Traits.DeadeyeIII, Traits.KeenShotIII };
        else if (focus == 2)
            options = tier == 0 ? new[] { Traits.MagicProwess, Traits.PowerCaster } : tier == 1 ? new[] { Traits.MagicFinesseII, Traits.PowerCasterII } : new[] { Traits.MagicFinesseIII, Traits.PowerCasterIII };
        else
            options = tier == 0 ? new[] { Traits.Precise, Traits.StrongMelee } : tier == 1 ? new[] { Traits.PreciseII, Traits.StrongMeleeII } : new[] { Traits.PreciseIII, Traits.StrongMeleeIII };

        // Preserve inborn traits, while still allowing the other specialty bonus if only one conflicts.
        var existingFamilies = unit.GetTraits.Select(TieredTraits.Find).Where(f => f != null).Select(f => f.Effect).ToArray();
        var available = options.Where(candidate =>
        {
            var family = TieredTraits.Find(candidate);
            return family != null && !existingFamilies.Contains(family.Effect);
        }).ToArray();
        if (available.Length > 0) unit.AddTrait(available[state.Next(available.Length)]);
    }
    static List<CompanyCharacter> Generate(CompanyCampaign state, int rating, bool bossContract, out string bossId)
    {
        bossId = null;
        Race[] races = SpeciesPolicy.GetAvailable(SpeciesUse.Enemy);
        if (races.Length == 0) throw new InvalidOperationException(SpeciesPolicy.UnavailableMessage);
        double budget = Budget(rating), low = budget * .92, high = budget * 1.08;
        // Generate a bounded candidate pool once, then compose distinct candidates within the band.
        var regularPool = Enumerable.Range(0, 48).Select(_ => Enemy(state, races[state.Next(races.Length)], rating)).ToArray();
        CompanyCharacter boss = null;
        if (bossContract)
        {
            Race[] sapientRaces = races.Where(r => SpeciesPolicy.GetKind(r) == SpeciesKind.Sapient &&
                SpeciesPolicy.IsAllowed(r, SpeciesUse.Mercenary)).ToArray();
            if (sapientRaces.Length == 0) throw new InvalidOperationException("No enabled sapient species are available for a boss contract.");
            Race bossRace = sapientRaces[state.Next(sapientRaces.Length)];
            int bossLevel = rating == int.MaxValue ? int.MaxValue : Math.Max(2, rating + 1);
            boss = CompanyEconomy.Create(state, bossRace, 1, false, bossLevel, enemy: true);
            AddEnemySpecialtyTrait(state, boss.Unit, 1, bossLevel);
            var melee = CompanyEquipment.Generated(state, CompanySlot.Melee, bossLevel);
            boss.Unit.SetItem(CompanyEquipment.Create(melee.Id), (int)CompanySlot.Melee);
            boss.Unit.Name = SpeciesPolicy.DisplayName(bossRace) + " Archer (Boss)";
        }
        var pool = boss == null ? regularPool : regularPool.Concat(new[] { boss }).ToArray();
        var powers = pool.ToDictionary(c => c.Id, c => Power(c.Unit));
        var exactPreferred = BestExactPreferredForce(pool, powers, budget, low, high, boss);
        if (exactPreferred != null)
        {
            foreach (CompanyCharacter selected in exactPreferred)
            { selected.Employment = CompanyEmployment.Unavailable; selected.Unit.Side = 1; }
            if (boss != null) bossId = boss.Id;
            return exactPreferred;
        }

        int sapientTarget = Math.Min(4, pool.Count(IsSapient));
        int animalTarget = Math.Min(2, pool.Length - pool.Count(IsSapient));
        for (int attempt = 0; attempt < 40; attempt++)
        {
            var force = new List<CompanyCharacter>(); double sum = 0;
            int sapients = 0, animals = 0;
            if (boss != null) { force.Add(boss); sum = powers[boss.Id]; sapients = 1; }

            // Establish the preferred composition using lower-power candidates first. If the full
            // 4+2 mix will not fit this rating's cap, keep the largest feasible partial mix.
            for (int pick = 0; pick < 96 && force.Count < CompanyBattleSession.MaximumCampaignBattleForceSize &&
                (sapients < sapientTarget || animals < animalTarget); pick++)
            {
                bool needSapient = sapients < sapientTarget;
                bool needAnimal = animals < animalTarget;
                bool preferSapient = needSapient && (!needAnimal || sapients / (double)sapientTarget <= animals / (double)animalTarget);
                var candidates = FeasibleCandidates(pool, powers, force, sum, high, preferSapient);
                if (candidates.Length == 0 && needSapient && needAnimal)
                    candidates = FeasibleCandidates(pool, powers, force, sum, high, !preferSapient);
                if (candidates.Length == 0) break;

                var selected = PickLowerPowerCandidate(state, candidates, powers);
                force.Add(selected); sum += powers[selected.Id];
                if (IsSapient(selected)) sapients++; else animals++;
            }

            // Once the preferred mix is in place, replace members with stronger same-kind candidates
            // when necessary. This spends the rating budget on quality without losing the new mix.
            for (int upgrade = 0; upgrade < 96 && force.Count > 0 && ForcePower(sum, force.Count) < low; upgrade++)
            {
                CompanyCharacter replacement = null;
                int replacementIndex = -1;
                double crossingPower = double.MaxValue, bestUnderPower = double.MinValue;
                for (int index = 0; index < force.Count; index++)
                {
                    CompanyCharacter current = force[index];
                    if (current == boss) continue;
                    foreach (CompanyCharacter candidate in pool.Where(c => !force.Contains(c) && IsSapient(c) == IsSapient(current)))
                    {
                        double projected = ForcePower(sum - powers[current.Id] + powers[candidate.Id], force.Count);
                        if (projected <= ForcePower(sum, force.Count) || projected > high) continue;
                        if (projected >= low)
                        {
                            if (projected < crossingPower)
                            { crossingPower = projected; replacement = candidate; replacementIndex = index; }
                        }
                        else if (crossingPower == double.MaxValue && projected > bestUnderPower)
                        { bestUnderPower = projected; replacement = candidate; replacementIndex = index; }
                    }
                }
                if (replacement == null) break;
                sum = sum - powers[force[replacementIndex].Id] + powers[replacement.Id];
                force[replacementIndex] = replacement;
            }

            // Add beyond the 4+2 target only when the band still needs it and no same-kind upgrade
            // can reach the band. If a desired slot cannot fit under the cap, another feasible role
            // may fill the remaining budget instead. The existing eight-unit cap still applies.
            for (int pick = 0; pick < 96 && force.Count < CompanyBattleSession.MaximumCampaignBattleForceSize && ForcePower(sum, force.Count) < low; pick++)
            {
                bool needSapient = sapients < sapientTarget;
                bool needAnimal = animals < animalTarget;
                var candidates = needSapient || needAnimal
                    ? pool.Where(c => !force.Contains(c) && ((needSapient && IsSapient(c)) || (needAnimal && !IsSapient(c))))
                        .Where(c => ForcePower(sum + powers[c.Id], force.Count + 1) <= high).ToArray()
                    : pool.Where(c => !force.Contains(c)).Where(c => ForcePower(sum + powers[c.Id], force.Count + 1) <= high).ToArray();
                // If no underrepresented type can fit, allow a different feasible member to reach
                // the lower band rather than failing a rating that the pool can otherwise satisfy.
                if (candidates.Length == 0 && (needSapient || needAnimal))
                    candidates = pool.Where(c => !force.Contains(c)).Where(c => ForcePower(sum + powers[c.Id], force.Count + 1) <= high).ToArray();
                if (candidates.Length == 0) break;

                CompanyCharacter selected = null;
                double crossingPower = double.MaxValue, bestUnderPower = double.MinValue;
                foreach (CompanyCharacter candidate in candidates)
                {
                    double projected = ForcePower(sum + powers[candidate.Id], force.Count + 1);
                    if (projected >= low)
                    {
                        if (projected < crossingPower) { crossingPower = projected; selected = candidate; }
                    }
                    else if (crossingPower == double.MaxValue && projected > bestUnderPower)
                    { bestUnderPower = projected; selected = candidate; }
                }
                if (selected == null) break;
                force.Add(selected); sum += powers[selected.Id];
                if (IsSapient(selected)) sapients++; else animals++;
            }

            if (force.Count > 0 && ForcePower(sum, force.Count) >= low && ForcePower(sum, force.Count) <= high)
            {
                foreach (CompanyCharacter selected in force)
                { selected.Employment = CompanyEmployment.Unavailable; selected.Unit.Side = 1; }
                if (boss != null) bossId = boss.Id;
                return force;
            }
        }
        throw new InvalidOperationException("The enabled species cannot currently fill this contract's power band. No fallback species were added.");
    }
    sealed class AnimalPair
    {
        public int First, Second;
        public double Power;
    }
    static List<CompanyCharacter> BestExactPreferredForce(CompanyCharacter[] pool, Dictionary<string, double> powers,
        double budget, double low, double high, CompanyCharacter requiredSapient = null)
    {
        var sapients = pool.Where(IsSapient).ToArray();
        var animals = pool.Where(c => !IsSapient(c)).ToArray();
        if (sapients.Length < 4 || animals.Length < 2) return null;

        var pairs = new List<AnimalPair>();
        for (int first = 0; first < animals.Length - 1; first++)
            for (int second = first + 1; second < animals.Length; second++)
                pairs.Add(new AnimalPair { First = first, Second = second, Power = powers[animals[first].Id] + powers[animals[second].Id] });
        pairs.Sort((left, right) => left.Power.CompareTo(right.Power));

        double formation = CompanyPower.FormationMultiplier(6);
        double targetSum = budget / formation;
        List<CompanyCharacter> best = null;
        double bestDistance = double.MaxValue;
        for (int first = 0; first < sapients.Length - 3; first++)
            for (int second = first + 1; second < sapients.Length - 2; second++)
                for (int third = second + 1; third < sapients.Length - 1; third++)
                    for (int fourth = third + 1; fourth < sapients.Length; fourth++)
                    {
                        if (requiredSapient != null && sapients[first] != requiredSapient && sapients[second] != requiredSapient &&
                            sapients[third] != requiredSapient && sapients[fourth] != requiredSapient) continue;
                        double sapientSum = powers[sapients[first].Id] + powers[sapients[second].Id] +
                            powers[sapients[third].Id] + powers[sapients[fourth].Id];
                        int near = LowerBoundPair(pairs, targetSum - sapientSum);
                        for (int candidateIndex = Math.Max(0, near - 1); candidateIndex < pairs.Count && candidateIndex <= near; candidateIndex++)
                        {
                            AnimalPair pair = pairs[candidateIndex];
                            double forcePower = (sapientSum + pair.Power) * formation;
                            if (forcePower < low || forcePower > high) continue;
                            double distance = Math.Abs(forcePower - budget);
                            if (distance >= bestDistance) continue;
                            bestDistance = distance;
                            best = new List<CompanyCharacter>
                            {
                                sapients[first], sapients[second], sapients[third], sapients[fourth],
                                animals[pair.First], animals[pair.Second]
                            };
                        }
                    }
        return best;
    }
    static int LowerBoundPair(List<AnimalPair> pairs, double power)
    {
        int low = 0, high = pairs.Count;
        while (low < high)
        {
            int middle = low + (high - low) / 2;
            if (pairs[middle].Power < power) low = middle + 1;
            else high = middle;
        }
        return low;
    }
    static bool IsSapient(CompanyCharacter character) => SpeciesPolicy.GetKind(character.Unit.Race) == SpeciesKind.Sapient;
    static double ForcePower(double sum, int count) => sum * CompanyPower.FormationMultiplier(count);
    static CompanyCharacter[] FeasibleCandidates(CompanyCharacter[] pool, Dictionary<string, double> powers, List<CompanyCharacter> force, double sum, double high, bool sapient)
    {
        return pool.Where(c => !force.Contains(c) && IsSapient(c) == sapient)
            .Where(c => ForcePower(sum + powers[c.Id], force.Count + 1) <= high).ToArray();
    }
    static CompanyCharacter PickLowerPowerCandidate(CompanyCampaign state, CompanyCharacter[] candidates, Dictionary<string, double> powers)
    {
        var lowerPower = candidates.OrderBy(c => powers[c.Id]).Take(Math.Max(1, (candidates.Length + 2) / 3)).ToArray();
        return lowerPower[state.Next(lowerPower.Length)];
    }
    internal static void EnsureEnemyForce(CompanyCampaign state, CompanyContract contract)
    {
        if (contract == null || contract.Status != CompanyContractStatus.Active)
            throw new InvalidOperationException("This target is no longer available.");
        if (contract.EnemyIds.Count > 0)
        {
            if (!Survivors(state, contract).Any()) throw new InvalidOperationException("This target is no longer available.");
            return;
        }
        if (contract.DefeatedIds.Count > 0) throw new InvalidOperationException("This target has an invalid saved roster.");

        // The offer fixes the rating and reward; only materialize the force when the company reaches the target.
        string bossId;
        var force = Generate(state, contract.Rating, contract.Objective == CompanyContractObjective.RoutBoss, out bossId);
        contract.EnemyIds = force.Select(c => c.Id).ToList();
        contract.DefeatedIds = new List<string>();
        contract.BossCharacterId = bossId;
        contract.Power = CompanyPower.Force(force.Select(c => c.Unit));
        state.Characters.AddRange(force);
    }
    internal static void EnsureTown(CompanyCampaign state, CompanyTown town)
    {
        if (state.Preview || (town.ContractRefreshDay > state.ElapsedDays)) return;
        bool openingPool = state.ElapsedDays == 0 && state.ContractsCompleted == 0 && town.ContractRefreshDay == 0 &&
            state.Towns.Count > 0 && town.Id == state.Towns[0].Id;
        foreach (var old in state.Contracts.Where(c => c.TownId == town.Id && c.Status == CompanyContractStatus.Offered).ToArray())
        {
            foreach (string id in old.EnemyIds) state.Characters.RemoveAll(c => c.Id == id);
            state.Contracts.Remove(old);
        }
        if (SpeciesPolicy.GetAvailable(SpeciesUse.Enemy).Length == 0)
        {
            town.ContractRefreshDay = state.ElapsedDays + RefreshDays;
            return;
        }
        double pressure = CalendarPressure(state);
        int reputationMaximum = ReputationRatingCeiling(state);
        double companyPower = OfferReferencePower(state);
        int reputationMinimum = MinimumOfferRatingForState(state, companyPower, reputationMaximum);
        int offerCount = openingPool ? OpeningContractCount : StandardOfferCount;
        Race[] availableEnemies = SpeciesPolicy.GetAvailable(SpeciesUse.Enemy);
        bool canOfferBoss = state.ContractsCompleted >= OpeningContractCount && availableEnemies.Any(r =>
            SpeciesPolicy.GetKind(r) == SpeciesKind.Sapient && SpeciesPolicy.IsAllowed(r, SpeciesUse.Mercenary));
        int bossOfferIndex = canOfferBoss ? state.Next(offerCount) : -1;
        int targetMinDistance = openingPool ? OpeningTargetMinDistance : 2;
        int targetMaxDistance = openingPool ? OpeningTargetMaxDistance : 6;
        for (int i = 0; i < offerCount; i++)
        {
            int rating = OfferRating(state, i, pressure, companyPower, reputationMaximum);
            int pressureAge = CalendarAgeDays(state);
            var possible = Enumerable.Range(0, state.Terrain.Length).Where(tile =>
                !state.Towns.Any(t => t.Tile == tile) &&
                !state.Contracts.Any(c => (c.Status == CompanyContractStatus.Active || c.Status == CompanyContractStatus.Offered) && c.Tile == tile) &&
                Distance(state, tile, town.Tile) >= targetMinDistance && Distance(state, tile, town.Tile) <= targetMaxDistance &&
                (!openingPool || !state.Contracts.Any(c => (c.Status == CompanyContractStatus.Active || c.Status == CompanyContractStatus.Offered) &&
                    c.TownId == town.Id && Distance(state, c.Tile, tile) < OpeningTargetMinSeparation))).ToArray();
            if (possible.Length == 0) break;
            var contract = new CompanyContract { Id = Guid.NewGuid().ToString("N"), TownId = town.Id, Rating = rating,
                Reward = Reward(rating, state.Next(21) - 10),
                Tile = possible[state.Next(possible.Length)], Power = 0, EnemyIds = new List<string>(), DefeatedIds = new List<string>(),
                Objective = i == bossOfferIndex ? CompanyContractObjective.RoutBoss : CompanyContractObjective.RoutForce,
                DifficultyRevision = DifficultyRevision, CalendarAgeDays = pressureAge, CalendarPressure = pressure };
            state.Contracts.Add(contract);
        }
        town.ContractRefreshDay = state.ElapsedDays + RefreshDays;
    }
    internal static int Distance(CompanyCampaign state, int a, int b)
    { return Math.Abs(a % state.Width - b % state.Width) + Math.Abs(a / state.Width - b / state.Width); }
    internal static void Accept(CompanyCampaign state, CompanyContract contract)
    {
        if (state.Busy || state.InBattle || contract.Status != CompanyContractStatus.Offered || !state.Towns.Any(t => t.Id == contract.TownId && t.Tile == state.Position))
            throw new InvalidOperationException("Visit the offering town to accept this contract.");
        contract.Status = CompanyContractStatus.Active; contract.Deadline = state.ElapsedDays + DeadlineDays;
    }
    internal static void Fail(CompanyCampaign state, CompanyContract contract, bool abandoned)
    {
        if (contract.Status != CompanyContractStatus.Active) return;
        contract.Status = abandoned ? CompanyContractStatus.Abandoned : CompanyContractStatus.Expired;
        state.Reputation = Math.Max(0, state.Reputation - contract.Rating);
    }
    internal static bool Complete(CompanyCampaign state, CompanyContract contract)
    {
        if (contract.Status != CompanyContractStatus.Active || contract.EnemyIds.Count == 0) return false;
        if (contract.Objective == CompanyContractObjective.RoutBoss)
        {
            if (string.IsNullOrEmpty(contract.BossCharacterId) || !contract.DefeatedIds.Contains(contract.BossCharacterId)) return false;
        }
        else if (Survivors(state, contract).Any()) return false;
        contract.Status = CompanyContractStatus.Completed; state.Gold += contract.Reward;
        state.Reputation++; state.TotalGoldEarned += contract.Reward; state.ContractsCompleted++;
        return true;
    }
    internal static bool HasReleasedBossForce(CompanyContract contract)
    {
        return contract != null && contract.Objective == CompanyContractObjective.RoutBoss &&
            contract.Status == CompanyContractStatus.Completed && contract.EnemyIds != null && contract.EnemyIds.Count == 0 &&
            contract.DefeatedIds != null && contract.DefeatedIds.Count == 0 && contract.Power == 0 &&
            string.IsNullOrEmpty(contract.BossCharacterId);
    }
    internal static void ReleaseCompletedBossForce(CompanyCampaign state, CompanyContract contract)
    {
        if (contract == null || contract.Objective != CompanyContractObjective.RoutBoss ||
            contract.Status != CompanyContractStatus.Completed) return;
        if (contract.EnemyIds == null) contract.EnemyIds = new List<string>();
        if (contract.DefeatedIds == null) contract.DefeatedIds = new List<string>();

        var enemyIds = new HashSet<string>(contract.EnemyIds.Where(id => id != state.LeaderId &&
            !state.Members.Any(member => member.CharacterId == id)));
        if (enemyIds.Count > 0)
        {
            var enemyUnits = new HashSet<Unit>(state.Characters.Where(c => enemyIds.Contains(c.Id)).Select(c => c.Unit));
            foreach (var character in state.Characters)
                if (!enemyIds.Contains(character.Id))
                {
                    if (character.Unit.KilledBy != null && enemyUnits.Contains(character.Unit.KilledBy)) character.Unit.KilledBy = null;
                    if (character.Unit.AttractedTo != null && enemyUnits.Contains(character.Unit.AttractedTo)) character.Unit.AttractedTo = null;
                }
            state.Characters.RemoveAll(c => enemyIds.Contains(c.Id));
            if (state.Battle != null && state.Battle.ContractId == contract.Id && state.Battle.DeployedIds != null)
                state.Battle.DeployedIds = state.Battle.DeployedIds.Where(id => !enemyIds.Contains(id)).ToArray();
        }

        // The battle event and result summary are recorded before this cleanup. Keep only the completed contract's public history.
        contract.EnemyIds.Clear(); contract.DefeatedIds.Clear(); contract.BossCharacterId = null; contract.Power = 0;
    }
    internal static void Validate(CompanyCampaign state)
    {
        if (state.Contracts == null || state.Memorial == null) throw new InvalidOperationException("Missing contract records.");
        if (state.CompanyRulesVersion != 7) throw new InvalidOperationException("Unsupported company rules version.");
        if (state.Battle != null)
        {
            var battle = state.Battle;
            var battleContract = battle.RoadEventId == null ? state.Contracts.SingleOrDefault(c => c.Id == battle.ContractId) : null;
            if (string.IsNullOrEmpty(battle.Id) || !Enum.IsDefined(typeof(CompanyBattlePhase), battle.Phase) ||
                (battle.RoadEventId == null ? battleContract == null || battleContract.EnemyIds == null ||
                    (battleContract.EnemyIds.Count == 0 && (!HasReleasedBossForce(battleContract) || battle.Phase == CompanyBattlePhase.Deployment)) :
                    state.RoadEvent == null || state.RoadEvent.Id != battle.RoadEventId || state.RoadEvent.Kind != CompanyRoadEventKind.SpitterSlugs || battle.ContractId != null) || battle.ReturnTile < 0 || battle.ReturnTile >= state.Terrain.Length ||
                battle.TargetTile < 0 || battle.TargetTile >= state.Terrain.Length ||
                (battle.DeployedIds != null && (battle.DeployedIds.Distinct().Count() != battle.DeployedIds.Length || battle.DeployedIds.Any(id => !state.Characters.Any(c => c.Id == id)))))
                throw new InvalidOperationException("Invalid saved battle transition.");
        }
        var ids = new HashSet<string>();
        foreach (var c in state.Contracts)
        {
            if (c == null || !ids.Add(c.Id) || !Enum.IsDefined(typeof(CompanyContractObjective), c.Objective) || c.Rating < 1 || c.Tile < 0 || c.Tile >= state.Terrain.Length || c.Reward < 0 ||
                c.DifficultyRevision < 0 || c.CalendarAgeDays < 0 || double.IsNaN(c.CalendarPressure) || double.IsInfinity(c.CalendarPressure) || c.CalendarPressure < 0 ||
                c.EnemyIds == null || c.DefeatedIds == null || c.EnemyIds.Distinct().Count() != c.EnemyIds.Count ||
                (c.Objective == CompanyContractObjective.RoutForce && !string.IsNullOrEmpty(c.BossCharacterId)) ||
                (c.EnemyIds.Count == 0 && (c.DefeatedIds.Count != 0 || c.Power != 0)) ||
                (c.EnemyIds.Count > 0 && (c.Power <= 0 || double.IsNaN(c.Power) || double.IsInfinity(c.Power))) ||
                c.DefeatedIds.Except(c.EnemyIds).Any() || c.EnemyIds.Any(id => !state.Characters.Any(person => person.Id == id)) ||
                (!string.IsNullOrEmpty(c.BossCharacterId) && (!c.EnemyIds.Contains(c.BossCharacterId) ||
                    !state.Characters.Any(person => person.Id == c.BossCharacterId && SpeciesPolicy.GetKind(person.Unit.Race) == SpeciesKind.Sapient))) ||
                (c.Objective == CompanyContractObjective.RoutBoss && c.EnemyIds.Count > 0 && string.IsNullOrEmpty(c.BossCharacterId)))
                throw new InvalidOperationException("Invalid contract/enemy identity.");
        }
    }
}
internal sealed class CompanyContractDeadlines : ICompanyDailySystem
{
    public CompanyDailyStage Stage { get { return CompanyDailyStage.Deadlines; } }
    public void Apply(CompanyCampaign state)
    {
        var expired = state.Contracts.Where(c => c.Status == CompanyContractStatus.Active && state.ElapsedDays >= c.Deadline).ToArray();
        foreach (var c in expired) CompanyContracts.Fail(state, c, false);
        if (expired.Length > 0) { state.PendingDecisionKind = "Contracts"; state.PendingDecision = expired.Length + " contract(s) expired. Targets were removed and reputation decreased."; }
    }
}

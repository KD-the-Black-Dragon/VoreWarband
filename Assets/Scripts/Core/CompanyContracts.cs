// Added by the VoreWar mercenary-company conversion fork.
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
[Serializable]
internal sealed class CompanyContract
{
    public string Id, TownId;
    public int Rating, Reward, Tile, Deadline;
    public double Power;
    public int DifficultyRevision, CalendarAgeDays;
    public double CalendarPressure;
    public CompanyContractStatus Status;
    public List<string> EnemyIds = new List<string>();
    public List<string> DefeatedIds = new List<string>();
}

internal static class CompanyContracts
{
    internal const int DeadlineDays = 30, RefreshDays = 7;
    internal const int DifficultyRevision = 1, ProtectedDays = 21;
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
        return .05 * days + .00015 * days * days;
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
    static int OfferRating(CompanyCampaign state, int offerIndex, double pressure, double companyPower, int maximumRating)
    {
        int maximum = (int)Math.Min(int.MaxValue, (long)maximumRating + RoundedPressureBonus(state, pressure));
        int minimum = MinimumOfferRating(companyPower, maximum);
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
            var enemy = CompanyEconomy.Create(state, race, state.Next(3), false, 1 + state.Next(Math.Max(1, rating)));
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
    static List<CompanyCharacter> Generate(CompanyCampaign state, int rating)
    {
        Race[] races = SpeciesPolicy.GetAvailable(SpeciesUse.Enemy);
        if (races.Length == 0) throw new InvalidOperationException(SpeciesPolicy.UnavailableMessage);
        double budget = Budget(rating), low = budget * .92, high = budget * 1.08;
        // Generate a bounded candidate pool once, then compose distinct candidates within the band.
        var pool = Enumerable.Range(0, 48).Select(_ => Enemy(state, races[state.Next(races.Length)], rating)).ToArray();
        var powers = pool.ToDictionary(c => c.Id, c => Power(c.Unit));
        for (int attempt = 0; attempt < 40; attempt++)
        {
            var force = new List<CompanyCharacter>(); double sum = 0;
            for (int pick = 0; pick < 96 && sum * CompanyPower.FormationMultiplier(force.Count) < low; pick++)
            {
                var c = pool[state.Next(pool.Length)]; double power = powers[c.Id];
                if (force.Contains(c) || (sum + power) * CompanyPower.FormationMultiplier(force.Count + 1) > high) continue;
                c.Employment = CompanyEmployment.Unavailable; c.Unit.Side = 1;
                force.Add(c); sum += power;
            }
            if (force.Count > 0 && sum * CompanyPower.FormationMultiplier(force.Count) >= low) return force;
        }
        throw new InvalidOperationException("The enabled species cannot currently fill this contract's power band. No fallback species were added.");
    }
    internal static void EnsureTown(CompanyCampaign state, CompanyTown town)
    {
        if (state.Preview || (town.ContractRefreshDay > state.ElapsedDays)) return;
        foreach (var old in state.Contracts.Where(c => c.TownId == town.Id && c.Status == CompanyContractStatus.Offered).ToArray())
        {
            foreach (string id in old.EnemyIds) state.Characters.RemoveAll(c => c.Id == id);
            state.Contracts.Remove(old);
        }
        double pressure = CalendarPressure(state);
        int reputationMaximum = ReputationRatingCeiling(state);
        double companyPower = OfferReferencePower(state);
        int reputationMinimum = MinimumOfferRating(companyPower, reputationMaximum);
        for (int i = 0; i < 4; i++)
        {
            int rating = OfferRating(state, i, pressure, companyPower, reputationMaximum);
            int pressureAge = CalendarAgeDays(state);
            var possible = Enumerable.Range(0, state.Terrain.Length).Where(tile =>
                !state.Towns.Any(t => t.Tile == tile) &&
                !state.Contracts.Any(c => (c.Status == CompanyContractStatus.Active || c.Status == CompanyContractStatus.Offered) && c.Tile == tile) &&
                Distance(state, tile, town.Tile) >= 2 && Distance(state, tile, town.Tile) <= 6).ToArray();
            if (possible.Length == 0) break;
            List<CompanyCharacter> force = null;
            int requestedRating = rating;
            for (int retry = 0; retry < 4; retry++)
            {
                int candidateRating = Math.Max(reputationMinimum, requestedRating - retry);
                if (candidateRating < 1 || (retry > 0 && candidateRating == Math.Max(reputationMinimum, requestedRating - retry + 1))) break;
                try { force = Generate(state, candidateRating); rating = candidateRating; break; }
                catch (InvalidOperationException) { } // If a composition is infeasible, try a nearby lower quote without inventing species.
            }
            if (force == null) continue; // Safe empty pool: do not invent unapproved enemies.
            var contract = new CompanyContract { Id = Guid.NewGuid().ToString("N"), TownId = town.Id, Rating = rating,
                Reward = Reward(rating, state.Next(21) - 10),
                Tile = possible[state.Next(possible.Length)], Power = CompanyPower.Force(force.Select(c => c.Unit)), EnemyIds = force.Select(c => c.Id).ToList(),
                DifficultyRevision = DifficultyRevision, CalendarAgeDays = pressureAge, CalendarPressure = pressure };
            state.Contracts.Add(contract); state.Characters.AddRange(force);
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
        if (contract.Status != CompanyContractStatus.Active || Survivors(state, contract).Any()) return false;
        contract.Status = CompanyContractStatus.Completed; state.Gold += contract.Reward;
        state.Reputation++; state.TotalGoldEarned += contract.Reward; state.ContractsCompleted++;
        return true;
    }
    internal static void Validate(CompanyCampaign state)
    {
        if (state.Contracts == null || state.Memorial == null) throw new InvalidOperationException("Missing contract records.");
        if (state.CompanyRulesVersion != 5) throw new InvalidOperationException("Unsupported company rules version.");
        if (state.Battle != null)
        {
            var battle = state.Battle;
            if (string.IsNullOrEmpty(battle.Id) || !Enum.IsDefined(typeof(CompanyBattlePhase), battle.Phase) ||
                (battle.RoadEventId == null ? !state.Contracts.Any(c => c.Id == battle.ContractId) :
                    state.RoadEvent == null || state.RoadEvent.Id != battle.RoadEventId || state.RoadEvent.Kind != CompanyRoadEventKind.SpitterSlugs || battle.ContractId != null) || battle.ReturnTile < 0 || battle.ReturnTile >= state.Terrain.Length ||
                battle.TargetTile < 0 || battle.TargetTile >= state.Terrain.Length ||
                (battle.DeployedIds != null && (battle.DeployedIds.Distinct().Count() != battle.DeployedIds.Length || battle.DeployedIds.Any(id => !state.Characters.Any(c => c.Id == id)))))
                throw new InvalidOperationException("Invalid saved battle transition.");
        }
        var ids = new HashSet<string>();
        foreach (var c in state.Contracts)
        {
            if (c == null || !ids.Add(c.Id) || c.Rating < 1 || c.Tile < 0 || c.Tile >= state.Terrain.Length || c.Reward < 0 ||
                c.DifficultyRevision < 0 || c.CalendarAgeDays < 0 || double.IsNaN(c.CalendarPressure) || double.IsInfinity(c.CalendarPressure) || c.CalendarPressure < 0 ||
                c.EnemyIds == null || c.DefeatedIds == null || c.EnemyIds.Count == 0 || c.EnemyIds.Distinct().Count() != c.EnemyIds.Count ||
                c.DefeatedIds.Except(c.EnemyIds).Any() || c.EnemyIds.Any(id => !state.Characters.Any(person => person.Id == id)))
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

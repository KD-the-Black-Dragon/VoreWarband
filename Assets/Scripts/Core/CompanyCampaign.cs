// Modified for the VoreWar mercenary-company conversion.
// 2026-11-08: Persist company identity and terminal-history state (spec 40E).
// 2026-11-08: Persist the calendar-difficulty enrollment day for safe campaign migration (spec 40F).
// 2026-11-07: Persist permanent Grudge wage raises after post-battle events (spec 40D).
// 2026-11-06: Keep action rollback in memory and remove action-level autosave work.
// Modified for the VoreWar mercenary-company conversion.
// 2026-11-06: Persist optional travel destination intent with safe old-save defaults (spec 40B).
// 2026-11-06: Record balance identity on new campaigns (spec 40A).
// 2026-11-05: Persist upkeep enrollment, debt and deadline and stop daily processing on insolvency (spec 38 phase B).
// 2026-11-05: Persist cultivated fields, clear saved road terrain and use a provisional six-action new-campaign day (spec 36).
// 2026-11-05: Generate larger six-town regions with saved roads while preserving older campaign geography (spec 35).
// See CHANGES.md and License.txt for GPL-3.0 terms.
// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-01: Company campaign foundation, independent of the retired empire loop (spec 06).
// 2026-11-01: Candidate/former identity, wage decisions and shared equipment ownership (specs 07–08).
// 2026-11-01: Persist contracts, battle transition phases, revival preference and active-campaign memorial/statistics (specs 09–10).
// 2026-11-01: Persist explicit rest-day intent, validate injuries and advance recovery through the resumable daily clock (spec 11).
// 2026-11-01: Persist a separate wage notice when departures and new threats coincide (spec 11 feedback).
// 2026-11-02: Persist road-event choices/cooldowns and block ordinary actions while an event awaits resolution (spec 13).
// 2026-11-02: Require a full daily action budget to start resting while keeping ordinary turn ending distinct (spec 14).
// 2026-11-02: Generate requested 80 by 60 new regions while retaining six towns and existing saved dimensions (spec 14 feedback).
// 2026-11-03: Store logical save slot labels separately from campaign progress (spec 19).
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
internal sealed class CompanyCharacter
{
    public string Id;
    public Unit Unit;
    public string Biography;
    public int Debt;
    public int BaseWage = 18;
    public int GrudgeWageSurcharge;
    public int DebtDueDay = -1;
    public int WarningShownDay = -1;
    public CompanyEmployment Employment;
    public string TownId;
    public int Focus;
    public Stat[] LevelChoices;

}

[Serializable]
internal sealed class CompanyTown
{
    public string Id;
    public string Name;
    public int Tile;
    public int RecruitRefreshDay;
    public int ContractRefreshDay;
}

[Serializable]
internal sealed class CompanyMember
{
    public string CharacterId;
    public bool Reserve;
}

// Mutable state is owned by this root; UI and legacy World are never serialized here.
[Serializable]
internal sealed class CompanyCampaign
{
    public int Schema = 1;
    public string BalanceRevision;
    public bool DeveloperBalance;
    public string Id = Guid.NewGuid().ToString("N");
    public bool Preview = true;
    public bool Ironman;
    public int Seed;
    public uint RandomState;
    public int Width = 20;
    public int Height = 12;
    public int[] Terrain; // 0 grass, 1 forest, 2 hills, 3 desert (provisional terrain tuning).
    public bool[] Discovered;
    public bool[] Roads; // Null in older campaigns: preserve their existing geography without generating roads on load.
    public bool[] Farms; // Saved presentation mask over cultivated grassland, excluding road and town tiles.
    public int Position;
    public int? TravelDestination;
    public int ElapsedDays;
    public int DailyActions = 6; // Provisional tuning, not a finalized design value.
    public int Actions = 6;
    public int TownEntryCost;
    public int Gold;
    public int Reputation;
    public List<Item> Inventory = new List<Item>();
    public string PendingDecisionKind;
    public string DeferredWageDecision; // A simultaneous financial warning follows a departure acknowledgment.
    public string DeferredDecisionKind; // Null in older saves denotes the original Payday warning.
    public string[] PendingCharacterIds = new string[0];
    public string LastVisitedTownId;
    public bool InBattle;
    public string SaveSlot, SaveLabel;
    public int SaveKind;

    public int CompanyRulesVersion = 5;
    // Existing saves enroll here on upgrade, so loading them cannot jump straight into late pressure.
    public int CalendarDifficultyStartDay;
    public bool LeaderRevival = true;
    public bool Ended;
    public string EndReason;
    public string CompanyName;
    public string EndingId, EndingKind;
    public int LeaderDigestions;
    public int TotalGoldEarned, MercenariesHired, ContractsCompleted, EnemiesDefeated;
    public List<CompanyContract> Contracts = new List<CompanyContract>();
    public List<CompanyMemorial> Memorial = new List<CompanyMemorial>();
    public CompanyBattleRecord Battle;
    public CompanyRoadEvent RoadEvent;
    public int NextRoadEventDay;
    public int NextPayday = 7;
    // Version zero denotes a pre-upkeep save. Begin/Bind explicitly enroll it without back billing.
    public int EconomyVersion;
    public int UpkeepBillsIssued;
    public int UpkeepDebt;
    public int UpkeepDueDay;
    public string LeaderId;
    public List<CompanyCharacter> Characters = new List<CompanyCharacter>();
    public List<CompanyMember> Members = new List<CompanyMember>();
    public List<CompanyTown> Towns = new List<CompanyTown>();
    public bool UpdatingDay;
    public int DailyStage;
    public string PendingDecision;
    public int RestDaysRemaining;
    public bool RestingDay; // Saved intent for an interrupted daily update, not inferred from queued rest.

    public static CompanyCampaign Generate(int seed, bool ironman)
    {
        var state = new CompanyCampaign { Seed = seed, RandomState = unchecked((uint)seed) | 1u, Ironman = ironman,
            BalanceRevision = BalanceAuthoring.OfficialRevision, DeveloperBalance = BalanceAuthoring.IsDraft };
        CompanyWorldGeneration.Generate(state);
        state.Reveal();
        return state;
    }

    public int Next(int maximum)
    {
        uint n = RandomState; n ^= n << 13; n ^= n >> 17; n ^= n << 5;
        RandomState = n;
        return (int)(n % (uint)maximum);
    }

    public int MoveCost(int tile) { return Terrain[tile] == 1 || Terrain[tile] == 2 ? 2 : 1; }
    public bool Busy { get { return Ended || InBattle || (Battle != null && Battle.Phase != CompanyBattlePhase.Returned) || UpdatingDay || !string.IsNullOrEmpty(PendingDecision) || CompanyRoadEvents.AwaitingChoice(this); } }
    public bool CanStartRest { get { return !Busy && Actions == DailyActions; } }
    public bool Spend(int cost)
    {
        if (Busy || cost < 0 || cost > Actions || (Actions == 0 && cost > 0)) return false;
        Actions -= cost;
        return true;
    }
    public bool Move(int tile)
    {
        if (tile < 0 || tile >= Terrain.Length) return false;
        int distance = Math.Abs(tile % Width - Position % Width) + Math.Abs(tile / Width - Position / Width);
        if (distance != 1 || !Spend(MoveCost(tile))) return false;
        Position = tile;
        Reveal();
        return true;
    }
    internal void Reveal()
    {
        int x = Position % Width, y = Position / Width;
        for (int dy = -2; dy <= 2; dy++)
            for (int dx = -2; dx <= 2; dx++)
                if (x + dx >= 0 && x + dx < Width && y + dy >= 0 && y + dy < Height)
                    Discovered[(y + dy) * Width + x + dx] = true;
    }

    public void Validate()
    {
        CompanyEconomy.Validate(this);
        CompanyContracts.Validate(this);
        CompanyRoadEvents.Validate(this);
        Guid parsedId;
        if (Schema != 1 || !Guid.TryParseExact(Id, "N", out parsedId) || Width < 4 || Height < 4 || Width > CompanyWorldGeneration.MaximumDimension || Height > CompanyWorldGeneration.MaximumDimension ||
            Terrain == null || Discovered == null || Terrain.Length != Width * Height || Discovered.Length != Terrain.Length ||
            (Roads != null && Roads.Length != Terrain.Length) || (Farms != null && Farms.Length != Terrain.Length) ||
            Position < 0 || Position >= Terrain.Length || DailyActions < 2 || DailyActions > 100 || Actions < 0 || Actions > DailyActions ||
            ElapsedDays < 0 || CalendarDifficultyStartDay < 0 || CalendarDifficultyStartDay > ElapsedDays ||
            NextPayday < 7 || TownEntryCost < 0 || Gold < 0 || RestDaysRemaining < 0 || RestDaysRemaining > 365 ||
            DailyStage < 0 || DailyStage > 5 || RandomState == 0 || Characters == null || Members == null || Towns == null || Towns.Count == 0 ||
            Terrain.Any(t => t < 0 || t > 3)) throw new InvalidOperationException("Invalid company campaign state.");
        if (TravelDestination.HasValue && (TravelDestination.Value < 0 || TravelDestination.Value >= Terrain.Length))
            throw new InvalidOperationException("Invalid travel destination.");
        foreach (var character in Characters) CompanyInjuries.Validate(character.Unit);
        if (RestingDay && !UpdatingDay) throw new InvalidOperationException("Rest recovery must belong to an active daily update.");
        var ids = new HashSet<string>();
        var units = new HashSet<Unit>();
        foreach (var c in Characters)
            if (c == null || string.IsNullOrEmpty(c.Id) || !ids.Add(c.Id) || c.Unit == null || !units.Add(c.Unit) || SpeciesPolicy.IsRetired(c.Unit.Race) || c.Debt < 0 || c.GrudgeWageSurcharge < 0)
                throw new InvalidOperationException("Invalid character registry.");
        var members = new HashSet<string>();
        foreach (var m in Members)
            if (m == null || !ids.Contains(m.CharacterId) || !members.Add(m.CharacterId)) throw new InvalidOperationException("Invalid company membership.");
        if (!Preview && !Ended && (string.IsNullOrEmpty(LeaderId) || !members.Contains(LeaderId))) throw new InvalidOperationException("Company leader is missing.");
        if (!Preview && !Ended && Members.First(m => m.CharacterId == LeaderId).Reserve) throw new InvalidOperationException("The company leader must remain active.");
        var townIds = new HashSet<string>(); var locations = new HashSet<int>();
        foreach (var t in Towns)
            if (t == null || string.IsNullOrEmpty(t.Id) || !townIds.Add(t.Id) || t.Tile < 0 || t.Tile >= Terrain.Length || !locations.Add(t.Tile))
                throw new InvalidOperationException("Invalid towns.");
        CompanyWorldGeneration.ApplyLandUse(this);
    }
}

internal enum CompanyDailyStage { Wages, Deadlines, Refresh, Recovery, Sparring }
internal interface ICompanyDailySystem
{
    CompanyDailyStage Stage { get; }
    // Set PendingDecision after applying this stage to interrupt rest for a player choice.
    void Apply(CompanyCampaign state);
}

internal sealed class CompanyClock
{
    readonly Dictionary<CompanyDailyStage, ICompanyDailySystem> systems = new Dictionary<CompanyDailyStage, ICompanyDailySystem>();
    public void Register(ICompanyDailySystem system) { systems.Add(system.Stage, system); }
    public void EndDay(CompanyCampaign state, bool resting = false)
    {
        if (state.Ended || !string.IsNullOrEmpty(state.PendingDecision)) return;
        if (resting && !state.UpdatingDay && state.Actions != state.DailyActions)
            throw new InvalidOperationException("Rest requires all daily actions. End the turn first.");
        if (!state.UpdatingDay) { state.UpdatingDay = true; state.DailyStage = 0; state.ElapsedDays++; state.RestingDay = resting; }
        Resume(state);
    }
    public void Resume(CompanyCampaign state)
    {
        while (!state.Ended && state.UpdatingDay && string.IsNullOrEmpty(state.PendingDecision) && state.DailyStage < 5)
        {
            // Commit a stage only after it succeeds; rollback all campaign mutations on error.
            var backup = CompanySnapshot.Take(state);
            try
            {
                ICompanyDailySystem system;
                if (systems.TryGetValue((CompanyDailyStage)state.DailyStage, out system)) system.Apply(state);
                state.DailyStage++;
            }
            catch { CompanySnapshot.Restore(state, backup); throw; }
        }
        if (state.Ended)
        {
            state.UpdatingDay = false; state.RestingDay = false;
            state.RestDaysRemaining = 0; state.DailyStage = 0;
            return;
        }
        if (state.DailyStage == 5 && string.IsNullOrEmpty(state.PendingDecision))
        {
            state.Actions = state.DailyActions;
            state.UpdatingDay = false;
            if (state.RestingDay && state.RestDaysRemaining > 0) state.RestDaysRemaining--;
            state.RestingDay = false;
        }
    }
}

// Modified for the VoreWar mercenary-company conversion.
// 2026-11-08: Rebind generic tiered weapon labels in saved company inventory.
// 2026-11-08: Mark upkeep insolvency as a uniquely identified 40E ending (spec 40E).
// 2026-11-08: Enroll existing campaigns into calendar difficulty without backdating pressure (spec 40F).
// 2026-11-07: Persist Grudge raises on level-ups and preview their next wage (spec 40D).
// 2026-11-06: Generate reputation-eligible spellbooks for experienced mages (spec 40C).
// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-06: Enroll older saved characters without rerolling their base stats (spec 40A).
// 2026-11-05: Implement upkeep-first weekly billing, grace-week insolvency and accelerating wages with safe save enrollment (spec 38 phase B).
// 2026-11-04: Apply explainable healthy power and approved capped encounter scaling (spec 34 slice 3).
// 2026-11-04: Charge quoted recruitment gear, compensate starting funds and generate tiered loadouts (spec 34 slice 5).
// 2026-11-01: Canonical recruitment, advance wages, persistent debt and player-owned repayment (spec 07).
// 2026-11-01: Migrate accepted saves non-destructively, count hires and replenish local contract pools (specs 09–10).
// 2026-11-01: Upgrade saved daily intent without replaying days and identify the leader for old-injury immunity (spec 11).
// 2026-11-01: Add provisional challenge-scaled progression, migrate sentinel XP and separate departure notices from queued wage warnings (spec 11 feedback).
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
// 2026-11-03: Implement four active stats and agreed combat rules (spec 29).
using System;
using System.Collections.Generic;
using System.Linq;

internal static class CompanyEconomy
{
    internal const int StartingGold = 260;
    internal const int InitialUpkeep = 40, WeeklyUpkeepIncrease = 10;
    internal const int RecruitRefreshDays = 7;
    internal static Race[] Sapients { get { return SpeciesPolicy.GetAvailable(SpeciesUse.Mercenary); } }
    internal static T Random<T>(CompanyCampaign state, Func<T> operation)
    {
        var old = State.Rand; var unity = UnityEngine.Random.state;
        try { int seed = state.Next(int.MaxValue); State.Rand = new Rand(seed); UnityEngine.Random.InitState(seed); return operation(); }
        finally { State.Rand = old; UnityEngine.Random.state = unity; }
    }
    internal static CompanyCharacter Create(CompanyCampaign state, Race race, int focus, bool leader, int level = 1)
    {
        if (!SpeciesPolicy.IsAllowed(race, leader ? SpeciesUse.Leader : SpeciesUse.Mercenary)) throw new InvalidOperationException("That species is not enabled for recruitment.");
        return Random(state, () =>
        {
            state.DeveloperBalance |= BalanceAuthoring.IsDraft;
            var unit = new Unit(0, race, 0, true, leader ? UnitType.Leader : UnitType.Soldier,
                itemContext: CompanyEquipment.Repository, companyLoadout: true);
            var record = new CompanyCharacter { Id = Guid.NewGuid().ToString("N"), Unit = unit, Focus = focus,
                BaseWage = 18 + state.Next(5), Employment = CompanyEmployment.Candidate,
                Biography = new[] { "A caravan guard seeking a steadier company.", "A local hunter looking for a life beyond familiar roads.", "An apprentice who left home to see the wider world.", "A former watch recruit hoping to prove themselves." }[state.Next(4)] };
            unit.PermanentInjuryImmune = leader;
            if (leader) foreach (var stat in WarbandStats.Active) unit.SetStatBase(stat, unit.GetStatBase(stat) + 3);
            for (int i = 1; i < level; i++) unit.LevelUp((Stat)(focus == 1 ? (int)Stat.Agility : focus == 2 ? (int)Stat.Intelligence : (int)Stat.Strength));
            if (level > 1) unit.SetExp(unit.GetExperienceRequiredForLevel(level - 1));
            string weaponId = level == 1 ? (focus == 1 ? "weapon.bow" : "weapon.mace") :
                CompanyEquipment.Generated(state, focus == 1 ? CompanySlot.Ranged : CompanySlot.Melee, level).Id;
            Item weapon = CompanyEquipment.Create(weaponId);
            unit.SetItem(weapon, focus == 1 ? (int)CompanySlot.Ranged : (int)CompanySlot.Melee);
            if (focus == 2)
            {
                unit.SetItem(CompanyEquipment.Create("spell.bolt"), (int)CompanySlot.Spell1);
                if (level >= 3)
                {
                    int maxTier = level >= 10 ? 3 : level >= 5 ? 2 : 1;
                    var books = CompanyEquipment.Available(state).Where(d => d.Slot == CompanySlot.Spell1 &&
                        d.Tier <= maxTier && d.Id != "spell.bolt").ToArray();
                    if (books.Length > 0) unit.SetItem(CompanyEquipment.Create(books[state.Next(books.Length)].Id), (int)CompanySlot.Spell2);
                }
            }
            if (level >= 3) { var armor = CompanyEquipment.Generated(state, state.Next(2) == 0 ? CompanySlot.Helmet : CompanySlot.Torso, level); unit.SetItem(CompanyEquipment.Create(armor.Id), (int)armor.Slot); }
            if (level >= 6) { var armor = CompanyEquipment.Generated(state, state.Next(2) == 0 ? CompanySlot.Legs : CompanySlot.Arms, level); unit.SetItem(CompanyEquipment.Create(armor.Id), (int)armor.Slot); }
            unit.UpdateSpells(); unit.Health = unit.MaxHealth; unit.RestoreManaPct(1);
            return record;
        });
    }
    internal static void Begin(CompanyCampaign state, CompanyCharacter leader)
    {
        if (state.Characters.Count != 0 || !state.Preview) throw new InvalidOperationException("Create a new company rather than replace an existing leader.");
        state.Characters.Add(leader); leader.Employment = CompanyEmployment.Employed;
        state.Members.Add(new CompanyMember { CharacterId = leader.Id }); state.LeaderId = leader.Id;
        state.Gold = StartingGold; state.Preview = false; state.EconomyVersion = 1;
        AddOwnedGear(state, leader);
        Visit(state, state.Towns.First(t => t.Tile == state.Position));
    }
    internal static void Bind(CompanyCampaign state)
    {
        if (string.IsNullOrEmpty(state.BalanceRevision)) state.BalanceRevision = BalanceAuthoring.LegacyRevision;
        if (!state.Preview && state.EconomyVersion == 0)
        {
            // Existing campaigns start with the first bill at their next scheduled payday.
            // Existing advances, debt, accepted rewards and possessions remain untouched.
            state.EconomyVersion = 1; state.UpkeepBillsIssued = 0;
            state.UpkeepDebt = 0; state.UpkeepDueDay = 0;
        }
        bool upgradingStats = state.Characters.Any(person => person.Unit.NeedsFourStatUpgrade);
        foreach (var person in state.Characters)
        {
            UnitSpeciesBalance.EnrollLegacy(person.Unit);
            person.Unit.EnsureFourStats();
            if (upgradingStats) person.LevelChoices = null;
            else if (person.LevelChoices != null) person.LevelChoices = person.LevelChoices.Select(WarbandStats.Canonical)
                .Where(WarbandStats.Active.Contains).Distinct().ToArray();
        }

        if (upgradingStats && state.Battle?.Tactical?.units != null)
            foreach (var actor in state.Battle.Tactical.units)
            {
                actor.Unit.EnsureFourStats();
                actor.Movement = Math.Min(actor.Movement, actor.CurrentMaxMovement());
            }
        if (state.Preview)
        {
            if (state.Inventory == null) state.Inventory = new List<Item>();
            if (state.PendingCharacterIds == null) state.PendingCharacterIds = new string[0];
        }
        if (state.CompanyRulesVersion < 2)
        {
            state.Contracts = new List<CompanyContract>(); state.Memorial = new List<CompanyMemorial>();
            state.LeaderRevival = true; state.CompanyRulesVersion = 2;
            state.MercenariesHired = state.Characters.Count(c => c.Id != state.LeaderId && (c.Employment == CompanyEmployment.Employed || c.Employment == CompanyEmployment.Former));
        }
        if (state.CompanyRulesVersion < 3)
        {
            state.RestingDay = state.UpdatingDay && state.RestDaysRemaining > 0;
            state.CompanyRulesVersion = 3;
        }
        if (state.CompanyRulesVersion < 4)
        {
            foreach (var c in state.Characters.Where(c => c.Unit.Experience >= 99999999))
            {
                // Earlier generated veterans inherited the old cap sentinel as their starting XP.
                c.Unit.SetExp(CompanyProgression.Threshold(c.Unit.Level - 1, 1)); c.LevelChoices = null;
            }
            if (state.PendingDecisionKind == "Departures")
            {
                state.PendingDecision = CompanyWages.DepartureMessage(state);
                var threats = Threatened(state).ToArray();
                if (threats.Length > 0) state.DeferredWageDecision = CompanyWages.WageMessage(threats, false);
            }
            state.CompanyRulesVersion = 4;
        }
        if (state.CompanyRulesVersion < 5)
        {
            // Start the protected period on the current saved day; never punish a returning campaign for elapsed time.
            state.CalendarDifficultyStartDay = state.ElapsedDays;
            state.CompanyRulesVersion = 5;
        }
        foreach (var c in state.Characters)
        {
            c.Unit.BindCompanyItems(CompanyEquipment.Repository);
            c.Unit.PermanentInjuryImmune = c.Id == state.LeaderId;
        }
        if (state.Inventory != null)
            foreach (var item in state.Inventory) CompanyEquipment.BindCompanyItem(item);
    }
    internal static int WageAtLevel(CompanyCampaign state, CompanyCharacter c, int level)
    {
        if (c.Id == state.LeaderId) return 0;
        return (int)Math.Min(int.MaxValue, (long)BaseWageAtLevel(c, level) + c.GrudgeWageSurcharge);
    }
    static int BaseWageAtLevel(CompanyCharacter c, int level)
    {
        long steps = Math.Max(0, level - 1);
        long veteranSteps = Math.Max(0, steps - 3);
        return (int)Math.Min(int.MaxValue, c.BaseWage + 3 * steps + 2 * steps * steps +
            2 * veteranSteps * veteranSteps * veteranSteps);
    }
    internal static int WageForNextLevel(CompanyCampaign state, CompanyCharacter c)
    {
        if (c.Id == state.LeaderId) return 0;
        int next = c.Unit.Level + 1;
        int ordinaryIncrease = Math.Max(0, BaseWageAtLevel(c, next) - BaseWageAtLevel(c, c.Unit.Level));
        return (int)Math.Min(int.MaxValue, (long)WageAtLevel(state, c, next) + CompanyPostBattleTraits.FutureGrudgeIncrease(c, ordinaryIncrease));
    }
    internal static int Wage(CompanyCampaign state, CompanyCharacter c) => WageAtLevel(state, c, c.Unit.Level);
    internal static int Upkeep(CompanyCampaign state)
    { return checked(InitialUpkeep + WeeklyUpkeepIncrease * state.UpkeepBillsIssued); }
    internal static long WeeklyWages(CompanyCampaign state)
    { return state.Characters.Where(c => c.Employment == CompanyEmployment.Employed).Sum(c => (long)Wage(state, c)); }
    internal static void PayUpkeep(CompanyCampaign state)
    {
        if (state.Ended || state.InBattle || state.UpkeepDebt <= 0 || state.Gold < state.UpkeepDebt)
            throw new InvalidOperationException("The entire upkeep debt must be affordable.");
        state.Gold -= state.UpkeepDebt; state.UpkeepDebt = 0; state.UpkeepDueDay = 0;
    }
    internal static string UpkeepWarning(CompanyCampaign state)
    {
        if (state.UpkeepDebt == 0) return "The company has no outstanding upkeep debt.";
        return "The company is in critical danger of insolvency. Pay its upkeep debt of " + state.UpkeepDebt +
            " gold before the next payday in " + Math.Max(0, state.UpkeepDueDay - state.ElapsedDays) +
            " days, or the company will cease operating. Use Pay Upkeep on the leader's company entry.";
    }
    internal static int AdvanceWages(CompanyCampaign state, CompanyCharacter c)
    { return (int)Math.Ceiling(Wage(state, c) * Math.Max(1, Math.Min(7, state.NextPayday - state.ElapsedDays)) / 7.0); }
    internal static int HirePrice(CompanyCampaign state, CompanyCharacter c)
    { return checked(c.Debt + AdvanceWages(state, c) + CompanyEquipment.GearPrice(c.Unit)); }
    internal static void Refresh(CompanyCampaign state, CompanyTown town)
    {
        state.Characters.RemoveAll(c => c.Employment == CompanyEmployment.Candidate && c.TownId == town.Id);
        var races = Sapients; int count = races.Length == 0 ? 0 : 4 + Math.Min(6, state.Reputation / 30);
        for (int i = 0; i < count; i++)
        {
            Race race = races[i < races.Length ? i : state.Next(races.Length)];
            int level = 1 + state.Reputation / 40 + (state.Next(40) < state.Reputation % 40 ? 1 : 0);
            int focus = race == Race.Avians ? 1 : race == Race.Lizards ? 2 : 0;
            CompanyCharacter c = null;
            for(int attempt=0;attempt<8;attempt++)
            {
                var candidate=Create(state,race,focus,false,attempt<4?level:1);
                if(CompanyPower.Healthy(candidate.Unit)<=CompanyPower.RecruitCeiling(state.Reputation)){c=candidate;break;}
            }
            if(c==null)continue; // Never invent an unapproved species or silently exceed the power ceiling.
            c.TownId = town.Id; state.Characters.Add(c);
        }
        town.RecruitRefreshDay = checked(state.ElapsedDays + RecruitRefreshDays);
    }
    internal static void Visit(CompanyCampaign state, CompanyTown town)
    {
        if (state.Preview) return;
        CompanyContracts.EnsureTown(state, town);
        if (town.RecruitRefreshDay == 0 || town.RecruitRefreshDay <= state.ElapsedDays) Refresh(state, town);
        if (state.LastVisitedTownId != town.Id)
        {
            foreach (var former in state.Characters.Where(c => c.Employment == CompanyEmployment.Former))
                former.TownId = state.Next(3) == 0 ? town.Id : null;
            state.LastVisitedTownId = town.Id;
        }
    }
    internal static IEnumerable<CompanyCharacter> Offers(CompanyCampaign state, CompanyTown town)
    { return state.Characters.Where(c => c.TownId == town.Id && (c.Employment == CompanyEmployment.Candidate || c.Employment == CompanyEmployment.Former)); }
    internal static void Hire(CompanyCampaign state, CompanyCharacter c, CompanyTown town)
    {
        if (state.Busy || state.InBattle || town.Tile != state.Position || !Offers(state, town).Contains(c)) throw new InvalidOperationException("That candidate is not available here.");
        int price = HirePrice(state, c); if (state.Gold < price) throw new InvalidOperationException("Not enough gold for equipment, advance wages and any debt.");
        state.Gold -= price; c.Debt = 0; c.DebtDueDay = -1; c.WarningShownDay = -1;
        c.Employment = CompanyEmployment.Employed; c.TownId = null;
        state.Members.Add(new CompanyMember { CharacterId = c.Id }); state.MercenariesHired++; AddOwnedGear(state, c);
    }
    static void AddOwnedGear(CompanyCampaign state, CompanyCharacter c)
    { foreach (Item item in c.Unit.Items.Where(i => i != null)) if (!state.Inventory.Contains(item)) state.Inventory.Add(item); }
    internal static void Leave(CompanyCampaign state, CompanyCharacter c)
    {
        if (c.Id == state.LeaderId || state.InBattle) throw new InvalidOperationException("The company leader cannot leave.");
        state.Members.RemoveAll(m => m.CharacterId == c.Id); c.Employment = CompanyEmployment.Former;
        c.TownId = null; c.DebtDueDay = -1;
        foreach (var item in c.Unit.Items.Where(i => i != null)) state.Inventory.Remove(item);
    }
    internal static void Pay(CompanyCampaign state, CompanyCharacter c)
    {
        if (state.InBattle || c.Debt <= 0 || state.Gold < c.Debt || c.Employment != CompanyEmployment.Employed) throw new InvalidOperationException("The entire personal debt must be affordable.");
        state.Gold -= c.Debt; c.Debt = 0; c.DebtDueDay = -1; c.WarningShownDay = -1;
    }
    internal static CompanyCharacter[] Threatened(CompanyCampaign state)
    { return state.Characters.Where(c => c.Employment == CompanyEmployment.Employed && c.Debt > 0 && c.DebtDueDay <= state.ElapsedDays + 1).ToArray(); }
    internal static void PayThreatened(CompanyCampaign state)
    {
        var threatened = Threatened(state); int total = threatened.Sum(c => c.Debt);
        if (total <= 0 || state.Gold < total) throw new InvalidOperationException("Not enough gold to pay this group in full.");
        foreach (var c in threatened) Pay(state, c);
    }
    internal static void Level(CompanyCampaign state, CompanyCharacter c, Stat stat)
    {
        if (state.InBattle || state.Busy || !state.Members.Any(m => m.CharacterId == c.Id) || !c.Unit.HasEnoughExpToLevelUp() || c.LevelChoices == null || !c.LevelChoices.Contains(stat))
            throw new InvalidOperationException("That level-up choice is unavailable.");
        int health = c.Unit.Health, maximum = c.Unit.MaxHealth, oldLevel = c.Unit.Level;
        int normalIncrease = Math.Max(0, BaseWageAtLevel(c, oldLevel + 1) - BaseWageAtLevel(c, oldLevel));
        int grudgeIncrease = CompanyPostBattleTraits.FutureGrudgeIncrease(c, normalIncrease);
        Random(state, () => { c.Unit.LevelUp(stat); return 0; });
        c.GrudgeWageSurcharge = (int)Math.Min(int.MaxValue, (long)c.GrudgeWageSurcharge + grudgeIncrease);
        c.Unit.Health = Math.Min(c.Unit.MaxHealth, health + c.Unit.MaxHealth - maximum);
        c.LevelChoices = null;
    }
    internal static Stat[] LevelChoices(CompanyCampaign state, CompanyCharacter c)
    {
        if (!c.Unit.HasEnoughExpToLevelUp()) return new Stat[0];
        if (c.LevelChoices != null) c.LevelChoices = c.LevelChoices.Select(WarbandStats.Canonical).Where(s => WarbandStats.Active.Contains(s)).Distinct().ToArray();
        if (c.LevelChoices == null || c.LevelChoices.Length == 0) c.LevelChoices = Random(state, () => c.Unit.GetLevelUpPossibilities(true).Where(s => s != Stat.Leadership).ToArray());
        return c.LevelChoices;
    }
    internal static void Validate(CompanyCampaign state)
    {
        if (state.Preview) return; // Old preview saves contain no people/economy state.
        if (state.EconomyVersion != 1 || state.UpkeepBillsIssued < 0 || state.UpkeepDebt < 0 || state.UpkeepDueDay < 0 ||
            (state.UpkeepDebt == 0) != (state.UpkeepDueDay == 0) ||
            (state.UpkeepDebt > 0 && state.UpkeepDueDay != state.NextPayday))
            throw new InvalidOperationException("Invalid company upkeep state.");
        if (state.Inventory == null || state.PendingCharacterIds == null || state.Reputation < 0) throw new InvalidOperationException("Invalid company economy.");
        var owned = new HashSet<string>();
        foreach (var item in state.Inventory)
            if (item == null || CompanyEquipment.Find(item.CompanyDefinitionId) == null || string.IsNullOrEmpty(item.CompanyInstanceId) || !owned.Add(item.CompanyInstanceId))
                throw new InvalidOperationException("Invalid company equipment registry.");
        var equipped = new HashSet<string>();
        foreach (var c in state.Characters)
        {
            if (c.Unit == null || !c.Unit.CompanyLoadout || c.Unit.Items == null || c.Unit.Items.Length != 9 || c.Unit.ItemUses.Length != 9 || c.Unit.ItemCooldowns.Length != 9 || c.BaseWage < 0)
                throw new InvalidOperationException("Invalid company character loadout.");
            bool member = state.Members.Any(m => m.CharacterId == c.Id);
            if (member != (c.Employment == CompanyEmployment.Employed)) throw new InvalidOperationException("Character employment disagrees with membership.");
            for (int slot = 0; slot < 9; slot++)
            {
                var item = c.Unit.Items[slot]; if (item == null) continue;
                if (!CompanyEquipment.CanPlace(item, slot) || string.IsNullOrEmpty(item.CompanyInstanceId) || !equipped.Add(item.CompanyInstanceId)) throw new InvalidOperationException("Duplicated/incompatible equipment.");
                if (member && !state.Inventory.Any(i => ReferenceEquals(i, item))) throw new InvalidOperationException("Equipped item is not in the canonical inventory.");
            }
        }
    }
}

internal sealed class CompanyWages : ICompanyDailySystem
{
    public CompanyDailyStage Stage { get { return CompanyDailyStage.Wages; } }
    public void Apply(CompanyCampaign state)
    {
        if (state.Preview) return;
        bool payday = state.ElapsedDays >= state.NextPayday;
        if (payday && state.UpkeepDebt > 0)
        {
            state.Ended = true;
            state.EndReason = "The company ceased operating due to critical insolvency. Its upkeep debt was not paid before payday.";
            state.EndingKind = CompanyEndingKind.Insolvency;
            state.EndingId = Guid.NewGuid().ToString("N");
            state.PendingDecision = null; state.PendingDecisionKind = null;
            state.DeferredWageDecision = null; state.DeferredDecisionKind = null; state.PendingCharacterIds = new string[0];
            return;
        }
        if (payday)
        {
            int bill = CompanyEconomy.Upkeep(state), paid = Math.Min(state.Gold, bill);
            state.Gold -= paid; state.UpkeepDebt = bill - paid;
            state.UpkeepBillsIssued = checked(state.UpkeepBillsIssued + 1);
            state.UpkeepDueDay = state.UpkeepDebt > 0 ? checked(state.NextPayday + 7) : 0;
        }
        var employed = state.Characters.Where(c => c.Employment == CompanyEmployment.Employed && c.Id != state.LeaderId).ToArray();
        var departed = employed.Where(c => c.Debt > 0 && c.DebtDueDay >= 0 && state.ElapsedDays >= c.DebtDueDay).ToArray();
        foreach (var c in departed) CompanyEconomy.Leave(state, c);
        if (payday)
        {
            employed = employed.Except(departed).ToArray();
            long oldDebt = employed.Sum(c => (long)c.Debt), bill = employed.Sum(c => (long)CompanyEconomy.Wage(state, c));
            state.NextPayday = checked(state.NextPayday + 7);
            if (oldDebt == 0 && state.Gold >= bill) state.Gold -= (int)bill;
            else foreach (var c in employed)
            {
                c.Debt = checked(c.Debt + CompanyEconomy.Wage(state, c));
                if (c.DebtDueDay < 0)
                {
                    int wage = CompanyEconomy.Wage(state, c);
                    c.DebtDueDay = checked(state.ElapsedDays + (wage >= 42 ? 1 : wage >= 30 ? 4 : 10));
                }
            }
        }
        var threats = CompanyEconomy.Threatened(state).Where(c => c.WarningShownDay != c.DebtDueDay).ToArray();
        foreach (var c in threats) c.WarningShownDay = c.DebtDueDay;
        bool wageNotice = threats.Length > 0 || (payday && employed.Any(c => c.Debt > 0));
        string notice = wageNotice ? WageMessage(threats, payday) : null;
        bool upkeepNotice = payday && state.UpkeepDebt > 0;
        if (upkeepNotice) notice = CompanyEconomy.UpkeepWarning(state) + (notice == null ? "" : "\n\n" + notice);
        if (departed.Length > 0)
        {
            state.PendingDecisionKind = "Departures";
            state.PendingCharacterIds = departed.Select(c => c.Id).ToArray();
            state.PendingDecision = DepartureMessage(state);
            state.DeferredWageDecision = notice;
            state.DeferredDecisionKind = notice == null ? null : upkeepNotice ? "Upkeep" : "Payday";
        }
        else if (notice != null)
        {
            state.PendingDecisionKind = upkeepNotice ? "Upkeep" : "Payday"; state.PendingCharacterIds = new string[0];
            state.PendingDecision = notice;
        }
    }
    internal static string DepartureMessage(CompanyCampaign state)
    {
        var names = state.Characters.Where(c => state.PendingCharacterIds.Contains(c.Id)).Select(c => c.Unit.Name).ToArray();
        return (names.Length == 0 ? "Mercenaries" : string.Join(", ", names)) +
            " left the company over unpaid wages. Their debt and equipment are retained; they may be available to rehire in a town.";
    }
    internal static string WageMessage(CompanyCharacter[] threats, bool payday)
    {
        return (payday ? "Payday could not be settled for everyone. Personal debts have increased.\n" : "") +
            (threats.Length > 0 ? string.Join(", ", threats.Select(c => c.Unit.Name).ToArray()) +
                " will leave after the next day if their debts remain unpaid." : "Mercenaries are willing to wait a little longer.");
    }

}
internal sealed class CompanyRecruitRefresh : ICompanyDailySystem
{
    public CompanyDailyStage Stage { get { return CompanyDailyStage.Refresh; } }
    public void Apply(CompanyCampaign state)
    {
        if (state.Preview) return;
        foreach (var town in state.Towns.Where(t => t.RecruitRefreshDay > 0 && state.ElapsedDays >= t.RecruitRefreshDay)) CompanyEconomy.Refresh(state, town);
        foreach (var town in state.Towns.Where(t => t.ContractRefreshDay > 0)) CompanyContracts.EnsureTown(state, town);
    }
}

// Company progression is independent of the retired empire world's cap/XP configuration.
internal static class CompanyProgression
{
    internal static int Threshold(int completedLevels, float modifier)
    {
        if (completedLevels <= 0) return 0;
        double total = 20 * (Math.Pow(1.8, completedLevels) - 1) / .8;
        return (int)Math.Min(int.MaxValue - 1024, Math.Ceiling(total * Math.Max(.1, modifier) - .000001));
    }
    internal static float ScaleCombatExp(float amount, int levelAdvantage)
    {
        double factor = levelAdvantage > 0 ? Math.Pow(.5, levelAdvantage) : Math.Min(4, Math.Pow(1.25, -levelAdvantage));
        return (float)(amount * factor);
    }
    internal static void AwardContract(CompanyCampaign state, CompanyContract contract)
    {
        foreach (var c in state.Characters.Where(c => state.Battle.DeployedIds.Contains(c.Id) &&
            state.Members.Any(m => m.CharacterId == c.Id) && !c.Unit.Digested))
        {
            c.Unit.GiveExp(ScaleCombatExp(20 * contract.Rating, c.Unit.Level - contract.Rating));
            // Even a slower-learning starter earns their first level-up after a completed contract.
            if (c.Unit.Level == 1 && !c.Unit.HasEnoughExpToLevelUp()) c.Unit.SetExp(c.Unit.ExperienceRequiredForNextLevel);
        }
    }
}

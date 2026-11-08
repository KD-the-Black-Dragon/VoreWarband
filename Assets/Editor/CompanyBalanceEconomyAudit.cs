// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-08: Verify calendar difficulty boundaries and record pressure in connected campaign simulations (spec 40F).
// 2026-11-08: Verify late road generation, current Ironman ending order and 40F campaign transitions (specs 40E–40F).
// 2026-11-05: Allow focused economy boundary verification for the integrated injury handoff (spec 38 phase D).
// 2026-11-05: Exercise upkeep boundaries and connected native economic campaign policies (spec 38 phase B).
// See CHANGES.md and License.txt for GPL-3.0 terms. Existing attribution remains applicable.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

public static partial class CompanyBalanceAudit
{
    [Serializable] public sealed class EconomicDay
    {
        public int day, gold, upkeep, upkeepDebt, wages, debt, members, levels, contracts, losses, reputation;
        public int hp, maxHp, injuries, oldInjuries, spentOnGear, spentOnHires, spentOnTreatment, departures, formerDebt;
        public int minOfferRating, maxOfferRating;
        public double power, calendarPressure;
    }
    [Serializable] public sealed class EconomicHistory
    {
        public string policy, folder, endReason, error;
        public int seed, startingGold, startingDay, horizon, battles, roadBattles, losses, gearDestroyed, notices, travelActions;
        public int restDays, spentOnGear, spentOnHires, spentOnTreatment, levelUps, earned, revivals;
        public int wagesPaid, upkeepPaid;
        public List<EconomicFight> fights = new List<EconomicFight>();
        public List<EconomicDay> days = new List<EconomicDay>();
    }
    [Serializable] public sealed class EconomicFight
    {
        public int day, rating, rounds, losses, destroyedGear, gold, injuries;
        public bool road, completed, ended;
        public int calendarAgeDays;
        public double companyPower, enemyPower, calendarPressure;
    }
    [Serializable] public sealed class EconomicReport
    {
        public bool complete;
        public int startingGold, initialUpkeep, weeklyUpkeepIncrease;
        public long lateOfferGenerationMilliseconds;
        public string wageCurve;
        public List<string> checks = new List<string>();
        public List<EconomicHistory> histories = new List<EconomicHistory>();
        public string assumptions;
    }
    static EconomicReport economicReport;
    static void EconomicCheck(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
        economicReport.checks.Add(description);
    }
    static void WriteEconomy()
    { File.WriteAllText(Path.Combine(report.directory, "economy-campaigns.json"), JsonUtility.ToJson(economicReport, true)); }

    static IEnumerator EconomyCampaigns()
    {
        economicReport = new EconomicReport { assumptions =
            "Connected native campaigns. No sponsored capital, free healing, invented victories or discarded losses. " +
            "Actual generated terrain is traversed by minimum-action routes with six actions per day; current road events run. " +
            "Native AI controls combat and settlements retain deaths, destroyed equipment, injuries, XP and revival fees. " +
            "Policies explicitly pay affordable upkeep first, then full individual debts in roster order. Practice duels are declined. " +
            "Recovery thresholds, elective advancement, shopping and selection policies are specified by each policy name. " +
            "Day 90 is recorded inside each 180-day history. Survival at either cutoff is censored, not proof of sustainability." };
        economicReport.startingGold = (int)G(T("CompanyEconomy"), "StartingGold");
        economicReport.initialUpkeep = (int)G(T("CompanyEconomy"), "InitialUpkeep");
        economicReport.weeklyUpkeepIncrease = (int)G(T("CompanyEconomy"), "WeeklyUpkeepIncrease");
        economicReport.wageCurve = "Base + 3n + 2nÃ‚Â² + 2max(0,n-3)Ã‚Â³; n=level-1";
        bool calendarOnly = Environment.GetCommandLineArgs().Contains("-balanceCalendarOnly");
        bool skipLegacyBoundaries = Environment.GetCommandLineArgs().Contains("-balanceSkipLegacyEconomyBoundaries");
        if (!calendarOnly && !skipLegacyBoundaries) CheckEconomyBoundaries();
        CheckCalendarBoundaries();
        WriteEconomy();
        if (calendarOnly || Environment.GetCommandLineArgs().Contains("-balanceBoundariesOnly"))
        {
            economicReport.complete = true;
            WriteEconomy();
            yield break;
        }
        int first = IntArgument("-balanceSeedStart", 4101), count = IntArgument("-balanceSeedCount", 4);
        int horizon = IntArgument("-balanceDays", 180);
        if (count < 1 || count > 20 || horizon < 1 || horizon > 730) throw new ArgumentOutOfRangeException("Economic history limits");
        var policies = new[] { "balanced", "basic-all", "favorite-veteran", "gear-first", "grow", "conservative", "risky", "bank-levels", "leader-only", "slow-field", "tour", "role-ranged", "role-magic", "role-melee" };
        string only = StringArgument("-balancePolicy");
        foreach (int seed in Enumerable.Range(first, count))
            foreach (string policy in policies.Where(name => only == null || only.Split(',').Contains(name)))
            {
                int policyHorizon = new[] { "balanced", "risky", "bank-levels", "leader-only" }.Contains(policy) ? horizon : Math.Min(90, horizon);
                var row = new EconomicHistory { policy = policy, seed = seed, horizon = policyHorizon,
                    folder = Path.Combine(report.directory, seed + "-" + policy) };
                Directory.CreateDirectory(row.folder); economicReport.histories.Add(row); WriteEconomy();
                var steps = EconomicCampaign(row);
                try
                {
                    while (steps.MoveNext()) yield return steps.Current;
                }
                finally { CleanupBattle(); WriteEconomy(); }
                UnityEngine.Debug.Log("Economic history " + seed + " " + policy + " days=" + row.days.Last().day + " end=" + row.endReason);
            }
        economicReport.complete = true; WriteEconomy();
    }

    static object Solo(int seed)
    { return Fresh(seed, new[] { seed % 3 }, 1, 1, false); }
    static void CheckCalendarBoundaries()
    {
        Type contracts = T("CompanyContracts");
        Func<int, double> pressureAt = day => (double)C(contracts, "CalendarPressureForAge", day);
        double day20 = pressureAt(20), day21 = pressureAt(21), day28 = pressureAt(28),
            day35 = pressureAt(35), day42 = pressureAt(42), day100 = pressureAt(100), day1000 = pressureAt(1000), day1007 = pressureAt(1007);
        EconomicCheck(day20 == 0 && day21 == 0, "Calendar pressure preserves the first 21 enrolled days");
        EconomicCheck(day28 > 0 && day28 < 1 && day35 > day28, "Pressure begins gently during days 21–28 and continues rising");
        EconomicCheck(day35 - day28 > day28 && day42 - day35 > day35 - day28,
            "Early weekly difficulty increases accelerate rather than repeat a fixed increment");
        EconomicCheck(Math.Abs(day100 - 4.886) < .001 && day1000 > 190 && day1007 > day1000,
            "Provisional curve reaches moderate pressure near day 100 and extreme, increasing pressure near day 1000");
        double encounter28 = (double)C(contracts, "RoadEncounterMultiplier", day28);
        double encounter100 = (double)C(contracts, "RoadEncounterMultiplier", day100);
        double encounter1000 = (double)C(contracts, "RoadEncounterMultiplier", day1000);
        EconomicCheck(encounter28 > 1 && encounter100 > encounter28 && encounter1000 > encounter100 && encounter1000 <= 4,
            "Road encounters receive a separate gradual calendar component with a bounded encounter-only multiplier");
        var lateRoad = Fresh(40603, new[] { 0, 1, 2 }, 1, 1, false);
        S(lateRoad, "ElapsedDays", 1000);
        int oldTile = (int)G(lateRoad, "Position");
        int travelTile = ((IEnumerable)C(T("CompanyWorldGeneration"), "Neighbors", lateRoad, oldTile)).Cast<int>()
            .First(tile => !L(lateRoad, "Towns").Cast<object>().Any(t => (int)G(t, "Tile") == tile));
        EconomicCheck((bool)C(lateRoad, "Move", travelTile), "Synthetic late campaign can take the triggering road move");
        object lateRoadEvent = null;
        for (uint seed = 1; seed < 10000 && lateRoadEvent == null; seed++)
        {
            S(lateRoad, "RandomState", seed); S(lateRoad, "NextRoadEventDay", 0); S(lateRoad, "RoadEvent", null);
            C(T("CompanyRoadEvents"), "AfterTravel", lateRoad, oldTile);
            object candidate = G(lateRoad, "RoadEvent");
            if (candidate != null && G(candidate, "Kind").ToString() == "SpitterSlugs") lateRoadEvent = candidate;
        }
        EconomicCheck(lateRoadEvent != null && (int)G(lateRoadEvent, "CalendarAgeDays") == 1000 &&
            Math.Abs((double)G(lateRoadEvent, "CalendarPressure") - day1000) < .000001 &&
            Math.Abs((double)G(lateRoadEvent, "TargetPower") - (double)C(T("CompanyRoadEvents"), "EncounterPower", lateRoad) * encounter1000) < .000001 &&
            L(lateRoadEvent, "EnemyIds").Count > 0 && L(lateRoadEvent, "EnemyIds").Count <= 128,
            "A real day-1000 road move generates a recorded slug encounter against the pressure-scaled company budget");
        int offerFloor = (int)C(contracts, "MinimumOfferRating", 500.0, 20);
        EconomicCheck(offerFloor == 6 && (double)C(contracts, "Budget", offerFloor) >= 200 &&
            (double)C(contracts, "Budget", offerFloor - 1) < 200, "Company-relative offer floor stays at or above the provisional 40% capability band");

        var state = Fresh(40601, new[] { 0, 1, 2 }, 1, 1, false);
        EconomicCheck((int)G(state, "CompanyRulesVersion") == 5 && (int)G(state, "CalendarDifficultyStartDay") == 0,
            "New campaigns enroll on their first day using the current difficulty rules");
        var town = L(state, "Towns").Cast<object>().Single(t => (int)G(t, "Tile") == (int)G(state, "Position"));
        var initial = L(state, "Contracts").Cast<object>().Where(c => (string)G(c, "TownId") == Id(town)).ToArray();
        EconomicCheck(initial.Length == 4 && initial.All(c => (int)G(c, "Rating") == 1 && (int)G(c, "DifficultyRevision") == 1 &&
            (int)G(c, "CalendarAgeDays") == 0 && (double)G(c, "CalendarPressure") == 0),
            "Opening town offers retain rating-one power and carry zero-pressure provenance");
        var accepted = initial[0]; S(accepted, "Status", Enum.Parse(G(accepted, "Status").GetType(), "Active"));
        int acceptedRating = (int)G(accepted, "Rating"), acceptedReward = (int)G(accepted, "Reward");
        double acceptedPower = (double)G(accepted, "Power"); var acceptedEnemies = L(accepted, "EnemyIds").Cast<string>().ToArray();
        S(state, "ElapsedDays", 28); S(town, "ContractRefreshDay", 0); C(contracts, "EnsureTown", state, town);
        var refreshed = L(state, "Contracts").Cast<object>().Where(c => (string)G(c, "TownId") == Id(town) &&
            G(c, "Status").ToString() == "Offered").ToArray();
        EconomicCheck(refreshed.Length > 0 && refreshed.All(c => (int)G(c, "CalendarAgeDays") == 28 &&
            Math.Abs((double)G(c, "CalendarPressure") - day28) < .000001 && (int)G(c, "DifficultyRevision") == 1),
            "Refreshed offers record the exact calendar age, pressure and rules revision");
        EconomicCheck((int)G(accepted, "Rating") == acceptedRating && (int)G(accepted, "Reward") == acceptedReward &&
            (double)G(accepted, "Power") == acceptedPower && L(accepted, "EnemyIds").Cast<string>().SequenceEqual(acceptedEnemies),
            "A refresh does not rewrite an already accepted contract, its quote or enemy roster");
        var roundTrip = Clone(state); C(T("CompanyCampaignSave"), "RequireHealthPreserved", state, roundTrip);
        EconomicCheck((int)G(roundTrip, "CalendarDifficultyStartDay") == 0 &&
            L(roundTrip, "Contracts").Cast<object>().Where(c => (int)G(c, "DifficultyRevision") == 1).All(c =>
                (int)G(c, "CalendarAgeDays") == 0 || (int)G(c, "CalendarAgeDays") == 28),
            "Difficulty epoch and generated-offer provenance survive save/resume");

        var generationTimer = Stopwatch.StartNew();
        S(state, "ElapsedDays", 1000); S(town, "ContractRefreshDay", 0); C(contracts, "EnsureTown", state, town);
        generationTimer.Stop(); economicReport.lateOfferGenerationMilliseconds = generationTimer.ElapsedMilliseconds;
        var lateOffers = L(state, "Contracts").Cast<object>().Where(c => (string)G(c, "TownId") == Id(town) &&
            G(c, "Status").ToString() == "Offered").ToArray();
        EconomicCheck(lateOffers.Length > 0 && lateOffers.All(c => (int)G(c, "CalendarAgeDays") == 1000 &&
            Math.Abs((double)G(c, "CalendarPressure") - day1000) < .000001) && lateOffers.Max(c => (int)G(c, "Rating")) > 100,
            "Synthetic day-1000 offer refresh produces valid high-pressure contracts and records bounded provenance");
        WriteEconomy();

        var legacy = Clone(state); S(legacy, "CompanyRulesVersion", 4); S(legacy, "ElapsedDays", 100);
        S(legacy, "CalendarDifficultyStartDay", 0);
        foreach (var contract in L(legacy, "Contracts"))
        { S(contract, "DifficultyRevision", 0); S(contract, "CalendarAgeDays", 0); S(contract, "CalendarPressure", 0.0); }
        int legacyGold = (int)G(legacy, "Gold"); var preservedActive = L(legacy, "Contracts").Cast<object>().Single(c => G(c, "Status").ToString() == "Active");
        int legacyRating = (int)G(preservedActive, "Rating"), legacyReward = (int)G(preservedActive, "Reward");
        var legacyEnemies = L(preservedActive, "EnemyIds").Cast<string>().ToArray();
        C(T("CompanyEconomy"), "Bind", legacy); C(contracts, "Validate", legacy);
        EconomicCheck((int)G(legacy, "CompanyRulesVersion") == 5 && (int)G(legacy, "CalendarDifficultyStartDay") == 100 &&
            (double)C(contracts, "CalendarPressure", legacy) == 0, "A pre-40F campaign receives a fresh protected enrollment without a late-game jump");
        EconomicCheck((int)G(legacy, "Gold") == legacyGold && (int)G(preservedActive, "Rating") == legacyRating &&
            (int)G(preservedActive, "Reward") == legacyReward && L(preservedActive, "EnemyIds").Cast<string>().SequenceEqual(legacyEnemies) &&
            (int)G(preservedActive, "DifficultyRevision") == 0, "Legacy campaign funds, active contracts and untagged generated forces remain unchanged");

        var road = Fresh(40602, new[] { 0, 1, 2 }, 1, 1, false);
        var enemy = C(contracts, "Enemy", road, Race.SpitterSlugs, 1);
        S(enemy, "Employment", Enum.Parse(G(enemy, "Employment").GetType(), "Unavailable")); U(enemy).Side = 1; L(road, "Characters").Add(enemy);
        Type roadType = T("CompanyRoadEvent"); var occurrence = Activator.CreateInstance(roadType, true);
        S(occurrence, "Id", Guid.NewGuid().ToString("N")); S(occurrence, "Kind", Enum.Parse(T("CompanyRoadEventKind"), "SpitterSlugs"));
        S(occurrence, "Phase", Enum.Parse(T("CompanyRoadEventPhase"), "Closed")); S(occurrence, "Tile", (int)G(road, "Position"));
        S(occurrence, "Rating", 1); S(occurrence, "DifficultyRevision", 1); S(occurrence, "CalendarAgeDays", 1000);
        S(occurrence, "CalendarPressure", day1000); S(occurrence, "TargetPower", 250);
        S(occurrence, "EnemyIds", new List<string> { Id(enemy) }); S(road, "RoadEvent", occurrence);
        var roadCopy = Clone(road);
        EconomicCheck((bool)C(T("CompanyRoadEvents"), "Same", occurrence, G(roadCopy, "RoadEvent")) &&
            (int)G(G(roadCopy, "RoadEvent"), "CalendarAgeDays") == 1000 &&
            Math.Abs((double)G(G(roadCopy, "RoadEvent"), "CalendarPressure") - day1000) < .000001,
            "A generated road encounter retains its pressure and target budget after save/resume");
    }
    static void SettleDay(object state, bool resting, EconomicHistory row = null)
    {
        PayEconomicDebts(state, row);
        int cash = (int)G(state, "Gold"), count = (int)G(state, "UpkeepBillsIssued");
        int upkeep = (int)C(T("CompanyEconomy"), "Upkeep", state);
        C(G(view, "clock"), "EndDay", state, resting);
        if (row != null)
        {
            int automaticUpkeep = (int)G(state, "UpkeepBillsIssued") > count ? Math.Min(cash, upkeep) : 0;
            row.upkeepPaid += automaticUpkeep;
            row.wagesPaid += cash - (int)G(state, "Gold") - automaticUpkeep;
        }
        for (int guard = 0; (bool)G(state, "UpdatingDay") && guard < 12; guard++)
        {
            if (!string.IsNullOrEmpty((string)G(state, "PendingDecision")))
            {
                if (row != null) row.notices++;
                PayEconomicDebts(state, row);
                S(state, "PendingDecision", null); S(state, "PendingDecisionKind", null);
                S(state, "DeferredWageDecision", null); S(state, "DeferredDecisionKind", null); S(state, "PendingCharacterIds", new string[0]);
            }
            C(G(view, "clock"), "Resume", state);
        }
        if ((bool)G(state, "UpdatingDay")) throw new InvalidOperationException("Economic day did not settle");
    }
    static void PayEconomicDebts(object state, EconomicHistory row = null)
    {
        if ((bool)G(state, "Ended")) return;
        int upkeepDebt = (int)G(state, "UpkeepDebt");
        if (upkeepDebt > 0 && (int)G(state, "Gold") >= upkeepDebt)
        {
            C(T("CompanyEconomy"), "PayUpkeep", state);
            if (row != null) row.upkeepPaid += upkeepDebt;
        }
        int before = (int)G(state, "Gold"); PayAffordable(state);
        if (row != null) row.wagesPaid += before - (int)G(state, "Gold");
    }
    static void CheckEconomyBoundaries()
    {
        var state = Solo(38021);
        int initialUpkeep = (int)C(T("CompanyEconomy"), "Upkeep", state);
        S(state, "Gold", initialUpkeep - 8); S(state, "ElapsedDays", 6);
        C(G(view, "clock"), "EndDay", state, true);
        EconomicCheck((int)G(state, "Gold") == 0 && (int)G(state, "UpkeepDebt") == 8 &&
            (int)G(state, "UpkeepDueDay") == 14 && (int)G(state, "NextPayday") == 14, "Partial upkeep allocation, separate debt and exact grace deadline");
        EconomicCheck((string)G(state, "PendingDecisionKind") == "Upkeep" && (bool)G(state, "UpdatingDay"), "Upkeep notice interrupts rest before later daily stages");
        var restored = Clone(state);
        C(T("CompanyCampaignSave"), "RequireHealthPreserved", state, restored);
        EconomicCheck((int)G(restored, "UpkeepDebt") == 8 && (int)G(restored, "DailyStage") == 1, "Interrupted upkeep state survives serialization without rebilling");
        S(restored, "Gold", 8); C(T("CompanyEconomy"), "PayUpkeep", restored);
        S(restored, "PendingDecision", null); C(G(view, "clock"), "Resume", restored);
        EconomicCheck(!(bool)G(restored, "UpdatingDay") && (int)G(restored, "UpkeepBillsIssued") == 1 &&
            (int)G(restored, "UpkeepDebt") == 0, "Repayment and resumed stage do not repeat payday");
        S(restored, "ElapsedDays", 13); S(restored, "Gold", 100); SettleDay(restored, false);
        EconomicCheck(!(bool)G(restored, "Ended") && (int)G(restored, "Gold") == 100 - (initialUpkeep + economicReport.weeklyUpkeepIncrease), "Next bill rises by the flat increment after grace repayment");
        S(state, "PendingDecision", null); C(G(view, "clock"), "Resume", state);
        S(state, "ElapsedDays", 13); S(state, "Gold", 1000);
        C(G(view, "clock"), "EndDay", state, true);
        EconomicCheck((bool)G(state, "Ended") && (int)G(state, "Gold") == 1000 &&
            (int)G(state, "UpkeepBillsIssued") == 1 && !(bool)G(state, "UpdatingDay"), "Outstanding debt ends company before new bills even with unspent treasury cash");
        EconomicCheck((string)G(state, "EndReason") != null && (int)G(state, "RestDaysRemaining") == 0, "Critical insolvency stops rest and records the ending");
        int endedDay = (int)G(state, "ElapsedDays"); SettleDay(state, false);
        EconomicCheck((int)G(state, "ElapsedDays") == endedDay, "Ended companies cannot advance calendar");
        state = Fresh(38022, new[] { 0, 1, 2 }, 1, 1, false);
        var people = Members(state); S(state, "Gold", initialUpkeep + 5); S(state, "ElapsedDays", 6);
        SettleDay(state, false);
        int wages = people.Sum(c => (int)C(T("CompanyEconomy"), "Wage", state, c));
        EconomicCheck((int)G(state, "Gold") == 5 && people.Sum(c => (int)G(c, "Debt")) == wages &&
            (int)G(state, "UpkeepDebt") == 0, "Upkeep takes priority and underfunded payroll records everyone's full personal debt");
        S(state, "Gold", 10000); PayEconomicDebts(state);
        var employee = people[1]; int level = U(employee).Level;
        int oldWage = (int)C(T("CompanyEconomy"), "Wage", state, employee), cash = (int)G(state, "Gold");
        float xp = U(employee).Experience; int rng = (int)(uint)G(state, "RandomState");
        int quoted = (int)C(T("CompanyEconomy"), "WageAtLevel", state, employee, level + 1);
        EconomicCheck(U(employee).Experience == xp && U(employee).Level == level && (int)(uint)G(state, "RandomState") == rng,
            "Wage preview preserves XP, level and campaign RNG");
        U(employee).GiveExp(1000); var choices = (Stat[])C(T("CompanyEconomy"), "LevelChoices", state, employee);
        C(T("CompanyEconomy"), "Level", state, employee, choices[0]);
        EconomicCheck((int)C(T("CompanyEconomy"), "Wage", state, employee) == quoted && quoted > oldWage &&
            (int)G(state, "Gold") == cash && (int)G(employee, "Debt") == 0, "Accepted level matches preview with no midweek charge or debt change");
        S(state, "ElapsedDays", 13); cash = (int)G(state, "Gold");
        int nextWages = Members(state).Sum(c => (int)C(T("CompanyEconomy"), "Wage", state, c));
        SettleDay(state, false);
        EconomicCheck(cash - (int)G(state, "Gold") == initialUpkeep + economicReport.weeklyUpkeepIncrease + nextWages, "Higher wage takes effect at next regular payday");
        var legacy = Clone(state); S(legacy, "EconomyVersion", 0); S(legacy, "ElapsedDays", 100); S(legacy, "NextPayday", 105);
        S(legacy, "UpkeepDebt", 0); S(legacy, "UpkeepDueDay", 0); S(Members(legacy)[1], "Debt", 77);
        int oldGold = (int)G(legacy, "Gold"); var reward = G(L(legacy, "Contracts")[0], "Reward");
        C(T("CompanyEconomy"), "Bind", legacy);
        EconomicCheck((int)G(legacy, "UpkeepBillsIssued") == 0 && (int)G(legacy, "UpkeepDebt") == 0 &&
            (int)G(legacy, "Gold") == oldGold && (int)G(Members(legacy)[1], "Debt") == 77 && (int)G(legacy, "NextPayday") == 105 &&
            reward.Equals(G(L(legacy, "Contracts")[0], "Reward")), "Old campaigns enroll at next scheduled payday without retroactive charges or reward changes");
        var idle = Solo(38023); int guardDay = 0;
        while (!(bool)G(idle, "Ended") && guardDay++ < 200) SettleDay(idle, false);
        EconomicCheck((bool)G(idle, "Ended") && (int)G(idle, "ElapsedDays") < 200, "Idle leader-only company reaches critical insolvency with finite capital");
        // 40E publishes an immutable ending record before game-owned Ironman save removal; probes remain preserved.
        var iron = Solo(38024); S(iron, "Ironman", true);
        C(T("CompanySaveSlots"), "Auto", iron, Path.Combine(report.directory, (string)G(iron, "Id")));
        S(iron, "UpkeepDebt", 1); S(iron, "UpkeepDueDay", 7); S(iron, "ElapsedDays", 6);
        C(G(view, "clock"), "EndDay", iron, false);
        string ironPath = Path.Combine(report.directory, (string)G(iron, "Id"));
        int filesBefore = Directory.GetFiles(ironPath, "*.company").Length;
        var endingRecord = C(T("CompanyHistoryRecord"), "From", iron);
        string historyPath = Path.Combine(report.directory, "HighScores");
        C(T("CompanyHistoryStore"), "Commit", endingRecord, historyPath);
        L(T("CompanySaveSlots"), "DiagnosticRemovals").Clear();
        C(T("CompanySaveSlots"), "RemoveCompany", (string)G(iron, "Id"), report.directory);
        var selectedForRemoval = L(T("CompanySaveSlots"), "DiagnosticRemovals").Cast<string>().ToArray();
        EconomicCheck(File.Exists(Path.Combine(historyPath, (string)G(iron, "EndingId") + ".score")) &&
            File.Exists(Path.Combine(ironPath, "fallen")) && Directory.GetFiles(ironPath, "*.company").Length == filesBefore &&
            selectedForRemoval.Length == filesBefore && selectedForRemoval.All(File.Exists),
            "Ironman ending is archived before save removal, and synthetic save files remain physically preserved");
        var preview = C(T("CompanyCampaign"), "Generate", 38030, false);
        C(T("CompanyCampaignSave"), "RequireHealthPreserved", preview, Clone(preview));
        EconomicCheck((int)G(preview, "EconomyVersion") == 0, "Legacy map-only preview retains serialization compatibility");
        var warning = Fresh(38031, new[] { 0, 1, 2 }, 1, 1, false);
        var leaving = Members(warning)[1]; S(leaving, "Debt", 1); S(leaving, "DebtDueDay", 7);
        S(warning, "Gold", 0); S(warning, "ElapsedDays", 6);
        C(G(view, "clock"), "EndDay", warning, true);
        EconomicCheck((string)G(warning, "PendingDecisionKind") == "Departures" &&
            (string)G(warning, "DeferredDecisionKind") == "Upkeep", "Departures queue a distinct upkeep warning");
        var queued = Clone(warning); C(T("CompanyCampaignSave"), "RequireHealthPreserved", warning, queued);
        C(view, "Install", queued); C(view, "ResolveDecision");
        EconomicCheck((string)G(queued, "PendingDecisionKind") == "Upkeep", "Queued notice resumes with correct upkeep identity");
        S(queued, "Gold", (int)G(queued, "UpkeepDebt") - 1);
        int unpaid = (int)G(queued, "UpkeepDebt"), partialGold = (int)G(queued, "Gold");
        bool rejected = false;
        try { C(T("CompanyEconomy"), "PayUpkeep", queued); } catch { rejected = true; }
        EconomicCheck(rejected && (int)G(queued, "UpkeepDebt") == unpaid && (int)G(queued, "Gold") == partialGold,
            "Unaffordable manual upkeep repayment is rejected without partial mutations");
        S(queued, "Gold", unpaid); C(T("CompanyEconomy"), "PayUpkeep", queued);
        C(G(view, "management"), "Decision");
        var page = (GameObject)G(G(view, "management"), "Page");
        var words = page.GetComponentsInChildren<UnityEngine.UI.Text>(true)
            .Where(t => t.gameObject.activeSelf && t.transform.parent.gameObject.activeSelf).Select(t => t.text).ToArray();
        EconomicCheck(words.Contains("Upkeep settled") && !words.Contains("Critical danger of insolvency"),
            "Paid upkeep notice updates its title and message after repayment from the roster");
        C(view, "ResolveDecision");
        EconomicCheck(!(bool)G(queued, "UpdatingDay") && (int)G(queued, "UpkeepBillsIssued") == 1,
            "Departure, warning and payment resume one committed payday exactly once");
        var trade = Solo(38032);
        var home = L(trade, "Towns").Cast<object>().Single(t => (int)G(t, "Tile") == (int)G(trade, "Position"));
        var hire = C(T("CompanyEconomy"), "Create", trade, Race.Avians, 1, false, 1);
        S(hire, "TownId", Id(home)); L(trade, "Characters").Add(hire);
        int originalCash = (int)G(trade, "Gold"); C(T("CompanyEconomy"), "Hire", trade, hire, home);
        foreach (var item in U(hire).Items.Where(i => i != null).ToArray())
        {
            int slot = Array.IndexOf(U(hire).Items, item);
            C(T("CompanyEquipment"), "Equip", trade, hire, null, slot);
            C(T("CompanyEquipment"), "Sell", trade, item);
        }
        EconomicCheck((int)G(trade, "Gold") < originalCash, "Hire, strip and sell cannot generate gold");
        C(T("CompanyEconomy"), "Leave", trade, hire);
        int beforeRehire = (int)G(trade, "Gold"); S(hire, "TownId", Id(home));
        C(T("CompanyEconomy"), "Hire", trade, hire, home);
        EconomicCheck((int)G(trade, "Gold") < beforeRehire && (int)G(hire, "Debt") == 0,
            "Dismissal and rehire charge a new advance without refunding the previous one");
        var staff = Members(trade)[1]; int previousWage = (int)C(T("CompanyEconomy"), "WageAtLevel", trade, staff, 1), previousRise = 0;
        for (int rank = 2; rank <= 20; rank++)
        {
            int nextWage = (int)C(T("CompanyEconomy"), "WageAtLevel", trade, staff, rank);
            EconomicCheck(nextWage - previousWage > previousRise, "Wage increases accelerate at level " + rank);
            previousRise = nextWage - previousWage; previousWage = nextWage;
        }
        EconomicCheck(economicReport.startingGold >= 244 && economicReport.startingGold < 264,
            "Opening capital covers any two current starters and excludes three even with only the cheapest species enabled");
        foreach (int role in new[] { 0, 1, 2 }) foreach (int remaining in Enumerable.Range(1, 7))
        {
            var quoteState = Solo(38025 + role); var person = C(T("CompanyEconomy"), "Create", quoteState,
                new[] { Race.Gnolls, Race.Avians, Race.Lizards }[role], role, false, 1);
            S(quoteState, "ElapsedDays", 7 - remaining);
            int hireQuote = (int)C(T("CompanyEconomy"), "HirePrice", quoteState, person);
            int salary = (int)C(T("CompanyEconomy"), "Wage", quoteState, person);
            EconomicCheck(hireQuote == (int)C(T("CompanyEquipment"), "GearPrice", U(person)) + (int)Math.Ceiling(salary * remaining / 7.0),
                "Replacement quote preserves gear and advance for role " + role + " remaining days " + remaining);
        }
    }

    static EconomicDay EconomicSnapshot(object state, EconomicHistory row)
    {
        var members = Members(state);
        var currentTown = L(state, "Towns").Cast<object>().FirstOrDefault(t => (int)G(t, "Tile") == (int)G(state, "Position"));
        var localOffers = currentTown == null ? new object[0] : L(state, "Contracts").Cast<object>().Where(c =>
            (string)G(c, "TownId") == Id(currentTown) && G(c, "Status").ToString() == "Offered").ToArray();
        return new EconomicDay { day = (int)G(state, "ElapsedDays"), gold = (int)G(state, "Gold"),
            upkeep = (int)C(T("CompanyEconomy"), "Upkeep", state), upkeepDebt = (int)G(state, "UpkeepDebt"),
            wages = members.Sum(c => (int)C(T("CompanyEconomy"), "Wage", state, c)), debt = members.Sum(c => (int)G(c, "Debt")),
            members = members.Length, levels = members.Sum(c => U(c).Level), contracts = (int)G(state, "ContractsCompleted"),
            losses = L(state, "Memorial").Count, reputation = (int)G(state, "Reputation"), hp = members.Sum(c => U(c).Health),
            maxHp = members.Sum(c => U(c).MaxHealth), injuries = members.Sum(c => L(U(c), "Injuries").Count),
            oldInjuries = OldCount(state), power = (double)C(T("CompanyPower"), "Force", (object)members.Select(U).ToArray()),
            calendarPressure = (double)C(T("CompanyContracts"), "CalendarPressure", state),
            minOfferRating = localOffers.Length == 0 ? 0 : localOffers.Min(c => (int)G(c, "Rating")),
            maxOfferRating = localOffers.Length == 0 ? 0 : localOffers.Max(c => (int)G(c, "Rating")),
            spentOnGear = row.spentOnGear, spentOnHires = row.spentOnHires, spentOnTreatment = row.spentOnTreatment,
            departures = L(state, "Characters").Cast<object>().Count(c => G(c, "Employment").ToString() == "Former"),
            formerDebt = L(state, "Characters").Cast<object>().Where(c => G(c, "Employment").ToString() == "Former").Sum(c => (int)G(c, "Debt")) };
    }
    static IEnumerator EconomicCampaign(EconomicHistory row)
    {
        string continuation = StringArgument("-balanceContinue");
        object state;
        if (continuation == null) state = Solo(row.seed);
        else
        {
            string path = Path.GetFullPath(continuation), logs = Path.GetFullPath("Logs") + Path.DirectorySeparatorChar;
            if (!path.StartsWith(logs, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Continuation inputs must be retained synthetic data under project Logs.");
            state = C(T("CompanyCampaignSave"), "Decode", File.ReadAllBytes(path));
            economicReport.assumptions += " Continuation loads the complete retained campaign at day " + G(state, "ElapsedDays") +
                "; its existing cash is starting capital, with no fabricated income or restored losses.";
        }
        row.startingGold = (int)G(state, "Gold"); row.startingDay = (int)G(state, "ElapsedDays");
        if (row.startingDay > 0) row.days.Add(EconomicSnapshot(state, row));
        int targetSize = row.policy == "leader-only" ? 1 : row.policy == "grow" ? 5 : 3;
        int travelTown = -1;
        object objective = null;
        for (int iteration = 0; !(bool)G(state, "Ended") && (int)G(state, "ElapsedDays") < row.horizon; iteration++)
        {
            if (iteration > row.horizon * 60) throw new InvalidOperationException("Campaign policy did not make calendar progress");
            PayEconomicDebts(state, row);
            EconomicLevels(state, row);
            int position = (int)G(state, "Position");
            var town = L(state, "Towns").Cast<object>().FirstOrDefault(t => (int)G(t, "Tile") == position);
            if (town != null && !(bool)G(state, "Busy")) EconomicShopping(state, town, targetSize, row);
            if (travelTown == position) travelTown = -1;
            if (G(state, "Battle") != null)
            {
                string phase = G(G(state, "Battle"), "Phase").ToString();
                if (phase == "Deployment")
                {
                    var run = EconomicBattle(state, row);
                    while (run.MoveNext()) yield return run.Current;
                    state = G(view, "state"); objective = null;
                    continue;
                }
                if (phase == "Summary") { C(T("CompanyBattleSession"), "Acknowledge", state); continue; }
                if (phase == "Revival")
                {
                    int beforeRevival = (int)G(state, "Gold");
                    C(T("CompanyBattleSession"), "Revive", state);
                    row.revivals += beforeRevival - (int)G(state, "Gold"); objective = null; continue;
                }
                if (phase == "PostBattleEvent") { C(T("CompanyBattleSession"), "AcknowledgePostBattle", state); continue; }
            }
            if ((bool)C(T("CompanyRoadEvents"), "AwaitingChoice", state))
            { C(T("CompanyRoadEvents"), "ResolveDuel", state, false); continue; }
            var members = Members(state);
            double health = members.Sum(c => (double)U(c).Health) / members.Sum(c => U(c).MaxHealth);
            double threshold = row.policy == "risky" ? .45 : row.policy == "conservative" ? .95 : .8;
            bool seekTown = health < threshold && row.policy == "conservative" && town == null;
            if ((int)G(state, "Actions") == 0 || (health < threshold && !seekTown))
            {
                bool rest = health < threshold && (int)G(state, "Actions") == (int)G(state, "DailyActions");
                if (rest) row.restDays++;
                SettleDay(state, rest, row); row.days.Add(EconomicSnapshot(state, row)); WriteEconomy();
                if ((int)G(state, "ElapsedDays") % 30 == 0)
                    File.WriteAllBytes(Path.Combine(row.folder, "day-" + G(state, "ElapsedDays") + ".bin"), Encode(state));
                yield return null; continue;
            }
            if (objective == null || G(objective, "Status").ToString() != "Active")
            {
                objective = L(state, "Contracts").Cast<object>().FirstOrDefault(c => G(c, "Status").ToString() == "Active");
                if (objective == null && town != null)
                {
                    var offers = L(state, "Contracts").Cast<object>().Where(c => (string)G(c, "TownId") == Id(town) && G(c, "Status").ToString() == "Offered");
                    double power = (double)C(T("CompanyPower"), "Force", (object)members.Select(U).ToArray());
                    double safety = row.policy == "risky" ? 1.15 : row.policy == "conservative" ? .75 : 1;
                    var eligible = offers.Where(c => (double)G(c, "Power") <= power * safety &&
                        (row.policy != "bank-levels" || (int)G(c, "Rating") == 1));
                    objective = (row.policy == "bank-levels" || row.policy == "leader-only" ? eligible.OrderBy(c => (int)G(c, "Rating")) :
                        eligible.OrderByDescending(c => (int)G(c, "Rating"))).FirstOrDefault();
                    if (objective != null)
                    {
                        C(T("CompanyContracts"), "Accept", state, objective);
                        // Efficient policies can batch a second nearby objective into the outing.
                        if (row.policy == "balanced" || row.policy == "gear-first")
                        {
                            var second = eligible.FirstOrDefault(c => !ReferenceEquals(c, objective));
                            if (second != null) C(T("CompanyContracts"), "Accept", state, second);
                        }
                    }
                }
                if (objective == null && town != null && row.policy == "tour")
                    travelTown = L(state, "Towns").Cast<object>().Where(t => Id(t) != Id(town))
                        .OrderBy(t => Math.Abs((int)G(t, "Tile") % (int)G(state, "Width") - position % (int)G(state, "Width")) +
                            Math.Abs((int)G(t, "Tile") / (int)G(state, "Width") - position / (int)G(state, "Width")))
                        .Select(t => (int)G(t, "Tile")).First();
                if (objective == null && town != null && travelTown < 0)
                {
                    // Exhausted/safe-offer policy waits for local refresh instead of inventing contracts.
                    SettleDay(state, (int)G(state, "Actions") == (int)G(state, "DailyActions"), row); row.restDays++; row.days.Add(EconomicSnapshot(state, row)); WriteEconomy();
                    yield return null; continue;
                }
            }
            int destination = seekTown ? NearestEconomicTown(state) : travelTown >= 0 ? travelTown :
                objective == null ? NearestEconomicTown(state) : (int)G(objective, "Tile");
            // Return to the nearest town when no active objective remains; an existing one is completed first.
            // An outing can finish on another live target after a road battle. Use actual
            // movement off the tile and back so the policy pays the normal entry costs.
            bool steppingAway = objective != null && destination == position;
            if (steppingAway)
            {
                File.AppendAllText(Path.Combine(row.folder, "policy-events.txt"), "Day " + G(state, "ElapsedDays") +
                    " active target at current tile " + position + " contract=" + Id(objective) +
                    " status=" + G(objective, "Status") + " previous=" + G(G(state, "Battle"), "Summary") + "\n");
                destination = position % (int)G(state, "Width") > 0 ? position - 1 : position + 1;
            }
            int next = EconomicStep(state, destination);
            if (next == position) throw new InvalidOperationException("Economic destination equals current position without a live objective");
            if ((int)C(state, "MoveCost", next) > (int)G(state, "Actions"))
            { SettleDay(state, false, row); row.days.Add(EconomicSnapshot(state, row)); yield return null; continue; }
            int cost = (int)C(state, "MoveCost", next);
            if (!(bool)C(state, "Move", next)) throw new InvalidOperationException("Economic travel rejected: position=" + position +
                " next=" + next + " destination=" + destination + " actions=" + G(state, "Actions") + "/" + G(state, "DailyActions") +
                " cost=" + cost + " busy=" + G(state, "Busy") + " inBattle=" + G(state, "InBattle") +
                " updatingDay=" + G(state, "UpdatingDay") + " decision=" + G(state, "PendingDecision") +
                " battlePhase=" + (G(state, "Battle") == null ? "none" : G(G(state, "Battle"), "Phase").ToString()) +
                " roadEvent=" + (G(state, "RoadEvent") == null ? "none" : G(G(state, "RoadEvent"), "Phase").ToString()));
            row.travelActions += cost;
            var reached = L(state, "Contracts").Cast<object>().FirstOrDefault(c =>
                G(c, "Status").ToString() == "Active" && (int)G(c, "Tile") == next);
            if (reached != null)
            {
                C(T("CompanyBattleSession"), "Prepare", state, reached);
                S(G(state, "Battle"), "ReturnTile", position); S(G(state, "Battle"), "PositionCommitted", true);
            }
            else C(T("CompanyRoadEvents"), "AfterTravel", state, position);
        }
        row.endReason = (bool)G(state, "Ended") ? (string)G(state, "EndReason") : "horizon-censored";
        EconomicCheck((long)row.startingGold + row.earned - row.spentOnGear - row.spentOnHires - row.spentOnTreatment -
            row.revivals - row.upkeepPaid - row.wagesPaid == (int)G(state, "Gold"), "Cash ledger reconciles " + row.seed + " " + row.policy);
        File.WriteAllBytes(Path.Combine(row.folder, "final-company.bin"), Encode(state));
        if (row.days.Count == 0 || row.days.Last().day != (int)G(state, "ElapsedDays")) row.days.Add(EconomicSnapshot(state, row));
    }
    static void EconomicLevels(object state, EconomicHistory row)
    {
        if (row.policy == "bank-levels" || (bool)G(state, "Busy")) return;
        var members = Members(state);
        foreach (var person in members)
        {
            if (row.policy == "favorite-veteran" && Id(person) != (string)G(state, "LeaderId") && person != members.Skip(1).FirstOrDefault()) continue;
            if (!U(person).HasEnoughExpToLevelUp()) continue;
            var choices = (Stat[])C(T("CompanyEconomy"), "LevelChoices", state, person);
            if (choices.Length == 0) continue;
            C(T("CompanyEconomy"), "Level", state, person, choices[0]); row.levelUps++;
        }
    }
    static void EconomicShopping(object state, object town, int targetSize, EconomicHistory row)
    {
        C(T("CompanyEconomy"), "Visit", state, town);
        while (Members(state).Length < targetSize)
        {
            var candidate = ((IEnumerable)C(T("CompanyEconomy"), "Offers", state, town)).Cast<object>()
                .Where(c => (int)G(c, "Debt") == 0)
                .OrderBy(c => row.policy == "role-ranged" ? ((int)G(c, "Focus") == 1 ? 0 : 1) :
                    row.policy == "role-magic" ? ((int)G(c, "Focus") == 2 ? 0 : 1) :
                    row.policy == "role-melee" ? ((int)G(c, "Focus") == 0 ? 0 : 1) :
                    Members(state).Any(m => (int)G(m, "Focus") == (int)G(c, "Focus")) ? 1 : 0)
                .ThenBy(c => (int)C(T("CompanyEconomy"), "HirePrice", state, c)).FirstOrDefault();
            if (candidate == null) break;
            int price = (int)C(T("CompanyEconomy"), "HirePrice", state, candidate);
            int reserve = Members(state).Length < 3 ? 0 : (int)C(T("CompanyEconomy"), "Upkeep", state) +
                Members(state).Sum(c => (int)C(T("CompanyEconomy"), "Wage", state, c));
            if ((int)G(state, "Gold") - price < reserve) break;
            C(T("CompanyEconomy"), "Hire", state, candidate, town); row.spentOnHires += price;
        }
        if (row.policy == "leader-only" || row.policy == "bank-levels") return;
        var catalog = ((IEnumerable)C(T("CompanyEquipment"), "Available", state)).Cast<object>();
        var targets = row.policy == "favorite-veteran" || row.policy == "gear-first" ? Members(state).Take(2) : Members(state).AsEnumerable();
        foreach (var person in targets)
        {
            if (row.policy == "conservative" && (bool)C(T("CompanyInjuries"), "CanTreat", U(person)) && (int)G(state, "Gold") > 120)
            {
                int before = (int)G(state, "Gold"); C(T("CompanyInjuries"), "Treat", state, Id(person));
                row.spentOnTreatment += before - (int)G(state, "Gold");
            }
            int reserve = (int)C(T("CompanyEconomy"), "Upkeep", state) + Members(state).Sum(c => (int)C(T("CompanyEconomy"), "Wage", state, c));
            if (row.policy == "gear-first" || row.policy == "favorite-veteran") EconomicWeapon(state, person, catalog, reserve, row);
            for (int slot = 0; slot < 4; slot++)
            {
                int oldTier = U(person).Items[slot] == null ? 0 : (int)G(C(T("CompanyEquipment"), "Find", (string)G(U(person).Items[slot], "CompanyDefinitionId")), "Tier");
                var item = catalog.Where(d => (int)G(d, "Slot") == slot && (int)G(d, "Tier") > oldTier &&
                    (row.policy != "basic-all" || (int)G(d, "Tier") == 1)).OrderBy(d => (int)G(d, "Cost")).FirstOrDefault();
                if (item == null || (int)G(state, "Gold") - (int)G(item, "Cost") < reserve) continue;
                EquipBuy(state, person, (string)G(item, "Id"), slot); row.spentOnGear += (int)G(item, "Cost");
            }
        }
    }
    static void EconomicWeapon(object state, object person, IEnumerable<object> catalog, int reserve, EconomicHistory row)
    {
        int slot = (int)G(person, "Focus") == 1 ? 6 : 5;
        var current = U(person).Items[slot];
        int tier = current == null ? 0 : (int)G(C(T("CompanyEquipment"), "Find", (string)G(current, "CompanyDefinitionId")), "Tier");
        var upgrade = catalog.Where(d => (int)G(d, "Slot") == slot && (int)G(d, "Tier") > tier)
            .OrderBy(d => (int)G(d, "Cost")).FirstOrDefault();
        if (upgrade == null || (int)G(state, "Gold") - (int)G(upgrade, "Cost") < reserve) return;
        EquipBuy(state, person, (string)G(upgrade, "Id"), slot); row.spentOnGear += (int)G(upgrade, "Cost");
    }
    static int NearestEconomicTown(object state)
    {
        int width = (int)G(state, "Width"), position = (int)G(state, "Position");
        return L(state, "Towns").Cast<object>().OrderBy(t => Math.Abs((int)G(t, "Tile") % width - position % width) +
            Math.Abs((int)G(t, "Tile") / width - position / width)).Select(t => (int)G(t, "Tile")).First();
    }
    static int EconomicStep(object state, int destination)
    {
        int width = (int)G(state, "Width"), source = (int)G(state, "Position");
        var terrain = (int[])G(state, "Terrain"); var distances = Enumerable.Repeat(int.MaxValue, terrain.Length).ToArray();
        var previous = Enumerable.Repeat(-1, terrain.Length).ToArray();
        var queue = new SortedSet<Tuple<int, int>>(); distances[source] = 0; queue.Add(Tuple.Create(0, source));
        while (queue.Count > 0)
        {
            var entry = queue.Min; queue.Remove(entry); int tile = entry.Item2;
            if (tile == destination) break;
            foreach (int neighbor in new[] { tile - width, tile + width, tile % width > 0 ? tile - 1 : -1, tile % width < width - 1 ? tile + 1 : -1 })
            {
                if (neighbor < 0 || neighbor >= terrain.Length) continue;
                int distance = distances[tile] + (terrain[neighbor] == 1 || terrain[neighbor] == 2 ? 2 : 1);
                if (distance >= distances[neighbor]) continue;
                queue.Remove(Tuple.Create(distances[neighbor], neighbor)); distances[neighbor] = distance;
                previous[neighbor] = tile; queue.Add(Tuple.Create(distance, neighbor));
            }
        }
        if (source == destination) return source;
        int step = destination;
        while (previous[step] != source) { step = previous[step]; if (step < 0) throw new InvalidOperationException("No travel path"); }
        return step;
    }
    static IEnumerator EconomicBattle(object state, EconomicHistory row)
    {
        int number = ++row.battles;
        if (G(G(state, "Battle"), "RoadEventId") != null) row.roadBattles++;
        File.WriteAllBytes(Path.Combine(row.folder, "battle-" + number + "-initial.bin"), Encode(state));
        int beforeGold = (int)G(state, "Gold"), beforeLosses = L(state, "Memorial").Count;
        var original = Members(state);
        var fight = new EconomicFight { day = (int)G(state, "ElapsedDays"),
            rating = (int)C(T("CompanyBattleSession"), "Rating", state, G(state, "Battle")),
            road = G(G(state, "Battle"), "RoadEventId") != null,
            companyPower = (double)C(T("CompanyPower"), "Force", (object)original.Select(U).ToArray()) };
        if (G(G(state, "Battle"), "RoadEventId") != null)
        {
            var road = G(state, "RoadEvent"); fight.calendarPressure = (double)G(road, "CalendarPressure");
            fight.calendarAgeDays = (int)G(road, "CalendarAgeDays");
        }
        else
        {
            var contract = L(state, "Contracts").Cast<object>().Single(c => (string)G(c, "Id") == (string)G(G(state, "Battle"), "ContractId"));
            fight.calendarPressure = (double)G(contract, "CalendarPressure");
            fight.calendarAgeDays = (int)G(contract, "CalendarAgeDays");
        }
        var enemies = ((IEnumerable)C(T("CompanyBattleSession"), "Survivors", state, G(state, "Battle"))).Cast<object>();
        fight.enemyPower = (double)C(T("CompanyPower"), "Force", (object)enemies.Select(U).ToArray()); var session = Activator.CreateInstance(T("CompanyBattleSession"), Flags, null, new[] { (object)view, state }, null);
        S(view, "state", state); S(view, "saveDirectory", Path.Combine(report.directory, (string)G(state, "Id")));
        C(session, "Start"); var mode = gm.TacticalMode;
        foreach (var camera in Resources.FindObjectsOfTypeAll<Camera>()) camera.enabled = false;
        foreach (var renderer in Resources.FindObjectsOfTypeAll<UnityEngine.Tilemaps.TilemapRenderer>()) renderer.enabled = false;
        S(mode, "AIAttacker", true); S(mode, "AIDefender", true); S(mode, "IsPlayerTurn", false);
        S(mode, "turboMode", false); C(mode, "RefreshAIIfNecessary"); C(mode, "TurboMode");
        var result = G(view, "returnedCampaign");
        if (result == null) throw new InvalidOperationException("Native economic battle did not settle");
        row.earned += Math.Max(0, (int)G(result, "Gold") - beforeGold);
        var afterMembers = new HashSet<string>(Members(result).Select(Id));
        row.gearDestroyed += original.Where(c => !afterMembers.Contains(Id(c))).Sum(c => (int)C(T("CompanyEquipment"), "GearPrice", U(c)));
        row.losses += L(result, "Memorial").Count - beforeLosses;
        fight.rounds = (int)G(mode, "currentTurn"); fight.losses = L(result, "Memorial").Count - beforeLosses;
        fight.destroyedGear = original.Where(c => !afterMembers.Contains(Id(c))).Sum(c => (int)C(T("CompanyEquipment"), "GearPrice", U(c)));
        fight.gold = (int)G(result, "Gold") - beforeGold;
        fight.ended = (bool)G(result, "Ended");
        fight.completed = fight.road ? !((IEnumerable)C(T("CompanyBattleSession"), "Survivors", result, G(result, "Battle"))).Cast<object>().Any() :
            L(result, "Contracts").Cast<object>().Any(c =>
            (string)G(c, "Id") == (string)G(G(result, "Battle"), "ContractId") && G(c, "Status").ToString() == "Completed");
        fight.injuries = Members(result).Sum(c => L(U(c), "Injuries").Count);
        row.fights.Add(fight);
        C(T("CompanyBattleSession"), "Acknowledge", result);
        if (!(bool)G(result, "Ended") && G(G(result, "Battle"), "Phase").ToString() == "Revival")
        { int cash = (int)G(result, "Gold"); C(T("CompanyBattleSession"), "Revive", result); row.revivals += cash - (int)G(result, "Gold"); }
        CleanupBattle(); S(view, "state", result);
        // Native result return chooses a nearby tile. Head to a town after the settled outing.
        yield return null;
    }
}

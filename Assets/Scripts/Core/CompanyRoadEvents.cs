// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-04: Apply explainable healthy power and approved capped encounter scaling (spec 34 slice 3).
// 2026-11-02: Saved travel encounters and the approved practice-duel proof of concept (spec 13).
// 2026-11-02: Reduce new slug encounter power budgets by 30% after player review (spec 13 feedback).
// 2026-11-08: Apply and persist calendar pressure only on newly generated road encounters (spec 40F).
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
using System;
using System.Collections.Generic;
using System.Linq;

internal enum CompanyRoadEventKind { PracticeDuel, SpitterSlugs }
internal enum CompanyRoadEventPhase { Choice, Battle, Result, Closed }
[Serializable]
internal sealed class CompanyRoadEvent
{
    public string Id, Result;
    public CompanyRoadEventKind Kind;
    public CompanyRoadEventPhase Phase;
    public int Tile, Rating, DifficultyRevision, CalendarAgeDays;
    public double CalendarPressure, TargetPower;
    public bool DuelSuccess;
    public string[] ParticipantIds = new string[0];
    public List<string> EnemyIds = new List<string>();
    public List<string> DefeatedIds = new List<string>();
}

internal static class CompanyRoadEvents
{
    // Provisional tuning: eligible travel only, no town/rest/service rolls.
    internal const int ChancePercent = 20, CooldownDays = 3;
    internal const double LeaderWeight = 1.25, EnemyPowerFraction = .56;
    internal static bool AwaitingChoice(CompanyCampaign state)
    {
        return state.RoadEvent != null && (state.RoadEvent.Phase == CompanyRoadEventPhase.Choice ||
            state.RoadEvent.Phase == CompanyRoadEventPhase.Result);
    }
    internal static double EncounterPower(CompanyCampaign state)
    {
        return state.Members.Sum(m => CompanyContracts.Power(state.Characters.Single(c => c.Id == m.CharacterId).Unit) *
            (m.CharacterId == state.LeaderId ? LeaderWeight : 1)) * CompanyPower.FormationMultiplier(state.Members.Count) * EnemyPowerFraction *
            CompanyPower.HealthMultiplier(state.Members.Select(m => state.Characters.Single(c => c.Id == m.CharacterId).Unit));
    }
    internal static void AfterTravel(CompanyCampaign state, int previousTile)
    {
        if (state.Preview || state.Busy || state.ElapsedDays < state.NextRoadEventDay ||
            state.Towns.Any(t => t.Tile == state.Position) || state.Next(100) >= ChancePercent) return;
        var candidates = state.Characters.Where(c => c.Id != state.LeaderId &&
            state.Members.Any(m => m.CharacterId == c.Id) && CompanyInjuries.Healthy(c.Unit)).ToList();
        bool slugs = SpeciesPolicy.IsAllowed(Race.SpitterSlugs, SpeciesUse.Enemy);
        if (candidates.Count < 2 && !slugs) return;
        var occurrence = new CompanyRoadEvent { Id = Guid.NewGuid().ToString("N"), Tile = state.Position };
        if (candidates.Count >= 2 && (!slugs || state.Next(2) == 0))
        {
            var first = candidates[state.Next(candidates.Count)]; candidates.Remove(first);
            var second = candidates[state.Next(candidates.Count)];
            occurrence.Kind = CompanyRoadEventKind.PracticeDuel;
            occurrence.Phase = CompanyRoadEventPhase.Choice;
            occurrence.ParticipantIds = new[] { first.Id, second.Id };
            occurrence.DuelSuccess = state.Next(2) == 0; // Saved before presenting the choice; no reroll on reload.
        }
        else
        {
            occurrence.Kind = CompanyRoadEventKind.SpitterSlugs;
            occurrence.Phase = CompanyRoadEventPhase.Battle;
            double pressure = CompanyContracts.CalendarPressure(state);
            double budget = EncounterPower(state) * CompanyContracts.RoadEncounterMultiplier(pressure);
            occurrence.DifficultyRevision = CompanyContracts.DifficultyRevision;
            occurrence.CalendarAgeDays = CompanyContracts.CalendarAgeDays(state);
            occurrence.CalendarPressure = pressure;
            occurrence.TargetPower = budget;
            var first = CompanyContracts.Enemy(state, Race.SpitterSlugs, 1);
            var enemies = new List<CompanyCharacter> { first };
            double sum = CompanyContracts.Power(first.Unit);
            while(sum * CompanyPower.FormationMultiplier(enemies.Count) < budget)
            {
                if(enemies.Count>=128)break;
                var next=CompanyContracts.Enemy(state,Race.SpitterSlugs,1);double nextPower=CompanyContracts.Power(next.Unit);
                double before=sum*CompanyPower.FormationMultiplier(enemies.Count);
                double after=(sum+nextPower)*CompanyPower.FormationMultiplier(enemies.Count+1);
                if(after>=budget && Math.Abs(before-budget)<Math.Abs(after-budget))break;
                enemies.Add(next);sum+=nextPower;
            }
            foreach (var enemy in enemies) { enemy.Employment = CompanyEmployment.Unavailable; enemy.Unit.Side = 1; }
            state.Characters.AddRange(enemies);
            occurrence.EnemyIds = enemies.Select(c => c.Id).ToList();
            occurrence.Rating = Math.Max(1, (int)Math.Round((budget - 45) / 30));
        }
        state.RoadEvent = occurrence;
        state.NextRoadEventDay = checked(state.ElapsedDays + CooldownDays);
        // The previous returned battle no longer needs its encounter context. Historical units remain recorded.
        state.Battle = occurrence.Kind == CompanyRoadEventKind.SpitterSlugs ? new CompanyBattleRecord
        {
            Id = Guid.NewGuid().ToString("N"), RoadEventId = occurrence.Id, Phase = CompanyBattlePhase.Deployment,
            ReturnTile = previousTile, TargetTile = state.Position, PositionCommitted = true, Seed = state.Next(int.MaxValue)
        } : null;
    }
    internal static void ResolveDuel(CompanyCampaign state, bool allow)
    {
        var occurrence = state.RoadEvent;
        if (state.InBattle || occurrence == null || occurrence.Kind != CompanyRoadEventKind.PracticeDuel ||
            occurrence.Phase != CompanyRoadEventPhase.Choice) throw new InvalidOperationException("No practice duel is awaiting a choice.");
        if (!allow) { occurrence.Phase = CompanyRoadEventPhase.Closed; return; }
        var participants = occurrence.ParticipantIds.Select(id => state.Characters.Single(c => c.Id == id)).ToArray();
        foreach (var c in participants)
        {
            if (occurrence.DuelSuccess) c.Unit.SetExp(c.Unit.Experience + c.Unit.CurrentLevelExperienceRequired / 2f);
            else c.Unit.Health = Math.Max(1, (c.Unit.Health + 1) / 2); // Direct attrition: deliberately no injury roll.
        }
        occurrence.Result = occurrence.DuelSuccess ?
            "The practice duel went well. Both mercenaries gained half their next-level XP requirement." :
            "An accident ended the duel. Both mercenaries lost half their current HP, but suffered no injuries.";
        occurrence.Phase = CompanyRoadEventPhase.Result;
    }
    internal static void Acknowledge(CompanyCampaign state)
    {
        if (state.RoadEvent == null || state.RoadEvent.Phase != CompanyRoadEventPhase.Result)
            throw new InvalidOperationException("No road-event result is awaiting acknowledgement.");
        state.RoadEvent.Phase = CompanyRoadEventPhase.Closed;
    }
    internal static void Validate(CompanyCampaign state)
    {
        if (state.NextRoadEventDay < 0) throw new InvalidOperationException("Invalid road-event cooldown.");
        var e = state.RoadEvent;
        if (e == null) return; // Accepted checkpoints created before road events.
        if (string.IsNullOrEmpty(e.Id) || !Enum.IsDefined(typeof(CompanyRoadEventKind), e.Kind) ||
            !Enum.IsDefined(typeof(CompanyRoadEventPhase), e.Phase) || e.Tile < 0 || e.Tile >= state.Terrain.Length ||
            e.ParticipantIds == null || e.EnemyIds == null || e.DefeatedIds == null ||
            e.ParticipantIds.Distinct().Count() != e.ParticipantIds.Length || e.EnemyIds.Distinct().Count() != e.EnemyIds.Count ||
            e.ParticipantIds.Concat(e.EnemyIds).Any(id => !state.Characters.Any(c => c.Id == id)) || e.DefeatedIds.Except(e.EnemyIds).Any())
            throw new InvalidOperationException("Invalid saved road event.");
        if (e.Kind == CompanyRoadEventKind.PracticeDuel && (e.ParticipantIds.Length != 2 || e.ParticipantIds.Contains(state.LeaderId) ||
            e.EnemyIds.Count != 0 || e.Phase == CompanyRoadEventPhase.Battle ||
            (e.Phase != CompanyRoadEventPhase.Closed && e.ParticipantIds.Any(id => !state.Members.Any(m => m.CharacterId == id)))))
            throw new InvalidOperationException("Invalid practice-duel participants.");
        if (e.Kind == CompanyRoadEventKind.SpitterSlugs && (e.EnemyIds.Count == 0 || e.Rating < 1 ||
            e.DifficultyRevision < 0 || e.CalendarAgeDays < 0 || double.IsNaN(e.CalendarPressure) || double.IsInfinity(e.CalendarPressure) || e.CalendarPressure < 0 ||
            double.IsNaN(e.TargetPower) || double.IsInfinity(e.TargetPower) || e.TargetPower < 0 ||
            e.ParticipantIds.Length != 0 || (e.Phase != CompanyRoadEventPhase.Battle && e.Phase != CompanyRoadEventPhase.Closed) ||
            (e.Phase == CompanyRoadEventPhase.Battle && (state.Battle == null || state.Battle.RoadEventId != e.Id))))
            throw new InvalidOperationException("Invalid road-encounter battle.");
        if (e.Phase == CompanyRoadEventPhase.Result && string.IsNullOrEmpty(e.Result))
            throw new InvalidOperationException("Missing road-event result.");
    }
    internal static bool Same(CompanyRoadEvent a, CompanyRoadEvent b)
    {
        if (a == null || b == null) return a == b;
        return a.Id == b.Id && a.Kind == b.Kind && a.Phase == b.Phase && a.Tile == b.Tile && a.Rating == b.Rating &&
            a.DifficultyRevision == b.DifficultyRevision && a.CalendarAgeDays == b.CalendarAgeDays &&
            a.CalendarPressure.Equals(b.CalendarPressure) && a.TargetPower.Equals(b.TargetPower) &&
            a.DuelSuccess == b.DuelSuccess && a.Result == b.Result && a.ParticipantIds.SequenceEqual(b.ParticipantIds) &&
            a.EnemyIds.SequenceEqual(b.EnemyIds) && a.DefeatedIds.SequenceEqual(b.DefeatedIds);
    }
}

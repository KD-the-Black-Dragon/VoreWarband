// Modified for the VoreWar mercenary-company conversion.
// 2026-11-06: Keep action rollback in memory and remove action-level autosave work.
// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-06: Explored-only weighted routes and transactional native movement steps (spec 40B).
// See CHANGES.md and License.txt for GPL-3.0 terms.
using System;
using System.Collections.Generic;
using System.Linq;

internal enum CompanyTravelStep { Blocked, Moved, Event, Deployment }

internal static class CompanyTravel
{
    internal const float SecondsPerTile = .1f;

    sealed class SearchNode : IComparable<SearchNode>
    {
        internal int Tile, Cost, Steps;
        public int CompareTo(SearchNode other)
        {
            int order = Cost.CompareTo(other.Cost);
            if (order == 0) order = Steps.CompareTo(other.Steps);
            return order == 0 ? Tile.CompareTo(other.Tile) : order;
        }
    }
    // Costs are independent of today's budget. Equal-cost routes prefer fewer steps.
    internal static int[] Path(CompanyCampaign state, int destination)
    {
        if (destination < 0 || destination >= state.Terrain.Length || !state.Discovered[destination]) return new int[0];
        if (destination == state.Position) return new[] { destination };
        int count = state.Terrain.Length;
        var costs = Enumerable.Repeat(int.MaxValue, count).ToArray();
        var steps = Enumerable.Repeat(int.MaxValue, count).ToArray();
        var parents = Enumerable.Repeat(-1, count).ToArray();
        var occupied = new HashSet<int>(state.Contracts.Where(c => c.Status == CompanyContractStatus.Active && c.Tile != destination).Select(c => c.Tile));
        var pending = new SortedSet<SearchNode>();
        costs[state.Position] = steps[state.Position] = 0;
        pending.Add(new SearchNode { Tile = state.Position });
        while (pending.Count > 0)
        {
            var current = pending.Min;
            pending.Remove(current);
            if (current.Cost != costs[current.Tile] || current.Steps != steps[current.Tile]) continue;
            if (current.Tile == destination)
            {
                var path = new List<int>();
                for (int tile = destination; tile != -1; tile = parents[tile]) path.Add(tile);
                path.Reverse();
                return path.ToArray();
            }
            foreach (int next in CompanyWorldGeneration.Neighbors(state, current.Tile))
            {
                if (!state.Discovered[next] || occupied.Contains(next)) continue;
                int cost = current.Cost + state.MoveCost(next), length = current.Steps + 1;
                if (cost > costs[next] || (cost == costs[next] && length >= steps[next])) continue;
                costs[next] = cost; steps[next] = length; parents[next] = current.Tile;
                pending.Add(new SearchNode { Tile = next, Cost = cost, Steps = length });
            }
        }
        return new int[0];
    }
    internal static bool Select(CompanyCampaign state, int destination)
    {
        state.TravelDestination = null; // A different map click ends the previous confirmation.
        int[] path = Path(state, destination);
        if (path.Length < 2) return false;
        state.TravelDestination = destination;
        return true;
    }
    internal static void ClearArrival(CompanyCampaign state)
    {
        if (state.TravelDestination == state.Position) state.TravelDestination = null;
    }
    internal static CompanyTravelStep Advance(CompanyCampaign state, int tile)
    {
        if (state.Busy || tile < 0 || tile >= state.Terrain.Length ||
            CompanyContracts.Distance(state, state.Position, tile) != 1 || state.Actions < state.MoveCost(tile)) return CompanyTravelStep.Blocked;
        int previousTile = state.Position, previousActions = state.Actions, previousCooldown = state.NextRoadEventDay;
        uint previousRandom = state.RandomState;
        int? previousDestination = state.TravelDestination;
        var previousBattle = state.Battle;
        var previousOccurrence = state.RoadEvent;
        int previousCharacters = state.Characters.Count;
        var newlyVisible = new List<int>(25);
        int x = tile % state.Width, y = tile / state.Width;
        for (int dy = -2; dy <= 2; dy++)
            for (int dx = -2; dx <= 2; dx++)
                if (x + dx >= 0 && x + dx < state.Width && y + dy >= 0 && y + dy < state.Height)
                {
                    int seen = (y + dy) * state.Width + x + dx;
                    if (!state.Discovered[seen]) newlyVisible.Add(seen);
                }
        try
        {
            var contract = state.Contracts.FirstOrDefault(c => c.Status == CompanyContractStatus.Active && c.Tile == tile);
            if (contract != null)
            {
                CompanyBattleSession.Prepare(state, contract);
                return CompanyTravelStep.Deployment;
            }
            string previousEvent = state.RoadEvent == null ? null : state.RoadEvent.Id;
            if (!state.Move(tile)) return CompanyTravelStep.Blocked;
            CompanyRoadEvents.AfterTravel(state, previousTile);
            ClearArrival(state);
            return state.RoadEvent != null && state.RoadEvent.Id != previousEvent ? CompanyTravelStep.Event : CompanyTravelStep.Moved;
        }
        catch
        {
            // Only native step/event fields can change here. Never copy the whole company for walking.
            state.Position = previousTile; state.Actions = previousActions; state.RandomState = previousRandom;
            state.TravelDestination = previousDestination; state.NextRoadEventDay = previousCooldown;
            state.Battle = previousBattle; state.RoadEvent = previousOccurrence;
            if (state.Characters.Count > previousCharacters)
                state.Characters.RemoveRange(previousCharacters, state.Characters.Count - previousCharacters);
            foreach (int seen in newlyVisible) state.Discovered[seen] = false;
            throw;
        }
    }
}

// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-06: Retain separate route evidence for the save responsiveness follow-up.
// 2026-11-06: Verify native route planning, click confirmation, interruptions and persistence (spec 40B).
// See CHANGES.md and License.txt for GPL-3.0 terms. All synthetic files are retained without deletion.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CompanyTravelAudit
{
    const string Key = "Warband.Travel40B.Audit";
    static string Prefix => Environment.GetCommandLineArgs().Contains("-saveReview") ? "save41-travel" : "travel40b";
    static IEnumerator work;
    static string directory;
    static double started;
    static readonly List<string> checks = new List<string>();
    static CompanyTravelAudit() { EditorApplication.update += Update; }
    public static void Run()
    {
        directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/" + Prefix + "-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(Application.dataPath, "../Logs/" + Prefix + "-latest.txt"), directory);
        SessionState.SetString(Key + ".directory", directory);
        Environment.SetEnvironmentVariable("VOREWAR_AUTHORING_USERDATA", Path.Combine(directory, "UserData"));
        EditorSceneManager.OpenScene("Assets/base.unity");
        SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
    }
    static void Update()
    {
        int exit = SessionState.GetInt(Key + ".exit", 0);
        if (exit != 0 && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.EraseInt(Key + ".exit");
            Environment.SetEnvironmentVariable("VOREWAR_AUTHORING_USERDATA", null);
            EditorApplication.Exit(exit - 1); return;
        }
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
        try
        {
            if (work == null) { started = EditorApplication.timeSinceStartup; work = Review(); }
            if (EditorApplication.timeSinceStartup - started > 240) throw new TimeoutException("Travel review timed out.");
            if (!work.MoveNext()) Finish(null);
        }
        catch (Exception error) { Finish(error); }
    }
    static void Finish(Exception error)
    {
        directory = SessionState.GetString(Key + ".directory", "");
        File.WriteAllText(Path.Combine(directory, "result.json"), JsonUtility.ToJson(new Result { passed = error == null, checks = checks.ToArray(), error = error == null ? null : error.ToString() }, true));
        if (error != null) Debug.LogException(error);
        SessionState.SetBool(Key, false); SessionState.SetInt(Key + ".exit", error == null ? 1 : 2);
        EditorApplication.isPlaying = false;
    }
    [Serializable] sealed class Result { public bool passed; public string[] checks; public string error; }
    static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); checks.Add(message); }
    static T Field<T>(object target, string name)
    { return (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target); }
    static CompanyCampaign Map(int width = 8, int height = 6)
    {
        var state = new CompanyCampaign { Width = width, Height = height, Terrain = new int[width * height],
            Discovered = Enumerable.Repeat(true, width * height).ToArray(), Position = 0, Actions = 6, DailyActions = 6,
            RandomState = 19, Seed = 19, Gold = 10000, BalanceRevision = BalanceAuthoring.OfficialRevision };
        state.Towns.Add(new CompanyTown { Id = "test-town", Name = "Test town", Tile = 0 });
        return state;
    }
    static int Cost(CompanyCampaign state, int[] path) { return path.Skip(1).Sum(state.MoveCost); }
    static void Click(CompanyCampaignView view, int tile)
    {
        int index = Array.IndexOf(Field<int[]>(view, "tileIndices"), tile);
        Check(index >= 0, "Destination has a native visible map button");
        var button = Field<Button[]>(view, "tiles")[index];
        Check(button.interactable, "Destination button accepts map input");
        button.onClick.Invoke();
    }
    static CompanyCampaign Company()
    {
        var state = Map();
        CompanyEconomy.Begin(state, CompanyEconomy.Create(state, Race.Avians, 1, true));
        state.Gold = 10000;
        foreach (Race race in new[] { Race.Gnolls, Race.Lizards })
        {
            var member = CompanyEconomy.Create(state, race, 0, false);
            member.Employment = CompanyEmployment.Employed;
            state.Characters.Add(member); state.Members.Add(new CompanyMember { CharacterId = member.Id });
            foreach (var item in member.Unit.Items.Where(i => i != null)) state.Inventory.Add(item);
        }
        state.Position = 17; state.Actions = 6;
        state.Validate();
        return state;
    }
    static uint DuelSeed()
    {
        for (uint seed = 1; seed < 100000; seed++)
        {
            var probe = new CompanyCampaign { RandomState = seed };
            if (probe.Next(100) < 20 && probe.Next(2) == 0) return seed;
        }
        throw new InvalidOperationException("No encounter seed found.");
    }
    static IEnumerator Review()
    {
        for (int frame = 0; frame < 8; frame++) yield return null;
        directory = SessionState.GetString(Key + ".directory", "");
        var gm = UnityEngine.Object.FindObjectOfType<GameManager>(); Check(gm != null, "Native scene initialized"); gm.enabled = false;
        Check(Path.GetFullPath(State.StorageDirectory).StartsWith(directory, StringComparison.OrdinalIgnoreCase), "Startup settings isolated");
        CompanyCampaignSave.DiagnosticDirectory = Path.Combine(directory, "Saves");
        var state = Map(); state.Position = 17;
        state.Terrain[18] = state.Terrain[19] = state.Terrain[20] = 2;
        uint random = state.RandomState;
        int[] path = CompanyTravel.Path(state, 21);
        Check(Cost(state, path) == 6 && path.Length == 7, "Weighted path beats geometrically shorter rough terrain");
        Check(state.Actions == 6 && state.Position == 17 && state.RandomState == random, "Preview spends no actions, time or RNG");
        state.Discovered[21] = false;
        Check(CompanyTravel.Path(state, 21).Length == 0, "Unknown destination has no route");
        state.Discovered[21] = true;
        foreach (int tile in Enumerable.Range(0, state.Terrain.Length).Where(t => t % state.Width == 3)) state.Discovered[tile] = false;
        Check(CompanyTravel.Path(state, 21).Length == 0, "Known destination cannot route through unknown intervening terrain");
        state = Map(); state.Position = 17;
        Check(CompanyTravel.Path(state, 18).SequenceEqual(new[] { 17, 18 }), "Equal-cost routing keeps direct adjacent steps");
        state.Position = 7;
        Check(CompanyTravel.Path(state, 8).Length > 2, "Routes never wrap grid rows");

        // Independent repeated relaxation oracle, including mixed costs and fog.
        for (int seed = 0; seed < 12; seed++)
        {
            var grid = Map(6, 6); var rng = new System.Random(seed);
            for (int i = 1; i < grid.Terrain.Length; i++) { grid.Terrain[i] = rng.Next(4); grid.Discovered[i] = rng.Next(5) != 0; }
            var costs = Enumerable.Repeat(int.MaxValue, grid.Terrain.Length).ToArray(); costs[0] = 0;
            for (int pass = 0; pass < grid.Terrain.Length; pass++)
                for (int tile = 0; tile < grid.Terrain.Length; tile++)
                    if (costs[tile] != int.MaxValue)
                        foreach (int next in CompanyWorldGeneration.Neighbors(grid, tile))
                            if (grid.Discovered[next]) costs[next] = Math.Min(costs[next], costs[tile] + grid.MoveCost(next));
            for (int target = 0; target < grid.Terrain.Length; target++)
            {
                path = CompanyTravel.Path(grid, target);
                Check(costs[target] == int.MaxValue ? path.Length == 0 : path.Length > 0 && Cost(grid, path) == costs[target], "Oracle shortest AP cost seed " + seed + " tile " + target);
                Check(path.All(t => grid.Discovered[t]), "Path uses explored terrain only");
            }
        }
        var view = gm.Start_Mode.CompanySaveView; view.Open(gm.Start_Mode);
        state = Map(); state.Position = 17;
        view.Install(state);
        Click(view, 18);
        Check(state.Position == 18 && state.Actions == 5, "Adjacent click commits one immediate step");
        Check(view.IsTravelling && CompanyTravel.SecondsPerTile == .1f, "Single move starts the fixed 0.1-second animation");
        while (view.IsTravelling) yield return null;
        Check(state.TravelDestination == null, "Arrival clears route intent");
        state = Map(); state.Position = 17; view.Install(state);
        Click(view, 21);
        Check(state.Position == 17 && state.Actions == 6 && state.TravelDestination == 21, "First distant click only previews");
        Click(view, 22);
        Check(state.Position == 17 && state.TravelDestination == 22, "Different destination redraws without moving");
        Click(view, 22);
        while (view.IsTravelling) yield return null;
        Check(state.Position == 22 && state.Actions == 1 && state.TravelDestination == null, "Second consecutive click follows path to arrival");

        state = Map(); state.Position = 17; view.Install(state);
        Click(view, 21); Click(view, 21);
        Check(state.Position == 18 && view.IsTravelling, "Multi-step travel yields after its first committed step");
        Click(view, 30);
        Check(state.Position == 18 && !view.IsTravelling && state.TravelDestination == 30, "Different destination stops movement and starts a fresh preview");
        yield return null;
        Check(state.Position == 18 && state.Actions == 5, "Cancelled route cannot execute another step");
        Click(view, 30);
        while (view.IsTravelling) yield return null;
        Check(state.Position == 30 && state.TravelDestination == null, "New route requires its own confirming click");

        state = Map(); state.Position = 17; view.Install(state);
        Click(view, 47);
        int[] indices = Field<int[]>(view, "tileIndices");
        var numbers = Field<Text[]>(view, "routeNumbers");
        Check(numbers[Array.IndexOf(indices, 17)].text == "6", "Origin overlay shows remaining AP");
        Check(numbers[Array.IndexOf(indices, 47)].text == "-3", "Over-budget overlay shows additional AP needed today");
        Check(numbers[Array.IndexOf(indices, 47)].color == CompanyRouteGraphic.OverBudget, "Over-budget tile is red");
        Click(view, 47);
        while (view.IsTravelling) yield return null;
        Check(state.Actions == 0 && state.Position != 47 && state.TravelDestination == 47, "Budget exhaustion retains destination and stops");
        int stopped = state.Position;
        Click(view, 47);
        Check(state.Position == stopped && state.Actions == 0, "Zero-action confirmation cannot move");
        var clock = new CompanyClock(); clock.EndDay(state);
        Check(state.ElapsedDays == 1 && state.Actions == 6 && state.TravelDestination == 47, "Explicit new day preserves route and refills AP");
        Click(view, 47);
        while (view.IsTravelling) yield return null;
        Check(state.Position == 47 && state.TravelDestination == null, "One click resumes selected route after new day");

        state = Map(); state.Position = 17; state.Actions = 1; state.Terrain[18] = 2; view.Install(state);
        Click(view, 18);
        Check(state.Position == 17 && state.Actions == 1 && state.TravelDestination == 18, "Unaffordable adjacent terrain preserves preview without spending AP");
        while (view.IsTravelling) yield return null;
        state = Company(); state.RandomState = DuelSeed(); view.Install(state);
        Click(view, 21); Click(view, 21);
        while (view.IsTravelling) yield return null;
        Check(state.Position == 18 && state.Actions == 5 && CompanyRoadEvents.AwaitingChoice(state), "Road event arrests automatic travel on its triggering step");
        Check(state.TravelDestination == 21, "Event interruption retains route destination");
        string eventId = state.RoadEvent.Id; bool outcome = state.RoadEvent.DuelSuccess; random = state.RandomState;
        string save = CompanyCampaignSave.WriteGeneration(state, Path.Combine(CompanyCampaignSave.DefaultDirectory, state.Id));
        var restored = CompanyCampaignSave.Load(save);
        Check(restored.TravelDestination == 21 && restored.Position == 18 && restored.Actions == 5, "Interrupted route survives checkpoint round trip");
        Check(restored.RoadEvent.Id == eventId && restored.RoadEvent.DuelSuccess == outcome && restored.RandomState == random, "Saved event outcome and RNG do not reroll");
        CompanyRoadEvents.ResolveDuel(state, false); view.ShowMap();
        Click(view, 21);
        while (view.IsTravelling) yield return null;
        Check(state.Position == 21 && state.TravelDestination == null, "One click resumes after event acknowledgment");

        state = Company(); state.NextRoadEventDay = 100;
        var contract = state.Contracts.First(); state.Position = state.Towns.First(t => t.Id == contract.TownId).Tile;
        CompanyContracts.Accept(state, contract); state.Position = 17; contract.Tile = 21;
        view.Install(state); Click(view, 21); Click(view, 21);
        while (view.IsTravelling) yield return null;
        Check(state.Position == 20 && state.Battle != null && state.Battle.Phase == CompanyBattlePhase.Deployment, "Contract destination stops at native deployment review");
        Check(state.Actions == 3 && state.TravelDestination == 21, "Deployment preserves target step cost and route intent");
        state.Battle = null;
        path = CompanyTravel.Path(state, 23);
        Check(!path.Contains(21), "Routes cannot pass through an intervening active enemy camp");
        state.TravelDestination = null;
        Check(CompanyCampaignSave.Load(CompanyCampaignSave.WriteGeneration(state, Path.Combine(CompanyCampaignSave.DefaultDirectory, state.Id))).TravelDestination == null, "No-route legacy-compatible default round trips");
        state = Map(120, 80); var timer = System.Diagnostics.Stopwatch.StartNew();
        path = CompanyTravel.Path(state, state.Terrain.Length - 1); timer.Stop();
        Check(Cost(state, path) == 198, "Full-size region has correct minimum route cost");
        Check(timer.Elapsed.TotalSeconds < 2, "Full-size pathfinding remains responsive");
        File.WriteAllText(Path.Combine(directory, "path-performance.txt"), timer.Elapsed.TotalMilliseconds + " milliseconds");
    }
}

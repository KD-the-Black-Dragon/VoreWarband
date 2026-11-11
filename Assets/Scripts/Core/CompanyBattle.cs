// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-10: Persist and sequence the independent leader-glory Grudge notice after the ordinary post-battle event (spec 40D narrative follow-up).
// 2026-11-09: Prune boss-contract enemies after result and post-battle event settlement (mission follow-up).
// 2026-11-09: Keep surviving enemies in their actual battle state when a boss objective is completed (campaign mission follow-up).
// 2026-11-09: Settle boss objectives on downing, digestion or flight before combat cleanup (campaign mission expansion).
// 2026-11-08: Assign unique ending identities and archive scores before any Ironman cleanup (spec 40E).
// 2026-11-08: Enforce the eight-unit limit on new campaign battle deployments (balance follow-up item 2).
// 2026-11-08: Generate contract enemies at target arrival and retain the force between attempts (spec 09 follow-up).
// 2026-11-07: Persist battle evidence and queue the post-battle trait scene after revival (spec 40D).
// 2026-11-06: Queue detached battle-turn autosaves and settle outcomes without serializing live gameplay.
// 2026-11-01: Isolated tactical adapter, transactional outcomes, revival and memorial records (spec 10).
// 2026-11-01: Guard HP during detached battle settlement and use individual species labels in memorials (09–10 feedback).
// 2026-11-01: Clear leader wounds on special revival and consolidate enemy recovery into the shared daily recovery stage (spec 11).
// 2026-11-01: Award surviving deployed members challenge-scaled XP once on contract completion (spec 11 feedback).
// 2026-11-02: Reuse tactical deployment/fate/return for saved road encounters while keeping contract rewards isolated (spec 13).
// 2026-11-03: Persist/resume full company battles and their random state at safe boundaries (spec 19).
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
using System;
using System.Collections.Generic;
using System.Linq;

// Keep the original serialized enum values stable; append the new phase.
internal enum CompanyBattlePhase { Deployment = 0, Summary = 1, Revival = 2, Returned = 3, PostBattleEvent = 4, PostBattleGrudgeEvent = 5 }
[Serializable]
internal sealed class CompanyBattleRecord
{
    public string Id, ContractId, RoadEventId, Summary;
    public CompanyBattlePhase Phase;
    public int ReturnTile, TargetTile, Seed;
    public int EvidenceVersion, InitialEnemyCount;
    public bool PositionCommitted;
    public string[] DeployedIds;
    public TacticalData Tactical;
    public int[] RandomState;
    public string UnityRandomState;
    public float CameraX, CameraY, CameraZoom;
    public int WorldTurn;
    public bool WorldNight;
    public CompanyPostBattleEvent PostBattleEvent;
    public CompanyPostBattleEvent GrudgeEvent;
}
[Serializable]
internal sealed class CompanyMemorial
{
    public string CharacterId, Name, Species, Fate;
    public int Day;
}

internal sealed class CompanyBattleSession
{
    internal const int MaximumCampaignBattleForceSize = 8;
    readonly CompanyCampaignView view;
    readonly CompanyCampaign state;
    readonly World previousWorld;
    readonly Rand previousRandom;
    readonly UnityEngine.Random.State previousUnityRandom;
    bool finished;
    int autosavedTurn = -1, autosavedSide = -1;
    internal CompanyBattleSession(CompanyCampaignView view, CompanyCampaign state)
    {
        this.view = view; this.state = state; previousWorld = State.World; previousRandom = State.Rand;
        previousUnityRandom = UnityEngine.Random.state;
    }
    internal bool LeaderPermanentlyLost
    { get { return !state.LeaderRevival && state.Characters.Single(c => c.Id == state.LeaderId).Unit.Digested; } }
    internal static void Prepare(CompanyCampaign state, CompanyContract contract)
    {
        if (state.Busy || contract.Status != CompanyContractStatus.Active)
            throw new InvalidOperationException("This target is no longer available.");
        CompanyContracts.EnsureEnemyForce(state, contract);
        if (!CompanyContracts.Survivors(state, contract).Any()) throw new InvalidOperationException("This target is no longer available.");
        state.Battle = new CompanyBattleRecord { Id = Guid.NewGuid().ToString("N"), ContractId = contract.Id,
            ReturnTile = state.Position, TargetTile = contract.Tile, Seed = state.Next(int.MaxValue), Phase = CompanyBattlePhase.Deployment };
    }
    internal static List<string> EnemyIds(CompanyCampaign state, CompanyBattleRecord battle)
    { return battle.RoadEventId == null ? state.Contracts.Single(c => c.Id == battle.ContractId).EnemyIds : state.RoadEvent.EnemyIds; }
    internal static List<string> DefeatedIds(CompanyCampaign state, CompanyBattleRecord battle)
    { return battle.RoadEventId == null ? state.Contracts.Single(c => c.Id == battle.ContractId).DefeatedIds : state.RoadEvent.DefeatedIds; }
    internal static IEnumerable<CompanyCharacter> Survivors(CompanyCampaign state, CompanyBattleRecord battle)
    { return EnemyIds(state, battle).Except(DefeatedIds(state, battle)).Select(id => state.Characters.Single(c => c.Id == id)); }
    internal static int Rating(CompanyCampaign state, CompanyBattleRecord battle)
    { return battle.RoadEventId == null ? state.Contracts.Single(c => c.Id == battle.ContractId).Rating : state.RoadEvent.Rating; }
    internal void Start()
    {
        var deployed = state.Members.Where(m => !m.Reserve || m.CharacterId == state.LeaderId).Select(m => state.Characters.Single(c => c.Id == m.CharacterId)).ToArray();
        if (deployed.Length > MaximumCampaignBattleForceSize)
            throw new InvalidOperationException("A company can deploy no more than " + MaximumCampaignBattleForceSize + " members in a campaign battle.");
        state.Battle.DeployedIds = deployed.Select(c => c.Id).ToArray();
        if (deployed.Length == 0) throw new InvalidOperationException("The company leader must deploy.");
        State.Rand = new Rand(state.Battle.Seed); UnityEngine.Random.InitState(state.Battle.Seed);
        new World(CompanyEquipment.Repository); // Battle services only; no empire simulation/configuration reset.
        var attacker = new Army(new Empire(), new Vec2i(0, 0), 0) { Name = "Your company" };
        var defender = new Army(new Empire(), new Vec2i(0, 0), 1) { Name = "Contract target" };
        attacker.Empire.ReplacedRace = deployed[0].Unit.Race;
        var enemies = Survivors(state, state.Battle).ToArray();
        state.Battle.EvidenceVersion = 1;
        state.Battle.InitialEnemyCount = enemies.Length;
        defender.Empire.ReplacedRace = enemies[0].Unit.Race;
        foreach (var c in deployed) { c.Unit.Side = 0; attacker.Units.Add(c.Unit); }
        foreach (var c in enemies) { c.Unit.Side = 1; defender.Units.Add(c.Unit); }
        state.InBattle = true;
        var gm = State.GameManager; gm.CompanyBattle = this; gm.PureTactical = false;
        var terrain = new[] { StrategicTileType.grass, StrategicTileType.forest, StrategicTileType.hills, StrategicTileType.desert }[state.Terrain[state.Battle.TargetTile]];
        gm.ActivateCompanyBattle(terrain, attacker, defender, Rating(state, state.Battle));
        Autosave();
    }
    internal CompanyCampaign Campaign => state;
    internal CompanyCampaignView View => view;
    internal void Capture()
    {
        state.Battle.Tactical = State.GameManager.TacticalMode.Export();
        state.Battle.RandomState = State.Rand.Capture();
        state.Battle.UnityRandomState = UnityEngine.JsonUtility.ToJson(UnityEngine.Random.state);
        var camera = State.GameManager.Camera;
        state.Battle.CameraX = camera.transform.position.x; state.Battle.CameraY = camera.transform.position.y;
        state.Battle.CameraZoom = camera.orthographicSize;
        state.Battle.WorldTurn = State.World.Turn;
        state.Battle.WorldNight = State.World.IsNight;
        CompanyBattleSnapshot.Validate(state);
    }
    internal void Autosave()
    {
        if (finished) return;
        Capture();
        var tactical = state.Battle.Tactical;
        if (!tactical.isAPlayerTurn || (autosavedTurn == tactical.currentTurn && autosavedSide == tactical.activeSide)) return;
        CompanySaveWorker.Enqueue(state, view.SaveDirectory);
        autosavedTurn = tactical.currentTurn; autosavedSide = tactical.activeSide;
    }
    internal void Resume()
    {
        var gm = State.GameManager;
        State.Rand = Rand.Resume(state.Battle.RandomState);
        UnityEngine.Random.state = UnityEngine.JsonUtility.FromJson<UnityEngine.Random.State>(state.Battle.UnityRandomState);
        new World(CompanyEquipment.Repository); State.World.Turn = state.Battle.WorldTurn; State.World.IsNight = state.Battle.WorldNight;
        gm.CompanyBattle = this; gm.PureTactical = false;
        CompanyBattleSnapshot.Bind(state);
        gm.ResumeCompanyBattle(state.Battle.Tactical);
        State.Rand = Rand.Resume(state.Battle.RandomState);
        UnityEngine.Random.state = UnityEngine.JsonUtility.FromJson<UnityEngine.Random.State>(state.Battle.UnityRandomState);
        gm.Camera.transform.position = new UnityEngine.Vector3(state.Battle.CameraX, state.Battle.CameraY, gm.Camera.transform.position.z);
        gm.Camera.orthographicSize = state.Battle.CameraZoom;
    }
    internal void AbandonForLoad()
    {
        finished = true;
        State.GameManager.ReturnFromCompanyBattle(); RestoreServices();
    }
    internal bool RevivalEnabled { get { return state.LeaderRevival; } }
    internal void ToggleRevival()
    {
        if (finished) return;
        if (!State.GameManager.TacticalMode.CanSaveCompanyBattle) throw new InvalidOperationException("Wait until your side is ready to act before changing this setting.");
        state.LeaderRevival = !state.LeaderRevival;
        Autosave();
    }
    internal void ExitToCheckpoint()
    {
        if (finished) return;
        if (State.GameManager.TacticalMode.CanSaveCompanyBattle) Autosave();
        finished = true;
        AbandonForLoad();
        view.ReturnToMainMenu();
    }

    internal void RestoreServices()
    {
        State.World = previousWorld; State.Rand = previousRandom; UnityEngine.Random.state = previousUnityRandom;
        State.GameManager.CompanyBattle = null;
    }
    internal void Complete(IList<Actor_Unit> actors, bool stalemate)
    {
        if (finished) return;
        // Reconcile a detached saved graph, then install it atomically. Actors keep the old graph until cleanup.
        var result = CompanySnapshot.Take(state);
        CompanyEconomy.Bind(result);
        CompanyBattleSnapshot.Bind(result);
        var battle = result.Battle;
        var contract = battle.RoadEventId == null ? result.Contracts.Single(c => c.Id == battle.ContractId) : null;
        var enemyIds = EnemyIds(result, battle); var defeatedIds = DefeatedIds(result, battle);
        var deployedCompanyActors = actors.Where(a => a.Unit.Side == 0 && battle.DeployedIds != null && battle.DeployedIds.Any(id =>
            state.Characters.Any(c => c.Id == id && ReferenceEquals(c.Unit, a.Unit)))).ToArray();
        bool companyRetreatedOrLost = deployedCompanyActors.Length > 0 && deployedCompanyActors.All(a =>
            a.Fled || a.Unit.Downed || a.Unit.Digested || a.Unit.IsDead);
        Unit bossUnit = contract == null || string.IsNullOrEmpty(contract.BossCharacterId) ? null :
            state.Characters.Single(c => c.Id == contract.BossCharacterId).Unit;
        bool bossRouted = contract != null && contract.Objective == CompanyContractObjective.RoutBoss && bossUnit != null &&
            actors.Any(a => ReferenceEquals(a.Unit, bossUnit) && (a.Unit.Downed || a.Unit.Digested || a.Unit.IsDead || a.Fled));
        var enemyActors = actors.Where(a => enemyIds.Any(id => state.Characters.Single(c => c.Id == id).Unit == a.Unit)).ToArray();
        int defeated = 0;
        foreach (var a in enemyActors.Where(a => a.Unit.Digested || a.Fled || a.Unit.IsDead))
        {
            string id = state.Characters.Single(c => c.Unit == a.Unit).Id;
            if (!defeatedIds.Contains(id)) { defeatedIds.Add(id); defeated++; }
        }
        // Routing the boss completes the contract, but does not change other enemies' battle outcomes.
        if (bossRouted && !defeatedIds.Contains(contract.BossCharacterId))
        { defeatedIds.Add(contract.BossCharacterId); defeated++; }
        int temporaryEnemies = actors.Where(a => a.Unit.Side == 1 && (a.Unit.IsDead || a.Fled) && !enemyActors.Contains(a)).Select(a => a.Unit).Distinct().Count();
        result.EnemiesDefeated += defeated + temporaryEnemies;
        bool leaderLost = result.Characters.Single(c => c.Id == result.LeaderId).Unit.Digested;
        var losses = result.Members.Select(m => result.Characters.Single(c => c.Id == m.CharacterId)).Where(c => c.Unit.Digested && c.Id != result.LeaderId).ToArray();
        foreach (var c in losses)
        {
            if (!result.Memorial.Any(m => m.CharacterId == c.Id)) result.Memorial.Add(new CompanyMemorial
            { CharacterId = c.Id, Name = c.Unit.Name, Species = SpeciesPolicy.DisplayName(c.Unit.Race), Day = result.ElapsedDays + 1,
                Fate = "Digested by " + (c.Unit.KilledBy == null ? "an opponent" : c.Unit.KilledBy.Name + " (" + SpeciesPolicy.DisplayName(c.Unit.KilledBy.Race) + ")") });
            result.Members.RemoveAll(m => m.CharacterId == c.Id); c.Employment = CompanyEmployment.Unavailable;
            foreach (var item in c.Unit.Items.Where(i => i != null)) result.Inventory.Remove(item);
        }
        foreach (var c in result.Characters)
        {
            if (battle.DeployedIds.Contains(c.Id) || enemyIds.Contains(c.Id))
            {
                c.Unit.StatusEffects.Clear(); c.Unit.SetBarrier(0);
                c.Unit.TempBoosts.ResetAll(); c.Unit.CurrentLeader = null; c.Unit.SetSizeToDefault(); c.Unit.FinishCombat();
            }
        }
        bool paid = contract != null && CompanyContracts.Complete(result, contract);
        if (paid) CompanyProgression.AwardContract(result, contract);
        bool companyAlive = result.Members.Any(m => !result.Characters.Single(c => c.Id == m.CharacterId).Unit.Digested);
        if (leaderLost) result.LeaderDigestions = checked(result.LeaderDigestions + 1);
        result.Ended = !companyAlive || (leaderLost && !result.LeaderRevival);
        result.EndReason = result.Ended ? (!companyAlive ? "The company has been wiped out." : "The company leader was digested with revival disabled.") : null;
        result.EndingKind = result.Ended ? (!companyAlive ? CompanyEndingKind.Wipeout : CompanyEndingKind.LeaderLost) : null;
        result.EndingId = result.Ended ? Guid.NewGuid().ToString("N") : null;
        if (contract == null)
        {
            result.RoadEvent.Phase = CompanyRoadEventPhase.Closed;
            // Random combat keeps its ordinary combat XP; it grants no contract payment, reputation or completion bonus.
            if (Survivors(result, battle).Any()) result.Position = battle.ReturnTile;
        }
        else if (!paid || (bossRouted && companyRetreatedOrLost)) result.Position = battle.ReturnTile;
        if (!result.Ended)
        {
            battle.PostBattleEvent = CompanyPostBattleTraits.Resolve(state, result, actors);
            battle.GrudgeEvent = CompanyPostBattleTraits.ResolveLeaderGloryGrudge(state, result, actors);
        }
        battle.Summary = (stalemate ? "Stalemate: everyone remaining fled and survived.\n" : "Battle settled.\n") +
            "Enemies defeated: " + (defeated + temporaryEnemies) + "\nMercenaries lost: " + losses.Length + "\n" +
            (contract == null ? "Road encounter settled. No gold or reputation awarded.\n" : paid ?
                (contract.Objective == CompanyContractObjective.RoutBoss ? "Boss routed. Contract completed: " : "Contract completed: ") +
                contract.Reward + " gold paid automatically; reputation +1. Surviving participants gained contract XP.\n" : "The surviving target remains available before its deadline.\n") +
            (leaderLost && !result.Ended ? "The leader requires revival after these results.\n" : "") + result.EndReason;
        if (paid) CompanyContracts.ReleaseCompletedBossForce(result, contract);
        battle.Tactical = null; battle.RandomState = null;
        battle.Phase = CompanyBattlePhase.Summary; result.InBattle = false; result.Validate();
        if (result.Ended) CompanyHistoryStore.ArchiveEnding(result);
        finished = true; view.QueueBattleReturn(result, this);
    }
    internal static void Acknowledge(CompanyCampaign state)
    {
        if (state.Battle.Phase != CompanyBattlePhase.Summary) throw new InvalidOperationException("No battle summary is pending.");
        var leader = state.Characters.Single(c => c.Id == state.LeaderId);
        state.Battle.Phase = !state.Ended && leader.Unit.Digested ? CompanyBattlePhase.Revival :
            state.Battle.PostBattleEvent != null ? CompanyBattlePhase.PostBattleEvent :
            state.Battle.GrudgeEvent != null ? CompanyBattlePhase.PostBattleGrudgeEvent : CompanyBattlePhase.Returned;
    }
    internal static void AcknowledgePostBattle(CompanyCampaign state)
    {
        if (state.Battle == null || state.Battle.Phase != CompanyBattlePhase.PostBattleEvent || state.Battle.PostBattleEvent == null)
            throw new InvalidOperationException("No post-battle event is pending.");
        state.Battle.Phase = state.Battle.GrudgeEvent == null ? CompanyBattlePhase.Returned : CompanyBattlePhase.PostBattleGrudgeEvent;
    }
    internal static void AcknowledgeGrudge(CompanyCampaign state)
    {
        if (state.Battle == null || state.Battle.Phase != CompanyBattlePhase.PostBattleGrudgeEvent || state.Battle.GrudgeEvent == null)
            throw new InvalidOperationException("No post-battle Grudge event is pending.");
        state.Battle.Phase = CompanyBattlePhase.Returned;
    }
    internal static int RevivalCost(CompanyCampaign state) { return 20 * state.Characters.Single(c => c.Id == state.LeaderId).Unit.Level; }
    internal static CompanyTown RevivalTown(CompanyCampaign state)
    { return state.Towns.OrderBy(t => CompanyContracts.Distance(state, state.Position, t.Tile)).ThenBy(t => t.Id).First(); }
    internal static void Revive(CompanyCampaign state)
    {
        if (state.Ended || state.Battle.Phase != CompanyBattlePhase.Revival) throw new InvalidOperationException("No revival is pending.");
        var leader = state.Characters.Single(c => c.Id == state.LeaderId).Unit;
        var town = RevivalTown(state); state.Gold = Math.Max(0, state.Gold - RevivalCost(state)); state.Position = town.Tile;
        state.Reveal(); leader.Digested = false; leader.BattleActive = false; leader.Downed = false;
        leader.TemporaryHealth = 0; leader.Health = leader.MaxHealth; leader.StatusEffects.Clear(); leader.Injuries.Clear();
        state.Battle.Phase = state.Battle.PostBattleEvent != null ? CompanyBattlePhase.PostBattleEvent :
            state.Battle.GrudgeEvent != null ? CompanyBattlePhase.PostBattleGrudgeEvent : CompanyBattlePhase.Returned;
        CompanyEconomy.Visit(state, town); // No elapsed days, wages or deadlines advance.
    }
}

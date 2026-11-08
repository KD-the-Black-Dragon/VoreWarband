// Modified for the VoreWar mercenary-company conversion.
// 2026-11-06: Consider available spell alternatives and valid ally targets (spec 40C).
// Modified for the VoreWar mercenary-company conversion.
// 2026-11-05: Use the shared downed-prey capacity rule in prey selection and cleanup planning.
// See CHANGES.md and License.txt for GPL-3.0 terms.
// Modified for the VoreWar mercenary-company conversion.
// 2026-11-04: Account for equipment armor in tactical previews and planning (spec 34 slice 4).
// 2026-11-04: Correct caster targeting and share side-effect-free spell damage estimates (spec 34 slice 1).
// 2026-10-30: 04B2: Replace retreat flavor with a functional objective notice.
// 2026-10-31: Retire obsolete Resurrection AI action and planning calls (spec 05A3B9).
// 2026-10-31: Retire obsolete Reanimate and Bind actions/planning (spec 05A3B12).
// 2026-11-01: Keep downed opponents off direct attack lists; retain consumption cleanup and normal battle AI (spec 10).
// 2026-11-02: Restore vore terminology in the existing tactical retreat log (Options feedback).
// 2026-11-03: Remove permanently retired identity branches/entries while preserving shared behavior (spec 21).
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
// 2026-11-03: Implement four active stats and agreed combat rules (spec 29).
// 2026-11-04: Reassess reaction-aware routes, same-turn charges and interrupted movement actions (spec 30).
// 2026-11-04: Preserve the final AP for reachable melee attacks and execute completed plans before ranged replanning (spec 30 feedback).
using OdinSerializer;
using System;
using System.Collections.Generic;
using System.Linq;
using static UnityEngine.UI.CanvasScaler;


public abstract class TacticalAI : ITacticalAI
{

    public class RetreatConditions
    {
        [OdinSerialize]
        protected internal float MinPowerRatio;
        [OdinSerialize]
        protected internal int targetsEaten;
        [OdinSerialize]
        protected internal float acceptableLossRatio;

        public RetreatConditions(float minPowerRatio, int targetsEaten, float lossRatio = 0f)
        {
            MinPowerRatio = minPowerRatio;
            this.targetsEaten = targetsEaten;
            this.acceptableLossRatio = lossRatio;
        }
    }

    protected class PotentialTarget
    {
        internal Actor_Unit actor;
        internal float chance;
        internal int distance;
        internal float utility;

        public PotentialTarget(Actor_Unit actor, float chance, int distance, int damage, float setUtility = 0)
        {
            this.actor = actor;
            this.chance = chance;
            this.distance = distance;
            if (setUtility != 0)
                utility = setUtility;
            else
                utility = chance / Math.Max(actor.Unit.Health / damage, 1);
        }
    }

    protected bool onlySurrenderedEnemies;
    protected bool lackPredators;
    protected bool onlyForeignTroopsLeft;

    protected bool didAction;
    protected bool foundPath;
    [OdinSerialize]
    protected List<Actor_Unit> actors;
    [OdinSerialize]
    protected int enemySide;
    [OdinSerialize]
    protected readonly TacticalTileType[,] tiles;
    [OdinSerialize]
    protected readonly int AISide;
    [OdinSerialize]
    protected int targetsEaten;
    [OdinSerialize]
    public RetreatConditions retreatPlan;
    [OdinSerialize]
    protected bool retreating;
    [OdinSerialize]
    protected readonly bool defendingVillage;
    [OdinSerialize]
    protected int currentTurn = 0;
    [OdinSerialize]
    public bool foreignTurn;

    bool ITacticalAI.ForeignTurn
    {
        get
        {
            return foreignTurn;
        }
        set => foreignTurn = value;
    }

    protected AIPlottedPath path;


    RetreatConditions ITacticalAI.RetreatPlan
    {
        get { return retreatPlan; }
        set
        {
            retreatPlan = value;
        }
    }

    public TacticalAI(List<Actor_Unit> actors, TacticalTileType[,] tiles, int AISide, bool defendingVillage = false)
    {
        this.AISide = AISide;
        this.tiles = tiles;
        this.actors = actors;
        this.defendingVillage = defendingVillage;
        enemySide = State.GameManager.TacticalMode.GetAttackerSide() == AISide ? State.GameManager.TacticalMode.GetDefenderSide() : State.GameManager.TacticalMode.GetAttackerSide();
    }
    public void TurnAI()
    {
        if (actors == null)
            actors = TacticalUtilities.Units;
        path = null;
        var actorsThatMatter = actors.Where(a => a.Targetable && a.Unit.IsDead == false && a.Surrendered == false && a.Unit.Side == AISide);
        onlyForeignTroopsLeft = actorsThatMatter.All(a => TacticalUtilities.GetMindControlSide(a.Unit) == -1 && TacticalUtilities.GetPreferredSide(a.Unit, enemySide, AISide) == enemySide);
        onlySurrenderedEnemies = actors.Where(s => s.Unit.Side != AISide && s.Unit.IsDead == false && s.Surrendered == false && !s.Fled).Any() == false;
        var preds = actors.Where(s => s.Unit.Side == AISide && s.Unit.IsDead == false && s.Unit.Predator);
        lackPredators = preds.Any() == false;
        bool tooBig = true;
        if (onlySurrenderedEnemies)
        {
            //Array of all opposing live units
            var enemies = actors.Where(s => s.Unit.Side != AISide && s.Unit.IsDead == false);
            foreach (var actor in preds)
            {
                if (tooBig == false)
                    break;
                foreach (var target in enemies)
                {
                    if ((actor.PredatorComponent.HasCapacityFor(target) || actor.PredatorComponent.TotalCapacity() >= target.Bulk()))
                    {
                        tooBig = false;
                        break;
                    }
                }
            }
            lackPredators = tooBig; //Attack if no units can ever eat an enemy unit.
        }

        if (retreatPlan != null && currentTurn >= 4)
        {
            if (currentTurn >= 8 && retreatPlan.targetsEaten > 0 && retreatPlan.targetsEaten <= targetsEaten && onlySurrenderedEnemies == false)
            {
                if (retreating == false)
                {
                    State.GameManager.TacticalMode.Log.RegisterMiscellaneous($"<color=orange>{(actors[0].Unit.Side == AISide ? "Attackers" : "Defenders")} are retreating (vore objective met)</color>");
                }
                retreating = true;
            }
            else if (retreatPlan.MinPowerRatio > 0.0001)
            {
                double friendlyPower = StrategicUtilities.ArmyPower(actors.Where(s => s.Unit.Side == AISide && s.Unit.IsDead == false && s.Surrendered == false).Select(s => s.Unit).ToList());
                double enemyPower = StrategicUtilities.ArmyPower(actors.Where(s => s.Unit.Side != AISide && s.Unit.IsDead == false && s.Surrendered == false).Select(s => s.Unit).ToList());

                if (defendingVillage)
                    friendlyPower *= 2;

                if ((enemyPower > 0) && (friendlyPower / enemyPower) < retreatPlan.MinPowerRatio)
                {
                    if (retreating == false)
                    {
                        State.GameManager.TacticalMode.Log.RegisterMiscellaneous($"<color=orange>{(actors[0].Unit.Side == AISide ? "Attackers" : "Defenders")} are now fleeing because they are significantly outmatched</color>");
                    }
                    retreating = true;
                }
                else
                {
                    if (retreating && (enemyPower > 0) && (friendlyPower / enemyPower) > 1.2f * retreatPlan.MinPowerRatio && !onlyForeignTroopsLeft)
                    {
                        State.GameManager.TacticalMode.Log.RegisterMiscellaneous($"<color=orange>{(actors[0].Unit.Side == AISide ? "Attackers" : "Defenders")} are no longer fleeing</color>");
                        retreating = false;
                    }
                }
            }
            else if (retreatPlan.acceptableLossRatio > 0.0001) // Made because it somewhat considers how the battle is developing instead of just a snapshot metric
            {
                bool aIisAttacker = actors[0].Unit.Side == AISide;

                var friendlies = actors.Where(s => s.Unit.Side == AISide && s.Unit.IsDead == false && s.Surrendered == false).Select(s => s.Unit).ToList();
                var enemies = actors.Where(s => s.Unit.Side != AISide && s.Unit.IsDead == false && s.Surrendered == false).Select(s => s.Unit).ToList();

                double friendlyPower = 0d;
                double enemyPower = 0d;

                actors.ForEach(actor =>
                {
                    if (AISide == actor.Unit.Side)
                    {
                        friendlyPower += actor.Unit.HealthPct * StrategicUtilities.ArmyPower(new List<Unit> { actor.Unit });  // More accurately than an average health pct, this correctly considers health loss on fodder units as less important
                    }
                    else
                    {
                        enemyPower += actor.Unit.HealthPct * StrategicUtilities.ArmyPower(new List<Unit> { actor.Unit });
                    }
                });

                double friendLoss;
                double enemyLoss;

                // This fails to accurately consider units that already started with less than full health, but I find that worth it in exchange for recognizing the peril in having many half digested and wounded units
                // Also viable retreat plan for 1 man armies and somesuch 
                // (Would go great with dragon monster packs, now that I think about it) (((they lowkey need their own AI where the kobolds serve and rub them and the dragons sometimes devour them for heals OwO)))
                if (aIisAttacker)
                {
                    enemyLoss = State.GameManager.TacticalMode.StartingDefenderPower - enemyPower * enemies.Count;
                    friendLoss = State.GameManager.TacticalMode.StartingAttackerPower - friendlyPower * friendlies.Count;
                }
                else
                {
                    friendLoss = State.GameManager.TacticalMode.StartingDefenderPower - friendlyPower * friendlies.Count;
                    enemyLoss = State.GameManager.TacticalMode.StartingAttackerPower - enemyPower * enemies.Count;
                }

                if ((enemyLoss > 0) && (friendLoss / enemyLoss) > retreatPlan.acceptableLossRatio || (enemyLoss <= 0 && friendLoss > 0))
                {
                    if (retreating == false)
                    {
                        State.GameManager.TacticalMode.Log.RegisterMiscellaneous($"<color=orange>{(aIisAttacker ? "Attackers" : "Defenders")} are now fleeing because the battle isn't developing favorably enough</color>");
                    }
                    retreating = true;
                }
                else
                {
                    if (retreating && (enemyLoss > 0) && (friendLoss / enemyLoss) < 0.8f * retreatPlan.acceptableLossRatio && !onlyForeignTroopsLeft)
                    {
                        State.GameManager.TacticalMode.Log.RegisterMiscellaneous($"<color=orange>{(aIisAttacker ? "Attackers" : "Defenders")} are no longer fleeing</color>");
                        retreating = false;
                    }
                }
            }
        }
    }

    public virtual bool RunAI()
    {
        if (actors == null)
            actors = TacticalUtilities.Units;
        if (currentTurn != State.GameManager.TacticalMode.currentTurn)
        {
            TurnAI();
            currentTurn = State.GameManager.TacticalMode.currentTurn;
        }
        if (path != null && !OpportunityAttacks.CanAct(path.Actor)) path = null;
        foreach (Actor_Unit actor in actors)
        {
            if (actor.Unit.Side == AISide && !OpportunityAttacks.CanAct(actor))
            {
                actor.ClearMovement();
                continue;
            }
            if (State.World.IsNight)
            {
                int unitSightRange = Config.DefualtTacticalSightRange + actor.Unit.TraitBoosts.SightRangeBoost;
                foreach (var seenUnit in TacticalUtilities.UnitsWithinTiles(actor.Position, unitSightRange).Where(s => TacticalUtilities.TreatAsHostile(actor, s)))
                {
                    seenUnit.InSight = true;
                }
            }
            if (actor.Targetable == true && actor.Unit.Side == AISide && (foreignTurn ? !TacticalUtilities.IsUnitControlledByPlayer(actor.Unit) : true) && actor.Movement > 0)
            {
                if (TacticalUtilities.IsUnitControlledByPlayer(actor.Unit) && State.GameManager.TacticalMode.RunningFriendlyAI == false && !State.GameManager.TacticalMode.IgnorePseudo && !State.GameManager.TacticalMode.turboMode)
                {
                    if (State.GameManager.TacticalMode.SkipPseudo)
                    {
                        actor.Movement = 0;
                        return true;
                    }
                    State.GameManager.TacticalMode.PseudoTurn = true;
                    State.GameManager.TacticalMode.StatusUI.EndTurn.interactable = true;
                    return true;
                }
                State.GameManager.TacticalMode.PseudoTurn = false;
                if (path != null && path.Actor == actor)
                {
                    if (retreating && actor.Movement == 1 && TacticalUtilities.TileContainsMoreThanOneUnit(actor.Position.x, actor.Position.y) == false)
                    {
                        FightWithoutMoving(actor);
                        if (actor.Movement == 0)
                            return true;
                    }
                    if (path.Path.Count == 0)
                    {
                        path.Action?.Invoke();
                        if (path.Action == null)
                            actor.Movement = 0;
                        path = null;
                        continue;
                    }
                    Vec2i newLoc = new Vec2i(path.Path[0].X, path.Path[0].Y);
                    if (actor.Movement == 1 && TacticalUtilities.OpenTile(newLoc.x, newLoc.y, actor) == false)
                    {
                        actor.Movement = 0;
                        return true;
                    }

                    // Reassess the remaining route after every step/reaction. Never
                    // execute a destination action just because movement was blocked.
                    if (!State.GameManager.TacticalMode.Opportunities.Busy && !retreating && !(path.HazardEscape ? HazardEscapeAllowed(actor, path.Path) : OpportunityAttacks.RouteAllowed(actor, path.Path, path.ChargeTarget)))
                    {
                        path = null;
                        return true;
                    }
                    if (actor.MoveTo(newLoc, tiles, (State.GameManager.TacticalMode.RunningFriendlyAI ? Config.TacticalFriendlyAIMovementDelay : Config.TacticalAIMovementDelay)) == false)
                    {
                        if (State.GameManager.TacticalMode.Opportunities.Busy) return true;
                        path = null;
                        return true;
                    }
                    path.Path.RemoveAt(0);
                    if (path.Path.Count == 0 || actor.Movement == 0)
                    {
                        if (path.Path.Count == 0 && actor.Movement > 0 && OpportunityAttacks.CanAct(actor)) path.Action?.Invoke();
                        if (path.Action == null)
                            actor.Movement = 0;
                        path = null;
                    }
                    else if (actor.Movement == 1 && IsRanged(actor) && TacticalUtilities.TileContainsMoreThanOneUnit(actor.Position.x, actor.Position.y) == false)
                    {
                        path = null;
                    }
                    return true;
                }
                else
                {
                    if (onlyForeignTroopsLeft)
                        HandleLeftoverForeigns(actor);
                    if (!retreating && TryLeaveHazard(actor)) return true;
                    if (!retreating && this is StandardTacticalAI && TryOpportunityCharge(actor)) return true;
                    GetNewOrder(actor);
                    return true; 
                }
            }
        }

        return false;
    }

    protected abstract void GetNewOrder(Actor_Unit actor);

    protected virtual int CheckActionEconomyOfActorFromPositionWithAP(Actor_Unit actor, Vec2i position, int ap)
    {
        int apRequired = -1;
        if (actor.Unit.GetStatusEffect(StatusEffectType.Temptation) != null)
        {
            apRequired = CheckForceFeed(actor, position, ap);
            if (apRequired > 0)
                return ap - apRequired;
        }
        if (actor.Unit.HasTrait(Traits.Pounce) && ap >= 2)
        {
            apRequired = CheckVorePounce(actor, position, ap);
            if (apRequired > 0)
                return ap - apRequired;
        }
        apRequired = CheckPred(actor, position, ap);
        if (apRequired > 0)
            return ap - apRequired;




        if (State.Rand.Next(2) == 0 || actor.Unit.HasWeapon == false)
            apRequired = CheckSpells(actor, position, ap);
        if (apRequired > 0)
            return ap - apRequired;

        if (actor.Unit.HasTrait(Traits.Pounce) && ap >= 2)
        {
            apRequired = CheckMeleePounce(actor, position, ap);
            if (apRequired > 0)
                return ap - apRequired;
        }
        if (IsRanged(actor))
            apRequired = CheckRanged(actor, position, ap);
        else
            apRequired = CheckMelee(actor, position, ap);
        if (apRequired > 0)
            return ap - apRequired;
        // Everything else is less important than belly rubs.
        return ap;
    }

    protected virtual int CheckForceFeed(Actor_Unit actor, Vec2i position, int ap)
    {
        StatusEffect temptation = actor.Unit.GetStatusEffect(StatusEffectType.Temptation);
        List<PotentialTarget> targets = new List<PotentialTarget>();

        foreach (Actor_Unit unit in actors)
        {
            if (unit.Targetable == true && unit.Unit.Predator && unit.Unit.FixedSide == temptation.Strength && TacticalUtilities.GetMindControlSide(unit.Unit) == -1 && !unit.Surrendered)
            {
                int distance = unit.Position.GetNumberOfMovesDistance(position);
                if (distance < ap)
                {
                    if (distance > 1 && TacticalUtilities.FreeSpaceAroundTarget(unit.Position, actor) == false)
                        continue;
                targets.Add(new PotentialTarget(unit, 100, distance, 4, -distance));
                }

            }
        }
        targets = targets.OrderByDescending(t => t.utility).ToList();

        if (!targets.Any())
            return -1;
        Actor_Unit reserveTarget = targets[0].actor;
        while (targets.Any())
        {
            if (targets[0].distance < 2)
            {
                return 1;
            }
            else
            {
                if (targets[0].actor.Position.GetNumberOfMovesDistance(position) < ap) //discard the clearly impossible
                {
                    int distance = CheckMoveTo(actor, position, targets[0].actor.Position, 1, ap);
                    if (distance < ap && distance >= 0)
                        return distance + 1;
                }
            }
            targets.RemoveAt(0);
        }

        return -1;
    }

    protected virtual void RunForceFeed(Actor_Unit actor)
    {
        StatusEffect temptation = actor.Unit.GetStatusEffect(StatusEffectType.Temptation);
        List<PotentialTarget> targets = new List<PotentialTarget>();

        foreach (Actor_Unit unit in actors)
        {
            if (unit.Targetable == true && unit.Unit.Predator && unit.Unit.FixedSide == temptation.Strength && TacticalUtilities.GetMindControlSide(unit.Unit) == -1 && !unit.Surrendered)
            {
                int distance = unit.Position.GetNumberOfMovesDistance(actor.Position);
                if (distance < actor.Movement)
                {
                    if (distance > 1 && TacticalUtilities.FreeSpaceAroundTarget(unit.Position, actor) == false)
                        continue;
                    targets.Add(new PotentialTarget(unit, 100, distance, 4, -distance));
                }

            }
        }
        targets = targets.OrderByDescending(t => t.utility).ToList();

        if (!targets.Any())
            return;
        Actor_Unit reserveTarget = targets[0].actor;
        while (targets.Any())
        {
            if (targets[0].distance < 2)
            {
                TacticalUtilities.ForceFeed(actor, targets[0].actor);
                didAction = true;
                break;
            }
            else
            {
                if (targets[0].actor.Position.GetNumberOfMovesDistance(actor.Position) < actor.Movement) //discard the clearly impossible
                {
                    MoveToAndAction(actor, targets[0].actor.Position, 1, actor.Movement, () => TacticalUtilities.ForceFeed(actor, targets[0].actor));
                    if (foundPath && path.Path.Count() < actor.Movement)
                        return;
                }
            }
            targets.RemoveAt(0);
        }
        if (didAction == false)
        {
            if (reserveTarget != null)
            {
                //Get as close to the target as you can if you can't reach it
                MoveToAndAction(actor, reserveTarget.Position, -1, 999, null);
                if (foundPath)
                    return;
                RandomWalkAndEndTurn(actor);
            }
            else
            {
                RandomWalkAndEndTurn(actor);
            }
        }
    }

    protected virtual int CheckVorePounce(Actor_Unit actor, Vec2i position, int ap)
    {
        if (!actor.Unit.Predator)
            return -1;
        List<PotentialTarget> targets = GetListOfPotentialVorePouncePrey(actor, position, ap);
        if (!targets.Any())
            return -1;
        Actor_Unit reserveTarget = targets[0].actor;
        while (targets.Any())
        {
            if (targets[0].distance == 1)
            {
                return 1;
            }
            if (targets[0].distance <= 4 && targets[0].distance > 1)
            {
                if (ap >= 2)
                {
                    return 2;
                }
            }
            int distance = CheckMoveTo(actor, position, targets[0].actor.Position, 4, 2 + ap);
            if (distance + 1 < ap && distance >= 0)
                return distance + 2;
            targets.RemoveAt(0);
        }
        return -1;
    }

    protected virtual void RunBellyRub(Actor_Unit actor, int spareAP)
    {
        int cost = Math.Max(1, actor.MaxMovement() / 3);
        List<PotentialTarget> targets = GetListOfPotentialRubTargets(actor, actor.Position, spareAP);

        if (!targets.Any())
        {
            return;
        }


        // no looping for now, due to gameplay concerns
        //while (spareAP > 0)
        //{
        while (targets.Any())
        {
            if (targets[0].distance < 2)
            {
                int before = actor.Movement;
                actor.BellyRub(targets[0].actor);
                if (actor.Movement == before) // The issue that caused this was fixed, so it's now a failsafe that should never actually run.
                {
                    targets.RemoveAt(0);
                    continue;
                }
                didAction = true;
                return;
                //spareAP -= cost;
                //targets.RemoveAt(0);
                //break;
            }
            else
            {
                // at this point we know that we have enough ap for both the distance to an enemy and doing something useful, AND for the distance to an ally and bellyrub, from where we stand. 
                // But could they still reach an enemy after changing position to be adjacent to ally?
                var rubPath = TacticalPathfinder.GetPath(actor.Position, targets[0].actor.Position, 1, actor, spareAP);
                if (rubPath != null && rubPath.Count > 0)
                {
                    PathNode destination = rubPath.Last();
                    var nextToAlly = new Vec2i(destination.X, destination.Y);
                    if (CheckActionEconomyOfActorFromPositionWithAP(actor, nextToAlly, actor.Movement - (rubPath.Count + cost)) >= 0)
                    {
                        MoveToAndAction(actor, nextToAlly, 0, spareAP, () => actor.BellyRub(targets[0].actor));
                        return;
                    }

                }

            }
            targets.RemoveAt(0);
        }
        return;
        //}
    }

    protected virtual List<PotentialTarget> GetListOfPotentialRubTargets(Actor_Unit actor, Vec2i position, int moves)
    {
        List<PotentialTarget> targets = new List<PotentialTarget>();

        foreach (Actor_Unit unit in actors)
        {
            if (unit.Targetable == true && unit.Unit.Predator && !TacticalUtilities.TreatAsHostile(actor, unit) && TacticalUtilities.GetMindControlSide(unit.Unit) == -1 && !unit.Surrendered && unit.PredatorComponent?.PreyCount > 0 && !unit.ReceivedRub) // includes self
            {
                int distance = unit.Position.GetNumberOfMovesDistance(position);
                if (distance - 1 + (Math.Max(1, actor.MaxMovement() / 3)) <= moves)
                {
                    if (distance > 1 && TacticalUtilities.FreeSpaceAroundTarget(unit.Position, actor) == false)
                        continue;
                    if (unit.PredatorComponent.CanFeed() || unit.PredatorComponent.CanFeedCum())
                        continue;
                    targets.Add(new PotentialTarget(unit, 100, distance, 4, 100 - (unit == actor ? 100 - unit.Unit.HealthPct + 10 : 100 - unit.Unit.HealthPct))); // self is weighted a little lower than the rest
                }

            }
        }
        return targets.OrderByDescending(t => t.utility).ToList();
    }

    protected virtual void FightWithoutMoving(Actor_Unit actor)
    {
        List<PotentialTarget> targets;
        if (actor.Unit.Predator)
        {
            targets = GetListOfPotentialPrey(actor, false, actor.Position, actor.Movement);
            while (targets.Any())
            {
                if (targets[0].distance < 2)
                {
                    if (actor.PredatorComponent.UsePreferredVore(targets[0].actor))
                        targetsEaten++;
                    didAction = true;
                    break;
                }
                targets.RemoveAt(0);
            }
        }
        if (didAction == false && IsRanged(actor))
        {
            targets = GetListOfPotentialRangedTargets(actor, actor.Position);
            while (targets.Any())
            {
                if (targets[0].distance <= actor.BestRanged.Range && targets[0].distance > 1)
                {
                    actor.Attack(targets[0].actor, true);
                    didAction = true;
                    break;
                }
                targets.RemoveAt(0);
            }
        }
        if (didAction == false)
        {
            targets = GetListOfPotentialMeleeTargets(actor, actor.Position, actor.Movement);
            while (targets.Any())
            {
                if (targets[0].distance < 2)
                {
                    actor.Attack(targets[0].actor, false);
                    didAction = true;
                    return;
                }
                targets.RemoveAt(0);
            }
        }
    }

    protected virtual int CheckPred(Actor_Unit actor, Vec2i position, int ap, bool anyDistance = false)
    {
        int distance = -1;
        if (!actor.Unit.Predator)
            return -1;
        List<PotentialTarget> targets = GetListOfPotentialPrey(actor, anyDistance, position, ap);
        if (!targets.Any())
            return -1;

        while (targets.Any())
        {
            if (targets[0].distance < 2)
            {
                return 1;
            }
            else
            {
                if (actor.Unit.HasTrait(Traits.RangedVore))
                {
                    distance = CheckMoveTo(actor, position, targets[0].actor.Position, 1, ap);
                    if (distance < ap && distance >= 0)
                        return distance + 1;
                    distance = CheckMoveTo(actor, position, targets[0].actor.Position, 4, ap);
                    if (distance < ap && distance >= 0)
                        return distance + 1;
                }
                else
                    distance = CheckMoveTo(actor, position, targets[0].actor.Position, 1, ap);
                if (distance < ap && distance >= 0)
                    return distance + 1;
            }
            targets.RemoveAt(0);
        }
        return -1;
    }

    protected virtual void RunPred(Actor_Unit actor, bool anyDistance = false)
    {

        if (actor.Unit.Predator == false)
            return;
        List<PotentialTarget> targets = GetListOfPotentialPrey(actor, anyDistance, actor.Position, actor.Movement);
        if (!targets.Any())
            return;

        while (targets.Any())
        {
            if (targets[0].distance < 2)
            {
                if (actor.PredatorComponent.UsePreferredVore(targets[0].actor))
                    targetsEaten++;
                didAction = true;
                break;
            }
            else
            {
                if (actor.Unit.HasTrait(Traits.RangedVore))
                {
                    MoveToAndAction(actor, targets[0].actor.Position, 1, 999, () =>
                    {
                        if (actor.PredatorComponent.UsePreferredVore(targets[0].actor))
                            targetsEaten++;
                    }); //If anydistance is off, this will already be limited to the units move radius
                    if (foundPath && path.Path.Count() < actor.Movement)
                        break;
                    MoveToAndAction(actor, targets[0].actor.Position, 4, 999, () =>
                    {
                        if (actor.PredatorComponent.UsePreferredVore(targets[0].actor))
                            targetsEaten++;
                    }); //If anydistance is off, this will already be limited to the units move radius                                      
                }
                else
                    MoveToAndAction(actor, targets[0].actor.Position, 1, 999, () =>
                    {
                        if (actor.PredatorComponent.UsePreferredVore(targets[0].actor))
                            targetsEaten++;
                    }); //If anydistance is off, this will already be limited to the units move radius
                if (foundPath && path.Path.Count() < actor.Movement)
                {
                    break;
                }
                else
                {
                    if (anyDistance)
                        break;
                    //If you can't get there in one turn, discard it
                    foundPath = false;
                    path = null;
                }
            }
            targets.RemoveAt(0);
        }
    }

    protected virtual List<PotentialTarget> GetListOfPotentialPrey(Actor_Unit actor, bool anyDistance, Vec2i position, int movement)
    {
        List<PotentialTarget> targets = new List<PotentialTarget>();
        if (State.GameManager.TacticalMode.IsOnlyOneSideVisible() && actor.Unit.IsInfiltratingSide(AISide)) // should prevent aggressive action while within a foreign army when the battle is basically over
            return targets;
        //check if we have at least 1 unit of capacity free
        float cap = actor.PredatorComponent.FreeCap();
        if (cap >= 1 || actor.PredatorComponent.PreyCount == 0)
        {
            foreach (Actor_Unit unit in actors)
            {

                if (unit.Targetable && (unit.InSight || !State.World.IsNight) && (TacticalUtilities.TreatAsHostile(actor, unit) || (unit.Unit.GetStatusEffect(StatusEffectType.Hypnotized)?.Duration < 3 && unit != actor)) && actor.PredatorComponent.HasCapacityFor(unit))
                {
                    int distance = unit.Position.GetNumberOfMovesDistance(position);
                    if (distance <= movement || anyDistance)
                    {
                        float chance = unit.GetDevourChance(actor, true);
                        if (((chance > .5f || (actor.Unit.HasTrait(Traits.Biter) && chance > .25f && actor.Unit.GetBestMelee().Damage > 2)) || actor.Unit.HasTrait(Traits.VoreObsession)) && unit.AIAvoidEat <= 0)
                        {
                            if (distance > 1 && TacticalUtilities.FreeSpaceAroundTarget(unit.Position, actor) == false)
                                continue;
                            if (unit.Unit.HasTrait(Traits.AcidImmunity)) //More interesting if mostly ignored
                                chance *= .5f;
                            targets.Add(new PotentialTarget(unit, chance, distance, 4, chance));
                        }
                    }
                }
            }
            PotentialTarget primeTarget = targets.Where(t => t.distance < 2).OrderByDescending(s => s.chance).FirstOrDefault();
            if (primeTarget != null)
                return new List<PotentialTarget>() { primeTarget };
            return targets.OrderByDescending(t => t.chance).ToList();
        }
        return targets;
    }

    protected virtual void WalkToYBand(Actor_Unit actor, int y)
    {
        var tempPath = TacticalPathfinder.GetPathToY(actor.Position, actor.Unit.HasTrait(Traits.Flight), y, actor);
        if (tempPath == null || tempPath.Count == 0)
            foundPath = false;
        else
        {
            foundPath = true;
            path = new AIPlottedPath
            {
                Actor = actor,
                Path = tempPath,
                Action = null
            };
        }
    }

    // Prefer a safe route. Risk is a utility check, never extra AP/terrain cost.
    protected virtual void MoveToAndAction(Actor_Unit actor, Vec2i p, int howClose, int maxDistance, Action action)
    {
        var tempPath = TacticalPathfinder.GetPath(actor.Position, p, howClose, actor, maxDistance, avoidOpportunity: true, avoidHazard: true);
        if (tempPath == null) tempPath = TacticalPathfinder.GetPath(actor.Position, p, howClose, actor, maxDistance, avoidOpportunity: true);
        Actor_Unit target = action == null ? null : actors.FirstOrDefault(u => u.Position.Matches(p) &&
            OpportunityAttacks.CanAct(u) && TacticalUtilities.TreatAsHostile(actor, u));
        int safeCost = tempPath == null ? int.MaxValue : tempPath.Sum(n => OpportunityAttacks.MoveCost(actor, n));
        if (tempPath == null || (target != null && safeCost >= actor.Movement))
        {
            var direct = TacticalPathfinder.GetPath(actor.Position, p, howClose, actor, maxDistance);
            if (direct != null && OpportunityAttacks.RouteAllowed(actor, direct, target)) tempPath = direct;
        }
        foundPath = tempPath != null && tempPath.Count > 0;
        if (foundPath) path = new AIPlottedPath { Actor = actor, Path = tempPath, Action = action, ChargeTarget = target };
    }

    bool HazardEscapeAllowed(Actor_Unit actor, IList<PathNode> route)
    {
        TileEffect hazard;
        if (!State.GameManager.TacticalMode.ActiveEffects.TryGetValue(new Vec2(actor.Position.x, actor.Position.y), out hazard) ||
            hazard.Type != TileEffectType.Fire || hazard.RemainingDuration <= 0 || route == null || route.Count == 0) return false;
        var destination = route[route.Count - 1];
        if (TacticalPathfinder.IsBurning(destination.X, destination.Y) ||
            route.Sum(node => OpportunityAttacks.MoveCost(actor, node)) > actor.Movement) return false;
        int cost;
        float risk = OpportunityAttacks.RouteRisk(actor, actor.Position, route, actor.Movement, out cost);
        return cost <= actor.Movement && risk < hazard.Strength * actor.Unit.TraitBoosts.FireDamageTaken * Math.Min(2, hazard.RemainingDuration);
    }

    bool TryLeaveHazard(Actor_Unit actor)
    {
        TileEffect hazard;
        if (!State.GameManager.TacticalMode.ActiveEffects.TryGetValue(new Vec2(actor.Position.x, actor.Position.y), out hazard) ||
            hazard.Type != TileEffectType.Fire || hazard.RemainingDuration <= 0) return false;
        float payoff = actors.Where(target => OpportunityAttacks.CanAct(target) && TacticalUtilities.TreatAsHostile(actor, target) &&
            target.Position.GetNumberOfMovesDistance(actor.Position) == 1)
            .Select(target => OpportunityAttacks.ExpectedDamage(actor, target)).DefaultIfEmpty(0).Max();
        float exposure = hazard.Strength * actor.Unit.TraitBoosts.FireDamageTaken * Math.Min(2, hazard.RemainingDuration);
        if (actor.Unit.Health > exposure && payoff >= exposure) return false;
        List<PathNode> best = null;
        float bestRisk = float.MaxValue;
        foreach (var destination in TacticalUtilities.TilesWithinRange(actor.Position, actor.Movement))
        {
            if (TacticalPathfinder.IsBurning(destination.x, destination.y) || !TacticalUtilities.OpenTile(destination, actor)) continue;
            var route = TacticalPathfinder.GetPath(actor.Position, destination, 0, actor, actor.Movement, avoidHazard: true);
            if (route == null) route = TacticalPathfinder.GetPath(actor.Position, destination, 0, actor, actor.Movement);
            if (route == null || route.Count == 0 || !HazardEscapeAllowed(actor, route)) continue;
            int cost;
            float risk = OpportunityAttacks.RouteRisk(actor, actor.Position, route, actor.Movement, out cost);
            if (risk + cost * .01f >= bestRisk) continue;
            best = route; bestRisk = risk + cost * .01f;
        }
        if (best == null) return false;
        path = new AIPlottedPath { Actor = actor, Path = best, HazardEscape = true, Action = () => { } };
        foundPath = true;
        return true;
    }

    bool TryOpportunityCharge(Actor_Unit actor)
    {
        if (!OpportunityAttacks.Threats(actor, actor.Position).Any() || actor.Movement < 2) return false;
        float currentPayoff = actors.Where(t => OpportunityAttacks.CanAct(t) &&
            TacticalUtilities.TreatAsHostile(actor, t) && t.Position.GetNumberOfMovesDistance(actor.Position) == 1)
            .Select(t => OpportunityAttacks.ExpectedDamage(actor, t)).DefaultIfEmpty(0).Max();
        AIPlottedPath best = null;
        float bestValue = currentPayoff;
        foreach (var target in actors.Where(t => OpportunityAttacks.CanAct(t) &&
            TacticalUtilities.TreatAsHostile(actor, t) && (t.InSight || !State.World.IsNight) &&
            OpportunityAttacks.IsPriorityTarget(t) && t.Position.GetNumberOfMovesDistance(actor.Position) > 1 &&
            t.Position.GetNumberOfMovesDistance(actor.Position) <= actor.Movement))
        {
            var route = TacticalPathfinder.GetPath(actor.Position, target.Position, 1, actor, actor.Movement);
            if (!OpportunityAttacks.CanAffordCharge(actor, target, route)) continue;
            int cost;
            float risk = OpportunityAttacks.RouteRisk(actor, actor.Position, route, int.MaxValue, out cost);
            var endpoint = route[route.Count - 1];
            TileEffect hazard;
            if (State.GameManager.TacticalMode.ActiveEffects.TryGetValue(new Vec2(endpoint.X, endpoint.Y), out hazard) &&
                hazard.Type == TileEffectType.Fire && hazard.RemainingDuration > 0)
                risk += hazard.Strength * actor.Unit.TraitBoosts.FireDamageTaken * Math.Min(2, hazard.RemainingDuration);
            float value = 1.75f * OpportunityAttacks.ExpectedDamage(actor, target) - risk;
            if (value <= bestValue) continue;
            bestValue = value;
            var captured = target;
            best = new AIPlottedPath { Actor = actor, Path = route, ChargeTarget = target,
                Action = () => { if (OpportunityAttacks.CanAct(captured) && TacticalUtilities.TreatAsHostile(actor, captured)) actor.Attack(captured, false); } };
        }
        if (best == null) return false;
        path = best;
        foundPath = true;
        return true;
    }

    protected virtual int CheckMoveTo(Actor_Unit actor, Vec2i actorPosition, Vec2i p, int howClose, int maxDistance)
    {
        var tempPath = TacticalPathfinder.GetPath(actorPosition, p, howClose, actor, maxDistance);
        if (tempPath == null || tempPath.Count == 0)
            return -1;
        else
        {
            return tempPath.Count;
        }
    }

    protected virtual void RandomWalkAndEndTurn(Actor_Unit actor)
    {
        RandomWalk(actor);
        actor.ClearMovement();
        didAction = true;
    }

    protected virtual bool RandomWalk(Actor_Unit actor)
    {

        // Aimless movement is not a reason to accept a reaction (or to kite).
        if (OpportunityAttacks.Threats(actor, actor.Position).Any()) return false;
        int r = State.Rand.Next(8);
        int d = 8;
        while (!actor.Move(r, tiles))
        {
            r++;
            d--;
            if (r > 7)
            {
                r = 0;
            }
            if (d < 1)
            {
                return false;
            }
        }
        didAction = true;
        return true;
    }

    protected virtual void RunVorePounce(Actor_Unit actor)
    {
        if (actor.Unit.Predator == false)
            return;
        List<PotentialTarget> targets = GetListOfPotentialVorePouncePrey(actor, actor.Position, actor.Movement);
        if (!targets.Any())
            return;
        Actor_Unit reserveTarget = targets[0].actor;
        while (targets.Any())
        {
            if (targets[0].distance == 1)
            {
                if (actor.PredatorComponent.UsePreferredVore(targets[0].actor))
                    targetsEaten++;
                didAction = true;
                break;
            }
            if (targets[0].distance <= 4 && targets[0].distance > 1)
            {
                actor.VorePounce(targets[0].actor, AIAutoPick: true);
                if (!targets[0].actor.Visible)
                {
                    targetsEaten++;
                }
                didAction = true;
                break;
            }
            MoveToAndAction(actor, targets[0].actor.Position, 4, 2 + actor.Movement, () =>
            {
                actor.VorePounce(targets[0].actor, AIAutoPick: true);
                if (!targets[0].actor.Visible)
                {
                    targetsEaten++;
                }
            });
            if (path != null)
                return;
            targets.RemoveAt(0);
        }
    }

    protected virtual List<PotentialTarget> GetListOfPotentialVorePouncePrey(Actor_Unit actor, Vec2i position, int moves)
    {
        List<PotentialTarget> targets = new List<PotentialTarget>();
        //check if we have at least 1 unit of capacity free
        float cap = actor.PredatorComponent.FreeCap();
        if ((cap >= 1 || actor.PredatorComponent.PreyCount == 0) && !(State.GameManager.TacticalMode.IsOnlyOneSideVisible() && actor.Unit.IsInfiltratingSide(AISide)))
        {
            foreach (Actor_Unit unit in actors)
            {

                if (unit.Targetable && (unit.InSight || !State.World.IsNight) && TacticalUtilities.TreatAsHostile(actor, unit) && actor.PredatorComponent.HasCapacityFor(unit) && TacticalUtilities.FreeSpaceAroundTarget(unit.Position, actor))
                {
                    int distance = unit.Position.GetNumberOfMovesDistance(position);
                    if (distance <= 2 + moves)
                    {
                        float chance = unit.GetDevourChance(actor, true);
                        if (chance > .5f)
                        {
                            targets.Add(new PotentialTarget(unit, chance, distance, 4, chance));
                        }
                    }
                }
            }
            PotentialTarget primeTarget = targets.Where(t => t.distance < 2).OrderByDescending(s => s.chance).FirstOrDefault();
            if (primeTarget != null)
                return new List<PotentialTarget>() { primeTarget };
            return targets.OrderByDescending(t => t.chance).ToList();
        }
        return targets;
    }

    protected virtual int CheckMeleePounce(Actor_Unit actor, Vec2i position, int ap)
    {
        List<PotentialTarget> targets = GetListOfPotentialPounceTargets(actor, position, ap);
        if (!targets.Any())
            return -1;
        Actor_Unit reserveTarget = targets[0].actor;
        while (targets.Any())
        {
            if (targets[0].distance == 1)
            {
                return 1;
            }
            if (targets[0].distance <= 4 && targets[0].distance > 1)
            {
                if (ap >= 2)
                {
                    return 2;
                }
            }
            int distance = CheckMoveTo(actor, position, targets[0].actor.Position, 4, 2 + ap);
            if (distance + 1 < ap && distance >= 0)
                return distance + 2;
            targets.RemoveAt(0);
        }
        return -1;
    }

    protected virtual void RunMeleePounce(Actor_Unit actor)
    {
        List<PotentialTarget> targets = GetListOfPotentialPounceTargets(actor, actor.Position, actor.Movement);
        if (!targets.Any())
            return;
        Actor_Unit reserveTarget = targets[0].actor;
        while (targets.Any())
        {
            if (targets[0].distance == 1)
            {
                actor.Attack(targets[0].actor, false);
                didAction = true;
                break;
            }
            if (targets[0].distance <= 4 && targets[0].distance > 1)
            {
                actor.MeleePounce(targets[0].actor);
                didAction = true;
                break;
            }
            MoveToAndAction(actor, targets[0].actor.Position, 4, 2 + actor.Movement, () => actor.Attack(targets[0].actor, false));
            if (path != null)
                return;
            targets.RemoveAt(0);
        }
    }

    protected virtual List<PotentialTarget> GetListOfPotentialPounceTargets(Actor_Unit actor, Vec2i position, int moves)
    {
        List<PotentialTarget> targets = new List<PotentialTarget>();
        if (State.GameManager.TacticalMode.IsOnlyOneSideVisible() && actor.Unit.IsInfiltratingSide(AISide))
            return targets;
        foreach (Actor_Unit unit in actors)
        {
            if (unit.Targetable == true && (unit.InSight || !State.World.IsNight) && TacticalUtilities.FreeSpaceAroundTarget(unit.Position, actor) && TacticalUtilities.TreatAsHostile(actor, unit) && unit.Surrendered == false)
            {
                int distance = unit.Position.GetNumberOfMovesDistance(position);
                if (distance <= 2 + moves)
                {
                    float chance = unit.GetAttackChance(actor, true, true);
                    targets.Add(new PotentialTarget(unit, chance, distance, 4));
                }
            }
        }
        return targets.OrderByDescending(t => t.utility).ToList();
    }

    protected virtual int CheckRanged(Actor_Unit actor, Vec2i position, int ap)
    {
        List<PotentialTarget> targets = GetListOfPotentialRangedTargets(actor, position);
        if (!targets.Any() || actor.BestRanged == null || actor.Unit.GetBestRanged() == null)
            return -1;
        Actor_Unit reserveTarget = targets[0].actor;
        while (targets.Any())
        {
            if (targets[0].distance <= actor.BestRanged.Range && (targets[0].distance > 1 || (targets[0].distance > 0 && actor.BestRanged.Omni)))
            {
                return 1;
            }
            targets.RemoveAt(0);
        }

        if (reserveTarget != null)
        {
            if (actor.Position.GetNumberOfMovesDistance(reserveTarget.Position) == 1)
            {
                return 1;
            }
            else
            {
                int distance = CheckMoveTo(actor, position, reserveTarget.Position, actor.BestRanged.Range, ap);
                if (distance < ap && distance >= 0)
                    return distance + 1;
            }
        }
        return -1;
    }

    protected virtual void RunRanged(Actor_Unit actor)
    {
        List<PotentialTarget> targets = GetListOfPotentialRangedTargets(actor, actor.Position);
        if (!targets.Any() || actor.BestRanged == null || actor.Unit.GetBestRanged() == null)
            return;
        Actor_Unit reserveTarget = targets[0].actor;
        while (targets.Any())
        {
            if (targets[0].distance <= actor.BestRanged.Range && (targets[0].actor.InSight || !State.World.IsNight) && (targets[0].distance > 1 || (targets[0].distance > 0 && actor.BestRanged.Omni)))
            {
                actor.Attack(targets[0].actor, true);
                didAction = true;
                break;
            }
            targets.RemoveAt(0);
        }
        if (didAction == false)
        {
            if (reserveTarget != null)
            {
                if (actor.Position.GetNumberOfMovesDistance(reserveTarget.Position) == 1)
                {
                    if ((Config.World.DefualtTacticalSightRange + actor.Unit.TraitBoosts.SightRangeBoost == 1) || RandomWalk(actor) == false)
                        RunMelee(actor); //We're surrounded or won't be able to see the target if we move away
                }
                else
                {
                    if (reserveTarget.InSight || !State.World.IsNight)
                    {
                        MoveToAndAction(actor, reserveTarget.Position, actor.BestRanged.Range, 999, () => actor.Attack(reserveTarget, true));
                    }
                    if (foundPath)
                        return;
                    MoveToAndAction(actor, reserveTarget.Position, 15, 999, null); //Just move towards if you can't find a great route
                }
            }
            else
            {
                RandomWalkAndEndTurn(actor);
            }
        }
    }

    protected virtual List<PotentialTarget> GetListOfPotentialRangedTargets(Actor_Unit actor, Vec2i position) // Adapted all the potential target checks to allow for positions aside from the actor's location
    {
        List<PotentialTarget> targets = new List<PotentialTarget>();
        if (actor.BestRanged == null) return targets; //This shouldn't happen, but just in case
        if (State.GameManager.TacticalMode.IsOnlyOneSideVisible() && actor.Unit.IsInfiltratingSide(AISide)) return targets;
        foreach (Actor_Unit target in actors)
        {
            if (target?.Unit == null) //If this doesn't prevent exceptions I might have to just try/catch this function.  
                continue;
            if (target.Targetable == true && TacticalUtilities.TreatAsHostile(actor, target) && target.Surrendered == false)
            {
                int distance = target.Position.GetNumberOfMovesDistance(position);
                float chance = target.GetAttackChance(actor, true, true);
                int damage = actor.PreviewWeaponDamageAgainstTarget(target, true);
                targets.Add(new PotentialTarget(target, chance, distance, damage,((target.InSight&&State.World.IsNight)? 100:0)));
            }
        }
        return targets.OrderByDescending(t => t.utility).ToList();
    }

    protected virtual int CheckMelee(Actor_Unit actor, Vec2i position, int ap)
    {
        List<PotentialTarget> targets = GetListOfPotentialMeleeTargets(actor, position, ap);
        if (!targets.Any())
            return -1;
        Actor_Unit reserveTarget = targets[0].actor;
        while (targets.Any())
        {
            if (targets[0].distance < 2)
            {
                return 1;
            }
            else
            {
                // Reaching the adjacent attack tile costs one fewer step than
                // reaching the enemy itself. A 2-AP actor can move once and attack.
                if (targets[0].actor.Position.GetNumberOfMovesDistance(position) <= ap)
                {
                    var route = TacticalPathfinder.GetPath(position, targets[0].actor.Position, 1, actor, ap);
                    if (route != null && route.Count > 0)
                    {
                        int cost = route.Sum(node => OpportunityAttacks.MoveCost(actor, node));
                        if (cost < ap) return cost + 1;
                    }
                }
            }
            targets.RemoveAt(0);
        }

        return -1;
    }


    protected virtual void RunMelee(Actor_Unit actor)
    {
        List<PotentialTarget> targets = GetListOfPotentialMeleeTargets(actor, actor.Position, actor.Movement);
        if (!targets.Any())
            return;
        Actor_Unit reserveTarget = targets[0].actor;
        while (targets.Any())
        {
            if (targets[0].distance < 2 && (targets[0].actor.InSight || !State.World.IsNight))
            {
                    if ((State.Rand.Next(2) == 0) && TacticalActionList.TargetedDictionary[SpecialAction.TailStrike].AppearConditional(actor))
                        actor.TailStrike(targets[0].actor);
                    if ((State.Rand.Next(4) == 0) && TacticalActionList.TargetedDictionary[SpecialAction.AllInVore].AppearConditional(actor))
                        actor.AllInVore(targets[0].actor, SpecialAction.None, true);
                    if ((State.Rand.Next(2) == 0) && TacticalActionList.TargetedDictionary[SpecialAction.DireInfection].AppearConditional(actor))
                        actor.DireInfection(targets[0].actor);
                    else
                    {
                actor.Attack(targets[0].actor, false);
                didAction = true;
                return;
                    }
            }
            else
            {
                if (targets[0].actor.Position.GetNumberOfMovesDistance(actor.Position) <= actor.Movement && (targets[0].actor.InSight || !State.World.IsNight)) //discard the clearly impossible
                {
                    if ((State.Rand.Next(2) == 0) && TacticalActionList.TargetedDictionary[SpecialAction.DireInfection].AppearConditional(actor))
                        MoveToAndAction(actor, targets[0].actor.Position, 1, actor.Movement, () => actor.DireInfection(targets[0].actor));
                    if ((State.Rand.Next(2) == 0) && TacticalActionList.TargetedDictionary[SpecialAction.TailStrike].AppearConditional(actor))
                        MiscUtilities.DelayedInvoke(() => MoveToAndAction(actor, targets[0].actor.Position, 1, actor.Movement, () => actor.TailStrike(targets[0].actor)), .06f);
                    if ((State.Rand.Next(4) == 0) && TacticalActionList.TargetedDictionary[SpecialAction.AllInVore].AppearConditional(actor))
                        MoveToAndAction(actor, targets[0].actor.Position, 1, actor.Movement, () => actor.AllInVore(targets[0].actor, SpecialAction.None, true));
                    else
                        MoveToAndAction(actor, targets[0].actor.Position, 1, actor.Movement, () => actor.Attack(targets[0].actor, false));
                    if (foundPath && path.Path.Sum(node => OpportunityAttacks.MoveCost(actor, node)) < actor.Movement)
                        return;
                }
            }
            targets.RemoveAt(0);
        }
        if (didAction == false)
        {
            if (reserveTarget != null)
            {
                //Get as close to the target as you can if you can't reach it
                if (reserveTarget.InSight)
                {
                    MoveToAndAction(actor, reserveTarget.Position, -1, 999, null);
                }
                else
                {
                    if (State.Rand.NextDouble() > 0.6f)
                    {
                        RandomWalkAndEndTurn(actor); //40% chance to hang back
                    }
                    else
                    {
                        int halfdist = (int)Math.Floor(actor.Position.GetDistance(reserveTarget.Position)) / 2;
                        halfdist += State.Rand.Next(-3, 3);
                        MoveToAndAction(actor, reserveTarget.Position, halfdist, 999, null); //Walk near target
                    }                  
                }
                if (foundPath)
                    return;
                RandomWalkAndEndTurn(actor);
            }
            else
            {
                RandomWalkAndEndTurn(actor);
            }
        }
    }

    protected virtual List<PotentialTarget> GetListOfPotentialMeleeTargets(Actor_Unit actor, Vec2i position, int moves)
    {
        List<PotentialTarget> targets = new List<PotentialTarget>();
        if (State.GameManager.TacticalMode.IsOnlyOneSideVisible() && actor.Unit.IsInfiltratingSide(AISide))
            return targets;
        foreach (Actor_Unit unit in actors)
        {
            if (unit.Targetable == true && TacticalUtilities.TreatAsHostile(actor, unit) && unit.Surrendered == false)
            {

                int distance = unit.Position.GetNumberOfMovesDistance(position);
                if (distance < moves)
                {
                    if (distance > 1 && TacticalUtilities.FreeSpaceAroundTarget(unit.Position, actor) == false)
                        continue;
                }
                int chance = (int)unit.GetAttackChance(actor, false, true);
                int damage = actor.PreviewWeaponDamageAgainstTarget(unit, false);
                targets.Add(new PotentialTarget(unit, chance, distance, damage, ((unit.InSight && State.World.IsNight) ? 100 : 0)));


            }
        }

        PotentialTarget primeTarget = targets.Where(t => t.distance < 2).OrderByDescending(s => s.utility).FirstOrDefault();
        if (primeTarget != null)
            return new List<PotentialTarget>() { primeTarget };
        return targets.OrderByDescending(t => t.utility).ToList();
    }

    List<Vec2i> SummonTiles(Actor_Unit actor, Spell spell, Vec2i position)
    {
        if (spell == SpellList.Summon && SpeciesPolicy.GetAvailable(SpeciesUse.CreatureSummon).Length == 0)
            return new List<Vec2i>();
        if (spell == SpellList.SummonSpawn && (!SpeciesPolicy.IsAllowed(actor.Unit.HiddenUnit.DetermineSpawnRace(), SpeciesUse.Spawn) ||
            !SpeciesPolicy.IsAllowed(actor.Unit.HiddenUnit.DetermineSpawnRace(), SpeciesUse.CreatureSummon)))
            return new List<Vec2i>();
        if (spell == SpellList.SummonDoppelganger && !SpeciesPolicy.IsAllowed(actor.Unit.Race, SpeciesUse.Copy))
            return new List<Vec2i>();
        var open = TacticalUtilities.TilesWithinRange(position, Math.Min(2, spell.Range.Max))
            .Where(tile => TacticalUtilities.OpenTile(tile, null)).ToList();
        var safe = open.Where(tile => !TacticalPathfinder.IsBurning(tile.x, tile.y)).ToList();
        return safe.Count > 0 ? safe : open;
    }

    protected virtual int CheckSpells(Actor_Unit actor, Vec2i position, int ap)
    {
        if (actor.Unit.UseableSpells == null || ap <= 0 || State.GameManager.TacticalMode.IsOnlyOneSideVisible()) return -1;
        foreach (var spell in actor.Unit.UseableSpells.Where(sp => !sp.IsRetired && sp.ManaCost <= actor.Unit.Mana))
        {
            if (spell.IsConsumption && !actor.Unit.Predator) continue;
            if (spell == SpellList.Summon || spell == SpellList.SummonDoppelganger || spell == SpellList.SummonSpawn)
            {
                if (SummonTiles(actor, spell, position).Count > 0) return 1;
                continue;
            }
            foreach (var target in GetListOfPotentialSpellTargets(actor, spell, position))
            {
                if (target.distance <= spell.Range.Max) return 1;
                if (target.actor.Position.GetNumberOfMovesDistance(actor.Position) < actor.Movement)
                {
                    int distance = CheckMoveTo(actor, position, target.actor.Position, 1, actor.Movement);
                    if (distance >= 0 && distance < ap) return distance + 1;
                }
            }
        }
        return -1;
    }

    protected virtual void RunSpells(Actor_Unit actor)
    {
        if (actor.Unit.UseableSpells == null || State.GameManager.TacticalMode.IsOnlyOneSideVisible()) return;
        var spells = actor.Unit.UseableSpells.Where(sp => !sp.IsRetired && sp.ManaCost <= actor.Unit.Mana).ToList();
        if (spells.Count == 0) return;
        int start = State.Rand.Next(spells.Count);
        for (int index = 0; index < spells.Count; index++)
        {
            var spell = spells[(start + index) % spells.Count];
            if (spell.IsConsumption && !actor.Unit.Predator) continue;
            if (spell == SpellList.Summon || spell == SpellList.SummonDoppelganger || spell == SpellList.SummonSpawn)
            {
                var locations = SummonTiles(actor, spell, actor.Position);
                if (locations.Count > 0 && spell.TryCast(actor, locations[State.Rand.Next(locations.Count)]))
                { didAction = true; return; }
                continue;
            }
            foreach (var target in GetListOfPotentialSpellTargets(actor, spell, actor.Position))
            {
                if (!target.actor.InSight && State.World.IsNight) continue;
                if (target.distance <= spell.Range.Max)
                {
                    if (spell.TryCast(actor, target.actor)) { didAction = true; return; }
                }
                else if (target.actor.Position.GetNumberOfMovesDistance(actor.Position) <= actor.Movement + spell.Range.Max)
                {
                    var selected = target;
                    MoveToAndAction(actor, selected.actor.Position, spell.Range.Max, actor.Movement, () => spell.TryCast(actor, selected.actor));
                    if (foundPath && path.Path.Count() < actor.Movement) return;
                    foundPath = false; path = null;
                }
            }
        }
    }

    protected virtual List<PotentialTarget> GetListOfPotentialSpellTargets(Actor_Unit actor, Spell spell, Vec2i position)
    {
        List<PotentialTarget> targets = new List<PotentialTarget>();
        if (State.GameManager.TacticalMode.IsOnlyOneSideVisible() && actor.Unit.IsInfiltratingSide(AISide))
            return targets;
        foreach (Actor_Unit unit in actors)
        {
            if (spell is StatusSpell statusSpell && unit.Unit.GetStatusEffect(statusSpell.Type) != null)
                continue; //Don't recast the same spell on the same unit
            if (TacticalUtilities.TreatAsHostile(actor, unit) && spell.AcceptibleTargets.Contains(AbilityTargets.Enemy))
            {
                if (spell.AreaOfEffect > 0)
                {
                    int distance = unit.Position.GetNumberOfMovesDistance(actor.Position);
                    float chance = (spell.Resistable ? unit.GetMagicChance(actor, spell) : 1f);
                    int friendlies = 0;
                    int enemies = 0;
                    foreach (var splashTarget in TacticalUtilities.UnitsWithinTiles(unit.Position, spell.AreaOfEffect))
                    {

                        if (spell is StatusSpell status && splashTarget.Unit.GetStatusEffect(status.Type) != null)
                            continue;
                        if (!TacticalUtilities.TreatAsHostile(actor, splashTarget))
                            friendlies++;
                        else if (splashTarget.Surrendered == false)
                            enemies++;
                    }
                    int net = enemies - friendlies;
                    if (net < 1)
                        continue;
                    targets.Add(new PotentialTarget(unit, net, distance, 4, net * 1000 + chance));
                }
                if (unit.Targetable == true && (unit.InSight || !State.World.IsNight) && unit.Surrendered == false)
                {
                    int distance = unit.Position.GetNumberOfMovesDistance(position);
                    float chance = (spell.Resistable ? unit.GetMagicChance(actor, spell) : 1f);
                    targets.Add(new PotentialTarget(unit, chance, distance, 4));
                }
            }
            else if (!TacticalUtilities.TreatAsHostile(actor, unit) && spell.AcceptibleTargets.Contains(AbilityTargets.Ally))
            {
                if (spell == SpellList.Mending && (100 * unit.Unit.HealthPct) > 84)
                    continue;
                if (spell is StatusSpell statSpell)
                {
                    if (unit.Unit.GetStatusEffect(statSpell.Type) != null)
                        continue;
                }
                if (unit.Targetable == true && unit.Surrendered == false)
                {
                    int distance = unit.Position.GetNumberOfMovesDistance(position);
                    float chance = (spell.Resistable ? unit.GetMagicChance(actor, spell) : 1f);
                    targets.Add(new PotentialTarget(unit, chance, distance, unit.Unit.Level));

                }
            }
            else if (!TacticalUtilities.TreatAsHostile(actor, unit) && spell.AcceptibleTargets.Contains(AbilityTargets.Self))
            {
                if (actor.Unit != unit.Unit)
                    continue;
                if (spell == SpellList.AssumeForm && unit?.PredatorComponent.PreyCount <= 0)
                    continue;
                if (spell == SpellList.RevertForm && unit.Unit.Race == unit.Unit.HiddenRace)
                    continue;
                if (unit.Targetable == true && unit.Surrendered == false)
                {
                    int distance = unit.Position.GetNumberOfMovesDistance(position);
                    float chance = (spell.Resistable ? unit.GetMagicChance(actor, spell) : 1f);
                    targets.Add(new PotentialTarget(unit, chance, distance, unit.Unit.Level));
                }
            }
        }
        return targets.OrderByDescending(t => t.utility).ToList();
    }

    protected virtual void RunPotions(Actor_Unit actor)
    {
        if (actor.Unit.EquippedPotions == null || actor.Unit.EquippedPotions.Any(p => p.Value[0] > 0) == false)
            return;
        List<Potion> availablePotions = new List<Potion>();
        foreach (var potionStash in actor.Unit.EquippedPotions)
        {
            if (potionStash.Value[0] > 0)
            {
                availablePotions.Add((Potion)State.World.ItemRepository.GetItem(potionStash.Key));
            }
        }

        if (availablePotions == null || availablePotions.Any() == false)
            return;

        Potion potion = availablePotions[State.Rand.Next(availablePotions.Count())];

        if (State.GameManager.TacticalMode.IsOnlyOneSideVisible())
            return;
        List<PotentialTarget> targets = GetListOfPotentialPotion(actor, potion, actor.Position, potion.NegativeEffect);
        if (!targets.Any())
            return;
        Actor_Unit reserveTarget = targets[0].actor;
        while (targets.Any())
        {
            if (targets[0].distance <= 3 && (targets[0].actor.InSight || !State.World.IsNight))
            {
                if (potion.ActivatePotion(actor, targets[0].actor))
                    didAction = true;
                return;
            }
            else
            {
                if (targets[0].actor.Position.GetNumberOfMovesDistance(actor.Position) <= actor.Movement + 3 && (targets[0].actor.InSight || !State.World.IsNight)) //discard the clearly impossible
                {
                    MoveToAndAction(actor, targets[0].actor.Position, 3, actor.Movement, () => potion.ActivatePotion(actor, targets[0].actor));
                    if (foundPath && path.Path.Count() < actor.Movement)
                        return;
                    else
                    {
                        foundPath = false;
                        path = null;
                    }
                }
            }
            targets.RemoveAt(0);
        }
    }

    protected virtual List<PotentialTarget> GetListOfPotentialPotion(Actor_Unit actor, Potion potion, Vec2i position, bool IsNegative)
    {
        List<PotentialTarget> targets = new List<PotentialTarget>();
        if (State.GameManager.TacticalMode.IsOnlyOneSideVisible() && actor.Unit.IsInfiltratingSide(AISide))
            return targets;
        foreach (Actor_Unit unit in actors)
        {
            if (TacticalUtilities.TreatAsHostile(actor, unit) && IsNegative)
            {
                if (unit.Targetable == true && (unit.InSight || !State.World.IsNight) && unit.Surrendered == false)
                {
                    int distance = unit.Position.GetNumberOfMovesDistance(position);
                    float chance = unit.GetAttackChance(unit, true);
                    targets.Add(new PotentialTarget(unit, chance, distance, 4));
                }
            }
            else if (!TacticalUtilities.TreatAsHostile(actor, unit) && !IsNegative)
            {
                if (unit.Targetable == true && unit.Surrendered == false)
                {
                    int distance = unit.Position.GetNumberOfMovesDistance(position);
                    float chance = (float)State.Rand.NextDouble();
                    targets.Add(new PotentialTarget(unit, chance, distance, unit.Unit.Level));
                }
            }
            else if (!TacticalUtilities.TreatAsHostile(actor, unit) && !IsNegative)
            {
                if (unit.Targetable == true && unit.Surrendered == false)
                {
                    int distance = unit.Position.GetNumberOfMovesDistance(position);
                    float chance = (float)State.Rand.NextDouble();
                    if (chance <= 0.5f)
                    {
                        chance += 0.3f;
                    }
                    targets.Add(new PotentialTarget(unit, chance, distance, unit.Unit.Level));
                }
            }
        }
        return targets.OrderByDescending(t => t.utility).ToList();
    }


    protected virtual bool IsRanged(Actor_Unit actor)
    {
        return actor.BestRanged != null;
    }

    public virtual void HandleLeftoverForeigns(Actor_Unit actor) 
    {
        actor.Unit.hiddenFixedSide = false;
       if (actor.Unit.HasTrait(Traits.Infiltrator) && actor.Unit.Type != UnitType.Summon) // You were there to cause the enemy a headache, get right back to it!
        {
            if (State.GameManager.TacticalMode.currentTurn < 500) // Someone just got STUCK trying to flee forever. Wow.
            {
                if (!retreating) { 
                    retreating = true;                      // Will hopefully cause inattentive opponents to have these sneaking right back into their cities
                    State.GameManager.TacticalMode.Log.RegisterMiscellaneous($"<color=orange>{(actors[0].Unit.Side == AISide ? "Attackers" : "Defenders")} are now fleeing</color>");
                }
                return;
            }
        }
        if (actor.allowedToDefect || (State.GameManager.TacticalMode.currentTurn > 500 && !actor.DefectedThisTurn))
        {
            State.GameManager.TacticalMode.SwitchAlignment(actor);
            actor.DefectedThisTurn = true;
        }
    }

    public void RunFeed(Actor_Unit actor, string feedType, bool forCombatHealing = false)
    {
        // Fail if no possible targets
        List<PotentialTarget> targets = GetListOfPotentialFeedTargets(actor);
        if (!targets.Any())
            return;
        if (targets[0].chance <= 1.0f && forCombatHealing)
            return;

        // If possible targets present
        while (targets.Any())
        {
            // If target is in range
            if (targets[0].distance < 2)
            {
                if (feedType == "breast")
                    actor.PredatorComponent.Feed(targets[0].actor);
                if (feedType == "cock")
                    actor.PredatorComponent.FeedCum(targets[0].actor);
                didAction = true;
                break;
            }
            else
            {
                if (feedType == "breast")
                    MoveToAndAction(actor, targets[0].actor.Position, 1, actor.Movement - 1, () => actor.PredatorComponent.Feed(targets[0].actor));
                if (feedType == "cock")
                    MoveToAndAction(actor, targets[0].actor.Position, 1, actor.Movement - 1, () => actor.PredatorComponent.FeedCum(targets[0].actor));
                if (foundPath && path.Path.Count() < actor.Movement)
                    break;
                else
                {
                    foundPath = false;
                    path = null;
                }
            }
            targets.RemoveAt(0);
        }
    }

    List<PotentialTarget> GetListOfPotentialFeedTargets(Actor_Unit actor)
    {
        List<PotentialTarget> targets = new List<PotentialTarget>();
        foreach (Actor_Unit unit in actors)
        {
            if (unit.Targetable && unit.Unit.Side == AISide && unit.Surrendered == false)
            {
                int distance = unit.Position.GetNumberOfMovesDistance(actor.Position);
                if (distance < actor.Movement)
                {
                    if ((distance > 1 && TacticalUtilities.FreeSpaceAroundTarget(unit.Position, actor) == false) || (unit.Unit.HealthPct == 1.0f && !Config.OverhealEXP) || unit == actor)
                        continue;
                    if (Config.OverhealEXP && unit.Unit.HealthPct == 1.0f)
                        targets.Add(new PotentialTarget(unit, 1.0f + unit.Unit.Experience, distance, 4));
                    else
                        targets.Add(new PotentialTarget(unit, unit.Unit.HealthPct, distance, 4));
                }
            }
        }
        PotentialTarget primeTarget = targets.Where(t => t.distance == 1).OrderBy(s => s.chance).FirstOrDefault();
        if (primeTarget != null)
            return new List<PotentialTarget>() { primeTarget };
        return targets.OrderBy(t => t.chance).ToList();
    }

    public void RunSuckle(Actor_Unit actor)
    {
        // Fail if no possible targets
        List<PotentialTarget> targets = GetListOfPotentialSuckleTargets(actor);
        if (!targets.Any())
            return;
        // If unit can vore and possible targets present
        while (targets.Any())
        {
            // If target is in range
            if (targets[0].distance < 2)
            {
                actor.PredatorComponent.Suckle(targets[0].actor);
                didAction = true;
                break;
            }
            else
            {
                MoveToAndAction(actor, targets[0].actor.Position, 1, 999, () => actor.PredatorComponent.Suckle(targets[0].actor));
                if (foundPath && path.Path.Count() < actor.Movement)
                    break;
                else
                {
                    foundPath = false;
                    path = null;
                }
            }
            targets.RemoveAt(0);
        }
    }

    List<PotentialTarget> GetListOfPotentialSuckleTargets(Actor_Unit actor)
    {
        List<PotentialTarget> targets = new List<PotentialTarget>();
        foreach (Actor_Unit unit in actors)
        {
            if (unit.Unit.Predator)
            {
                if (unit.Targetable && unit.Unit.Side == AISide && (unit.PredatorComponent.CanFeed() || unit.PredatorComponent.CanFeedCum()))
                {
                    int distance = unit.Position.GetNumberOfMovesDistance(actor.Position);
                    if (distance < actor.Movement)
                    {
                        if ((distance > 1 && TacticalUtilities.FreeSpaceAroundTarget(unit.Position, actor) == false) || (unit.Unit.HealthPct == 1.0f && !Config.OverhealEXP) || unit == actor)
                            continue;
                        int[] suckling = actor.PredatorComponent.GetSuckle(unit);
                        if (actor.Unit.HealthPct < 1.0f && suckling[0] == 0)
                            targets.Add(new PotentialTarget(unit, suckling[0], distance, 4));
                        if (Config.OverhealEXP && suckling[1] != 0)
                            targets.Add(new PotentialTarget(unit, suckling[1], distance, 4));
                    }
                }
            }
        }
        PotentialTarget primeTarget = targets.Where(t => t.distance == 1).OrderByDescending(s => s.chance).FirstOrDefault();
        if (primeTarget != null)
            return new List<PotentialTarget>() { primeTarget };
        return targets.OrderByDescending(t => t.chance).ToList();
    }

}

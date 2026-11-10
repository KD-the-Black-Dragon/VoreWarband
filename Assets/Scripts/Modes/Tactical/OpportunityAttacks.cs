// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-04: Account for old-injury critical penalties in opportunity risk (spec 33).
// 2026-11-04: Estimate armor/barrier mitigation separately for hits, grazes and criticals (spec 34 slice 4).
// 2026-11-04: Implement exclusive baseline trait tiers, flat combat modifiers and categorized Help (spec 32).
// 2026-11-04: Shared move-command reactions, sequencing and deterministic risk estimates (spec 30).
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
// 2026-11-04: Apply approved Dodger, KeenEye and action-economy trait refinements (spec 31).
// 2026-11-04: Distinguish Juggernaut active turns from scheduled and actual skipped turns (grouped review).
// 2026-11-09: Share projected weapon values and unarmed-sapient filtering with tactical AI planning (tactical AI review).
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// One battle-local transaction. Save/input/turn changes wait for it to settle;
// no reaction queue needs to be serialized or replayed after loading.
internal sealed class OpportunityAttacks
{
    internal Actor_Unit Mover { get; private set; }
    Vec2i origin;
    Vec2i destination;
    List<Actor_Unit> reactors;
    int next;
    float delay;
    internal bool Ready { get; private set; }
    internal bool Busy => Mover != null;
    internal Vec2i Destination => destination;
    internal static bool Resolving { get; private set; }

    internal static bool CanAct(Actor_Unit actor)
    {
        return actor != null && actor.Unit != null && actor.Targetable && !actor.Fled &&
            !actor.Unit.IsDead && !actor.Surrendered && actor.SelfPrey == null && !actor.Paralyzed && actor.TurnsSinceLastParalysis > 0 &&
            !actor.SkippedBySlacking && actor.Unit.GetStatusEffect(StatusEffectType.Petrify) == null &&
            actor.Unit.GetStatusEffect(StatusEffectType.Frozen) == null &&
            actor.Unit.GetStatusEffect(StatusEffectType.Sleeping) == null &&
            actor.Unit.GetStatusEffect(StatusEffectType.Stunned) == null;
    }

    internal static IEnumerable<Actor_Unit> Threats(Actor_Unit mover, Vec2i position)
    {
        // Stable battlefield order; AP and player visibility do not grant immunity.
        return (TacticalUtilities.Units ?? new List<Actor_Unit>()).Where(enemy =>
            enemy != mover && CanAct(enemy) && enemy.BestMelee != null &&
            enemy.Position.GetNumberOfMovesDistance(position) == 1 &&
            TacticalUtilities.TreatAsHostile(enemy, mover));
    }

    internal static IEnumerable<Actor_Unit> AIThreats(Actor_Unit mover, Vec2i position)
    {
        // AI planning discounts weak barehanded sapient reactions. Threats() is
        // unchanged, so these attacks still resolve normally in battle.
        return Threats(mover, position).Where(enemy => !IsUnarmedSapient(enemy));
    }

    internal bool Begin(Actor_Unit mover, Vec2i target)
    {
        var adjacent = Threats(mover, mover.Position).ToList();
        if (adjacent.Count == 0) return false;
        Mover = mover;
        origin = mover.Position;
        destination = target;
        reactors = adjacent;
        next = 0;
        delay = 0;
        Ready = false;
        State.GameManager.TacticalMode.CommitOpportunityMovement(mover, false);
        return true;
    }

    internal void Reset()
    {
        Mover = null;
        reactors = null;
        Ready = false;
        Resolving = false;
    }

    internal bool Matches(Actor_Unit actor, Vec2i target)
    {
        return Mover == actor && destination.Matches(target);
    }

    // Called before normal battle processing (also in auto-resolve). Each visible
    // attack gets its own presentation interval before the next attack or walk.
    internal bool Advance(float dt)
    {
        if (!Busy) return true;
        bool turbo = State.GameManager.TacticalMode.turboMode;
        if (!turbo && delay > 0)
        {
            delay -= dt;
            return false;
        }
        if (!CanAct(Mover) || !Mover.Position.Matches(origin) || Mover.Movement <= 0)
        {
            Finish(true);
            return true;
        }
        while (next < reactors.Count)
        {
            var enemy = reactors[next++];
            if (!Threats(Mover, origin).Contains(enemy)) continue;
            Resolving = true;
            try { enemy.Attack(Mover, false, opportunity: true); }
            finally { Resolving = false; }
            State.GameManager.TacticalMode.DirtyPack = true;
            State.GameManager.TacticalMode.RebuildInfo();
            if (!turbo)
            {
                delay = Mathf.Max(1f, Config.TacticalAttackDelay);
                return false;
            }
            if (!CanAct(Mover) || !Mover.Position.Matches(origin) || Mover.Movement <= 0)
            {
                Finish(true);
                return true;
            }
        }
        Ready = true;
        return true;
    }

    internal void Finish(bool interrupted)
    {
        var actor = Mover;
        if (interrupted && !CanAct(actor)) actor.ClearMovement();
        Mover = null;
        reactors = null;
        Ready = false;
        State.GameManager.TacticalMode.CommitOpportunityMovement(actor, true, interrupted);
    }

    internal static bool IsPriorityTarget(Actor_Unit target)
    {
        bool mage = target.Unit.GetStat(Stat.Intelligence) > target.Unit.GetStat(Stat.Strength);
        bool archer = target.Unit.GetStat(Stat.Agility) > target.Unit.GetStat(Stat.Strength);
        return mage || archer || IsUnarmedSapient(target);
    }

    internal static bool IsUnarmedSapient(Actor_Unit target)
    {
        if (target == null || target.Unit == null)
            return false;
        bool sapient = SpeciesPolicy.IsCurated(target.Unit.Race) ?
            SpeciesPolicy.GetKind(target.Unit.Race) == SpeciesKind.Sapient : !target.Unit.HasTrait(Traits.Feral);
        return sapient && !(target.Unit.Items ?? new Item[0]).OfType<Weapon>().Any(weapon => weapon.Range <= 1);
    }

    internal static int MoveCost(Actor_Unit actor, PathNode node)
    {
        return actor.Unit.HasTrait(Traits.Flight) ? 1 : TacticalTileInfo.TileCost(new Vec2(node.X, node.Y));
    }

    internal static float ExpectedDamage(Actor_Unit attacker, Actor_Unit target)
    {
        // No random rolls and no temporarily changed positions/stats during planning.
        return ExpectedWeaponDamage(attacker, target, false, true);
    }

    internal static float ExpectedWeaponDamage(Actor_Unit attacker, Actor_Unit target, bool ranged,
        bool opportunity = false, int distanceOverride = -1)
    {
        float chance = Mathf.Clamp01(target.GetAttackChance(attacker, ranged, true, distanceOverride: distanceOverride));
        float graze = chance >= .51f ? (1 - chance) * (attacker.Unit.HasTrait(Traits.KeenEye) ? 1f : chance) : 0;
        float crit = attacker.CritCheck(attacker, target) + attacker.Unit.TraitBoosts.Outgoing.CritRateShift -
            target.Unit.TraitBoosts.Incoming.CritRateShift;
        crit = Mathf.Clamp01(crit * TagConditionChecker.ApplyTagEffect(attacker.Unit, target.Unit, UnitTagModifierEffect.CritRateShift));
        crit = Mathf.Clamp01(crit + TieredTraits.Amount(attacker.Unit, TierEffect.CriticalChance) - CompanyInjuries.CriticalPenalty(attacker.Unit));
        return chance * ((1 - crit) * attacker.PreviewWeaponDamageAgainstTarget(target, ranged, opportunity: opportunity) +
            crit * attacker.PreviewWeaponDamageAgainstTarget(target, ranged, attacker.ResultDamageMultiplier(target, 2), opportunity)) +
            graze * attacker.PreviewWeaponDamageAgainstTarget(target, ranged, attacker.ResultDamageMultiplier(target, 3), opportunity);
    }

    internal static float AIRouteRisk(Actor_Unit actor, Vec2i start, IList<PathNode> route, out int cost)
    {
        float risk = 0;
        cost = 0;
        Vec2i position = start;
        foreach (var node in route)
        {
            foreach (var enemy in AIThreats(actor, position)) risk += ExpectedDamage(enemy, actor);
            cost += MoveCost(actor, node);
            position = new Vec2i(node.X, node.Y);
        }
        return risk;
    }

    internal static bool AIRouteRiskAllowed(Actor_Unit actor, IList<PathNode> route, float maximumRisk)
    {
        int cost;
        return AIRouteRisk(actor, actor.Position, route, out cost) <= maximumRisk + .01f;
    }

    internal static bool RouteAllowed(Actor_Unit actor, IList<PathNode> route, Actor_Unit target)
    {
        Vec2i from = actor.Position;
        int budget = actor.Movement;
        foreach (var node in route)
        {
            budget -= MoveCost(actor, node);
            if (budget < 0) break;
            if (AIThreats(actor, from).Any()) return target != null && CanAffordCharge(actor, target, route);
            from = new Vec2i(node.X, node.Y);
        }
        return true;
    }

    internal static bool CanAffordCharge(Actor_Unit actor, Actor_Unit target, IList<PathNode> route)
    {
        if (!CanAct(target) || !TacticalUtilities.TreatAsHostile(actor, target) || route == null || route.Count == 0)
            return false;
        int cost;
        float risk = AIRouteRisk(actor, actor.Position, route, out cost);
        var end = route[route.Count - 1];
        // Reserve one real AP for the legal ordinary melee attack at the endpoint.
        if (cost >= actor.Movement || new Vec2i(end.X, end.Y).GetNumberOfMovesDistance(target.Position) != 1 ||
            !TacticalUtilities.OpenTile(end.X, end.Y, actor)) return false;
        if (risk == 0) return true;
        float payoff = ExpectedDamage(actor, target) * (IsPriorityTarget(target) ? 1.75f : 1f);
        return risk < actor.Unit.Health * .6f && risk <= payoff;
    }
}

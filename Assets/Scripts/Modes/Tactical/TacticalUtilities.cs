// Modified for the VoreWar mercenary-company conversion.
// 2026-11-06: Record the owner of newly created tactical hazards (spec 40C).
// Modified for the VoreWar mercenary-company conversion.
// 2026-10-30: 04B2: Replace callback prose with neutral action and status notices.
// 2026-10-31: Retire the obsolete post-battle village conquest helper (spec 05A3B2).
// Upstream additional narrative contributions were credited to Tatltuae; credit retained after reset.
// 2026-10-31: Retire traveling-unit routing and monster army reformation helpers (spec 05A3B3).
// 2026-10-31: Retire unused Resurrection targeting, picker, revive and respawn placement helpers (spec 05A3B9).
// 2026-10-31: Retire obsolete reanimation targeting, picker, runtime and private picker context (spec 05A3B12).
// 2026-11-01: Use retained skirmish control/hostility/visibility rules without empire diplomacy (spec 05A6).
// 2026-11-03: Remove permanently retired identity branches/entries while preserving shared behavior (spec 21).
// See CHANGES.md for the fork's change record and License.txt for GPL-3.0 terms.
// 2026-11-04: Exclude forced displacement and reaction-context knockback (spec 30).
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

static class TacticalUtilities
{
    static Dictionary<Traits, List<Actor_Unit>> linkedUnits;
    static TacticalTileType[,] tiles;
    static bool[,] blockedTiles;
    static bool[,] blockedClimberTiles;

    static internal TacticalMessageLog Log => State.GameManager.TacticalMode.Log;

    static internal List<Actor_Unit> Units { get; private set; }

    static internal void ResetData()
    {
        Units = null;
        tiles = null;
        blockedTiles = null;
        blockedClimberTiles = null;
    }

    static internal void ResetData(List<Actor_Unit> lunits, TacticalTileType[,] ltiles, bool[,] lblockedTiles, bool[,] lblockedClimberTiles, Dictionary<Traits, List<Actor_Unit>> llinkedUnits)
    {
        Units = lunits;
        tiles = ltiles;
        blockedTiles = lblockedTiles;
        blockedClimberTiles = lblockedClimberTiles;
        linkedUnits = llinkedUnits;
    }

    internal static bool IsUnitControlledByPlayer(Unit unit)
    {
        if (GetMindControlSide(unit) != -1)  // Charmed units may fight for the player, but they are always AI controlled
            return false;
        int defenderSide = State.GameManager.TacticalMode.GetDefenderSide();
        int attackerSide = State.GameManager.TacticalMode.GetAttackerSide();
        bool aiDefender = State.GameManager.TacticalMode.AIDefender;
        bool aiAttacker = State.GameManager.TacticalMode.AIAttacker;
        if (State.GameManager.TacticalMode.CheatAttackerControl && unit.Side == attackerSide)
            return true;
        if (State.GameManager.TacticalMode.CheatDefenderControl && unit.Side == defenderSide)
            return true;

        return !aiAttacker && attackerSide == unit.FixedSide || !aiDefender && defenderSide == unit.FixedSide;
    }

    static internal bool AppropriateVoreTarget(Actor_Unit pred, Actor_Unit prey)
    {
        if (pred == prey)
            return false;
        if (pred.Unit.Side == prey.Unit.Side)
        {
            if (prey.Surrendered || pred.Unit.HasTrait(Traits.Cruel) || Config.AllowInfighting || pred.Unit.HasTrait(Traits.FriendlyStomach) || pred.Unit.HasTrait(Traits.Endosoma) || !(prey.Unit.GetApparentSide(pred.Unit) == pred.Unit.FixedSide && prey.Unit.GetApparentSide(pred.Unit) == pred.Unit.GetApparentSide()) || GetMindControlSide(prey.Unit) != -1 || GetMindControlSide(pred.Unit) != -1 )
                return true;
            return false;
        }
        return true;
    }

    static public int GetPreferredSide(Unit actor, int sideA, int sideB) // Retain side parameters for existing callers; skirmish loyalty uses the fixed side.
    {
        return GetMindControlSide(actor) != -1 ? GetMindControlSide(actor) : actor.FixedSide;
    }

    static public bool TreatAsHostile(Actor_Unit actor, Actor_Unit target)
    {
        if (actor == target) return false;
        if (actor.Unit.Side == actor.Unit.FixedSide && !(target.sidesAttackedThisBattle?.Contains(actor.Unit.FixedSide) ?? false) && target.Unit.Side == actor.Unit.Side && GetMindControlSide(actor.Unit) == -1)
            return false;
        int effectiveTargetSide = target.Unit.GetApparentSide(actor.Unit);
        int effectiveActorSide = GetMindControlSide(actor.Unit) != -1 ? GetMindControlSide(actor.Unit) : actor.Unit.FixedSide;
        if (GetMindControlSide(target.Unit) == effectiveActorSide)
            return false;
        return effectiveTargetSide != effectiveActorSide;
    }

    static public bool SneakAttackCheck(Unit attacker, Unit target)
    {
        if (GetMindControlSide(attacker) != -1) return false;
        return attacker.GetApparentSide(target) == target.GetApparentSide() && attacker.IsInfiltratingSide(target.GetApparentSide());
    }

    static public int GetMindControlSide(Unit unit)
    {
        if (unit.GetStatusEffect(StatusEffectType.Hypnotized) != null)
            return (int)(unit.GetStatusEffect(StatusEffectType.Hypnotized).Strength);
        if (unit.GetStatusEffect(StatusEffectType.Charmed) != null)
            return (int)(unit.GetStatusEffect(StatusEffectType.Charmed).Strength);
        return -1;
    }

    static public bool OpenTile(Vec2i vec, Actor_Unit actor) => OpenTile(vec.x, vec.y, actor);

    static public bool FreeSpaceAroundTarget(Vec2i targetLocation, Actor_Unit actor)
    {
        for (int x = targetLocation.x - 1; x < targetLocation.x + 2; x++)
        {
            for (int y = targetLocation.y - 1; y < targetLocation.y + 2; y++)
            {
                if (x == targetLocation.x && y == targetLocation.y)
                    continue;
                if (OpenTile(x, y, actor))
                {
                    return true;
                }
            }
        }
        return false;
    }

    static public bool OpenTile(int x, int y, Actor_Unit actor)
    {
        if (x < 0 || y < 0 || x > tiles.GetUpperBound(0) || y > tiles.GetUpperBound(1))
            return false;
        if (blockedTiles != null)
        {
            if (actor?.Unit.HasTrait(Traits.NimbleClimber) ?? false)
            {
                if (x <= blockedClimberTiles.GetUpperBound(0) || y <= blockedClimberTiles.GetUpperBound(1))
                {
                    if (blockedClimberTiles[x, y])
                        return false;
                }
            }
            else
            {
                if (x <= blockedTiles.GetUpperBound(0) || y <= blockedTiles.GetUpperBound(1))
                {
                    if (blockedTiles[x, y])
                        return false;
                }
            }

        }

        if (TacticalTileInfo.CanWalkInto(tiles[x, y], actor))
        {
            for (int i = 0; i < Units.Count; i++)
            {
                if (Units[i].Targetable == true && !Units[i].Hidden)
                {
                    if (Units[i].Position.x == x && Units[i].Position.y == y)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        return false;
    }

    static public bool PassableOpenTile(Vec2i vec, Actor_Unit actor) => PassableOpenTile(vec.x, vec.y, actor);

    static public bool PassableFreeSpaceAroundTarget(Vec2i targetLocation, Actor_Unit actor)
    {
        for (int x = targetLocation.x - 1; x < targetLocation.x + 2; x++)
        {
            for (int y = targetLocation.y - 1; y < targetLocation.y + 2; y++)
            {
                if (x == targetLocation.x && y == targetLocation.y)
                    continue;
                if (OpenTile(x, y, actor))
                {
                    return true;
                }
            }
        }
        return false;
    }

    static public bool PassableOpenTile(int x, int y, Actor_Unit actor)
    {
        if (x < 0 || y < 0 || x > tiles.GetUpperBound(0) || y > tiles.GetUpperBound(1))
            return false;
        if (blockedTiles != null)
        {
            if (actor?.Unit.HasTrait(Traits.NimbleClimber) ?? false)
            {
                if (x <= blockedClimberTiles.GetUpperBound(0) || y <= blockedClimberTiles.GetUpperBound(1))
                {
                    if (blockedClimberTiles[x, y])
                        return false;
                }
            }
            else
            {
                if (x <= blockedTiles.GetUpperBound(0) || y <= blockedTiles.GetUpperBound(1))
                {
                    if (blockedTiles[x, y])
                        return false;
                }
            }

        }

        if (TacticalTileInfo.CanWalkInto(tiles[x, y], actor))
        {
            for (int i = 0; i < Units.Count; i++)
            {
                if ((Units[i].Targetable == true && !Units[i].Hidden && Units[i].Unit.Side != actor.Unit.Side && actor.Unit.HasTrait(Traits.PassThrough)) || (Units[i].Targetable == true && !Units[i].Hidden && Units[i].Unit.Side == actor.Unit.Side && actor.Unit.HasTrait(Traits.Blitz)))
                {
                    if (Units[i].Position.x == x && Units[i].Position.y == y)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        return false;
    }

    static public bool TileContainsMoreThanOneUnit(int x, int y)
    {
        if (x < 0 || y < 0 || x > tiles.GetUpperBound(0) || y > tiles.GetUpperBound(1))
            return false;
        if (Units == null)
        {
            Debug.Log("This shouldn'targetPred have happened");
            return false;
        }
        int count = 0;
        for (int i = 0; i < Units.Count; i++)
        {
            if (Units[i].Targetable == true)
            {
                if (Units[i].Position.x == x && Units[i].Position.y == y)
                {
                    count++;
                }
            }
        }

        return count > 1;
    }


    static public bool IsWalkable(int x, int y, Actor_Unit actor)
    {
        return TacticalTileInfo.CanWalkInto(tiles[x, y], actor);
    }


    static public bool FlyableTile(int x, int y)
    {
        if (x < 0 || y < 0 || x > tiles.GetUpperBound(0) || y > tiles.GetUpperBound(1))
            return false;
        return true;
    }


    static internal void CheckKnockBack(Actor_Unit Attacker, Actor_Unit Target, ref float damage)
    {
        if (OpportunityAttacks.Resolving) return;
        int xDiff = Target.Position.x - Attacker.Position.x;
        int yDiff = Target.Position.y - Attacker.Position.y;
        int direction = Attacker.DiffToDirection(xDiff, yDiff);
        if (OpenTile(Attacker.GetTile(Target.Position, direction), Target))
            return;
        if (OpenTile(Attacker.GetTile(Target.Position, (direction + 1) % 8), Target))
            return;
        if (OpenTile(Attacker.GetTile(Target.Position, (direction + 7) % 8), Target))
            return;
        damage *= 1.2f;
        return;
    }

    static internal void KnockBack(Actor_Unit Attacker, Actor_Unit Target)
    {
        if (OpportunityAttacks.Resolving) return;
        int xDiff = Target.Position.x - Attacker.Position.x;
        int yDiff = Target.Position.y - Attacker.Position.y;
        int direction = Attacker.DiffToDirection(xDiff, yDiff);

        Target.Movement += 1;
        if (Target.Move(direction, tiles, forced: true))
            return;
        else if (Target.Move((direction + 1) % 8, tiles, forced: true))
            return;
        else if (Target.Move((direction + 7) % 8, tiles, forced: true))
            return;
        Target.Movement -= 1;

        return;
    }

    static internal void CheckSpellKnockBack(Vec2i Knocker, Actor_Unit Attacker, Actor_Unit Target, ref float damage)
    {
        if (OpportunityAttacks.Resolving) return;
        int xDiff = Target.Position.x - Knocker.x;
        int yDiff = Target.Position.y - Knocker.y;
        int direction = Attacker.DiffToDirection(xDiff, yDiff);
        if (OpenTile(Attacker.GetTile(Target.Position, direction), Target))
            return;
        if (OpenTile(Attacker.GetTile(Target.Position, (direction + 1) % 8), Target))
            return;
        if (OpenTile(Attacker.GetTile(Target.Position, (direction + 7) % 8), Target))
            return;
        Target.Unit.TraitBoosts.Incoming.MagicDamage += ((int)(damage));

        return;
    }

    static internal void SpellKnockBack(Vec2i Knocker, Actor_Unit Attacker, Actor_Unit Target)
    {
        if (OpportunityAttacks.Resolving) return;
        int xDiff = Target.Position.x - Knocker.x;
        int yDiff = Target.Position.y - Knocker.y;
        int direction = Attacker.DiffToDirection(xDiff, yDiff);

        Target.Movement += 1;
        if (Target.Move(direction, tiles, forced: true))
            return;
        else if (Target.Move((direction + 1) % 8, tiles, forced: true))
            return;
        else if (Target.Move((direction + 7) % 8, tiles, forced: true))
            return;
        Target.Movement -= 1;

        return;
    }

    static public void MutuallyDamageUnits(Actor_Unit source, int damage)
    {
        if (linkedUnits == null)
            return;
        foreach (var unit in linkedUnits[Traits.MutualBiology].Where(u => u != source && !u.Unit.IsEnemyOfSide(source.Unit.Side)))
        {
            unit.Damage(damage, damageType: DamageTypes.Mutual);
        }
    }

    static public void MutuallyHealUnits(Unit source, int amt)
    {
        if (linkedUnits == null)
            return;
        foreach (var unit in linkedUnits[Traits.MutualBiology].Where(u => u.Unit != source && !u.Unit.IsEnemyOfSide(source.Side)))
        {
            unit.Unit.Heal(amt, true);
        }
    }

    static internal PredatorComponent GetPredatorComponentOfUnit(Unit unit)
    {
        foreach (Actor_Unit actor in Units)
        {
            if (actor.Unit == unit)
                return actor.PredatorComponent;
        }
        return null;
    }

    static internal Actor_Unit FindPredator(Actor_Unit searcher)
    {
        foreach (Actor_Unit Unit in Units)
        {
            if (Unit.PredatorComponent?.IsActorInPrey(searcher) ?? false)
                return Unit;
        }
        return null;
    }

    static internal void UpdateActorLocations()
    {
        foreach (Actor_Unit unit in Units)
        {
            if (unit.UnitSprite == null)
                continue;
            Vec2i vec = unit.Position;
            Vector2 vector2 = new Vector2(vec.x, vec.y);
            unit.UnitSprite.transform.position = vector2;
        }
    }


    static internal void RefreshUnitGraphicType()
    {
        if (Units == null)
            return;
        foreach (Actor_Unit actor in Units)
        {
            {
                Races.GetRace(actor.Unit).RandomCustom(actor.Unit);
            }
        }
    }

    static internal void UpdateVersion()
    {
        foreach (Actor_Unit actor in Units)
        {
            actor.PredatorComponent?.UpdateVersion();
        }
    }

    static internal List<Actor_Unit> UnitsWithinTiles(Vec2 target, int tiles)
    {
        List<Actor_Unit> unitList = new List<Actor_Unit>();
        foreach (Actor_Unit actor in Units)
        {
            
            if (actor.Visible && actor.Targetable)
            {
                if (actor.Position.GetNumberOfMovesDistance(target) <= tiles)
                {
                    unitList.Add(actor);
                }
            }
        }
        return unitList;
    }
    static internal Actor_Unit UnitOnTile(Vec2 target)
    {
        List<Actor_Unit> unitList = new List<Actor_Unit>();
        foreach (Actor_Unit actor in Units)
        {
            if (actor.Position == target)
            {
                return actor;
            }
        }
        return null;
    }

    static internal List<Vec2i> TilesOnPattern(Vec2i location, int[,] TargetTiles, int rows)
    {
        List<Vec2i> tile_positions = new List<Vec2i>();
        int outer_matrix_cursor = 0;
        for (int y = location.y + rows; y >= location.y - rows; y--)
        {
            int inner_matrix_cursor = 0;
            for (int x = location.x + rows; x >= location.x - rows; x--)
            {
                if (x < 0 || y < 0 || x > tiles.GetUpperBound(0) || y > tiles.GetUpperBound(1))
                {
                    inner_matrix_cursor++;
                    continue;
                }
                if (TargetTiles[outer_matrix_cursor,inner_matrix_cursor] >= 1)
                    tile_positions.Add(new Vec2i(x, y));
                inner_matrix_cursor++;
            }
            outer_matrix_cursor++;
        }
        return tile_positions;
    }
    static internal int GetRotatingOctant(Vec2i unit, Vec2i target)
    {
        /// Octant positions are as follows
        /// 7 0 1
        /// 6 X 2
        /// 5 4 3
        /// Where X is this unit's position
        float dist = unit.GetNumberOfMovesDistance(target);
        bool target_above_unit = unit.y < target.y;
        bool target_about_even_with_unit_vert = unit.y == target.y || dist > Math.Abs(unit.y - target.y) + 0.5 * dist;
        bool target_about_even_with_unit_horz = unit.x == target.x || dist > Math.Abs(unit.x - target.x) + 0.5 * dist;
        bool target_to_right_of_unit = unit.x <= target.x;

        if (target_about_even_with_unit_horz)
        {
            if (target_above_unit)
                return 0;
            else
                return 4;
        }
        if (target_about_even_with_unit_vert)
        {
            if (target_to_right_of_unit)
                return 6;
            else
                return 2;
        }
        if (target_above_unit)
        {
            if (target_to_right_of_unit)
            {
                return 1;
            }
            return 3;
        }
        else
        {
            if (target_to_right_of_unit)
            {
                return 7;
            }
            return 5;
        }
        
    }
    static internal Vec2i RoundIndexToPoint(int index, int radius)
    {
        if (radius == 0)
            return new Vec2i(0, 0);
        Vec2i result = new Vec2i(-radius, -radius);

        while (index < 0) index += radius * 8;
        index = index % (radius * 8);

        int edgeLen = radius * 2;

        if (index < edgeLen)
        {
            result.x += index;
        }
        else if ((index -= edgeLen) < edgeLen)
        {
            result.x = radius;
            result.y += index;
        }
        else if ((index -= edgeLen) < edgeLen)
        {
            result.x = radius - index;
            result.y = radius;
        }
        else if ((index -= edgeLen) < edgeLen)
        {
            result.y = radius - index;
        }

        return result;
    }

    static internal int[,] Rotate45(int[,] array)
    {
        int dim = array.GetLength(0);
        int[,] result = new int[dim, dim];

        Vec2i center = new Vec2i((result.GetLength(0) - 1) / 2, (result.GetLength(1) - 1) / 2);
        Vec2i center2 = new Vec2i((array.GetLength(0) - 1) / 2, (array.GetLength(1) - 1) / 2);
        for (int r = 0; r <= (dim - 1) / 2; r++)
        {
            for (int i = 0; i <= r * 8; i++)
            {
                Vec2i source = RoundIndexToPoint(i, r);
                Vec2i target = RoundIndexToPoint(i + r, r);

                if (!(center2.x + source.x < 0 || center2.y + source.y < 0 || center2.x + source.x >= array.GetLength(0) || center2.y + source.y >= array.GetLength(1)))
                    result[center.x + target.x, center.y + target.y] = array[center2.x + source.x, center2.y + source.y];
            }
        }
        return result;
    }
    static internal int[,] Rotate90(int[,] array, bool clockwise)
    {
        // printing the matrix on the basis of
        // observations made on indices.
        int dim = array.GetLength(0);
        int[,] result = new int[dim, dim];
        int outer_cursor = 0;
        if (clockwise)
        {
            for (int j = 0; j < dim; j++)
            {
                int inner_cursor = 0;
                for (int i = dim - 1; i >= 0; i--)
                {
                    result[outer_cursor, inner_cursor] = array[i, j];
                    inner_cursor++;
                }
                outer_cursor++;
            }
        }
        else
        {
            for (int j = dim - 1; j >= 0; j--)
            {
                int inner_cursor = 0;
                for (int i = 0; i < dim; i++)
                {
                    result[outer_cursor, inner_cursor] = array[i, j];
                    inner_cursor++;
                }
                outer_cursor++;
            }
        }

        return result;
    }
    static internal int[,] Rotate180(int[,] array)
    {
        // printing the matrix on the basis of
        // observations made on indices.
        int dim = array.GetLength(0);
        int[,] result = new int[dim, dim];
        int outer_cursor = 0;
        for (int i = dim - 1; i  >= 0; i--)
        {
            int inner_cursor = 0;
            for (int j = dim - 1; j >= 0; j--)
            {
                result[outer_cursor, inner_cursor] = array[i, j];
                inner_cursor++;
            }
            outer_cursor++;
        }
        return result;
    }
    static internal List<Vec2i> rotateTilePattern(Vec2i location, int[,] TargetTiles, int aoe_size, int octant)
    {
        int dim = TargetTiles.GetLength(0);
        int[,] rotated_tiles = TargetTiles;
        switch (octant)
        {
            case 0: // 0 deg
                break;
            case 1: // 45 deg
                rotated_tiles = Rotate45(rotated_tiles);
                break;
            case 2: // 90 deg
                rotated_tiles = Rotate90(rotated_tiles, true);
                break;
            case 3: // 135 deg
                rotated_tiles = Rotate90(rotated_tiles, true);
                rotated_tiles = Rotate45(rotated_tiles);
                break;
            case 4: // 180 deg
                rotated_tiles = Rotate180(rotated_tiles);
                break;
            case 5: // 225 deg
                rotated_tiles = Rotate180(rotated_tiles);
                rotated_tiles = Rotate45(rotated_tiles);
                break;
            case 6: // 270 deg
                rotated_tiles = Rotate90(rotated_tiles, false);
                break;
            case 7: // 315 deg
                rotated_tiles = Rotate90(rotated_tiles, false);
                rotated_tiles = Rotate45(rotated_tiles);
                break;
            default:
                Debug.Log("Incorrect Octant Error");
                break;
        }
        return TilesOnPattern(location, rotated_tiles, aoe_size);
    }

    static internal List<Actor_Unit> UnitsWithinPattern(Vec2i location, int[,] TargetTiles)
    {
        int target_box = (int)((Math.Sqrt(TargetTiles.Length) / 2) - 0.5);
        List<Actor_Unit> pruned_unitList = new List<Actor_Unit>();
        List<Actor_Unit> unitList = UnitsWithinTiles(new Vec2(location.x, location.y), target_box);
        List<Vec2i> tile_positions = TilesOnPattern(location, TargetTiles, target_box);
        foreach (Actor_Unit unit in unitList)
        {
            foreach (Vec2i target_tile in tile_positions)
            {
                if (unit.Position.x == target_tile.x && unit.Position.y == target_tile.y)
                {
                    pruned_unitList.Add(unit);
                }
            }
        }
        return pruned_unitList;
    }
    static internal List<Actor_Unit> UnitsWithinRotatingPattern(Vec2i location, int[,] TargetTiles, int octant)
    {
        int target_box = (int)((Math.Sqrt(TargetTiles.Length) / 2) - 0.5);
        List<Actor_Unit> pruned_unitList = new List<Actor_Unit>();
        List<Actor_Unit> unitList = UnitsWithinTiles(new Vec2(location.x, location.y), target_box);
        List<Vec2i> true_tile_positions = rotateTilePattern(location, TargetTiles, target_box, octant);
        foreach (Actor_Unit unit in unitList)
        {
            foreach (Vec2i target_tile in true_tile_positions)
            {
                if (unit.Position.x == target_tile.x && unit.Position.y == target_tile.y)
                {
                    pruned_unitList.Add(unit);
                }
            }
        }
        return pruned_unitList;
    }


    static internal List<Vec2i> TilesWithinRange(Vec2i location, int range)
    {
        List<Vec2i> tile_positions = new List<Vec2i>();
        int outer_matrix_cursor = 0;
        for (int y = location.y + range; y >= location.y - range; y--)
        {
            int inner_matrix_cursor = 0;
            for (int x = location.x + range; x >= location.x - range; x--)
            {
                if (x < 0 || y < 0 || x > tiles.GetUpperBound(0) || y > tiles.GetUpperBound(1))
                {
                    inner_matrix_cursor++;
                    continue;
                }
                tile_positions.Add(new Vec2i(x, y));
            }
            outer_matrix_cursor++;
        }
        return tile_positions;
    }

    static internal bool MeetsQualifier(List<AbilityTargets> targets, Actor_Unit actor, Actor_Unit target)
    {
        if ((target.Unit.GetApparentSide() != actor.Unit.FixedSide) && targets.Contains(AbilityTargets.Enemy))
            return true;
        if ((target.Unit.GetApparentSide() == actor.Unit.GetApparentSide() || target.Unit.GetApparentSide() == actor.Unit.FixedSide) && targets.Contains(AbilityTargets.Ally))
            return true;
        if ((target.Unit.GetApparentSide() == actor.Unit.GetApparentSide() || target.Unit.GetApparentSide() == actor.Unit.FixedSide) && target.Surrendered && targets.Contains(AbilityTargets.SurrenderedAlly))
            return true;
        if (targets.Contains(AbilityTargets.Enemy) && Config.AllowInfighting)
            return true;
        if (targets.Contains(AbilityTargets.Self) && actor.Unit == target.Unit)
            return true;
        return false;

    }

    static internal Actor_Unit GetActorAt(Vec2 location)
    {
        foreach (Actor_Unit actor in Units)
        {
            if (actor == null)
                continue;
            if (actor.Position.x == location.x && actor.Position.y == location.y)
                return actor;
        }
        return null;
    }

    static internal Actor_Unit GetActorOf(Unit unit)
    {
        return Units.FirstOrDefault(actor => actor.Unit == unit);
    }

    static internal void CreateEffect(Vec2i location, TileEffectType type, int areaOfEffect, float strength, int duration, int? ownerSide = null)
    {
        for (int x = location.x - areaOfEffect; x <= location.x + areaOfEffect; x++)
        {
            for (int y = location.y - areaOfEffect; y <= location.y + areaOfEffect; y++)
            {
                if (x < 0 || y < 0 || x > tiles.GetUpperBound(0) || y > tiles.GetUpperBound(1))
                    continue;
                Vec2 position = new Vec2(x, y);
                TileEffect effect = new TileEffect(duration, strength, type) { OwnerSide = ownerSide };
                State.GameManager.TacticalMode.ActiveEffects[position] = effect;
                switch (type)
                {
                    case TileEffectType.Fire:
                        State.GameManager.TacticalMode.EffectTileMap.SetTile(new Vector3Int(position.x, position.y, 0), State.GameManager.TacticalMode.Pyre);
                        break;
                    case TileEffectType.IcePatch:
                        State.GameManager.TacticalMode.EffectTileMap.SetTile(new Vector3Int(position.x, position.y, 0), State.GameManager.TacticalMode.Ice);
                        break;
                }
            }
        }

    }

    static internal void CreateEffectWithPattern(Vec2i location, Vec2i unit, TileEffectType type, float strength, int duration, int[,] pattern, AreaOfEffectType aoeType)
    {
        List<Vec2i> tile_positions;
        int target_box = (int)((Math.Sqrt(pattern.Length) / 2) - 0.5);
        if (aoeType == AreaOfEffectType.RotatablePattern)
        {
            tile_positions = rotateTilePattern(location, pattern, target_box, GetRotatingOctant(unit, location));
        }
        else
            tile_positions = TilesOnPattern(location, pattern, target_box);
        foreach (Vec2i position in tile_positions)
        {
            TileEffect effect = new TileEffect(duration, strength, type);
            State.GameManager.TacticalMode.ActiveEffects[position] = effect;
            switch (type)
            {
                case TileEffectType.Fire:
                    State.GameManager.TacticalMode.EffectTileMap.SetTile(new Vector3Int(position.x, position.y, 0), State.GameManager.TacticalMode.Pyre);
                    break;
                case TileEffectType.IcePatch:
                    State.GameManager.TacticalMode.EffectTileMap.SetTile(new Vector3Int(position.x, position.y, 0), State.GameManager.TacticalMode.Ice);
                    break;
            }
        }
        

    }

    static public bool IsUnitControlledBySide(Unit unit, int side)
    {
        if (GetMindControlSide(unit) != -1)
            return false;
        return side == unit.FixedSide;
    }

    static public bool PlayerCanSeeTrueSide(Unit unit)
    {
        if (!unit.hiddenFixedSide || unit.FixedSide == unit.Side) return true;
        return unit.FixedSide == (!State.GameManager.TacticalMode.AIAttacker ?
            State.GameManager.TacticalMode.GetAttackerSide() : (!State.GameManager.TacticalMode.AIDefender ? State.GameManager.TacticalMode.GetDefenderSide() : unit.FixedSide));
    }

    static public bool UnitCanSeeTrueSideOfTarget(Unit viewer, Unit target)
    {
        return !target.hiddenFixedSide || target.FixedSide == target.Side;
    }

    internal static void ForceFeed(Actor_Unit actor, Actor_Unit targetPred, bool DisablecontrolBypass = true)
    {
        float r = (float)State.Rand.NextDouble();
        if (targetPred.Unit == actor.Unit)
        {
            State.GameManager.TacticalMode.Log.RegisterMiscellaneous($"{actor.Unit.Name} cannot consume itself.");
            actor.Movement = 0;
        }
        else if (targetPred.Unit.Predator)
        {
            PreyLocation preyLocation = PreyLocation.stomach;
            var possibilities = new Dictionary<string, PreyLocation>();
            possibilities.Add("Maw", PreyLocation.stomach);

            if (targetPred.Unit.CanAnalVore && State.RaceSettings.GetVoreTypes(targetPred.Unit.Race).Contains(VoreType.Anal)) possibilities.Add("Anus", PreyLocation.anal);
            if (targetPred.Unit.CanBreastVore && State.RaceSettings.GetVoreTypes(targetPred.Unit.Race).Contains(VoreType.BreastVore)) possibilities.Add("Breast", PreyLocation.breasts);
            if (targetPred.Unit.CanCockVore && State.RaceSettings.GetVoreTypes(targetPred.Unit.Race).Contains(VoreType.CockVore)) possibilities.Add("Cock", PreyLocation.balls);
            if (targetPred.Unit.CanUnbirth && State.RaceSettings.GetVoreTypes(targetPred.Unit.Race).Contains(VoreType.Unbirth)) possibilities.Add("Pussy", PreyLocation.womb);
            if (targetPred.Unit.CanTailVore && State.RaceSettings.GetVoreTypes(targetPred.Unit.Race).Contains(VoreType.TailVore)) possibilities.Add("Tail", PreyLocation.tail);
            if (targetPred.Unit.CanBladderVore && State.RaceSettings.GetVoreTypes(targetPred.Unit.Race).Contains(VoreType.BladderVore)) possibilities.Add("Bladder", PreyLocation.bladder);

            if (State.GameManager.TacticalMode.IsPlayerInControl && State.GameManager.CurrentScene == State.GameManager.TacticalMode && possibilities.Count > 1 && DisablecontrolBypass)
            {
                var box = State.GameManager.CreateOptionsBox();
                box.SetData($"Which way do you want to enter?", "Maw", () => targetPred.PredatorComponent.ForceConsume(actor, preyLocation), possibilities.Keys.ElementAtOrDefault(1), () => targetPred.PredatorComponent.ForceConsume(actor, possibilities.Values.ElementAtOrDefault(1)), possibilities.Keys.ElementAtOrDefault(2), () => targetPred.PredatorComponent.ForceConsume(actor, possibilities.Values.ElementAtOrDefault(2)), possibilities.Keys.ElementAtOrDefault(3), () => targetPred.PredatorComponent.ForceConsume(actor, possibilities.Values.ElementAtOrDefault(3)), possibilities.Keys.ElementAtOrDefault(4), () => targetPred.PredatorComponent.ForceConsume(actor, possibilities.Values.ElementAtOrDefault(4)));
                actor.Movement = 0;
            }
            else
            {
                preyLocation = possibilities.Values.ToList()[State.Rand.Next(possibilities.Count)];
                actor.Movement = 0;
                targetPred.PredatorComponent.ForceConsumeAuto(actor);
            }
        }
        else
        {
            State.GameManager.TacticalMode.Log.RegisterMiscellaneous($"{actor.Unit.Name} failed to enter {targetPred.Unit.Name}.");
            actor.Movement = 0;
        }
    }
    internal static void AssumeForm(Actor_Unit actor, Actor_Unit target)
    {
        actor.ChangeRacePrey();
    }
    internal static void RevertForm(Actor_Unit actor, Actor_Unit target)
    {
        actor.RevertRace();
    }

    internal static void ShapeshifterPanel(Actor_Unit selectedUnit)
    {
        //int children = UnitPickerUI.ActorFolder.transform.childCount;
        //for (int i = children - 1; i >= 0; i--)
        //{
        //    UnityEngine.Object.Destroy(UnitPickerUI.ActorFolder.transform.GetChild(i).gameObject);
        //}
        //foreach (Unit shape in selectedUnit.Unit.ShifterShapes)
        //{
        //    GameObject obj = UnityEngine.Object.Instantiate(UnitPickerUI.HiringUnitPanel, UnitPickerUI.ActorFolder);
        //    UIUnitSprite sprite = obj.GetComponentInChildren<UIUnitSprite>();
        //    Actor_Unit actor = new Actor_Unit(new Vec2i(0, 0), shape);
        //    sprite.UpdateSprites(actor);
        //    Text text = obj.transform.GetChild(3).GetComponent<Text>();
        //    text.text = 
        //        $"Items: {shape.GetItem(0)?.Name} {shape.GetItem(1)?.Name}" + (shape.HasTrait(Traits.Resourceful) ? $" { shape.GetItem(2)?.Name}" : "") + "\n" +
        //        $"Str: {shape.GetStatBase(Stat.Strength)} Dex: {shape.GetStatBase(Stat.Dexterity)} Agility: {shape.GetStatBase(Stat.Agility)}\n" +
        //        $"Mind: {shape.GetStatBase(Stat.Mind)} Will: {shape.GetStatBase(Stat.Will)} Endurance: {shape.GetStatBase(Stat.Endurance)}\n";
        //    if (shape.Predator)
        //        text.text += $"Vore: {shape.GetStatBase(Stat.Voracity)} Stomach: {shape.GetStatBase(Stat.Stomach)}";
        //    sprite.Name.text = InfoPanel.RaceSingular(shape);
        //    Button button = obj.GetComponentInChildren<Button>();
        //    button.GetComponentInChildren<Text>().text = "Transform";
        //    button.onClick.AddListener(() => selectedUnit.Shapeshift(shape));
        //    button.onClick.AddListener(() => UnitPickerUI.gameObject.SetActive(false));
        //}
        //UnitPickerUI.ActorFolder.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 300 * (1 + (selectedUnit.Unit.ShifterShapes.Count / 3)));
        //UnitPickerUI.GetComponentInChildren<HirePanel>().GetComponentInChildren<Button>().GetComponentInChildren<Text>().text = "Cancel";
        //UnitPickerUI.gameObject.SetActive(true);
    }

    internal static bool IsPreyEndoTargetForUnit(Prey preyUnit, Unit unit)
    {
        return (unit.HasTrait(Traits.FriendlyStomach) && (preyUnit.Unit.FixedSide == unit.GetApparentSide(preyUnit.Unit))) || unit.HasTrait(Traits.Endosoma) && preyUnit.Unit.IsDead == false;
    }
}


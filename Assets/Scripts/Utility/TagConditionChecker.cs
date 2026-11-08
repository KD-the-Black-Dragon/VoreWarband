// 2026-11-06: Preserve per-character Unit Tag definitions and associations (spec 40A).
// Modified for the VoreWar mercenary-company conversion.
// 2026-11-03: Remove permanently retired identity branches/entries while preserving shared behavior (spec 21).
// 2026-11-03: Correct legacy category conditions and retain trait-derived tags without species overrides (spec 22).
// See CHANGES.md and License.txt for GPL-3.0 terms.
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TagConditionChecker
{
    public static void CompileTraitTagAssociateDict()
    {
        Dictionary<Traits, List<int>> tagTraitRelation = new Dictionary<Traits, List<int>>();
        foreach (UnitTag unitTag in State.UnitTagList)
        {
            foreach (Traits trait in unitTag.AssociatedTraits)
            {
                if (tagTraitRelation.ContainsKey(trait))
                {
                    if (!tagTraitRelation[trait].Contains(unitTag.id))
                        tagTraitRelation[trait].Add(unitTag.id);
                }
                else 
                {
                    tagTraitRelation.Add(trait, new List<int>());
                    tagTraitRelation[trait].Add(unitTag.id);
                }
            }
        }
        State.UnitTagAssociatedTraitDictionary = tagTraitRelation;
    }

    public static float ApplyTagEffect(Unit owner, Unit target, UnitTagModifierEffect modifierEffect) 
    {
        float mult = 1;
        foreach (UnitTag tag in GetCombinedUnitTags(owner))
        {
            foreach (UnitTagModifier modifier in tag.modifiers.Where(m => m.tagEffect == modifierEffect))
            {
                if (IsModifierActive(owner,target,modifier, false))
                {
                    mult *= modifier.modifierValue;
                }
            }
        }

        foreach (UnitTag tag in GetCombinedUnitTags(target))
        {
            foreach (UnitTagModifier modifier in tag.modifiers.Where(m => m.tagEffect == modifierEffect))
            {
                if (IsModifierActive(target, owner,modifier, true))
                {
                    mult *= modifier.modifierValue;
                }
            }
        }
        return mult;
    }

    public static bool IsModifierActive(Unit owner, Unit target, UnitTagModifier modifier, bool incoming) 
    {
        if (incoming)
        {
            switch (modifier.tagCase)
            {
                case UnitTagModifierCase.FromTargetIf:
                    return ModifierTargetCheck(owner, target, modifier);
                case UnitTagModifierCase.FromTargetIfNot:
                    return !ModifierTargetCheck(owner, target, modifier);
                case UnitTagModifierCase.FromAnyTarget:
                    return true;
                case UnitTagModifierCase.FromAnyIfSelfIs:
                    return ModifierTargetCheck(owner, owner, modifier);
                case UnitTagModifierCase.FromAnyIfSelfIsNot:
                    return !ModifierTargetCheck(owner, owner, modifier);
                default:
                    return false;
            }
        }
        else 
        {

            switch (modifier.tagCase)
            {
                case UnitTagModifierCase.AgainstTargetIf:
                    return ModifierTargetCheck(owner, target, modifier);
                case UnitTagModifierCase.AgainstTargetIfNot:
                    return !ModifierTargetCheck(owner, target, modifier);
                case UnitTagModifierCase.AgainstAnyTarget:
                    return true;
                case UnitTagModifierCase.AgainstAnyIfSelfIs:
                    return ModifierTargetCheck(owner, owner, modifier);
                case UnitTagModifierCase.AgainstAnyIfSelfIsNot:
                    return !ModifierTargetCheck(owner, owner, modifier);
                default:
                    return false;
            }
        }
    }

    public static bool ModifierTargetCheck(Unit owner, Unit target, UnitTagModifier modifier) 
    {
        switch (modifier.target)
        {
            case UnitTagModifierTarget.All:
                return true;
            case UnitTagModifierTarget.IsRace:
                return target.Race == (Race)modifier.targetValue;
            case UnitTagModifierTarget.HasTrait:
                return target.HasTrait((Traits)modifier.targetValue);
            case UnitTagModifierTarget.HasTag:
                return GetCombinedUnitTags(target).Any(t => t.id == modifier.targetValue);
            case UnitTagModifierTarget.IsGender:
                return target.GetGender() == (Gender)modifier.targetValue;
            case UnitTagModifierTarget.HasStatusEffect:
                return target.StatusEffects.Any(s => s.Type == (StatusEffectType)modifier.targetValue);
            case UnitTagModifierTarget.HasAnyStatusEffect:
                return target.StatusEffects.Any();
            case UnitTagModifierTarget.IsUnitType:
                return target.Type == (UnitType)modifier.targetValue;
            case UnitTagModifierTarget.IsEnemy:
                return target.IsEnemyOfSide(owner.Side);
            case UnitTagModifierTarget.IsAlly:
                return !target.IsEnemyOfSide(owner.Side);
            case UnitTagModifierTarget.HasRanged:
                return target.GetBestRanged() != null;
            case UnitTagModifierTarget.HasMelee:
                return target.GetBestMelee() != State.World.ItemRepository.Claws || target.HasTrait(Traits.Feral);
            case UnitTagModifierTarget.HasSpells:
                return target.UseableSpells != null;
            case UnitTagModifierTarget.IsEmpireRace:
                return (int)target.Race >= 0 && (int)target.Race < 50;
            case UnitTagModifierTarget.IsMercenaryRace:
                return (int)target.Race >= 50 && (int)target.Race < 100;
            case UnitTagModifierTarget.IsMonsterRace:
                return (int)target.Race >= 100 && (int)target.Race < 170;
            case UnitTagModifierTarget.IsSpecialMercenaryRace:
                return (int)target.Race >= 170;
            case UnitTagModifierTarget.IsPredator:
                return target.Predator;
            case UnitTagModifierTarget.IsFull:
                var actor = TacticalUtilities.Units.FirstOrDefault(s => s.Unit == target);
                if (actor != null && actor.PredatorComponent != null)
                {
                    if (actor.PredatorComponent.PreyCount >= 1)
                        return true;
                }
                break;
            case UnitTagModifierTarget.IsDigesting:
                actor = TacticalUtilities.Units.FirstOrDefault(s => s.Unit == target);
                if (actor != null && actor.PredatorComponent != null)
                {
                    if (actor.PredatorComponent.AlivePrey >= 1)
                        return true;
                }
                break;
            case UnitTagModifierTarget.IsAbsorbing:
                actor = TacticalUtilities.Units.FirstOrDefault(s => s.Unit == target);
                if (actor != null && actor.PredatorComponent != null)
                {
                    if (actor.PredatorComponent.PreyCount > actor.PredatorComponent.AlivePrey)
                        return true;
                }
                break;
            case UnitTagModifierTarget.IsGreaterLevel:
                return target.Level > owner.Level;
            case UnitTagModifierTarget.IsGreaterBodySize:
                return target.GetScale() > owner.GetScale();
            case UnitTagModifierTarget.HasGreaterStatTotal:
                return target.GetStatTotal() > owner.GetStatTotal();
            case UnitTagModifierTarget.HasGreaterHealthPct:
                return target.HealthPct > owner.HealthPct;
            case UnitTagModifierTarget.HasGreaterMandPct:
                return target.ManaPct > owner.ManaPct;
            case UnitTagModifierTarget.IsOverHealthPct:
                return target.HealthPct > modifier.targetValue/100;
            case UnitTagModifierTarget.IsOverManaPct:
                return target.ManaPct > modifier.targetValue / 100;
            case UnitTagModifierTarget.HasBarrier:
                return target.Barrier > 0;
            case UnitTagModifierTarget.HasGreaterBarrier:
                return target.Barrier > owner.Barrier;
            default:
                break;
        }
        return false;
    }

    public static List<UnitTag> GetCombinedUnitTags(Unit unit) 
    {
        List<UnitTag> combinedTagLsit = new List<UnitTag>();

        var definitions = unit.SpeciesBalance == null ? State.UnitTagList : unit.SpeciesBalance.UnitTags;
        var ids = unit.SpeciesSettings.GetRaceTags(unit.Race) ?? new List<int>();
        foreach (var definition in definitions)
            if (ids.Contains(definition.id) || definition.AssociatedTraits.Any(unit.GetTraits.Contains))
                combinedTagLsit.Add(definition);

        return combinedTagLsit.Distinct().ToList();
    }

}
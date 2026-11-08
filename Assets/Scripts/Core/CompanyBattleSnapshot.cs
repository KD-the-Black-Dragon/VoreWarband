// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-07: Preserve battle evidence and settled post-battle events across saves (spec 40D).
// 2026-11-06: Verify hazard ownership survives battle snapshot restoration (spec 40C).
// 2026-11-03: Validate canonical company/battle graph identity and exact tactical snapshot preservation (spec 19).
// See CHANGES.md and License.txt for GPL-3.0 terms; inherited notices remain applicable.
using System;
using System.Linq;

internal static class CompanyBattleSnapshot
{
    internal static void Bind(CompanyCampaign state)
    {
        if (!state.InBattle || state.Battle == null || state.Battle.Tactical == null) return;
        foreach (var actor in state.Battle.Tactical.units)
        {
            actor.Unit.BattleActive = true;
            if (actor.Unit.CompanyLoadout) actor.Unit.BindCompanyItems(CompanyEquipment.Repository);
        }
    }
    internal static void Validate(CompanyCampaign state)
    {
        if (state.Battle != null && (state.Battle.EvidenceVersion < 0 || state.Battle.EvidenceVersion > 1 || state.Battle.InitialEnemyCount < 0 ||
            (state.Battle.EvidenceVersion == 1 && state.Battle.InitialEnemyCount < 1)))
            throw new InvalidOperationException("Invalid saved post-battle evidence version.");
        if (state.Battle != null && (state.Battle.Phase == CompanyBattlePhase.PostBattleEvent && state.Battle.PostBattleEvent == null ||
            state.Battle.PostBattleEvent != null && (!state.Battle.PostBattleEvent.HasTraitChange || string.IsNullOrEmpty(state.Battle.PostBattleEvent.SubjectId) ||
                string.IsNullOrEmpty(state.Battle.PostBattleEvent.Description) || !state.Characters.Any(c => c.Id == state.Battle.PostBattleEvent.SubjectId))))
            throw new InvalidOperationException("Invalid saved post-battle event.");
        if (!state.InBattle) return;
        var battle = state.Battle;
        if (battle == null || battle.Tactical == null || battle.Tactical.units == null || battle.RandomState == null || battle.DeployedIds == null)
            throw new InvalidOperationException("Incomplete battle save.");
        if (battle.DeployedIds.Any(id => !battle.Tactical.units.Any(a => ReferenceEquals(a.Unit, state.Characters.Single(c => c.Id == id).Unit))))
            throw new InvalidOperationException("Battle characters are not bound to the company registry.");
        if (battle.EvidenceVersion == 1 && battle.Tactical.units.Any(a => !a.Evidence.IsValid()))
            throw new InvalidOperationException("Invalid saved battle evidence.");
        foreach (var id in CompanyBattleSession.EnemyIds(state, battle).Except(CompanyBattleSession.DefeatedIds(state, battle)))
            if (!battle.Tactical.units.Any(a => ReferenceEquals(a.Unit, state.Characters.Single(c => c.Id == id).Unit)))
                throw new InvalidOperationException("Battle enemies are not bound to their campaign records.");
        foreach (var actor in battle.Tactical.units.Where(a => a.SelfPrey != null))
            if (!ReferenceEquals(actor.SelfPrey.Actor, actor) || !ReferenceEquals(actor.SelfPrey.Unit, actor.Unit) || !battle.Tactical.units.Contains(actor.SelfPrey.Predator))
                throw new InvalidOperationException("Invalid saved containment links.");
    }
    internal static void RequirePreserved(CompanyCampaign source, CompanyCampaign copy)
    {
        if (source.InBattle != copy.InBattle) throw new InvalidOperationException("Battle phase changed while saving.");
        if ((source.Battle == null) != (copy.Battle == null)) throw new InvalidOperationException("Battle record changed while saving.");
        if (source.Battle != null && (source.Battle.Id != copy.Battle.Id || source.Battle.Phase != copy.Battle.Phase ||
            source.Battle.EvidenceVersion != copy.Battle.EvidenceVersion || source.Battle.InitialEnemyCount != copy.Battle.InitialEnemyCount ||
            source.Battle.Summary != copy.Battle.Summary ||
            !(source.Battle.DeployedIds == null ? copy.Battle.DeployedIds == null : copy.Battle.DeployedIds != null && source.Battle.DeployedIds.SequenceEqual(copy.Battle.DeployedIds)) ||
            !CompanyPostBattleEvent.Same(source.Battle.PostBattleEvent, copy.Battle.PostBattleEvent)))
            throw new InvalidOperationException("Battle result or post-battle event changed while saving.");
        if (!source.InBattle) return;
        Validate(copy);
        var a = source.Battle.Tactical; var b = copy.Battle.Tactical;
        if (a.currentTurn != b.currentTurn || a.activeSide != b.activeSide || a.attackersTurn != b.attackersTurn ||
            a.units.Count != b.units.Count || !source.Battle.RandomState.SequenceEqual(copy.Battle.RandomState) || source.Battle.UnityRandomState != copy.Battle.UnityRandomState)
            throw new InvalidOperationException("Battle progress changed while saving.");
        if ((a.tiles == null) != (b.tiles == null) || (a.tiles != null &&
            (a.tiles.GetLength(0) != b.tiles.GetLength(0) || a.tiles.GetLength(1) != b.tiles.GetLength(1) || !a.tiles.Cast<TacticalTileType>().SequenceEqual(b.tiles.Cast<TacticalTileType>()))))
            throw new InvalidOperationException("Battle terrain changed while saving.");
        if ((a.activeEffects == null) != (b.activeEffects == null) || (a.activeEffects != null &&
            (a.activeEffects.Count != b.activeEffects.Count || a.activeEffects.Any(p => !b.activeEffects.ContainsKey(p.Key) ||
             p.Value.OwnerSide != b.activeEffects[p.Key].OwnerSide || p.Value.Type != b.activeEffects[p.Key].Type || p.Value.Strength != b.activeEffects[p.Key].Strength || p.Value.RemainingDuration != b.activeEffects[p.Key].RemainingDuration))))
            throw new InvalidOperationException("Battle tile effects changed while saving.");
        for (int i = 0; i < a.units.Count; i++)
        {
            var x = a.units[i]; var y = b.units[i];
            if (!CompanyBattleEvidence.Same(x.Evidence, y.Evidence))
                throw new InvalidOperationException("Battle evidence changed while saving.");
            if (x.Position.x != y.Position.x || x.Position.y != y.Position.y || x.Movement != y.Movement ||
                x.Unit.Health != y.Unit.Health || x.Unit.TemporaryHealth != y.Unit.TemporaryHealth || x.Unit.Downed != y.Unit.Downed ||
                x.Unit.Digested != y.Unit.Digested || x.Fled != y.Fled || (x.SelfPrey == null) != (y.SelfPrey == null) ||
                (x.SelfPrey != null && (x.SelfPrey.TurnsDigested != y.SelfPrey.TurnsDigested || x.SelfPrey.Location != y.SelfPrey.Location)))
                throw new InvalidOperationException("Battle unit state changed while saving.");
            if (x.Unit.Mana != y.Unit.Mana || x.Unit.Barrier != y.Unit.Barrier || x.Unit.MaxBarrier != y.Unit.MaxBarrier ||
                x.Unit.TempBoosts.HealthBoost != y.Unit.TempBoosts.HealthBoost || x.Unit.TempBoosts.ManaBoost != y.Unit.TempBoosts.ManaBoost || x.Unit.TempBoosts.ExtraAttacks != y.Unit.TempBoosts.ExtraAttacks ||
                !x.Unit.ItemUses.SequenceEqual(y.Unit.ItemUses) || !x.Unit.ItemCooldowns.SequenceEqual(y.Unit.ItemCooldowns) ||
                x.Unit.StatusEffects.Count != y.Unit.StatusEffects.Count || x.Unit.StatusEffects.Where((s, n) =>
                    s.Type != y.Unit.StatusEffects[n].Type || s.Duration != y.Unit.StatusEffects[n].Duration || s.Strength != y.Unit.StatusEffects[n].Strength).Any())
                throw new InvalidOperationException("Battle resources or effects changed while saving.");
        }
    }
}

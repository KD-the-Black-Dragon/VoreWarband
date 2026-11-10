# 10 — Deployment, battle results, revival, and memorial

**Status:** Implemented as grouped09–10 with provisional tuning, 2026-11-01; agent compile/scene/source checks passed; one connected runtime review pending. See [implementation and boundaries](../implementation/09-10-contract-and-battle-loop.md). Injury/recovery work remains11.

**Phase:** Phase 3 — complete the first company campaign loop

**Prerequisites:** 07 and 09; 05 combat boundary and 06 identity/save model.

## Purpose and scope

### Preparation from study 01C

The [combat/result study](../architecture/01c-combat-equipment-and-results.md) and [damage/outcome candidates](../architecture/01c-damage-outcome-inventory.md) identify these required integration points:

- Legacy automatic surrender is conditional and restores HP to one; traits and settings can also change sides, heal, or regurgitate. Implement the agreed downed behavior explicitly rather than merely relabeling every surrender path. Remove voluntary case-9 and skip/simulation surrender entry paths as appropriate.
- Review AllSurrenderedCheck, CalculateRemaining, and the 500-round safeguard: they can clear surrender or force deaths. Distinguish combat-capable deployed members, contained living members, reserves, and permanent losses; do not derive company wipeout from the old army count.
- Preserve the actual retreat interaction: attacker top edge, defender bottom edge, movement/trait checks and special warp behavior. Replace ProcessTravelingUnits' old village/invisible-army routing with the company destination; fled enemies leave the contract as defeated.
- Legacy retreated predators can still carry prey and continue digestion. The new design forbids enemies from fleeing with captives: release first or block flight, including AI/forced/special paths. Containment alone is not death; record settled outcomes and final cause before cleanup removes or rewrites actors.
- VictoryCheck both detects and mutates results and can be reentered by dialogs. Separate result settlement, company reconciliation, summary, and enabled revival, with an identity/token reconciled with packet 01D's save design.
- Permanently deprecate legacy resurrection/reincarnation/replacement/saved-copy effects that bypass loss. No exceptions are requested. Ordinary trait/spell healing may instead grant temporary combat HP; retire automatic full-heal finalization. The leader's unique paid service is separate.
- Unit.Kill is bookkeeping, not a universal HP setter. With revival disabled, observe completed leader digestion for immediate game over; downing alone is survivable and must not trigger game over/revival. See [the clarified decisions](../design-clarifications-2026-10-30.md).
- Existing loot transfers stock and certain drops; it does not define a new full-equipment-drop policy. Contract rewards remain gold only initially. Preserve the distinct Pure Tactical return without campaign reconciliation.

### Agreed scope

- Moving onto a contract target triggers a final deployment review every time, showing generated enemies and held weapons and allowing last-minute active/reserve changes.
- Deploy up to eight active members and always include the leader; preserve terrain selection, enemy tactical AI, retained vore/digestion mechanics, and edge-of-map retreat.
- Replace all surrendered states with downed, including fear/pain effects above 0 HP. Reaching 0 HP always downs persistent units rather than killing them. Downed units cannot act or receive commands for the rest of battle, even if healed. Summons disappear rather than becoming downed. Remove voluntary surrender controls in both modes.
- Ordinary attacks do not directly target downed units; collateral damage cannot permanently kill them. Consumption succeeds against downed units with 100% chance. Preserve the existing simulated battle resolution and make AI prioritize consuming downed opponents during cleanup.
- Downed/contained living units keep the battle running until a side is gone through final death/digestion/flight. Downed company members facing standing enemies remain in the battle for actual simulated consumption/digestion; no assumed off-screen death, abandonment state, or immediate retreat-loss reconciliation. The player can delegate the remainder to existing AI simulation.
- Declare stalemate when both sides have zero active units or the battle cannot finish after an excessively generous turn limit (200 is illustrative). All remaining units count as fled and go home, including downed survivors; do not force death or recovery of command ability to end the stall. Apply the enemy captive-release restriction before flight.
- Return battle outcomes to the same persistent company members once. Digestion is the sole means of permanently removing an otherwise persistent character. Surviving downed members, including the leader and stalemate survivors, retain injuries and receive a modest post-battle HP baseline after temporary HP clears (10%/20% are examples). This supersedes every earlier permanent loss on being left behind.
- Resolve fleeing enemies as defeated for contracts, including on a draw. If the target is fully defeated this way, remove it and grant normal automatic gold/reputation once; a draw is not an exception that preserves the target for retry. Settle actual member outcomes, earned contract rewards, and the results summary before any enabled leader revival transition. A draw label alone must neither imply company deaths nor suppress earned contract rewards.
- With revival disabled, completed leader digestion ends the campaign immediately. With revival enabled and company survivors (including reserves), finish battle then move to the nearest town via cutscene, charge base fee times level even if funds are insufficient, and restore the leader fully intact at full HP. This advances no campaign time. Merely downed leaders need neither revival nor game over; immortality/revival is unique to the leader.
- Support retreat to a nearby location and retryable surviving targets. Handle company wipeout and record fallen members' actual final fates in the memorial.
- Maintain campaign statistics for the agreed end summary; Pure Tactical results are discarded and do not change campaign records.

### Preparation from study 01D

[01D](../architecture/01d-campaign-persistence-and-identity.md) confirms existing TacticalData/Export/Import as an adaptation starting point, not proof of new-rule persistence. Save explicit deployed-member bindings, battle ID, active/determined/applied/summary/revival/returned phase, actual digestion causes, and the new HP/downed/summon/stalemate state. The health and snapshot contract for separate lasting/temporary HP must be established with 11 before dependent combat changes, rather than leaving a core invariant until after results implementation.

Do not serialize pending dialog callbacks as the authority for money/death/revival. Apply recorded result changes transactionally or persist progress with them; one flag written after partly mutating a roster is insufficient. Loading must rebuild the same member identities and shared battle/prey references without calling legacy strategic cleanup. Review both ordinary and turbo turn boundaries and immediate leader-digestion ending for Ironman support.

## Completion and review criteria

- Review victory, retreat, complete active-force defeat with reserves, leader digestion with revival on/off, leader merely downed, summons disappearing, collateral nonlethality, captive-release/blocking before enemy retreat, draws, and total company wipeout.
- Every result reconciles once; rewards, kills, memorial records, and losses cannot duplicate on return/loading.
- Revival occurs after settled results and grants no extra day updates; insufficient funds cannot strand the campaign in an unfinished transition.
- Fallen records exclude quitters/dismissals. Summary covers reputation, earned gold, hires, memorialized losses, contracts, survived days, and enemies defeated.
- Pure Tactical exits cleanly without retaining company state. Normal battle return never starts a legacy empire turn.

## Decisions, constraints, and risks

- Confirm revival treasury behavior when short of gold; do not invent revival debt.
- Set nearest/reachable-town and retreat-location rules, defeated target state, and handling of reserves if no active unit escapes.
- Clarify temporary injuries after revival when 11 is implemented; permanent injuries are forbidden for the leader.
- Set the final generous stalemate turn limit; the two eligibility cases, survivor return, absence of abandonment loss, and contract treatment are agreed. Keep battle result labels separate from contract completion and company survival.
- Ordinary battlefield gear loot remains undecided; the user leans toward destruction with digested members. Contract rewards are still gold-only.

## Deliverable

A repeatable accept/travel/fight/get-paid/manage loop, deployment/results, revival, memorial, and company-ending flow.

## Execution record

- Source trace, actual implementation, numeric choices and save boundaries: [grouped09–10 report](../implementation/09-10-contract-and-battle-loop.md).
- User accepted07–08, authorized the next grouped milestone and closed Unity for checks. No new species inferred.
- Final compilation, explicit/read-only scene opening and source/resource checks passed. Exact evidence is recorded in the report/checkpoint; actual battle outcomes and nonempty result/revival round-trips await the combined player review. No Play-mode or balance pass claimed.

For subsequent work, refine this spec from actual runtime findings and resolve decisions needed for the next milestone. Follow the review and file-operation rules in [the roadmap](../conversion-roadmap.md). Design intent comes from [the campaign plan](../mercenary-company-campaign-plan.md) and the three curation checklists.



## Current save and HP boundary

Temporary/lasting HP and permanent downed behavior are implemented with this milestone because results depend on them. Company battle saving uses the user-approved between-battle fallback: saved deployment, then saved summary/revival/return phases. No tactical-turn persistence is claimed. Revival fee20×level, HP baseline15% and200turn stalemate are provisional. Zero gold when short is the specified treasury behavior; no revival debt. Retained complex trait/copy interactions remain integratedMVP regression work.

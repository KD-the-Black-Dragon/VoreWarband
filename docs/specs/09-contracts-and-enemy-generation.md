# 09 — Contracts, reputation, and procedural enemy power

**Status:** Implemented as grouped09–10 with provisional tuning, 2026-11-01; agent compile/scene/source checks passed; one connected runtime review pending. See [implementation and boundaries](../implementation/09-10-contract-and-battle-loop.md). Injury/recovery work remains11.

**Phase:** Phase 3 — build the first company campaign loop

**Prerequisites:** 06–08; use the combat boundary from 05.

## Purpose and scope

Study [01C](../architecture/01c-combat-equipment-and-results.md) identified ArmyPower and UnitValue as existing approximations, not the new contract rating model. ArmyPower multiplies its sum by member count and includes leadership, level, weapon, scale, and race adjustment terms. Reusing it directly would import unintended headcount/leadership assumptions, especially for swarm forces. AI also uses it for retreat evaluation: review that coupling rather than silently replacing AI policy when introducing a contract formula. Account for gear bonuses already baked into stored stats so the rating model does not score them twice.

- Add separate replenishing town offer pools, several equal-priority active contracts, a persistent map-side list, and exact target markers even under fog.
- Implement only rout-enemy contracts initially with fixed generous deadlines, expiration/abandonment penalties, target removal at expiration, and exactly three easy starting contracts whose targets are 5–10 Manhattan tiles from the starting town.
- Define a transparent numerical power model covering unit level, stats, traits/abilities, and equipment; generate enemy groups to a tight target band with varied composition and no more than eight units, including approved swarm profiles. Early forces should reach a preferred mix of up to four sapients and two animals before spending the rating budget on stronger levels, role traits, weapons, or armor. Keep sapient members in a narrow level band so a squad does not mix widely separated levels.
- Use fixed difficulty independent of company strength. Bound generation attempts and provide a safe failure result when disabled species or available gear cannot satisfy a rating.
- Quote numeric difficulty and exact rating-based gold with modest variation rounded to tens. Reward gold only, automatically on success; no return trip.
- Implement visible company reputation: completion always increases it, only contract outcomes change it, difficulty scales failure loss, and reputation continuously widens eligible contract/recruit power.
- Show only contract rating, reward, deadline and target in town/active-contract lists; do not create or expose an enemy roster when offers are generated or accepted.
- When the company reaches an active contract target, generate its enemy force once and show it in final deployment. Keep the generated characters, injuries, health, equipment and casualties in the contract save state so a failed attempt reuses the surviving force instead of rerolling it.
- Keep the quoted contract rating and reward fixed. Existing company power/reputation/calendar offer selection still determines the accepted rating; do not add an unapproved arrival-time multiplier as part of this visibility/lifecycle change.
- Failed attempts preserve casualties and surviving enemies' wounds. The stationary map army recovers HP at a typical daily rate; reuse applicable healing rules without restoring empire simulation, replacing casualties, or rebuilding the force to its original power budget. See [the clarification record](../design-clarifications-2026-10-30.md).
- Fleeing target enemies leave the game and count as defeated even on a stalemate/draw. When all target members have been defeated, grant the normal automatic contract reward/reputation once; do not preserve a retryable target merely because the battle result says draw. Retry persistence applies only where an actual surviving target remains.

### Preparation from study 01D

[The final study](../architecture/01d-campaign-persistence-and-identity.md) makes targets first-class saved records: target/contract/enemy IDs, actual surviving roster/wounds, location, deadline, and already quoted/generated reward. Loading or reopening a town must not reroll these. Daily target recovery uses selected health rules, not all of Army.Refresh/Empire.Regenerate. Separate contract completion from the battle's victory/draw label, and apply outcomes through the same once-only battle/result identity as 10.

## Completion and review criteria

- Targets and rewards match their quoted offers; the generator explains its computed power and remains within the chosen tolerance.
- Weak contracts remain weak for a strong company; reward does not scale with company size.
- Expiration/abandonment removes the correct target and applies one penalty; failed attempts can be retried before the deadline.
- Empty eligibility pools and unreachable targets are handled without an endless generation loop or unapproved fallback.

## Decisions, constraints, and risks

- Power weights, rating scale, tolerance, payout curve, offer refresh, deadline length, and reputation gain formula require tuning decisions.
- Computed power needs playtesting: arithmetic alone does not establish difficulty, especially with vore/digestion interactions.
- Define exact enemy daily recovery rate and ensure save/load retains the agreed casualty/wound state after failed attempts.

### Follow-up implementation, 2026-11-08

- New contract forces are capped at eight. Sapient enemies generated for a contract vary within at most three consecutive levels and receive a role-matching positive tier trait from level 3 onward when no existing trait from that effect family would be overwritten. Existing level-based weapons, spellbooks, and armor provide additional quality progression. The bounded power budget now favors stronger candidates before adding additional units.
- New Spitter Slug road encounters are capped at eight; their target budget is reduced to the maximum power representable by eight slugs when needed.
- Campaign deployments are capped at eight active members, including the mandatory leader. A clear modal blocks Begin Battle when the active count is too high; the player returns to deployment and can mark additional members as reserves. The overall roster remains unlimited.
- These rules apply to newly generated forces and new deployments. Existing saved contract forces and battles are preserved rather than silently regenerated or rewritten. Values remain provisional and were not battle-calibrated in this packet.

### Preferred early force composition, 2026-11-09

- New contract-force generation first attempts up to four sapients and two animals, choosing lower-power candidates so the composition can fit within the unchanged 92–108% rating band.
- When an exact four-sapient/two-animal force from the generated candidate pool fits inside the rating band, select the valid combination closest to the rating budget midpoint before considering higher-level or better-equipped partial forces. If no exact mix fits, retain the existing bounded fallback; opening and other low-budget contracts are not forced to six enemies.
- If the target mix fits, same-kind candidate upgrades are considered before adding beyond six members. When a desired slot cannot fit under the rating cap, the generator can use another feasible candidate to meet the minimum power; the existing eight-unit maximum remains in force.
- This affects only forces generated when a campaign reaches an active contract target. Existing saved enemy forces and their retry state are unchanged. The selected composition is a preference bounded by the rating budget, not a guarantee that every rating can contain all six target slots.

### Opening contracts and progression step, updated 2026-11-09

- The starting town's first pool contains exactly three rating-1 contracts. Their targets are 5–10 Manhattan tiles from the town and at least eight tiles apart, giving a shorter opening trip while keeping the choices from clustering together. Ordinary later town pools retain four offers and their existing 2–6 tile target distance.
- After three successful contract completions, newly generated pools start at rating 2 or higher, subject to the existing reputation ceiling and company-power floor. With three uninterrupted completions, reputation reaches 3 and permits rating 2; the new floor prevents the next fresh pool from still being entirely novice work. Contract-rating budgets and rewards remain fixed; rating 1 is 75 power, while rating 2 is 105 power (+40%). Calendar pressure can still add difficulty under the accepted 40F rule.
- Existing accepted contracts and already-created offers remain unchanged. Only new offers use the progression rule; reputation loss can lower the available rating ceiling again.

### Deferred enemy roster generation, 2026-11-08

- Town offer pools now create contract metadata only. The contracts list does not include an enemy-roster button, and accepted contracts keep their quoted rating/reward while the actual characters remain ungenerated.
- Reaching an active contract target materializes the force for the first time, before final deployment. The existing pre-battle roster view remains available there; the player proceeds into the battle after revealing the force.
- An existing force is reused on every subsequent attempt. Only unaccepted legacy offer rosters are cleared during campaign rules migration; accepted/active forces and all existing battle saves are preserved.
- Force composition is generated for the accepted rating, whose offer-time selection already reflects the existing company-power, reputation and calendar rules. No new ramp coefficient was introduced in this packet.

### Boss contracts, 2026-11-09

- The three opening offers remain ordinary rout-the-force contracts. Once the opening is complete, each four-offer town refresh assigns one offer to “Rout the boss” when an enabled sapient species is available.
- Boss offers reveal only their objective, rating, reward, deadline and location. The full force is generated only when the company reaches the target and is kept unchanged for retries.
- The generated force includes one explicitly identified sapient boss. The boss is one level above the contract rating, focused on ranged combat, carries both melee and ranged weapons, and receives the level-appropriate loadout. The ordinary force generator must include the boss in the same rating power band; the boss's power is not added on top of the contract budget.
- Routing the boss means the boss is downed, digested, or flees. The objective is complete even if the player retreats or the rest of the enemy force is still standing. Routing the boss does not force the other enemies to retreat; they follow the ordinary battle AI and their actual battle outcomes are retained through settlement. After payout and post-battle event processing, the generated enemy records are removed from the completed contract. The quoted reward, completion reputation and surviving-participant contract XP are granted once.
- If the boss is not routed, existing battle rules apply: surviving enemy members persist with their actual state for another attempt. Ordinary rout-force contracts are unchanged.
- The deployment roster labels the boss. When an eligible mercenary digests the boss, the existing exceptional post-battle event system can select the boss-digestion event. Leader digestion of the boss alone causes no post-battle reaction; the separate large-digestion Grudge condition may qualify only on its own thresholds. No new narrative is authored.

## Deliverable

Contracts/reputation screens, saved targets, and a bounded procedural force generator with inspectable power calculations.

## Execution record

- Source trace, actual implementation, numeric choices and save boundaries: [grouped09–10 report](../implementation/09-10-contract-and-battle-loop.md).
- User accepted07–08, authorized the next grouped milestone and closed Unity for checks. No new species inferred.
- Final compilation, explicit/read-only scene opening and source/resource checks passed. Exact evidence is recorded in the report/checkpoint; actual battle outcomes and nonempty result/revival round-trips await the combined player review. No Play-mode or balance pass claimed.

For subsequent work, refine this spec from actual runtime findings and resolve decisions needed for the next milestone. Follow the review and file-operation rules in [the roadmap](../conversion-roadmap.md). Design intent comes from [the campaign plan](../mercenary-company-campaign-plan.md) and the three curation checklists.


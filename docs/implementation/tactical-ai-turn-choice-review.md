# Tactical AI turn-choice review

Date: 2026-11-09

## Findings

- Standard AI treated any unit with a ranged weapon as a ranged-only unit. Its turn order therefore overlooked a simultaneously equipped melee weapon, while its ranged routine shot immediately whenever a target was in range.
- When a ranged unit was adjacent to an enemy, the old fallback could either stand still and use its weak barehanded attack or reject a retreat route because any nearby opportunity attacker counted equally. It did not compare that against the value of a vore attempt or the expected ranged attack after moving.
- AI risk estimates used the same opportunity-threat list as battle resolution, even for sapients with no melee weapon. Their weak barehanded reactions still occur, but they should not have the same planning weight as a dangerous melee weapon or animal attack.
- `PotentialTarget` divided by projected damage without guarding against zero. The `lackPredators` state was calculated but never read.

The earlier 200-turn case was a capacity deadlock, not evidence that ordinary battles routinely need 200 turns. The remaining standing enemy predators could not contain the oversized downed mercenaries. The separate approved rule allowing an otherwise-empty predator to vore a downed target removed that deadlock; its replay ended in 60 rounds. This change leaves the 200-turn stalemate safety rule untouched.

## Changes

- Standard, Hedonist and race-servant combatants now compare expected melee and ranged damage, including the existing hit, graze and critical calculations, alongside available vore attempts. Their existing specialized actions still run first. A downed target receives strong cleanup priority. A sapient with a ranged weapon but no equipped melee weapon may consider a vore attempt at 25% or better; other units retain their previous prey-eligibility rules.
- All combatants can consider a reachable position before attacking while leaving at least one AP for the attack. They weigh expected weapon damage, melee spacing, movement cost and route reactions. This applies to 2 AP and 3 AP units; movement remains optional. Ranged attacks also receive a modest distance preference even when no melee threat is immediately reachable, so a unit can keep a safe firing distance as enemies approach.
- The shared planner runs after existing pounce and hedonist feeding actions, but before the ordinary spell/potion/role fallback. Far from enemies it prefers the highest-tier available Valor buff on another ally with a ranged weapon. At closer range it makes a weighted fuzzy choice among damage, the highest-tier legal ally buff and the highest-tier legal enemy debuff, using provisional 4:3:3 weights (damage:buff:debuff), normalized to the available choices. The damage option compares expected weapon and damaging-spell output; a higher-value vore plan still takes precedence. Ally support spells are not aimed at the caster and are offered only to allies without an active magic buff. If no planner choice is available, the prior fallback still gets a chance to act.
- Melee approaches that accept reactions still require a worthwhile attack reachable this turn. The AI does not accept risk to chase an out-of-reach target.
- AI-only route and payoff estimates ignore unarmed sapient reactions. Battle resolution is unchanged, so movement still triggers those attacks. Other opportunity attacks remain in AI pathfinding, charge and spacing decisions.
- Accuracy preview now accepts a proposed tile distance without moving the actor. Zero projected damage scores as zero; the unused predator scan was removed.
- A new Downed transition now immediately releases all living occupants regardless of whether the transition came from reaching zero HP or a fear/surrender effect. This is unconditional; the old chance-based duplicate checks are no longer used. Daily injury-complication checks were verified to run in the campaign's daily recovery stage and remain unchanged.

The scoring weights are initial AI heuristics, not a claim that the AI is optimal. Vore value is success chance times `0.15 × current HP + 2` to value both capture and the temporary removal of an enemy action; downed cleanup gets a large priority. Ranged spacing gains use a 0.65 weight against the strongest nearby melee threat, with a small range preference used only as a tie-breaker. Adjust these values after observing representative battles.

## Checks and follow-up

For the 2026-11-09 tactical-AI follow-up, the Unity 2018.3.12f1 native battle audit compiled and completed four synthetic 3v3 auto-resolves in 0.16–0.83 seconds, over 21–40 rounds, with no driver errors or 200-turn stalemates. Three ended as contract wins; one ranged-focused seed ended the company after three mercenary digestions (two permanent losses). These are small, headless AI-vs-AI performance samples, not a balance verdict or a player-build timing guarantee. The battle-turn autosave completed in the audit's test wait; the production battle simulation did not wait for that disk write. This does not replace in-game behavior checks:

1. Use a 2 AP and a 3 AP ranged mercenary under melee pressure; confirm a move-and-shoot turn when safer spacing is worthwhile, and a stationary shot when moving adds no useful value.
2. Give one mercenary a strong melee weapon and weak ranged weapon, then place them adjacent to an enemy; confirm they compare the melee attack instead of automatically retreating or voring.
3. Use a ranged-only sapient adjacent to an enemy with a 25%+ vore chance; compare that choice against barehanded attack and moving away.
4. Compare retreating past an unarmed sapient with retreating past a dangerous melee enemy; confirm both attacks still trigger in battle, while only the dangerous attack meaningfully deters the AI.
5. Let the AI auto-resolve a small battle with downed units remaining; confirm the existing downed cleanup and stalemate fallback still work.

## GPL-3.0 check

Each modified C# source file has a dated VoreWar conversion notice. `CHANGES.md` lists the exact source paths and this review. Actual `License.txt` sections 4–5 were checked; existing copyright, license, attribution and warranty notices remain. No code, assets or dependencies were copied from outside the project. This records the source-change checks; it does not certify the project for public distribution.

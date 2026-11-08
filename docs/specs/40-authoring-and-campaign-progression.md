# 40 — Official balance authoring and campaign progression

Planning draft, updated 2026-11-08. This official, dependency-ordered module covers six workstreams and an authoring workflow for shipping species balance changes. Spec 38 remains closed and accepted.

Implementation update, 2026-11-08. Phases 40A–40F are implemented and accepted at their recorded scope. [40B implementation](../implementation/40b-map-route-planning.md) passed 937 focused and 963 grouped native checks. [40C implementation](../implementation/40c-spellbook-progression.md) passed grouped native and save checks, simulated small seeded battles, and received grouped manual acceptance. 40D event firing was confirmed by the user; exhaustive event-by-event testing is deferred. Spell prices, combat weights, 40D thresholds and 40F calendar tuning remain provisional. 40E's company naming, retirement, provisional scoring and High Scores archive are implemented and accepted at their recorded scope; see [40E implementation](../implementation/40e-ending-scoreboard-and-history.md). [40F implementation](../implementation/40f-calendar-difficulty.md) passed grouped native checks and connected campaign simulations and was accepted by the user on 2026-11-08.

## Goals

- Give the curator a reliable way to edit species balance visually, test a draft and promote approved data into future builds.
- Add useful travel planning, broader spell progression, earned post-battle traits, durable company history and eventual calendar pressure.
- Use existing mechanics, the Battle Lab and native campaign simulations. Narrative prose and the extensive Help rewrite remain separate work.
- Deliver substantial reviewable phases, with one grouped player handoff per meaningful workflow. Internal diagnostic batches can be smaller.

## Implementation order

| Phase | Scope | Dependency and reason |
| --- | --- | --- |
| [40A](40a-official-balance-authoring.md) | Developer settings reliability and official balance authoring | Establishes reproducible shipped data, draft isolation and revision identity for subsequent tuning. |
| [40B](40b-map-route-planning.md) | World-map route planning | Uses the resolved rules and existing per-tile travel. Completes the travel throughput needed for later campaign calibration. |
| [40C](40c-spellbook-progression.md) | Spellbook variety and progression | Uses the official-data workflow. Establishes combat and equipment choices before trait-event and pressure calibration. |
| [40D](40d-post-battle-trait-progression.md) | Post-battle evidence and trait progression | Uses the retained spell/trait vocabulary and revisioned rules. Establishes the company's additional growth channel. |
| [40E](40e-ending-scoreboard-and-history.md) | Company naming, ending scoreboard, retirement and durable history | Establishes reliable terminal accounting before calendar pressure adds more end-to-end loss scenarios. |
| [40F](40f-calendar-difficulty.md) | Calendar-driven difficulty progression | Calibrates against final travel, spells, trait growth, wages and uncapped upkeep. |

The principal implementation dependencies are A before C, C before D, and the settled growth/travel systems before F. Route planning and history can be developed independently after their shared save conventions are established. The order above reduces repeated balance work and gives each phase a distinct player workflow.

## Shared requirements

1. Preserve the accepted four-stat system, AP and action economy, opportunity attacks, hit/graze/critical rules, temporary HP, injuries, digestion-only permanent losses, leader revival and separate Single Battle mode.
2. Preserve fixed contract-rating power meanings. Calendar progression must act through disclosed offer/encounter rules without silently changing an accepted force's rating or roster.
3. Keep curated species and permanent retirements authoritative. Importing a draft cannot restore retired species, OCs, traits or their artwork. Player species exclusions continue to block new generation while preserving existing individuals.
4. Version additions to company, battle and history data. Define old-save defaults and exactly-once settlement before adding persistent rewards. Preserve existing manual saves and autosave recovery.
5. Reuse Unity 2018.3.12f1 and existing dependencies. Avoid package upgrades and unrelated scene/asset changes.
6. Later implementation must include dated source notices, CHANGES.md and licensing/provenance review under AGENTS.md. No new artwork, dependency or external publication is part of this module.
7. Verification plans below are future work. Focused checks require implementation-stage authorization under the repository workflow. Fixtures must remain synthetic, retained and deletion-suppressed. Personal settings and saves are excluded from agent test inputs.

## Confirmed design answers

- Keep the password-protected Race Editor and add export/import for promoting approved defaults into future builds.
- Preserve existing characters when species defaults change. New defaults apply to newly generated individuals. This requires preserving relevant species-derived data as well as stored base stats. Global combat-formula changes need their own explicit compatibility decision.
- Produce one automatic post-battle scene recognizing the most significant qualifying experience and assigning a related trait. Existing traits influence the outcome.
- Balance positive and negative acquisitions by tier-weighted counts. Good/Bad tier I/II/III contributes 1/2/3 to its side. Upgrades replace the previous tier. Equal totals give equal chances. A difference of one gives a 75% chance toward the minority side. A difference of two gives a 100% chance toward the minority side. Larger differences should use that same 100% correction as a defensive extension.
- Classify all traits internally as Good, Bad or Neutral. Count both species and acquired personal traits for the Good/Bad ratio. Mixed-effect traits are Neutral and contribute to neither side. The user explicitly classifies Featherweight as Neutral because it combines evasion with increased damage received. Old injuries remain separate.
- There is no hard trait-count limit. When an occurrence permits multiple recipients or tied performers, favor those with fewer weighted traits; do not replace the user's random draw among occurrences in the highest eligible tier. Old injuries already add lasting penalties and have no positive counterpart.
- The leader never receives post-battle traits. Other potential leader-trait sources remain future scope.
- Use tier weights in every trait-count calculation, including subject preference and removal eligibility. Neutral traits contribute to total weight while contributing to neither Good nor Bad. Untiered traits count as one. Old injuries remain separate.
- Allow trait removal/downgrade outcomes after a sizable collection has formed, especially for opposed traits. Use eight weighted trait points as the user's provisional minimum and increase removal preference with collection size. A removal outcome reduces a tiered trait by one level, with tier I removed outright. Preserve newly acquired quirks long enough to develop character.
- Route planning uses explored terrain only.
- Magic emphasizes buffs, debuffs, manipulation, healing, protection, summoning and area denial. Direct-damage spells remain a minority. Movement prohibition is high-level magic. Summons and Gatemaw are appealing later candidates subject to source review. Area-denial AI is required for those effects.
- Players name their company at campaign start. Display identities as "[Leader Name] of [Company Name]". Ending history includes both an overall score and selectable detailed accomplishments, and retains separate endings from reloaded continuations.
- Provide voluntary retirement as a terminal outcome. The curator's provisional scoring formula and confirmed Town flow are recorded in [40E](40e-ending-scoreboard-and-history.md); no retirement bonus is currently specified.
- Calendar difficulty has a protected opening, then a gentle onset and increasingly severe acceleration. Around day 21 is an illustrative onset. Roughly 100 days is the successful-run pacing target, followed by absurdly escalating difficulty. Preserve some relatively easy training/recovery contracts while excluding offers far below company capability. The current plan applies pressure to new contracts and road encounters, interpreting the user's answer to question 10 as both.

## Decisions still open

All trait-count follow-ups are answered. Tier I/II/III counts as 1/2/3 throughout, and removal reduces a higher tier before removing its final tier. Phase 40D implements the exceptional/mid-tier/mundane occurrence catalog and approved trait classification/outcome mapping with provisional thresholds. No event is shown if no listed occurrence qualifies or has a legal outcome. Do not add injury-dependent polarity odds without approval. Boss-digestion events remain stubbed until a future explicitly boss-bearing contract variant stores boss identity rather than inferring it from enemy level.

Phase 40D's occurrence catalog and trait classification have been implemented with provisional thresholds; the user confirmed that post-battle events are firing and accepted the slice without exhaustive event-by-event testing. Phase 40E design and implementation are settled with a provisional explainable score, confirmed retirement flow, and no history backfill for old ended saves; see its phase spec. Phase 40F implements provisional simulation-informed calendar pressure on new content only, preserving accepted contracts and enrolling older saves at their current day; see its phase spec and implementation evidence. The user accepted 40F at its recorded scope; future campaign feedback may guide tuning.

Each phase also identifies decisions to settle immediately before its implementation. There is no need to choose every spell, event threshold or calendar curve to approve the module structure.

## Handoff and completion

For each phase, record changed behavior, actual checks, provisional numerical values, pending player review and the exact next action in the resume checkpoint. Request a grouped manual review when a new workflow is ready. Acceptance of this planning draft does not establish numerical tuning or resolve the open choices.

The module completes when all six workflows have their own implementation evidence and user acceptance. Public-release provenance, licensing and Corresponding Source review remain separately tracked release gates.

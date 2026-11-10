# 13 — Road encounters, practice duels and per-level XP

2026-11-02. The grouped implementation is user-accepted with the lighter slug tuning below; the user explicitly waived an additional test. The user approved one combat and one noncombat proof of concept, company-scaled combat with extra leader weighting, forced combat with tactical retreat, provisional travel frequency/cooldown and no random-combat gold/reputation. The approved noncombat event is a practice duel between two healthy non-leader mercenaries, including reserves. Help rewriting remains explicitly deferred.

## Implemented behavior

- Successful ordinary movement outside towns has a provisional 20% event chance, with three elapsed days between occurrences. No event rolls on failed movement, redraw, load, services, rest, or entering a contract's deployment. A duel is eligible only with two healthy non-leader members. Slugs require currently enabled Spitter Slugs; no substitute species is introduced. If both types are eligible, either is selected equally. A missing type can leave only the other available.
- Each occurrence is saved with its ID, participants/enemy identities, preselected duel outcome, phase and cooldown before presentation. Movement and its action cost are committed once. Road events block normal management/time progression until their decision or battle route is settled. Save failure preserves the occurrence in memory and exposes existing Retry checkpoint; repeat clicks/reloads cannot apply a settled choice twice.
- Wild Spitter Slugs force final deployment; active/reserve review, enemy roster, leader participation, tactical retreat, manual/instant combat, injuries, actual fate, summary and leader revival use the shared battle flow. There is no deployment Cancel button for this forced encounter. The saved deployment checkpoint remains the supported battle-interruption fallback.
- Slug target power is 56% of full company power (reduced from80% after player review), counting all active/reserve members and weighting the leader at 1.25 rather than 1. Power uses the existing stats/gear/traits model, not current HP. The roster rounds to the nearest whole number of existing level-1 slugs, minimum one, with a strict eight-enemy maximum; when the target exceeds that maximum force's power, the recorded target is reduced to the representable maximum. Discrete unit power means small forces cannot promise a tight percentage band. These values are provisional code tuning, not a balance claim.
- Road combat retains normal combat XP and enemy-defeat statistics. It grants no contract completion XP bonus, gold, reputation or contract count. If surviving enemies remain, the company returns to its prior tile; there is no new persistent contract marker or automatic follow-up pursuit. Survivors are not counted as defeated or regenerated into a fresh copy of the same encounter. Historical character records remain in the campaign registry; the latest event is retained until another occurrence replaces it.
- The practice-duel choice names the two participants. Declining grants nothing and causes no damage. Accepting uses one saved 50/50 result for both: each gains exactly half their current level's XP cost, or each loses half their current HP, rounding remaining HP up and keeping at least 1. Direct HP attrition does not call battle injury logic, add wounds, or clear existing injury state. Eligibility uses the existing Healthy rule (full HP and no injuries); neither the leader nor former members participate. Result acknowledgement is separately saved. No extra campaign time is charged beyond the triggering travel.
- XP shown in company details and tactical unit inspection is current-level progress divided by current-level cost. The underlying cumulative save totals and level thresholds remain compatible with accepted saves and existing level-up/stat systems. For example, at cumulative 110 XP with thresholds 100 and 300, leveling changes the visible 110/100 to 10/200. Surplus XP remains, and leveling itself is still chosen by the player. No progression rates, contract rewards, sparring rates or trait XP rules were retuned. The duel's explicit half-cost award is exact and is not reduced by an XP-gain trait.

## Source trace

| Source | Change |
| --- | --- |
| Core/CompanyRoadEvents.cs (new) | Serializable occurrence, code-level tuning, eligible travel trigger, slug scaling/generation, approved duel resolution, acknowledgement and validation/round-trip comparison. |
| Core/CompanyCampaign.cs | Optional current road occurrence and cooldown; event-aware Busy and validation. Existing saves have no occurrence and remain valid. |
| Core/CompanyContracts.cs | Shares the existing enemy factory and validates explicit road battle references without fake contracts; contract budgets/generation/payment unchanged. |
| Core/CompanyBattle.cs | Explicit RoadEventId and shared enemy/source accessors; contract-specific rewards/XP stay conditional while common fate/cleanup/return is shared. |
| Core/CompanyCampaignSave.cs | Verifies committed position/actions, RNG, cooldown, occurrence/outcome/participants and battle source on save/detached-copy round trips. |
| Subwindows/CompanyCampaignView.cs | Transactional travel event hook, occurrence checkpoint before display and event-aware map return. Ordinary non-event movement retains its existing unsaved-until-checkpoint behavior. |
| Subwindows/CompanyMissionsUI.cs | Choice/result presentation, forced deployment with roster but no Cancel, and resume routing. |
| Entities/Units/Unit.cs | Computed per-level XP values; no stored XP migration or change to level-up mechanics. |
| Subwindows/CompanyManagementUI.cs | Uses per-level XP values in company member details. |
| Subwindows/InfoPanel.cs | Uses the same XP numerator/cost/remaining amounts in unit details and its progress bar. |

New CompanyRoadEvents.cs.meta records its Unity identity; no scene/prefab edit is needed. Legacy EventScreen and RandomEvent remain untouched and retired. No new species, text assets, authoring editor, global settings, packages or third-party dependencies were introduced. User-owned replacement digestion narrative and deferred help rewrite were not edited. The eventual help rewrite should also present XP as per-level progress.

## Performed checks and limits

- First Unity batch attempt could not renew the installed editor license under restricted networking, so it exited 1 before compilation. A network-enabled retry of the same installed Unity 2018.3.12f1 succeeded, exit 0: Logs/road13-compile-network-retry.log. This is a successful editor compile and read-only scene check, not a player build or Play-mode test.
- Read-only scene audit retains all inspected references and 72 terrain sprites. No new normalized compiler warnings compared with the previous integration compile.
- Re-ran the existing 32 managed production-code regression checks unchanged except for project-local output paths: Logs/road13-managed-diagnostics.json. This covers the prior persistence, wage/rest/summary/revival, species, health/mana/equipment and summon-data checks. These existing checks do not exercise the new events or the new XP display; no new test cases/framework were added.
- Task-start source scope audit (Logs/road13-source-scope.json) found exactly nine existing source changes, one new source and its meta, no missing files, and no changed scenes/prefabs/art/ProjectSettings/Packages. Source snapshots/diffs are in ignored Logs/road-xp-* and Logs/road13-*. Existing source notices/attribution and actual GPL terms were reviewed; exact paths are in root CHANGES. Inherited provenance and distribution-method gates remain.
- New event choices, actual slug battles/retreat/leader fate, new-event save/reload and XP UI still require player observation. No graphical/manual battle pass, current Windows rebuild, full balance calibration or distribution approval is claimed. The older private Win64 artifact does not contain this phase. Large histories/forces and long-run encounter balance remain provisional.

## One combined player review

Use Unity and the current source, not the previous Windows build. No fresh main-menu/settings/injury-font/payday review is needed.

1. Start or use a healthy company with two non-leader mercenaries and travel outside towns. For a practice-duel event, inspect the named choice and try accepting or declining. On acceptance, check whichever result naturally occurs: both gain half a level's XP cost, or both lose half current HP without a new injury. Reload a pending choice/result once; it should remain the same and not apply twice. No need to grind for both coin-flip outcomes.
2. After the three-day cooldown, travel to encounter wild slugs (Spitter Slugs must be enabled). Check final active/reserve review and roster, finish or retreat using the ordinary tactical flow, then inspect results: no contract gold/reputation/completion reward. Reload a deployment or result once to check routing. A normal contract afterwards should still deploy and pay normally.
3. When a character levels, compare the XP display before and after: excess progress remains but the consumed amount is removed from both displayed totals. Tactical details and company details should agree. Report any unexpected event repetition, save/retry error or company/contract-state change together.

These are a single milestone, not separate per-control tests. Await feedback before additional event content or broader systems.


## Acceptance and encounter tuning (2026-11-02)

The user reports the slice basically good and accepts it with one tuning change: roughly five slugs rather than seven for their three-member company, to reduce focus-fire casualty risk. EnemyPowerFraction is now0.56 instead of0.80, a30% reduction in newly generated encounter budgets. Existing company scaling and leader1.25weight are preserved. Rounding a former7-unit budget at70% gives5units throughout its original rounding interval; no hard five-slug cap is introduced. Existing saved/generated forces stay intact. Contract difficulty, slug stats, event frequency and the duel are unchanged.

Only CompanyRoadEvents.cs changed for this follow-up, plus the dated notice/root change record and status documentation. Task-start source diff reviewed for scope and notice preservation. No new compile/build/playtest or new tests; the user explicitly waived a further test. Earlier checks above predate this tuning. The combined checklist above is historical and must not be requested again by default. Help rewriting remains deferred; no release/publication is authorized.

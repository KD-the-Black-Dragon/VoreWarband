# Total Conversion Roadmap

## Current planning module — 2026-11-06

The user authorized a save-responsiveness follow-up after 40B. It now queues turn-boundary autosaves and manual saves, removes action-level saving and makes the browser read summaries in the background. Native checks passed 51 focused, 937 route and 963 grouped checks, with a successful private Windows review build. [Implementation and player review](implementation/41-save-responsiveness.md). AGENTS.md records the permanent responsiveness-first policy.

Phases 40A–40F are implemented and accepted at their recorded scope. Phase 40C adds a provisional spellbook catalog, tactical ground fire and hazard-aware AI; see its [implementation and evidence](implementation/40c-spellbook-progression.md). 40D post-battle events were confirmed to fire; exhaustive event-by-event testing remains deferred. Prices, combat weights, event thresholds, trait outcomes and 40F calendar tuning can be adjusted from future play data. Phase 40E implements company naming, provisional final scoring, retirement and an independent High Scores archive. Phase 40F implements provisional calendar pressure; native checks and connected simulations passed, and the user accepted the phase on 2026-11-08. See the [40F spec](specs/40f-calendar-difficulty.md) and [implementation evidence](implementation/40f-calendar-difficulty.md).

Spec 38 is closed and accepted. [Module 40](specs/40-authoring-and-campaign-progression.md) covers official balance authoring/settings reliability, map route planning, spellbook progression, post-battle trait progression, ending scoreboard/history and calendar difficulty. Race Editor remains the editing surface with export/import into shipped defaults. Species updates preserve existing individuals and affect new generation. Post-battle scenes assign related traits automatically with count-based positive/negative balancing. Both species and personal traits count using internal Good/Bad/Neutral classifications. Featherweight is Neutral; old injuries are separate. Trait count is uncapped, with fewer-trait characters favored as event subjects. Phase-specific decisions and evidence are recorded in the specs and handoffs below.

Further design answers, 2026-11-06 and 2026-11-08, confirm explored-only routing, utility/control-focused magic with hazard-aware AI, tier-weighted counts in every trait-count calculation, leader exclusion from post-battle traits and removal/downgrade outcomes at a provisional eight-weighted-point threshold. Removal reduces higher tiers one step before removing tier I. Extraordinary experiences dominate subject weighting. Company naming, overall score plus detailed records, distinct endings from reloaded continuations and voluntary retirement are confirmed. The 40E score formula is provisional and recorded in its spec; no retirement bonus is specified. Calendar difficulty now has a protected 21-day opening, a provisional accelerating curve, modestly easier company-relative offers, and a bounded calendar component for newly generated road encounters. Fixed rating power/reward meanings and already accepted contracts are preserved. The rough 100-day target remains a tuning goal, not a promised survival time; exact curve values remain provisional pending player feedback.

**Status:** Conversion milestones00–14 are user-accepted at their recorded functional scope; specialist/release gates remain. Map/navigation placement is accepted. Beautification is deferred until a [unified interface design](specs/15-unified-interface-design.md) covers campaign, Options, content menus and tactical view. The user authorized Options first; [15A](implementation/15a-options-redesign.md) now implements the blue categorized scroll column and four requested control removals, the user found the layout mostly okay. Slider alignment, larger hover text and vore terminology corrections now await focused confirmation; beautification and broader screen redesign remain later. Help prose rewriting remains separately deferred.

## Direction and authority

Build the conversion in distinct reviewed stages: establish the original-game reference, study dependencies, retire the agreed unwanted features, then build the mercenary-company campaign. Keep Unity 2018.3.12f1 and the retained tactical mechanics/AI as the technical starting point.

The [campaign design record](mercenary-company-campaign-plan.md) is the source of gameplay intent. The [species](species-curation-checklist.md), [options](options-curation-checklist.md), and [screen](screen-curation-checklist.md) checklists supply the exact curation decisions. This roadmap assigns that work to specs; it does not replace those lists or approve unchosen species or balance formulas.

Specs are ordinary Markdown in this repository. No OpenSpec installation or external service is needed. Each spec has prerequisites, bounded scope, completion criteria, open decisions, and an execution record. The architecture study must refine the later specs with actual source/scene dependencies before their implementation.

## Phases and review points

| Phase | Specs, in recommended order | Result presented for review |
|---|---|---|
| 0 â€” Establish the reference | [00 â€” Editor and original-game baseline](specs/00-editor-baseline.md) | Known editor/import/compile/launch state and an original-game reference. |
| 1 â€” Comprehensive architecture study | [01 â€” Architecture, dependencies, and removal order](specs/01-architecture-study.md) | Dependency map, source-linked inventories, safe removal order, and refined implementation batches. |
| 2 â€” Retire unwanted features and content | [02 â€” Retire unwanted menus and lock balance settings](specs/02-settings-and-screen-retirement.md); [03 â€” Disable the legacy roster and retire rejected species](specs/03-species-retirement.md); [04 â€” Reset legacy prose and retire 4X events](specs/04-narrative-and-event-reset.md); [05 â€” Retire the 4X loop and preserve tactical combat](specs/05-strategic-retirement-and-combat-boundary.md) | A clean application shell and retained tactical boundary, with a manifest of retired content and any justified residual dependencies. |
| 3 â€” Build the first company campaign loop | [06 â€” Company campaign state, world, clock, and saving](specs/06-campaign-foundation.md); [07 â€” Curated starters, company members, recruitment, and wages](specs/07-leader-recruitment-and-wages.md); [08 â€” Expanded equipment and town commerce](specs/08-equipment-and-shops.md); [09 â€” Contracts, reputation, and procedural enemy power](specs/09-contracts-and-enemy-generation.md); [10 â€” Deployment, battle results, revival, and memorial](specs/10-battle-results-and-company-fate.md) | Create a leader, recruit, equip, accept work, travel, deploy, fight, receive rewards, manage wages, save, and continue or lose the company. |
| 4 â€” Complete the intended character experience | [11 â€” Persistent injuries, recovery, and downtime](specs/11-injuries-rest-and-progression.md) | Persistent injuries, treatment, daily recovery, downtime sparring, and multi-day rest. |
| 5 â€” Integration, balance, and help | [12 â€” Integrated campaign review, help, and first release candidate](specs/12-mvp-review-and-help.md) | Reviewed campaign proof of concept and Pure Tactical, save/Ironman coverage, help, known limitations, and next-work list. |
| 6 â€” Later campaign expansion | [13 â€” New road events and ambushes](specs/13-new-random-events.md) | Newly designed road events and ambushes. Timing remains open; legacy 4X events never return. |
| 7 — Campaign presentation | [14 — Persistent navigation, map, town and roster](specs/14-campaign-interface.md) | User-defined map-first layout, grouped status, direct town services/level-up and full-action rest. |

The first playable loop in Phase 3 is an intermediate milestone. The intended injury/recovery experience is not complete until Phases 4â€“5. Avoid labeling intermediate screens or systems as finished simply because their navigation exists.

Within Phase 2, study findings may change the order of settings, species, and text retirement. Phase 3 foundations may also need small compatibility changes identified in Phase 1. Record those changes in the specs and explain material tradeoffs before implementation. Do not expand the work into an engine upgrade or a rewrite of retained combat.

## Phase 1 study packets

Work through these reviewable packets within spec 01 rather than producing a single unexplained architecture dump:

1. **Startup and screens:** mode transitions, base-scene wiring, settings persistence, Race Editor, and retired entry points.
2. **Species and content:** every species generation/filter route, numeric identities, external content, narrative selection, and legacy event triggers/choices/effects.
3. **Units and tactical combat:** stats, traits, inventory, AI, terrain, battle setup, surrender/downed, vore/digestion, retreat, outcomes, and results.
4. **Campaign and persistence:** old strategic turn ownership, world generation, saves, canonical member identity, proposed company boundary, and ordered daily updates.

For each important behavior, show the chain from player action to state update to display. Record the actual scripts, methods, scene/prefab references, settings, and runtime data involved.

Expected study deliverables are an architecture report, a keep/adapt/retire manifest, a risk/decision register, and source-based amendments to specs 02â€“13. Each manifest row identifies what is retired, how it is reached, what still depends on it, how it is disconnected/replaced, and how that change will be checked.

The study is complete when it explains the important migration paths and supports small implementation batches. It need not document every unrelated asset or every line of the original game.

## Meaning of a clean slate

Cleanup is functional retirement in the reviewed dependency order. Old player entry points, generation routes, callbacks, settings overrides, event triggers, and campaign simulation must cease to run where their removal is agreed. Hiding a button alone is insufficient.

Shared code may survive temporarily if retained tactical combat still needs it. Record the reason, owner, and later replacement point. Do not remove a shared data structure solely because it has an empire-era name.

Keep the underlying Trait system and approved runtime behavior while retiring its unwanted authoring screens. Removing a settings control does not automatically retire its mechanic. Defaults and persisted overrides require explicit treatment so hidden balance controls cannot keep influencing the game unexpectedly.

All existing species become unavailable before curation. During that intentional empty-roster stage, the editor/application/menu checks can pass while battles cannot start. Show a safe unavailable-state message; do not enable an unapproved species for convenience. Record battle checks as pending, then perform them as soon as the first approved species is curated in 07. Phase 2's cleanup review must disclose this limitation.

All old 4X event content is retired, though useful generic event machinery may be reused later. Existing vore/digestion prose is reset to neutral functional placeholders; the user will supply replacement narrative. Neither action authorizes changing retained combat mechanics.

### File-operation constraint

AGENTS.md prohibits deleting files or directories. These specs use edits, disconnection, and retirement from active code/content. If completing a later batch requires removing a whole file from active use, follow AGENTS.md and obtain the required specific direction then; a project Recycle Bin move is available when appropriate and authorized. Never use destructive cleanup, drop licenses/attribution, or automatically remove Unity-generated paths.

The user's request to plan removal is not permission to delete paths now. No file moves or deletions are included in this planning work.

## How a spec is executed

1. Confirm prerequisites and the exact source/scene trace; resolve only decisions needed for the next batch.
2. State that batch's scope and intended visible result. Keep changes small enough to review separately.
3. Implement the authorized batch and inspect its diff, including serialized Unity changes and preserved user work.
4. Perform only the checks authorized for that stage. Agree the relevant compile/build and in-game checks when starting it; the checklists here describe required evidence, not checks already run.
5. Report what changed, checks actually performed, remaining limitations, and a short player-facing checklist.
6. Wait for user feedback on that stage before beginning the next, following AGENTS.md.

Use Git checkpoints according to the agreed repository workflow. Do not silently commit unrelated changes. Record a spec as complete only when its criteria and pending checks are satisfied; record blocked checks explicitly.

## Decisions to settle when they become relevant

| Decision | Needed by | Why it matters |
|---|---|---|
| Any discrepancy between a listed settings-control removal and removal of its mechanic; final legacy Fog of War page disposition | 02/04 | Prevents menu cleanup from changing combat without a design decision. |
| Developer password for Race Editor | 02 | Completes the convenience gate; keep it out of reports. |
| First user-selected species and their sapient/animal classification | 07 | Enables real generation/battle checks after the initial empty roster. |
| Safe empty species pools and preserving existing individuals after disabling new generation | 03 design; 07/12 integration | Policy is settled: preserve every existing individual, including offered/former mercenaries and contract enemies. No retroactive removal. |
| Campaign map dimensions, action budget, terrain costs, feasible battle saves and turn-by-turn Ironman backups | 06 | Battle saves are desired with between-battle fallback acceptable; assess both campaign/tactical checkpoints. Service costs can remain zero and configurable. |
| Recruit refresh, wages/level increases, prorating, initial prepayment, and debt tolerance | 07 | Unaffordable payday recipient choice is settled: debts first, then explicit player-selected group or individual payment; never automatic prioritization. |
| Nine agreed equipment slots: compatibility, armor coverage, HP adjustment safety, and basic item prices | 08 | Helmet/torso/legs/arms/accessory/melee/ranged/two spells; visuals not required. Review symmetric maximum/current HP changes. |
| Power scale/weights, rating tolerance, deadlines, reward variation, offer refresh, and reputation progression | 09 | Turns the procedural contract idea into tunable rules; does not require exponential reputation growth. |
| Generous stalemate turn limit, retreat destination, nearest-town selection, insufficient revival funds, and ordinary loot | 10/11 | No abandonment state: simulate actual outcomes. Both sides inactive or a stalled battle trigger stalemate; survivors flee. Fled enemies count defeated even on a draw. Leader revival/game over is digestion-only. |
| Temporary HP caps/injury interaction, injury chances/penalties, recovery, healthy-member eligibility, treatment, and survivor HP baseline | 10/11 | Temporary HP absorbs damage first and clears after battle; vore heals lasting HP. Rest interrupts for warnings/required choices. Exact numeric tuning is deferred. |
| New event content, frequency, triggers, and priority | 13 | Retains the desired random-event capability without carrying over legacy 4X events. |

These are checkpoints for later work, not a request to answer them all now. Most numerical values should be developer-tunable and reviewed against the intended experience.

The [2026-10-30 clarification record](design-clarifications-2026-10-30.md) contains the full 24 answers and follow-up decisions, including permanent deprecation of resurrection/conversion effects, downed targeting/cleanup, temporary summons, and the resolved no-abandonment/stalemate/contract rules. Recording these decisions is documentation work, not implementation or completion of packet 01D.

## Evidence and present limits

The project records Unity 2018.3.12f1. Spec 00 has now verified import/compilation and a Windows player build with that editor, and the user confirmed successful main-menu startup. Broader gameplay checks remain pending; automated window capture still times out. See [the baseline report](unity-baseline-report.md).

Study packet 01A traced startup/navigation and settings. Packet 01B mapped all 90 rejected species names and traced content/generation/narrative routes. Packet 01C traced combat/equipment/results. Packet [01D](architecture/01d-campaign-persistence-and-identity.md) now covers campaign/day ownership, generation/discovery, identity, normal/tactical saves and Ironman feasibility. The [consolidated removal manifest, order and risk register](architecture/01d-removal-sequence-and-risks.md) reconciles the four packets and subsequent design answers. The study is prepared for review. 02C resolved the earlier unmatched settings bindings and relevant inherited callbacks; shared-resource ownership remains a preflight for later content retirement. This does not mark implementation, baseline gameplay testing, or all possible dependency paths complete.

Authoring the original roadmap changed documentation only. Spec 00 performed the recorded import/build checks. Packet 02A is the first source-change batch; no package upgrade or gameplay-rule change is included.

## Recommended next stage

The user accepted [06 foundation preview](implementation/06-campaign-foundation-preview.md). [Grouped07–08 people/economy](implementation/07-08-people-and-economy.md) now adds the selected Avians, Lizards, Gnolls and animal SpitterSlugs, leader creation, recruitment/rehire, wages/debt, nine-slot gear/shopping, checkpoint navigation and retained terrain artwork. The user confirmed every07–08 review item working. [Grouped09–10](implementation/09-10-contract-and-battle-loop.md) now connects fixed-power procedural contracts, target travel/every-battle deployment, shared downed/temporary-HP rules, actual digestion/flight/stalemate outcomes, automatic rewards, saved results/revival and records. The user confirmed the HP persistence/naming feedback works and authorized the next milestone. [Spec11 injuries/recovery](implementation/11-injuries-rest-and-progression.md) now adds persistent wounds/old injuries, daily healing, chosen-day rest, paid town treatment and healthy-member sparring; the reported XP, departure-dialog and injury-display feedback corrections are now user-accepted. The user authorized12. [Integrated review and help](implementation/12-integrated-review-and-help.md) now implements current help, save/retry and expiry corrections, daily mana restoration, summon downing cleanup and centralized validated resumption. Managed and compile/scene evidence is recorded; the user accepted the combined player review on 2026-11-02 with no material issues. A concise player-facing help rewrite is deferred, and the payday countdown is now user-accepted. The user authorized and defined two proof-of-concept events. [Spec13 road events and XP](implementation/13-road-events-and-xp.md) now implements forced company-scaled wild slug combat, the practice-duel choice and per-level XP display; the user accepted this slice with lighter slug encounters (new encounter budget0.56 rather than0.80) and explicitly waived an additional test for that tuning. Earlier compilation/scene/managed checks predate the constant adjustment. Unexercised specialist cases and the [release gates](release-readiness.md) remain. No first release candidate or broader test pass is being inferred. Company tactical saving still uses deployment fallback. Do not restart accepted milestones or request per-control tests.

The user selected campaign interface/map presentation next and supplied concrete requirements before implementation. [Spec14 implementation](implementation/14-campaign-interface.md) now provides the persistent top navigation, bottom status, larger retained-art map, town service column, direct roster level-up and full-action rest requirement. Compile/scene and existing managed checks passed; one connected UI review remains. Do not expand into the deferred Help rewrite or further content without direction.

## Consolidated remaining milestones — user direction 2026-11-01

03B8 manual check accepted. Replace micro-packet handoffs with these larger review milestones; retain individual specs as implementation references.

1. **Finish the cleanup milestone (02–05 closure):** refresh remaining sprite readers after class/effect retirement; clear only demonstrated obsolete bindings/references; reconcile residual shared helpers, retired screens/settings/events and campaign providers; record intentional shared assets/ID tombstones, warning and battle-testing limits. No automatic artwork deletion/repack and no requirement to erase every unused file before new work. One cleanup completion report and at most one consolidated manual check where it adds coverage.
2. **Campaign foundation (06):** company identity/state, procedural grid travel/discovery, daily action/turn flow, map/town/company navigation and saving foundation. Manual review exercises a connected campaign workflow.
3. **People and economy (07–08):** player leader/creation, approved MVP species selection, town recruitment/rehire identity, roster/active/reserve, wages/debt, equipment slots and shopping. Do not choose species for the user. Review the connected hiring/payment/equipment workflow once stable.
4. **First complete contract loop (09–10):** offers/reputation/power-budget enemy generation, target travel/deployment, battle integration/results/rewards, downed/digestion/retreat/stalemate and leader fate, campaign return and persistence. Manual review follows a complete mission and relevant fate cases; source-only retirement checks never substitute for this.
5. **Attrition and character experience (11):** lasting/temporary HP boundary as coordinated with10, injuries/old injuries/treatment, recovery/rest/downtime progression and memorial/statistics integration. Review a multi-day campaign with injury and wage consequences.
6. **Integrated MVP (12):** end-to-end regression, save/reload/Ironman coverage, balance, new help and release-readiness/license/provenance review.
7. **Later expansion (13):** new road events/ambushes and other optional content after the core loop; no legacy events restored.

Manual testing is milestone-based, not per source packet. Continue autonomously through related low-risk sub-batches; stop for user input only for genuine unresolved design choices, material risks, or useful milestone feedback. Keep recovery notes and required modification records current.


### 15B Content Settings review (2026-11-02)

User authorized the next UI group after accepting the Options slider/hover correction, with a remaining checkmark visibility tweak. [15B](implementation/15b-content-settings-redesign.md) applies the shared blue scroll presentation, retains the top page dropdown without subcategories, retires nine named controls to existing code defaults and shares bright checkmarks with Options. Compilation/read-only scene and scoped source/licensing review passed. One focused player review is pending; broad beautification, tactical/campaign styling and Help rewriting remain deferred.


### 15C main menu (2026-11-02)

Content Settings accepted; the user requested a similarly restrained blue main menu, preset/status retirement, displayed1.0 and an elevated New Campaign / Single Battle / Load Saved Game / Options column. [15C implementation](implementation/15c-main-menu-redesign.md) keeps title/peripheral positions and uses the existing company checkpoint browser for loading. Compilation/read-only scene and scoped source/licensing review passed; one focused menu review is pending. Broad campaign/tactical styling, beautification and Help rewriting remain deferred.


### Paused after accepted main menu; next Single Battle setup (2026-11-02)

The user approves the main menu and requests a pause until they report recovery of the usage allowance. Next authorized direction, not yet started: [15D Single Battle setup brief](specs/15-unified-interface-design.md). Shared blue style, core controls centered at top, Attackers left/Defenders right, AI-watch choice bottom, two subtle separators. Preserve ALL existing fields and species sample images. No further work during the pause; no repeated main-menu/Content Settings testing. Battle HUD styling and Help rewrite remain separate later work.


### 15D resumed: Single Battle setup (2026-11-02)

User reports usage recovered and resumes work. [15D](implementation/15d-single-battle-setup.md) implements the recorded blue top-controls/two-combatant-columns/bottom-watch layout. All fields, original action buttons, species previews and conditional bulk controls retained; columns scroll to fit bounded windows. Compilation/read-only scene passed, focused graphical review pending after scope review. Main menu and Content Settings stay accepted; live battle HUD/Help rewrite/final beautification remain later work.


### 16 — Tactical battle interface (2026-11-02)

The Single Battle setup and final 2x sample fit are accepted. The user now authorized the live battle interface update. [Spec 16 implementation](implementation/16-tactical-interface.md) covers blue panels/controls, unified Attack on both existing hotkeys, four weapon/spell readouts, larger log and smaller trait text. Compilation/read-only scene and private Win64 build passed; one combined battle UI/targeting review remains. Help rewriting, broader beautification and larger company/recruitment portrait rows remain deferred.


### 17 — Shared popups and Esc overlay (2026-11-03)

[Implementation](implementation/17-shared-popups-and-escape-menu.md) applies the shared blue palette to player-facing popup families and introduces a compact Esc menu. Compile/read-only scene and private Windows build passed; one focused graphical/navigation review remains. HP fix accepted; developer Race Editor styling and Help rewrite remain outside this scope.


### 18 — Company leader creator (2026-11-03)

[Implementation](implementation/18-company-leader-creator.md) introduces the one-screen blue creator with species dropdown, two compact appearance columns and center-contained paper doll. All41native appearance labels/controls and gender visibility logic retained. Clean compile/read-only scene and final private Windows build passed; one combined creator interaction/visual review remains. Other accepted UI is unchanged.


### 19 — Named Save / Load and exact battle persistence (2026-11-03)

Leader creator accepted. [Implementation](implementation/19-save-load-and-battle-persistence.md) replaces public checkpoints/legacy World saves with named slots, progress previews, confirmed deletion, map/side-turn autosaves and exact company battle resumption. Ironman uses its latest valid autosave, corruption backups and defeat invalidation/removal. Final Unity native-runtime audit passed34 with synthetic data, deletion suppressed and every probe preserved. Final private Windows build passed. On 2026-11-03 the user reports manual test results look good; this milestone is accepted with no issues reported. Help redesign remains deferred.


### 20 — Menu cleanup and shared campaign Help (2026-11-03)

[Implementation](implementation/20-menu-cleanup-and-campaign-help.md) removes obsolete Continue/map-preview entries, uses Load Game, silences routine default race-setting status and unifies Help in a compact blue chapter/scroll layout across menu/campaign/battle. Existing articles/references retained. Clean compilation/read-only scene and private Windows build passed; the user explicitly accepted this milestone on 2026-11-03. Broader help prose editing remains separate.


## Post-injury engineering direction — 2026-11-04

Traits/injuries provisionally accepted; focused injury playtests and Help prose remain deferred. The user approved bounded follow-up directions and requested one slice at a time to monitor usage. [Spec34](specs/34-follow-up-engineering-slices.md) orders combat targeting/previews, password-gated developer lab, power scoring, equipment/merchant foundation, gear/reputation progression, balance simulation reports, larger world generation and roads. Crystaline/Fractured gets a known-broken/very-low-priority comment only. Post-battle occurrence-based trait events are recorded as a later design/narrative project, explicitly not to implement now. This entry records a plan, not completed code or permission to run all slices continuously.

## Cleanup, recovery and connected regions — 2026-11-05

User authorized the four follow-up items as one ordered batch. [Spec35](specs/35-cleanup-recovery-and-world.md) and [implementation review](implementation/35-cleanup-recovery-and-world.md) record the approved downed-prey capacity exception/removal of overcapacity self-damage, quicker HP versus longer injuries, focused recovery comparisons, and160x120six-town coherent regions/roads. Native grouped checks890, world/map checks571 and27recovery comparisons passed; fresh private Windows review build compiled. No graphical acceptance claimed. Road costs remain terrain-based and recovery figures provisional pending user steering. Existing campaigns keep their geography. Help prose/post-battle narratives and broader balance work remain deferred.

## Compact zones, road clearings and fields — 2026-11-05

User requested a more compact3x2town layout, bottom-center start, actual road grassland/one-action costs, original farming fields and HP10/20/30. User chose120x80; six daily actions is an explicitly stated provisional value for new companies, preserving existing saved allowances. [Implementation/spec36](implementation/36-compact-zones-roads-and-fields.md) records the changes and focused native evidence. Existing road-bearing maps retain dimensions/town locations but clear routes and gain adjacent non-road fields. Pre-road saves remain geographically unchanged. No4Xsettlement production restored or new artwork imported. Manual visual review remains for the player.

## Five-day road journeys and two exits per town — 2026-11-05

User requested25–35road-move village distances andexactlytwooutgoingroads. [Spec/review37](implementation/37-town-road-journeys.md) retains120x80/sixzones/bottom-centerstart, bringsnewtownsclosertogether andusesa short six-town circuit. Forty seeded regions pass821focusedchecks:240connections28–34moves; mean nearest-town30.125moves (~5.02six-action budgets); everytownhas two physicalexits to differenttowns. Acceptedroadgrass/farms/HP rules preserved. Existing saves retain their layouts. Freshprivateplayerbuild17b72003 compiled; graphicalreview remains.

## Acceptance — World-generation and road slices closed, 2026-11-05

User accepted the final two-exit/five-day-road result and asked whether slice7 remains. Cross-check confirms original spec34slice7(worldgeneration) andslice8(roads/travel/mapintegration) were covered by the subsequently authorized four-item batch plus specs35–37feedback. Both are complete and accepted for the current scope. Current region120x80/sixzones/bottom-centerstart, closer reproducible settlement layout, retained art/farms, single two-exit circuit, grassland roads and measured~five-day nearesttownjourneys. See implementation35–37 and existing native/save/performance/build records. Further geography or travel expansion is future scope, not a pending requirement of those slices. Documentation-only acceptance reconciliation; no source edits, tests or new build.

## Follow-up module closed; calibration and quality plan — 2026-11-05

The user explicitly closes the original spec 34 module. [Spec 38](specs/38-calibration-economy-and-quality-review.md) plans four substantial next phases: mathematical/native combat calibration; connected campaign economy and level-up wage disclosure; grouped resolution/input review; and deliberate injury validation/integrated handoff. The user prefers simulation-derived numerical recommendations. New economic goals include a starter contract funding a basic replacement plus surplus, modest inexpensive basic armor, meaningful premium investment lost with its wearer, and accelerating level-based wages that make low-rating work inadequate for veterans. Surplus accounting, deliberately deferred levels/free-leader sustainability and midweek salary timing are pending clarification. This is planning only: no implementation, new simulations, tests or build. Post-battle narratives, Help prose and Crystaline remain deferred.

Clarification, 2026-11-05: replacement surplus is after wages/recovery; level-up wage increases start next regular payday. The user adds separate compulsory weekly company upkeep, paid before employee wages, including leader-only companies. Unpaid upkeep triggers a critical-insolvency warning and must be cleared before the next payday; outstanding prior upkeep debt at that payday ends the campaign. Upkeep increases should be mostly flat, important early and relatively minor later. Spec 38 records the expanded Phase B scope and pending clock/partial-payment/repayment choices. These are design instructions for the plan, not implemented gameplay.

The user subsequently confirmed a positive starting amount plus a flat weekly increase, automatic spending of available payday gold with only the shortfall recorded as upkeep debt, and an explicit Pay Upkeep control associated with the leader. Upkeep covers the leader's and company's necessities and operating requirements. Spec 38 now incorporates all answers. The economic design is ready for calibration when implementation is requested. Actual tuning values remain to be calculated.

## Phase 38A combat calibration implemented — 2026-11-05

The user authorized Phase A and confirmed eventual roguelike defeat with uncapped flat weekly upkeep. The implementation used 1,072 native calibration battle runs, including independent fixed-roster comparisons, exact mitigation and weapon-outcome math, and 40,000 native probability rolls. Selected light armor totals are 16/28/40. Healthy weapon power includes rescued grazes and KeenEye's guarantee. Final checks passed 446 focused and 893 grouped assertions. A fresh private Windows build compiled with zero new warnings. Player review remains pending. The all-ranged cleanup stalemate has since been traced to cycling flight routes and resolves under the user-directed [global Flight retirement](implementation/39-flight-retirement-and-stalemate.md). Phase B economy/upkeep, Phase C display/input review, Phase D focused injury validation and the deferred ending scoreboard remain subsequent work. [Phase A review](implementation/38a-combat-calibration.md)


## Phase 38B company economy implemented — 2026-11-05

The user authorized Phase B after the Flight detour. Upkeep is now charged before wages, starts at 40 and grows by 10 weekly without a cap. Unpaid upkeep ends the company at the following payday unless explicitly repaid through the leader. Existing saves start with the initial bill at their next payday without retroactive debt. Wages accelerate with level and are previewed before commitment. New contract rewards and cheaper basic armor support the agreed replacement target, with 260 starting gold retained to fund two starters. Final checks covered 14 connected histories, 602 native battles and an exact saved continuation to day-210 insolvency, plus 963 grouped assertions and a fresh private build. Numerical values and rendered player workflows remain provisional pending player review. Phase C display/input and Phase D injury review remain subsequent work. [Phase B evidence](implementation/38b-company-economy.md)

## Phase 38C display and input review implemented — 2026-11-05

The user accepted Phase B and authorized Phase C. The five-resolution native matrix passed 306 assertions with 187 rendered frames. A focused resize follow-up passed 24 assertions with 30 frames, and the final grouped regression passed 963 assertions. Corrections cover slider precision/non-finite entry, screen-edge tooltip placement, long roster/save captions and trait scrolling above the combat log, including aspect-ratio changes. A fresh private player compiled without new warnings. Physical mouse/keyboard and standalone resizing remain one grouped player check after desktop capture failed. Phase D injury validation remains subsequent work. [Phase C evidence](implementation/38c-display-and-input-review.md)

## Phase 38D injury and integrated validation complete — 2026-11-05

The user accepted Phase C and authorized Phase D. Focused native verification passed 671 assertions and 170,000 seeded trials covering injury effects, probabilities, treatment/recovery, leaders, reserves, actual combat, saves/resume and Single Battle isolation. Integrated economy boundaries passed 64 checks and the grouped suite passed 963. The approved implementation passed with its existing tuning. This packet adds editor verification/documentation, and accepted player 512e67a8 remains current. Spec 38's four implementation/review phases are complete for the agreed scope. Subjective campaign balance, broad Help/narrative work, ending scoreboard, time-based enemy escalation and public-distribution gates remain future work. [Phase D evidence](implementation/38d-injury-validation-and-integrated-handoff.md)

## Acceptance — Calibration and quality module closed, 2026-11-06

The user accepted Phase D and requested closure. All four spec 38 phases are now complete and accepted. No additional design answer or implementation task is required to close this module. The developer race-settings warning remains separately tracked technical work, alongside the existing public-release gates. Deferred features remain future workstreams. This acceptance update changes documentation only. The accepted player remains 512e67a8. [Closed spec 38](specs/38-calibration-economy-and-quality-review.md)

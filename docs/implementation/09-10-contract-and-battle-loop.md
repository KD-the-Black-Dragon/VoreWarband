# 09–10 — First contract and battle loop

Implemented 2026-11-01 after the user confirmed every07–08 review item working. Final agent checks are recorded below and in the resume checkpoint; one connected runtime review is pending. This is provisional campaign tuning, not a final difficulty/balance claim.

## Player workflow

Town hubs offer local rout contracts. Contract lists show rating, reward, deadline and target, but never an enemy roster. Accepting fixes the quote and activates the target without creating enemy characters. Reaching the target generates the force once and opens final deployment; the roster/weapons are visible there, and the player proceeds into battle. A failed attempt retains its living enemies, current state and missing casualties for retry. At most eight active members deploy, with the leader always included; see the balance item 2 follow-up for enemy quality and size limits.

Battle terrain, sprites, tactical actions, enemy AI and the existing simulation button are reused. The new adapter supplies battle-only services; it does not reactivate empire turns, village income, garrisons or army maintenance. Battles consume no campaign days. Successful contracts grant gold automatically, including when their last enemies flee during a stalemate. A target with real surviving enemies remains retryable with those same people, wounds and missing casualties.

Normal mode retains checkpoint history. Ironman retains latest-valid checkpoint access. This milestone uses the explicitly permitted **between-battle fallback**: deployment is saved before combat; results are saved after settlement. Leaving combat through the menu or restarting resumes deployment. It does not preserve tactical turns. No live-battle/turn-by-turn tactical-save success is claimed.

## Fixed power and provisional values

| Item | Current provisional rule |
|---|---|
| Contract power budget | `45 + 30 × rating`, independent of company growth |
| Force band | 92–108% of the quoted budget |
| Generator bounds | 48 generated candidates, at most40 composition attempts of96 selections; no unapproved fallback |
| Local offers | Up to4; pool replenishes every7days; starts at rating1 |
| Reputation access | Maximum offer rating grows continuously from reputation, capped at20 for this first tuning |
| Deadline | 30days from acceptance, same generous duration for every contract |
| Gold | `60 × rating`, ±10%, rounded to tens; exact quote shown; no gear reward |
| Reputation | Completion +1; expiration/abandonment minus rating, floored at0; no passive decay |
| Surviving target recovery | 2% maximum HP/day, minimum1; no replacement/repopulation of defeated members |
| Stalemate | Both sides have no active on-field units, or200full turns |
| Post-battle HP baseline | Surviving persistent units at least15% maximum HP after temporary HP clears |
| Revival fee | 20 × leader level; insufficient funds become0gold, no additional revival debt |

Power is inspectable: eight base combat stats ×.18, maximum HP ×.2, melee weapon damage/ranged damage ×1.5, usable spells ×7, traits (normally2, Featherweight/NaturalCaster5), armor protection, and level ×2. SpitterSlugs use a .65 multiplier. Accessory bonuses already in stored stats are not counted twice as equipment. This is an initial approximation requiring combat playtesting; it is not a proven measurement of vore/digestion difficulty. Recruitment retains the accepted07 reputation/level/gear hooks; the new combat power calculation does not silently replace the AI's existing retreat evaluation.

The generator makes real varied units rather than curated squads. SpitterSlugs have an enemy-only weak ranged natural weapon; it cannot be purchased. Empty/impossible eligibility pools yield fewer/no offers rather than a loop or an unapproved species. Once generated, accepted enemies/rewards are saved and never rebuilt merely by reopening a screen or loading. Expiration/abandonment removes the active marker and applies its penalty once.

## Downed, digestion and HP

Shared tactical combat now distinguishes temporary HP, downed survival and actual digestion death:

- Persistent units at0HP become downed. Fear/pain can also down a unit above0HP. Downing latches for the rest of the battle; healing, escape and the Fearless reset cannot restore command ability. Movement reads0 and actor/caster command checks reject actions. Summons disappear when defeated instead.
- The old internal `Surrendered` access remains as a compatibility name mapped to downed status. Player-facing information says downed. The voluntary action, case9 callback and skip/simulation surrender option are retired in both modes. Existing cosmetic flag preferences are retained.
- Ordinary weapon attacks and hostile attack/status spells do not intentionally target downed units. Retained consumption magic, its consumption bite and temporary clone spells remain available; consumption magic sees downed targets as100% success. AI's old late-turn/downed attack exceptions are removed. Collateral damage remains possible and cannot permanently kill a persistent downed unit. Existing consumption checks see downed as100% success, subject to retained legal-vore/capacity rules.
- Ordinary healing grants grey temporary HP. Damage spends that pool first; it does not drain over time and disappears after battle. For example,50lasting+30temporary taking20damage returns to50lasting at battle end. Vore nutrition and retained digestion-healing effects restore lasting HP, replacing temporary HP when necessary at the total cap. Zero-damage friendly containment cannot count as lethal digestion merely because its passenger is downed at0HP.
- Actual lethal digestion is marked separately from ordinary0HP; original digestion/absorption, healing and experience continue to simulate. There is no instant abstract cleanup meal, end-of-battle regeneration to full, forced casualty, or left-behind assumption.
- A side's downed/contained living units keep combat running. If standing enemies face downed company members, those members stay in the actual simulation. The existing simulation control can delegate the rest. Both sides inactive, or the generous turn limit, instead produces stalemate: all remaining people flee and survive.
- Enemy flight is blocked while carrying living captives, including warp and direct retreat paths. The player retains existing edge-of-map retreat and transport behavior. An enemy that actually flees is defeated for its contract even on a draw.

This health boundary is implemented now because results depend on it. Injury rolls, permanent old injuries, paid treatment, company daily/rest recovery and healthy-member sparring remain spec11. Do not describe them as already implemented. Pure Tactical shares the combat/downed/temporary-HP rules but has no persistent company or long-term casualties.

## Settlement, revival and records

Every encounter has a stable battle ID, saved deployed IDs and a recorded transition: deployment → summary → optional revival → returned. Settlement works on a detached saved graph, validates it and installs it once. It does not serialize a dialog callback as the authority for money, deaths or revival. Reopening a saved summary or revival screen advances its phase without reapplying the contract reward or a previous fee.

Actual digested non-leader members leave membership and add one memorial record with their saved name/species/day and actual predator identity. Their equipment leaves owned inventory; there is no equipment salvage/drop feature. Dismissals, debt departures and stalemate survivors never enter the memorial. Former members continue to retain their accepted07 identity/debt behavior. The active-campaign record shows reputation, elapsed days, gold earned (contracts and merchant sales, excluding starting cash), hire transactions, memorialized losses, contracts completed and enemies defeated (including defeated temporary enemies).

With leader revival disabled, actual leader digestion stops battle and ends the campaign immediately; downing alone does neither. With revival enabled, survivors including reserves preserve the company. Battle outcomes, earned rewards and the summary settle first. The subsequent cutscene moves to the nearest town by traversable-grid distance with a deterministic tie break, pays the level-scaled fee (clamped at available gold) and restores the leader at full health with the retained loadout. No wages/deadlines/day updates occur during this trip. No revival occurs if the company has no surviving member. Permanent leader injuries remain forbidden when11 adds them.

Revival can be changed on the campaign settings page and through a company-only tactical button. During battle, changing it writes only that preference into the deployment fallback; it does not masquerade as a tactical checkpoint. Ironman mode remains locked at campaign creation.

Accepted07 saves are upgraded in memory with empty mission/memorial records and the default enabled revival option. Existing people, items, debt, gold, days and map are retained. Historical income/contracts were absent in07; the new counters start with the information available (known hired/former people supply a lower bound for prior hires). The upgrade does not import old empire saves or infer historical deaths/income. Map-only previews stay previews.

## Source/dependency trace

- New `Core/CompanyContracts.cs`: saved offers/target IDs, power formula, bounded composition, acceptance/completion/failure and deadline stage.
- New `Core/CompanyBattle.cs`: explicit tactical adapter, detached result reconciliation, transition records, memorial/revival and daily target recovery.
- New `Subwindows/CompanyMissionsUI.cs`: contracts, actual enemy roster, every-battle deployment, saved summary/cutscene and active-campaign record.
- `Core/CompanyCampaign.cs`, `CompanyEconomy.cs`, `CompanyEquipment.cs`: saved mission/fate fields, migration/refresh/hire counters, natural enemy weapon and revenue accounting.
- `Core/World.cs`, `GameManager.cs`, `State.cs`: battle-only context, explicit company entry/return, context restoration and legacy-save guard.
- `Modes/TacticalMode.cs`: new completion lifecycle, preserved terrain/AI setup and absorption, removed obsolete private strategic/abstract-result helpers, captive-safe retreat and revival control. Temporary-glamour restoration remains within the retained framework.
- `Entities/Units/Unit.cs`, `Actor_Unit.cs`, `PredatorComponent.cs`, `Races/Traits.cs`: HP property/pools, downed latch, movement/attack protection, explicit digestive death and lasting digestion recovery.
- `AI/TacticalAI.cs`, `Entities/Spells/SpellList.cs`, `Modes/Tactical/TacticalAction.cs`: direct-target/command filtering and voluntary-action retirement.
- `Races/Graphics/UnitSprite.cs`, `UI/RightClickMenu.cs`, `Subwindows/InfoPanel.cs`, `Utility/Texts/HoveringTooltip.cs`: grey HP and accurate downed previews/help.
- `Subwindows/CompanyCampaignView.cs`, `CompanyManagementUI.cs`: map list/markers, town offers, decision/return routing and enemy-only shop exclusion.

No scene/prefab/terrain asset, package, engine version or species approval was changed. The three new scripts have associated metadata; exact23C# paths and3metadata paths are identified in CHANGES.md. No files were deleted/moved, no new test framework was added, and nothing was committed or published.

## Verification and remaining review

Final Unity2018.3.12f1 compilation and read-only base-scene opening/wiring inspection passed, return0, with no C#errors/exceptions (`Logs/contract-loop-consumption-handoff.log`). Explicit editor opening also passed (`Logs/contract-loop-handoff-compile-and-scene.log`); the last changes were then covered by the final read-only scene/compile pass. The first import pass missed the newly added mission UI; the subsequent complete pass compiled it. Existing warnings remain; final normalized comparison adds no compiler warnings versus the accepted07–08 log.

`Logs/contract-loop-source-review.json` records23source hashes, dated notices/prior notice retention/CHANGES and3metadata coverage, original BOM/newline preservation, no added trailing whitespace or removed legal-notice candidates, unchanged base-scene bytes and1248retained resource hashes. `License.txt` remains byte-identical; dependencies and project settings are unchanged. `Logs/contract-loop-wiring.json` retains the read-only reference report (all inspected creator/portrait/button refs true;72terrain sprites). No Unity process remains. Source/data checks do not demonstrate actual battle outcomes. No automated Play-mode, battle/save outcome, injury, clone/morph, high-roster or balance pass is claimed. The previously failing screen-capture helper was not retried as proof of gameplay.

One connected player review:

1. Load an accepted07 company (or create one), visit a town, accept several local contracts, and inspect exact quotes and target markers. Confirm neither town offers nor active contracts expose an enemy roster. Travel onto one; the roster should appear at deployment, where reserves can be selected before starting. The rating and reward remain the accepted quote.
2. Fight one contract manually or use the existing simulation control. Downed units must remain out of command, with no voluntary surrender. Check grey healing where a relevant spell/trait is available, lasting vore recovery, normal digestion/absorption, automatic gold/reputation and no campaign day advance. Save/reload at the result summary and continue once; no duplicate reward.
3. Retreat using the existing map edge, then retry the surviving target: no new force is generated, actual casualties stay gone and survivors keep wounds and recover over days. Downed company members facing standing enemies must remain in actual combat until consumed or a real stalemate occurs; no assumed abandonment. On a stalemate, everyone remaining survives/flees and target enemies still count defeated.
4. Exercise leader downing separately from digestion. With a reserve survivor and revival enabled, digestion should finish the battle, show results, then the nearest-town paid cutscene without advancing a day. Load a pending cutscene once; charge/revive only once. Check low funds, then a separate disposable run with revival disabled for immediate game over. Verify the company-only tactical revival toggle and the deployment-save fallback.
5. Let one contract expire or abandon it: one reputation penalty and no active target. Inspect memorial/statistics; only actual digested mercenaries belong there. Check a separate Pure Tactical battle returns to its normal menu without campaign rewards or records.

These are one milestone review, not requests for repeated menu tests. Complex retained traits/copies and large forces still need the integratedMVP regression/balance pass in12. Historical contract/person records and immutable checkpoint generations currently remain retained; long-run storage/performance and the32MBpayload guard also need that pass. After acceptance, proceed to11 injuries, company recovery/treatment and downtime progression.

## Boss-contract expansion, 2026-11-09

Standard town refreshes now offer one “Rout the boss” contract among four after the three opening contracts. The generated boss is explicitly stored on the contract, is a ranged-focused sapient one level above the contract rating, and carries both weapon classes. The generator includes the boss in the existing fixed rating band. Enemy rosters remain hidden until arrival and the saved force remains stable across retries.

Settlement records boss downing before `FinishCombat()` resets the transient Downed flag. Downing, digestion, death or flight routes the boss objective; other opponents retain their actual battle outcomes and use only their normal retreat behavior. Rewards, reputation and contract XP are applied once even if the player retreated. A retreat or company defeat returns to the pre-move tile. Boss digestion is connected to the existing exceptional post-battle trait outcome; leader digestion routes eligible mercenaries through the existing Grudge outcome. After the result and event are recorded, the paid boss contract's generated enemies are removed from the campaign save.

Additive saved-contract fields default old contracts to `RoutForce`. Campaign rules version 7 also removes enemy records and stale enemy deployment references from boss contracts already completed in older saves; pending battle summaries and post-battle events remain valid. The existing ordinary rout-force path is unchanged. No Unity build, automated test, or player battle test was run for this expansion. The next player check should cover an ordinary offer, a boss offer generated only after arrival, each boss outcome (down/digest/flee), retreat after boss neutralization, and retry when the boss remains active.


## Runtime feedback correction (2026-11-01)

User review exposed HP loss during battle copying/checkpoint loading: custom Health was not serialized by this Odin version. [HP/naming feedback fix](09-10-feedback-hp-and-labels.md) explicitly serializes its backing field and adds health preflight/readback/copy guards. Enemy forces/archetype names and singular species labels are also implemented. These fixes await the focused new-campaign reserve→revival→save/reload check; do not mark the original milestone accepted. Recovery remains11. User prioritized future games over existing-checkpoint repair.

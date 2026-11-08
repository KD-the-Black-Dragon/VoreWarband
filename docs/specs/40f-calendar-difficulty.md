# 40F — Calendar-driven difficulty progression

Implemented and accepted with provisional tuning, 2026-11-08. Final phase of [module 40](40-authoring-and-campaign-progression.md). The user confirms a protected opening, accelerating later pressure and a rough 100-day campaign horizon. The values below are simulation-informed starting points, not final balance.

## Confirmed creative direction

The user wants an open-ended roguelike in which expenses and enemy difficulty eventually outpace the company. Compulsory upkeep already starts positive and increases by a flat weekly amount without a cap. That accepted economy must be included in difficulty analysis. A permanent safe, cost-free leader-only campaign is outside the intended experience.

Calendar difficulty has no early pressure, then begins gently and accelerates increasingly severely. The user's illustrative onset is around day 21, with a light increase over days 21–28. Subsequent weekly intervals should increase both total enemy power and the rate at which it rises. A very late interval such as days 1000–1007 would be extreme. Day 21 is a provisional calibration anchor rather than a finalized boundary.

The desired successful-run horizon is roughly 100 in-game days. Beyond that, difficulty should become increasingly absurd and eventually overwhelm even successful companies. The intended challenge is scoring as highly as possible before defeat, with voluntary retirement available through 40E. Interpret this as a pacing target, with variation across play policies and seeds. It does not define a scripted day-100 loss.

## Current boundary

`CompanyContracts.EnsureTown` generates contract difficulty from reputation, with fixed rating-based force budgets and quoted rewards. `CompanyRoadEvents` scales encounters to company capability with extra leader weight and the accepted capped health reduction. Random road encounters occur during movement. Rest already advances wages, upkeep, deadlines, healing and refresh systems.

Do not change a rating's meaning or rescale an existing contract roster to match the company. Existing accepted targets, quotes, casualties and healing remain persistent. Calendar pressure must be a separate disclosed input into newly generated content.

Calendar pressure applies only when contract offers refresh or a new road encounter is created. Existing accepted contracts and forces are not rescaled. Contract ratings keep their fixed force and reward meanings; calendar pressure extends the available rating distribution above the existing reputation contribution. Road encounters receive a separate calendar multiplier after the established company-strength/health calculation. Both types record the generated rules revision, enrolled age and pressure.

## Easier work and company-relative offers

Keep some contracts that are easier than the company's capability, so the player can train new mercenaries or avoid exposing injured veterans to maximum danger. Exclude contracts that are far below company capability and would merely clutter the offer list. The user confirms that easy work may pay too little to sustain an expensive company. This availability filter must not rescale the fixed enemy budget or reward attached to a rating.

The provisional company-power reference uses current capability: active members count fully, reserves at 35%, and active formation weight is applied. The minimum offer rating targets contracts at or above 40% of this reference, but never removes the reputation-eligible minimum. Four new offers include one at the floor, one at the pressure-adjusted maximum and two distributed between them. If an enemy composition cannot meet the requested power band, generation retries a nearby lower rating but never below the company-relative/reputation floor, and never invents an unapproved species. Existing quotes/accepted targets stay unchanged until normal refresh, expiry or completion.

## Design and calibration sequence

1. Measure the combined baseline after route planning, spell progression and earned traits. Record travel/recovery, contracts, replacements, purchases, level-ups, wages and upkeep.
2. Calculate candidate curves with a protected opening, gentle onset and accelerating weekly growth. Compare contract and encounter pressure separately while preserving fixed rating meanings.
3. Run connected native campaigns over multiple policies and seeds, including routine growth, conservative play, low-rating farming, delayed leveling, leader-heavy parties, attrition/replacement, recovery and inactivity.
4. Choose provisional values from the distributions. The first implementation is recorded below; ordinary play data can guide later tuning. Exact campaign survival is not a deterministic target because player decisions and combat rolls vary.

## Persistence and fairness

Use elapsed campaign days as an explicit monotonic input. Travel automation, rest skips and loading must process the same boundaries once. Tactical turns consume zero campaign time. Preserve previously generated offers until their normal refresh/expiry rules act. Stamp generated content with the relevant rules revision and pressure state for diagnosis.

Each new campaign enrolls on its first day. Campaigns saved before rules version 5 set their enrollment day to their current elapsed day during migration. This grants a fresh protected opening without rewriting existing contracts, forces, rewards or funds. The enrollment day and rules version persist across save/resume. Tactical combat remains zero campaign time; ordinary daily processing continues through rest and travel as before.

## Grouped checks and player review

Native checks verify the protected opening, pressure onset/acceleration, days 100 and 1000, late offer/encounter generation, save/resume, old-save enrollment, contract refresh, active contract stability and the existing Ironman ending sequence. Connected simulation covers 14 policy/seed histories to days 90–100. It does not establish outcomes for skilled human play or prove that every possible policy eventually loses. The user accepted the phase at this scope; future play can still guide pacing and balance changes.

## Provisional implementation

The enrolled campaign age is `ElapsedDays - CalendarDifficultyStartDay`, clamped at zero. Calendar pressure is zero through age 21, then `P = 0.05d + 0.00015d²`, where `d = max(0, age - 21)`. The curve is deliberately uncapped for contract ratings: its fractional component is stochastically rounded and added above the existing reputation-derived maximum. The underlying reputation contribution remains 1–20 and each rating still uses the existing fixed budget `45 + 30 × rating` and rating-based reward calculation.

| Enrolled day | Pressure | Added contract-rating range | New road-event multiplier |
| ---: | ---: | ---: | ---: |
| 1 | 0 | +0 | 1.000× |
| 21 | 0 | +0 | 1.000× |
| 28 | 0.357 | +0 or +1 | 1.005× |
| 42 | 1.116 | +1 or +2 | 1.017× |
| 100 | 4.886 | +4 or +5 | 1.073× |
| 1000 | 192.716 | +192 or +193 | 3.891× |
| 1007 | 195.129 | +195 or +196 | 3.927× |

Road encounters use `1 + min(3, 0.015 × P)` after the existing company-strength and health factors, so this separate component caps at 4× rather than causing unbounded encounter swarms. This does not cap contract pressure. Newly generated contract and encounter records retain their difficulty revision, calendar age and pressure. Existing content is not rewritten.

### Verification and limits

- Unity 2018.3.12f1 compiled the editor scripts and completed **81 grouped economy/calendar checks**. This included actual synthetic day-1000 offer refresh, actual day-1000 Spitter Slug encounter generation, rules-version-4 enrollment, persistence round-trips, fixed contract roster/quote preservation, and the current Ironman ending order. Late synthetic contract refresh took **150 ms** in the editor audit; this is a single measurement, not a guarantee for a player build or slower hardware.
- **14 connected policy/seed histories** completed without harness errors: **284 battles, 38 permanent losses**. Three histories reached their 90/100-day horizon and were censored; eight ended by company wipeout; three ended by insolvency. Outcomes varied substantially: e.g. one balanced run reached day 100 with 46 battles and one loss, while another wiped at day 64; a conservative no-contract policy reached insolvency on day 14. These are simulated policies, not skilled-player outcomes. The range shows that early losses can occur for reasons other than the new calendar ramp, while the curve contributes only after the protected opening.
- The audit histories and grouped checks are retained under `Logs/balance34f-939fa45e6309462c9196d774a43d6d14` and `Logs/balance34f-ec3cc203888a410f9bd2e976a6d45c06`. No standalone player build or manual gameplay review was performed for 40F.
- Source changes carry dated notices. `CHANGES.md` records the exact source paths; actual `License.txt` sections 4–5 and preservation of upstream notices were checked. No third-party material was added. This check does not by itself clear the separate GPL distribution requirements for a future binary.

### Grouped player check

In a new campaign, review contract ratings and enemy strength at the start and after the protected opening; check that a moderate company can still find some recovery/training work. Move outside a town on a later day to observe road encounters. Load an older campaign if available and confirm it does not start at late-game pressure. Existing accepted contracts should retain their original target and reward. Treat the values as provisional and report whether the ramp feels fair, too gentle or too steep.

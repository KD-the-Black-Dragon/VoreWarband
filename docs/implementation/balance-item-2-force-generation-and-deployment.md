# Balance follow-up, item 2: battle-force size and enemy quality

Date: 2026-11-08

## Implemented

- The company roster remains unlimited. A campaign battle can include at most eight active members, counting the mandatory leader. Final deployment now displays the active count out of eight. Clicking **Begin battle** above the limit opens the requested blocking message and leaves the player on the deployment page to place additional members in reserve.
- Newly generated contract forces contain no more than eight enemies. Their candidate levels occupy up to three consecutive levels, from `max(1, rating - 2)` through the contract rating. Existing level-driven weapons, spellbooks and armor continue to improve with level.
- Sapient enemies at level 3 or above can receive one randomly selected, role-matched positive tier trait: melee accuracy/damage, ranged accuracy/damage, or magic accuracy/damage. The tier rises at levels 3, 6 and 10. Existing traits from those effect families are preserved; a conflicting inborn trait is not overwritten.
- Contract force assembly still honors the fixed rating's 92–108% target power band and its existing bounded retry/failure path. It now tries to establish a mix of up to four sapients and two animals using lower-power candidates first. It then spends remaining rating budget on stronger candidates of the same broad type before adding beyond that preferred mix. If a desired slot cannot fit under the power cap, another feasible candidate may fill the remaining band; the force never exceeds the eight-unit cap.
- Newly generated Spitter Slug road encounters also stop at eight. If the usual company-scaled target exceeds the maximum representable by eight slugs, the saved target is reduced to that maximum.

## Compatibility and limits

The eight-member deployment rule applies to new campaign battles. Existing tactical battles resume with their saved participants. Existing accepted contract forces are not silently regenerated or rewritten; an old campaign retains the force already recorded in that save. The company-wide roster has no new cap. Single Battle mode is unchanged.

The three-level sapient band and trait thresholds are provisional progression choices, not a mathematical win-rate calibration. The force generator's numerical power model remains an estimate, and this packet was not run through battle simulations. Starting-offer count/distance and the post-opening contract ramp were completed separately in [balance follow-up item 3](balance-item-3-opening-contract-progression.md).

## Composition-selection follow-up, 2026-11-09

The focused feasibility audit found that some rating-3 candidate pools contain exact four-sapient/two-animal combinations inside the unchanged rating band, while the prior greedy selection did not choose them. `CompanyContracts` now searches the 48 generated candidates for exact 4+2 combinations and, when one fits, selects the combination closest to the fixed rating budget. Ratings with no feasible exact mix continue through the existing bounded composition/quality fallback. The opening rating-1 contracts remain free to use smaller forces; this change does not require six enemies in every battle.

The updated 40-seed-per-rating audit completed in Unity 2018.3.12f1. All 200 generated forces stayed inside their assigned bands with zero generation errors. The exact 4+2 mix was feasible in 11/40 rating-3 pools (905 combinations across those pools), and the production generator selected it in all 11. No exact mix fit the sampled rating-1, -2, -4 or -5 pools, so the existing fallback retained their smaller/partial compositions. Rating-3 average generated power moved from 131.0 to 135.7 against a budget of 135; the generator chooses the valid mix closest to the budget midpoint. Full report: `Logs/balance34f-d1bc8875ec2d4ce4a1b2ec7331b9cf8a/composition-feasibility.json`; run log: `Logs/item4-composition-feasibility-selection-fix.log`. This verifies selection feasibility, not tactical win rate.

## Verification and licensing

The headless Unity diagnostic compiled the changed project scripts and completed its 200 seeded force-generation checks. No player build or manual tactical gameplay test was run. Source logic, affected documentation and the final diff were reviewed. The changed C# source carries a dated modification notice; root `CHANGES.md` lists the exact source path. Actual `License.txt` sections 4–5 were reviewed. Existing copyright, license, attribution and warranty notices remain. No external code, assets or dependencies were introduced. Separate public-distribution and third-party asset review remains necessary before conveying a binary.

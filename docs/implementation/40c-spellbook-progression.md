# 40C — Spellbook progression and tactical control

Implemented and accepted after grouped manual review on 2026-11-07. Prices and combat weights remain provisional for future balance tuning.

## Catalog

All towns offer the same unlimited stock. Existing book IDs and owned equipment remain intact. Merchant tiers unlock at reputation 0, 10 and 25 respectively.

| Tier | Book | Price | Role |
| --- | --- | ---: | --- |
| 1 | Power Bolt | 30 | Basic direct attack |
| 1 | Valor / Shield | 60 each | Damage buff / protection |
| 1 | Mending | 90 | Temporary battle healing over time |
| 2 | Predation | 170 | Vore support |
| 2 | Poison | 220 | Damage over time |
| 2 | Cinder Field | 270 | Modest initial area damage followed by burning ground |
| 3 | Trance | 360 | Resistible sleep, preventing action and movement while it lasts |
| 3 | Summon | 440 | Temporary enabled animal summon |
| 3 | Gate Maw | 600 | Area vore, subject to predator and capacity rules |

Tier refers to merchant availability; each spell uses its existing mana and action rules. Cinder Field is a range-six, nine-tile area that damages allies and enemies alike. Its fire ticks for three side turns and may be overwritten by another ground effect on a tile, as with the existing effect system. Direct spell damage uses the normal armor/injury rules; lingering fire and poison do not roll hit-based injuries. Fire also affects multiple occupants on the same tile. Existing characters and generated offers keep their equipment; newly generated experienced mages may carry a second eligible spellbook and its value enters the hiring quote. Summons use half the caster's accumulated progression and temporary summons remain confined to the battle.

The tactical AI considers available spell alternatives, verifies valid summon tiles and eligible animals, favors routes clear of fire, and weighs immediate attack value and opportunity-attack risk when deciding whether to leave a fire. Manual play and auto-resolve use the same tactical rules. The high-tier Trance is the initial control option; its sleep prevents all action, not only walking. Further specialized movement-only control and a wider spell library can be developed separately.

## Validation

The grouped native fixture passed 1,004 assertions, including catalog gates and price matching, duplicate spells, temporary healing, actual Cinder casting and mana, fire duration and ownership, AI escape planning, and multi-occupant fire without hit-based injuries. A separate actual battle save/resume fixture passed 51 checks, including hazard owner, strength and duration. These use synthetic data under `Logs`; deletion is suppressed.

A small paired native simulation used three seeds each for three level-five Lizard companies against rating-four forces: basic bolt only, bolt plus Cinder, and bolt plus a basic bow. All nine completed the contract. Permanent company losses were 2, 0 and 2 respectively; battle lengths varied from 27 to 54 turns. The Cinder sample suffered less damage in two seeds and more in one. The scenarios used synthetic loadouts without purchase costs and are too few to establish balance; they do demonstrate functioning AI battles, not optimal human play or economic profitability. Results are retained in `Logs/balance34f-625360b2df984a699814ef2899277c1b/report.json`. An earlier failed simulation exposed an outdated synchronous-autosave assumption in the old turbo benchmark and a second failed run used noncanonical synthetic gear; neither failure is treated as a passing gameplay check.

A grouped player review is still needed for book usability, visual presentation, spell effects and balance. Later work includes the broader magic library and event/narrative development. No external code, assets or dependencies were introduced.

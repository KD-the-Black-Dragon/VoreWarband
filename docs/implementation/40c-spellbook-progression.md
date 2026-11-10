# 40C — Spellbook progression and tactical control

Implemented and accepted after grouped manual review on 2026-11-07. A follow-up on 2026-11-09 added power tiers, town stock rotation and enemy level gates. Prices and reputation thresholds remain provisional.

## Current Company spellbook catalog

Company spellbooks use each spell's existing `SpellList.Tier` as its power tier. The provisional reputation thresholds for tiers I–IV are 0, 10, 25 and 50. The tier indicates spell power; mana costs and combat actions remain defined by the spell itself.

| Tier | Category | Book | Price |
| --- | --- | --- | ---: |
| I | Damage | Power Bolt | 30 |
| I | Damage | Fireball | 45 |
| I | Buff | Valor | 60 |
| I | Buff | Shield | 60 |
| I | Buff | Predation | 170 |
| I | Debuff | Prey's Hex | 90 |
| I | Other | Mending | 90 |
| II | Damage | Arc Bolt | 210 |
| II | Damage | Ice Blast | 240 |
| II | Damage | Cinder Field | 270 |
| II | Debuff | Bolas | 200 |
| II | Debuff | Poison | 220 |
| III | Damage | Diamond Storm | 440 |
| III | Buff | Enlarge | 360 |
| III | Debuff | Prey's Curse | 420 |
| III | Other | Maw | 430 |
| III | Other | Summon | 440 |
| IV | Damage | Flamberge | 900 |
| IV | Debuff | Diminishment | 650 |
| IV | Other | Gate Maw | 700 |

Each town stocks one or two spellbooks from each category that has unlocked options. The first selection in a category is always from its highest currently unlocked tier; an additional book may be drawn from any unlocked tier. A stable town-specific selection changes at payday and remains the same across saves until then. Ordinary gear continues to have the same stock in every town. Trance is retired and no longer appears in Company stock.

Newly generated enemy mages keep their basic Power Bolt and, from level 3, may receive a second spellbook at the highest tier allowed by their level: tier I below level 5, II at level 5, III at level 10 and IV at level 15. Enemy assignment ignores player reputation. New recruits' optional second book is limited by both their level and company reputation. NaturalCaster's generated innate spell is also limited by the unit's level. Existing characters and their saved equipment are unchanged; previously generated contract forces keep their stored roster.

Cinder Field is a range-six, nine-tile area that damages allies and enemies alike. Its fire ticks for three side turns and may be overwritten by another ground effect on a tile, as with the existing effect system. Direct spell damage uses the normal armor/injury rules; lingering fire and poison do not roll hit-based injuries. Fire also affects multiple occupants on the same tile. Newly generated experienced recruits include their gear value in the hiring quote. Summons use half the caster's accumulated progression and temporary summons remain confined to the battle.

The tactical AI considers available spell alternatives, verifies valid summon tiles and eligible animals, favors routes clear of fire, and weighs immediate attack value and opportunity-attack risk when deciding whether to leave a fire. Manual play and auto-resolve use the same tactical rules. Further specialized movement-only control and a wider spell library can be developed separately.

## Validation

The grouped native fixture passed 1,004 assertions, including catalog gates and price matching, duplicate spells, temporary healing, actual Cinder casting and mana, fire duration and ownership, AI escape planning, and multi-occupant fire without hit-based injuries. A separate actual battle save/resume fixture passed 51 checks, including hazard owner, strength and duration. These use synthetic data under `Logs`; deletion is suppressed.

A small paired native simulation used three seeds each for three level-five Lizard companies against rating-four forces: basic bolt only, bolt plus Cinder, and bolt plus a basic bow. All nine completed the contract. Permanent company losses were 2, 0 and 2 respectively; battle lengths varied from 27 to 54 turns. The Cinder sample suffered less damage in two seeds and more in one. The scenarios used synthetic loadouts without purchase costs and are too few to establish balance; they do demonstrate functioning AI battles, not optimal human play or economic profitability. Results are retained in `Logs/balance34f-625360b2df984a699814ef2899277c1b/report.json`. An earlier failed simulation exposed an outdated synchronous-autosave assumption in the old turbo benchmark and a second failed run used noncanonical synthetic gear; neither failure is treated as a passing gameplay check.

A grouped player review is still needed for book usability, visual presentation, spell effects and balance. Later work includes the broader magic library and event/narrative development. No external code, assets or dependencies were introduced.

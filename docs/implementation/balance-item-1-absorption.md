# Balance follow-up, item 1: absorption and mana

Date: 2026-11-08

## Implemented

- Dead-prey absorption now uses a global `1.25` speed multiplier. The existing body-size, trait and tag modifiers still apply. At the current locked settings, absorption ramping and multi-prey rate division remain off.
- HP healing produced by absorption now uses a global `0.5` multiplier. Existing nutrition traits/tags still apply before that multiplier. Vore still provides lasting HP recovery; it does not restore mana.
- The old absorption-mana calculation and its three `RestoreMana` calls were removed. Tactical mana regeneration remains at turn start and can be changed by traits/equipment. Existing dedicated spell/trait effects that restore or refund mana remain separate from absorption (for example, Magic Synthesis and Meditate).
- Mana Rich now only raises maximum mana by 50%. The soft-retired Mana Drain trait no longer grants mana from absorption; its retained nutrition penalty is described accurately.
- Enemy belly rubbing is enabled by default and remains a player preference. Friendly Regurgitation remains enabled by default. Vore-method choices remain as currently configured; they do not change combat balance.

## Formula and rounding notes

For each absorption update, the existing speed factor starts at the square root of predator body size divided by prey body size, then applies predator/prey absorption traits and tag modifiers. The new global multiplier is applied with the existing optional ramp; current defaults set that ramp to zero. Unless rate division is enabled, prey HP lost per update is rounded from `prey max HP × speed factor ÷ 15`, with a minimum of 1.

The pre-existing HP-healing amount is based on half the prey HP removed, modified by nutrition traits and tags. The new global `0.5` setting halves that amount after those modifiers. Integer conversion means a healing tick can round down to zero; small per-tick values will not always be exactly half of the previous integer result. Likewise, the `1.25` speed multiplier is applied before the existing per-tick rounding, so observed completion time changes in whole-turn steps rather than a fractional turn.

## Other balance settings reviewed, not changed

| Area | Current behavior/default | Decision |
|---|---|---|
| Digestion speed | Global multiplier `1.0`; existing damage rules remain | Leave unchanged |
| Escape from a predator | Uses INT versus VOR and current condition; global legacy escape modifier is `0` (neutral multiplier) | Leave as-is |
| Size effects | Accuracy and damage have separate size-difference formulas; global settings are `0.01` per configured interval, with a lower bound of 10 and interval of 5 | Leave unchanged as low impact |
| Critical hits | Chance uses the current stat formula plus modifiers; critical damage multiplier is `1.5` | Accepted as-is |
| Grazes | Miss-to-graze uses the same displayed accuracy for the second roll at 51%+; graze damage multiplier is `0.30` | Accepted as-is |
| Company HP recovery | 10% daily, 20% during field rest, 30% during town rest, with the existing minimum recovery and injury ceiling rules | Keep quicker than body-part injury recovery |
| Body-part injuries | 5/14/28 days for Bruised/Battered/Mangled; town treatment reduces remaining time by 2 days and affects the complication roll | Keep separate from ordinary HP recovery |
| Tactical mana | `ManaRegen` is restored at each unit's turn start. Campaign daily recovery also restores 60% max mana; special traits/spells can modify or restore mana | No change in this item |

Other balance work remains separate: contract power/rewards and opening difficulty; wages/upkeep; recruitment costs; weapon and armor power/prices; XP and downtime gains; random encounter scaling; and later spell/trait review. This note records the baseline; it does not retune those systems.

## Verification and licensing

No Unity build, automated test or gameplay playtest was run for this item. The source diff and settings paths were reviewed statically. Modified C# files have dated fork notices, and root `CHANGES.md` records this work. Actual `License.txt` sections 4–5 were reviewed; existing copyright, attribution, license and warranty notices were preserved. No external code, assets or dependencies were introduced. Separate third-party asset provenance and public binary-distribution review remain outstanding.

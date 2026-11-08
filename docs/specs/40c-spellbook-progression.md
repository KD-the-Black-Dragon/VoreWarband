# 40C — Spellbook variety and progression

Implemented and accepted after grouped player review, 2026-11-07. Part of [module 40](40-authoring-and-campaign-progression.md). See the [implementation and validation record](../implementation/40c-spellbook-progression.md) for the delivered catalog, provisional tuning and test limits. The sections below retain the original planning context; specialized movement-only control and further library expansion are future work.

## Current foundation

`Assets/Scripts/Core/CompanyEquipment.cs` provides explicit equipment IDs, two spell slots, merchant categories, whole-tier reputation gates, common unlimited stock and gear-inclusive recruitment quotes. Its current books provide Power Bolt, Valor and Shield. `CompanyPower.cs`, tactical AI, spell previews and native damage resolution must all understand additions.

The existing weapon/armor economy and accepted calibration are already implemented. This phase expands spell choices and their progression.

## Confirmed magic identity

The user wants magic to emphasize manipulation, buffs, debuffs, healing, protection, summoning and area control. Direct-damage spells remain a minority of the library. Keep a basic bolt option, with meaningful reasons to choose either a mage or an archer. A mana-limited attack must justify its cost, and ordinary bows must retain a useful role beside magic. Bows scale with AGI and spells with INT under the existing rules.

Wildermyth's environmental manipulation is an inspiration for this identity. The first catalog should express that through supported tactical effects. No furniture-communing subsystem is proposed. The curator is unfamiliar with the original spell list, so the source inventory must supply an understandable mechanical shortlist.

Area spells should emphasize area denial, with modest initial damage and substantial consequences for remaining inside. Hazard-aware AI is a prerequisite for shipping such spells. Evaluate leaving a hazard, detouring, approaching targets, casting into occupied areas and remaining inside when the tactical payoff justifies the cost. Account for opportunity attacks when comparing escape routes. Manual combat and Auto-resolve must share these rules.

Movement prohibition belongs at high level. Confirm duration, targeting and interactions with special movement during the spell review. Summoning and Gatemaw are explicitly appealing high-level themes. Inspect their existing behavior and dependencies before proposing inclusion, and preserve curated summon eligibility and temporary-summon lifecycles.

Healing spells use the accepted temporary-HP model. Permanent recovery remains governed by established recovery and vore mechanics. Area effects need explicit damage categories so direct spell damage, ongoing effects, armor and injury eligibility retain their approved distinctions.

## Design work and bounded implementation

1. Inventory retained spells by practical role, targeting, AP/mana costs, duration, friendly fire, AI support, retirement status and existing bugs. Present a compact mechanical shortlist for curator approval.
2. Select a small first catalog centered on buffs, debuffs, temporary healing, protection and manipulation. Include a basic direct-damage option and present later summoning, Gatemaw and movement-prohibition candidates separately. Maintain direct damage as a minority across the planned library.
3. Assign approved books to existing reputation tiers with provisional prices and power values, using 40A data where supported. Preserve stable inventory IDs and existing owned items.
4. Check equipped books, innate spells, NaturalCaster and class/focus behavior together. Avoid duplicate spell offerings, free gear value exploits and abilities that bypass the accepted action economy.
5. Check actual AI use, hazard-aware movement, target selection, previews and temporary-effect cleanup. Area-denial effects require saveable duration/ownership and native turn processing. Permanent spell healing, resurrection, conversion, polymorph and other retired mechanics remain excluded.
6. Measure meaningful tradeoffs in native battles and connected economic scenarios. Report provisional costs and the situations where each book helps before requesting one grouped player review.

## Save and grant boundaries

Existing books retain their IDs and saved state. Import validation rejects unknown or retired spell references. New catalogs do not reroll existing recruits, accepted forces or owned items. Any proposed rebalance to an existing definition follows the compatibility policy chosen in 40A. Newly generated enemy forces must be scored for their actual spells and mana capability within the fixed rating budget.

## Planned grouped acceptance

Cover both book slots, equipment swaps, duplicate spell sources, mana/AP limits, damage previews, area targeting, AI cast decisions, temporary statuses, save/resume, pricing and gear-inclusive hiring. Include hazard entry, continued exposure, exit, overlapping zones, expiry, path choices with opportunity attacks and Auto-resolve parity. Compare bow/magic roles and mana costs in actual small-company scenarios. Reuse native Battle Lab and simulation infrastructure with synthetic fixtures. Player review covers choosing and using the approved catalog, without a narrative-writing requirement.

## Decisions before implementation

Approve the source-derived mechanical shortlist and any genuinely new spell behavior. The overall utility/control emphasis and high-level placement of movement prohibition are confirmed. Specific effects, durations and tiers require the inventory first. Derive provisional prices and scoring through calculation and simulation, following the user's existing tuning preference.

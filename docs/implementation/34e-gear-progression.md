# Spec34 slice5 — Gear progression and recruitment costs

Implemented 2026-11-04 after user acceptance of slice4. User confirmed whole-tier reputation unlocks, the same stock in every town, unlimited quantities, and charging recruits' equipment alongside advance wages. User explicitly requested increased new-campaign starting funds to compensate. Values below are provisional tuning, not measured win-rate/economy calibration.

## Catalog and availability

Three tiers unlock at reputation0/10/25. Unlocks reveal the whole tier and retain lower tiers. The merchant filters/paging use the same eligible-stock function as the purchase command; direct calls cannot bypass reputation. Owned equipment remains usable/sellable at lower reputation. No town inventory, restock clock, limited quantities, crafting or regional system was added. Each purchase creates a distinct stable item instance.

The original slice5 catalog offered accuracy-rating versus damage weapon alternatives. The 2026-11-08 follow-up replaces those with one generic melee and one generic ranged choice per tier, with species-native art and names in combat logs; existing accuracy mechanics remain rating multipliers. The original slice5 catalog offered AC and evasion armor alternatives; the 2026-11-08 follow-up removed evasion-granting armor from new stock and recruit generation while retaining definitions for saved items. Tiered charms cover the four stats. Existing Power Bolt/Valor/Shield books remain; no unreviewed spells introduced. Names use existing project art; no new narrative or artwork was introduced.

Existing definition IDs, serialized damage/range/accuracy/stat bonuses and ownership are preserved. Existing armor definitions keep slice4's AC/evasion. Catalog-owned saved gear refreshes its display name and weapon art family from the stable ID, but combat stats are not rewritten. Transactions and hiring quotes use current catalog prices; sale proceeds remain half the catalog price. This can revalue prototype-era items but does not grant treasury adjustments on load.

## Provisional investment scale

| Category | Tier I | Tier II | Tier III |
| --- | ---: | ---: | ---: |
| Melee weapons | 70–100 | 180–200 | 750–900 |
| Ranged weapons | 90–120 | 230–260 | 850–1000 |
| Armor pieces | 30–60 | 120–240 | 450–950 |
| Stat charms | 60 | 220 | 800 |

Books cost30/60/60. Contract rewards remain rating×60 with the existing modest rounded variation: rating1 roughly60, rating4 roughly240, rating9 roughly540. A basic melee weapon therefore exceeds one typical rating1 reward; a masterwork weapon is several gross rewards even from stronger offers. Fully equipping multiple mercenaries competes with wages and treatment. No contract gold, reputation gain, wage amounts or payday rules were altered to fund gear.

Full heavy sets provide AC5/10/15. The original evasive sets' 12/20/30 percentage-point values remain only on already-owned saved gear; those pieces are no longer available to buy or generate. Controlled balance simulations remain slice6.

## Recruitment and generated loadouts

Hire quote = current equipment value + prorated advance wages + full personal debt. This applies to first hires and former mercenaries returning with retained gear. Hiring pays the quoted total once, clears debt and transfers gear into canonical company inventory. Dismissing someone leaves their worn equipment with them as before. Stripping/reselling cannot produce profit because the full gear cost was paid while resale returns half. The UI itemizes gear/advance/debt on recruitment rows and detail pages.

New-campaign starting gold increases50→260. Starter gear remains Mace70, Simple bow90, and mage Mace70 + Power Bolt30, with weekly wage18–22. A full-week starter hire costs88–122. Any two cost at most244, while the cheapest three cost at least264: two comfortable starter choices, with a remaining budget depending on composition. The leader's initial gear remains part of character creation; it is not deducted. Existing campaigns receive no top-up, and debt/wages remain unchanged.

New generated higher-level sapients draw role-appropriate weapons/armor from tier I at levels1–4, II at5–9, III at10+. Level1 retains its guaranteed basic role weapon; armor still starts at level3 with a second piece at6, preserving the original loadout cadence. The scorer prices actual stats/gear, so fixed contract budgets remain independent of player reputation/treasury and do not use item prices. Recruitment still respects its power ceiling. Existing saved candidate/roster/enemy loadouts are never rerolled; leveling a current member does not automatically upgrade equipment.

## Validation and scope

Extend the existing authorized isolated Unity audit only. Synthetic saves stay under Logs and are preserved; deletion is intercepted, no personal saves. Check all catalog IDs/slots/prices, reputation boundaries, lower-tier retention, shared town stock, unlimited unique purchases, core purchase rejection, AC/evasion alternatives, merchant visibility/paging, starter affordability, itemized prorated hiring/debt, resale exploit prevention, former gear valuation and save/reload without treasury top-up. Prior trait/injury/combat/equipment checks remain.

Source paths/notices are recorded in CHANGES. Original fork code and existing generic artwork only; no dependencies/assets/imports/scenes/prefabs added or files moved/deleted. License4–5 reviewed, prior legal notices/attribution preserved. Existing distribution/provenance/Corresponding Source gates remain open. Build/check results recorded at handoff below.

Stop after slice5. Next slice6 is controlled balance reporting, not automatic tuning or new narrative. Help rewrite/post-battle events remain deferred.

## Handoff

Native runtime1 passed883 checks (731 prior plus152 progression checks), actual editor exit0. Catalog report has52 shop items:19/17/16 in tiers I/II/III, plus one enemy-only slug weapon excluded from shops. No C#/shader errors or new compiler warning messages; fresh Windows build `Builds/Warband-Review-20261104-15ad5c3f/Win64/VoreWar.exe` exit0,41 unchanged distinct compiler warnings. Seven existing notices packaged/hash-verified. No graphical playtest or balance calibration claimed.

Evidence: Logs/progression34e-runtime1-result.json, runtime1.log, progression34e-catalog.json, build.log and progression34e-result.json. Source dates/CHANGES reviewed, README points to the fresh build; earlier builds/logs remain. User review: fresh campaign treasury260, recruit quote/affordability, merchant categories/tier notices and purchases. Existing campaigns retain their treasury and existing gear. No request to grind25 contracts merely to retest automated unlock boundaries. Next work awaits user direction on slice6.

## Catalog follow-up — 2026-11-08

The player-facing armor names are Helmet, Tunic, Greaves and Bracers. Active armor uses the following stat bonuses, with torso/Tunic stronger than the other three slots:

| Tier | Helmet, Greaves, Bracers | Tunic |
| --- | ---: | ---: |
| I | +2 | +4 |
| II | +4 | +6 |
| III | +6 | +8 |

Heavy armor retains AC equal to its tier on non-torso slots and twice its tier on Tunic, giving full sets AC 5/10/15. “Guarded” was removed from generated names. Evasion-granting armor definitions remain only to resolve already-owned saved gear; those pieces are no longer offered or newly generated.

Mace remains the Tier 0 melee baseline at 4 damage; Simple bow is the Tier 0 ranged baseline at 3. Melee weapons deal 11/18/25 at Tiers I/II/III. The four available ranged weapons are Simple bow (Tier 0, 3 damage), Hunting bow (Tier I, 9), Compound bow (Tier II, 16) and Masterwork bow (Tier III, 23); all use range 6 and a 1.0× accuracy modifier. Fine bow, War bow and Masterwork war bow remain defined only for compatibility with existing saved gear and are not available in shops or new generated loadouts.

New purchases/generated items use these catalog values and names. When an existing company save is loaded, catalog-owned gear refreshes its display name (including removing “Guarded”) while its serialized damage, armor and stat values remain unchanged.

## Weapon artwork follow-up — 2026-11-08

Company weapons now select explicit shared artwork families: light melee, heavy melee, light ranged, heavy ranged, and alternate light melee. These use existing paired idle/attack sprites. Species-specific paper-doll renderers still control placement, layer, and species presentation; the company weapon selector supplies the equipped item's graphic family. Legacy randomized light-melee variants are bypassed for company equipment so they cannot replace the selected graphic. When both a melee and ranged weapon are available, the unit displays the last category it used. A new character defaults to melee when it has one; if it has only ranged gear, that weapon is shown. The selection is saved with the unit. A weapon entry with no assigned art intentionally displays no held item.

The Race Editor's password-gated developer area now has a Weapon art gallery beside Balance workflow. It discovers art by calling the weapon renderer for all currently defined species, including species that are not curated for campaign use. It numbers each species/graphic-slot mapping, shows its idle and attack frames, includes separate body-form variants when the weapon art differs, and lists species with no mapped weapon art. Graphic-slot names describe their code role; they are not claims about what the depicted object is. Retired weapon definitions remain visible as non-purchasable stubs in the equipment catalog. The gallery uses existing project sprites and adds no artwork, purchases, or game content by itself.

Existing saves retain their weapon items and statistics. The new last-used category field defaults to melee on older saves, with fallback to whichever equipped weapon exists. Unity 2018.3.12f1 compiled and built the expanded gallery and transplant trial at `Builds/Warband-Test-20261108-weapon-art-transplant-v2/Win64/Vore Warband.exe`; the trial still needs visual review in-game.

## Weapon art identification — 2026-11-08

The owner identified the numbered mappings shown in test build `Builds/Warband-Test-20261108-weapon-art-gallery-all-species-v2/Win64/Vore Warband.exe` as follows. These are visual identifications, not final assignment decisions. “Unsure” and “Not a weapon” are retained as stated. Numbering refers to that gallery build and can change if its discovered mappings change.

1. Tiny mace
2. Large axe
3. Small bow
4. Large fancy bow
5. Sword
6. macuahuitl
7. Axe with decorative feathers
8. Large bow with decorative feathers.
9. Javelin with decorative feathers.
10. Not a weapon.
11. Small club
12. Large spear
13. Boomerang
14. Large javelin
15. Small axe
16. Whip
17. Throwing Axe
18. Bow
19. Large spear
20. Large naginata
21. Unsure. Bolas maybe? Very hard to tell.
22. Unsure
23. Not a weapon
24. Spear
25. Small javelin
26. Small javelin
27. Clawed gloves on the feet (very bat specific)
28. Different clawed gloves on the feet
29. Unsure
30. Unsure
31. Small curved knife
32. Kukri
33. Unsure
34. Unsure
35. Same knife as 31
36. Same kukri as 32
37. Unsure
38. Unsure
39. Knife
40. Short sword
41. Small bow
42. Crossbow
43. Don't know, doesn't look like a weapon.
44. Large spear
45. Four daggers (specific to a multi-armed ant)
46. Bow
47. Larger bow
48. Arms? Gauntlets?
49. Spiked club
50. Axe
51. Slingshot
52. Bow
53. Cutlass
54. Saber
55. Spear with gnarled shaft
56. Harpoon gun
57. Not a weapon
58. Spear
59. Halberd
60. Bow
61. Larger bow
62. Not a weapon
63. Dagger
64. Bardiche
65. Small crossbow
66. Large bow
67. Not a weapon
68. Pick
69. Pick
70. Pick
71. Pick
72. Pick
73. Spiked crude club
74. Cleaver
75. Javelin
76. Bow
77. Spiked crude club
78. Spiked crude club
79. Cleaver
80. Javelin
81. Bow
82. Quarterstaff maybe?
83. Sword
84. Revolver??
85. MP-40??? (Why do they have modern guns in this game...?)
86. Not a weapon
87. Same as 82
88. Same as 83
89. Same as 84
90. Same as 85
91. Quarterstaff again
92. Khopesh
93. Ancient Egyptian-style axe.
94. Unsure
95. Bow
96. Axe
97. Poleaxe
98. Crossbow
99. Shiny crossbow
100. Unsure, maybe a mace.
101. Spear
102. Pick
103. Pick
104. Pick
105. Pick
106. Pick
107. Axe
108. Knife or short sword
109. Bow
110. Shinier bow
111. Not a weapon
112. Sword
113. Khopesh
114. Throwing axe
115. Labrys
116. Bow
117. Larger bow
118. Not a weapon
119. Throwing axe
120. Glaive

### Visual transplant trial — developer gallery

The password-gated Weapon art gallery provides a trial view for the numbered sprites above. Select Avian, Lizard or Gnoll, choose the body form and idle/attack frame, then compare every discovered sprite on that species' paper doll. The target species renderer supplies the item's normal slot position and layer; the trial substitutes only the source sprite. It does not change equipment assignments, shop stock or campaign content. Renderer offsets and sprite delegates are restored after the preview is generated.

The 120-entry list is still numbered in discovery order, matching the identification list above. Cross-species substitution remains an isolated developer diagnostic only; production weapons use each species' own light/heavy art family. The user's inspection rejected transplanting sprites between species because the weapon assets are species-specific. Descriptive words in the identification list (such as “with decorative feathers”) distinguish the pictured artwork only; they are not part of in-game weapon names.

## Generic weapon names and native species art — 2026-11-08

The company shop and inventory use **Sword**, **Sword I**, **Sword II**, **Sword III** and **Bow**, **Bow I**, **Bow II**, **Bow III**. New shops and generated forces have one melee and one ranged choice at each tier. Duplicate legacy melee IDs remain resolvable for existing saves but are retired from purchases and new generation; the better-value, higher-accuracy profiles remain active at tiers II and III.

The catalog ranks active weapons in each class by damage × accuracy rating. The strongest floor(n/2) entries receive the heavy-art family; the remaining entries receive light art. With the current four tiers, tiers II and III use heavy art, while baseline and tier I use light art. These are family IDs passed through the unit's own species renderer, so the equipped sprite, placement and offsets stay species-native; no art is transplanted between paper dolls.

On campaign load, catalog-owned items refresh their generic display name and assigned family from the stable definition ID without changing saved damage, range, accuracy, ownership or price paid. Company combat logs use species-native weapon names for Avians, Lizards and Gnolls, with descriptive modifiers removed (for example, Bow rather than Short Bow or Feathered Bow). The Lizard melee names are Macuahuitl and Axe; the Gnoll melee names are Club and Cleaver. Other unreviewed species/category combinations currently fall back to the generic item name until curated.

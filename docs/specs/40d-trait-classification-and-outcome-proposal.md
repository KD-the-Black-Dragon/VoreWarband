# 40D trait classification and event outcome record

**Status:** curator decisions incorporated for 40D implementation (2026-11-07); numeric thresholds and reward tuning remain provisional.  
**Prepared:** 2026-11-07.  
**Parent spec:** [40D post-battle evidence and earned trait progression](40d-post-battle-trait-progression.md).

This document records the curator's 40D trait decisions and the working reward catalog used by the implementation. The 23 **soft-retired** names remain in source for possible future reconsideration, but are excluded from the post-battle reward pool. The remaining 173 names are classified Good, Bad or Neutral. Every one of the original 196 names is accounted for exactly once across these statuses. **Grudge I–III is an added Bad tier family.** Numeric event thresholds, candidate reward choices and wage increments remain provisional and can be tuned after play.

## Classification rule

Classify each trait from the trait-holder's perspective in the mercenary campaign:

- **Good** gives the holder a clear, general advantage.
- **Bad** gives the holder a clear, general disadvantage.
- **Neutral** has a meaningful tradeoff, a matchup-dependent effect, or an uncertain/broken effect.

For traits whose effects depend on being digested, assess the consequence for the holder. Tasty increases the predator's healing and Disgusting reduces it; both are soft-retired and therefore do not enter the live polarity totals or reward pool. Neutral traits count toward total trait weight and subject preference, but not toward either Good or Bad total. They are not post-battle rewards because they cannot move the Good/Bad balance.

Traits designated **inborn** describe a character's lifelong condition. Assign them only when generating that character (including a new company leader, recruit, or contract enemy); never add, remove, or change them later in the campaign. They still contribute to Good/Bad polarity counts. Size remains unchanged mechanically: larger scale traits are Good and smaller scale traits are Bad, all inborn.

## Soft-retired traits

These remain preserved in source for possible future review. They are neutral for 40D balancing and are not post-battle rewards. This phase does not remove their other legacy assignment/effect paths, except that inborn traits are protected from mid-campaign transfer and removal as described above:

**AcellularBody, AllOutFirstStrike, Competitive, Crystaline, Disgusting, EasilySatisfied, Elite, Elementist, Feral, GelatinousBody, HeavyPounce, ManaAttuned, ManaBarrier, ManaDrain, ManaRich, PleasurableTouch, SerialSwallower, SiphoningAura, SleepItOff, Tasty, Tempered, WastefulProcessing, WeaponChanneler**.

Crystaline is among these because it remains known broken/low priority. No artwork, code or files are being deleted as part of this list.

### Curator-reviewed Good traits

**Tiered families (all available tiers):** Assassin, Deadeye, Dodger, HardPrey, Hearty, KeenShot, MagicFinesse, MagicResistance, ManaBattery, PowerCaster, Precise, Resilient, StrongMelee, WideMaw

**Other traits:** AcidImmunity, AcidResistant, ArcaneMagistrate, Berserk, Biter, BladeDance, Blitz, BoggingSlime, BornToMove, BoundWeapon, Bulky, ColdTolerance, Colossal, Crusher, Dazzle, DefensiveStance, DexterousDefense, DireInfection, DoubleShot, DualStomach, Duelist, EfficientGuts, EscapeArtist, FastAbsorption, FastCaster, FastDigestion, FasterAbsorption, FasterDigestion, FearsomeAppetite, Fit, Finesse, FocusedDodge, ForcefulBlow, Frenzy, Fervor, GiantSlayer, GlueBomb, Grounded, HardSkin, HotBlooded, Huge, Intimidating, InvigoratingEscape, IronGut, KeenEye, KeenReflexes, Large, LavaWalker, MadMage, MagicSynthesis, Maul, ManaBurn, ManaDynamo, MetabolicSurge, MountainWalker, NaturalCaster, NightEye, NimbleClimber, PackAgility, PackMind, PackStrength, PackTactics, PackVoracity, Paralyzer, PassThrough, PeakCondition, Perseverance, Petrifier, PoisonSpit, PollenProjector, Pounce, QuickLearner, RangedVore, Ravenous, Regeneration, SedativeStomach, SenseWeakness, SmokeOut, SpectralStep, SpellBlade, SteadyStomach, Stinger, StretchyInsides, StunningStrike, SweepingStrikes, SwiftStrike, TailStrike, Talented, TasteForBlood, Tenacious, ThrillSeeker, Titanic, Toxic, Unflinching, UnpleasantDigestion, Vampirism, VenomousBite, VenomShock, Vindication, ViralBiology, WaterWalker, Webber

**Availability restrictions:** Duelist and Fervor are Good but player-side only; do not place them on AI-generated NPC forces. The size traits Colossal, Huge, Large and Titanic are Good, while Small and Tiny are Bad; size differences are assigned at character generation and are never changed later. Source review confirms that larger scale makes a unit bulkier, changes size-based vore odds, and makes the unit harder to vore when it is the target. Larger scale does **not** currently increase its own stomach capacity: that multiplier is commented out in `PredatorComponent.TotalCapacity`. ManaBurn is classified Good because it damages a target's mana and can spill into health damage when the target has no mana. ManaDynamo is reclassified Good: retain its +3 mana per turn and remove the mana it grants to a predator when absorbed. Regeneration is reclassified Good: retain its temporary combat healing and remove its extra nutrition to a predator when absorbed. TailStrike is Good because it is an optional activated attack, not a mandatory replacement for normal attacks. It consumes the remaining AP, hits its selected adjacent target twice at reduced damage, and can also strike nearby orthogonal enemies.

### Curator-reviewed Bad traits

**Tiered families (all available tiers):** Brutish, Clumsy, EasyPrey, ErrantMage, Fizzler, Flatfooted, Frail, MagicIneptitude, MagicVulnerable, Nearsighted, RangedIneptitude, WeakAttack, WeakBones, WeakGullet

**Other traits:** AwfulAim, DesertImpedence, Diseased, EasyToVore, ElecVulnerable, Farsighted, FireVulnerable, FoodComaProne, Foolish, ForestImpedence, GrassImpedence, Greedy, HighlyAbsorbable, HillImpedence, IceVulnerable, IdealSustenance, Illness, IntrusiveAppetite, Lethargic, Nauseous, Opossum, PleasantDigestion, PoorConstitution, ProteinRich, SlowAbsorption, SlowDigestion, SlowerAbsorption, SlowerDigestion, SlowerMetabolism, SlowMetabolism, Small, SnowImpedence, SoftBody, Spartan, Submissive, SwampImpedence, Timid, Tiny, VolcanicImpedence

Spartan remains Bad but has a 15% stat increase as compensation. This bonus is implemented; the trait still prevents fleeing.

### Curator-reviewed Neutral traits

Featherweight, Juggernaut, MutualBiology, Slippery

Neutral is not a judgment of quality. These are mixed or matchup-sensitive. Featherweight is Neutral and inborn only. Juggernaut is Neutral, NPC-only, and never a post-battle award; its tooltip now states that it skips every other turn. MutualBiology is NPC-only and never a post-battle award. Slippery remains Neutral.

## Provisional occurrence thresholds

These numbers translate the curator's qualitative examples into provisional implementation rules. They are intentionally tunable after play.

| Occurrence | Proposed threshold |
| --- | --- |
| Mercenary or leader digests an unusually large share | At least 6 deployed enemies; subject digests more than half of that force and has the highest allied digestion count. If multiple mercenaries tie for highest, draw randomly among them. For the leader-sourced Grudge event, the leader must digest more than half of the force; the recipient is chosen from eligible surviving mercenaries. |
| Escapes or is freed from vore 3+ times | At least 3 incidents, and the subject has the unique highest incident count; ties among the top count are drawn randomly. |
| Sustains 3+ injuries | At least 3 battle injuries to the subject. |
| Significant company losses | More than 50% of deployed company combatants are digested. Reserves are excluded. |
| Adjacent to enemy digestion multiple times | At least 2 completed enemy digestions while the subject is adjacent. |
| Last active company member standing | Subject survives after being the only active company unit remaining at some point, with at least 4 company fighters deployed at battle start. This is a provisional threshold because 1–2 mercenaries were judged too small to make the event notable. |
| Mercenary downs the majority of enemies | At least 6 deployed enemies; the subject personally delivers the finishing blow to more than half of them. |
| Hit-rate comparison | At least 5 melee or ranged attacks for each compared mercenary. |
| Critical-rate comparison | At least 5 successful attacks of the same attack type for each compared mercenary, since only hits can critically hit. |

For strict-majority checks, exactly half does not qualify. Highest/lowest-rate ties are randomly resolved among tied eligible mercenaries. These minimums do not cap battle size; the 6-v-6 example is a benchmark for avoiding trivial majority awards.

## Provisional relationship between battle events and trait outcomes

These are the working reward pools. The event must first qualify under the occurrence rules, then the polarity rule chooses an outcome from the matching column. Outcomes use related trait families; they do not add authored narrative. If the required polarity has no legal outcome, discard that event and continue to the next candidate/tier. If none remain, show no post-battle event.

| Qualified occurrence | Good-side candidates | Bad-side candidates |
| --- | --- | --- |
| Mercenary digests an unusually large share of a sufficiently large enemy force | Frenzy, WideMaw, FastDigestion | Nauseous, WeakGullet, SlowDigestion |
| Leader digests an unusually large share | No Good counterpart; this occurrence is workable only when Bad is selected | Grudge I–III for an eligible surviving mercenary |
| Mercenary escapes/is freed 3+ times and has the highest count | EscapeArtist, InvigoratingEscape | Timid, Opossum |
| Mercenary sustains 3+ injuries | Resilient, Tenacious | WeakBones, Frail |
| Digests a marked boss | Frenzy, WideMaw, FastDigestion | Nauseous, WeakGullet, SlowDigestion |
| More than half of deployed company combatants are digested | Resilient, Tenacious, PackTactics | Timid, Opossum, EasyPrey |
| Mercenary is adjacent to a digestion at least twice | Berserk, TasteForBlood | Timid, Opossum, PoorConstitution |
| Mercenary is the last active company member standing and survives | Resilient, Tenacious, Perseverance | Timid, Opossum, PoorConstitution |
| Adjacent to one completed enemy digestion | Berserk, TasteForBlood | Timid, Opossum, PoorConstitution |
| Mercenary deals the blow that downs most enemies in a sufficiently large force | Matching melee/ranged/magic accuracy or damage family | Opposing accuracy or damage family for that attack type |
| Reaches 0 HP and survives | Resilient, Hearty, HardPrey | WeakBones, Frail, EasyPrey |
| Receives 1–2 injuries | Tenacious, Resilient | WeakBones, Frail |
| Highest melee/ranged/magic damage | Matching damage family | Opposing damage family |
| Spends the most mana | ManaBattery, PowerCaster, MagicFinesse | Fizzler, MagicIneptitude |
| Highest/lowest melee or ranged hit rate | Matching accuracy family | Opposing accuracy family |
| Highest melee or ranged critical rate | Assassin | Brutish |
| Escapes/is freed once | EscapeArtist, InvigoratingEscape | Timid, Opossum |
| Survives damage exceeding normal maximum HP without being downed | Resilient, Hearty | WeakBones, Frail |
| Survives more incoming opportunity-attack attempts than ordinary incoming attacks | Dodger, Resilient | Flatfooted, WeakBones |

The same family may appear in multiple rows because its relationship to the measured event is direct. The table does not assert that every listed candidate is always legal: existing family tier, polarity arithmetic, species assignment, and already-held traits still have to be checked. Event-specific prose will be written later by the curator.

## Implemented selection and progression details

- Keep the approved eligibility and category order: at least three deployed units, at least one surviving hired active mercenary, no event if the leader is the sole survivor; exceptional, then mid-tier, then mundane. Reserves and summons are excluded.
- Keep random selection among multiple qualifying occurrences within the highest tier. A subject preference must not silently replace that event draw. Apply the “fewer traits” preference when an occurrence permits multiple equally valid recipients or tied performers; use random choice for equal-weight candidates.
- Proposed tie rule: tied performers remain equally eligible and are selected randomly. A unique-highest condition is required only where the occurrence definition explicitly says “highest.”
- Proposed small-sample guard: require at least five relevant attacks before comparing hit or critical rates; compare only mercenaries who meet the same minimum. A tied maximum/minimum remains a random tie.
- Use only tier I for a new tiered trait; repeated eligible outcomes can raise it one tier at a time. This makes each acquisition/upgrade a one-point balance change. The validator must evaluate the actual before/after weighted Good/Bad counts, not merely the label of the trait.
- At a Good/Bad difference of 2 or more, accept only an outcome whose resulting absolute difference is smaller. At a difference of 1, use the confirmed 75%/25% direction and reject an outcome that moves further away by more than one point. At equality, use the confirmed 50/50 direction. A candidate is not made workable by substituting a Neutral trait.
- Tiered trait families and their opposing families share an exclusive effect. Replacing a held trait must pass the same before/after balance check. Downgrades/removals remain limited to one tier step and require at least 8 total weighted trait points, as already specified.
- Leader digestion can award Grudge only when the Bad polarity is selected and the recipient's trait state allows a legal Grudge change. Otherwise discard that occurrence and examine other candidates; there is no positive Grudge counterpart.
- The occurrence catalog intentionally has no generic fallback. A battle with no listed qualifying occurrence, or no workable polarity/outcome, produces no event.

## Provisional Grudge wage values

Use an additional raise based on the **normal wage increase for that level-up**, not the mercenary's entire wage:

| Grudge tier | Additional permanent raise on each future level-up while held |
| --- | ---: |
| I | +25% of the normal level-up wage increase |
| II | +50% |
| III | +100% |

Round the extra raise upward to a whole gold, with a minimum increase of 1 gold whenever the normal raise is positive. Calculate each level-up from the normal existing wage curve, then add this level-up's Grudge amount to a separately stored permanent surcharge. Grudge does not change the current contract or current debt; the surcharge is effective at the next regular payday. Removing Grudge later does not remove any surcharge already earned. These are provisional implementation values that may be tuned after play.

For scale, the current normal wage increase from level 1 to 2 is 5 gold, so Grudge I/II/III would add 2/3/5 gold. From level 2 to 3, the normal increase is 9 gold, so it would add 3/5/9 gold. The increment naturally grows with the existing wage curve.

## Curator decisions and implementation limits

- The curator's item-by-item review sets the soft-retired list, polarity categories, neutral/inborn exceptions, NPC-only traits, optional TailStrike reward status, and Spartan's Bad classification with a 15% stat bonus. These classifications are applied to post-battle balancing; this phase does not broadly delete or rewrite unrelated trait mechanics.
- Anything declared inborn is fixed for life: it may be assigned only during character generation, cannot be added, removed or changed mid-campaign, and still contributes to trait weight and Good/Bad counts. Current inborn set: size traits and Featherweight.
- Duelist and Fervor are player-side only; Juggernaut and MutualBiology are NPC-only; none are post-battle outcomes. Neutral and soft-retired traits are not rewards.
- Tier III contributes three points everywhere. A selected downgrade reduces a tier by one step, and tier I or an untiered trait may be removed only after the weighted total reaches eight. The current reduction chance starts at 5% at eight points and rises 5 percentage points per additional point, capped at 30%.
- The code uses a five-attempt minimum for rate comparisons; random occurrence/tie selection; at least six enemies for majority events; at least four deployed company fighters for the last-active event; and strictly more than 50% company losses. These are provisional implementation choices, not final balance values.
- Grudge I/II/III currently adds 25%/50%/100% of a future normal level-up wage increase (round up, at least one gold for a positive raise). Each earned raise is permanent after Grudge is removed and affects pay from the next regular payday.
- Boss digestion is recognized for explicitly marked boss contracts. No generic event fallback is used. If no occurrence or polarity-valid trait outcome remains, the battle has no post-battle event.

The main phase specification describes the event sequence, saved evidence and remaining grouped player acceptance. No build or gameplay tests have been reported for this implementation yet.


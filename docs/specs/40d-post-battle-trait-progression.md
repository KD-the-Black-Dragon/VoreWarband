# 40D — Post-battle evidence and earned trait progression

Implementation record, 2026-11-07, updated 2026-11-10. Part of [module 40](40-authoring-and-campaign-progression.md). Phase 40D adds saved battle evidence, polarity-balanced trait outcomes, and the post-battle notice sequence. Thresholds and reward values called provisional below are initial tuning for later grouped review. The curator supplied the remaining event narratives; their wording is preserved, with pronouns routed through the existing unit pronoun system. The single-escape passage is a curator-approved, milder adaptation of the curator's repeated-escape passage.

## Confirmed direction

The user wants a post-battle event when eligible non-leader participants remain. A battle is eligible only if at least three units were deployed (leader plus at least two mercenaries) and at least one hired active mercenary survives. A leader who is the sole survivor does not receive an event. Reserve units never participate in event evidence or qualify as subjects. The leader is ineligible for most event subjects and never receives a post-battle trait; the exception is that a leader's unusually large digestion count may trigger a separate company envy/Grudge notice received by a mercenary. Higher natural stats are the leader's compensation. Other player-influenced leader-trait sources, such as road events, remain future work. Events recognize actual battle experiences and can grant positive or negative related traits. Extraordinary experiences take priority over mundane ones.

Examples already supplied by the user are unusually high ranged activity and surviving being swallowed at least three times. The implementation uses the reviewed occurrence catalog below and assigns a related trait in light of the unit's existing traits. Within each tier, it randomly selects among qualifying occurrences. The ordinary catalog selects at most one event. A qualified leader-glory Grudge notice is resolved independently and may appear as an additional event.

## Working occurrence catalog

The curator's initial examples and follow-up decisions (2026-11-07) establish the following implemented categories. See [40D trait classification and outcome record](40d-trait-classification-and-outcome-proposal.md) for the curator-reviewed polarity and reward catalog. Prefer exceptional candidates over mid-tier, then mundane candidates; choose randomly among occurrences in the highest qualifying tier. A battle with no listed qualifying ordinary occurrence has no event—there is no generic or mundane fallback. A qualified leader-glory Grudge notice is checked separately and may accompany one ordinary event.

### Exceptional candidates

- A mercenary digests an unusually large number of enemies and leads by count. Require a sufficiently large opposing force; a three-enemy battle must not qualify.
- The company leader digests an unusually large number. This is a company experience that may make mercenaries feel envious; the leader is never the trait recipient.
- A mercenary escapes or is freed from being swallowed at least three times and has the highest count in that battle.
- A mercenary receives at least three injuries in battle.
- A mercenary digests a designated boss on a “Rout the boss” contract. Boss identity is explicit in saved contract data; do not infer it from level or relative power. Downing or routing the boss completes the contract but does not count as the boss-digestion post-battle event. If the leader digests the boss, there is no boss-specific post-battle reaction; the separate large-digestion event can still qualify only if its own enemy-count and digestion thresholds are met.
- The company suffers significant losses: strictly more than 50% of its deployed combatants are digested. Count company fighters deployed to the battle; reserves are not combatants in that battle. For a 6-person deployed company, 4 losses qualify and 3 do not.
- A mercenary is adjacent when an enemy completes a digestion at least twice.
- A surviving mercenary becomes the last active company member standing during the battle. Require a minimum starting company size; a two-person company cannot qualify.

### Mid-tier candidates

- A mercenary is adjacent when an enemy completes a digestion once.
- A mercenary deals the blow that downs most of the enemy force. Use 6-v-6 as the initial benchmark when evaluating majority-based event thresholds. The exact minimum force size remains undecided; this is a benchmark, not a squad-size cap.

### Mundane but notable candidates

- Reaches 0 HP and survives the battle.
- Receives one or two injuries, especially if no other deployed company member is injured.
- Deals the highest melee or ranged damage after at least 10 attacks, or the highest magic damage after at least five offensive damage spell casts.
- Spends the most mana after at least five spell casts.
- Has the lowest or highest melee hit rate, or the lowest or highest ranged hit rate, with at least 10 attacks per compared mercenary. A low hit-rate event additionally requires the subject to miss at least half of their attacks. Highest and lowest melee hit-rate events have separate narratives.
- Has the highest melee or ranged critical rate after at least 10 successful hits per compared mercenary.
- Escapes or is freed from being swallowed once.
- Survives while taking cumulative damage greater than normal maximum HP without being downed. Temporary HP may absorb this additional damage; temporary healing or shields are not separate events.
- Survives after receiving more opportunity-attack attempts than ordinary incoming attacks. Count attack attempts whether they hit or miss. This remains separate from the cumulative-damage event.

An enemy being digested once is not an event because it is expected in ordinary battles. “Most successful attacks” is already represented by hit-rate candidates, so do not add a duplicate count event. A generic “received the most temporary healing” or “benefited from shield” event is also excluded; those effects can contribute to the exceptional-damage survival event. Reaching 0 HP and surviving remains distinct from surviving damage exceeding maximum HP.

Boss contracts use an explicit boss-bearing force and keep the entire enemy power within the fixed rating budget. The boss-vore event qualifies only when an eligible mercenary completes digestion of the marked enemy; leader digestion of the boss alone causes no post-battle reaction. The contract itself completes as soon as the boss is downed, digested or flees; that objective does not force the remaining enemies to retreat. After the result and any post-battle event are recorded, the paid contract's generated enemy records are removed.

Implementation uses provisional starting values: a six-enemy minimum for majority events, at least 10 attacks for melee/ranged performance comparisons, at least five offensive damage spell casts for magic-damage comparisons, at least five total spell casts for mana comparisons, at least 10 successful hits for critical-rate comparisons, and four deployed company fighters for the last-active event. A low hit-rate event also requires at least half of the subject's attempts to miss. Ties are eligible for random selection. Significant losses mean strictly more than 50% of deployed company combatants; more incoming opportunity-attack attempts than ordinary attacks is the separate survival threshold. Boss contracts are implemented in the 2026-11-09 campaign-mission expansion.

## Event eligibility and examination order

The curator specifies this order:

1. No ordinary event if only the leader remains as survivor. The separate Grudge notice still requires at least one surviving eligible mercenary.
2. No event unless at least three units were deployed. The regular minimum shape is leader plus two mercenaries. Reserves do not count. Require at least one surviving hired active mercenary.
3. Examine exceptional occurrences first. If several happened, choose among them randomly.
4. For each ordinary candidate, determine its trait outcome before treating the event as eligible to fire. If the outcome conflicts with the subject's existing configuration, cannot be granted, or violates the required polarity/parity rule, discard that candidate and try another candidate from the same tier. The separate Grudge notice is not subject to the ordinary polarity roll.
5. If no exceptional candidate has a workable outcome, use the same process for mid-tier, then mundane occurrences.
6. Use a bounded attempt list so a candidate is not retried indefinitely. If every candidate is exhausted or unworkable, show no event, as the user directed on 2026-11-07.

For a personal accomplishment, its performer is the subject. Company-level occurrences have a source and a separate eligible mercenary recipient. Prefer recipients with fewer weighted traits when an occurrence allows multiple recipients, while the exceptional occurrence itself remains the first selection priority. Within a tier, selection is random as specified; do not let the trait-count preference silently replace random choice of which occurrence happened. The leader-glory Grudge notice is independent: with at least six enemy combatants, it resolves when the leader digests more than half of them and an eligible mercenary who is below Grudge III remains. Grudge III mercenaries cannot receive another Grudge event, but a lower Grudge tier can be upgraded. The notice is outside ordinary Good/Bad parity; Grudge does not count as Good or Bad. If every eligible mercenary already has Grudge III, skip the Grudge event.

The independent leader-glory event grants or upgrades tiered **Grudge** for an eligible mercenary. Grudge is a wage-penalty trait but does not count as Bad for the ordinary Good/Bad balance. It does not lower combat performance. While the mercenary has Grudge, a later level-up increases their wage beyond the normal level-up raise. Gaining Grudge does not immediately change their current contracted wage. The extra raise becomes permanent and remains in their wage if Grudge is later removed. The accumulated increase is persisted separately from the live trait and included in the level-up preview; it takes effect at the next regular payday. The current wage curve is `BaseWage + 3 * levelSteps + 2 * levelSteps^2 + 2 * veteranSteps^3`. Grudge I/II/III use the provisional +25%/+50%/+100% of each future level-up's normal wage increase, rounded up to whole gold with a minimum of 1 when that raise is positive. Tactical disobedience was considered and rejected in favor of the wage-growth effect; taking tactical control away could cause involuntary vore losses.

## Event and revival sequence

Settle tactical results, contracts and rewards first. If the leader was digested, show a brief revival interlude next with one **OK** button and no additional narration. Its message is: “[Leader name] was digested during the last battle. The surviving company members travel swiftly to [town name] to pay [amount] gold for revival services.” On acknowledgement, show the one selected ordinary post-battle event, then the independent Grudge event if it qualified, then return to the normal campaign flow. If no ordinary event was selected, show Grudge directly. This immediate sequence keeps the notices within the player's sense of the same last battle. The interlude does not advance campaign time, and existing revival payment rules remain in effect.

## Positive and negative balance

The user specifies the following polarity rule before an eligible acquired outcome is granted.

| Current count difference | Outcome polarity |
| --- | --- |
| Equal | 50% positive, 50% negative |
| One | 75% toward the minority side, 25% toward the majority |
| Two | 100% toward the minority side |
| Two or more | 100% toward greater parity |

Do not substitute uniform random quirks or continue adding to a majority at a difference of two. The desired polarity and the significant occurrence must both have a valid outcome. If a candidate cannot produce a legal outcome, discard it and try the remaining candidates; if none work, show no event. Do not silently reverse the desired polarity when a family is capped or an outcome conflicts.

The user requires internal Good, Bad and Neutral classifications for all traits. Both innate species traits and acquired personal traits count toward the Good/Bad ratio. Neutral traits have mixed benefits and drawbacks and contribute to neither side. Featherweight is explicitly Neutral because it grants evasion while increasing damage received. Traits designated inborn can only be assigned when a character is generated and cannot be added, removed, or changed later; they still count toward polarity. This includes size traits. Post-battle outcomes, legacy trait-transfer/extraction paths, and temporary/shared/conditional trait paths preserve inborn traits; ordinary trait removal refuses to change them, and the legacy size-shrink helper is inert. Developer-only editing remains outside campaign gameplay. The implementation applies the reviewed classification inventory and event-to-trait outcome catalog in the linked record. Regeneration and ManaDynamo keep their beneficial effects without granting extra healing or mana to a predator when absorbed.

The user confirms tier-weighted polarity counts. A Good tier I/II/III contributes 1/2/3 Good points respectively, and a Bad tier I/II/III contributes 1/2/3 Bad points. Thus Precise II contributes two Good points and Precise III contributes three. Upgrading replaces the previous tier, so Precise I becoming Precise II changes its contribution from one to two. Neutral traits contribute zero to both polarity totals. When the weighted difference is two or greater, the applied trait change must strictly reduce the absolute difference. That can mean gaining toward the minority or reducing/removing a tiered trait from the majority. At a difference of one, preserve the confirmed 75% minority-side / 25% majority-side chances; at equality, use 50% each.

Old injuries are separate from traits and from the Good/Bad ratio. Their burden must be visible in outcome review and simulations. Do not secretly change the specified polarity probabilities to compensate. Whether injuries affect candidate eligibility or severity requires a separate approved rule.

## Event-subject preference and growth

The user confirms there is no hard trait-count limit. Prefer eligible characters with fewer traits when an occurrence permits a choice among recipients or tied performers. Do not let that preference override the random draw among qualifying occurrences in the highest tier. Among recipient candidates, the implementation weights the choice toward those with fewer weighted traits; equal weights remain equally likely.

The user confirms tier weighting in all trait-count calculations. Sum currently held tier I/II/III traits as 1/2/3 for event-subject preference and removal eligibility, including Good, Bad and Neutral traits from species and personal sources. Untiered traits contribute one. Neutral traits contribute to this total while contributing zero to either polarity total. Old injuries remain separate. Count only the held tier in an exclusive family. For example, two tier III traits plus one tier II trait meet the provisional eight-point removal threshold.

Existing mutually exclusive tier upgrades replace the previous tier within their family. Trait weight is based on the currently held tier, including neutral and inborn traits; old injuries remain outside the trait counts.

## Removing or downgrading traits

The user permits event outcomes that remove or downgrade existing traits, especially when an opposed trait expresses the new experience. Introduce that possibility only after the character has a sizable trait collection. The user's provisional minimum is eight tier-weighted trait points. At or above that threshold, removal/downgrade becomes eligible and should become more likely as the weighted collection grows. Exact threshold and probability tuning remain for review. Avoid immediately undoing a newly acquired quirk in the next battle.

The user further confirms that a tier III trait selected for removal is downgraded. Apply a single-step reduction, III to II or II to I, with tier I or an untiered trait removed outright. Replacing the held tier changes its weighted contribution by one. Repeated removal outcomes must earn each successive reduction through separate eligible events. Opposed-trait handling must respect this single-step reduction and the existing family exclusivity rules. Do not erase an entire tier III family in one removal outcome. Recompute weighted totals after the committed change.

Keep the event's mechanical direction consistent with polarity balancing. Removing or reducing a Bad trait is a positive outcome, while removing or reducing a Good trait is a negative outcome. Review this interpretation with the outcome catalog, including opposed families and innate species-trait protection. Every applied change must leave the unit in a valid trait configuration.

Tier-weighted counts and removal-as-downgrade are implemented. The eight-point threshold and gradual reduction chance are provisional tuning. A grouped play review should examine the event thresholds, candidate balance, save/reload sequencing, and long-term trait accumulation before 40D is closed.

## Evidence foundation

`Assets/Scripts/Modes/Tactical/TacticalStats.cs` mainly records side totals. Trait events require a per-participant ledger linked to stable company character and battle IDs. Extend battle snapshots and settlement alongside `CompanyBattle`, `CompanyBattleSnapshot` and company saves.

Record actual resolved outcomes, including relevant attack counts, hits, grazes, criticals, meaningful damage, swallowing, escapes, digestion, downing and survival. Previews, AI scoring and cancelled actions must never count as accomplishments. A graze remains distinct from a hit for hit-rate and critical-rate comparisons, though its actual damage contributes to damage totals. Distinguish ordinary attacks, opportunity attacks and summons where attribution matters.

## Selection and reward transaction

1. Snapshot deployed participants and their identities at battle start. The leader, reserves and temporary summons are ineligible for lasting grants.
2. Settle the battle's actual fates and identify eligible surviving participants. Do not grant traits to digested characters or create rewards for a company that has ended.
3. Build ordinary candidates from approved occurrence rules, ordered by exceptional, mid-tier and mundane. Randomly choose among qualifying occurrences in the highest tier. Apply the fewer-traits preference only when choosing among multiple recipients/tied performers, not in place of the event draw. Determine outcome polarity from all held Good/Bad traits, then select an appropriate related outcome. Independently resolve and queue the leader-glory Grudge event when its trigger qualifies and a surviving eligible mercenary below Grudge III can receive it.
4. Persist the chosen ordinary event and independent Grudge event before presenting them. Persist each grant and its applied state so reload cannot reroll or duplicate a reward.
5. Apply ordinary outcomes through the exclusive I–III trait-family rules. Capped-tier or conflicting outcomes are rejected and the candidate search continues. Eligible removal/downgrade outcomes pass the weighted-count rule and never change inborn traits. Grudge bypasses ordinary polarity and grants/upgrades independently. Mercenaries at Grudge III are not recipients; if no eligible mercenary can receive or upgrade Grudge, skip that event.
6. Present short, natural aftermath prose with positive or negative continuations, dynamic names/pronouns, and a clearly separated mechanical notice of the actual trait change. Preserve the curator's five authored passages verbatim.

Manual combat, AI Finish Turn and Auto-resolve must use the same evidence and settlement logic. Single Battle may produce diagnostic evidence but cannot progress a campaign or permanent character history.

## Implementation limits and remaining tuning

- The classification inventory and event-to-trait catalog are recorded in the linked curator decision document. Numeric thresholds, reward frequencies, and long-term trait accumulation remain provisional for play review.
- Existing mid-battle saves without the complete evidence version do not fabricate past accomplishments and cannot produce a post-battle event for that battle.
- Candidate search is bounded. If no occurrence has a polarity-valid, non-conflicting outcome, no event appears.
- Grudge wage percentages and the trait-reduction probability are provisional values and can be tuned after observed campaigns.
- Boss digestion uses the marked boss identity saved on its contract; boss flight/downing alone completes the contract but does not count as digestion evidence.

## Planned grouped acceptance

Grouped player review should cover exceptional-to-mundane selection, no-event cases, reserves, summons, leader-only and ended campaigns, relevant fates, tier caps and opposed traits. Verify the leader cannot receive an event. Check weighted equal/one/two boundaries, the 50%/75% direction, minority correction and outcome exhaustion. Verify upgrades replace their prior contribution; species traits count; Neutral traits contribute only to total weight; inborn traits remain unchanged; and old injuries remain separate. Check tied-recipient preference, reduction threshold and single-tier downgrades, plus saved battle/revival/event progression without rerolls or duplicate grants. Compare manual, AI-finished and auto-resolved battles. Existing injury probabilities and accepted combat rules remain independent. No build or gameplay tests have been run for this implementation.

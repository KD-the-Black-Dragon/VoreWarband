# Phase 40D implementation handoff

Date: 2026-11-07  
Status: Implemented; provisional gameplay values await grouped player review. No Unity build or gameplay test was run during this handoff.

## Delivered

- Battle participants record relevant attack outcomes, weapon and magic damage, injuries, mana use, incoming ordinary and opportunity attacks, escapes/freed-from-vore events, completed digestions witnessed nearby, downing, and last-active survival. Evidence belongs to the battle actors and is serialized with battle saves.
- Battle start stores the evidence version and initial enemy-force size. Older in-progress saves without the new ledger do not invent past evidence and do not receive a post-battle event for that battle.
- After fates, contract/road-event rewards, and campaign results settle, the system resolves at most one event. It prioritizes exceptional, then mid-tier, then mundane occurrences. It has no generic fallback and exits with no event if every eligible occurrence has an unusable outcome.
- The automatic outcome uses the settled campaign random state and is persisted before display. Save/reload keeps the chosen subject and trait change, preventing a reroll or duplicate grant.
- Trait polarity includes species and personal traits; tier weight is I/II/III = 1/2/3; Neutral contributes to total weight only. The confirmed equal/one/two-point balance probabilities are used. Inborn size traits and Featherweight count normally but cannot be changed by this event system.
- Post-battle rewards use only curated Good/Bad traits. Soft-retired names are neutral and excluded from this reward pool; this phase leaves other legacy assignment/effect paths unchanged. Duelist and Fervor remain player-side only; Juggernaut and MutualBiology are not rewards.
- Trait reduction can begin at eight weighted trait points, at a provisional 5% chance per point above seven and a 30% cap. Tier III and II reduce one tier at a time. Old injuries remain outside trait counts.
- The leader can be the source of an envy event but never the recipient. Grudge I/II/III provisionally adds 25%/50%/100% of the normal wage increase on future level-ups, rounded up. Earned wage surcharge persists after removing Grudge and appears in the next-level wage preview.
- The results screen proceeds to the concise leader-revival interlude if needed, then to the post-battle event. Neither notice advances campaign time.
- Regeneration retains its +2 temporary HP per combat turn and ManaDynamo retains +3 mana per turn; neither gives its predator an absorption bonus.

## Provisional event rules

- Major digestion event: enemy force has at least six members; the subject digests more than half and leads the deployed company's digestion count. A leader who digests more than half can instead cause a Grudge event for an eligible mercenary.
- Repeated escapes/freed events: at least three and highest count. Three new injuries qualifies exceptionally.
- Significant losses: strictly more than half of deployed company fighters are digested. Reserves are excluded.
- Adjacent enemy digestions: two is exceptional; one is mid-tier.
- Last active survivor: starting force has at least four company fighters and a surviving mercenary was the only active company member at some point.
- Damage over normal maximum HP without downing, and taking more incoming opportunity attacks than ordinary attacks, are separate exceptional events.
- Majority-downing requires at least six enemies. Rate comparisons require at least five attempts per compared mercenary; current event list compares melee/ranged hit rates and melee/ranged critical rates. Grazes count as neither hits nor criticals but their damage still counts in damage totals. Highest damage and mana-use comparisons require at least two eligible subjects and a nonzero result.
- Boss digestion is intentionally stubbed out until boss-bearing contracts exist.

## Grouped player review

1. Confirm event priority, thresholds, no-event fallback, and the factual placeholder copy.
2. Confirm the revival-then-event sequence, including saved/reloaded pending notices.
3. Confirm the awarded or downgraded trait, the polarity balance, inborn protection, and one-step tier changes.
4. Confirm Grudge levels, future wage preview, permanent surcharge after trait removal, and next-payday timing.
5. Compare manual, AI-finished, and auto-resolved battles, and save/reload before and between settlement notices.

This implementation adds no narrative event text. Existing injury probabilities and accepted battle rules are unchanged. Mid-campaign trait-transfer, temporary, shared and conditional paths preserve inborn size and Featherweight traits; ordinary trait removal refuses to change them, and the unused legacy size-shrink method is inert. Developer-only editing remains available outside campaign gameplay. No third-party code, artwork, or dependencies were introduced for 40D.

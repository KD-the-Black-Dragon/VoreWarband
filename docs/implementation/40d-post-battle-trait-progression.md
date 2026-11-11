# Phase 40D implementation handoff

Date: 2026-11-07  
Status: Implemented; provisional gameplay values await grouped player review. Curator-authored event prose, independent Grudge sequencing and minimum activity requirements were added 2026-11-10. No Unity build or gameplay test was run during this handoff.

## Delivered

- Battle participants record relevant attack outcomes, weapon and magic damage, injuries, mana use, incoming ordinary and opportunity attacks, escapes/freed-from-vore events, completed digestions witnessed nearby, downing, and last-active survival. Evidence belongs to the battle actors and is serialized with battle saves.
- Battle start stores the evidence version and initial enemy-force size. Older in-progress saves without the new ledger do not invent past evidence and do not receive a post-battle event for that battle.
- After fates, contract/road-event rewards, and campaign results settle, the system resolves at most one ordinary event. It prioritizes exceptional, then mid-tier, then mundane occurrences. It has no generic fallback and exits with no ordinary event if every eligible occurrence has an unusable outcome.
- A leader who digests more than half of an enemy force of at least six triggers a separate Grudge notice for a surviving eligible mercenary below Grudge III, in addition to any ordinary event. It runs after the ordinary event, including after leader revival, and is saved with the battle. Grudge I–III is excluded from Good/Bad parity. Grudge III mercenaries cannot receive it; if none can receive or upgrade Grudge, skip the event. Leader digestion of a marked boss alone causes no reaction.
- The curator supplied the remaining event narratives, replacing the assistant drafts; their wording is in `CompanyPostBattleNarratives.cs`. The earlier user-written passages remain unchanged, and the single-escape text is a milder adaptation the curator approved. Pronoun placeholders use the unit's existing pronoun list. Trait-change notices remain separate from the narrative.
- The automatic outcome uses the settled campaign random state and is persisted before display. Save/reload keeps the chosen subject and trait change, preventing a reroll or duplicate grant.
- Trait polarity includes species and personal traits; tier weight is I/II/III = 1/2/3; Neutral contributes to total weight only. The confirmed equal/one/two-point balance probabilities are used. Inborn size traits and Featherweight count normally but cannot be changed by this event system.
- Post-battle rewards use only curated Good/Bad traits. Soft-retired names are neutral and excluded from this reward pool; this phase leaves other legacy assignment/effect paths unchanged. Duelist and Fervor remain player-side only; Juggernaut and MutualBiology are not rewards.
- Trait reduction can begin at eight weighted trait points, at a provisional 5% chance per point above seven and a 30% cap. Tier III and II reduce one tier at a time. Old injuries remain outside trait counts.
- The leader can be the source of an independent envy event but never the recipient. Grudge I/II/III provisionally adds 25%/50%/100% of the normal wage increase on future level-ups, rounded up. Earned wage surcharge persists after removing Grudge and appears in the next-level wage preview.
- The results screen proceeds to the concise leader-revival interlude if needed, then to the ordinary event and independent Grudge notice when qualified. Neither notice advances campaign time.
- Regeneration retains its +2 temporary HP per combat turn and ManaDynamo retains +3 mana per turn; neither gives its predator an absorption bonus.

## Provisional event rules

- Major digestion event: enemy force has at least six members; the subject digests more than half and leads the deployed company's digestion count. Independently, a leader who digests more than half triggers a Grudge event for an eligible mercenary.
- Repeated escapes/freed events: at least three and highest count. Three new injuries qualifies exceptionally.
- Significant losses: strictly more than half of deployed company fighters are digested. Reserves are excluded.
- Adjacent enemy digestions: two is exceptional; one is mid-tier.
- Last active survivor: starting force has at least four company fighters and a surviving mercenary was the only active company member at some point.
- Damage over normal maximum HP without downing, and taking more incoming opportunity attacks than ordinary attacks, are separate exceptional events.
- Majority-downing requires at least six enemies. Melee/ranged damage and hit-rate comparisons require at least 10 attempts per compared mercenary; magic-damage comparisons require at least five offensive damage spell casts, while mana comparisons require at least five total spell casts. Cast counts are recorded once per cast, not once per target, so area spells cannot inflate the sample. Low hit-rate events additionally require at least half the subject's attempts to miss. Critical-rate comparisons require at least 10 successful hits per compared mercenary. Highest/lowest melee hit-rate events use separate narratives. Grazes count as neither hits nor criticals but their damage still counts in damage totals. Comparisons require at least two eligible subjects and a nonzero result.
- A mercenary's digestion of the marked boss can qualify for the boss-digestion occurrence. Leader digestion of the boss alone is not a post-battle event; the independent leader large-digestion condition remains governed by its own thresholds.

## Grouped player review

1. Review the aftermath prose and confirm it does not expose hidden event calculations; verify the trait-change notice is clear.
2. Confirm the revival → ordinary event → independent Grudge sequence, including saved/reloaded pending notices.
3. Confirm the awarded or downgraded trait, the polarity balance, inborn protection, and one-step tier changes.
4. Confirm Grudge levels, future wage preview, permanent surcharge after trait removal, and next-payday timing.
5. Compare manual, AI-finished, and auto-resolved battles, and save/reload before and between settlement notices.

Existing injury probabilities and accepted battle rules are unchanged. Mid-campaign trait-transfer, temporary, shared and conditional paths preserve inborn size and Featherweight traits; ordinary trait removal refuses to change them, and the unused legacy size-shrink method is inert. Developer-only editing remains available outside campaign gameplay. No third-party code, artwork, or dependencies were introduced for 40D.

# In-Battle Vore Narrative Token Reference

This page documents the exact template tokens accepted by `InBattleVoreNarratives.json`. It describes the current implementation, including its limits. Use the brace syntax shown here in narrative strings.

## How the event roles are assigned

The context (`companyPredator`, `enemySapientPredator`, or `enemyAnimalPredator`) selects which narrative set is read. A role token then reads a unit from the recorded battle-log event. Context and role are separate concepts.

| Role | Exact source and meaning |
| --- | --- |
| `actor` | The event's `Unit` field. For an ordinary vore action, this is the predator performing the action. |
| `predator` | Alias for `actor`; it also reads the event's `Unit` field. |
| `target` | The event's `Target` field. For an ordinary vore action, this is the prey being targeted. |
| `prey` | The event's separately recorded `Prey` field, if present; otherwise it falls back to `target`. |
| `contained` | The event's separately recorded `Prey` field only. If no separate prey is recorded, this role has no unit. |
| `donor` | For ordinary events, the event's `Unit` field. For `VoreStealSuccess` and `VoreStealFail` events, the event's `Target` field. |
| `recipient` | For ordinary events, the event's `Target` field. For `VoreStealSuccess` and `VoreStealFail` events, the event's `Unit` field. |

For standard vore-action lines, use `actor` for the predator and `target` for the prey. Transfer events reverse the donor/recipient fields for vore-steal events; use `{donor...}` and `{recipient...}` when those roles matter. In transfer events, `actor` and `predator` still mean the event's `Unit` field, so they may not mean the original predator in the ordinary-language sense.

## Name and display tokens

The following forms are available for every role in the table above:

| Token form | Definition |
| --- | --- |
| `{role}` | The unit's display name. The game adds `the ` when it recognizes the name as a generic species or generated archetype; a recognized proper name is unchanged. |
| `{role.display}` | Same as `{role}`. |
| `{role.articlename}` | Same as `{role}` and `{role.display}`. |
| `{role.name}` | The unit's exact stored name, without an automatically added article. |

Replace `role` with `actor`, `predator`, `target`, `prey`, `contained`, `donor`, or `recipient`. Role names and suffixes are matched without regard to case.

Automatic article detection is limited to these cases:

1. The stored name exactly matches that unit's species display name.
2. The stored name starts with the species display name followed by one of these generated labels: `Warrior`, `Archer`, `Mage`, `Warrior (Boss)`, `Archer (Boss)`, or `Mage (Boss)`.

If the stored name already starts with `the `, the game does not add another article. Other names are left as written. The automatic article is always `the`; the system does not choose `a` or `an`.

Examples:

- `{actor.name}` might produce `Alice`.
- `{actor.display}` might produce `Alice` or `the Avian Archer`, depending on the stored name.
- `{target.name}` never adds an article. Use `{target.display}` when the target may be a generic species or generated archetype.

If a role has no unit, a bare role such as `{target}` becomes `Unknown unit`. A role property such as `{target.name}` or `{target.he}` becomes an empty string.

## Pronoun tokens

Pronouns come from the unit's existing pronoun list. Tokens do not infer pronouns from species, gender labels, or whether a unit is an animal. The same token may therefore produce `he`, `she`, or `they`, depending on the unit's saved pronouns.

| Role suffix | Alias suffix | Pronoun slot | Definition |
| --- | --- | --- | --- |
| `.he` | `.subject` | Slot 0 | Subject pronoun, such as `he`, `she`, or `they`. |
| `.him` | `.object` | Slot 1 | Object pronoun, such as `him`, `her`, or `them`. |
| `.his` | `.possessive` | Slot 2 | Possessive determiner, such as `his`, `her`, or `their`. |
| `.himself` | `.reflexive` | Slot 4 | Reflexive pronoun, such as `himself`, `herself`, or `themself`. |

Examples:

- `{actor.he}` — subject pronoun for the actor.
- `{target.him}` — object pronoun for the target. It can resolve to `him`, `her`, or `them` according to the target's pronouns.
- `{prey.his}` — possessive determiner for the prey, such as `his`, `her`, or `their`.
- `{recipient.himself}` — reflexive pronoun for the recipient.

The current token set does **not** expose pronoun slot 3, the standalone possessive pronoun (`his`, `hers`, or `theirs`). The `.his` token uses slot 2, the possessive determiner (`his`, `her`, or `their`). There is no built-in `it` pronoun selection by species; a unit will use whatever pronouns its existing pronoun list contains. If the pronoun list cannot be read, the fallback forms are `they`, `them`, `their`, and `themself`.

The template system does not change verb agreement. Write the complete sentence around the resolved pronoun; for example, write `they are` or `she is` explicitly in the relevant narrative variant.

## Short actor aliases

These tokens always refer to the event's `Unit` field, which is normally the predator for an ordinary vore action:

| Token | Definition |
| --- | --- |
| `{name}` | Actor's exact stored name; no article is added. |
| `{he}` | Actor's subject pronoun. |
| `{him}` | Actor's object pronoun. |
| `{his}` | Actor's possessive determiner. |
| `{himself}` | Actor's reflexive pronoun. |

Capitalized brace forms such as `{Name}`, `{He}`, and `{His}` are also accepted. Role-qualified forms are preferable in lines that mention both participants because the short aliases always refer to the actor.

## Capitalization

Token matching is case-insensitive. For output capitalization, the game checks only the first character inside the braces:

- `{Actor.he}` resolves the actor's subject pronoun and capitalizes its first letter.
- `{actor.he}` resolves it without changing its first letter.
- `{actor.He}` is accepted as the actor's subject pronoun, but does **not** capitalize the result because the token begins with lowercase `a`.
- `{Stomach}` selects from the lowercase `stomach` synonym group and capitalizes the selected word's first letter.

Capitalization changes only the first character of the resolved value. It does not uppercase the entire value. For a role-qualified token at the beginning of a sentence, capitalize the first role character, as in `{Target.he}`.

## Synonym tokens

A token matching a key in `NarrativeSynonyms.json` selects one non-empty word from that key's list. For example, `{stomach}` selects one word from the lowercase `stomach` group. Each occurrence selects independently; two `{stomach}` tokens in one line may produce different words. Synonym keys should be written in lowercase because the lookup uses a lowercased token against the JSON key.

Synonym values are inserted as literal text. They are not scanned again for other tokens. Do not put `{actor}` or another template token inside a synonym value.

Avoid naming synonym groups after reserved role names, short actor aliases, or role suffixes (`actor`, `target`, `prey`, `contained`, `donor`, `recipient`, `predator`, `name`, `he`, `him`, `his`, `himself`, `display`, `articlename`, `subject`, `object`, `possessive`, `reflexive`). Those names are interpreted as roles or pronouns before they can be treated as synonym groups.

## Legacy square-bracket aliases

These exact legacy spellings are converted to brace tokens before expansion:

| Legacy spelling | Equivalent token |
| --- | --- |
| `[Name]` | `{Name}` |
| `[name]` | `{name}` |
| `[He]` | `{He}` |
| `[he]` | `{he}` |
| `[him]` | `{him}` |
| `[His]` | `{His}` |
| `[his]` | `{his}` |
| `[himself]` | `{himself}` |

These aliases refer only to the actor. There are no square-bracket aliases for `target`, `prey`, `contained`, `donor`, `recipient`, or `predator`. Use brace syntax for new text.

## Validation, random selection, and fallback

- The outer JSON chooses a context, method, and event stage before any template tokens are expanded.
- If an event has multiple non-empty narrative strings, one is chosen uniformly using a narrative-only random source. It does not advance combat randomness.
- A synonym token independently chooses one non-empty synonym each time it appears.
- The rendered sentence is cached on the tactical log event so refreshing or reloading that log does not choose a different variant.
- An unknown brace token in a validated required entry produces a startup warning and marks that whole entry invalid; if its event occurs, the line renders as `[Missing vore narrative: context/method/stage]`.
- If an unknown brace token reaches expansion without having been rejected by validation, that token renders as `[Missing narrative token: token]`.
- A missing context/method/stage entry, an empty list, or a list with no non-empty strings produces a startup validation warning and renders as `[Missing vore narrative: context/method/stage]` when that event occurs.

## Complete token inventory

This compact inventory lists every brace-token family currently recognized by the parser:

- For each `role` in `{actor, predator, target, prey, contained, donor, recipient}`:
  - `{role}`
  - `{role.name}`
  - `{role.display}`
  - `{role.articlename}`
  - `{role.he}` and `{role.subject}`
  - `{role.him}` and `{role.object}`
  - `{role.his}` and `{role.possessive}`
  - `{role.himself}` and `{role.reflexive}`
- Actor-only short aliases: `{name}`, `{he}`, `{him}`, `{his}`, `{himself}`.
- Any lowercase key configured in `NarrativeSynonyms.json`, such as `{stomach}`.

The parser also accepts case variants of these brace tokens; only the first character of the complete token controls first-letter capitalization as described above.

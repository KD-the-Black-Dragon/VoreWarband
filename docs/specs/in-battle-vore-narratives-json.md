# Editable in-battle vore narratives

The game reads these files from `Assets/StreamingAssets/Narrative/` when it starts:

- `InBattleVoreNarratives.json` contains the narrative variants.
- `NarrativeSynonyms.json` contains word lists referenced by `{groupName}` tokens. Each occurrence selects a fresh random word; this uses a narrative-only random source and does not change combat rolls.

The narrative file is organized as **context → method → event stage → list of variants**. The three contexts implement the approved split:

- `companyPredator`: a company member or ally is the actor.
- `enemySapientPredator`: an enemy sapient is the actor.
- `enemyAnimalPredator`: an enemy animal is the actor.

In Single Battle, context is selected from the actor's species type regardless of which side the player controls. In campaign battles, company characters use the company context; other actors are classified by species. Curated species use their approved species role; other species use the existing Feral trait as the animal/sapient fallback.

Method keys are `oral`, `womb`, `cock`, `anal`. The JSON includes each currently supported context/method/stage combination. Empty arrays are deliberately unfilled authoring slots; they are not usable narrative content.

Event stages include `action`, `nearDigestion`, `digestionProgress`, `digestionComplete`, `absorptionComplete`, `escape`, `freed`, `regurgitated`, `partialEscape`, `greatEscapeKeep`, `greatEscapeFlee`, `manualRegurgulation`, `curseExpires`, `diminishmentExpires`, and method-specific support/transfer stages. The entry names in the JSON are the authoritative list.

Each array can contain one or more complete lines. Variants are selected with a narrative-only random source. The rendered line is cached in the battle log so refreshes and save/load do not reroll it.

## Template tokens

For the complete parser-verified token inventory, role mappings, pronoun slots, article behavior, capitalization rules, aliases, and fallback behavior, see the [In-Battle Vore Narrative Token Reference](in-battle-vore-narrative-token-reference.md).

- `{actor}` or `{actor.display}`: actor's displayed label with an automatic “the” for generated species/archetype labels; proper names remain unprefixed.
- `{actor.name}`: exact unit name without an article.
- `{actor.he}`, `{actor.him}`, `{actor.his}`, `{actor.himself}`: actor's current unit pronouns. Also available for `target`, `prey`, `contained`, `donor`, `recipient`, and `predator` where that role exists. For example, `{prey.his}`.
- `{target}` / `{target.display}`: exact target label. `{prey}` refers to a separately logged contained unit when present, or the target for an ordinary vore action. `{contained}` always refers only to the separately logged contained unit, if present.
- `{name}`, `{Name}`, `{he}`, `{He}`, `{him}`, `{his}`, `{His}`, and `{himself}` are short actor aliases. The older square-bracket spellings such as `[Name]` and `[he]` are also accepted.
- `{stomach}` selects from the `stomach` group in `NarrativeSynonyms.json`. Other synonym groups can be added the same way.

At startup, validation reports every missing context/method/stage entry and every unknown template token as one warning. Missing entries display a keyed technical fallback (for example, `[Missing vore narrative: enemyAnimalPredator/oral/digestionComplete]`), which signals that the authoring file needs attention. No generic vore prose is supplied as a substitute.

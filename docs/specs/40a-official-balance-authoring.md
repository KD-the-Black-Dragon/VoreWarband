# 40A — Reliable developer settings and official balance authoring

Planning draft, 2026-11-06. Foundation of [module 40](40-authoring-and-campaign-progression.md). The user confirms keeping Race Editor with export/import, and preserving existing characters when new species defaults ship.

## Implementation handoff — 2026-11-06

The user authorized implementation. [The implementation record](../implementation/40a-official-balance-authoring.md) documents the draft/promotion workflow, per-character snapshots, legacy-data limits, 72 focused assertions, 963 grouped checks and successful private Windows build. Curator review remains pending. The planning requirements below remain the design reference.

## Current wiring

- `State.LoadRaceData`, `ChangeRaceSlotUsed`, `LoadEditedRaces` and `SaveEditedRaces` in `Assets/Scripts/Core/State.cs` select and serialize local `EditedRaces*.dat` files. Windows storage currently uses relative `UserData` paths. These edits do not update project defaults for the next build.
- `Assets/Scripts/Subwindows/RaceEditorPanel.cs` saves through that path. Its General Options, Trait Options and Unit Tags Options remain developer tools behind existing access control.
- `Assets/Scripts/Utility/Stored/RaceSettings.cs` stores partial overrides and falls back to `Assets/Scripts/Races/RaceParameters.cs`. Sanitization assumes metadata exists for several legacy fields. Source review identifies possible failure points, but does not establish the cause of the observed startup warning.
- `RaceStats` in `RaceParameters.cs` retains eight storage fields and a four-stat conversion marker. The official authoring schema must expose STR, AGI, INT and VOR once and avoid repeated legacy consolidation.
- Unit Tag definitions are separate local JSON loaded by `Assets/Scripts/Utility/ExternalTraitHandler.cs`. A race's tag IDs alone cannot reproduce those definitions. Tags can also reference associated traits and other modifier metadata.
- Some unit creation paths use `RaceParameters` directly. `WarbandStats.BaseAP` also supplies species AP outside RaceSettings. An export of the current override dictionary alone cannot provide a complete authoritative balance profile.

## A1 — Reliable loading and explicit developer drafts

1. Trace startup order and reproduce the warning using synthetic settings. Distinguish missing files, malformed files, unsupported versions, missing metadata and retired references. Identify the actual failing path before changing behavior.
2. Validate a complete candidate in isolation before installing it into live state. On failure, retain the original draft and report the actionable cause. Do not silently overwrite a draft with empty defaults or partially apply valid rows.
3. Establish deterministic loading precedence. Ordinary play uses the packaged official profile and player preferences. A developer session may explicitly select a draft overlay and shows its identity. Existing local race files cannot silently override release balance.
4. Design a deliberate legacy-draft import with a preview of ignored or invalid fields. Preserve originals. Missing Unit Tag definitions and retired dependencies must be visible to the curator.
5. Confirm that preview generation, cancelled edits and Battle Lab isolation do not mutate shared official defaults. Define whether draft changes affect already-created test units or require regenerating them.

## A2 — Proposed authoring and promotion workflow

The editing surface is the existing password-protected Race Editor, as confirmed by the user.

1. Open an editable draft based on a named official revision.
2. Edit species stat ranges, supported numerical properties, trait assignments and Unit Tag data. Retain all normal Race Editor tabs and existing wording. Keep legacy-only controls developer-facing and clearly identify fields with no conversion effect.
3. Test the explicit draft in the Battle Lab. Test output records the draft revision/hash and seed.
4. Export a versioned, readable balance package from the developer interface. A standalone build can author a draft even though it cannot alter a previously built executable's packaged defaults.
5. In Unity, import the package, inspect a concise before/after review and validate the complete result. Promotion requires an explicit action and refuses stale-base conflicts or unresolved references.
6. Promote approved data into the project and include it automatically in subsequent builds. A clean installation must reproduce it without any curator UserData or PlayerPrefs.

Proposed storage is one canonical, versioned JSON profile loaded as a Unity TextAsset from a dedicated project Resources folder. The exact asset path is selected during implementation. Existing Newtonsoft JSON support can provide interchange without a new dependency. Drafts and review exports are separate working files. Avoid a second editable C# copy of promoted values.

The profile carries schema version, revision, modification date, parent revision and stable species/trait/spell/tag identifiers. Author four stat ranges, relevant species balance properties, trait lists and supported tag definitions explicitly. Resolve dependent metadata as a validated set. Include an effective-value review so an untouched fallback cannot accidentally change during import.

Reserve a compatible extension for the Good/Bad/Neutral trait classifications and family/tier identifiers required by 40D. Its classification inventory remains a later phase. Profile validation must preserve the user's distinction between trait polarity, tier weight and old injuries.

Trait assignment and existing declarative tag modifiers are data-authorable. Genuinely new trait behavior still requires code. Inventory the numeric parameters currently embedded in retained trait implementations and identify which are useful to expose through supported fields. Do not promise that a trait's arbitrary C# logic becomes editable through its display name.

## Integration and compatibility

- Route relevant generation, AP, trait, spell, power-scoring and preview consumers through one effective balance source. Keep existing compatibility APIs where necessary.
- Retain species curation policy outside draft overrides. Disabled-but-retained future species may retain data, while current spawn eligibility remains curated. Permanently retired content fails promotion.
- Keep global cosmetic/content preferences and player spawn exclusions separate from official balance. Developer drafts must not become ordinary player balance controls.
- Preserve existing characters' base stats, species-derived assignments and relevant effective species properties when defaults change. Snapshot or retain their referenced data revision wherever current code derives these properties dynamically. Newly generated individuals use the latest official defaults. Do not reroll existing recruits, former mercenaries or contract enemies. Older saves need an explicit baseline-enrollment rule because they lack the new snapshots.
- Distinguish species-data preservation from changes to shared trait definitions or global combat formulas. Merely storing a revision string does not freeze those effects. Any future formula rebalance that would alter existing characters requires an explicit compatibility decision. This phase must inventory and explain such exceptions before claiming preservation is complete.
- Stamp campaign and diagnostic metadata with official revision and developer-override status. Historical records retain the revision under which they were produced.
- Stage promotion, validation and change-record generation before activation. A failed or interrupted promotion must leave the previous official profile usable. Repeating the operation must not duplicate change entries.
- Promotion should prepare the dated associated notice and CHANGES.md record for exact data paths. A build check verifies profile validity and required records. This lets the curator publish values without hand-editing source or legal-record text. It does not claim to automate every distribution obligation.

## Planned grouped acceptance

Use synthetic legacy slots, missing/null metadata, corrupt data, duplicate/missing tag IDs, retired references and unsupported versions. Verify complete fallback, actionable diagnostics, draft isolation and export/import round trips. Check four-stat ranges and the conversion marker, AP, traits and tag effects in real generated units and Battle Lab results. Check direct metadata consumers for bypasses.

The decisive curator check is to edit one species stat and one trait/tag assignment, export, promote, build, then confirm the new defaults in a clean synthetic user directory. Also confirm ordinary settings and existing-save behavior under the selected policy. No personal draft or save needs to be deleted or supplied to the agent.

## Implementation gate

Editing surface and new-generation-only species updates are confirmed. During A1, identify necessary per-character snapshots, old-save enrollment and numeric trait-effect coverage before committing the schema. Password behavior is a convenience lock under the user's established expectations.

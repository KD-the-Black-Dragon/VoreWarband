# 40A — Official balance authoring and character preservation

Implementation record, 2026-11-06. The user authorizes [phase 40A](../specs/40a-official-balance-authoring.md). Other module 40 phases remain subsequent work.

## Curator workflow

Layout feedback, 2026-11-06. The Balance workflow entry now anchors to the bottom-left action bar, to the left of Save Changes and Cancel Changes. Its entry canvas stretches to the editor's dimensions. The workflow window keeps its centered placement. This source-only adjustment follows the build/check evidence below. It has no new Unity validation or replacement player build.

1. Open Race Editor through its existing password gate. This explicitly starts a developer draft. General Options, Trait Options and Unit Tags Options remain available.
2. Edit species stats, traits and tag modifiers. Open **Balance workflow** for draft saving/loading, package review/import/export and species base AP. The main menu and Battle Lab identify the active developer draft.
3. **Save draft** retains a new JSON generation under the selected UserData BalanceDrafts slot. **Load saved draft** resumes the newest valid generation, retaining corrupt generations and reporting recovery. **Export package** writes a portable JSON package and bundles Unit Tag definitions. **Show export folder** opens its folder. Repeated UI exports choose a fresh filename.
4. Test new individuals in Battle Lab or a developer campaign. Existing characters keep their stored settings. **Use official defaults** ends draft use. Cancelling Race Editor restores the profile/draft that was active before opening it.
5. In Unity, select **Warband > Balance > Import and promote**. Choose the exported package, review field-level differences and promote approved values. A stale draft requires an explicit rebase after reviewing its differences. Rebase retains the reviewed values and updates their base identity.
6. Restart Play mode or make a fresh build to load promoted official defaults. Clean clients use packaged data without curator UserData. Normal startup never auto-activates a saved draft or legacy EditedRaces binary.

**Promote reviewed values to shipped defaults** publishes the reviewed data as the next official revision. **Rebase reviewed values onto current official revision** changes the draft's parent revision/hash while retaining its reviewed values. Rebase is for stale drafts, does not publish them and does not merge newer official values into them. The curator reviews the full comparison again before promotion.

Legacy binary slots can be imported deliberately through **Import legacy slot**. Unavailable records are reported and originals retained. Retired or unsupported trait IDs are excluded during legacy conversion. Missing dependencies or malformed supported entries reject the candidate as a whole. Import review does not grant species spawn eligibility.

## Data and preservation

- Official resources are immutable JSON revisions under `Assets/Resources/WarbandBalance/Profiles`, with `active.txt` as the append-only checksum commit catalog. A partial/corrupt newest commit falls back to the preceding valid commit. The build guard requires a complete final commit and its modification records.
- Initial official data and the archived `legacy-pre40a` profile come from retained pre-40A defaults. STR/AGI/INT/VOR ranges are represented directly. Exporting does not re-average four-stat data. Missing required fields, unsupported schemas, non-finite values, duplicate species/tags, retired traits/species and missing tag dependencies are rejected.
- Promotion serializes concurrent publishers, rechecks the base under the lock and prepares the profile, associated NOTICE and CHANGES record before committing. Original/prepared records remain under project Logs for interruption recovery. No generation cleanup occurs.
- Each new Unit snapshots species settings, AP, trait assignments, Unit Tag definitions and species spell sources. These survive company and battle serialization. Combat, AI, power scoring and tooltips use the unit's snapshot. New individuals use the current official profile or explicit draft.
- Older company saves enroll against the frozen pre-40A profile, preserving stored stats, health and observed trait assignments. Historical local-only numeric overrides that were never stored in those saves cannot be reconstructed automatically. Such developer saves may need deliberate migration using their original settings. New snapshots remove this ambiguity going forward.
- Global combat formulas and the implemented logic of a trait remain code. Unit Tag numeric modifiers and species trait assignments are authorable data. Legacy power adjustment, upkeep/deployment, mappings and automatic weapon-selection flags retain their legacy scope. They do not define the new company wage curve or manual equipment permissions.
- Good/Bad/Neutral classifications have a reserved profile field. The reviewed classification inventory and post-battle grants belong to 40D.

## Loading failure review

Retained MatronsMinions and FeralUmbreon have null metadata. Old sanitization dereferenced metadata without guarding those entries. The focused fixture reproduces this failure condition and verifies safe retention. The user's private failing binary was not inspected, so its precise contents are not asserted. Normal play now loads validated packaged defaults. Developer imports report invalid data without replacing a valid active draft.

## Verification and review build

- Final focused run passed **72 assertions**, actual Unity exit 0. `Logs/balance40a-final3.log` and the result under the retained directory named by `Logs/balance40a-latest.txt` cover real password entry, native Race Editor controls/tabs, draft cancellation, new-unit stats/AP/traits/tags, existing-unit preservation, package validation, saved-draft corruption recovery, export/import, clean-client data installation, immutable promotion, concurrent/stale commit rejection, retained record backups, catalog recovery, company saves, legacy enrollment and explicit binary imports. Native modifier calculations also verify outgoing/incoming behavior, serialized effects and isolation from later tag edits.
- Existing grouped regression passed **963 checks**, actual Unity exit 0. `Logs/balance40a-regression-result.json` and `Logs/balance40a-regression1.log` cover the established company, combat, gear, save/battle, developer lab and world workflows. Startup UserData was separately isolated under Logs for this run.
- Fresh private Windows player built with actual Unity exit 0. `Logs/balance40a-build1.log`, build pointer `Logs/balance40a-build-directory.txt`. Executable is `Builds/Warband-Review-20261106-40a-62e4c791/Win64/VoreWar.exe`. SHA256 is `8c2f1c941fa8a64166ef56e921d6cd2807a07c7d634e1e619f1b923d2baee868`. The official-balance build guard passed outside Play mode. Gameplay assembly SHA256 is `a65036a8420957a2e357cdda96d93c0b865f5fed1e08c736cc6910dadbc468e5`. Launcher, gameplay assembly and resource hashes are in `Logs/balance40a-player-hashes.json`. Only the editor audit gained additional checks after this build; `Logs/balance40a-build-source-verification.json` confirms production source remained identical.
- Compiler review has zero C#/shader errors. There are 16 normalized build warning categories versus 42 in the previous build, with zero new categories. Source-coordinate-normalized warning comparisons are in `Logs/balance40a-compiler-review.json`. All 25 changed/added C# files have dated notices and retain preceding notice lines. `Logs/balance40a-source-manifest.json` records source hashes/coverage. License.txt retains SHA256 `f29a6348428c7ae57a3011fa79f9f8aec3511375227f4ac3157d2ff71fb8c985`.
- Seven existing license/change/dependency notices are packaged beside the player and verified in `Logs/balance40a-notices.json`. Existing scenes, prefabs, packages and project settings have no packet changes.

Earlier native pilots passed 50, 58, 61 and 64 checks as coverage grew. Pilot2's detached launcher did not retain an exit code. Restricted pilot1 stalled on license access; pilot3 found the already-open licensed pilot2 instance. Later explicit process handles captured actual successful exits. These launch diagnostics are retained separately from final evidence.

All focused fixtures redirect startup UserData and company saves to a unique project Logs directory. Synthetic promotion projects and original/corrupt files are retained. No personal save or draft is used as an automated fixture, and no file deletion or moves are performed. Native UI callbacks are exercised. Physical desktop interaction and curator workflow review remain a grouped player check.

## Grouped curator check

1. Open the review build, unlock Race Editor and edit one curated species stat range or trait assignment. Save the draft, reopen the editor and use Load saved draft. Confirm values and tabs are usable.
2. Test newly generated individuals in Battle Lab, then use Balance workflow to export a package and inspect its review. Use official defaults to leave draft mode. Existing saved company characters should retain their earlier stats, traits and AP.
3. In Unity outside Play mode, import that package through Warband > Balance > Import and promote. Review and promote intentional changes, restart Play mode and create a new individual to confirm the shipped-default authoring path. Report any unexpected fields in the comparison before promoting them.

The user accepted 40A and its subsequent footer placement adjustment, then authorized 40B. The 40B private player includes that footer adjustment.

## Licensing and scope

Per-file dated conversion notices and CHANGES cover the source additions/edits. Associated NOTICE and promotion entries cover JSON/text assets and their metadata. Existing GPL-3.0 terms and upstream notices remain. Newtonsoft.Json 13.0.3 uses its existing MIT notice, and Odin Serializer its existing Apache-2.0 notice. No new dependency, artwork or externally copied code was introduced. Public-release provenance and Corresponding Source gates remain separately open.

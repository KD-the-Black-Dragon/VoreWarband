# AGENTS.md

Working instructions for contributors and AI agents in this VoreWar checkout. Read this file at the start of each task, then inspect the relevant project files and current Git state before making changes.

## Project identity

- **Project:** Vore Warband, a VoreWar total conversion and turn-based tactical game with RPG elements in a vore-related setting. The README describes it as a continuation of darkevilme's Vore War.
- **This checkout:** The `KD-the-Black-Dragon/VoreWar` fork (`origin`). The configured source repository is `Flame-Valxsarion/VoreWar` (`upstream`). Verify `git remote -v` if repository identity or remotes matter; remotes can change.
- **Engine:** Unity **2018.3.12f1**, recorded in `ProjectSettings/ProjectVersion.txt`.
- **Code and content:** C# scripts and Unity assets are under `Assets/`; package dependencies are declared in `Packages/manifest.json`.
- **License:** The repository includes GNU GPL version 3 in `License.txt`. Retain the license and upstream copyright/attribution notices. Check individual asset and dependency notices before changing or redistributing them; they may have separate terms.
- **Open the project:** Use Unity Hub or the Unity Editor with the version recorded above. For any Unity launch, use the normal desktop environment with the active license; do not try a restricted launch first because it is known to fail here. Do not assume Node, Vite, ChatGPT Sites, OpenSpec, or the previous project's release/deployment tools apply here.
- **Build and tests:** Follow instructions found in this checkout and its installed Unity project. No root-level automated test or build workflow was identified during initial orientation; inspect the relevant code, packages, and documentation before recommending or inventing one. Whenever asked to produce a new test build, run the available Unity build workflow and provide the resulting executable path; do not hand off build instructions in place of the requested build. If the available Unity license or build route blocks completion, report the specific blocker and do not claim a build succeeded. Routine changes do not require a build unless requested.

## Collaboration preferences

The user leads creative direction and may describe intent in technical terms. Preserve the requested experience and mechanics. Treat a proposed technical implementation as a way to express the goal, not automatically as a binding implementation detail. If another approach would materially improve correctness, performance, readability, maintainability, or compatibility with this older Unity version, explain the tradeoff in plain language before making a non-obvious substitution.

The user is not a programming specialist. Explain decisions and results in clear, concise language, and give player-facing checks rather than asking the user to perform code inspection or run technical commands when the agent can do that work.

A direct request to make a change authorizes work within that request. A request to plan, review, or discuss does not authorize implementation. Keep work to the requested scope; do not introduce unrelated cleanup, refactors, features, or changes to upstream behavior. For larger work, state the scope and split it into reviewable steps. Ask when a gameplay or product requirement is genuinely ambiguous; do not invent mechanics.

## Player-facing terminology

Use **vore** as the general term in labels, help, tooltips and messages. Do not substitute **consumption** for it. This is the user's explicit terminology preference (2026-11-02); more specific mechanical terms such as digestion remain appropriate.

Use **herm** / **herms** in player-facing text; do not expand these to **hermaphrodite** / **hermaphrodites**. Preserve existing wording during UI/layout changes unless the user asks for a wording change. Outright spelling mistakes may be corrected freely; bring ambiguous wording or terminology changes to the user for judgment. This is the user's explicit direction (2026-11-02).

## Preserve the game and the fork

- Preserve existing behavior unless the user requests a change.
- Keep changes as small and clear as practical. Prefer straightforward, idiomatic C# that fits Unity 2018.3.12f1 and the existing codebase; check API and language compatibility before using newer Unity or C# features.
- Respect the upstream project's structure, attribution, copyright notices, asset provenance, and GPL-3.0 license. Do not replace or remove license notices as part of routine changes.
- Avoid unrelated asset reimports, package upgrades, scene/prefab rewrites, or broad formatting changes. Unity can modify serialized assets as a side effect; inspect the resulting diff and keep only changes required by the task.
- Do not assume this fork uses the upstream project's hosting, release process, credentials, or services. Publishing, uploading, or changing external services requires explicit authorization.

## Gameplay responsiveness and practical recovery

User direction, 2026-11-06. This is a video game. Immediate response to player commands takes priority over exhaustive data preservation. Crashes, accidental closure and power loss may lose progress since the last completed save. Autosaves, backups and rollback are a practical soft landing.

- Keep player input, visible action results and animation responsive. Do not block gameplay on autosave serialization, validation, disk writes, flushing, backup maintenance or save-browser catalog scans. Use background work with a small, consistent snapshot and avoid locks that make gameplay wait for the worker.
- Autosave at the start of each campaign day and each player turn in battle. Do not add saves after individual moves, purchases, hires or other small actions unless the user explicitly changes this policy.
- Do not add full-campaign copies, serialization round trips, repeated whole-graph validation or other costly per-action safeguards merely to obtain stronger recovery guarantees. Prefer cheap preconditions and appropriately scoped state updates. Any necessary snapshot capture must fit the gameplay frame budget and be measured on representative company/battle data.
- Keep save browsing lightweight. Read summaries and load details on demand or in the background, with paging/caching where appropriate. Large save collections must not freeze the interface.
- Keep reasonable protection against incomplete writes and some recoverable backups, but do not pursue zero data loss, exhaustive verification or transaction-style durability at the expense of play. Extra safeguards must justify their player-visible cost.
- Treat a noticeable pause caused by added engineering safeguards as a regression. Explain any genuinely unavoidable delay and obtain design guidance before making it part of the normal gameplay loop.

## GPL-3.0 compliance for every source change

Every intentional source-code change must include a GPL-3.0 compliance check before handoff. This includes new code, modifications, and any authorized retirement or relocation of source. Treat this as part of the change itself, including during private development, so notices are ready before distribution. This project workflow is deliberately ongoing; GPL section 5 concerns conveying modified source, while section 2 permits private modifications without requiring public release.

Use the actual terms in `License.txt`, especially sections 4 and 5, rather than relying only on this summary. Keep this fork's covered derivative work under the applicable GPL-3.0 terms. Preserve existing copyright, license, attribution, additional-term, and warranty notices. Do not silently change an existing version grant to "only" or "or later," invent copyright ownership, or relabel separately licensed assets/dependencies as GPL-covered.

For each source change:

1. Add or update a prominent notice near the top of each modified source file, after preserving existing legal notices. Identify the VoreWar mercenary-company conversion, the actual modification date in `YYYY-MM-DD` form, and a short description. Retain meaningful earlier modification notices. Mark new source files as additions by this fork, without implying that they came from upstream. These per-file notices are this project's practical policy for keeping modifications distinguishable; section 5(a) itself speaks of notices on the modified work and does not mandate this exact format.
2. Add a dated entry to root `CHANGES.md` listing the affected source paths, a concise description, and the related spec where applicable. A reviewable change batch can have one entry covering several files; an exhaustive design narrative or per-line history is unnecessary. Use real dates, preserve earlier entries, and distinguish completed changes from plans. Git history supplements this record rather than replacing the notices.
3. Check notice placement and coverage against the actual diff. For non-commentable source/content formats such as JSON or Unity serialized assets, use a clearly associated notice/change record identifying the exact paths rather than inserting invalid syntax. Review whether the notice remains prominent in the form that will be distributed.
4. Review any introduced/copied code, asset, or dependency for its applicable license, notices, and compatibility with the intended distribution. Do not assume that an upstream inclusion or a passing Unity build proves that this review has been done. Record a concrete unresolved issue if the evidence is insufficient.
5. Report the check briefly at handoff: modification notices/dates, `CHANGES.md`, and preserved licensing/attribution, plus any unresolved distribution issue. Do not claim complete GPL compliance from a changelog check alone.

A suitable C# modification-notice pattern, adapted to the actual change, is:

```csharp
// Modified for the VoreWar mercenary-company conversion.
// YYYY-MM-DD: Brief description of this file's changes.
// See CHANGES.md for the fork's change record and License.txt for GPL-3.0 terms.
```

Before any authorized source or binary distribution, review the applicable obligations again: include the license and required notices; clearly identify the modified work and its GPL terms; preserve or provide Appropriate Legal Notices where section 5(d) requires them, taking account of its exception for original interfaces that did not display them; and satisfy section 6's Corresponding Source requirements for the chosen binary-distribution method, including necessary build scripts and Installation Information when applicable. Review third-party redistribution terms separately. A source link, Git history, or `CHANGES.md` alone is not automatically sufficient for every distribution method.

Documentation-only changes should be described accurately and must not be recorded as gameplay-source changes. Do not add modification headers to untouched upstream files merely to make the entire tree look newly licensed. These instructions do not authorize publishing or override the file-operation restrictions below.

## Resuming the staged conversion

For interrupted migration work, read `docs/implementation/resume-checkpoint.md` after checking Git status. It records the active packet, latest user confirmation, performed checks and exact next action. Update it at meaningful checkpoints; do not treat an incomplete check as passed or restart accepted packets.

## Change workflow

1. Read this file, `README.md`, and any more specific `AGENTS.md` files that apply. Check `git status` before editing so existing user changes are preserved.
2. Trace the affected behavior through the relevant C# scripts, scenes, prefabs, ScriptableObjects, and assets before choosing an implementation.
3. Implement only the requested change. Update project documentation when behavior, setup, or contributor instructions change.
   For every source-code change, complete the GPL-3.0 check above and update its notices and `CHANGES.md` as part of the same work.
4. Run checks only when the user asks for testing/verification or the task explicitly includes it. Use the narrowest relevant checks documented by this Unity project, and report what ran and any limits. Do not add a test framework or new tests unless requested.
5. For meaningful gameplay or UI changes, provide a short, concrete in-game checklist in chat. Distinguish checks the agent actually performed from playtesting that still needs the user's confirmation. For staged work, wait for feedback on the current stage before starting the next one.

If this repository later adds an OpenSpec workflow or another project-specific process, follow its current instructions when applicable. Do not require artifacts from the previous project when they are absent here.

## File operations: do not delete

**Never delete files or directories.** This includes `git rm`, `rm`, `del`, `Remove-Item`, `git clean`, deleting through a GUI, or any command that destroys a path. Editing lines or content within a file is allowed.

User clarification (2026-11-03): the game may delete its own saved games at runtime, including Ironman loss and player-confirmed save deletion. This authorizes implementing that game behavior, not agent filesystem deletion. Automated checks must use synthetic data, preserve their files and suppress actual deletion; never exercise deletion against personal saves.

If a file must be removed from active use, ask the user before taking action. If the user wants it retained but out of the working tree, move it into a project `Recycle Bin/` directory; do not copy it and then delete the original. Never force-add Recycle Bin contents.

Before any copy or move, verify that the operation is intentional and that the destination is inside the project or explicitly requested. Do not use destructive cleanup to resolve Unity-generated files or Git changes.

## Quality and reporting

- Keep code readable, consistent with nearby code, and free of unused imports, debug output, and dead code introduced by the change.
- Consider likely regressions in gameplay, save data, scenes/prefabs, input, and supported platform behavior.
- Do not claim a build, automated test, or manual playtest passed unless it was actually performed. State important limitations plainly.
- Before handing off, review the diff for scope, accidental Unity serialization changes, and preservation of unrelated user work.

## Review cadence

Group related, approved low-risk ability retirements into a consolidated stage. Complete the source review, compile and scene checks before requesting one combined manual test; do not request a new test after each one-to-three-ability packet. Split only where a genuine design decision or material risk requires it.


### Larger milestones and manual testing (user direction, 2026-11-01)

The user accepted 03B8 and explicitly requested larger groups and fewer repetitive tests. Group the remaining low-risk cleanup into one closure milestone. Internal source batches/checks may remain separate for diagnosis, but do not stop after each batch to request the same menu-opening test. Request manual testing when a milestone adds a meaningful player workflow, a material behavior/serialization risk needs human observation, or a failure needs focused diagnosis. Documentation-only, engine-equivalent and repeated low-risk retirements do not need fresh manual checks by default. Preserve necessary compile/source/GPL checks and report actual limits; batching is not permission to skip verification or claim untested battles passed.

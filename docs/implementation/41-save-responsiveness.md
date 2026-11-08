# Save responsiveness follow-up

Implemented, 2026-11-06. Native checks passed, and the private Windows player built successfully. Grouped player review is pending. The user prioritizes immediate gameplay response and accepts losing progress since the last completed autosave. AGENTS.md records this standing policy.

## Changes

Autosaves are queued at a new campaign's opening day, each subsequent day start and each player battle-turn start. Ordinary moves, purchases, hires, equipment changes, decisions and battle-result screens no longer autosave. The worker serializes a detached consistent copy, writes a private staging file, checks its checksum and publishes it atomically. It keeps the latest generation and three valid backups per slot. Queued snapshots for the same company/slot coalesce. Loading, manual saving and game-owned deletion invalidate pending older jobs before publication.

Input and animation do not wait for background encoding, verification, disk work or backup maintenance. A small main-thread capture copies mutable game data, retaining aliases and cycles. Already-frozen species records are shared read-only. Failure is reported without blocking play. Application exit may interrupt a pending save, whose staging file remains excluded from loading. The last completed save provides recovery.

The save browser opens immediately and reads its catalog in the background. New format-2 saves contain compact preview data. Paging and hovering reuse that catalog. Format-1 saves remain readable and derive a summary in the background. Full gameplay graphs are loaded only when resuming or explicitly requested by developer checks. Explicit Save/Quicksave requests also capture their click-time state and run on the same worker, so choosing Save cannot make the gameplay thread wait for an autosave. Pending manual requests survive normal timeline/autosave cancellation; deliberate save/company deletion invalidates them. The low-level synchronous writer remains available to native diagnostics, with the player-facing routes using the worker.

Walking uses a local rollback of position, AP, newly revealed cells, RNG and any appended event characters/context. Generic management actions use their domain preconditions and existing scoped rollback. Whole-campaign serialization/validation is removed from these action paths. Daily-stage/battle-transition rollback still uses detached in-memory copies.

## Native validation

- Final focused native run passed 51 checks with actual Unity exit 0, in `Logs/save41-native7.log` and `Logs/save41-runtime-result.json`. It verifies capture isolation, movement/management while the worker is deliberately held, stale-job cancellation, continued play after save failure, nonblocking manual saves, format-1 compatibility, full battle restoration, player-turn-only cadence, corruption recovery and Ironman restrictions.
- A hundred named slots list without gameplay-graph hydration. The main-thread browser opened in 10.4083 ms and its background catalog took 889.7173 ms. A representative detached snapshot took 6.3587 ms. Metrics are retained in the focused result directory. These are editor-machine measurements, not universal latency guarantees.
- Route regression passed 937 checks with actual Unity exit 0. Result directory is located by `Logs/save41-travel-latest.txt`, log `Logs/save41-routes1.log`.
- Grouped regression passed 963 checks with actual Unity exit 0, in `Logs/save41-regression-result.json` and `Logs/save41-grouped1.log`.
- Static runtime/editor compilation against the installed Unity 2018.3 assemblies also passed. Earlier approval-service timeouts, a restricted license failure, headless rendering crash and obsolete enemy-turn test expectation are retained as diagnostic history. The final audit disables rendering and matches the requested cadence.

All fixtures use synthetic data under project Logs, suppress actual deletion and retain their files. Personal saves are excluded. Earlier accepted 40A/40B work is preserved. Scenes, prefabs, packages and project settings have no packet changes.

## Private build and player check

The private Windows player built successfully with actual Unity exit 0, log `Logs/save41-build1.log`. Launch `Builds/Warband-Review-20261106-save41-facf910b/Win64/VoreWar.exe` with its full folder present. It includes the accepted 40A footer fix and 40B route workflow.

Fifteen source notices and three new metadata companions are recorded in `Logs/save41-source-manifest.json`. Player hashes and actual native exit codes are in `Logs/save41-build-manifest.json`. Seven inherited license/change/dependency notices are packaged and hash-verified in `Logs/save41-notices.json`. No physical desktop playtest is claimed.

1. Move repeatedly, buy equipment and hire a mercenary. These actions should respond immediately and create no autosave.
2. End the day, then keep moving or managing the company immediately. Check the map autosave restores that day's opening state.
3. In battle, check autosaves at player-turn starts, then continue acting immediately. Check an explicit mid-turn Save/Quicksave and restore.
4. Open Load Game with a large collection, browse pages and hover for summaries. The menu should stay responsive while its catalog is read.

Graphical input/feel remains a grouped player review. Interrupting the application can lose progress after the last completed write. Incomplete staging files stay excluded from loading.

## Licensing

Dated per-file notices and root CHANGES identify all source changes, additions and metadata. Actual License.txt sections 4–5 reviewed. Inherited licensing/attribution is retained. No new external asset, dependency or copied code. Public-distribution provenance and Corresponding Source review remain separate open gates.

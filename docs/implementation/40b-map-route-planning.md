# 40B — Explored routes and animated travel

Implemented, 2026-11-06. Native validation and the private Windows build passed. Grouped player review remains pending.

## Scope

Adjacent movement remains single-click. Distant destinations preview on the first click and travel on the second consecutive click. Native explored-only weighted paths use gold within budget and red beyond it, with remaining AP on each tile. Values below zero show extra actions needed today. The existing C marker slides for 0.1 seconds per committed tile.

Routes stop on exhaustion, road events and contract deployment. Destination intent persists until arrival or another map selection and is included in saves. Resumption is one deliberate click after a new day or resolved interruption. Manual End Turn and normal random-event frequency are retained.

## Wiring

- `CompanyTravel` owns deterministic Dijkstra routing and transactional native steps. Hidden terrain and intervening active enemy camps are excluded from planned travel.
- `CompanyCampaignView` renders only visible arrows/labels, animates the existing marker and camera, and handles confirmation, retargeting and one save at a travel stop.
- `CompanyCampaign` and its existing save round-trip guard retain nullable destination intent. Old saves need no destination migration.
- `CompanyTravelAudit` checks native UI callbacks and synthetic campaigns. `TraitCurationAudit` has a separate 40B report selector.

All synthetic startup settings, saves and diagnostic output are retained under project Logs. No personal saves or drafts are used as fixtures. No files are deleted or moved.

## Native evidence

The first native pilot passed route calculations and the independent mixed-terrain oracle, then found that the synthetic test had not opened its map view. The second pilot passed 915 checks through event/save resumption before its contract fixture attempted acceptance away from the offering town. Both setup errors were corrected in the audit. These failed runs are retained and are not final acceptance evidence.

Final focused pilot 3 passed 937 checks with actual Unity exit 0. Its result is located by `Logs/travel40b-latest.txt`, with log `Logs/travel40b-pilot3.log`. The independent oracle compares all 36 destinations across 12 mixed-terrain/fog grids. A full 120×80 region route took 18.134 milliseconds in that run. Native click callbacks also verify retargeting during movement, day resumption, event saves and contract deployment.

The grouped review passed 963 checks with actual Unity exit 0, recorded in `Logs/travel40b-regression-result.json` and `Logs/travel40b-regression1.log`.

The fresh Windows player built successfully in `Builds/Warband-Review-20261106-40b-70790a95/Win64/`, with actual Unity exit 0. Launch `VoreWar.exe` with the whole folder present. It includes the accepted 40A Balance workflow footer adjustment. Build log `Logs/travel40b-build1.log`. Seven inherited license/change/dependency records are packaged alongside the player and hash-verified in `Logs/travel40b-notices.json`. Source and player hashes are retained in `Logs/travel40b-source-manifest.json` and `Logs/travel40b-build-manifest.json`. No physical desktop review is claimed.

## Grouped player check

1. Click an adjacent tile once, then choose a distant explored tile. Confirm preview, AP numbers and red over-budget segments, then click it again to travel.
2. Select another distant tile while travelling. Confirm movement stops at its committed step and a second click is required for the new destination.
3. Exhaust today's actions, explicitly end the day and click the retained destination once to resume. Check save/load retains the arrow without moving on its own.
4. Continue into a road event or contract. Confirm movement stops for its normal review and the pending route can resume afterward. Check arrows and the 0.1-second marker animation visually in the player.

## Licensing and limits

Per-file conversion notices dated 2026-11-06 and root CHANGES identify all source changes/additions and metadata. Actual License.txt sections 4–5 reviewed, with upstream copyright, licensing, attribution and warranty notices preserved. This implementation introduces no external code, new artwork or dependency. Public-release provenance and Corresponding Source obligations remain separate open distribution gates. Native automated results do not establish visual approval.

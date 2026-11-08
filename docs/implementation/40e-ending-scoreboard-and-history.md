# Phase 40E — Company endings, retirement and High Scores

**Status:** Implemented; awaiting one grouped player review. Scores remain provisional.  
**Date:** 2026-11-08  
**Specification:** [40E ending scoreboard and durable history](../specs/40e-ending-scoreboard-and-history.md)

## Delivered

- New campaigns collect a separate company name (up to 64 characters). Save previews and records display it beside the leader's name. Blank or older names display as **Unnamed Company**.
- The company record shows the current score projection. Terminal records show the final score components, approved campaign statistics and memorial.
- Retirement becomes available in a town from Day 14 onward. The player first sees a score projection, then a separate Yes/No confirmation. No returns to the projection; Yes ends the campaign and opens its final record.
- A **High Scores** button is available from the main menu and company record. The list ranks endings by final score and opens a detailed score/statistics/memorial view.
- Provisional formula: reputation +1 each, days +1 each (maximum 100), mercenary loss −10 each, leader digestion −20 each, with a ×0.9 multiplier for company wipeout. Scores retain their full numeric value and display at most one decimal place.
- Battle endings, upkeep insolvency and retirement each assign a unique ending ID. Distinct endings from reloaded branches remain separate; repeated presentation of one ending resolves to that same ID. Old ended saves without a 40E ending ID are not backfilled.
- Compact immutable score records live in a separate `HighScores` directory under the company's persistent-data root, independent of resumable campaign slots. Background writes are read-back checked and atomically published; the High Scores view can include an in-progress record. Corrupt records are skipped individually.
- Ironman campaign-save removal now happens in the background only after its ending record is published. A background pass at the next main-menu startup recovers completed staged records and retries cleanup for published Ironman endings if the prior process stopped between publication and cleanup. This keeps disk work off the player's result transition and preserves the required record-before-cleanup order.

## Scope and limitations

No score tuning, extra ending bonuses, or legacy-history reconstruction was added. UI copy is limited to the flow and score explanations specified by the curator. Background writes can fail; the record screen reports that state and reopening it retries. If the application exits before a background record finishes, recent ending history may be lost, consistent with the project's responsiveness-first recovery policy. The history viewer currently reads its compact record files when opened; its behavior with extremely large archives has not been measured.

## Checks and player review

No Unity build, automated checks or gameplay tests were run for this implementation pass. One grouped player review should cover:

1. Enter a company name, create a campaign, and verify the name in its save preview and company record.
2. Verify retirement is unavailable before Day 14 or outside a town; at/after Day 14 in a town, inspect the score projection, select No, then reopen and confirm with Yes.
3. Verify the ending screen shows the breakdown and memorial, then open High Scores from both the company and main menu and inspect entry details.
4. If convenient, end an Ironman campaign and confirm the record appears in High Scores after its company saves are removed.

The user should report any problems before treating 40E as accepted. Phase 40F remains separate.

## GPL-3.0 change record

The changed/new C# files have dated modification/addition notices, and root `CHANGES.md` records the affected paths. `License.txt` sections 4–5 and retained upstream notices were reviewed for this source batch. No third-party code, assets or dependencies were added. This source review is not a complete binary-distribution compliance review; the distribution obligations remain a separate release gate.

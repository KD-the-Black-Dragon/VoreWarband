# 40E — Ending scoreboard and durable company history

Implementation update, 2026-11-08. Part of [module 40](40-authoring-and-campaign-progression.md). The phase is implemented with provisional scoring and is awaiting grouped player review; see [implementation notes](../implementation/40e-ending-scoreboard-and-history.md).

## Existing records

`Assets/Scripts/Subwindows/CompanyMissionsUI.cs` already shows active/ended company records and a memorial. The approved statistics include reputation, total gold earned, hired mercenaries, actual losses, completed contracts, days survived and enemies defeated. Dismissals and quitters are excluded from losses. Extend these established records into a clear ending summary and persistent history.

## Workflow and data

1. Add company naming to new-campaign creation. Persist that name separately from the leader's name and display identity as "[Leader Name] of [Company Name]", for example "BirdGuy of The Fighting Avians". Include the name in save previews and ending records where company identity is shown. If a save has no usable company name, display "Unnamed Company".
2. Show the final identity, end reason, overall score and approved statistics after a terminal outcome. Keep the fallen-mercenary memorial accessible.
3. Store a compact immutable final record independently of resumable saves. Include campaign ID, ending ID, names, record schema, final day, ending category, score and score-formula version, Ironman status, official rules revision and developer-override status. Retain the inputs needed to explain that recorded score.
4. Add a scored history viewer using the established UI style. Selecting an entry shows its detailed accomplishments and score breakdown. The user explicitly wants both an overall score and detailed statistics.
5. Retain distinct endings reached from different continuations of the same campaign. Reopening one already settled ending remains an idempotent operation.

## Voluntary retirement

Retirement is a distinct, terminal ending category and uses the same final-record handling as defeat. It becomes available after Day 14 as a Town option. Selecting it first displays a score projection with the score factors and projected final score. A separate button opens a final Yes/No confirmation that clearly says retirement ends the game immediately. No returns to the projection. Yes ends the campaign, displays the final score and records it in the High Score table. There is no retirement score bonus in the approved provisional formula.

### Provisional score formula

The final score is calculated from these factors:

- Reputation: +1 point per point of company reputation.
- Days survived: +1 point per day, capped at 100 points.
- Mercenaries lost: −10 points per fallen mercenary. Dismissed mercenaries and quitters do not count as losses.
- Company leader digestions: −20 points each time the company leader is digested.
- Company wipeout: multiply the score after the other factors by 0.9.

This scoring is explicitly provisional and may be tuned. Show the component values and the resulting score in the retirement projection and ending details. Do not add a score bonus or hidden factor without curator approval. Rounding and treatment of negative totals are left for implementation review if they affect presentation.

## Terminal transaction and recovery

Trace every end path, including `CompanyBattle.Complete`, immediate leader-digestion loss, upkeep insolvency and voluntary retirement. Persist the final record before Ironman invalidation/removal in `CompanySaveSlots.RemoveCompany`. Archive failure must have a recoverable state and actionable message. Reopening results or retrying cleanup cannot duplicate a terminal record.

Use a stable ending/transaction ID in addition to campaign ID. The user confirms that separate endings from reloaded saves are all retained. Persist the ending identity before archival and carry it through retries. A genuine new continuation gets its own terminal transaction. Repeated display, save recovery or interrupted archival of one terminal transaction must resolve to its existing history record. Define the journal/write order to handle interruption between assigning an ID, archiving and marking settlement complete.

History loading must tolerate a malformed record without losing other entries. Preserve actual unknown fields/version limits deliberately. A history record is not resumable campaign state. Ironman recovery backups remain subject to the existing game-owned save-removal rules. Agent checks suppress deletion and retain all synthetic files.

## Curator decisions

The curator's 2026-11-08 answers settle the outstanding 40E design choices:

- Overall score and detailed statistics are both shown. The provisional formula is listed above; its values are not final balance commitments.
- Retirement is available after Day 14 from a Town option, presents a score projection, and requires a separate final Yes/No confirmation. Confirming ends the run and writes a High Score record; declining returns to the projection.
- Company-name fallback is "Unnamed Company" when the saved name is empty or unusable.
- Do not backfill history from older saves that were already ended before this feature. Do not invent past campaign records.

No further curator decisions were needed for implementation. Scores are displayed to one decimal place only when needed; underlying score values are not rounded.

## Implementation notes

- New ending IDs are assigned only by current 40E ending paths. An ended save without an ending ID is treated as an older record and is not backfilled.
- Final score records are written independently from resumable company saves. File writes run in the background and publish a read-back-checked staging record by rename. High Scores can also display a record while its background write is pending.
- Ironman save removal runs only after the final record has been published. A startup background recovery pass promotes completed staged records and retries cleanup recorded by an already-published Ironman ending. Repeated archive calls reuse the ending ID and cannot create a duplicate entry.
- If a score write fails, the company record screen can retry it; the High Scores screen reports a pending or failed write.

## Planned grouped acceptance

One grouped player review should exercise company naming and save previews, Day 14 retirement availability, the score projection and Yes/No behavior, a terminal battle or insolvency ending, High Scores/details/memorial, and an Ironman ending. Automated and Unity checks were not run for this implementation pass.

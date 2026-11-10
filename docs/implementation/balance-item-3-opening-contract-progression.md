# Balance follow-up item 3: opening contracts and the first difficulty step

Date: 2026-11-08

## Implemented

- A new campaign's starting town creates three opening contracts instead of four. All three are rating 1, as before, and target camps spawn 5–10 Manhattan tiles from the town. The three targets remain at least eight tiles apart. At the current six actions per day, a clear all-grass route takes about one to two days for one target; rough terrain can take longer. This shortens the initial travel while preserving the campaign clock, deadlines, road network, and standard map movement rules.
- Later pools at other towns and refreshed pools retain four offers and their existing 2–6 tile target distance.
- After the company has completed three contracts, newly generated offers have a rating floor of 2. This is still capped by the current reputation rating ceiling and is raised further by the existing company-power floor or calendar pressure when appropriate. The initial three successes each add one reputation, so an unpenalized company reaches the reputation ceiling for rating 2 at the same time the progression floor turns on.
- Existing accepted contracts and offers already in a town pool are not regenerated or changed. The rule affects only newly generated pools. A reputation penalty may lower the eligible ceiling again.

## Numerical reasoning and limits

The existing fixed rating budgets are 75 power at rating 1 and 105 at rating 2, a 40% increase. Their allowed generated-force bands are 69–81 and 96.6–113.4 respectively (the existing ±8% tolerance). This is a deliberate first step after three introductory victories. Rating 2 sapient candidates are still within the existing level-1-to-2 band; the eight-unit cap, quality-first composition and level-based equipment/role traits from item 2 remain in force. Higher fixed ratings continue to advance levels and gear through the existing generation code.

The travel distance uses Manhattan distance rather than a second pathfinding pass. The map remains fully traversable off road; forest and hills can increase action cost, so actual trips may take longer than the one-to-two-day grassland estimate. An eight-tile spacing rule reduces the chance that the three choices point to the same small local area.

This packet does not change the accepted 40F calendar-pressure curve or claim a calibrated win-rate. Its rating step is grounded in the existing fixed-power scale and the user's requested post-opening increase. Four synthetic 45-day campaign histories were run with the existing balanced AI policy (seeds 4101–4104). They ended on days 30, 11, 24 and 33, with 8, 1, 6 and 9 contract victories; all ended in company wipeout before the horizon. Three reached newly generated rating-2 contract work: two completed their rating-2 missions without permanent losses, while one was wiped out after two permanent losses in its first rating-2 contract. The remaining history was wiped out before its third contract victory, after three road encounters and two rating-1 contract battles, only one of which was completed. This small AI sample shows that the rating-2 step can be survivable but consequential; it also shows that three easy opening victories are not guaranteed under all battle/event rolls. AI policy outcomes are not a prediction of skilled human play.

## Verification and licensing

The existing `CompanyBalanceEconomyAudit` opening assertions were updated to require three rating-1 offers, the target range/separation, and rating-2 offers after three wins, while retaining the reputation-cap check and the accepted-contract preservation check. Its late-road-event check was updated for item 2's eight-slug cap. A licensed Unity 2018.3.12f1 desktop batch run compiled successfully, passed all 88 economy/calendar/opening assertions and completed the four campaign histories above. Earlier attempts exposed and corrected a Unity 2018 local-name collision in the item 2 force generator, and stale reflection-based test expectations for the slug cap and contract constants. No player build or manual gameplay playtest was performed. Full outcomes are retained under `Logs/balance34f-9d6afb13db79437e8cdb909111a67c4a`.

Modified source paths:

- `Assets/Scripts/Core/CompanyContracts.cs`
- `Assets/Editor/CompanyBalanceEconomyAudit.cs`

Both files have dated VoreWar conversion notices. Root `CHANGES.md` lists the exact paths. Actual `License.txt` sections 4–5 were reviewed; existing copyright, license, attribution and warranty notices remain. No external code, assets or dependencies were introduced. This source review is not a complete determination of requirements for any future binary distribution.

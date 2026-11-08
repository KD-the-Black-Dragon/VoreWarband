# 40B — World-map route planning

Implementation in progress, 2026-11-06. Part of [module 40](40-authoring-and-campaign-progression.md). The user's implementation instruction replaces the earlier proposed Travel button.

## Accepted interaction

- One click on an adjacent tile immediately attempts that single move, including ordinary exploration into fog.
- A first click two or more tiles away previews the lowest-action route. A second consecutive click on that destination starts travel. Selecting another distant tile replaces the preview and requires its own confirmation.
- Routes use explored terrain only, weighted by native movement cost. A contract marker under fog does not reveal hidden geography or create a routable explored corridor.
- Each route tile shows actions remaining from the current position. Gold marks affordable travel, red marks travel beyond today's allowance. Negative values show the additional actions that would be required today.
- Travel commits one native adjacent step at a time and slides the company marker over 0.1 seconds per tile. Per-step discovery, terrain cost and eligible random-event rolls retain their normal rules.
- Arrival clears the route. Insufficient actions, an event or battle deployment stops travel and retains the destination. After explicitly ending the day or resolving the interruption, one click on the same destination resumes. Travel never ends days automatically.
- Another map destination can interrupt ongoing movement. The already committed step stays spent. The new distant destination is a fresh preview.

## Native integration and persistence

`CompanyTravel.Path` plans explored orthogonal routes, preferring fewer steps on equal AP cost. Active enemy camps are valid destinations but cannot be traversed as intermediate tiles. `CompanyTravel.Advance` shares the native movement, discovery, contract-deployment and road-event entry points with adjacent travel. Contract entry retains the existing final deployment review before committing its target tile and cost.

`CompanyCampaignView` virtualizes route arrows and labels with existing map tiles. The existing C company marker is a separate, smoothly translated overlay. No artwork is replaced. Management and day actions wait until travel stops, and save failure blocks further mutations. Ordinary movement is saved once at arrival or interruption, without writing a full save on each animation frame. Per-step rollback protects native mutations.

The nullable destination is persisted in the existing save foundation. Paths and AP labels are derived again from current terrain and position. Older saves default to no route. Loading does not start movement or replay a committed encounter roll.

The accepted map size, towns, farms, road circuit, action budgets, event frequency and world generation remain governed by their existing rules.

## Grouped acceptance

Native checks cover weighted routes against an independent shortest-cost oracle, fog, retargeting, adjacent movement, AP labels, exhausted movement, explicit day changes, event interruption, native contract deployment, save/load and full-size pathfinding. A private Windows build will support one grouped visual review of arrow readability, camera/marker animation, multiday continuation and event resumption. Recorded results belong in [the implementation report](../implementation/40b-map-route-planning.md).

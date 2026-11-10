# Phase 40F — Calendar difficulty implementation

**Status:** Implemented and accepted by the user on 2026-11-08 at the recorded scope. The numerical tuning remains provisional and may be adjusted from future campaign feedback.

## Behavior

New campaigns enroll at day zero and receive 21 protected campaign days. After that, an accelerating calendar-pressure value affects only newly generated content:

```text
d = max(0, enrolled campaign age in days - 21)
pressure = 0.05 × d + 0.00015 × d²
```

For newly refreshed contract offers, the pressure is stochastically rounded and added above the existing reputation-based rating ceiling (which still ranges from 1 to 20). Existing rating power budgets and rewards are unchanged. The player's current company power also sets a provisional easy-work floor: active members count at full weight, reserve members at 35%, and current condition/injuries and active formation are considered. The minimum offer rating targets at least 40% of this reference power, while retaining the reputation-eligible range. An offer refresh aims for one minimum, one maximum and two intermediate offers. If the approved species cannot fill a requested power band, generation retries nearby lower ratings without crossing the calculated floor or adding an unapproved species.

New Spitter Slug road encounters receive a separate calendar multiplier after the existing company-strength and health factors:

```text
1 + min(3, pressure × 0.015)
```

That encounter-only component is bounded at four times its existing target power to avoid unbounded slug swarms. Contract-rating pressure is uncapped. Existing accepted contracts, enemy forces, rewards, road events and their saved provenance remain unchanged. New contract and road-event records persist the difficulty revision, enrolled age and pressure for save/resume and diagnosis.

Campaigns saved under rules version 4 enroll at their current elapsed day when migrated to version 5. They therefore receive a fresh 21-day protected opening instead of a sudden late-game jump. Funds, active contracts and their generated enemy forces are preserved.

## Pressure examples

| Enrolled day | Pressure | Added contract rating | Road-event multiplier |
| ---: | ---: | ---: | ---: |
| 1 or 21 | 0 | +0 | 1.000× |
| 28 | 0.357 | +0 or +1 | 1.005× |
| 42 | 1.116 | +1 or +2 | 1.017× |
| 100 | 4.886 | +4 or +5 | 1.073× |
| 1000 | 192.716 | +192 or +193 | 3.891× |
| 1007 | 195.129 | +195 or +196 | 3.927× |

Stochastic rounding means the exact rating bonus varies by generated offer. Day 100 is a pacing target, not a guaranteed loss or survival day. The contract ramp eventually becomes very large; the separate road-event multiplier deliberately has a fourfold ceiling and will not itself continue to accelerate indefinitely.

## Exploratory late-curve comparison — 2026-11-09

The editor-only audit compared the production curve with two provisional variants that preserved the production curve through enrolled Day 100, then scaled only additional pressure by 1.5× or 2×. It ran four paired seeds (40701–40704) with the native balanced company policy and a 180-day horizon. All 12 histories completed without driver errors and all 96 economy/calendar/opening assertions passed.

| Seed | End day, all three curves | Contract battles | Permanent mercenary losses | Outcome |
| ---: | ---: | ---: | ---: | --- |
| 40701 | 60 | 40 | 6 | Company wipeout |
| 40702 | 32 | 22 | 4 | Company wipeout |
| 40703 | 30 | 21 | 2 | Company wipeout |
| 40704 | 37 | 23 | 2 | Company wipeout |

Every seed produced the same end day, battle count, losses, completed contracts and remaining gold under `current`, `late-1.5x` and `late-2x`. Each run ended before Day 100, so none reached the point where the candidate curves diverged. These results cannot support increasing the late ramp; the production curve remains unchanged. The outcomes also show early company wipeout under this synthetic AI policy, but are not a prediction of skilled human play. Full synthetic data: `Logs/balance34f-8207824ca9ad4235b93378de3ab91643/economy-campaigns.json`; Unity log: `Logs/balance40f-calendar-curves.log`.

## Verification

Unity 2018.3.12f1 completed the grouped editor audit with **81 checks**, including:

- protected opening, gentle onset, accelerating growth and late-date pressure values;
- actual day-1000 contract refresh and Spitter Slug encounter generation;
- new-company and pre-40F save enrollment;
- generated difficulty metadata surviving save/resume;
- existing accepted contract ratings, rewards and enemy rosters staying unchanged;
- recovery of the normal Ironman ending sequence after updating an outdated audit expectation.

The synthetic day-1000 contract refresh took **150 ms** in this editor run. This single test measurement is not a guarantee for a player build or slower hardware, and should be watched during player review if entering a town at very late dates feels slow.

The connected campaign simulator completed **14 policy/seed histories**, covering **284 battles** and **38 permanent losses**. Three histories reached their 90- or 100-day horizon and were censored; eight ended by company wipeout and three by insolvency. A balanced policy reached day 100 in one seed with 46 battles and one loss, while the other balanced seed ended at day 64. The two conservative policies differed sharply: one became insolvent at day 14 after taking one battle, while the other reached day 90 with 30 battles and no losses. This variation shows the model is sensitive to decisions and random outcomes; these AI policies do not predict skilled human survival or prove every strategy eventually fails. Some early failures predate calendar pressure and are not caused by this phase.

The audit records remain in `Logs/balance34f-ec3cc203888a410f9bd2e976a6d45c06` and `Logs/balance34f-939fa45e6309462c9196d774a43d6d14`. No standalone player build or manual gameplay review was performed in this packet.

## Source and license record

The source paths changed for this phase are:

- `Assets/Scripts/Core/CompanyCampaign.cs`
- `Assets/Scripts/Core/CompanyEconomy.cs`
- `Assets/Scripts/Core/CompanyCampaignSave.cs`
- `Assets/Scripts/Core/CompanyContracts.cs`
- `Assets/Scripts/Core/CompanyRoadEvents.cs`
- `Assets/Editor/CompanyBalanceEconomyAudit.cs`

Each changed C# file has a dated VoreWar conversion notice. `CHANGES.md` lists the same paths and describes this batch. Actual `License.txt` sections 4–5 were checked, and existing copyright, license, attribution and warranty notices remain. No third-party code, assets or dependencies were introduced. This source review is not a complete determination of requirements for future binary distribution; that review remains separate.

## Optional future balance feedback

1. Start a new campaign and inspect contract ratings during the protected opening and after the ramp begins.
2. Check that the offer list still has some sensible training/recovery contracts for the current company, without being cluttered with trivial missions.
3. Travel outside towns later in the campaign and observe any generated Spitter Slug force.
4. If available, load an older campaign and confirm its current contracts and company remain intact and the new difficulty starts gently from its migration point.
5. Report whether the contract pace, road encounters and town refresh responsiveness feel fair. The curve is provisional and can be tuned from that feedback.

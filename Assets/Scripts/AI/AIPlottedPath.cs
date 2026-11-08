// Modified for the VoreWar mercenary-company conversion.
// 2026-11-06: Identify emergency hazard-escape paths for risk revalidation (spec 40C).
// Modified for the VoreWar mercenary-company conversion.
// 2026-11-04: Retain offensive target for reaction-risk revalidation (spec 30).
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
using System;
using System.Collections.Generic;


public class AIPlottedPath
{
    internal Actor_Unit Actor;
    internal Actor_Unit ChargeTarget;
    internal bool HazardEscape;
    internal Action Action;
    internal List<PathNode> Path;
}

// Modified for the VoreWar mercenary-company conversion.
// 2026-11-08: Allow catalog-owned company gear to refresh its display name without changing saved stats.
// 2026-11-01: Stable company equipment definition and instance identities (spec 08).
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
using OdinSerializer;

public abstract class Item
{
    [OdinSerialize] internal string CompanyDefinitionId;
    [OdinSerialize] internal string CompanyInstanceId;
    [OdinSerialize]
    public string Name { get; protected set; }
    internal void SetCompanyDisplayName(string name) { if (!string.IsNullOrEmpty(name)) Name = name; }
    [OdinSerialize]
    public int Cost { get; protected set; }
    [OdinSerialize]
    public string Description { get; protected set; }
    [OdinSerialize]
    public bool LockedItem { get; protected set; }
}

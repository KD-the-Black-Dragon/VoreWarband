using OdinSerializer;

// Modified for the VoreWar mercenary-company conversion.
// 2026-11-08: Allow company catalog binding to refresh weapon art families without changing combat stats.
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
public class Weapon : Item
{

    internal void SetCompanyGraphic(int graphic) { Graphic = graphic; }

    [OdinSerialize]
    public int Graphic { get; private set; }
    [OdinSerialize]
    public int Damage { get; private set; }
    [OdinSerialize]
    public int Range { get; private set; }
    [OdinSerialize]
    public bool Omni { get; private set; }
    [OdinSerialize]
    public bool Magic { get; private set; }

    [OdinSerialize]
    public float AccuracyModifier { get; private set; }

    internal void ResetAccuracy()
    {
        AccuracyModifier = 1;
    }


    public Weapon(string name, string description, int cost, int graphic, int damage, int range, float accuracyModifier = 1f, bool omniWeapon = false, bool lockedItem = false,bool magicWeapon = false)
    {
        Name = name;
        Description = description;
        Cost = cost;
        Graphic = graphic;
        Damage = damage;
        Range = range;
        AccuracyModifier = accuracyModifier;
        Omni = omniWeapon;
        Magic = magicWeapon;
        LockedItem = lockedItem;
    }
}

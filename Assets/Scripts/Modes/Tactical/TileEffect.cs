// Modified for the VoreWar mercenary-company conversion.
// 2026-11-06: Retain optional hazard ownership across battle saves (spec 40C).
// Modified for the VoreWar mercenary-company conversion.
// 2026-11-03: Preserve tile-effect type, strength and remaining turns in battle saves (spec 19).
// See CHANGES.md and License.txt for GPL-3.0 terms.
using OdinSerializer;

class TileEffect
{
    [OdinSerialize] internal int RemainingDuration;
    [OdinSerialize] internal float Strength;
    [OdinSerialize] internal TileEffectType Type;
    [OdinSerialize] internal int? OwnerSide;

    public TileEffect(int remainingDuration, float strength, TileEffectType type)
    {
        RemainingDuration = remainingDuration;
        Strength = strength;
        Type = type;
    }
}

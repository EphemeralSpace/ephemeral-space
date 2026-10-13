using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee.Events;

[Serializable, NetSerializable]
public sealed class ShoveAttackEvent : AttackEvent
{
    public NetEntity? Target;

    public ShoveAttackEvent(NetEntity? target, NetCoordinates coordinates) : base(coordinates)
    {
        Target = target;
    }
}

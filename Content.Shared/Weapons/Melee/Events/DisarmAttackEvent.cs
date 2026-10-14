using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee.Events;

[Serializable, NetSerializable]
public sealed class WideShoveAttackEvent : AttackEvent
{
    public List<NetEntity> Entities;

    public WideShoveAttackEvent(List<NetEntity> entities, NetCoordinates coordinates) : base(coordinates)
    {
        Entities = entities;
    }
}

[Serializable, NetSerializable]
public sealed class ShoveAttackEvent : AttackEvent
{
    public NetEntity? Target;

    public ShoveAttackEvent(NetEntity? target, NetCoordinates coordinates) : base(coordinates)
    {
        Target = target;
    }
}

using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Forensics.Fibers.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESFiberSystem))]
public sealed partial class ESFiberKitComponent : Component
{
    /// <summary>
    /// Time it takes to collect a fiber
    /// </summary>
    [DataField]
    public TimeSpan CollectTime = TimeSpan.FromSeconds(3f);

    /// <summary>
    /// Fiber prototype
    /// </summary>
    [DataField]
    public EntProtoId FiberPrototype = "ESFiberClothing";
}

[Serializable, NetSerializable]
public sealed partial class ESCollectFiberDoAfterEvent : SimpleDoAfterEvent;

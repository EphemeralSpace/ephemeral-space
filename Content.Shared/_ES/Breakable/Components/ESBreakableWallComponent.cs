using Content.Shared.Physics;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Breakable.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ESBreakableWallComponent : Component
{
    [DataField]
    public CollisionGroup BaseLayer = CollisionGroup.MidImpassable | CollisionGroup.LowImpassable;

    [DataField]
    public CollisionGroup BrokenLayer = CollisionGroup.None;
}

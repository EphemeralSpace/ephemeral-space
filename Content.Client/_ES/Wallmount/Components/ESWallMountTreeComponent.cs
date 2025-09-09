using Content.Shared.Wall;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;

namespace Content.Client._ES.Wallmount.Components;

[RegisterComponent]
public sealed partial class ESWallMountTreeComponent : Component, IComponentTreeComponent<WallMountComponent>
{
    public DynamicTree<ComponentTreeEntry<WallMountComponent>> Tree { get; set; }
}

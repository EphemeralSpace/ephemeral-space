// ES START
// Modified to implement IComponentTreeEntry for visibility purposes

using Robust.Shared.ComponentTrees;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;

namespace Content.Shared.Wall;

/// <summary>
///     This component enables an entity to ignore some obstructions for interaction checks.
/// </summary>
/// <remarks>
///     This will only exempt anchored entities that intersect the wall-mount. Additionally, this exemption will apply
///     in a limited arc, providing basic functionality for directional wall mounts.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WallMountComponent : Component, IComponentTreeEntry<WallMountComponent>
{
    /// <summary>
    ///     Range of angles for which the exemption applies. Bigger is more permissive.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("arc"), AutoNetworkedField]
    public Angle Arc = new(MathF.PI);

    /// <summary>
    ///     The direction in which the exemption arc is facing, relative to the entity's rotation. Defaults to south.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("direction"), AutoNetworkedField]
    public Angle Direction = Angle.Zero;

    // ES START
    public EntityUid? TreeUid { get; set; }
    public DynamicTree<ComponentTreeEntry<WallMountComponent>>? Tree { get; set; }
    public bool AddToTree => Arc != Angle.FromDegrees(360);
    public bool TreeUpdateQueued { get; set; }
    // ES END
}

using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Masks.Components;

/// <summary>
/// Marks troupes that the entity is hostile towards, namely for targeting specfic troupes in mob behavior
/// </summary>
[RegisterComponent]
[Access(typeof(ESSharedMaskSystem))]
public sealed partial class ESHostileTowardsTroupeComponent : Component
{
    /// <summary>
    /// If specified, all members not of this troupe will be hostile.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<ESTroupePrototype>>? NonHostileTroupes;

    [DataField]
    public HashSet<ProtoId<ESTroupePrototype>>? HostileTroupes;
}

using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Masks.Components;

/// <summary>
/// Marks troupes that the entity is hostile towards, namely for targeting specfic troupes in mob behavior
/// </summary>
[RegisterComponent]
public sealed partial class ESHostileTowardsTroupeComponent : Component
{
    [DataField]
    public HashSet<ProtoId<ESTroupePrototype>> HostileTroupes = new();
}

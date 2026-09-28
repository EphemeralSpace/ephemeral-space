using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Polymorph.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Polymorph.Components;

/// <summary>
/// A chameleon projector polymorphs you into a clicked entity, then polymorphs back when clicked on or destroyed.
/// This creates a new dummy polymorph entity and copies the appearance over.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedChameleonProjectorSystem))]
public sealed partial class ChameleonProjectorComponent : Component
{
    /// <summary>
    /// Disguise entity to spawn and use.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector DisguiseProto = new NoneSelector();

    /// <summary>
    /// User currently disguised by this projector, if any
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Disguised;
}

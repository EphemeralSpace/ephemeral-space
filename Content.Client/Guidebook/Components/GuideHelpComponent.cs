using Content.Shared.Guidebook;
using Robust.Shared.Prototypes;

namespace Content.Client.Guidebook.Components;

/// <summary>
/// This component stores a reference to a guidebook that contains information relevant to this entity.
/// </summary>
[RegisterComponent]
[Access(typeof(GuidebookSystem))]
public sealed partial class GuideHelpComponent : Component
{
    /// <summary>
    /// What guides to include show when opening the guidebook. The first entry will be used to select the currently
    /// selected guidebook.
    /// </summary>
    // ES START
    // dont write yaml for this
    // bc we are sometimes modifying it to try and point to our prototypes instead
    // also theres no reason for this to write anyway. its client so it wont. but like yknow
    [DataField(required: true, readOnly: true)]
    // ES END
    public List<ProtoId<GuideEntryPrototype>> Guides = new();

    /// <summary>
    /// Whether or not to automatically include the children of the given guides.
    /// </summary>
    [DataField("includeChildren")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IncludeChildren = true;

    /// <summary>
    /// Whether or not to open the UI when interacting with the entity while on hand.
    /// Mostly intended for books
    /// </summary>
    [DataField("openOnActivation")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool OpenOnActivation;
}

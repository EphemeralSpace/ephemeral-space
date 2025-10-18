using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Masks.Components;

/// <summary>
/// Used for members of a <see cref="ESTroupePrototype"/> that can see icons on each other
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESSharedMaskSystem))]
public sealed partial class ESTroupeFactionIconComponent : Component
{
    /// <summary>
    /// The status icon to show
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<FactionIconPrototype> Icon;

    /// <summary>
    /// The troupe that must be shared for this comp to be networked
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<ESTroupePrototype> Troupe;

    /// <summary>
    /// Field shown to members of the same troupe on examine.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? ExamineString;

    public override bool SessionSpecific => true;
}

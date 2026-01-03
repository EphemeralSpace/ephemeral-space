using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Masks.Components;

/// <summary>
/// Used for members of a <see cref="ESTroupePrototype"/> that can see icons on each other
/// </summary>
/// <remarks>
/// The way this works is specifically for IC reasons and kinda strange deduction purposes.
/// In the event that someone swaps bodies, they should still be able to identify other traitor's via their body.
/// However, swapping INTO a traitor's body doesn't give you the same ability. The icon displays on BODIES with the icon
/// But that only occurs if your MIND has knowledge of the traitor troupe.
/// </remarks>
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

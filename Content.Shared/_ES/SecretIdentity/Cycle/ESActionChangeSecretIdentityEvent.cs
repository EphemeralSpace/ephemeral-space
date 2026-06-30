using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.SecretIdentity.Cycle;

/// <summary>
///     An action event for changing to another mask.
/// </summary>
public sealed partial class ESActionChangeSecretIdentityEvent : InstantActionEvent
{
    [DataField(required: true)]
    public ProtoId<ESSecretIdentityPrototype> Mask;
}

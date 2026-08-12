using Content.Shared._ES.SecretIdentity;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Chat.Processor.Components;

/// <summary>
/// Chat channel that is broadcast to all members of an organization
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESOrganizationChatChannelSystem))]
public sealed partial class ESOrganizationChatChannelComponent : Component
{
    [DataField(required: true)]
    public ProtoId<ESOrganizationPrototype> Organization;

    [DataField]
    public HashSet<ProtoId<ESSecretIdentityPrototype>> IgnoredIdentities = new();
}

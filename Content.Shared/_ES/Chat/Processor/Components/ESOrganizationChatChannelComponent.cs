using Content.Shared._ES.Chat.Components;
using Content.Shared._ES.SecretIdentity;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Chat.Processor.Components;

/// <summary>
/// <see cref="ESChatProcessorComponent"/> for a chat channel that is broadcast to all members of an <see cref="ESOrganizationPrototype"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESOrganizationChatChannelSystem))]
public sealed partial class ESOrganizationChatChannelComponent : Component
{
    /// <summary>
    /// The organization that will be broadcast to
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ESOrganizationPrototype> Organization;

    /// <summary>
    /// Members of an organization with these specified <see cref="ESSecretIdentityPrototype"/>s will be ignored and not broadcast to.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<ESSecretIdentityPrototype>> IgnoredIdentities = new();
}

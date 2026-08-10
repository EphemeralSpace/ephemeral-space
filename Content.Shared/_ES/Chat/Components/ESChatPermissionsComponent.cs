using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Chat.Components;

/// <summary>
/// Defines what channels a player can send from.
/// Note that this only applies to player input from the chat box, not all messaging.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(ESSharedChatSystem))]
public sealed partial class ESChatPermissionsComponent : Component
{
    /// <summary>
    /// Chat channels that will always be available.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<ESChatChannelPrototype>> InherentChannels = new();

    /// <summary>
    /// All the chat channels that the person has access to.
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public HashSet<ProtoId<ESChatChannelPrototype>> PermittedChannels = new();
}

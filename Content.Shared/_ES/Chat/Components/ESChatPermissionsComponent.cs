using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

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
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public HashSet<ProtoId<ESChatChannelPrototype>> PermittedChannels = new();
}

/// <summary>
/// Event raised from client to inform the server that the chat permissions need to be refreshed and to send a new state.
/// </summary>
[Serializable, NetSerializable]
public sealed class ESClientRefreshChatPermissions : EntityEventArgs;

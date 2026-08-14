using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Chat.Radio.Components;

/// <summary>
/// Denotes an entity which is capable of receiving radio messages of any kind.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESRadioSystem), Other = AccessPermissions.None)]
public sealed partial class ESRadioReceiverComponent : Component
{
    [DataField]
    public HashSet<ProtoId<ESChatChannelPrototype>> IntrinsicChannels = new();

    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<ESChatChannelPrototype>> Channels = new();
}

using Content.Shared._ES.Chat.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Chat.Radio.Components;

/// <summary>
/// <see cref="ESChatProcessorComponent"/> for a chat channel that models radio messaging to entities with <see cref="ESRadioReceiverComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESRadioSystem))]
public sealed partial class ESRadioChatChannelComponent : Component
{
    /// <summary>
    /// Whether a corresponding server is required for this radio channel
    /// </summary>
    [DataField]
    public bool RequireServer = true;
}

using Robust.Shared.GameStates;

namespace Content.Shared._ES.Chat.Components;

/// <summary>
/// <see cref="ESChatProcessorComponent"/> that provides a formatting to a chat message based on a fixed fluent string.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedChatSystem))]
public sealed partial class ESSimpleFormatChatChannelComponent : Component
{
    [DataField(required: true)]
    public LocId Format;
}

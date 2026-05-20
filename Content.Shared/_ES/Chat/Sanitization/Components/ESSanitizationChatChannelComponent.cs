using Robust.Shared.GameStates;

namespace Content.Shared._ES.Chat.Sanitization.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSanitizationChatChannelSystem))]
public sealed partial class ESSanitizationChatChannelComponent : Component
{
    [DataField]
    public bool ShouldCapitalize = true;
}

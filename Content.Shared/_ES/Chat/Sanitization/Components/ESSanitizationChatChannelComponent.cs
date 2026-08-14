using Content.Shared._ES.Chat.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Chat.Sanitization.Components;

/// <summary>
/// <see cref="ESChatProcessorComponent"/> for a chat channel that sanitizes player input to remove text abbreviations and shorthands.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSanitizationChatChannelSystem))]
public sealed partial class ESSanitizationChatChannelComponent : Component
{
    /// <summary>
    /// Should the start of the message be capitalized.
    /// </summary>
    [DataField]
    public bool ShouldCapitalize = true;

    /// <summary>
    /// The channel that sanitized out emotes are sent on
    /// </summary>
    [DataField]
    public ProtoId<ESChatChannelPrototype> EmoteChannel = ESSharedChatSystem.EmoteChannel;
}

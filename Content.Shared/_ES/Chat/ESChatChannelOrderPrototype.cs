using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Chat;

/// <summary>
/// Prototype that defines the order of chat channels.
/// </summary>
[Prototype("esChatChannelOrder")]
public sealed partial class ESChatChannelOrderPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Ordered list of how the channels should appear
    /// </summary>
    [DataField]
    public List<ProtoId<ESChatChannelPrototype>> Order = [];
}

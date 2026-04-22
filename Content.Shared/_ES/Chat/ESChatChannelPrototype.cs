using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Chat;

[Prototype("esChatChannel")]
public sealed partial class ESChatChannelPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public EntProtoId ChatProcessor;

    /// <summary>
    /// Determines whether any message sent on this channel will be saved to replay.
    /// </summary>
    [DataField]
    public bool SaveReplay = true;
}

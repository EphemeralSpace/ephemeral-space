using System.Diagnostics.CodeAnalysis;
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

    [DataField]
    public List<char> Prefixes = new();

    /// <summary>
    /// Determines whether any message sent on this channel will be saved to replay.
    /// </summary>
    [DataField]
    public bool SaveReplay = true;

    [DataField]
    public SpeechType SpeechBubbleType = SpeechType.Say;

    public bool TryGetDefaultPrefix([NotNullWhen(true)] out char? prefix)
    {
        prefix = null;
        if (Prefixes.Count == 0)
            return false;

        prefix = Prefixes[0];
        return true;
    }
}

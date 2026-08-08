using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Input;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Chat;

[Prototype("esChatChannel")]
public sealed partial class ESChatChannelPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public Color Color = Color.DarkGray;

    [DataField(required: true)]
    public EntProtoId ChatProcessor;

    [DataField]
    public List<string> Prefixes = new();

    [DataField(required: true)]
    public ProtoId<ESChatChannelFilterPrototype> FilterCategory;

    [DataField]
    public BoundKeyFunction? FocusKey;

    // TODO: add datafield for groups for filtering chat channels by.

    /// <summary>
    /// Determines whether any message sent on this channel will be saved to replay.
    /// </summary>
    [DataField]
    public bool SaveReplay = true;

    [DataField]
    public SpeechType? SpeechBubbleType;

    public bool TryGetDefaultPrefix([NotNullWhen(true)] out string? prefix)
    {
        prefix = null;
        if (Prefixes.Count == 0)
            return false;

        prefix = Prefixes[0];
        return true;
    }
}

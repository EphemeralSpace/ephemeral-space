using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Input;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

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
    public int Order = int.MaxValue;

    [DataField]
    public Color Color = Color.DarkGray;

    [DataField]
    public Color TextColor = Color.White;

    [DataField(required: true)]
    public EntProtoId ChatProcessor;

    [DataField]
    public List<string> Prefixes = new();

    [DataField(required: true)]
    public ProtoId<ESChatChannelFilterPrototype> FilterCategory;

    [DataField]
    public BoundKeyFunction? FocusKey;

    [DataField]
    public ESChatBoxLocation ChatBoxLocation = ESChatBoxLocation.Primary;

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

[Serializable, NetSerializable]
public enum ESChatBoxLocation : byte
{
    Primary, // Main chat box
    Stagehand, // Upper chatbox used for stagehand text and notifs
}

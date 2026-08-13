using System.Diagnostics.CodeAnalysis;
using Content.Shared._ES.SecretIdentity;
using Robust.Shared.Input;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._ES.Chat;

[Prototype("esChatChannel")]
public sealed partial class ESChatChannelPrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<ESOrganizationPrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField]
    public bool Abstract { get; private set; }

    [DataField]
    public LocId Name = "generic-unknown-title";

    [DataField]
    public int Order = -1;

    [DataField]
    public Color Color = Color.DarkGray;

    [DataField]
    public Color TextColor = Color.LightGray;

    [DataField]
    public EntProtoId? ChatProcessor;

    [DataField]
    public List<string> Prefixes = new();

    [DataField(required: true)]
    public ProtoId<ESChatChannelFilterPrototype> FilterCategory;

    [DataField]
    public BoundKeyFunction? FocusKey;

    [DataField]
    public ESChatBoxLocation ChatBoxLocation = ESChatBoxLocation.Primary;

    /// <summary>
    /// Channel is used for Forced Damage Say code (GLORF).
    /// </summary>
    [DataField]
    public bool GlorfAffected;

    /// <summary>
    /// Determines whether any message sent on this channel will be saved to replay.
    /// </summary>
    [DataField]
    public bool SaveReplay = true;

    [DataField]
    public SpeechType? SpeechBubbleType;

    [DataField]
    public ESDiscordChannel? DiscordRelayChannel;

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

[Serializable, NetSerializable]
public enum ESDiscordChannel : byte
{
    OOC,
    AdminChat,
}

using System.IO;
using JetBrains.Annotations;
using Lidgren.Network;
using Robust.Shared.Audio;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Chat;

[Serializable, NetSerializable]
public sealed class ESChatMessage
{
    public string FormattedMessage => string.Format(Format, Content, Name);

    /// <summary>
    /// Content of the message
    /// </summary>
    public string Content;

    /// <summary>
    /// Channel prototype of the message
    /// </summary>
    public ProtoId<ESChatChannelPrototype> Channel;

    /// <summary>
    /// Entity which sent the chat message
    /// </summary>
    public NetEntity Source;

    /// <summary>
    /// Identifier sent when <see cref="Source"/> is <see cref="NetEntity.Invalid"/>
    /// if this was sent by a player to assign a key to the sender of this message.
    /// This is unique per sender.
    /// </summary>
    public int? SourceKey;

    /// <summary>
    /// Message is not logged in chat and is only displayed in the viewport
    /// </summary>
    public bool Ephemeral;

    /// <summary>
    /// Sound played when the message is initially read
    /// </summary>
    public SoundSpecifier? Sound;

    #region Appearance
    /// <summary>
    /// Base color of the message
    /// </summary>
    public Color Color;

    /// <summary>
    /// Name used for display in the chat message
    /// </summary>
    public string Name;

    /// <summary>
    /// Optional override font for message display
    /// </summary>
    public string? Font;

    /// <summary>
    /// Optional override font size
    /// </summary>
    public int? FontSize;

    /// <summary>
    /// formatting string used to format the message for the chat.
    /// Taken in the form "{0}, {1}: {2}"
    /// </summary>
    public string Format;
    #endregion

    // TODO: fuuucccckkkkk this is gonna mess up prediction resolving
    /// <summary>
    /// Client-only: Whether this message has been read and processed
    /// </summary>
    [NonSerialized]
    public bool Read = false;

    /// <summary>
    /// Tick when this message was sent (used for resolving client-server collision)
    /// </summary>
    public GameTick Tick;

    public ESChatMessage(
        string content,
        ProtoId<ESChatChannelPrototype> channel,
        NetEntity source,
        int? sourceKey,
        bool ephemeral,
        SoundSpecifier? sound,
        Color color,
        string? name,
        string? font,
        int? fontSize,
        string format,
        GameTick tick)
    {
        Content = content;
        Channel = channel;
        Source = source;
        SourceKey = sourceKey;
        Ephemeral = ephemeral;
        Sound = sound;
        Color = color;
        Name = name ?? string.Empty;
        Font = font;
        FontSize = fontSize;
        Format = format;
        Tick = tick;
    }
}

/// <summary>
/// Net message sent from server to client to transmit message data.
/// </summary>
[UsedImplicitly]
public sealed class ESChatNetMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public ESChatMessage Message = default!;

    public ESChatNetMessage()
    {

    }

    public ESChatNetMessage(ESChatMessage message)
    {
        Message = message;
    }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var length = buffer.ReadVariableInt32();
        using var stream = new MemoryStream(length);
        buffer.ReadAlignedMemory(stream, length);
        serializer.DeserializeDirect(stream, out Message);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        var stream = new MemoryStream();
        serializer.SerializeDirect(stream, Message);
        buffer.WriteVariableInt32((int) stream.Length);
        buffer.Write(stream.AsSpan());
    }
}

[Serializable, NetSerializable]
public sealed partial class ESRequestSendChatMessage
{
    public string Text;

    public ProtoId<ESChatChannelPrototype> ChatChannel;

    public ESRequestSendChatMessage(string text, ProtoId<ESChatChannelPrototype> chatChannel)
    {
        Text = text;
        ChatChannel = chatChannel;
    }
}

public sealed class ESRequestSendChatNetMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public ESRequestSendChatMessage Message = default!;

    public ESRequestSendChatNetMessage()
    {

    }

    public ESRequestSendChatNetMessage(ESRequestSendChatMessage message)
    {
        Message = message;
    }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var length = buffer.ReadVariableInt32();
        using var stream = new MemoryStream(length);
        buffer.ReadAlignedMemory(stream, length);
        serializer.DeserializeDirect(stream, out Message);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        var stream = new MemoryStream();
        serializer.SerializeDirect(stream, Message);
        buffer.WriteVariableInt32((int) stream.Length);
        buffer.Write(stream.AsSpan());
    }
}

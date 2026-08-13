using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._ES.Chat.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Chat;

public abstract partial class ESSharedChatSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private IESSharedChatManager _chat = default!;
    [Dependency] protected ISharedPlayerManager PlayerManager = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private SharedPvsOverrideSystem _pvsOverride = default!;

    // Default channel for situations where UI *needs* a channel
    public static readonly ProtoId<ESChatChannelPrototype> LocalChannel = "Speak";
    public static readonly ProtoId<ESChatChannelPrototype> WhisperChannel = "Whisper";
    public static readonly ProtoId<ESChatChannelPrototype> EmoteChannel = "Emote";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESAfterRoundRestartCleanupEvent>(OnAfterRoundRestartCleanup);

        SubscribeLocalEvent<ESSimpleFormatChatChannelComponent, ESGetChatMessageFormatEvent>(OnSimpleGetFormat);

        InitializeNameColor();
        InitializePermissions();

        _chat.OnRequestSendChatMessage += OnRequestSendChatMessage;
    }

    private void OnAfterRoundRestartCleanup(ESAfterRoundRestartCleanupEvent ev)
    {
        // TODO: support prototype reloading
        foreach (var channel in _prototype.EnumeratePrototypes<ESChatChannelPrototype>())
        {
            TryGetProcessor(channel, out _);
        }
    }

    private void OnRequestSendChatMessage(EntityUid source, string content, ProtoId<ESChatChannelPrototype> channel)
    {
        if (!GetPermittedChannels(source).Contains(channel))
            return;

        // TODO: Slur filters happen here

        TrySendMessage(content, channel, source);
    }

    private void OnSimpleGetFormat(Entity<ESSimpleFormatChatChannelComponent> ent, ref ESGetChatMessageFormatEvent args)
    {
        args.Format = Loc.GetString(ent.Comp.Format);
    }

    public ProtoId<ESChatChannelPrototype> GetChannel(Entity<ESChatProcessorComponent?> uid)
    {
        if (!Resolve(uid, ref uid.Comp))
            return default;

        return uid.Comp.Channel;
    }

    /// <summary>
    /// Retrieves the corresponding processor entity for a given chat channel
    /// </summary>
    /// <remarks>
    /// If the processor already exists, it'll use the existing one.
    /// Otherwise, it'll initialize a new one and then reuse it for further calls.
    /// </remarks>
    private bool TryGetProcessor(ProtoId<ESChatChannelPrototype> channel, [NotNullWhen(true)] out Entity<ESChatProcessorComponent>? processor)
    {
        processor = null;

        var prototype = _prototype.Index(channel);
        if (!prototype.ChatProcessor.HasValue)
            return false;

        // Consider directly mapping these for performance.
        // Currently, this is just an O(n) linear lookup.
        // Directly caching in a dict would be O(1) but has the added complexity of needing to dump refs.
        var query = EntityQueryEnumerator<ESChatProcessorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Channel != channel)
                continue;
            processor= (uid, comp);
            return true;
        }

        var processorUid = Spawn(prototype.ChatProcessor);
        var processorComp = EnsureComp<ESChatProcessorComponent>(processorUid);
        processorComp.Channel = channel;
        Dirty(processorUid, processorComp);
        _pvsOverride.AddGlobalOverride(processorUid);

        processor = (processorUid, processorComp);
        return true;
    }

    public bool TrySendMessage(
        string content,
        ProtoId<ESChatChannelPrototype> channel,
        EntityUid source,
        string? nameOverride = null,
        bool force = false,
        bool hideChat = false,
        bool? logOverride = null)
    {
        if (!TryGetProcessor(channel, out var processer))
        {
            Log.Warning($"Tried to send message {content} from {source} on channel without processor entity {channel}");
            return false;
        }

        return TrySendMessage(
            content,
            processer.Value,
            source,
            nameOverride: nameOverride,
            force: force,
            hideChat: hideChat,
            logOverride: logOverride);
    }

    public bool TrySendMessage(
        string content,
        Entity<ESChatProcessorComponent> processor,
        EntityUid source,
        string? nameOverride = null,
        bool force = false,
        bool hideChat = false,
        bool? logOverride = null)
    {
        var originalContent = content;

        if (!force && !AttemptSendMessage(source, processor, out var fallback))
        {
            if (fallback.HasValue)
            {
                return TrySendMessage(content,
                    fallback.Value,
                    source,
                    nameOverride: nameOverride,
                    force: force,
                    hideChat: hideChat,
                    logOverride: logOverride);
            }
            return false;
        }

        var preEv = new ESPreTransformChatMessageEvent(content, source);
        RaiseLocalEvent(processor, ref preEv);

        var ev = new ESTransformChatMessageEvent(preEv.Content, source);
        RaiseLocalEvent(processor, ref ev);

        var transformedContent = ev.Content;

        // do not send empty messages
        if (string.IsNullOrWhiteSpace(transformedContent))
            return false;

        // Get the message format
        var formatEv = new ESGetChatMessageFormatEvent(transformedContent, source);
        RaiseLocalEvent(processor, ref formatEv);

        var nameEv = new ESTransformMessageSourceNameEvent(Name(source), source);
        RaiseLocalEvent(processor, ref nameEv);

        var postNameEv = new ESPostTransformMessageSourceNameEvent(nameEv.Name, source);
        RaiseLocalEvent(processor, ref postNameEv);

        var name = postNameEv.Name;

        foreach (var recipient in GetMessageRecipients(source, processor))
        {
            var recipientEv = new ESRecipientTransformChatMessageEvent(transformedContent, source, recipient);
            RaiseLocalEvent(processor, ref recipientEv);

            var recipientContent = recipientEv.Content;

            // do not send empty messages
            if (string.IsNullOrWhiteSpace(recipientContent))
                continue;

            if (PlayerManager.TryGetSessionByEntity(recipient, out var session))
            {
                _chat.SendChatMessage(
                    recipientContent,
                    session,
                    processor.Comp.Channel,
                    source,
                    formatEv.Format,
                    ephemeral: hideChat,
                    name: nameOverride ?? name,
                    font: formatEv.Font,
                    fontSize: formatEv.FontSize,
                    color: formatEv.Color,
                    recordReplay: false); // Don't record replays for each individual message, record the "canon" message afterwards
            }

            var receivedEvent = new ESChatMessageReceivedEvent(source, recipientContent, processor.Comp.Channel);
            RaiseLocalEvent(recipient, ref receivedEvent);
            RaiseLocalEvent(processor, ref receivedEvent);
        }

        _chat.RecordReplayChatMessage(
            transformedContent,
            processor.Comp.Channel,
            source,
            formatEv.Format,
            name: nameOverride ?? name,
            font: formatEv.Font,
            fontSize: formatEv.FontSize,
            color: formatEv.Color);

        var sentEv = new ESChatMessageSentEvent(source, transformedContent, processor.Comp.Channel);
        RaiseLocalEvent(processor, ref sentEv);

        if (logOverride != false && PlayerManager.TryGetSessionByEntity(source, out _))
        {
            _adminLog.Add(
                LogType.Chat,
                LogImpact.Low,
                $"{ToPrettyString(source)} sent message on {processor.Comp.Channel}. Original: {originalContent}, Transformed: {transformedContent}");
        }

        return true;
    }

    private bool AttemptSendMessage(
        EntityUid source,
        Entity<ESChatProcessorComponent> processor,
        out ProtoId<ESChatChannelPrototype>? fallback)
    {
        var sourceEv = new ESSendChatMessageAttemptEvent(source, processor.Comp.Channel);
        RaiseLocalEvent(source, ref sourceEv);

        fallback = sourceEv.FallbackChannel;
        if (sourceEv.Canceled)
            return false;

        var processorEv = new ESSendChatMessageAttemptEvent(source, processor.Comp.Channel);
        RaiseLocalEvent(processor, ref processorEv);

        fallback = processorEv.FallbackChannel;
        if (processorEv.Canceled)
            return false;

        return true;
    }

    /// <summary>
    /// Gets the recipients (people who will be sent the message) for a message
    /// sent by the given source on the specified channel
    /// </summary>
    private IEnumerable<EntityUid> GetMessageRecipients(
        EntityUid source,
        Entity<ESChatProcessorComponent> processor)
    {
        var sourceEv = new ESGetChatMessageRecipientsEvent(source);
        RaiseLocalEvent(source, ref sourceEv);

        var processorEv = new ESGetChatMessageRecipientsEvent(source);
        RaiseLocalEvent(processor, ref processorEv);

        foreach (var recipient in sourceEv.GetRecipients().Concat(processorEv.GetRecipients()).Distinct())
        {
            var recipientEv = new ESReceiveChatMessageAttemptEvent(source);
            RaiseLocalEvent(recipient, ref recipientEv);

            if (recipientEv.Canceled)
                continue;

            yield return recipient;
        }
    }
}

/// <summary>
/// Event raised on a chat message source and processor when a message is attempted to be sent over a channel.
/// </summary>
[ByRefEvent]
public record struct ESSendChatMessageAttemptEvent(EntityUid Source, ProtoId<ESChatChannelPrototype> Channel)
{
    /// <summary>
    /// The message's source
    /// </summary>
    public readonly EntityUid Source = Source;

    public readonly ProtoId<ESChatChannelPrototype> Channel = Channel;

    /// <summary>
    /// Optional channel that will be used as fallback if this is canceled.
    /// </summary>
    public ProtoId<ESChatChannelPrototype>? FallbackChannel;

    public bool Canceled { get; private set; } = false;

    public void Cancel()
    {
        Canceled = true;
    }
}

/// <summary>
/// Event raised on both the source and processor of a chat message to determine who will receive the message.
/// Recipients collected through this event may be further filtered via <see cref="ESReceiveChatMessageAttemptEvent"/>
/// </summary>
[ByRefEvent]
public record struct ESGetChatMessageRecipientsEvent(EntityUid Source)
{
    /// <summary>
    /// The message's source
    /// </summary>
    public readonly EntityUid Source = Source;

    private readonly HashSet<EntityUid> _recipients = new();

    public void AddRecipient(params EntityUid[] recipients)
    {
        foreach (var recipient in recipients)
        {
            _recipients.Add(recipient);
        }
    }

    public void AddRecipient(IEnumerable<EntityUid> recipients)
    {
        foreach (var recipient in recipients)
        {
            _recipients.Add(recipient);
        }
    }

    public IEnumerable<EntityUid> GetRecipients()
    {
        return _recipients;
    }
}

[ByRefEvent]
public record struct ESPreTransformChatMessageEvent(string Content, EntityUid Source)
{
    /// <summary>
    /// The original string sent
    /// </summary>
    public readonly string OriginalContent = Content;

    /// <summary>
    /// The modified message.
    /// </summary>
    public string Content = Content;

    /// <summary>
    /// The message's source
    /// </summary>
    public readonly EntityUid Source = Source;
}

/// <summary>
/// Event raised once on the chat processor entity per chat message.
/// </summary>
[ByRefEvent]
public record struct ESTransformChatMessageEvent(string Content, EntityUid Source)
{
    /// <summary>
    /// The original string sent
    /// </summary>
    public readonly string OriginalContent = Content;

    /// <summary>
    /// The modified message.
    /// </summary>
    public string Content = Content;

    /// <summary>
    /// The message's source
    /// </summary>
    public readonly EntityUid Source = Source;
}

/// <summary>
/// Event raised on a chat processor to determine how the message's content and name will be formatted.
/// </summary>
[ByRefEvent]
public record struct ESGetChatMessageFormatEvent(string Content, EntityUid Source)
{
    /// <summary>
    /// The original message sent
    /// </summary>
    public readonly string Content = Content;

    /// <summary>
    /// The message's source
    /// </summary>
    public readonly EntityUid Source = Source;

    /// <summary>
    /// Formatting string that will be used to
    /// </summary>
    public string Format = IESSharedChatManager.DefaultFormat;

    /// <summary>
    /// Override message font size
    /// </summary>
    public int? FontSize = null;

    /// <summary>
    /// Override message font
    /// </summary>
    public string? Font = null;

    /// <summary>
    /// Override message color
    /// </summary>
    public Color? Color = null;
}

/// <summary>
/// Event raised on a chat processor to determine how the message's content and name will be formatted.
/// </summary>
[ByRefEvent]
public record struct ESGetPostChatMessageFormatEvent(string Content, EntityUid Source, string Format, int? FontSize, string? Font)
{
    /// <summary>
    /// The original message sent
    /// </summary>
    public readonly string Content = Content;

    /// <summary>
    /// The message's source
    /// </summary>
    public readonly EntityUid Source = Source;

    /// <summary>
    /// Formatting string that will be used to
    /// </summary>
    public string Format = Format;

    /// <summary>
    /// Override message font size
    /// </summary>
    public int? FontSize = FontSize;

    /// <summary>
    /// Override message font
    /// </summary>
    public string? Font = Font;
}

[ByRefEvent]
public record struct ESTransformMessageSourceNameEvent(string Name, EntityUid Source)
{
    public readonly EntityUid Source = Source;

    public string Name = Name;
}

[ByRefEvent]
public record struct ESPostTransformMessageSourceNameEvent(string Name, EntityUid Source)
{
    public readonly EntityUid Source = Source;

    public string Name = Name;
}

/// <summary>
/// Event raised on a chat processor entity per recipient of a chat message to modify its content.
/// This is the final modification done to the text itself before being displayed.
/// </summary>
[ByRefEvent]
public record struct ESRecipientTransformChatMessageEvent(string Content, EntityUid Source, EntityUid Recipient)
{
    /// <summary>
    /// The original string sent
    /// </summary>
    public readonly string OriginalContent = Content;

    /// <summary>
    /// The modified message.
    /// </summary>
    public string Content = Content;

    /// <summary>
    /// The message's source
    /// </summary>
    public readonly EntityUid Source = Source;

    /// <summary>
    /// The message's recipient
    /// </summary>
    public readonly EntityUid Recipient = Recipient;
}

/// <summary>
/// Event raised on a potential recipient of a chat message in order to determine if they are actually capable of receiving this.
/// By default, recipients retrieved via <see cref="ESGetChatMessageRecipientsEvent"/> will be sent the chat message.
/// However, this event allows the behavior to be canceled.
/// </summary>
[ByRefEvent]
public record struct ESReceiveChatMessageAttemptEvent(EntityUid Source)
{
    /// <summary>
    /// The message's source
    /// </summary>
    public readonly EntityUid Source = Source;

    public bool Canceled { get; private set; } = false;

    public void Cancel()
    {
        Canceled = true;
    }
}

[ByRefEvent]
public record struct ESChatMessageReceivedEvent(EntityUid Source, string Content, ProtoId<ESChatChannelPrototype> Channel)
{
    public readonly EntityUid Source = Source;

    public readonly string Content = Content;

    public readonly ProtoId<ESChatChannelPrototype> Channel = Channel;
}

[ByRefEvent]
public record struct ESChatMessageSentEvent(EntityUid Source, string Content, ProtoId<ESChatChannelPrototype> Channel)
{
    public readonly EntityUid Source = Source;

    public readonly string Content = Content;

    public readonly ProtoId<ESChatChannelPrototype> Channel = Channel;
}

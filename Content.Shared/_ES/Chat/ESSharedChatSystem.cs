using System.Linq;
using Content.Shared._ES.Chat.Components;
using Content.Shared.GameTicking;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Chat;

public abstract partial class ESSharedChatSystem : EntitySystem
{
    [Dependency] private IESSharedChatManager _chat = default!;
    [Dependency] protected ISharedPlayerManager PlayerManager = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private SharedPvsOverrideSystem _pvsOverride = default!;

    // Default channel for situations where UI *needs* a channel
    public static readonly ProtoId<ESChatChannelPrototype> DefaultChannel = "Speak";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESAfterRoundRestartCleanupEvent>(OnAfterRoundRestartCleanup);

        SubscribeLocalEvent<ESChatPermissionsComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ESChatPermissionsComponent, ESGetChatPermissionsEvent>(OnGetChatPermissions);

        SubscribeLocalEvent<ESSimpleFormatChatChannelComponent, ESGetChatMessageFormatEvent>(OnSimpleGetFormat);

        InitializeNameColor();

        _chat.OnRequestSendChatMessage += OnRequestSendChatMessage;
    }

    private void OnAfterRoundRestartCleanup(ESAfterRoundRestartCleanupEvent ev)
    {
        // TODO: support prototype reloading
        foreach (var channel in _prototype.EnumeratePrototypes<ESChatChannelPrototype>())
        {
            GetProcessor(channel);
        }
    }

    private void OnStartup(Entity<ESChatPermissionsComponent> ent, ref ComponentStartup args)
    {
        RefreshChatPermissions(ent.AsNullable());
    }

    private void OnGetChatPermissions(Entity<ESChatPermissionsComponent> ent, ref ESGetChatPermissionsEvent args)
    {
        foreach (var channel in ent.Comp.InherentChannels)
        {
            args.Channels.Add(channel);
        }
    }

    private void OnRequestSendChatMessage(EntityUid source, ESRequestSendChatMessage msg)
    {
        if (!GetPermittedChannels(source).Contains(msg.ChatChannel))
            return;

        TrySendMessage(msg.Text, msg.ChatChannel, source);
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
    private Entity<ESChatProcessorComponent> GetProcessor(ProtoId<ESChatChannelPrototype> channel)
    {
        // TODO: Consider directly mapping these for performance.
        // Currently this is just an O(n) linear lookup.
        // Directly caching in a dict would be O(1) but has the added complexity of needing to dump refs.
        var query = EntityQueryEnumerator<ESChatProcessorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Channel == channel)
                return (uid, comp);
        }

        var prototype = _prototype.Index(channel);
        var processorUid = Spawn(prototype.ChatProcessor);
        var processorComp = EnsureComp<ESChatProcessorComponent>(processorUid);
        processorComp.Channel = channel;
        Dirty(processorUid, processorComp);
        _pvsOverride.AddGlobalOverride(processorUid);

        return (processorUid, processorComp);
    }

    public bool TrySendMessage(
        string content,
        ProtoId<ESChatChannelPrototype> channel,
        EntityUid source)
    {
        var processer = GetProcessor(channel);
        return TrySendMessage(
            content,
            processer,
            source);
    }

    public bool TrySendMessage(
        string content,
        Entity<ESChatProcessorComponent> processor,
        EntityUid source)
    {
        // TODO: Generic ratelimiting

        if (!CanSendMessage(source, processor))
        {
            // TODO: Attempt to coerce channel into a sendable type.
            // If someone talks but is only able to whisper, attempt to resend
            // the message as a whisper automatically.
            return false;
        }

        // TODO: Chat filtering occurs here

        var preEv = new ESPreTransformChatMessageEvent(content, source);
        RaiseLocalEvent(processor, ref preEv);

        var ev = new ESTransformChatMessageEvent(preEv.Content, source);
        RaiseLocalEvent(processor, ref ev);

        var transformedContent = ev.Content;

        // BUG: If an event zeroes out this content and sends their own message (emote sanitization), this will register as a failure.
        // do not send empty messages
        if (string.IsNullOrWhiteSpace(transformedContent))
            return false;

        // Get the message format
        var formatEv = new ESGetChatMessageFormatEvent(transformedContent, source);
        RaiseLocalEvent(processor, ref formatEv);

        foreach (var recipient in GetMessageRecipients(source, processor))
        {
            if (!PlayerManager.TryGetSessionByEntity(recipient, out var session))
                continue;

            var nameEv = new ESTransformMessageSourceNameEvent(Name(source), source, recipient);
            RaiseLocalEvent(processor, ref nameEv);

            var postNameEv = new ESPostTransformMessageSourceNameEvent(nameEv.Name, source, recipient);
            RaiseLocalEvent(processor, ref postNameEv);

            var name = postNameEv.Name;

            var recipientEv = new ESRecipientTransformChatMessageEvent(transformedContent, source, recipient);
            RaiseLocalEvent(processor, ref recipientEv);

            var recipientContent = recipientEv.Content;

            // do not send empty messages
            if (string.IsNullOrWhiteSpace(recipientContent))
                continue;

            // TODO: Don't record messages for replays here. Otherwise, we'll log the same message multiple times.
            // Instead, record the "canonical" message after this loop.
            _chat.SendChatMessage(
                recipientContent,
                session,
                processor.Comp.Channel,
                source,
                formatEv.Format,
                name: name);
        }

        // TODO: Entity spoke event

        // TODO: Logging

        return true;
    }

    public bool CanSendMessage(
        EntityUid source,
        ProtoId<ESChatChannelPrototype> channel)
    {
        var processor = GetProcessor(channel);
        return CanSendMessage(source, processor);
    }

    public bool CanSendMessage(
        EntityUid source,
        Entity<ESChatProcessorComponent> processor)
    {
        var sourceEv = new ESSendChatMessageAttemptEvent(source);
        RaiseLocalEvent(source, ref sourceEv);

        if (sourceEv.Canceled)
            return false;

        var processorEv = new ESSendChatMessageAttemptEvent(source);
        RaiseLocalEvent(processor, ref processorEv);

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

    public virtual void RefreshChatPermissions(Entity<ESChatPermissionsComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var ev = new ESGetChatPermissionsEvent(ent);
        RaiseLocalEvent(ent, ref ev, true);

        ent.Comp.PermittedChannels = ev.Channels;
        Dirty(ent);
    }

    public HashSet<ProtoId<ESChatChannelPrototype>> GetPermittedChannels(Entity<ESChatPermissionsComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return [ DefaultChannel ];

        return ent.Comp.PermittedChannels;
    }
}

/// <summary>
/// Event raised on a chat message source and processor when a message is attempted to be sent over a channel.
/// </summary>
[ByRefEvent]
public record struct ESSendChatMessageAttemptEvent(EntityUid Source)
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
}

[ByRefEvent]
public record struct ESTransformMessageSourceNameEvent(string Name, EntityUid Source, EntityUid Recipient)
{
    public readonly EntityUid Source = Source;

    public readonly EntityUid Recipient = Recipient;

    public string Name = Name;
}

[ByRefEvent]
public record struct ESPostTransformMessageSourceNameEvent(string Name, EntityUid Source, EntityUid Recipient)
{
    public readonly EntityUid Source = Source;

    public readonly EntityUid Recipient = Recipient;

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

/// <summary>
/// Event broadcast and raised on an entity to determine what chat channels they can send from.
/// </summary>
[ByRefEvent]
public record struct ESGetChatPermissionsEvent(EntityUid Source)
{
    public readonly EntityUid Source = Source;

    public readonly HashSet<ProtoId<ESChatChannelPrototype>> Channels = [];
}

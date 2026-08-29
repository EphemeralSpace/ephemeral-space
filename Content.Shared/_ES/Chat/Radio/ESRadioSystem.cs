using Content.Shared._ES.Chat.Radio.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Radio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Chat.Radio;

public sealed partial class ESRadioSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ESSharedChatSystem _chat = default!;
    [Dependency] private SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private EntityQuery<TelecomExemptComponent> _exemptQuery;
    [Dependency] private SharedAudioSystem _audio = default!;

    private static readonly SoundSpecifier RadioClick = new SoundCollectionSpecifier("ESRadioClick");

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESRadioReceiverComponent, ESGetRadioChannelsEvent>(OnGetRadioChannels);

        SubscribeLocalEvent<ESWhisperRadioChatChannelComponent, ESChatMessageSentEvent>(OnChatMessageSent);

        SubscribeLocalEvent<ESRadioChatChannelComponent, ESChatMessageSentEvent>(OnRadioChatMessageSent);
        SubscribeLocalEvent<ESRadioChatChannelComponent, ESSendChatMessageAttemptEvent>(OnSendChatMessageAttempt);
        SubscribeLocalEvent<ESRadioChatChannelComponent, ESGetChatMessageRecipientsEvent>(OnGetRecipients);
        SubscribeLocalEvent<ESRadioChatChannelComponent, ESTransformChatMessageEvent>(OnRecipientTransformChatMessage);
    }

    private void OnGetRadioChannels(Entity<ESRadioReceiverComponent> ent, ref ESGetRadioChannelsEvent args)
    {
        foreach (var channel in ent.Comp.IntrinsicChannels)
        {
            args.Channels.Add(channel);
        }
    }

    private void OnChatMessageSent(Entity<ESWhisperRadioChatChannelComponent> ent, ref ESChatMessageSentEvent args)
    {
        //TODO: maybe dont do name override and instead split the name derivation out from the message transforming for speach
        _chat.TrySendMessage(args.Content, ent.Comp.RadioChannel, args.Source, nameOverride: FormattedMessage.RemoveMarkupPermissive(args.Name));
    }

    private void OnRadioChatMessageSent(Entity<ESRadioChatChannelComponent> ent, ref ESChatMessageSentEvent args)
    {
        _audio.PlayPvs(RadioClick, args.Source, AudioParams.Default.WithVolume(-5f).WithMaxDistance(5f));
    }

    private void OnSendChatMessageAttempt(Entity<ESRadioChatChannelComponent> ent, ref ESSendChatMessageAttemptEvent args)
    {
        var source = Transform(args.Source);
        var channel = _chat.GetChannel(ent.Owner);

        var needsServer = !_exemptQuery.HasComp(args.Source) && !ent.Comp.RequireServer;
        if (!needsServer && !HasActiveServer(channel, source.MapID))
        {
            args.Cancel();
            return;
        }

        var sendAttemptEv = new RadioSendAttemptEvent(channel, args.Source);
        RaiseLocalEvent(ref sendAttemptEv);
        RaiseLocalEvent(args.Source, ref sendAttemptEv);
        if (sendAttemptEv.Cancelled)
        {
            args.Cancel();
        }
    }

    private void OnGetRecipients(Entity<ESRadioChatChannelComponent> ent, ref ESGetChatMessageRecipientsEvent args)
    {
        var channel = _chat.GetChannel(ent.Owner);

        var query = EntityQueryEnumerator<ESRadioReceiverComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Channels.Contains(channel))
                continue;

            var attemptEv = new RadioReceiveAttemptEvent(channel, args.Source, uid);
            RaiseLocalEvent(ref attemptEv);
            RaiseLocalEvent(uid, ref attemptEv);
            if (attemptEv.Cancelled)
                continue;

            args.AddRecipient(uid);
        }
    }

    private void OnRecipientTransformChatMessage(Entity<ESRadioChatChannelComponent> ent, ref ESTransformChatMessageEvent args)
    {
        if (IsGlobalDistortActive())
            args.Content = DistortRadioMessage(args.Content, 0.6f, _prototype, _random, Loc);
    }

    public void RefreshRadioChannels(EntityUid uid)
    {
        var ev = new ESGetRadioChannelsEvent();
        RaiseLocalEvent(uid, ref ev);

        if (ev.Channels.Count == 0)
        {
            RemComp<ESRadioReceiverComponent>(uid);
        }
        else
        {
            var comp = EnsureComp<ESRadioReceiverComponent>(uid);
            comp.Channels = ev.Channels;
            Dirty(uid, comp);
        }
    }

    /// <summary>
    /// Checks if a given chat channel has a corresponding telecom server.
    /// </summary>
    public bool HasActiveServer(ProtoId<ESChatChannelPrototype> channelId, MapId map)
    {
        foreach (var (uid, _, keys, xform) in EntityQueryEnumerator<TelecomServerComponent, EncryptionKeyHolderComponent, TransformComponent>())
        {
            if (xform.MapID != map)
                continue;

            if (_powerReceiver.IsPowered(uid) &&
                keys.Channels.Contains(channelId))
            {
                return true;
            }
        }
        return false;
    }
}

/// <summary>
/// Event raised on an entity to determine what radio channels they can hear
/// </summary>
[ByRefEvent]
public record struct ESGetRadioChannelsEvent()
{
    public HashSet<ProtoId<ESChatChannelPrototype>> Channels = new();
}

/// <summary>
/// Use this event to cancel sending message to every receiver
/// </summary>
[ByRefEvent]
public record struct RadioSendAttemptEvent(ProtoId<ESChatChannelPrototype> Channel, EntityUid RadioSource)
{
    public readonly ProtoId<ESChatChannelPrototype> Channel = Channel;
    public readonly EntityUid RadioSource = RadioSource;
    public bool Cancelled = false;
}

/// <summary>
/// Use this event to cancel sending message per receiver
/// </summary>
[ByRefEvent]
public record struct RadioReceiveAttemptEvent(ProtoId<ESChatChannelPrototype> Channel, EntityUid RadioSource, EntityUid RadioReceiver)
{
    public readonly ProtoId<ESChatChannelPrototype> Channel = Channel;
    public readonly EntityUid RadioSource = RadioSource;
    public readonly EntityUid RadioReceiver = RadioReceiver;
    public bool Cancelled = false;
}

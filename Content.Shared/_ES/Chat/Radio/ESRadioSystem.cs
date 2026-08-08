using Content.Shared._ES.Chat.Radio.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Chat.Radio;

public sealed partial class ESRadioSystem : EntitySystem
{
    [Dependency] private ESSharedChatSystem _chat = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESRadioReceiverComponent, ESGetRadioChannelsEvent>(OnGetRadioChannels);

        SubscribeLocalEvent<ESRadioChatChannelComponent, ESSendChatMessageAttemptEvent>(OnSendChatMessageAttempt);
        SubscribeLocalEvent<ESRadioChatChannelComponent, ESGetChatMessageRecipientsEvent>(OnGetRecipients);
    }

    private void OnGetRadioChannels(Entity<ESRadioReceiverComponent> ent, ref ESGetRadioChannelsEvent args)
    {
        foreach (var channel in ent.Comp.IntrinsicChannels)
        {
            args.Channels.Add(channel);
        }
    }

    private void OnSendChatMessageAttempt(Entity<ESRadioChatChannelComponent> ent, ref ESSendChatMessageAttemptEvent args)
    {
        // TODO
    }

    private void OnGetRecipients(Entity<ESRadioChatChannelComponent> ent, ref ESGetChatMessageRecipientsEvent args)
    {
        var channel = _chat.GetChannel(ent.Owner);

        var query = EntityQueryEnumerator<ESRadioReceiverComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // TODO: marker component for receive all channels
            if (!comp.Channels.Contains(channel))
                continue;

            args.AddRecipient(uid);
        }
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
}

/// <summary>
/// Event raised on an entity to determine what radio channels they can hear
/// </summary>
[ByRefEvent]
public record struct ESGetRadioChannelsEvent()
{
    public HashSet<ProtoId<ESChatChannelPrototype>> Channels = new();
}

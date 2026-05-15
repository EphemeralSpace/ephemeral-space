using Content.Shared._ES.Chat.Processor.Components;
using Content.Shared.Ghost;

namespace Content.Shared._ES.Chat.Processor;

public sealed class ESGhostListenableChatChannelSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESGhostListenableChatChannelComponent, ESGetChatMessageRecipientsEvent>(OnGetRecipients);
    }

    private void OnGetRecipients(Entity<ESGhostListenableChatChannelComponent> ent, ref ESGetChatMessageRecipientsEvent args)
    {
        var query = EntityQueryEnumerator<GhostHearingComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            args.AddRecipient(uid);
        }
    }
}

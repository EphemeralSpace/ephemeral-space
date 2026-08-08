using Content.Server._ES.Chat.Radio.Components;
using Content.Shared._ES.Chat;
using Content.Shared.Radio.Components;

namespace Content.Server._ES.Chat.Radio;

public sealed partial class ESRadioChatChannelSystem : EntitySystem
{
    [Dependency] private ESChatSystem _chat = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESRadioChatChannelComponent, ESGetChatMessageRecipientsEvent>(OnGetRecipients);
    }

    private void OnGetRecipients(Entity<ESRadioChatChannelComponent> ent, ref ESGetChatMessageRecipientsEvent args)
    {
        var channel = _chat.GetChannel(ent.Owner);

        var query = EntityQueryEnumerator<ActiveRadioComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {

        }
    }
}

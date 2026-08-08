using Content.Shared._ES.Chat.Processor.Components;
using Content.Shared._ES.Lobby.Components;

namespace Content.Shared._ES.Chat.Processor;

public sealed class ESTheaterChatChannelSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESTheatergoerChatChannelComponent, ESGetChatMessageRecipientsEvent>(OnGetChatMessageRecipients);
    }

    private void OnGetChatMessageRecipients(Entity<ESTheatergoerChatChannelComponent> ent, ref ESGetChatMessageRecipientsEvent args)
    {
        foreach (var entity in AllEntityQuery<ESTheatergoerMarkerComponent>())
        {
            args.AddRecipient(entity);
        }
    }
}

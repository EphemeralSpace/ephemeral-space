using Content.Shared._ES.Chat.Processor.Components;
using Robust.Shared.Player;

namespace Content.Shared._ES.Chat.Processor;

public sealed partial class ESGlobalChatChannelSystem : EntitySystem
{
    [Dependency] private ISharedPlayerManager _player = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESGlobalChatChannelComponent, ESGetChatMessageRecipientsEvent>(OnGetRecipients);
    }

    private void OnGetRecipients(Entity<ESGlobalChatChannelComponent> ent, ref ESGetChatMessageRecipientsEvent args)
    {
        foreach (var session in _player.Sessions)
        {
            if (session.AttachedEntity.HasValue)
                args.AddRecipient(session.AttachedEntity.Value);
        }
    }
}

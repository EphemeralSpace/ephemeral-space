using Content.Shared._ES.Chat.Components;
using Content.Shared.Chat;

namespace Content.Shared._ES.Chat;

public sealed partial class ESEmoteSystem : EntitySystem
{
    [Dependency] private SharedChatSystem _chat = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESEmoteChatChannelComponent, ESChatMessageSentEvent>(OnChatMessageSent);
    }

    private void OnChatMessageSent(Entity<ESEmoteChatChannelComponent> ent, ref ESChatMessageSentEvent args)
    {
        _chat.TryEmoteChatInput(args.Source, args.Content);
    }
}

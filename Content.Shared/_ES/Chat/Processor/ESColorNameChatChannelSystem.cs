using Content.Shared._ES.Chat.Processor.Components;

namespace Content.Shared._ES.Chat.Processor;

public sealed partial class ESColorNameChatChannelSystem : EntitySystem
{
    [Dependency] private ESSharedChatSystem _chat = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESColorNameChatChannelComponent, ESPostTransformMessageSourceNameEvent>(OnPostTransformMessageSourceName);
    }

    private void OnPostTransformMessageSourceName(Entity<ESColorNameChatChannelComponent> ent, ref ESPostTransformMessageSourceNameEvent args)
    {
        var color = _chat.GetChatColor(args.Name);

        args.Name = Loc.GetString("es-chat-color-name-fmt",
            ("color", color),
            ("name", args.Name));
    }
}

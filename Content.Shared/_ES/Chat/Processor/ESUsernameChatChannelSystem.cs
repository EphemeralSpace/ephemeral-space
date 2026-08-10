using Content.Shared._ES.Chat.Processor.Components;
using Robust.Shared.Player;

namespace Content.Shared._ES.Chat.Processor;

public sealed partial class ESUsernameChatChannelSystem : EntitySystem
{
    [Dependency] private ISharedPlayerManager _player = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESUsernameChatChannelComponent, ESTransformMessageSourceNameEvent>(OnTransformName);
    }

    private void OnTransformName(Entity<ESUsernameChatChannelComponent> ent, ref ESTransformMessageSourceNameEvent args)
    {
        args.Name = string.Empty;
        if (_player.TryGetSessionByEntity(args.Source, out var session) &&
            _player.TryGetPlayerData(session.UserId, out var data))
            args.Name = data.UserName;
    }
}

using Content.Server._ES.Chat.Processor.Components;
using Content.Server.Preferences.Managers;
using Content.Shared._ES.Chat;
using Robust.Shared.Player;

namespace Content.Server._ES.Chat.Processor;

public sealed partial class ESUsernameChatChannelSystem : EntitySystem
{
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private IServerPreferencesManager _preferences = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESUsernameChatChannelComponent, ESGetChatMessageFormatEvent>(OnGetChatMessageFormat);
        SubscribeLocalEvent<ESUsernameChatChannelComponent, ESTransformMessageSourceNameEvent>(OnTransformName);
    }

    private void OnGetChatMessageFormat(Entity<ESUsernameChatChannelComponent> ent, ref ESGetChatMessageFormatEvent args)
    {
        if (!_player.TryGetSessionByEntity(args.Source, out var session))
            return;

        var prefs = _preferences.GetPreferences(session.UserId);
        args.Color = prefs.AdminOOCColor;
    }

    private void OnTransformName(Entity<ESUsernameChatChannelComponent> ent, ref ESTransformMessageSourceNameEvent args)
    {
        args.Name = string.Empty;
        if (!_player.TryGetSessionByEntity(args.Source, out var session) ||
            !_player.TryGetPlayerData(session.UserId, out var data))
            return;

        args.Name = data.UserName;
    }
}

using Content.Shared.Chat;
using Content.Shared._ES.Chat;
using Robust.Shared.Prototypes;

namespace Content.Server.Speech;

public sealed partial class EmotesMenuSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private ESEmoteSystem _emote = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<PlayEmoteMessage>(OnPlayEmote);
    }

    private void OnPlayEmote(PlayEmoteMessage msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession.AttachedEntity;
        if (!player.HasValue)
            return;

        if (!_prototypeManager.Resolve(msg.ProtoId, out var proto) || proto.ChatTriggers.Count == 0)
            return;

        _emote.TryEmoteWithChat(player.Value, msg.ProtoId);
    }
}

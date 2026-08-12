using Content.Shared._ES.Chat.Processor.Components;
using Content.Shared.ActionBlocker;
using Robust.Shared.Player;

namespace Content.Shared._ES.Chat.Processor;

public sealed partial class ESActionBlockerChatChannelSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private EntityQuery<ActorComponent> _actorQuery = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESRequireCanSpeakChatChannelComponent, ESSendChatMessageAttemptEvent>(OnSendChatMessageAttempt);
        SubscribeLocalEvent<ESRequireCanEmoteChatChannelComponent, ESSendChatMessageAttemptEvent>(OnSendEmoteChatMessageAttempt);
    }

    private void OnSendChatMessageAttempt(Entity<ESRequireCanSpeakChatChannelComponent> ent, ref ESSendChatMessageAttemptEvent args)
    {
        if (!_actorQuery.HasComp(args.Source))
            return;

        if (!_actionBlocker.CanSpeak(args.Source))
            args.Cancel();
    }

    private void OnSendEmoteChatMessageAttempt(Entity<ESRequireCanEmoteChatChannelComponent> ent, ref ESSendChatMessageAttemptEvent args)
    {
        if (!_actorQuery.HasComp(args.Source))
            return;

        if (!_actionBlocker.CanEmote(args.Source))
            args.Cancel();
    }
}

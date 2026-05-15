using Content.Shared._ES.Chat.Processor.Components;
using Content.Shared.ActionBlocker;

namespace Content.Shared._ES.Chat.Processor;

public sealed partial class ESRequireCanSpeakChatChannelSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESRequireCanSpeakChatChannelComponent, ESSendChatMessageAttemptEvent>(OnSendChatMessageAttempt);
    }

    private void OnSendChatMessageAttempt(Entity<ESRequireCanSpeakChatChannelComponent> ent, ref ESSendChatMessageAttemptEvent args)
    {
        if (!_actionBlocker.CanSpeak(args.Source))
            args.Cancel();
    }
}

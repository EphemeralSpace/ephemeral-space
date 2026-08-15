using Content.Shared._ES.Chat;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed class SilencedStatusEffectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SilencedStatusEffectComponent, StatusEffectRelayedEvent<ESSendChatMessageAttemptEvent>>(OnSendChatMessageAttempt);
    }

    private void OnSendChatMessageAttempt(Entity<SilencedStatusEffectComponent> ent, ref StatusEffectRelayedEvent<ESSendChatMessageAttemptEvent> args)
    {
        // Way more brittle than it ought to be
        if (args.Args.Channel != ESSharedChatSystem.LocalChannel)
            return;
        var ev = args.Args with { FallbackChannel = ESSharedChatSystem.WhisperChannel };
        ev.Cancel();

        args.Args = ev;
    }
}

using Content.Shared._ES.Chat;
using Content.Shared._Offbrand.Wounds;

namespace Content.Shared._ES.Wounds;

public sealed partial class MessageOnHeartstopSystem : EntitySystem
{
    [Dependency] private ESEmoteSystem _emote = default!;

    [SubscribeLocalEvent]
    private void OnHeartStopped(Entity<MessageOnHeartstopComponent> ent, ref HeartStoppedEvent args)
    {
        if (ent.Comp.Message is not { } message)
            return;
        _emote.TryEmoteWithChat(ent.Owner, message, ignoreActionBlocker: true);
    }
}
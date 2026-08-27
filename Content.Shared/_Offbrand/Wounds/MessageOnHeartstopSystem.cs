using Content.Shared._ES.Chat;
using Content.Shared._Offbrand.Wounds;

public sealed partial class MessageOnHeartstopSystem : EntitySystem
{
    [Dependency] private ESEmoteSystem _emote = default!;

    [SubscribeLocalEvent]
    private void OnHeartStopped(Entity<MessageOnHeartstopComponent> ent, ref HeartStoppedEvent args)
    {
        if (ent.Comp.Message is null)
            return;
        _emote.TryEmoteWithChat(ent.Owner, ent.Comp.Message.Value, ignoreActionBlocker: true);
    }
}
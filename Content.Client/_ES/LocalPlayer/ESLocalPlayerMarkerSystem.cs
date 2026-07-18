using Robust.Shared.Player;

namespace Content.Client._ES.LocalPlayer;

public sealed class ESLocalPlayerMarkerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnAttach);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnDetach);
    }

    private void OnAttach(LocalPlayerAttachedEvent ev)
    {
        EnsureComp<ESLocalPlayerMarkerComponent>(ev.Entity);
    }

    private void OnDetach(LocalPlayerDetachedEvent ev)
    {
        if (TerminatingOrDeleted(ev.Entity))
            return;

        RemCompDeferred<ESLocalPlayerMarkerComponent>(ev.Entity);
    }
}

using Content.Shared._ES.Breakable.Components;
using Content.Shared.UserInterface;

namespace Content.Shared._ES.Breakable;

public sealed partial class ESBreakableActivatableUiSystem : EntitySystem
{
    [Dependency] private ESBreakableSystem _breakable = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESBreakableActivatableUiComponent, ActivatableUIOpenAttemptEvent>(OnOpenAttempt);
    }

    private void OnOpenAttempt(Entity<ESBreakableActivatableUiComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (_breakable.IsBroken(ent.Owner))
            args.Cancel();
    }
}

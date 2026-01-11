using Content.Shared._ES.Destructible.Components;
using Content.Shared.Destructible;

namespace Content.Shared._ES.Destructible;

public sealed class ESDestroyOnUnanchorSystem : EntitySystem
{
    [Dependency] private readonly SharedDestructibleSystem _destructible = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESDestroyOnUnanchorComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
    }

    private void OnAnchorStateChanged(Entity<ESDestroyOnUnanchorComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored || args.Detaching)
            return;

        if (TerminatingOrDeleted(ent))
            return;

        _destructible.DestroyEntity(ent.Owner);
    }
}
